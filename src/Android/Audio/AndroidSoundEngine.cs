using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Java.Nio;
using Bejeweled3Accessible.Audio;

namespace Bejeweled3Accessible.AndroidApp.Audio
{
    public class AndroidSoundEngine : Java.Lang.Object, SoundPool.IOnLoadCompleteListener, IDisposable
    {
        private readonly Context _context;
        private readonly SoundPool _soundPool;
        private readonly Dictionary<string, int> _soundMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _loadedSoundIds = new HashSet<int>();
        private readonly Dictionary<string, string> _assetSoundFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private MediaPlayer _musicPlayer;
        private string _currentMusicTrack;

        // Reproductor del modulo real (Bejeweled3_suite.mo3) via libopenmpt/AudioTrack.
        private AndroidModulePlayer _modulePlayer;
        private string _currentModuleTrack;
        private byte[] _mo3Bytes;

        // Cache de PCM mono decodificado (modelo binaural completo tipo Windows).
        // Si la decodificacion OGG->PCM falla en un dispositivo, el sonido se marca
        // en _pcmFailed y se cae al paneo equal-power de SoundPool (.10).
        private readonly Dictionary<string, MonoSamples> _pcmCache = new Dictionary<string, MonoSamples>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pcmFailed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<SpatialVoice> _activeVoices = new List<SpatialVoice>();

        public int MusicVol { get; set; } = 80;
        public int SfxVol { get; set; } = 100;
        public int VoiceVol { get; set; } = 100;
        public Engine.Language VoiceLanguage { get; set; } = Engine.Language.Spanish;
        public bool BinauralEnabled { get; set; } = true;

        public void UpdateMusicVolume()
        {
            float vol = (MusicVol / 100f) * 0.85f;
            if (_musicPlayer != null)
            {
                try { _musicPlayer.SetVolume(vol, vol); } catch { }
            }
            if (_modulePlayer != null && _modulePlayer.IsValid)
            {
                try { _modulePlayer.SetVolume(vol); } catch { }
            }
        }

        public AndroidSoundEngine(Context context)
        {
            _context = context;

            var attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Game)
                .SetContentType(AudioContentType.Sonification)
                .Build();

            _soundPool = new SoundPool.Builder()
                .SetMaxStreams(32)
                .SetAudioAttributes(attributes)
                .Build();

            _soundPool.SetOnLoadCompleteListener(this);

            IndexAssetSounds();

            // Pre-decodificar en segundo plano los SFX espaciales mas frecuentes
            // para no encadenar decodificaciones OGG en el hilo de juego (cascadas).
            Task.Run(() => WarmSpatialCache());
        }

        private void WarmSpatialCache()
        {
            try
            {
                string[] common = { "gem_hit", "combo_1", "combo_2", "combo_3" };
                foreach (var k in common)
                {
                    if (BinauralEnabled) GetMonoSamples(k);
                }
            }
            catch { }
        }

        private void IndexAssetSounds()
        {
            try
            {
                string[] assets = _context.Assets?.List("sounds") ?? Array.Empty<string>();
                foreach (string file in assets)
                {
                    string nameNoExt = Path.GetFileNameWithoutExtension(file);
                    _assetSoundFiles[nameNoExt] = file;
                    _assetSoundFiles[file] = file;
                }

                // Pre-cargar todos los sonidos frecuentes y voces del locutor de inmediato
                EnsureSoundLoaded("select");
                EnsureSoundLoaded("button_mouseover");
                EnsureSoundLoaded("button_press");
                EnsureSoundLoaded("gem_hit");
                EnsureSoundLoaded("backtomain");
                EnsureSoundLoaded("menuspin");
                EnsureSoundLoaded("combo_1");
                EnsureSoundLoaded("combo_2");
                EnsureSoundLoaded("combo_3");
                EnsureSoundLoaded("voice_welcometobejeweled");
                EnsureSoundLoaded("voice_welcomeback");
                EnsureSoundLoaded("voice_getready");
                EnsureSoundLoaded("voice_go");
                EnsureSoundLoaded("voice_good");
                EnsureSoundLoaded("voice_excellent");
                EnsureSoundLoaded("voice_awesome");
                EnsureSoundLoaded("voice_spectacular");
                EnsureSoundLoaded("voice_extraordinary");
                EnsureSoundLoaded("voice_unbelievable");
                EnsureSoundLoaded("voice_levelcomplete");
                EnsureSoundLoaded("voice_gameover");
                EnsureSoundLoaded("voice_nomoremoves");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledAudio", "Error al listar assets: " + ex.Message);
            }
        }

