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
namespace FantasticAgent.Claude
{

    public class ClaudeThreadResponse : LLMThreadResponse<ClaudeTurnMessage>
    {


        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? ResponseObjectType { get; set; }


        [JsonPropertyName("created_at")]
        public int CreatedAt { get; set; }

        [JsonPropertyName("reasoning")]
        public ClaudeReasoning? Reasoning { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage? Usage { get; set; }


        [JsonPropertyName("content")]
        public List<ClaudeTurnMessageContent>? OuputMessages { get; set; } 




        public override string MessageThinking
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (OuputMessages != null)
                {
                    foreach (var msg in OuputMessages)
                    {
                        if (msg.MessageContentType == "thinking")
                            sb.Append(msg.Thinking);


                    }
                }
                return sb.ToString();
            }

        }


        public override string MessageContent 
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (OuputMessages != null)
                {
                    foreach (var msg in OuputMessages)
                    {
                        if (msg.MessageContentType == "text")
                            sb.Append(msg.Text);


                    }
                }
                return sb.ToString();
            }
        }



    }




}