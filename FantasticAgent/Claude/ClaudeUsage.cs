using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Claude
{

    /*
        {
            "input_tokens":713,
            "cache_creation_input_tokens":0,
            "cache_read_input_tokens":0,
            "cache_creation":
                {
                    "ephemeral_5m_input_tokens":0,
                    "ephemeral_1h_input_tokens":0
                },
            "output_tokens":147,
            "service_tier":"standard"
        } 
     */




    public class ClaudeUsage
    {

        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OuputTokens { get; set; }


        [JsonPropertyName("service_tier")]
        public string? ServiceTier { get; set; }
    }
}
