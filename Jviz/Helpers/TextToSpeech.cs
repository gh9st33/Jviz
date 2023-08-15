using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
using DotNetEnv;

namespace Jviz.Helpers
{
    public class TextToSpeech
    {
        public enum TTSState
        {
            Idle,
            Speaking,
            Error
        }

        public TTSState State { get; private set; } = TTSState.Idle;

        public event EventHandler StartedSpeaking;
        public event EventHandler FinishedSpeaking;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        private readonly string _azureSpeechKey;
        private readonly string _azureRegion;
        private readonly SpeechConfig _config;

        public TextToSpeech(string envPath)
        {
            Env.Load(envPath);
            _azureRegion = Environment.GetEnvironmentVariable("SpeechRegion");
            _azureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");

            _config = SpeechConfig.FromSubscription(_azureSpeechKey, _azureRegion);
            _config.SpeechSynthesisVoiceName = "en-US-ChristopherNeural"; // You can change this to your preferred voice
        }

        public async Task PlayTextAsSpeechAsync(string text)
        {
            if (State == TTSState.Speaking)
            {
                throw new InvalidOperationException("Text-to-Speech is currently speaking. Please wait.");
            }

            using (var synthesizer = new SpeechSynthesizer(_config))
            {
                try
                {
                    State = TTSState.Speaking;
                    StartedSpeaking?.Invoke(this, EventArgs.Empty);

                    var result = await synthesizer.SpeakTextAsync(text);

                    if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        State = TTSState.Error;
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(cancellation.Reason.ToString(), cancellation.ErrorDetails));
                    }
                    else
                    {
                        State = TTSState.Idle;
                        FinishedSpeaking?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    State = TTSState.Error;
                    ErrorOccurred?.Invoke(this, new ErrorEventArgs("Speech Error", ex.Message));
                }
            }
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }

        public ErrorEventArgs(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
