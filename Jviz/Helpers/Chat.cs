using MediaFoundation.Misc;
using MediaFoundation;
using System;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Charting;
using Telerik.Windows.Controls.ConversationalUI;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telerik.Windows.Controls.DataVisualization.Map.BingRest;
using Telerik.Windows.Documents.Model.Themes;
using Telerik.Windows.Shapes;

namespace Jviz.Helpers
{
    public class Chat
    {
        private RadChat _chatControl;
        public OpenAIService OpenAIService { get; set; }
        public SpeechToText SpeechToTextService { get; set; }
        public TextToSpeech TextToSpeechService { get; set; }
        public Wake WakeService { get; set; }
        public enum WakeState
        {
            ListeningForWakeWord,
            ProcessingSpeechToText,
            ProcessingTextToSpeech,
            Idle
        }

        public WakeState CurrentState { get; private set; } = WakeState.Idle;
        public Chat(RadChat chatControl)
        {
            _chatControl = chatControl;
            OpenAIService = new OpenAIService();
            //will change this later
            SpeechToTextService = new SpeechToText("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            TextToSpeechService = new TextToSpeech("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            WakeService = new Wake(this);
            // Subscribe to events
            SpeechToTextService.Recognized += SpeechToTextService_Recognized;
            TextToSpeechService.StartedSpeaking += TextToSpeechService_StartedSpeaking;
            TextToSpeechService.FinishedSpeaking += TextToSpeechService_FinishedSpeaking;

            //InitializeAsync().GetAwaiter();
        }

        //public async Task InitializeAsync()
        //{

        //}
        private async void HandleWakeStateChange(WakeState newState)
        {
            CurrentState = newState;
            switch (newState)
            {
                case WakeState.ProcessingSpeechToText:
                    await WakeService.StopWakeWordDetection();
                    break;
                case WakeState.ProcessingTextToSpeech:
                    await WakeService.StopWakeWordDetection();
                    break;
                case WakeState.ListeningForWakeWord:
                    break;
                case WakeState.Idle:
                    await WakeService.StartWakeWordDetection();
                    CurrentState = WakeState.ListeningForWakeWord;
                    break;
            }
        }
        public void SendMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatMessage = new TextMessage(new Author("Assistant"), message);
                _chatControl.AddMessage(chatMessage);
            });
        }

        public async Task ReceiveMessage(string message)
        {
            CurrentState = WakeState.ProcessingSpeechToText;
            // Add the user's message to the chat control
            var userMessage = new TextMessage(new Author("User"), message);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chatControl.AddMessage(userMessage);
            });

            OpenAIService.AddUserMessage(message); // Add the latest user message

            // Get the assistant's response
            var response = await OpenAIService.GetChatResponse(message);

            // Add the assistant's response to the chat control
            var assistantMessage = new TextMessage(new Author("Assistant"), response);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chatControl.AddMessage(assistantMessage);
            });

            // Play the response using Azure TTS
            try
            {
                await TextToSpeechService.PlayTextAsSpeechAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                // Handle the exception here. For example, log it or show a user-friendly message.
                Console.WriteLine(ex.Message);
            }
        }


        // When speech is recognized, process the message
        private async void SpeechToTextService_Recognized(object sender, EventArgs e)
        {
            if (e is RecognitionEventArgs speechArgs && !string.IsNullOrWhiteSpace(speechArgs.Text))
            {
                await ReceiveMessage(speechArgs.Text);
            }
        }


        private async void TextToSpeechService_StartedSpeaking(object sender, EventArgs e)
        {
            CurrentState = WakeState.ProcessingTextToSpeech;
            HandleWakeStateChange(CurrentState);
            // Handle UI updates or other actions when the system starts speaking.
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Set the typing indicator to show "Jarvis is thinking..."
                _chatControl.TypingIndicatorText = "Jarvis is thinking...";
                _chatControl.TypingIndicatorVisibility = Visibility.Visible; // This will make the typing indicator visible
            });
            // Pause or stop the SpeechToText service
            await SpeechToTextService.StopRecognitionAsync();
        }
        private async void TextToSpeechService_FinishedSpeaking(object sender, EventArgs e)
        {
            CurrentState = WakeState.Idle;
            // ... other code ...

            // Wait for a short duration before restarting the SpeechToText service
            await Task.Delay(2000); // 2 seconds delay

            // Check if TTS has completely finished (assuming TextToSpeechService has an IsSpeaking property)
            if (!TextToSpeechService.IsSpeaking)
            {
                try
                {
                    // Resume or restart the SpeechToText service
                    await SpeechToTextService.StartRecognitionAsync();
                }
                catch (Exception ex)
                {
                    // Handle the exception here. For example, log it or show a user-friendly message.
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }
}