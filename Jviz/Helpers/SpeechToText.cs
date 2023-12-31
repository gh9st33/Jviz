﻿using DotNetEnv;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Threading.Tasks;

namespace Jviz.Helpers
{
    public class SpeechToText : IDisposable
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
        private bool isRecognitionRunning = false;  // Add this at the class level
        public SpeechToText(string envPath)
        {
            Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SpeechRegion")) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SpeechKey")))
            {
                Env.Load(envPath);
            }
            _azureRegion = Environment.GetEnvironmentVariable("SpeechRegion");
            _azureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");
            _config = SpeechConfig.FromSubscription(_azureSpeechKey, _azureRegion);
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _recognizer = new SpeechRecognizer(_config, audioConfig);

            _recognizer.Recognized += Recognizer_Recognized;
            _recognizer.Canceled += Recognizer_Canceled;
        }

        private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                OnRecognized(new RecognitionEventArgs(e.Result.Text));
            }
            State = STTState.Idle;
            OnFinishedListening();
        }

        private void Recognizer_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            State = STTState.Error;
            OnErrorOccurred(new ErrorEventArgs("Recognition Error", e.Reason.ToString()));
        }

        public async Task StartRecognitionAsync()
        {
            if (!isRecognitionRunning)
            {
                try
                {
                    await _recognizer.StartContinuousRecognitionAsync();
                    isRecognitionRunning = true;
                }
                catch (ApplicationException ex)
                {
                    // Handle the exception here. For example, log it or show a user-friendly message.
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public async Task StopRecognitionAsync()
        {
            if (State == STTState.Listening)
            {
                State = STTState.Recognizing;
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }

        protected virtual void OnStartedListening() => StartedListening?.Invoke(this, EventArgs.Empty);
        protected virtual void OnFinishedListening() => FinishedListening?.Invoke(this, EventArgs.Empty);
        protected virtual void OnRecognized(RecognitionEventArgs e) => Recognized?.Invoke(this, e);
        protected virtual void OnErrorOccurred(ErrorEventArgs e) => ErrorOccurred?.Invoke(this, e);

        public void Dispose()
        {
            _recognizer?.Dispose();
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
