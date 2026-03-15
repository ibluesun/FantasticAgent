using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{

    public class GeminiTurnMessageCandidateContent
    {
        [JsonPropertyName("parts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GeminiPart>? Parts { get; set; } = null;


        [JsonPropertyName("role")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; } = null;
    }


    public class GeminiTurnMessageCandidate
    {

        [JsonPropertyName("index")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Index { get; set; } = null;

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiTurnMessageCandidateContent? Content { get; set; } = null;

        [JsonPropertyName("finishReason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FinishReason { get; set; } = null;

        [JsonPropertyName("finishMessage")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FinishMessage { get; set; } = null;


    }
}
