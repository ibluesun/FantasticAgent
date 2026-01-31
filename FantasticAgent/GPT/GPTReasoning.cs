using FantasticAgent.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.GPT
{




    public class GPTReasoning
    {
        [JsonPropertyName("effort")]
        public ReasoningEffortLevel? Effort { get; set; } = ReasoningEffortLevel.Medium;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("summary")]
        public ReasoningSummary? Summary { get; set; } = null;
    }
}