        private readonly List<PendingSound> _pendingSounds = new List<PendingSound>();

        private struct PendingSound
        {
            public int SoundId;
            public float LeftVol;
            public float RightVol;
        }

        public void OnLoadComplete(SoundPool soundPool, int sampleId, int status)
        {
            if (status == 0)
            {
                lock (_soundMap)
                {
                    _loadedSoundIds.Add(sampleId);
                    for (int i = _pendingSounds.Count - 1; i >= 0; i--)
                    {
                        if (_pendingSounds[i].SoundId == sampleId)
                        {
                            var ps = _pendingSounds[i];
                            _soundPool.Play(ps.SoundId, ps.LeftVol, ps.RightVol, 1, 0, 1.0f);
                            _pendingSounds.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private int EnsureSoundLoaded(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return -1;

            string targetKey = key;
            if (key.StartsWith("voice_", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = (VoiceLanguage == Engine.Language.Spanish) ? "_es" : "_en";
                string localizedKey = key + suffix;
                if (_assetSoundFiles.ContainsKey(localizedKey))
                {
                    targetKey = localizedKey;
                }
            }

            lock (_soundMap)
            {
                if (_soundMap.TryGetValue(targetKey, out int id))
                {
                    return id;
                }

                if (_assetSoundFiles.TryGetValue(targetKey, out string file))
                {
                    try
                    {
                        AssetFileDescriptor afd = _context.Assets.OpenFd("sounds/" + file);
                        int soundId = _soundPool.Load(afd, 1);
                        _soundMap[targetKey] = soundId;
                        _soundMap[Path.GetFileNameWithoutExtension(file)] = soundId;
                        return soundId;
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("BejeweledAudio", "Error cargando sonido " + targetKey + ": " + ex.Message);
                    }
                }
            }
            return -1;
        }

        public void PlaySound(string key, float volume = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            bool isVoice = key.StartsWith("voice_", StringComparison.OrdinalIgnoreCase);
            float master = isVoice ? (VoiceVol / 100f) : (SfxVol / 100f);
            float finalVol = volume * master;

            int soundId = EnsureSoundLoaded(key);
            if (soundId > 0)
            {
                lock (_soundMap)
                {
                    if (_loadedSoundIds.Contains(soundId))
                    {
                        _soundPool.Play(soundId, finalVol, finalVol, 1, 0, 1.0f);
                    }
                    else
                    {
                        // Evitar registrar duplicados en la cola si ya está encolado
                        bool alreadyPending = false;
                        for (int i = 0; i < _pendingSounds.Count; i++)
                        {
                            if (_pendingSounds[i].SoundId == soundId)
                            {
                                alreadyPending = true;
                                break;
                            }
                        }
                        if (!alreadyPending)
                        {
                            _pendingSounds.Add(new PendingSound { SoundId = soundId, LeftVol = finalVol, RightVol = finalVol });
                        }
                    }
                }
            }
        }

        public void PlaySoundSpatial(string key, int col, int row, float baseVol = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            float pan = BinauralEnabled ? SpatialAudio.PanColumn(col) : 0f;
            float depth = BinauralEnabled ? SpatialAudio.DepthForRow(row) : 0f;
            PlaySoundSpatialPan(pan, depth, key, baseVol);
        }

        public void PlaySoundSpatialPan(float pan, float depth, string key, float baseVol = 1.0f, float rate = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            bool isVoice = key.StartsWith("voice_", StringComparison.OrdinalIgnoreCase);
            float master = isVoice ? (VoiceVol / 100f) : (SfxVol / 100f);
            float scaledBase = baseVol * master;

            // Paneo equal-power + atenuacion por profundidad via SoundPool.
            int soundId = EnsureSoundLoaded(key);
            if (soundId > 0)
            {
                float t = (pan + 1.0f) * 0.5f;
                float lg = (float)Math.Cos(t * Math.PI * 0.5f);
                float rg = (float)Math.Sin(t * Math.PI * 0.5f);
                float depthVol = scaledBase * SpatialAudio.VolumeForDepth(depth);
                float leftVol = depthVol * lg;
                float rightVol = depthVol * rg;
                lock (_soundMap)
                {
                    if (_loadedSoundIds.Contains(soundId))
                    {
                        _soundPool.Play(soundId, leftVol, rightVol, 1, 0, ClampRate(rate));
                    }
                    else
                    {
                        _pendingSounds.Add(new PendingSound { SoundId = soundId, LeftVol = leftVol, RightVol = rightVol });
                    }
                }
            }
        }

        public void PlaySoundPitch(string key, float pitch)
        {
            PlaySoundSpatialPan(0f, 0f, key, 1.0f, pitch);
        }

        public void PlaySoundSpatialPitch(string key, int col, int row, float pitch)
        {
            float pan = BinauralEnabled ? SpatialAudio.PanColumn(col) : 0f;
            float depth = BinauralEnabled ? SpatialAudio.DepthForRow(row) : 0f;
            PlaySoundSpatialPan(pan, depth, key, 1.0f, pitch);
        }

        private static float ClampRate(float r)
        {
            if (r <= 0f) return 1.0f;
            if (r < 0.5f) return 0.5f;
            if (r > 2.0f) return 2.0f;
            return r;
        }

        private static float[] ResampleMono(float[] src, float factor)
        {
            if (src == null || src.Length == 0) return src;
            int n = (int)(src.Length / factor);
            if (n < 1) n = 1;
            float[] dst = new float[n];
            for (int i = 0; i < n; i++)
            {
                float pos = i * factor;
                int i0 = (int)pos;
                int i1 = Math.Min(i0 + 1, src.Length - 1);
                float frac = pos - i0;
                dst[i] = src[i0] * (1f - frac) + src[i1] * frac;
            }
            return dst;
        }

        private MonoSamples GetMonoSamples(string key)
        {
            lock (_pcmCache)
            {
                if (_pcmFailed.Contains(key)) return null;
                if (_pcmCache.TryGetValue(key, out MonoSamples cached)) return cached;
            }
            MonoSamples decoded = DecodeMonoSamples(key);
            lock (_pcmCache)
            {
                if (decoded == null) _pcmFailed.Add(key);
                else _pcmCache[key] = decoded;
                return decoded;
            }
        }

        private MonoSamples DecodeMonoSamples(string key)
        {
            if (!_assetSoundFiles.TryGetValue(key, out string file)) return null;
            try
            {
                using (var afd = _context.Assets.OpenFd("sounds/" + file))
                {
                    var extractor = new MediaExtractor();
                    try
                    {
                        extractor.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
                        int trackCount = extractor.TrackCount;
                        int audioTrack = -1;
                        for (int i = 0; i < trackCount; i++)
                        {
                            var tf = extractor.GetTrackFormat(i);
                            string mime = tf.GetString(MediaFormat.KeyMime);
                            if (mime != null && mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                            {
                                audioTrack = i;
                                break;
                            }
                        }
                        if (audioTrack < 0) return null;
                        var fmt = extractor.GetTrackFormat(audioTrack);
                        extractor.SelectTrack(audioTrack);
                        int sampleRate = fmt.ContainsKey(MediaFormat.KeySampleRate) ? fmt.GetInteger(MediaFormat.KeySampleRate) : 44100;
                        int channels = fmt.ContainsKey(MediaFormat.KeyChannelCount) ? fmt.GetInteger(MediaFormat.KeyChannelCount) : 1;
                        int pcmEnc = fmt.ContainsKey(MediaFormat.KeyPcmEncoding)
                            ? fmt.GetInteger(MediaFormat.KeyPcmEncoding)
                            : (int)Android.Media.Encoding.Pcm16bit;

                        var codec = MediaCodec.CreateDecoderByType(fmt.GetString(MediaFormat.KeyMime));
                        if (codec == null) return null;
                        try
                        {
                            codec.Configure(fmt, null, null, 0);
                            codec.Start();
                            var bufferInfo = new MediaCodec.BufferInfo();
                            var pcmList = new List<byte>(2048);
                            bool sawInputEos = false;
                            bool sawOutputEos = false;
                            int guard = 0;
                            while (!sawOutputEos && guard++ < 200000)
                            {
                                if (!sawInputEos)
                                {
                                    int inIdx = codec.DequeueInputBuffer(5000);
                                    if (inIdx >= 0)
                                    {
                                        var inBuf = codec.GetInputBuffer(inIdx);
                                        inBuf.Clear();
                                        int size = extractor.ReadSampleData(inBuf, 0);
                                        if (size < 0)
                                        {
                                            codec.QueueInputBuffer(inIdx, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                                            sawInputEos = true;
                                        }
                                        else
                                        {
                                            long pts = extractor.SampleTime;
                                            codec.QueueInputBuffer(inIdx, 0, size, pts, 0);
                                            extractor.Advance();
                                        }
                                    }
                                }
                                int outIdx = codec.DequeueOutputBuffer(bufferInfo, 5000);
                                if (outIdx >= 0)
                                {
                                    if ((bufferInfo.Flags & MediaCodecBufferFlags.EndOfStream) != 0) sawOutputEos = true;
                                    if (bufferInfo.Size > 0)
                                    {
                                        var outBuf = codec.GetOutputBuffer(outIdx);
                                        outBuf.Position(bufferInfo.Offset);
                                        outBuf.Limit(bufferInfo.Offset + bufferInfo.Size);
                                        byte[] chunk = new byte[bufferInfo.Size];
                                        outBuf.Get(chunk);
                                        pcmList.AddRange(chunk);
                                    }
                                    codec.ReleaseOutputBuffer(outIdx, false);
                                    if (sawOutputEos) break;
                                }
                                else if (outIdx == (int)MediaCodecInfoState.OutputFormatChanged)
                                {
                                    var nf = codec.OutputFormat;
                                    if (nf.ContainsKey(MediaFormat.KeySampleRate)) sampleRate = nf.GetInteger(MediaFormat.KeySampleRate);
                                    if (nf.ContainsKey(MediaFormat.KeyChannelCount)) channels = nf.GetInteger(MediaFormat.KeyChannelCount);
                                    if (nf.ContainsKey(MediaFormat.KeyPcmEncoding)) pcmEnc = nf.GetInteger(MediaFormat.KeyPcmEncoding);
                                }
                            }

                            float[] mono = DownmixToMono(pcmList.ToArray(), channels, pcmEnc);
                            return mono == null ? null : new MonoSamples { Data = mono, SampleRate = sampleRate };
                        }
                        finally
                        {
                            try { codec.Stop(); } catch { }
                            try { codec.Release(); } catch { }
                        }
                    }
                    finally
                    {
                        try { extractor.Release(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledAudio", "Decode OGG fallo " + key + ": " + ex.Message);
                return null;
            }
        }

        private static float[] DownmixToMono(byte[] pcm, int channels, int pcmEnc)
        {
            if (pcm == null || pcm.Length == 0) return null;
            channels = Math.Max(1, channels);
            if (pcmEnc == (int)Android.Media.Encoding.PcmFloat)
            {
                int bytesPerSample = 4;
                int frames = (pcm.Length / bytesPerSample) / channels;
                if (frames <= 0) return null;
                float[] mono = new float[frames];
                for (int i = 0; i < frames; i++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++)
                        sum += BitConverter.ToSingle(pcm, (i * channels + c) * bytesPerSample);
                    mono[i] = sum / channels;
                }
                return mono;
            }
            else
            {
                int bytesPerSample = 2;
                int frames = (pcm.Length / bytesPerSample) / channels;
                if (frames <= 0) return null;
                float[] mono = new float[frames];
                for (int i = 0; i < frames; i++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++)
                        sum += BitConverter.ToInt16(pcm, (i * channels + c) * bytesPerSample) / 32768f;
                    mono[i] = sum / channels;
                }
                return mono;
            }
        }

        private void PlayStereoFrames(float[] stereo, int sampleRate)
        {
            int shortsLen = stereo.Length;
            short[] pcm16 = new short[shortsLen];
            for (int i = 0; i < shortsLen; i++)
            {
                float v = stereo[i];
                if (v > 1f) v = 1f; else if (v < -1f) v = -1f;
                pcm16[i] = (short)(v * 32767f);
            }
            int frames = shortsLen / 2;

            var attrs = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Game)
                .SetContentType(AudioContentType.Sonification)
                .Build();
            var afmt = new AudioFormat.Builder()
                .SetEncoding(Android.Media.Encoding.Pcm16bit)
                .SetSampleRate(sampleRate)
                .SetChannelMask(ChannelOut.Stereo)
                .Build();
            int minBuf = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Stereo, Android.Media.Encoding.Pcm16bit);
            int bufSize = Math.Max(minBuf, pcm16.Length * 2);
            var track = new AudioTrack(attrs, afmt, bufSize, AudioTrackMode.Static, 0);
            int written = track.Write(pcm16, 0, pcm16.Length);
            if (written <= 0) { track.Release(); return; }

            var voice = new SpatialVoice(this, track);
            lock (_activeVoices)
            {
                if (_activeVoices.Count >= 40)
                {
                    var old = _activeVoices[0];
                    _activeVoices.RemoveAt(0);
                    old.ForceRelease();
                }
                _activeVoices.Add(voice);
            }
            track.SetPlaybackPositionUpdateListener(voice);
            track.SetNotificationMarkerPosition(frames);
            track.Play();
        }

        public void PlayMusic(string trackName, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(trackName)) return;

            // Pista del modulo real (01-23): reproducir con libopenmpt/AudioTrack
            // igual que Windows. Si la libreria nativa no esta disponible en el
            // dispositivo, se usa el fallback de mp3 por separado (misma musica,
            // sin regresion).
            int order = MusicMap.OrderForFile(trackName);
            if (order >= 0 && AndroidModulePlayer.IsAvailable)
            {
                StopMediaPlayerMusic();

                if (_modulePlayer == null)
                {
                    byte[] mo3 = LoadMo3Bytes();
                    if (mo3 != null && mo3.Length > 0) _modulePlayer = AndroidModulePlayer.TryCreate(mo3);
                }

                if (_modulePlayer != null && _modulePlayer.IsValid)
                {
                    if (_currentModuleTrack != trackName)
                    {
                        _modulePlayer.SeekTo(order, MusicMap.NextOffsetAfter(order));
                        if (!_modulePlayer.IsPlaying) _modulePlayer.Start();
                        else _modulePlayer.SetVolume((MusicVol / 100f) * 0.85f);
                        _currentModuleTrack = trackName;
                    }
                    return;
                }
                // Si el modulo fallo al crear, cae al mp3 por separado.
            }

            // Fallback: archivos mp3 sueltos (ambientales 24-29 o modulo no disponible).
            PlayMusicFile(trackName, loop);
        }

        private byte[] LoadMo3Bytes()
        {
            if (_mo3Bytes != null) return _mo3Bytes;
            try
            {
                using (var s = _context.Assets.Open("music/" + MusicMap.ModuleFile))
                using (var ms = new System.IO.MemoryStream())
                {
                    s.CopyTo(ms);
                    _mo3Bytes = ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledAudio", "No se cargo el modulo MO3: " + ex.Message);
            }
            return _mo3Bytes;
        }

        private void StopMediaPlayerMusic()
        {
            if (_musicPlayer != null)
            {
                try
                {
                    if (_musicPlayer.IsPlaying) _musicPlayer.Stop();
                    _musicPlayer.Release();
                }
                catch { }
                _musicPlayer = null;
                _currentMusicTrack = null;
            }
        }

        private void PlayMusicFile(string trackName, bool loop)
        {
            if (string.IsNullOrWhiteSpace(trackName) || _currentMusicTrack == trackName) return;

            try
            {
                string baseName = Path.GetFileNameWithoutExtension(trackName);
                string[] musicAssets = _context.Assets?.List("music") ?? Array.Empty<string>();

                string targetFile = null;
                foreach (string m in musicAssets)
                {
                    if (m.StartsWith(baseName, StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileNameWithoutExtension(m).Equals(baseName, StringComparison.OrdinalIgnoreCase) ||
                        m.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetFile = m;
                        break;
                    }
                }

                // Fallback si no existe la pista
                if (targetFile == null && musicAssets.Length > 0)
                {
                    targetFile = musicAssets[0];
                }

                if (targetFile != null)
                {
                    AssetFileDescriptor afd = _context.Assets.OpenFd("music/" + targetFile);
                    if (afd != null)
                    {
                        _musicPlayer = new MediaPlayer();
                        _musicPlayer.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
                        _musicPlayer.Prepare();

                        // Enlace automatico y continuo para las 4 partes de Clasico y Zen
                        if (IsClassicTrack(targetFile) || IsZenTrack(targetFile))
                        {
                            _musicPlayer.Looping = false;
                            _musicPlayer.SetOnCompletionListener(new MusicCompletionListener(this, targetFile));
                        }
                        else
                        {
                            _musicPlayer.Looping = loop;
                        }

                        _musicPlayer.SetVolume(0.85f, 0.85f);
                        _musicPlayer.Start();
                        _currentMusicTrack = trackName;
                    }
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledMusic", "Error al reproducir musica " + trackName + ": " + ex.Message);
            }
        }

        private static bool IsClassicTrack(string filename)
        {
            return filename.IndexOf("Classic Mode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsZenTrack(string filename)
        {
            return filename.IndexOf("Zen - Part", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private class MusicCompletionListener : Java.Lang.Object, MediaPlayer.IOnCompletionListener
        {
            private readonly AndroidSoundEngine _engine;
            private readonly string _finishedTrack;

            public MusicCompletionListener(AndroidSoundEngine engine, string finishedTrack)
            {
                _engine = engine;
                _finishedTrack = finishedTrack;
            }

            public void OnCompletion(MediaPlayer mp)
            {
                string nextTrack = GetNextChainedTrack(_finishedTrack);
                if (!string.IsNullOrEmpty(nextTrack))
                {
                    _engine.PlayMusic(nextTrack, false);
                }
            }

            private string GetNextChainedTrack(string current)
            {
                if (current.IndexOf("Part 1", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return current.IndexOf("Zen", StringComparison.OrdinalIgnoreCase) >= 0 ? "12 - Zen - Part 2 - Schein Zwei" : "04 - Classic Mode - Part 2";
                }
                if (current.IndexOf("Part 2", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return current.IndexOf("Zen", StringComparison.OrdinalIgnoreCase) >= 0 ? "13 - Zen - Part 3 - The Return" : "05 - Classic Mode - Part 3";
                }
                if (current.IndexOf("Part 3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return current.IndexOf("Zen", StringComparison.OrdinalIgnoreCase) >= 0 ? "14 - Zen - Part 4" : "06 - Classic Mode - Part 4";
                }
                if (current.IndexOf("Part 4", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return current.IndexOf("Zen", StringComparison.OrdinalIgnoreCase) >= 0 ? "11 - Zen - Part 1" : "03 - Classic Mode - Part 1";
                }
                return null;
            }
        }

        public void StopMusic()
        {
            StopMediaPlayerMusic();
            if (_modulePlayer != null)
            {
                try { _modulePlayer.Stop(); } catch { }
                try { _modulePlayer.Dispose(); } catch { }
                _modulePlayer = null;
                _currentModuleTrack = null;
            }
        }

        public void Dispose()
        {
            StopMusic();
            _soundPool?.Release();
            lock (_activeVoices)
            {
                foreach (var v in _activeVoices) v.ForceRelease();
                _activeVoices.Clear();
            }
        }

        private sealed class MonoSamples
        {
            public float[] Data;
            public int SampleRate;
        }

        private sealed class SpatialVoice : Java.Lang.Object, AudioTrack.IOnPlaybackPositionUpdateListener
        {
            private readonly AndroidSoundEngine _owner;
            private readonly AudioTrack _track;

            public SpatialVoice(AndroidSoundEngine owner, AudioTrack track)
            {
                _owner = owner;
                _track = track;
            }

            public void OnMarkerReached(AudioTrack track)
            {
                ForceRelease();
            }

            public void OnPeriodicNotification(AudioTrack track)
            {
            }

            public void ForceRelease()
            {
                try { _track.Release(); } catch { }
                lock (_owner._activeVoices) _owner._activeVoices.Remove(this);
            }
        }
    }
}
