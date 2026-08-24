using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;
using Bejeweled3Accessible.AndroidApp.Audio;
using Bejeweled3Accessible.AndroidApp.UI;

namespace Bejeweled3Accessible.AndroidApp
{
    [Activity(Label = "@string/app_name",
              MainLauncher = true,
              ScreenOrientation = ScreenOrientation.Portrait,
              Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private Board _board;
        private TalkBackBridge _talkBack;
        private AndroidSoundEngine _sound;
        private TouchBoardView _boardView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _sound = new AndroidSoundEngine(this);
            _talkBack = new TalkBackBridge(this);
            _board = new Board(new Random().Next());
            _boardView = new TouchBoardView(this, _board, _talkBack, _sound);

            SetContentView(_boardView);
            _sound.PlayMusic(MusicMap.AllTrackKeys[0]); // Pista de inicio / Menu
            _talkBack.Speak("Bejeweled 3 Accesible. Toca o desliza en la pantalla para jugar.", true);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _sound?.PlayMusic(MusicMap.AllTrackKeys[0]);
        }

        protected override void OnPause()
        {
            _sound?.StopMusic();
            base.OnPause();
        }

        protected override void OnDestroy()
        {
            _sound?.Dispose();
            _talkBack?.Dispose();
            base.OnDestroy();
        }
    }
}