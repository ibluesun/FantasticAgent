using FantasticAgent.Base;
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
namespace FantasticAgent.Ollama
{




    public class OllamaThreadResponse : LLMThreadResponse<OllamaTurnMessage>
    {
        //{"model":"gpt-oss","created_at":"2025-10-20T21:58:27.2433266Z","message":{"role":"assistant","content":"","thinking":"We"},"done":false}



        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("message")]
        public OllamaTurnMessage Message { get; set; }


        [JsonPropertyName("error")]
        public string? Error { get; set; }


        public override string MessageContent => Message.Content;

        public override string MessageThinking => Message.Thinking;

    }




}