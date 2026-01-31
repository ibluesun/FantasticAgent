using FantasticAgent.Base;
using FantasticAgent.Claude;
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


namespace FantasticAgent.GPT
{



    public class GPTThreadRequest : LLMThreadRequest<GPTTurnMessage>
    {


        [JsonPropertyName("input")]
        public List<GPTTurnMessage> InputMessages { get; protected set; } = new List<GPTTurnMessage>();

        [JsonPropertyName("reasoning")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GPTReasoning? Reasoning { get; set; }


        [JsonPropertyName("instructions")]
        public string SystemInstructions { get; set; } = "";




        public override void ClearMessages()
        {
            InputMessages.Clear();
        }


        public override GPTTurnMessage? SystemMessage(string content)
        {
            SystemInstructions = content;

            return null;
        }

        public GPTTurnMessage? DeveloperMessage(string content)
        {
            GPTTurnMessageContent mc = new GPTTurnMessageContent
            {
                MessageContentType = "input_text",
                Text = content
            };

            GPTTurnMessage tm = new GPTTurnMessage
            {
                Role = "developer",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;

        }

        public override GPTTurnMessage? UserMessage(string content)
        {
            GPTTurnMessageContent mc = new GPTTurnMessageContent
            {
                MessageContentType = "input_text",
                Text = content
            };

            GPTTurnMessage tm = new GPTTurnMessage
            {
                Role = "user",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }

        public override GPTTurnMessage? UserCategoryMessage(string category, string content)
        {
            GPTTurnMessageContent mc = new GPTTurnMessageContent
            {
                MessageContentType = "input_text",
                Text = $"[{category}]\n{content}"
            };

            GPTTurnMessage tm = new GPTTurnMessage
            {
                Role = "user",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }

        public override GPTTurnMessage? AssistantReplyMessage(string reply)
        {
            GPTTurnMessageContent mc = new GPTTurnMessageContent
            {
                MessageContentType = "output_text",
                Text = reply
            };

            GPTTurnMessage tm = new GPTTurnMessage
            {
                Role = "assistant",
                Contents = [mc]
            };

            InputMessages.Add(tm);
            return tm;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="jsonOutput">Json formatted text</param>
        /// <returns></returns>
        public GPTTurnMessage FunctionToolReply(string callId, string jsonOutput)
        {
            GPTTurnMessage tm = new GPTTurnMessage();

            tm.MessageType = "function_call_output";
            tm.CallId = callId;

            tm.CallOutput = jsonOutput;

            InputMessages.Add(tm);

            return tm;
        }
    }
}