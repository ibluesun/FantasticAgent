using FantasticAgent.Attributes;
using FantasticAgent.Base;


using FantasticAgent.Gemini;
using FantasticAgent.Gemini.Tools;
using FantasticAgent.Tools;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;



namespace FantasticAgent
{
    public class GeminiThread : LLMThread<GeminiThreadRequest, GeminiThreadResponse, GeminiTurnMessage>
    {

        public GeminiThread(string secretKey, string model, string systemRole) 
            : base($"https://generativelanguage.googleapis.com/v1beta/models/{model}", model, systemRole)
        {


            LLMHttpThreadClient.DefaultRequestHeaders.Add("Accept", $"application/json");
            LLMHttpThreadClient.DefaultRequestHeaders.Add("x-goog-api-key", $"{secretKey}");
            //LLMHttpThreadClient.DefaultRequestHeaders.Add("Content-Type", $"application/json");


        }



        public override string ProviderName => "Gemini";

        public override string[] AvailableModels => new string[] { "gemini-3-flash-preview" };


        protected Dictionary<string, FunctionDefinition> DeclaredFunctions = new Dictionary<string, FunctionDefinition>(StringComparer.OrdinalIgnoreCase);
        GeminiFunctionsDeclarationsToolDefinition? GeminiDeclaredFunctionsTool = null;

        public override void DeclareFunctionTool(MethodInfo method)
        {
            ObjectDefinition methodParameters = new ObjectDefinition();

            var prms = method.GetParameters();
            foreach (var prm in prms)
            {
                LLMDescriptionAttribute? descAttr = prm.GetCustomAttribute<LLMDescriptionAttribute>();

                string prmDescription = prm.Name!;
                if (descAttr != null) prmDescription = descAttr.Description;

                methodParameters.AddProperty(prm.Name!, IsNumeric(prm.ParameterType) ? MemberDataType.Number : MemberDataType.String, true, prmDescription);
            }


            string MethodDescription = method.Name;
            LLMDescriptionAttribute? methDesc = method.GetCustomAttribute<LLMDescriptionAttribute>();
            if (methDesc != null) MethodDescription = methDesc.Description;



            FunctionDefinition f = new FunctionDefinition(method.Name, MethodDescription, methodParameters);
            f.UnderlyingMethod = method;

            DeclaredFunctions.Add(f.Name, f);

            if (GeminiDeclaredFunctionsTool == null)
            {
                GeminiDeclaredFunctionsTool = new GeminiFunctionsDeclarationsToolDefinition();
                this.DeclareTool(GeminiDeclaredFunctionsTool);
            }

            GeminiDeclaredFunctionsTool.AddFunctionDefinition(f);
        }


        public object? ExecuteFunctionCall(GeminiPartFunctionCall call)
        {
            if (!DeclaredFunctions.TryGetValue(call.Name, out var funcDef))
                throw new LLMException($"Function '{call.Name}' not declared");
            var method = funcDef.UnderlyingMethod;
            if (method == null)
                throw new LLMException($"Function '{call.Name}' has no underlying method");
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var prm = parameters[i];
                var ncargs = NormalizeArguments(call.Arguments);
                var argProperty = ncargs.GetProperty(prm.Name!);
                if (argProperty.ValueKind == JsonValueKind.String)
                    args[i] = Convert.ChangeType(argProperty.GetString(), prm.ParameterType);
                else if (argProperty.ValueKind == JsonValueKind.Number)
                    args[i] = Convert.ChangeType(argProperty.GetDouble(), prm.ParameterType);
                else
                    args[i] = Convert.ChangeType(argProperty.GetRawText(), prm.ParameterType);

            }
            var result = method.Invoke(null, args);
            return result;

        }



