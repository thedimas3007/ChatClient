using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.ObjectModels.ResponseModels;

namespace ChatClient.Generation {
    internal class OpenAIException : Exception {
        public Error Error { get; }

        public OpenAIException(Error error) 
            : base($"OpenAI API response error: {error.Message}") {
            Error = error;
        }
    }
}
