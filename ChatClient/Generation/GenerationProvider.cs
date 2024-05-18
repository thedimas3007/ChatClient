using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatClient.Repositories;

namespace ChatClient.Generation {
    internal abstract class GenerationProvider {
        public readonly string Name;
        public readonly List<Model> Models;

        public abstract Task<MessageResponse> GenerateResponseAsync(List<Message> messages, GenerationSettings settings);
        public abstract IAsyncEnumerable<MessageResponse> GenerateResponseAsStreamAsync(List<Message> messages, GenerationSettings settings);
    }
}
