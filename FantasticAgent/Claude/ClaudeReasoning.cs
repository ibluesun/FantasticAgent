using FantasticAgent.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Claude
{




    public class ClaudeReasoning
    {
        /// <summary>
        /// enabled 
        /// or 
        /// disabled
        /// </summary>
        [JsonPropertyName("type")]
        public ReasoningType ThinkingType { get; set; } = ReasoningType.Enabled;


        //[JsonPropertyName("enabled")]
        //public bool Enabled { get; set; } = true;

        [JsonPropertyName("budget_tokens")]
        public int? BudgetTokens { get; set; } = 1024;
    }
}
