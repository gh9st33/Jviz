using DotNetEnv;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Threading.Tasks;

namespace Jviz.Helpers
{
    public class SpeechToText
    {
        public enum STTState
        {
            Idle,
            Listening,
            Recognizing,
            Error
        }

        public STTState State { get; private set; } = STTState.Idle;

        public event EventHandler StartedListening;
        public event EventHandler FinishedListening;
        public event EventHandler<RecognitionEventArgs> Recognized;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        private readonly string _azureSpeechKey;
        private readonly string _azureRegion;
        private readonly SpeechConfig _config;
        private SpeechRecognizer _recognizer;

        public SpeechToText()
        {
            Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            _azureRegion = Environment.GetEnvironmentVariable("SpeechRegion");
            _azureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");
            _config = SpeechConfig.FromSubscription(_azureSpeechKey, _azureRegion);
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _recognizer = new SpeechRecognizer(_config, audioConfig);

            _recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Recognized?.Invoke(this, new RecognitionEventArgs(e.Result.Text));
                }
                State = STTState.Idle;
                FinishedListening?.Invoke(this, EventArgs.Empty);
            };

            _recognizer.Canceled += (s, e) =>
            {
                State = STTState.Error;
                ErrorOccurred?.Invoke(this, new ErrorEventArgs("Recognition Error", e.Reason.ToString()));
            };
        }

        public async Task StartRecognitionAsync()
        {
            if (State != STTState.Idle)
            {
                throw new InvalidOperationException("Speech-to-Text is currently active. Please wait.");
            }

            State = STTState.Listening;
            StartedListening?.Invoke(this, EventArgs.Empty);

            await _recognizer.StartContinuousRecognitionAsync();
        }

        public async Task StopRecognitionAsync()
        {
            if (State == STTState.Listening)
            {
                State = STTState.Recognizing;
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }
    }

    public class RecognitionEventArgs : EventArgs
    {
        public string Text { get; }

        public RecognitionEventArgs(string text)
        {
            Text = text;
        }
    }
}
