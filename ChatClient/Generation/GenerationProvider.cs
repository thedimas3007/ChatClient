using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatClient.Repositories;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;

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

        public async Task<string> GoogleAsync(string query, string searchId, string searchToken) {
            var searchAPI = new CustomSearchAPIService(new BaseClientService.Initializer { ApiKey = searchToken });
            var request = searchAPI.Cse.List();
            request.Cx = searchId;
            request.Q = query;
            var search = await request.ExecuteAsync();
            return string.Join('\n', search.Items.Select(r => $"{r.Title}: {r.Link}"));
        }

        public abstract Task<MessageResponse> GenerateResponseAsync(List<Message> messages, GenerationSettings settings, bool withTools = false);
        public abstract IAsyncEnumerable<MessageResponse> GenerateResponseAsStreamAsync(List<Message> messages, GenerationSettings settings, bool withTools = false);
    }
}
