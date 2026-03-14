using FantasticAgent.Base;


using FantasticAgent.Gemini;


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


        public override void DeclareFunctionTool(MethodInfo method)
        {
            
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

                        if(!LogTurns) LogRequest(ActiveRequest.DebugView);
                        LogResponseError(response.StatusCode, err);
                        throw new Exception($"LLM API Error: {response.StatusCode} - {err}");
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

                    string reasoning = ReasoningFromThreadResponse(c.Candidates!);
                    _LastReply = AssistantReplyFromThreadResponse(c.Candidates!);

                    
                    if (string.IsNullOrEmpty(reasoning))
                        ActiveRequest.AssistantReplyMessage(_LastReply);
                    else
                        ActiveRequest.AssistantReasoningReplyMessage(reasoning, _LastReply);


                        

                    

                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }



                    /*

                    IsToolReplyPending = false;

                    foreach (var ot in c.OuputMessages)
                    {
                        if (ot.CallId != null)
                        {
                            string result = "";
                            try
                            {
                                result = ExecuteFunctionCall(ot);
                            }
                            catch (Exception e)
                            {
                                result = $"Tool call named [{ot.Name}] has halted because of a catastrophic internal runtime exception description of [{e.Message}]. Stop calling this function again and tell the user to report to developers about this function.";
                            }

                            ActiveRequest.FunctionToolReply(ot.CallId, result);

                            _LLMLogger?.LogInformation($"Function {ot.Name}({ot.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;

                        }
                    }
                        */
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


            /*
            else if (c.Message.ToolCalls != null && c.Message.ToolCalls.Count > 0 && ReplyFlow == ReplyLadder.Thinking)
            {
                // we were reasoning then we needed a tool call
                ReplyFlow = ReplyLadder.Tooling;
                InvokeAssistantReasoningEnded();

                // in ollama the tool use is done in one line  so we need to call the three functions here
                foreach (var tt in c.Message.ToolCalls)
                {
                    InvokeToolCallingStarted(tt.Function.Name);
                    InvokeToolCallingParameterChunkReceived(tt.Function.Arguments.ToString());
                    InvokeToolCallingEnded();
                }


            }

            else if (c.Message.ToolCalls != null && c.Message.ToolCalls.Count > 0 && ReplyFlow == ReplyLadder.Nothing)
            {
                // we jumped to the tool call immediately without thinking .. some models do that 
                ReplyFlow = ReplyLadder.Tooling;

                // in ollama the tool use is done in one line  so we need to call the three functions here
                foreach (var tt in c.Message.ToolCalls)
                {
                    InvokeToolCallingStarted(tt.Function.Name);
                    InvokeToolCallingParameterChunkReceived(tt.Function.Arguments.ToString());
                    InvokeToolCallingEnded();
                }


            }
            */

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
                        if (LogEvents) LogResponseEvent(line);
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
                                        else
                                        {
                                            Reply.Parts.Add(prt);
                                        }
                                    }
                                }

                                c.Done = candy.FinishReason == "STOP";
                            }

                            c.Usage = cbs.usageMetadata;
                            c.ModelVersion = cbs.modelVersion;
                            c.ResponseId = cbs.responseId;
                            
                            
                        }

                    }


                    if (LogEvents) LogEventsFinishedFile();

                    if (c == null || c.Candidates == null || c.Candidates.Count == 0)
                    {
                        IsToolReplyPending = false;
                        return;
                    }



                    IsToolReplyPending = false;
                    /*
                    string toolname = "";

                    if (c.StopReason == "tool_use")
                    {
                        // only execute tools if the reply was a complete reply.
                        foreach (var om in c.OuputMessages)
                        {
                            if (om.MessageContentType == "tool_use")
                            {
                                FunctionCall fc = new FunctionCall { Id = om.Id, Name = om.Name, Arguments = om.Input };
                                string result = "";

                                try
                                {
                                    result = ExecuteFunctionCall(fc);
                                }
                                catch (Exception e)
                                {
                                    result = $"Tool call named [{fc.Name}] has halted because of a catastrophic internal runtime exception description of [{e.Message}]. Stop calling this function again and tell the user to report to developers about this function.";
                                }

                                _ThreadToolsResults.Add(new ThreadToolCallResult(fc.Name, result));

                                ActiveRequest.FunctionToolReply(fc.Id, result);

                                _LLMLogger?.LogInformation($"Function {fc.Name}({fc.Arguments.ToString()}) executed with result: {result}");

                                IsToolReplyPending = true;
                                toolname += fc.Name;
                            }
                        }

                    }
                    */

                    

                    string reasoning = ReasoningFromThreadResponse(c.Candidates!);
                    _LastReply = AssistantReplyFromThreadResponse(c.Candidates!);

                    
                    if (string.IsNullOrEmpty(reasoning))
                        ActiveRequest.AssistantReplyMessage(_LastReply);
                    else
                        ActiveRequest.AssistantReasoningReplyMessage(reasoning, _LastReply);


                    

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

    }
}
