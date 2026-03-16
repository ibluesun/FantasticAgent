using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasticAgent.Base
{


    public class ErrorEnvelope
    {
        [JsonPropertyName("error")]
        public LLMError? Error { get; init; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, this.GetType());
        }
    }


    public class LLMError
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = "";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";

        [JsonPropertyName("type")]
        public string Type { get; init; } = "";

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, this.GetType());
        }
    }
}
