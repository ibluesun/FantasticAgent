using FantasticAgent.Base;
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


namespace FantasticAgent.Claude
{

    public class ClaudeTurnMessage : LLMTurnMessage
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageId { get; set; } = null;


        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageType { get; set; } = null;


        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageName { get; set; } = null;



        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageStatus { get; set; }





        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ClaudeTurnMessageContent>? Contents { get; set; } = null;


        [JsonIgnore]
        public override string? MessageTextContent
        {
            get
            {
                if (Contents != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var content in Contents)
                    {
                        if (content.Text != null)
                        {
                            sb.Append(content.Text);
                        }
                    }
                    return sb.ToString();
                }
                return null;
            }
        }

        [JsonIgnore]
        public override string? MessageReasoningOrThinking
        {
            get
            {
                if (Contents != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var content in Contents)
                    {
                        if (content.Thinking != null)
                        {
                            sb.Append(content.Thinking);
                        }
                    }
                    return sb.ToString();
                }
                return null;
            }
        }

    }


}