using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FantasticAgent.Base
{
    public class LLMTurnInformation 
    {

        public int TurnIndex { get; init; }


        public string UserMessage { get; set; }

        public string AiResponse { get; set; }

        public bool IsToolReply { get; set; } = false;


        public int InputTokens { get; set; }

        public int ModelThinkingTokens { get; set; }

        public int ModelOutputTokens { get; set; }

        public int OutputTokens => ModelOutputTokens + ModelThinkingTokens;


        public int ToolCalls { get; set; }
    }

}
