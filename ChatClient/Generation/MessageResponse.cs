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
        
        public MessageResponse(string role, string content, string name, string toolCallId, IList<ToolCall> toolCalls) {
            Role = role;
            Content = content;
            Name = name;
            ToolCallId = toolCallId;
            ToolCalls = toolCalls;
        }
    }
}
