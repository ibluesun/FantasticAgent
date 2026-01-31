using FantasticAgent.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Base
{
    public static class LLM
    {





    }



    public enum MessgageRoles
    {
        /// <summary>
        /// First message to tell the llm how would he acts
        /// </summary>
        System,

        /// <summary>
        /// The user that really needs the conversation 
        /// </summary>
        User,

        /// <summary>
        /// The LLM reply  
        /// </summary>
        Assistant
    }

    public class LLMToolEventArgs : EventArgs
    {

    }

    public class LLMAssistantEventArgs : EventArgs
    {
        public required string Message { get; set; }
    }

    public class LLMUserEventArgs<TM> : EventArgs where TM : LLMTurnMessage
    {
        public required TM UserMessage { get; set; }

    }


    public class LLMResponseEventArgs<TM> : EventArgs where TM : LLMTurnMessage
    {
        public required LLMThreadResponse<TM> Reply { get; set; }

    }

    public enum ReplyLadder
    {
        Nothing,
        Thinking,
        Tooling,
        Replying,
    }



}
