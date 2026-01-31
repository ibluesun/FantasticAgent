using FantasticAgent.Base;
using FantasticAgent.GPT;
using FantasticAgent.Ollama;
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
namespace FantasticAgent.GPT
{

    public class GPTThreadResponse : LLMThreadResponse<GPTTurnMessage>
    {


        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? ResponseObjectType { get; set; }


        [JsonPropertyName("created_at")]
        public int CreatedAt { get; set; }

        [JsonPropertyName("reasoning")]
        public GPTReasoning? Reasoning { get; set; }


        [JsonPropertyName("output")]
        public List<GPTTurnMessage>? OuputMessages { get; set; } 




        public override string MessageThinking => "";
        public override string MessageContent =>"";



    }




}