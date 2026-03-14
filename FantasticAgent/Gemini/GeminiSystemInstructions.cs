using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiSystemInstructions
    {

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();

    }
}
