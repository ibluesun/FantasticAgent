using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using FantasticAgent.Claude.Tools;
using FantasticAgent.GPT.Tools;
using FantasticAgent.Ollama.Tools;

namespace FantasticAgent.Tools
{



    [JsonDerivedType(typeof(ClaudeFunctionToolDefinition))]
    [JsonDerivedType(typeof(GPTFunctionToolDefinition))]
    [JsonDerivedType(typeof(OllamaFunctionToolDefinition))]
    public class ToolDefinition
    {
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public virtual ToolType ToolType { get; set; }

    }



    


}
