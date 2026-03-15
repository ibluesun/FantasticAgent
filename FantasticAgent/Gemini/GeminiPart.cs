using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{

    public class GeminiPartFunctionCall
    {

        [JsonPropertyName("if")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }


        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }


        [JsonPropertyName("args")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement Arguments { get; set; }


    }

    public class GeminiPartFunctionResponse
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        [JsonPropertyName("response")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Response { get; set; }

    }

    public class GeminiPart
    {

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; } = null;


        [JsonPropertyName("thought")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Thought { get; set; } = null;


        [JsonPropertyName("thoughtSignature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ThoughtSignature { get; set; } = null;



        [JsonPropertyName("functionCall")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiPartFunctionCall? FunctionCall { get; set; } = null;

        [JsonPropertyName("functionResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiPartFunctionResponse? FunctionResponse { get; set; } = null;



    }
}
