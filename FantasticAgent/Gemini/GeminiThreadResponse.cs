using FantasticAgent.Base;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiThreadResponse : LLMThreadResponse<GeminiTurnMessage>
    {

        [JsonPropertyName("candidates")]
        public List<GeminiTurnMessageCandidate>? Candidates { get; set; }




        [JsonPropertyName("usageMetadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiUsage? Usage { get; set; } = null;



        [JsonPropertyName("modelVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ModelVersion { get; set; } = null;

        [JsonPropertyName("responseId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResponseId { get; set; } = null;



        [JsonPropertyName("finishReason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FinishReason { get; set; } = null;


    }
}
