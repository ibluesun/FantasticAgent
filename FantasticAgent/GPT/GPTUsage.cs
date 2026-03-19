using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.GPT
{



    //"usage":{"input_tokens":134,"input_tokens_details":{"cached_tokens":0},"output_tokens":221,"output_tokens_details":{ "reasoning_tokens":128},"total_tokens":355}

    public record GPTInputTokenDetails(int cached_tokens);
    public record GPTOutputTokenDetails(int reasoning_tokens);

    public class GPTUsage
    {

        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("input_tokens_details")]
        public GPTInputTokenDetails? InputTokenDetails { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OuputTokens { get; set; }

        [JsonPropertyName("output_tokens_details")]
        public GPTOutputTokenDetails? OutputTokenDetails{ get; set; }


    }
}
