using Jviz.Helpers;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ConversationalUI;
using DotNetEnv;
using System.Net.Http;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Jviz
{

    public class Wake
    {
        private readonly HttpClient _httpClient = new HttpClient();
        public OpenAIService OpenAIService { get; set; }
        public string AzureSpeechKey { get; set; }
        public string AzureRegion { get; set; }
        public KeywordRecognitionModel keywordModel { get; set; }
        public AudioConfig AudioConfig { get; set; }
        public SpeechConfig Config;
        public SpeechRecognizer Recognizer;
        public Chat _chatControl;
        public bool isSpeaking = false;
        public string WakeKeyword { get; set; } = "Jarvis"; // Replace with your actual wake word

        public Wake(Chat chatControl, OpenAIService openAIService)
        {
            OpenAIService = openAIService;
            // Load environment variables
            DotNetEnv.Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            AzureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");
            AzureRegion = Environment.GetEnvironmentVariable("SpeechRegion");
            // Initialize properties
            keywordModel = KeywordRecognitionModel.FromFile(Environment.GetEnvironmentVariable("KeywordModel"));
            _chatControl = chatControl;
            Config = SpeechConfig.FromSubscription(Environment.GetEnvironmentVariable("SpeechKey"), Environment.GetEnvironmentVariable("SpeechRegion"));
            AudioConfig = AudioConfig.FromDefaultMicrophoneInput();
            Recognizer = new SpeechRecognizer(Config, AudioConfig);

            // Start continuous recognition
            Recognizer.StartContinuousRecognitionAsync();
            chatControl.SendMessage("Listening for wake word...");
            // Event when a keyword is recognized
            Recognizer.Recognized += async (s, e) =>
            {
                Debug.WriteLine($"Recognizer event triggered with reason: {e.Result.Reason}");

                if (isSpeaking) return; // Do not process any speech when the system is speaking

                if (e.Result.Reason == ResultReason.RecognizedKeyword)
                {
                    Debug.WriteLine("Wake word detected. Stopping continuous recognition.");

                    // Stop continuous recognition
                    await Recognizer.StopContinuousRecognitionAsync();

                    // Start single-shot recognition to capture the subsequent user message
                    ListenAndConvertToText();
                }
            };

        }
        public void StartListening()
        {
            Recognizer.StartContinuousRecognitionAsync();
        }

        public void StopListening()
        {
            Recognizer.StopContinuousRecognitionAsync();
        }
        public async Task PlayTextAsSpeech(string text)
        {
            isSpeaking = true; // Set the flag
            StopListening(); // Stop the recognizer
            var config = SpeechConfig.FromSubscription(AzureSpeechKey, AzureRegion);
            config.SpeechSynthesisVoiceName = "en-US-ChristopherNeural";
            using (var synthesizer = new SpeechSynthesizer(config))
            {
                try
                {
                    var result = await synthesizer.SpeakTextAsync(text);
                    if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        _chatControl.SendMessage($"Speech Error: {cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            _chatControl.SendMessage($"Error Details: {cancellation.ErrorDetails}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _chatControl.SendMessage($"Speech Error: {ex.Message}");
                }
            }

            isSpeaking = false; // Reset the flag
            StartListening(); // Resume the recognizer
        }
        public async void ListenAndConvertToText()
        {
            Debug.WriteLine("Starting single-shot recognition.");

            try
            {
                var result = await Recognizer.RecognizeOnceAsync();
                Debug.WriteLine($"Single-shot recognition result: {result.Text}");

                var userMessage = result.Text.Trim();

                // Check if the recognized text contains the keyword and remove it
                if (userMessage.Contains(WakeKeyword))
                {
                    userMessage = userMessage.Replace(WakeKeyword, "").Trim();
                }

                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    _chatControl.ReceiveMessage(userMessage);
                }

                Debug.WriteLine("Restarting continuous recognition for wake word detection.");
                await Recognizer.StartContinuousRecognitionAsync();
            }
            catch (Exception ex)
            {
                _chatControl.SendMessage($"Error: {ex.Message}");
            }
        }
    }
        public class OpenAIChatResponse
        {
            public List<ChatMessage> Messages { get; set; }
        }

        public class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

}





