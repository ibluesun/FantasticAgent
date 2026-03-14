using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiPart
    {

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; } = null;


        [JsonPropertyName("thought")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Thought { get; set; } = null;


        [JsonPropertyName("thoughtSignature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ThoughtSignature { get; set; } = null;






    }
}
