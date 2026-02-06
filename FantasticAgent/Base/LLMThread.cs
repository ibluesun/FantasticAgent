using FantasticAgent.Attributes;
using FantasticAgent.Ollama;
using FantasticAgent.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;



namespace FantasticAgent.Base
{

    public record ThreadToolCallResult(string Name, string Result);

    public abstract class LLMThread<RQ, RP, TM> : ILLMThread where TM : LLMTurnMessage, new() where RQ : LLMThreadRequest<TM>, new() where RP : LLMThreadResponse<TM>, new()
    {
        protected readonly HttpClient LLMHttpThreadClient;
        protected readonly Uri ServerUri;

        public event EventHandler<LLMUserEventArgs<TM>> UserMessageQueued;


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


        public bool LogEvents { get; set; } = false;

        public bool LogResponses { get; set; } = false;


        protected List<ThreadToolCallResult> _ThreadToolsResults = new List<ThreadToolCallResult>();

        public ThreadToolCallResult[] ToolsResults => _ThreadToolsResults.ToArray();

        public int ToolsCallsCount => _ThreadToolsResults.Count;


        protected LLMThreadDebugger<LLMThread<RQ, RP, TM>> _LLMLogger;

        public LLMThreadDebugger<LLMThread<RQ, RP, TM>> LLMLogger
        {
            get { return _LLMLogger; }
            set { _LLMLogger = value; }
        }


        public bool IsReasoning { get; set; } = false;
        public bool IsReplying { get; set; } = false;
        public bool IsToolCalling { get; set; } = false;


        public void InvokeAssistantReasoningStarted()
        {
            IsReasoning = true;
            AssistantReasoningStarted?.Invoke(this, new LLMAssistantEventArgs { Message = "" });
        }

        public void InvokeAssistantReasoningChunkReceived(string message)
        {
            AssistantReasoningChunkReceived?.Invoke(this, new LLMAssistantEventArgs { Message = message });
        }
        public void InvokeAssistantReasoningEnded()
        {
            IsReasoning = false;
            AssistantReasoningEnded?.Invoke(this, new LLMAssistantEventArgs { Message = "" });
        }


        public void InvokeAssistantReplyStarted()
        {
            IsReplying = true;
            AssistantReplyStarted?.Invoke(this, new LLMAssistantEventArgs { Message = "" });
        }

        public void InvokeAssistantReplyChunkReceived(string message)
        {
            AssistantReplyChunkReceived?.Invoke(this, new LLMAssistantEventArgs { Message = message });
        }

        public void InvokeAssistantReplyEnded()
        {
            IsReplying = false;
            AssistantReplyEnded?.Invoke(this, new LLMAssistantEventArgs { Message = "" });
        }

        public void InvokeToolCallingStarted(string tool)
        {
            IsToolCalling = true;
            AssistantToolRequestStarted?.Invoke(this, new LLMAssistantEventArgs { Message = tool +"(" });
        }

        public void InvokeToolCallingParameterChunkReceived(string message)
        {
            AssistantToolRequestChunkReceived?.Invoke(this, new LLMAssistantEventArgs { Message = message });
        }

        public void InvokeToolCallingEnded()
        {
            AssistantToolRequestEnded?.Invoke(this, new LLMAssistantEventArgs { Message = ")" });
            IsToolCalling = false;
        }


        /// <summary>
        /// this contains the new message that will be sent
        /// </summary>
        public RQ ActiveRequest { get; protected set; }

        public RQ BufferedRequests { get; protected set; }

        public TM UserMessage(string content)
        {
            if (OnGoingCall)
            {
                // buffer the message
                return BufferedRequests.UserMessage(content);
            }
            else
            {
                // store immediately
                return ActiveRequest.UserMessage(content);
            }
        }

        public TM UserCategoryMessage(string category, string content)
        {
            if (OnGoingCall)
            {
                // buffer the message
                return BufferedRequests.UserCategoryMessage(category, content);
            }
            else
            {
                // store immediately
                return ActiveRequest.UserCategoryMessage(category, content);
            }
        }




        public void DeclareTool(ToolDefinition tool)
        {
            ActiveRequest.DeclareTool(tool);
        }

        protected bool IsNumeric(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
                _ => false
            };
        }


        protected JsonElement NormalizeArguments(JsonElement args)
        {
            if (args.ValueKind == JsonValueKind.String)
            {
                var json = args.GetString();

                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("Empty arguments string");

                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }

            if (args.ValueKind == JsonValueKind.Object)
            {
                return args;
            }

            throw new InvalidOperationException(
                $"Unsupported arguments kind: {args.ValueKind}"
            );
        }




        public virtual void DeclareFunctionTool(MethodInfo method)
        {
            throw new NotImplementedException();    
        }


        public abstract string ProviderName { get; }


        public string ActiveModelName
        {
            get => ActiveRequest.Model;
            set 
            {
                ActiveRequest.Model = value;
                
            }
        }

        public string Title => $"{ProviderName}[{ActiveModelName}]";

        public abstract string[] AvailableModels { get; }


        public LLMThread(string serverUri, string model, string systemRoleMessage)
        {
            ServerUri = new Uri(serverUri);

            LLMHttpThreadClient = new HttpClient();

            LLMHttpThreadClient.Timeout = Timeout.InfiniteTimeSpan;

            ActiveRequest = new RQ();
            ActiveRequest.Model = model;
            var msg = ActiveRequest.SystemMessage(systemRoleMessage);

            BufferedRequests = new RQ();
            BufferedRequests.Model = model;




        }


        protected readonly Channel<string> _AssistantReplies = Channel.CreateUnbounded<string>();

        public ChannelReader<string> AssistantReplies => _AssistantReplies.Reader;


        protected bool OnGoingCall = false;

        public bool IsBusy => OnGoingCall;

        public bool IsToolReplyPending = false;



        protected string _LastReply = string.Empty;
        public string LastReply => _LastReply;


        public virtual TM LastTurnMessage { get; }

        public virtual async Task SendToLLMThread()
        {
            if (OnGoingCall)
                return;

            OnGoingCall = true;

            this.ActiveRequest.Stream = true;

            await OnStreamSend();

        }

        protected virtual async Task OnStreamSend()
        {
            throw new NotImplementedException();
        }


        public async Task SendToLLMThreadNoStream()
        {
            if (OnGoingCall)
                return;

            OnGoingCall = true;
            this.ActiveRequest.Stream = false;

            await OnNoStreamSend();

        }


        protected virtual async Task OnNoStreamSend()
        {
            throw new NotImplementedException();
        }
    }
}