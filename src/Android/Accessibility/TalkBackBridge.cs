using System;
using Android.Content;
using Android.Speech.Tts;
using Java.Util;

namespace Bejeweled3Accessible.AndroidApp.Accessibility
{
    public class TalkBackBridge : Java.Lang.Object, TextToSpeech.IOnInitListener
    {
        private readonly TextToSpeech _tts;
        private bool _isReady = false;

        public TalkBackBridge(Context context)
        {
            _tts = new TextToSpeech(context, this);
        }

        public void OnInit(OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                _tts.SetLanguage(Locale.Default);
                _isReady = true;
            }
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_isReady || string.IsNullOrWhiteSpace(text)) return;
            var queueMode = interrupt ? QueueMode.Flush : QueueMode.Add;
            _tts.Speak(text, queueMode, null, null);
        }

        public void Stop()
        {
            if (_isReady) _tts.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tts?.Stop();
                _tts?.Shutdown();
            }
            base.Dispose(disposing);
        }
    }
}
