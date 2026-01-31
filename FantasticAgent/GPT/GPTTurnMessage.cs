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


namespace FantasticAgent.GPT
{


    public class GPTTurnMessageContent
    {
        /// <summary>
        /// input_text  
        /// output_text
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageContentType { get; set; } 

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; } = null;


    }


    public class GPTTurnMessage : LLMTurnMessage
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageId { get; set; } = null;


        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageType { get; set; } = null;



        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageStatus { get; set; }


        [JsonPropertyName("summary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GPTTurnMessageContent>? Summaries { get; set; } = null;



        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GPTTurnMessageContent>? Contents { get; set; } = null;





        #region tool 

        [JsonPropertyName("call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CallId { get; set; } = null;


        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; } = null;



        [JsonPropertyName("arguments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        //public required Dictionary<string, object> Arguments { get; set; }
        public JsonElement Arguments { get; set; }


        /// <summary>
        /// Json output specially for returned values that tuples or arrays
        /// </summary>
        [JsonPropertyName("output")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CallOutput { get; set; } = null;


        #endregion



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
                if (Summaries != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var content in Summaries)
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

    }


}