        private string AssistantReplyFromThreadResponse(ICollection<GeminiTurnMessageCandidate> candidates)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var candy in candidates)
            {
                if (candy.Content != null && candy.Content.Parts != null)
                {
                    foreach (var contentPart in candy.Content.Parts)
                    {
                        if (contentPart.Thought.HasValue)
                        {
                            if (contentPart.Thought.Value == false)
                                sb.Append(contentPart.Text);

                        }
                        else
                            sb.Append(contentPart.Text);
                    }
                }

            }

            return sb.ToString();
        }

        private string ReasoningFromThreadResponse(ICollection<GeminiTurnMessageCandidate> candidates)
        {
            StringBuilder sb = new StringBuilder();


            foreach (var candy in candidates)
            {
                if (candy.Content != null && candy.Content.Parts != null)
                {
                    foreach (var contentPart in candy.Content.Parts)
                    {
                        if (contentPart.Thought.HasValue == true && contentPart.Thought == true)
                            sb.Append(contentPart.Text);
                    }
                }

            }


            return sb.ToString();
        }

        private GeminiPartFunctionCall[] CallsFromThreadResponse(ICollection<GeminiTurnMessageCandidate> candidates)
        {
            List<GeminiPartFunctionCall> calls = new List<GeminiPartFunctionCall>();

            foreach (var candy in candidates)
            {
                if (candy.Content != null && candy.Content.Parts != null)
                {
                    foreach (var contentPart in candy.Content.Parts)
                    {
                        if (contentPart.FunctionCall != null)
                            calls.Add(contentPart.FunctionCall);
                    }
                }

            }


            return calls.ToArray();
        }

        public override GeminiTurnMessage LastTurnMessage => ActiveRequest.Contents.LastOrDefault();


        protected override async Task OnNoStreamSend()
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Clone the active request and ensure "stream": false
                    var cloned = JsonSerializer.SerializeToDocument(ActiveRequest).RootElement;
                    using var ms = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(ms))
                    {
                        writer.WriteStartObject();
                        foreach (var prop in cloned.EnumerateObject())
                            prop.WriteTo(writer);
                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");


                    // notice .. add   :streamGenerateContent   for sse streaming

                    var request = new HttpRequestMessage(HttpMethod.Post, ServerUri + ":generateContent")
                    {
                        Content = content
                    };

                    if (LogTurns) LogRequest(ActiveRequest.DebugView);

                    using var response = await LLMHttpThreadClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode == false)
                    {
                        var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!LogTurns) LogRequest(ActiveRequest.DebugView);
                        LogResponseError(response.StatusCode, err);
                        var error = JsonSerializer.Deserialize<ErrorEnvelope>(err);


                        InvokeAssistantErrorReceived(error!.Error!);
                        IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                        return;
                    }



                    GeminiThreadResponse c;
                    if (LogTurns)
                    {
                        var tt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        LogResponse(tt);

                        c = JsonSerializer.Deserialize<GeminiThreadResponse>(tt);

                    }
                    else
                    {


                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        c = await JsonSerializer.DeserializeAsync<GeminiThreadResponse>(stream).ConfigureAwait(false);
                    }


                    if (c == null)
                        return;

                    if (c.Error != null)
                    {
                        InvokeAssistantErrorReceived(c.Error!);
                        IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                        return;
                    }


                    if (c == null || c.Candidates == null || c.Candidates.Count == 0)
                    {
                        IsToolReplyPending = false;
                        return;
                    }

                    LastTurnInformation.InputTokens = c.Usage.PromptTokenCount;
                    LastTurnInformation.ModelThinkingTokens = c.Usage.ThoughtsTokenCount;
                    LastTurnInformation.ModelOutputTokens = c.Usage.CandidatesTokenCount;


                    IsToolReplyPending = false;
                    // each candidate will be a turn message
                    foreach (var ot in c.Candidates)
                    {
                        ActiveRequest.AssistantFromCandidate(ot);

                        foreach (var prt in ot.Content!.Parts!)
                        {
                            if (prt.FunctionCall != null)
                            {
                                object? result = null;
                                try
                                {
                                    result = ExecuteFunctionCall(prt.FunctionCall);
                                    LastTurnInformation.ToolCalls++;
                                }
                                catch (Exception e)
                                {
                                    result = $"Tool call named [{prt.FunctionCall.Name}] has halted because of a catastrophic internal runtime exception description of [{e.Message}]. Stop calling this function again and tell the user to report to developers about this function.";
                                }

                                ActiveRequest.FunctionToolReply(prt.FunctionCall.Name!, result!);

                                _LLMLogger?.LogInformation($"Function {prt.FunctionCall.Name}({prt.FunctionCall.Arguments.ToString()}) executed with result: {result}");

                                IsToolReplyPending = true;
                            }
                        }
                    }

                    string reasoning = ReasoningFromThreadResponse(c.Candidates!);
                    _LastReply = AssistantReplyFromThreadResponse(c.Candidates!);






                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }



                }
                //catch(Exception ex)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine(ex.ToString());
                //}
                finally
                {
                    foreach (var msg in BufferedRequests.Contents)
                        ActiveRequest.Contents.Add(msg);

                    BufferedRequests.Contents.Clear();
                    OnGoingCall = false;
                }
            }).ConfigureAwait(false);

        }



        public record GeminiEventBlock(List<GeminiTurnMessageCandidate> candidates, GeminiUsage usageMetadata, string modelVersion, string responseId);

        protected ReplyLadder ReplyFlow = ReplyLadder.Nothing;

        void AnalyseEvent(GeminiEventBlock c)
        {
            var MessageThinking = ReasoningFromThreadResponse(c.candidates);
            var MessageContent = AssistantReplyFromThreadResponse(c.candidates);
            var MessageCalls = CallsFromThreadResponse(c.candidates);

            string? finishReason = c.candidates.Where(candy => string.IsNullOrEmpty(candy.FinishReason) == false).Select(candy => candy.FinishReason).FirstOrDefault();

            if (string.IsNullOrEmpty(finishReason)) finishReason = "";

            if (string.IsNullOrEmpty(MessageThinking) == false && ReplyFlow == ReplyLadder.Nothing)
            {

                //Thinking started 

                ReplyFlow = ReplyLadder.Thinking;


                InvokeAssistantReasoningStarted();

                InvokeAssistantReasoningChunkReceived(MessageThinking);


            }
            else if (string.IsNullOrEmpty(MessageThinking) == false && string.IsNullOrEmpty(MessageContent) == true && ReplyFlow == ReplyLadder.Thinking)
            {
                // we are still in thinking
                InvokeAssistantReasoningChunkReceived(MessageThinking);

            }


            
            else if (MessageCalls != null && MessageCalls.Length > 0 && ReplyFlow == ReplyLadder.Thinking)
            {
                // we were reasoning then we needed a tool call
                ReplyFlow = ReplyLadder.Tooling;
                InvokeAssistantReasoningEnded();

                // in ollama the tool use is done in one line  so we need to call the three functions here
                foreach (var tt in MessageCalls)
                {
                    InvokeToolCallingStarted(tt.Name!);
                    InvokeToolCallingParameterChunkReceived(tt.Arguments.ToString());
                    InvokeToolCallingEnded();
                }


            }

            else if (MessageCalls != null && MessageCalls.Length > 0 && ReplyFlow == ReplyLadder.Nothing)
            {
                // we jumped to the tool call immediately without thinking .. some models do that 
                ReplyFlow = ReplyLadder.Tooling;

                // in ollama the tool use is done in one line  so we need to call the three functions here
                foreach (var tt in MessageCalls)
                {
                    InvokeToolCallingStarted(tt.Name!);
                    InvokeToolCallingParameterChunkReceived(tt.Arguments.ToString());
                    InvokeToolCallingEnded();
                }


            }
            

            else if (string.IsNullOrEmpty(MessageContent) == false && ReplyFlow == ReplyLadder.Thinking)
            {
                // we were thinking and now we are replying
                ReplyFlow = ReplyLadder.Replying;

                InvokeAssistantReasoningEnded();

                InvokeAssistantReplyStarted();

                InvokeAssistantReplyChunkReceived(MessageContent);
            }

            else if (string.IsNullOrEmpty(MessageContent) == false && ReplyFlow == ReplyLadder.Nothing)
            {
                // we are replying immediately  .. but we didn't reason at all
                ReplyFlow = ReplyLadder.Replying;

                InvokeAssistantReplyStarted();

                InvokeAssistantReplyChunkReceived(MessageContent);
            }

            else if (string.IsNullOrEmpty(MessageContent) == false && ReplyFlow == ReplyLadder.Replying)
            {
                InvokeAssistantReplyChunkReceived(MessageContent);

            }
            else if (finishReason=="STOP" && ReplyFlow == ReplyLadder.Replying)
            {
                InvokeAssistantReplyEnded();
                ReplyFlow = ReplyLadder.Nothing;
            }
            else if (finishReason == "STOP" && ReplyFlow == ReplyLadder.Tooling)
            {
                // we wanted tool use  .. so ending here shouldn't trigger replying ended
                ReplyFlow = ReplyLadder.Nothing;
            }

        }



        protected override async Task OnStreamSend()
        {


            await Task.Run(async () =>
            {
                var cloned = JsonSerializer.SerializeToDocument(ActiveRequest).RootElement;
                using var ms = new MemoryStream();
                using (var writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartObject();
                    foreach (var prop in cloned.EnumerateObject())
                        prop.WriteTo(writer);

                    writer.WriteEndObject();
                }

                ms.Position = 0;
                var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

                if (LogTurns) LogRequest(ActiveRequest.DebugView);

                var request = new HttpRequestMessage(HttpMethod.Post, ServerUri + ":streamGenerateContent?alt=sse")
                {
                    Content = content
                };

                try
                {
                    using var response = await LLMHttpThreadClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead
                    ).ConfigureAwait(false);

                    using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var reader = new StreamReader(stream);

                    List<GeminiEventBlock> ChunkReplies = new List<GeminiEventBlock>();

                    string? line;


                    GeminiThreadResponse c = new GeminiThreadResponse();
                    c.Candidates = new List< GeminiTurnMessageCandidate>();

                    GeminiTurnMessageCandidate kolloh = new GeminiTurnMessageCandidate();
                    GeminiTurnMessageCandidateContent Thoughts = new GeminiTurnMessageCandidateContent();
                    GeminiTurnMessageCandidateContent Reply = new GeminiTurnMessageCandidateContent();

                    Thoughts.Parts = new List<GeminiPart>();
                    Reply.Parts = new List<GeminiPart>();


                    c.Candidates.Add(new GeminiTurnMessageCandidate { Content = Thoughts });
                    c.Candidates.Add(new GeminiTurnMessageCandidate { Content = Reply });


                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        if (LogStreamingEvents) LogResponseEvent(line);
                        GeminiEventBlock cc;

                        string payLoad;


                        if (line.StartsWith("data"))
                        {
                            payLoad = line.Substring("data:".Length).Trim();

                            var cbs = JsonSerializer.Deserialize<GeminiEventBlock>(payLoad);

                            AnalyseEvent(cbs);

                            ChunkReplies.Add(cbs);   

                            foreach(var candy in cbs.candidates)
                            {
                                if (candy.Content != null && candy.Content.Parts != null)
                                {
                                    foreach (var prt in candy.Content.Parts)
                                    {
                                        if (prt.Thought == true)
                                        {
                                            Thoughts.Parts.Add(prt);
                                        }
                                        else if (prt.FunctionCall != null)
                                        {
                                            Thoughts.Parts.Add(prt);
                                        }
                                        else if(!string.IsNullOrEmpty(prt.Text))
                                        {
                                            Reply.Parts.Add(prt);
                                        }
                                    }
                                }

                                if (candy.FinishReason != null && candy.FinishReason.Length > 0)
                                {
                                    c.Done = candy.FinishReason == "STOP";
                                    c.FinishReason = candy.FinishReason;

                                    LastTurnInformation.InputTokens = c.Usage.PromptTokenCount;
                                    LastTurnInformation.ModelThinkingTokens = c.Usage.ThoughtsTokenCount;
                                    LastTurnInformation.ModelOutputTokens = c.Usage.CandidatesTokenCount;


                                }
                            }

                            c.Usage = cbs.usageMetadata;
                            c.ModelVersion = cbs.modelVersion;
                            c.ResponseId = cbs.responseId;
                            
                            
                            
                        }

                    }


                    if (LogStreamingEvents) LogEventsFinishedFile();

                    if (c == null || c.Candidates == null || c.Candidates.Count == 0)
                    {
                        IsToolReplyPending = false;
                        return;
                    }


                    IsToolReplyPending = false;
                    // each candidate will be a turn message
                    foreach (var ot in c.Candidates)
                    {
                        ActiveRequest.AssistantFromCandidate(ot);

                        foreach (var prt in ot.Content!.Parts!)
                        {
                            if (prt.FunctionCall != null)
                            {
                                object? result = null;
                                try
                                {
                                    result = ExecuteFunctionCall(prt.FunctionCall);
                                    LastTurnInformation.ToolCalls++;
                                }
                                catch (Exception e)
                                {
                                    result = $"Tool call named [{prt.FunctionCall.Name}] has halted because of a catastrophic internal runtime exception description of [{e.Message}]. Stop calling this function again and tell the user to report to developers about this function.";
                                }

                                ActiveRequest.FunctionToolReply(prt.FunctionCall.Name!, result!);

                                _LLMLogger?.LogInformation($"Function {prt.FunctionCall.Name}({prt.FunctionCall.Arguments.ToString()}) executed with result: {result}");

                                IsToolReplyPending = true;
                            }
                        }
                    }


                    string reasoning = ReasoningFromThreadResponse(c.Candidates!);
                    _LastReply = AssistantReplyFromThreadResponse(c.Candidates!);



               

                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }

                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
                finally
                {
                    foreach (var msg in BufferedRequests.Contents)
                        ActiveRequest.Contents.Add(msg);

                    BufferedRequests.Contents.Clear();
                    OnGoingCall = false;


                }
            }).ConfigureAwait(false);


        }



        public override string[] UserMessages
        {
            get
            {
                List<string> messages = new List<string>();
                foreach (var ms in ActiveRequest.Contents)
                {
                    if (ms.Role == "user" && ms.Parts != null)
                    {
                        foreach (var c in ms.Parts)
                        {


                            if (c.Text != null) messages.Add(c.Text);

                        }
                    }
                }
                return messages.ToArray();
            }
        }

    }
}
