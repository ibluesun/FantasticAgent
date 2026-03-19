using FantasticAgent.Base;


using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiThreadRequest : LLMThreadRequest<GeminiTurnMessage>
    {

        [JsonPropertyName("system_instruction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiSystemInstructions? SystemInstructions { get; set; } = null;




        [JsonIgnore]
        public override string? Model { get => base.Model; set => base.Model = value; }

        [JsonPropertyName("contents")]
        public List<GeminiTurnMessage> Contents { get; protected set; } = new List<GeminiTurnMessage>();



        [JsonPropertyName("generationConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiGenerationConfiguration? Configuration { get; set; } = null;

        


        public override GeminiTurnMessage? SystemInstructionsPrompt(string content)
        {
            GeminiPart part = new GeminiPart { Text = content };
            SystemInstructions = new GeminiSystemInstructions();
            SystemInstructions.Parts.Add(part);
            return null;
        }



        public override GeminiTurnMessage? UserMessage(string content)
        {
            GeminiPart mc = new GeminiPart
            {
               
                Text = content
            };

            GeminiTurnMessage tm = new GeminiTurnMessage
            {
                Role = "user",
                Parts = [mc]
            };

            Contents.Add(tm);
            return tm;
        }

        public override GeminiTurnMessage? AssistantReplyMessage(string reply)
        {
            GeminiPart mc = new GeminiPart
            {

                Text = reply
            };

            GeminiTurnMessage tm = new GeminiTurnMessage
            {
                Role = "model",
                Parts = [mc]
            };

            Contents.Add(tm);
            return tm;
        }



        public override GeminiTurnMessage? AssistantReasoningReplyMessage(string reasoning, string reply)
        {
            GeminiPart th = new GeminiPart
            {

                Text = reasoning,
                Thought = true

            };

            GeminiPart mc = new GeminiPart
            {

                Text = reply
            };

            GeminiTurnMessage tm = new GeminiTurnMessage
            {
                Role = "model",
                Parts = [th, mc]
            };

            Contents.Add(tm);
            return tm;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="jsonOutput">Json formatted text</param>
        /// <returns></returns>
        public GeminiTurnMessage FunctionToolReply(string name, object result)
        {

            GeminiPartFunctionResponse fr = new GeminiPartFunctionResponse();
            fr.Name = name;
            fr.Response = result;

            GeminiPart mc = new GeminiPart
            {

                FunctionResponse = fr
            };

            GeminiTurnMessage tm = new GeminiTurnMessage
            {
                Role = "user",
                Parts = [mc]
            };

            Contents.Add(tm);
            return tm;
        }


        public GeminiTurnMessage AssistantFromCandidate(GeminiTurnMessageCandidate candidate)
        {
            if (candidate.Content.Parts.Count == 0) return null;
            GeminiTurnMessage tm = new GeminiTurnMessage
            {
                Role = "model",
                Parts = candidate.Content!.Parts!
            };

            Contents.Add(tm);
            return tm;
        }





    }
}
