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
              ScreenOrientation = ScreenOrientation.SensorLandscape,
              Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private TalkBackBridge _talkBack;
        private AndroidSoundEngine _sound;
        private GameScreenView _screenView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _sound = new AndroidSoundEngine(this);
            _talkBack = new TalkBackBridge(this);

            _screenView = new GameScreenView(this, _talkBack, _sound);
            SetContentView(_screenView);
        }

        protected override void OnResume()
        {
            base.OnResume();
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