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


    public class LLMThreadRequest<TM> where TM : LLMTurnMessage, new()
    {


        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolDefinition>? Tools { get; protected set; } = null;



        public virtual void ClearMessages()
        {

        }


        public void DeclareTool(ToolDefinition tool)
        {
            if (Tools == null) Tools = new List<ToolDefinition>();

            Tools.Add(tool);
        }


        [JsonIgnore]
        public string DebugView => JsonSerializer.Serialize(this, this.GetType());



        public virtual TM? SystemMessage(string content)
        {
            throw new NotImplementedException();
        }

        public virtual TM? UserMessage(string content)
        {
            throw new NotImplementedException();
        }

        public virtual TM? UserCategoryMessage(string category, string content)
        {
            throw new NotImplementedException();
        }


        public virtual TM? AssistantThinkingMessage(string thinking)
        {
            throw new NotImplementedException();
        }
        public virtual TM? AssistantReplyMessage(string reply)
        {
            throw new NotImplementedException();
        }



    }






}