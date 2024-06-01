using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABI.Windows.UI.WebUI;
using ChatClient.Repositories;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using HtmlAgilityPack;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;
using SharpToken;

namespace ChatClient.Generation {
    internal abstract class GenerationProvider {
        public static readonly IReadOnlyCollection<GenerationProvider> Providers = new List<GenerationProvider>() {
            new OpenAIProvider(),
        };

        public static readonly IReadOnlyCollection<Model> AllModels = Providers
            .SelectMany(p => p.Models)
            .ToList()
            .AsReadOnly();

        public readonly string Name;
        public readonly List<Model> Models;
        public readonly bool SupportsStreaming;
        public readonly bool SupportsFunctions;

        protected GenerationProvider(string name, List<Model> models, bool supportsStreaming, bool supportsFunctions) {
            Name = name;
            Models = models;
            SupportsStreaming = supportsStreaming;
            SupportsFunctions = supportsFunctions;
        }

        public IReadOnlyCollection<Model> GetModels(string name) {
            GenerationProvider provider = Providers.FirstOrDefault(p => p.Name == name);
            if (provider == null) {
                return new List<Model>();
            }

            return provider.Models;
        }

        public abstract Task<MessageResponse> GenerateResponseAsync(List<Message> messages, GenerationSettings settings, List<ToolDefinition> tools = null);
        public abstract IAsyncEnumerable<MessageResponse> GenerateResponseAsStreamAsync(List<Message> messages, GenerationSettings settings, List<ToolDefinition> tools = null);
    }
}
