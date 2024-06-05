using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Generation {
    internal class MessageResponse {
        public string Role { get; set; }
        public string? Content { get; set; }
        public string? Name { get; set; }
        public string? ToolCallId { get; set; }
        public IList<ToolCall>? ToolCalls { get; set; } // TODO: Make universal for different providers
        public int TokensIn { get; set; }
        public int TokensOut { get; set; }
        
        public MessageResponse(string role, string content, string name, string toolCallId, IList<ToolCall> toolCalls, int tokensIn, int tokensOut) {
            Role = role;
            Content = content;
            Name = name;
            ToolCallId = toolCallId;
            ToolCalls = toolCalls;
            TokensIn = tokensIn;
            TokensOut = tokensOut;
        }
    }
}
