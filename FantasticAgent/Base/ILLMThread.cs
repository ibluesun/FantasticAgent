using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Base
{
    public interface ILLMThread
    {


        public string ProviderName { get; }

        public string ActiveModelName { get; }

        public string[] AvailableModels { get; }

        public string Title { get; }

        public event EventHandler<LLMAssistantEventArgs> AssistantReasoningStarted;
        public event EventHandler<LLMAssistantEventArgs> AssistantReasoningChunkReceived;
        public event EventHandler<LLMAssistantEventArgs> AssistantReasoningEnded;


        public event EventHandler<LLMAssistantEventArgs> AssistantReplyStarted;
        public event EventHandler<LLMAssistantEventArgs> AssistantReplyChunkReceived;
        public event EventHandler<LLMAssistantEventArgs> AssistantReplyEnded;

        public event EventHandler<LLMAssistantEventArgs> AssistantToolRequestStarted;
        public event EventHandler<LLMAssistantEventArgs> AssistantToolRequestChunkReceived;
        public event EventHandler<LLMAssistantEventArgs> AssistantToolRequestEnded;


        public event EventHandler<LLMToolEventArgs> HostToolCalled;
        public event EventHandler<LLMToolEventArgs> HostToolReplied;


    }
}
