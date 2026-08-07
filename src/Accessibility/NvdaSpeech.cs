using System;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;

namespace Bejeweled3Accessible.Accessibility
{
    public class NvdaSpeech : IDisposable
    {
        [DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvdaController_speakText(string text);

        [DllImport("nvdaControllerClient32.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvdaController_cancelSpeech();

        [DllImport("nvdaControllerClient32.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvdaController_testIfRunning();

        private readonly SpeechSynthesizer _sapi;
        private readonly bool _nvdaAvailable;

        // Voice gate: never let two speech lines overlap or pile up.
        // - interrupt=true cancels whatever is being spoken and replaces it right away.
        // - interrupt=false (countdowns, mantras, breath) is DROPPED if something was
        //   spoken recently, instead of being queued behind it.
        private const int NVDA_CANCEL_SETTLE_MS = 40;
        private const int NON_INTERRUPT_DROP_MS = 1000;
        private readonly object _speechLock = new object();
        private DateTime _lastSpeakTime = DateTime.MinValue;

        public NvdaSpeech()
        {
            _nvdaAvailable = false;

            try
            {
                int test = nvdaController_testIfRunning();
                _nvdaAvailable = (test == 0);
            }
            catch (Exception)
            {
                _nvdaAvailable = false;
            }

            try
            {
                _sapi = new SpeechSynthesizer();
                _sapi.SetOutputToDefaultAudioDevice();
            }
            catch (Exception)
            {
                _sapi = null;
            }
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text)) return;

            bool nvdaCancelNeeded = false;

            lock (_speechLock)
            {
                bool recentlySpoke = (DateTime.UtcNow - _lastSpeakTime).TotalMilliseconds < NON_INTERRUPT_DROP_MS;

                // Non-interrupting speech never stacks over an active or recent voice
                if (!interrupt && recentlySpoke) return;

                // SAPI still talking: drop instead of stacking
                if (!interrupt && _sapi != null && _sapi.State == SynthesizerState.Speaking && !_nvdaAvailable)
                    return;

                if (_nvdaAvailable && interrupt && recentlySpoke)
                    nvdaCancelNeeded = true;
            }

            if (nvdaCancelNeeded)
            {
                try
                {
                    // NVDA cancel is asynchronous; block outside the lock so the UI
                    // thread is not stuck behind SpeechLock during the settle wait.
                    nvdaController_cancelSpeech();
                    System.Threading.Thread.Sleep(NVDA_CANCEL_SETTLE_MS);
                }
                catch (Exception) { }
            }

            lock (_speechLock)
            {
                if (_nvdaAvailable)
                {
                    try
                    {
                        int res = nvdaController_speakText(text);
                        if (res == 0)
                        {
                            _lastSpeakTime = DateTime.UtcNow;
                            return;
                        }
                    }
                    catch (Exception) { }
                }

                if (_sapi != null)
                {
                    try
                    {
                        if (interrupt) _sapi.SpeakAsyncCancelAll();
                        _sapi.SpeakAsync(text);
                        _lastSpeakTime = DateTime.UtcNow;
                    }
                    catch (Exception) { }
                }
            }
        }

        public void Dispose()
        {
            if (_sapi != null) _sapi.Dispose();
        }
    }
}
