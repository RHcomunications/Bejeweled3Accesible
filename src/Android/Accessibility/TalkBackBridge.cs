using System;
using Android.Content;
using Android.Views;
using Android.Views.Accessibility;

namespace Bejeweled3Accessible.AndroidApp.Accessibility
{
    // TalkBackBridge: canaliza el 100% de la accesibilidad directamente a través
    // del framework nativo de accesibilidad de Android (AccessibilityManager / AccessibilityEvent).
    // No utiliza TextToSpeech interno para evitar solapamientos, voces duplicadas o bloqueos.
    public class TalkBackBridge : Java.Lang.Object
    {
        private readonly Context _context;
        private readonly AccessibilityManager _accessibilityManager;
        private View _attachedView;

        public TalkBackBridge(Context context)
        {
            _context = context;
            _accessibilityManager = (AccessibilityManager)context.GetSystemService(Context.AccessibilityService);
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
                       _accessibilityManager.IsEnabled;
            }
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (_attachedView != null)
            {
                try
                {
                    _attachedView.Post(() =>
                    {
                        try
                        {
                            if (_attachedView.IsShown)
                            {
                                _attachedView.AnnounceForAccessibility(text);
                            }
                            else
                            {
                                AccessibilityEvent evt = AccessibilityEvent.Obtain(EventTypes.Announcement);
                                evt.Text.Add(new Java.Lang.String(text));
                                evt.ClassName = _attachedView.Class.Name;
                                evt.PackageName = _context.PackageName;
                                evt.Enabled = true;
                                _accessibilityManager?.SendAccessibilityEvent(evt);
                            }
                        }
                        catch (Exception) { }
                    });
                }
                catch (Exception) { }
            }
            else if (_accessibilityManager != null && _accessibilityManager.IsEnabled)
            {
                try
                {
                    AccessibilityEvent evt = AccessibilityEvent.Obtain(EventTypes.Announcement);
                    evt.Text.Add(new Java.Lang.String(text));
                    evt.PackageName = _context.PackageName;
                    evt.Enabled = true;
                    _accessibilityManager.SendAccessibilityEvent(evt);
                }
                catch (Exception) { }
            }
        }

        public void Stop()
        {
            // Las interrupciones son gestionadas por el gestor de accesibilidad del sistema
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
