using System;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ConversationalUI;
using Jviz; // Assuming the Listener and SpeechToText classes are in the Jviz namespace

namespace Jviz.Helpers
{
    public class Chat
    {
        private RadChat _chatControl;
        public OpenAIService OpenAIService { get; set; }
        public SpeechToText SpeechToTextService { get; set; }

        public Chat(RadChat chatControl)
        {
            _chatControl = chatControl;
            OpenAIService = new OpenAIService();

            string azureSpeechKey = Environment.GetEnvironmentVariable("SpeechKey");
            string azureRegion = Environment.GetEnvironmentVariable("SpeechRegion");

            SpeechToTextService = new SpeechToText();

           SpeechToTextService.Recognized += async (speech) =>
            {
                var text = await SpeechToTextService.ConvertSpeechToTextAsync();
                ReceiveMessage(text);
            };

            InitializeAsync().GetAwaiter();
        }

        public async Task InitializeAsync()
        {
            await ListenerService.StartListeningAsync();
        }

        public void SendMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatMessage = new TextMessage(new Author("Assistant"), message);
                _chatControl.AddMessage(chatMessage);
            });
        }

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

        }
    }
}
