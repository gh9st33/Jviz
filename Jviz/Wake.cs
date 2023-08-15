using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
using DotNetEnv;
using Jviz.Helpers;

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
        public Chat ChatControl { get; set; }
        private readonly SpeechToText _speechToText;
        public Wake(Chat chatControl, OpenAIService openAIService)
        {
            // Load environment variables
            Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
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

            _speechToText = new SpeechToText();
            _speechToText.Recognized += OnSpeechRecognized;
        }
        private void OnSpeechRecognized(object sender, Helpers.RecognitionEventArgs e)
        {
            // Handle the recognized speech here or pass it to another service
            Console.WriteLine($"Recognized: {e.Text}");
        }
        public async Task StartWakeWordDetection()
        {
            Recognizer.Recognized += Recognizer_Recognized;
            await Recognizer.StartContinuousRecognitionAsync();
        }

        private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                // Wake word detected
                Console.WriteLine($"Wake word '{WakeKeyword}' detected!");
                // You can trigger other actions here, like starting the Listener
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
        public void OnWakeWordDetected()
        {
            WakeWordDetected?.Invoke();
        }

        public void OnProcessingDone()
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
