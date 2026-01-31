using FantasticAgent.Base;
using FantasticAgent.GPT;
using FantasticAgent.Ollama;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;


namespace FantasticAgent.Claude
{



    public class ClaudeThreadRequest : LLMThreadRequest<ClaudeTurnMessage>
    {


        [JsonPropertyName("messages")]
        public List<ClaudeTurnMessage> InputMessages { get; protected set; } = new List<ClaudeTurnMessage>();

        [JsonPropertyName("thinking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ClaudeReasoning? Reasoning { get; set; }


        /// <summary>
        /// Must be bigger the reasoning budge tokens
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 2048;


        [JsonPropertyName("system")]
        public string SystemInstructions { get; set; } = "";

        public override void ClearMessages()
        {
            InputMessages.Clear();
        }


        public override ClaudeTurnMessage? SystemMessage(string content)
        {
            SystemInstructions = content;

            return null;
        }

        public ClaudeTurnMessage? DeveloperMessage(string content)
        {
            throw new LLMUnSupportedFeatureException("Claude does not support developer messages.");

        }

        public override ClaudeTurnMessage? UserMessage(string content)
        {
            ClaudeTurnMessageContent mc = new ClaudeTurnMessageContent
            {
                MessageContentType = "text",
                Text = content
            };

            ClaudeTurnMessage tm = new ClaudeTurnMessage
            {
                Role = "user",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }

        public override ClaudeTurnMessage? UserCategoryMessage(string category, string content)
        {

            ClaudeTurnMessageContent mc = new ClaudeTurnMessageContent
            {
                MessageContentType = "text",
                Text = $"[{category}]\n{content}"
            };
            ClaudeTurnMessage tm = new ClaudeTurnMessage
            {
                Role = "user",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }

        public ClaudeTurnMessage? AssistantMessages(List<ClaudeTurnMessageContent> messages)
        {   
            ClaudeTurnMessage tm = new ClaudeTurnMessage
            {
                Role = "assistant",
                Contents = messages
            };

            InputMessages.Add(tm);
            return tm;
        }

        public override ClaudeTurnMessage? AssistantReplyMessage(string reply)
        {

            ClaudeTurnMessageContent mc = new ClaudeTurnMessageContent
            {
                MessageContentType = "text",
                Text = reply
            };

            ClaudeTurnMessage tm = new ClaudeTurnMessage
            {
                Role = "assistant",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }


        public ClaudeTurnMessage ToolUseCall(string toolId, string toolName, string toolInput)
        {
            throw new NotImplementedException();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="jsonOutput">Json formatted text</param>
        /// <returns></returns>
        public ClaudeTurnMessage FunctionToolReply(string toolId, string jsonOutput)
        {
            ClaudeTurnMessageContent mc = new ClaudeTurnMessageContent
            {
                MessageContentType = "tool_result",
                ToolUseId = toolId,
                Content = jsonOutput

            };
            ClaudeTurnMessage tm = new ClaudeTurnMessage
            {
                Role = "user",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }

    }





}