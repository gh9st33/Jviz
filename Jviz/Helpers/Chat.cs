using System;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ConversationalUI;

namespace Jviz.Helpers
{
    public class Chat
    {
        private RadChat _chatControl;
        public OpenAIService OpenAIService { get; set; }
        public Wake WakeService { get; set; }
        public Chat(RadChat chatControl)
        {
            _chatControl = chatControl;
            OpenAIService = new OpenAIService();
            WakeService = new Wake(this, OpenAIService);
        }

        public void SendMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatMessage = new TextMessage(new Author("Assistant"), message);
                _chatControl.AddMessage(chatMessage);
            });
        }
        // New method to receive messages from the user
        public async void ReceiveMessage(string message)
        {
            var chatMessage = new TextMessage(new Author("User"), message);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chatControl.AddMessage(chatMessage);
            });

            OpenAIService.AddUserMessage(message); // Add the latest user message

            var response = await OpenAIService.GetChatResponse(message);

            // Send the response as a message from the assistant
            SendMessage(response); 

            // Stop listening
            WakeService.StopListening();

            // Play the response using Azure TTS
            await WakeService.PlayTextAsSpeech(response);

            // Introduce a delay (e.g., 2 seconds) before starting the recognizer again
            await Task.Delay(2000);

            // Resume listening
            WakeService.StartListening();
        }


    }
}