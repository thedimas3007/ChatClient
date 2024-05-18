using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Generation {
    internal class GenerationSettings {
        public string Token { get; set; }
        public float Temperature { get; set; }
        public float TopP { get; set; }
        public float FrequencyPenalty { get; set; }
        public float PresencePenalty { get; set; }
        public Model Model { get; set; }

        public GenerationSettings() { }

        public GenerationSettings(string token, float temperature, float topP, float frequencyPenalty,
            float presencePenalty, Model model) {
            Token = token;
            Temperature = temperature;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            Model = model;
        }
    }
}
