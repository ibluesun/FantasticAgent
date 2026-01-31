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


    public class ClaudeTurnMessageContent
    {
        /// <summary>
        /// text  
        /// thinking
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageContentType { get; set; }


        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; } = null;


        [JsonPropertyName("thinking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Thinking { get; set; } = null;

        [JsonPropertyName("signature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Signature { get; set; } = null;











        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; } = null;

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; } = null;

        [JsonPropertyName("tool_use_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolUseId { get; set; } = null;

        [JsonPropertyName("partial_json")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PartialJson { get; set; } = null;

        [JsonPropertyName("input")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement Input { get; set; }




        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Content { get; set; } = null;



    }


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