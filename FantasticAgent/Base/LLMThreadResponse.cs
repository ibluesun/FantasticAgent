using FantasticAgent.Ollama;
using FantasticAgent.Tools;
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
namespace FantasticAgent.Base
{




    public class LLMThreadResponse<TM> where TM : LLMTurnMessage
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }


        [JsonPropertyName("done")]
        public bool Done { get; set; }


        [JsonIgnore]
        public virtual string MessageThinking { get; }

        [JsonIgnore]
        public virtual string MessageContent { get; }
    }





}