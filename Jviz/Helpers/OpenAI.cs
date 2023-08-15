using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;
using Telerik.Windows.Data;

namespace Jviz.Helpers
{
    public class OpenAIService
    {
        public OpenAIClient OpenAIClient { get; set; }
        public List<Message> Messages { get; set; }

        public OpenAIService()
        {
            Env.Load("C:\\Users\\wareb\\source\\repos\\Jviz\\Jviz\\.env");
            var apiKey = Environment.GetEnvironmentVariable("OpenAiApiKey");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("OpenAI API key is missing.");
            }

            OpenAIClient = new OpenAIClient(apiKey);
            Messages = new List<Message>(); // Initialize the list
            Messages.Add(new Message(Role.System, "You are a funny, satirical, and opinionated assistant"));
        }

        public void AddUserMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Messages.Add(new Message(Role.User, message));
            }
        }

        public void AddAssistantMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Messages.Add(new Message(Role.Assistant, message));
            }
        }
        public async Task<string> GetChatResponse(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return "Sorry, I didn't catch that. Please try again.";
            }

            try
            {
                // Add the latest user message to the Messages list
                AddUserMessage(userMessage);

                // Create a new list containing only the latest user message
                var latestMessages = new List<Message>
                {
                    new Message(Role.User, userMessage)
                };

                var chatRequest = new ChatRequest(latestMessages, "gpt-3.5-turbo-16k");
                var result = await OpenAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);

                // Add the assistant's response to the Messages list
                AddAssistantMessage(result.FirstChoice.Message.Content.ToString());

                return result.FirstChoice.Message.Content.ToString();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, return a default message, etc.)
                return $"Error: {ex.Message}";
            }
        }


    }
}