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

        public void OnLoadComplete(SoundPool soundPool, int sampleId, int status)
        {
            if (status == 0)
            {
                lock (_soundMap)
                {
                    _loadedSoundIds.Add(sampleId);
                }
            }
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

                // Pre-cargar sonidos principales del menu y tablero de inmediato
                EnsureSoundLoaded("select");
                EnsureSoundLoaded("button_mouseover");
                EnsureSoundLoaded("button_press");
                EnsureSoundLoaded("gem_hit");
                EnsureSoundLoaded("combo_1");
                EnsureSoundLoaded("voice_getready");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledAudio", "Error al listar assets: " + ex.Message);
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

            int soundId = EnsureSoundLoaded(key);
            if (soundId > 0)
            {
                _soundPool.Play(soundId, volume, volume, 1, 0, 1.0f);
            }
        }

        public void PlaySoundSpatial(string key, int col, int row, float baseVol = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            int soundId = EnsureSoundLoaded(key);
            if (soundId > 0)
            {
                float pan = SpatialAudio.PanColumn(col);
                float leftVol = baseVol * Math.Min(1.0f, 1.0f - pan);
                float rightVol = baseVol * Math.Min(1.0f, 1.0f + pan);
                _soundPool.Play(soundId, leftVol, rightVol, 1, 0, 1.0f);
            }
        }

        public void PlayMusic(string trackName, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(trackName) || _currentMusicTrack == trackName) return;

            StopMusic();

            try
            {
                string baseName = Path.GetFileNameWithoutExtension(trackName);
                string[] musicAssets = _context.Assets?.List("music") ?? Array.Empty<string>();

                string targetFile = null;
                foreach (string m in musicAssets)
                {
                    if (m.StartsWith(baseName, StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileNameWithoutExtension(m).Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetFile = m;
                        break;
                    }
                }

                if (targetFile != null)
                {
                    AssetFileDescriptor afd = _context.Assets.OpenFd("music/" + targetFile);
                    if (afd != null)
                    {
                        _musicPlayer = new MediaPlayer();
                        _musicPlayer.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
                        _musicPlayer.Prepare();
                        _musicPlayer.Looping = loop;
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

        public void StopMusic()
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

        public void Dispose()
        {
            StopMusic();
            _soundPool?.Release();
        }
    }
}
