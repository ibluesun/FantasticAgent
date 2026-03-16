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
        public ApiError? Error { get; init; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, this.GetType());
        }
    }


    public class ApiError
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = "";

        [JsonPropertyName("type")]
        public string Type { get; init; } = "";

        [JsonPropertyName("code")]
        public string Code { get; init; } = "";

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, this.GetType());
        }
    }
}
