using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ChatClient.Repositories;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace ChatClient.Generation {
    internal class OpenAIProvider : GenerationProvider {
        public static readonly Model Gpt35Turbo = new("OpenAI", "gpt-3.5-turbo", "GPT-3.5 Turbo");
        public static readonly Model Gpt4Turbo = new("OpenAI", "gpt-4-turbo", "GPT-4 Turbo");
        public static readonly Model Gpt4o = new("OpenAI", "gpt-4o", "GPT-4o");

        public OpenAIProvider() : 
            base("OpenAI", new List<Model>() { Gpt35Turbo, Gpt4Turbo, Gpt4o }, true) { }

        private OpenAIService GetApi(string key) {
            return new OpenAIService(new OpenAiOptions() {
                ApiKey = key ?? "not-set"
            });
        }

        public override async Task<MessageResponse> GenerateResponseAsync(List<Message> messages, GenerationSettings settings) {
            if (!Models.Any(m => m.Id == settings.Model.Id)) {
                throw new ArgumentException($"Model {settings.Model.Id} is either not found or not available.");
            }

            var api = GetApi(settings.Token);
            var response = await api.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest() {
                Messages = messages.ConvertAll(m => m.AsChatMessage()),
                Model = settings.Model.Id,
                Temperature = settings.Temperature,
                TopP = settings.TopP,
                FrequencyPenalty = settings.FrequencyPenalty,
                PresencePenalty = settings.PresencePenalty
            });
            var message = response.Choices.First().Message;
            return new MessageResponse(message.Role, message.Content, message.Name, message.ToolCallId,
                message.ToolCalls);
        }

        public override async IAsyncEnumerable<MessageResponse> GenerateResponseAsStreamAsync(List<Message> messages,
            GenerationSettings settings) {
            if (!Models.Any(m => m.Id == settings.Model.Id)) {
                throw new ArgumentException($"Model {settings.Model.Id} is either not found or not available.");
            }

            var api = GetApi(settings.Token);
            var call = api.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest() {
                Messages = messages.ConvertAll(m => m.AsChatMessage()),
                Model = settings.Model.Id,
                Temperature = settings.Temperature,
                TopP = settings.TopP,
                FrequencyPenalty = settings.FrequencyPenalty,
                PresencePenalty = settings.PresencePenalty
            });
            await foreach (var response in call) {
                if (!response.Successful) {
                    throw new OpenAIException(response.Error);
                }

                var message = response.Choices.First().Message;
                yield return new MessageResponse(message.Role, message.Content, message.Name, message.ToolCallId,
                    message.ToolCalls);
            }
        }
    }
}
