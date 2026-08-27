using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.Content.Res;
using Android.Media;
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

        public int MusicVol { get; set; } = 80;
        public int SfxVol { get; set; } = 100;
        public int VoiceVol { get; set; } = 100;
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

            lock (_soundMap)
            {
                if (_soundMap.TryGetValue(key, out int id))
                {
                    return id;
                }

                if (_assetSoundFiles.TryGetValue(key, out string file))
                {
                    try
                    {
                        AssetFileDescriptor afd = _context.Assets.OpenFd("sounds/" + file);
                        int soundId = _soundPool.Load(afd, 1);
                        _soundMap[key] = soundId;
                        _soundMap[Path.GetFileNameWithoutExtension(file)] = soundId;
                        return soundId;
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("BejeweledAudio", "Error cargando sonido " + key + ": " + ex.Message);
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

        public void PlaySoundSpatialPan(float pan, float depth, string key, float baseVol = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            bool isVoice = key.StartsWith("voice_", StringComparison.OrdinalIgnoreCase);
            float master = isVoice ? (VoiceVol / 100f) : (SfxVol / 100f);
            float scaledBase = baseVol * master;

            int soundId = EnsureSoundLoaded(key);
            if (soundId > 0)
            {
                // Mismo modelo espacial que el motor de Windows (GridSpatializer):
                // paneo equal-power (cos/sin) + atenuacion por profundidad de fila.
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
                        _soundPool.Play(soundId, leftVol, rightVol, 1, 0, 1.0f);
                    }
                    else
                    {
                        _pendingSounds.Add(new PendingSound { SoundId = soundId, LeftVol = leftVol, RightVol = rightVol });
                    }
                }
            }
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
        }
    }
}
