using FantasticAgent.Attributes;
using FantasticAgent.Base;
using FantasticAgent.Ollama;
using FantasticAgent.Ollama.Tools;
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

namespace FantasticAgent
{



    public class OllamaThread : LLMThread<OllamaThreadRequest, OllamaThreadResponse, OllamaTurnMessage>
    {

        readonly string OllamaServer;
        readonly int OllamaServerPort;


        public OllamaThread(string ollamaServer, int ollamaServerPort, string ollamaModel, string systemRole) : base($"http://{ollamaServer}:{ollamaServerPort}/api/chat", ollamaModel, systemRole)
        {

            OllamaServer = ollamaServer;
            OllamaServerPort = ollamaServerPort;

        }

        public OllamaModel GetModelInformation()
        {
            var mods = new Uri($"http://{OllamaServer}:{OllamaServerPort}/api/show");

            var modclient = new HttpClient();

            var content = new StringContent(JsonSerializer.Serialize(new { model = LLMModel }), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, mods)
            {
                Content = content
            };


            using (var response = LLMHttpThreadClient.Send(request, HttpCompletionOption.ResponseContentRead))
            {


                var tt = response.Content.ReadAsStream();


                OllamaModel model = JsonSerializer.Deserialize<OllamaModel>(tt);


                return model;
            }



        }


        public override OllamaTurnMessage LastTurnMessage => ActiveRequest.TurnMessages.Last();

        
        
        private string AssistantReplyFromThreadResponse(ICollection<OllamaThreadResponse> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (OllamaThreadResponse tr in replies)
            {

                sb.Append(tr.Message.Content);

            }
            return sb.ToString();
        }

        private string ThinkingFromThreadResponse(ICollection<OllamaThreadResponse> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (OllamaThreadResponse tr in replies)
            {
                if (!string.IsNullOrEmpty(tr.Message.Thinking)) sb.Append(tr.Message.Thinking);

            }
            return sb.ToString();
        }

        private ToolCall[] ToolCallsFromThreadResponse(ICollection<OllamaThreadResponse> replies)
        {
            List<ToolCall> ret = new List<ToolCall>();
            foreach (OllamaThreadResponse tr in replies)
            {
                if (tr.Message.ToolCalls != null)
                    ret.AddRange(tr.Message.ToolCalls);
            }

            return ret.ToArray();
        }

        protected Dictionary<string, FunctionDefinition> DeclaredFunctions = new Dictionary<string, FunctionDefinition>(StringComparer.OrdinalIgnoreCase);

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


            // Declare the tool that holds the function tool  

            OllamaFunctionToolDefinition ftd = new OllamaFunctionToolDefinition(f);

            this.DeclareTool(ftd);
        }

