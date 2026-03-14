using FantasticAgent.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{

    public class GeminiThinkingLevelConverter : JsonConverter<ReasoningEffortLevel>
    {
        public override ReasoningEffortLevel Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, ReasoningEffortLevel value, JsonSerializerOptions options)
        {
            var geminiValue = value switch
            {
                ReasoningEffortLevel.None => "THINKING_LEVEL_UNSPECIFIED",
                ReasoningEffortLevel.Low => "MINIMAL",
                ReasoningEffortLevel.Medium => "LOW",
                ReasoningEffortLevel.High => "MEDIUM",
                ReasoningEffortLevel.Max => "HIGH",
                _ => "THINKING_LEVEL_UNSPECIFIED"
            };
            writer.WriteStringValue(geminiValue);
        }
    }

    public class GeminiThinkingConfiguration
    {

        [JsonPropertyName("includeThoughts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IncludeThoughts { get; set; } = false;

        [JsonPropertyName("thinkingBudget")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ThinkingBudget { get; set; } = 1024;


        ReasoningEffortLevel? _ThinkingLevel = null;

        [JsonPropertyName("thinkingLevel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(GeminiThinkingLevelConverter))]
        public ReasoningEffortLevel? ThinkingLevel 
        { 
            get => _ThinkingLevel;
            set
            {
                
                _ThinkingLevel = value;
                if (value != null) ThinkingBudget = null;
            }
        }
    }

    public class GeminiGenerationConfiguration
    {

        [JsonPropertyName("thinkingConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiThinkingConfiguration? ThinkingConfig { get; set; } = null;

    }
}
