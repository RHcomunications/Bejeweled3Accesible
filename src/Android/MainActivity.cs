using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;
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
        private TouchBoardView _boardView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _talkBack = new TalkBackBridge(this);
            _board = new Board(new Random().Next());
            _boardView = new TouchBoardView(this, _board, _talkBack);

            SetContentView(_boardView);
            _talkBack.Speak("Bejeweled 3 Accesible. Toca o desliza en la pantalla para jugar.", true);
        }

        protected override void OnDestroy()
        {
            _talkBack?.Dispose();
            base.OnDestroy();
        }
    }
}