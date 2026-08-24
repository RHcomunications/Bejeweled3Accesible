using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Bejeweled3Accessible.Audio;

namespace Bejeweled3Accessible.AndroidApp.Audio
{
    public class AndroidSoundEngine : IDisposable
    {
        private readonly Context _context;
        private readonly SoundPool _soundPool;
        private readonly Dictionary<string, int> _soundMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
                .SetMaxStreams(16)
                .SetAudioAttributes(attributes)
                .Build();

            PreloadSounds();
        }

        private void PreloadSounds()
        {
            try
            {
                string[] assets = _context.Assets?.List("sounds") ?? Array.Empty<string>();
                foreach (string file in assets)
                {
                    string nameNoExt = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        AssetFileDescriptor afd = _context.Assets.OpenFd("sounds/" + file);
                        int soundId = _soundPool.Load(afd, 1);
                        _soundMap[nameNoExt] = soundId;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void PlaySound(string key, float volume = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (_soundMap.TryGetValue(key, out int soundId))
            {
                _soundPool.Play(soundId, volume, volume, 1, 0, 1.0f);
            }
        }

        public void PlaySoundSpatial(string key, int col, int row, float baseVol = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (_soundMap.TryGetValue(key, out int soundId))
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
                string fileName = trackName;
                if (!fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".mp3";
                }

                AssetFileDescriptor afd = _context.Assets?.OpenFd("music/" + fileName);
                if (afd != null)
                {
                    _musicPlayer = new MediaPlayer();
                    _musicPlayer.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
                    _musicPlayer.Prepare();
                    _musicPlayer.Looping = loop;
                    _musicPlayer.SetVolume(0.7f, 0.7f);
                    _musicPlayer.Start();
                    _currentMusicTrack = trackName;
                }
            }
            catch
            {
                // Fallback silencioso si el archivo de musica no esta en assets
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
