using System;
using Android.Content;
using Android.Speech.Tts;
using Android.Views;
using Android.Views.Accessibility;
using Java.Util;

namespace Bejeweled3Accessible.AndroidApp.Accessibility
{
    public class TalkBackBridge : Java.Lang.Object, TextToSpeech.IOnInitListener
    {
        private readonly Context _context;
        private readonly AccessibilityManager _accessibilityManager;
        private View _attachedView;
        private TextToSpeech _tts;
        private bool _isTtsReady = false;
        private string _pendingInitialSpeech = null;

        public TalkBackBridge(Context context)
        {
            _context = context;
            _accessibilityManager = (AccessibilityManager)context.GetSystemService(Context.AccessibilityService);
            try
            {
                _tts = new TextToSpeech(context, this);
            }
            catch (Exception)
            {
                _tts = null;
            }
        }

        public void AttachView(View view)
        {
            _attachedView = view;
        }

        public bool IsScreenReaderActive
        {
            get
            {
                return _accessibilityManager != null && 
                       _accessibilityManager.IsEnabled && 
                       _accessibilityManager.IsTouchExplorationEnabled;
            }
        }

        public void OnInit(OperationResult status)
        {
            if (status == OperationResult.Success && _tts != null)
            {
                _tts.SetLanguage(Locale.Default);
                _isTtsReady = true;
                if (!string.IsNullOrWhiteSpace(_pendingInitialSpeech))
                {
                    Speak(_pendingInitialSpeech, true);
                    _pendingInitialSpeech = null;
                }
            }
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Si TalkBack / Lector de pantalla está activo, enviar evento nativo de accesibilidad
            // Esto garantiza que se use el sintetizador propio del usuario (Vocalizer, Eloquence, etc.)
            if (_attachedView != null && _accessibilityManager != null && _accessibilityManager.IsEnabled)
            {
                try
                {
                    _attachedView.AnnounceForAccessibility(text);
                    return;
                }
                catch (Exception) { }
            }

            // 2. Fallback con TTS interno si el usuario no tiene TalkBack encendido
            if (_tts != null)
            {
                if (!_isTtsReady)
                {
                    _pendingInitialSpeech = text;
                    return;
                }
                var queueMode = interrupt ? QueueMode.Flush : QueueMode.Add;
                _tts.Speak(text, queueMode, null, null);
            }
        }

        public void Stop()
        {
            if (_isTtsReady && _tts != null)
            {
                try { _tts.Stop(); } catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_tts != null)
                {
                    try
                    {
                        _tts.Stop();
                        _tts.Shutdown();
                    }
                    catch { }
                }
            }
            base.Dispose(disposing);
        }
    }
}
