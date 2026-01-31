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



    public class OllamaThreadRequest : LLMThreadRequest<OllamaTurnMessage>
    {

        [JsonPropertyName("messages")]
        public List<OllamaTurnMessage> TurnMessages { get; protected set; } = new List<OllamaTurnMessage>();


        public override void ClearMessages()
        {
            TurnMessages.Clear();
        }

        #region Built In Message Signals
        public override OllamaTurnMessage SystemMessage(string content)
        {
            var msg = new OllamaTurnMessage { Role = "system", Content = content };
            TurnMessages.Add(msg);
            return msg;
        }

        public override OllamaTurnMessage UserMessage(string content)
        {
            var msg = new OllamaTurnMessage { Role = "user", Content = content };
            TurnMessages.Add(msg);
            return msg;
        }

        public override OllamaTurnMessage UserCategoryMessage(string category, string content)
        {
            var msg = new OllamaTurnMessage { Role = "user", Content = $"[{category}]\n{content}" };
            TurnMessages.Add(msg);
            return msg;
        }


        public OllamaTurnMessage AssistantMessage(string thinking, string reply)
        {
            var msg = new OllamaTurnMessage { Role = "assistant" };
            if (!string.IsNullOrEmpty(thinking)) msg.Thinking = thinking;
            if (!string.IsNullOrEmpty(reply)) msg.Content = reply;
            TurnMessages.Add(msg);
            return msg;
        }

        public override OllamaTurnMessage AssistantThinkingMessage(string thinking)
        {
            var msg = new OllamaTurnMessage { Role = "assistant", Thinking = thinking };
            TurnMessages.Add(msg);
            return msg;
        }

        public override OllamaTurnMessage AssistantReplyMessage(string reply)
        {
            var msg = new OllamaTurnMessage { Role = "assistant", Content = reply };
            TurnMessages.Add(msg);
            return msg;
        }

        public OllamaTurnMessage AssistantToolCalls(string thinking, string reply, ToolCall[] calls)
        {
            var msg = new OllamaTurnMessage { Role = "assistant" };
            if (!string.IsNullOrEmpty(thinking)) msg.Thinking = thinking;
            if (!string.IsNullOrEmpty(reply)) msg.Content = reply;

            msg.ToolCalls = calls.ToList();
            TurnMessages.Add(msg);
            return msg;
        }


        public OllamaTurnMessage ToolReplyMessage(string toolName, string content)
        {
            var msg = new OllamaTurnMessage { Role = "tool", ToolName = toolName, Content = content };
            TurnMessages.Add(msg);
            return msg;

        }
        #endregion




    }





}