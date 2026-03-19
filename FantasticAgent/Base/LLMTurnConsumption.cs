using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Base
{
    public class LLMTurnConsumption 
    {

        public int TurnIndex { get; init; }

        public int InputTokens { get; set; }

        public int ModelThinkingTokens { get; set; }

        public int ModelOutputTokens { get; set; }

        public int OutputTokens => ModelOutputTokens + ModelThinkingTokens;


        public int ToolCalls { get; set; }
    }

}
