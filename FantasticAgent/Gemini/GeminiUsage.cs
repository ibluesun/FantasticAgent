using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiUsage
    {

        /*
         
        "usageMetadata": {
            "promptTokenCount": 3,
            "candidatesTokenCount": 10,
            "totalTokenCount": 102,
            "promptTokensDetails": [
              {
                "modality": "TEXT",
                "tokenCount": 3
              }
            ],
            "thoughtsTokenCount": 89
          }
         
         */


        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        [JsonPropertyName("thoughtsTokenCount")]
        public int ThoughtsTokenCount { get; set; }


    }
}
