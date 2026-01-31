using FantasticAgent.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;


namespace FantasticAgent.Base
{


    public class LLMTurnMessage
    {

        [JsonPropertyName("role")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; } = null;


        [JsonIgnore]
        public virtual string? MessageTextContent { get; }


        [JsonIgnore]
        public virtual string? MessageReasoningOrThinking { get; }
    }



}