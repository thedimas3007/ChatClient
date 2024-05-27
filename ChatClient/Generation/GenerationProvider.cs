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
        public readonly bool SupportStreaming;

        protected GenerationProvider(string name, List<Model> models, bool supportStreaming) {
            Name = name;
            Models = models;
            SupportStreaming = supportStreaming;
        }

        public IReadOnlyCollection<Model> GetModels(string name) {
            GenerationProvider provider = Providers.FirstOrDefault(p => p.Name == name);
            if (provider == null) {
                return new List<Model>();
            }

            return provider.Models;
        }

        public abstract Task<MessageResponse> GenerateResponseAsync(List<Message> messages, GenerationSettings settings, bool withTools = false);
        public abstract IAsyncEnumerable<MessageResponse> GenerateResponseAsStreamAsync(List<Message> messages, GenerationSettings settings, bool withTools = false);
    }
}
