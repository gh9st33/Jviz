using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
using DotNetEnv;
using Jviz.Helpers;
using static Jviz.Helpers.Chat;

namespace Jviz
{
    public class Wake
    {
        public KeywordRecognitionModel KeywordModel { get; set; }
        public SpeechConfig Config { get; set; }
        public SpeechRecognizer Recognizer { get; set; }
        public string WakeKeyword { get; set; } = "Jarvis"; // Replace with your actual wake word
        public event Action WakeWordDetected;
        public event Action ProcessingDone;
        public Action<WakeState> OnStateChange { get; set; }

        public Chat ChatControl { get; set; }
        private readonly SpeechToText _speechToText;

        public Wake(Chat chatControl, OpenAIService openAIService)
        {
            // Load environment variables
            Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\.env");
            var azureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");
            var azureRegion = Environment.GetEnvironmentVariable("SpeechRegion");
            var keywordModelPath = Environment.GetEnvironmentVariable("KeywordModel");

            if (string.IsNullOrEmpty(azureSpeechKey) || string.IsNullOrEmpty(azureRegion) || string.IsNullOrEmpty(keywordModelPath))
            {
                throw new ArgumentException("Required environment variables are missing.");
            }

            // Initialize properties
            Config = SpeechConfig.FromSubscription(azureSpeechKey, azureRegion);
            KeywordModel = KeywordRecognitionModel.FromFile(keywordModelPath);
            Recognizer = new SpeechRecognizer(Config);

            _speechToText = new SpeechToText("C:\\Users\\wareb\\source\\repos\\Jviz\\.env");
            _speechToText.Recognized += OnSpeechRecognized;
        }

        private async void OnSpeechRecognized(object sender, Helpers.RecognitionEventArgs e)
        {
            OnStateChange?.Invoke(WakeState.ProcessingSpeechToText);
            // Handle the recognized speech here or pass it to another service
            Console.WriteLine($"Recognized: {e.Text}");
            await ChatControl.ReceiveMessage(e.Text);
        }

        public async Task StartWakeWordDetection()
        {
            OnStateChange?.Invoke(WakeState.ListeningForWakeWord);
            if (Recognizer != null)
            {
                await Recognizer.StopContinuousRecognitionAsync();
                Recognizer.Recognized -= Recognizer_Recognized;
                Recognizer.Dispose();
            }
            Recognizer.Recognized += Recognizer_Recognized;
            await Recognizer.StartContinuousRecognitionAsync();
        }

        private async void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Text.Contains("Jarvis"))
            {
                OnStateChange?.Invoke(WakeState.ProcessingSpeechToText);
                // Wake word detected
                Console.WriteLine($"Wake word '{WakeKeyword}' detected!");
                OnWakeWordDetected();
                await ListenAndConvertToText();
            }
        }

        public async Task ListenAndConvertToText()
        {
            try
            {
                await _speechToText.StartRecognitionAsync();
            }
            catch (Exception ex)
            {
                ChatControl.SendMessage($"Error: {ex.Message}");
            }
        }

        private void OnWakeWordDetected()
        {
            WakeWordDetected?.Invoke();
        }

        private void OnProcessingDone()
        {
            ProcessingDone?.Invoke();
        }

        public async Task StopWakeWordDetection()
        {
            Recognizer.Recognized -= Recognizer_Recognized;
            await Recognizer.StopContinuousRecognitionAsync();
        }
    }
}
