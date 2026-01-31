using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Ollama
{

    public class OllamaModelInfo
    {

        [JsonPropertyName("general.architecture")]
        public string? GeneralArchitecture { get; set; }


        [JsonPropertyName("general.basename")]
        public string? GeneralBaseName { get; set; }
    }


    public class OllamaModel
    {

        [JsonPropertyName("license")]
        public string? License { get; set; }


        [JsonPropertyName("model_info")]
        OllamaModelInfo? ModelInfo { get; set; }


        [JsonPropertyName("capabilities")]
        public string[]? Capabilities { get; set; }



    }
}