        public string ExecuteFunctionCall(FunctionCall call)
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
            return JsonSerializer.Serialize(result) ?? string.Empty;

        }


        protected ReplyLadder ReplyFlow = ReplyLadder.Nothing;

        void AnalyseEvent(OllamaThreadResponse c)
        {
            if (string.IsNullOrEmpty(c.MessageThinking) == false && ReplyFlow == ReplyLadder.Nothing)
            {

                //Thinking started 

                ReplyFlow = ReplyLadder.Thinking;


                InvokeAssistantReasoningStarted();

                InvokeAssistantReasoningChunkReceived(c.MessageThinking);


            }
            else if (string.IsNullOrEmpty(c.MessageThinking) == false && string.IsNullOrEmpty(c.MessageContent) == true && ReplyFlow == ReplyLadder.Thinking)
            {
                // we are still in thinking
                InvokeAssistantReasoningChunkReceived(c.MessageThinking);

            }

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

            else if (string.IsNullOrEmpty(c.MessageContent) == false && ReplyFlow == ReplyLadder.Thinking)
            {
                // we were thinking and now we are replying
                ReplyFlow = ReplyLadder.Replying;

                InvokeAssistantReasoningEnded();

                InvokeAssistantReplyStarted();

                InvokeAssistantReplyChunkReceived(c.MessageContent);
            }

            else if (string.IsNullOrEmpty(c.MessageContent) == false && ReplyFlow == ReplyLadder.Nothing)
            {
                // we are replying immediately  .. but we didn't reason at all
                ReplyFlow = ReplyLadder.Replying;

                InvokeAssistantReplyStarted();

                InvokeAssistantReplyChunkReceived(c.MessageContent);
            }

            else if (string.IsNullOrEmpty(c.MessageContent) == false && ReplyFlow == ReplyLadder.Replying)
            {
                InvokeAssistantReplyChunkReceived(c.MessageContent);

            }
            else if (c.Done && ReplyFlow == ReplyLadder.Replying)
            {
                InvokeAssistantReplyEnded();
                ReplyFlow = ReplyLadder.Nothing;
            }
            else if (c.Done && ReplyFlow == ReplyLadder.Tooling)
            {
                // we wanted tool use  .. so ending here shouldn't trigger replying ended
                ReplyFlow = ReplyLadder.Nothing;
            }

        }

        protected override async Task OnStreamSend()
        {


            await Task.Run(async () =>
            {
                var crj = JsonSerializer.Serialize(ActiveRequest);
                var content = new StringContent(crj, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, ServerUri)
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

                    var rrs = new List<OllamaThreadResponse>();

                    string? line;

                    var mlog = new MemoryStream();
                    var cllog = new StreamWriter(mlog);



                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        cllog.WriteLine(line);

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var c = JsonSerializer.Deserialize<OllamaThreadResponse>(line);

                        if (string.IsNullOrEmpty(c.Error) == false)
                        {
                            throw new LLMException(c.Error);
                        }

                        // Optional: fire event, but do not touch UI here
                        //base.InvokePartialResponseChunkReceived(new LLMResponseEventArgs<OllamaTurnMessage> { Reply = c });
                        AnalyseEvent(c);

                        rrs.Add(c);
                    }

                    if (LogEvents)
                    {
                        // IMPORTANT
                        cllog.Flush();          // push text into MemoryStream
                        mlog.Position = 0;      // rewind stream

                        // Write MemoryStream to file
                        using (var file = File.Create("ollama_events_log.txt"))
                        {
                            mlog.CopyTo(file);
                        }
                    }


                    cllog.Dispose();
                    mlog.Dispose();



                    string thinking = ThinkingFromThreadResponse(rrs);  // we don't include thinking in the payload when we send the chat thread again

                    _LastReply = AssistantReplyFromThreadResponse(rrs);

                    ToolCall[] calls = ToolCallsFromThreadResponse(rrs);

                    IsToolReplyPending = false;

                    if (calls.Length > 0)
                    {
                        ActiveRequest.AssistantToolCalls(thinking, _LastReply, calls);

                        foreach (var tc in calls)
                        {
                            var result = ExecuteFunctionCall(tc.Function);

                            _ThreadToolsResults.Add(new ThreadToolCallResult(tc.Function.Name, result));

                            ActiveRequest.ToolReplyMessage(tc.Function.Name, result);

                            _LLMLogger?.LogInformation($"Function {tc.Function.Name} executed with result: {result}");
                        }
                        IsToolReplyPending = true;
                    }
                    else
                    {

                        ActiveRequest.AssistantReplyMessage(_LastReply);

                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);

                    }

                }
                //catch (Exception ex)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine(ex.ToString());
                //}
                finally
                {
                    foreach (var msg in BufferedRequests.TurnMessages)
                        ActiveRequest.TurnMessages.Add(msg);

                    BufferedRequests.TurnMessages.Clear();
                    OnGoingCall = false;


                }
            }).ConfigureAwait(false);
        }



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

                        writer.WriteBoolean("stream", false); // disable streaming explicitly
                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, ServerUri)
                    {
                        Content = content
                    };

                    using var response = await LLMHttpThreadClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode == false)
                    {
                        var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new Exception($"LLM API Error: {response.StatusCode} - {err}");
                    }


                    OllamaThreadResponse c;
                    if (LogResponses)
                    {
                        var tt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        using (var cllog = new StreamWriter("ollama_log.txt", false))
                        {
                            cllog.Write(tt);
                        }

                        c = JsonSerializer.Deserialize<OllamaThreadResponse>(tt);

                    }
                    else
                    {


                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        c = await JsonSerializer.DeserializeAsync<OllamaThreadResponse>(stream).ConfigureAwait(false);
                    }


                    if (c == null)
                        return;

                    string thinking = ThinkingFromThreadResponse([c]);
                    string reply = AssistantReplyFromThreadResponse([c]);

                    ToolCall[] calls = ToolCallsFromThreadResponse([c]);

                    IsToolReplyPending = false;

                    if (calls.Length > 0)
                    {
                        ActiveRequest.AssistantToolCalls(thinking, reply, calls);

                        foreach (var tc in calls)
                        {
                            var result = ExecuteFunctionCall(tc.Function);
                            ActiveRequest.ToolReplyMessage(tc.Function.Name, result);

                            _LLMLogger?.LogInformation($"Function {tc.Function.Name} executed with result: {result}");

                        }
                        IsToolReplyPending = true;

                    }
                    else
                    {

                        ActiveRequest.AssistantReplyMessage(reply);

                        // Send assistant reply to channel
                        await _AssistantReplies.Writer.WriteAsync(reply);
                    }

                }
                //catch(Exception ex)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine(ex.ToString());
                //}
                finally
                {
                    foreach (var msg in BufferedRequests.TurnMessages)
                        ActiveRequest.TurnMessages.Add(msg);

                    BufferedRequests.TurnMessages.Clear();
                    OnGoingCall = false;
                }
            }).ConfigureAwait(false);

        }




    }

}