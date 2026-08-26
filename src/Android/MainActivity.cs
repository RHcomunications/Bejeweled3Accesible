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
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
              Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private TalkBackBridge _talkBack;
        private AndroidSoundEngine _sound;
        private NativeMenuManager _menuManager;
        private GameScreenView _gameView;
        private bool _resumeMusicOnReturn = false;

        public void SetDesiredOrientation(bool landscape)
        {
            RequestedOrientation = landscape ? ScreenOrientation.SensorLandscape : ScreenOrientation.Portrait;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            EnableFullScreen();

            Localization.UseAndroidStrings = true;

            string dataDir = FilesDir.AbsolutePath;
            StoragePaths.ResolveDataDirectory(dataDir);
            ProfileManager.OverrideDataDirectory = dataDir;
            GameProgress.OverrideDataDirectory = dataDir;
            GameOptions.OverrideDataDirectory = dataDir;
            BadgeManager.OverrideDataDirectory = dataDir;

            _sound = new AndroidSoundEngine(this);
            _talkBack = new TalkBackBridge(this);

            _menuManager = new NativeMenuManager(this, _talkBack, _sound, (modeKey) =>
            {
                StartGameBoard(modeKey);
            });

            _menuManager.ShowLoadingScreen();
        }

        public void StartGameBoard(string modeKey)
        {
            SetDesiredOrientation(true);
            _gameView = new GameScreenView(this, _talkBack, _sound, modeKey, () =>
            {
                _menuManager.ShowPauseMenu();
            });
            SetContentView(_gameView);
        }

        public void ResumeGame()
        {
            if (_gameView != null)
            {
                SetDesiredOrientation(true);
                SetContentView(_gameView);
                _gameView.Resume();
            }
            else
            {
                _menuManager.ShowMainMenu();
            }
        }

        public void RestartGame()
        {
            if (_gameView != null)
            {
                StartGameBoard(_gameView.CurrentModeKey);
            }
            else
            {
                _menuManager.ShowMainMenu();
            }
        }

        public void ReturnToMainMenu()
        {
            _sound?.StopMusic();
            _menuManager.ShowMainMenu();
        }

        protected override void OnResume()
        {
            base.OnResume();
            EnableFullScreen();
            if (_resumeMusicOnReturn && _gameView != null && _gameView.CurrentScreen == AndroidGameScreen.Playing)
            {
                _gameView.ResumePlayback();
                _gameView.Invalidate();
            }
            _resumeMusicOnReturn = false;
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            // Con ConfigurationChanges declarado, la Activity NO se recrea al
            // rotar: solo avisamos a la vista para que se redibuje con las
            // nuevas dimensiones y seguimos en la partida en curso.
            base.OnConfigurationChanged(newConfig);
            EnableFullScreen();
            _gameView?.Invalidate();
            _gameView?.RefreshAccessibilityStructure();
            _gameView?.AnnounceCurrentMenu();
        }

        protected override void OnPause()
        {
            _resumeMusicOnReturn = (_gameView != null && _gameView.CurrentScreen == AndroidGameScreen.Playing);
            _sound?.StopMusic();
            base.OnPause();
        }

        protected override void OnStop()
        {
            _sound?.StopMusic();
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            _sound?.Dispose();
            _talkBack?.Dispose();
            base.OnDestroy();
        }

        private void EnableFullScreen()
        {
            // Modo inmersivo (sticky): oculta barra de estado y de navegacion y
            // reaparecen solo con un deslizamiento desde el borde. Funciona en
            // todas las versiones soportadas (API 21+), incluida R+, donde los
            // flags legacy siguen vigentes.
#pragma warning disable CS0618
            var flags = SystemUiFlags.LayoutStable
                      | SystemUiFlags.LayoutHideNavigation
                      | SystemUiFlags.LayoutFullscreen
                      | SystemUiFlags.HideNavigation
                      | SystemUiFlags.Fullscreen
                      | SystemUiFlags.ImmersiveSticky;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)flags;
#pragma warning restore CS0618
        }
    }
}