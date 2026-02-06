using FantasticAgent.Attributes;
using FantasticAgent.Base;
using FantasticAgent.Claude;
using FantasticAgent.Claude.Tools;
using FantasticAgent.Ollama;
using FantasticAgent.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace FantasticAgent
{



    public class ClaudeThread : LLMThread<ClaudeThreadRequest, ClaudeThreadResponse, ClaudeTurnMessage>
    {
        public ClaudeThread(string secretKey, string anthropicModel, string systemRole) : base("https://api.anthropic.com/v1/messages", anthropicModel, systemRole)
        {


            LLMHttpThreadClient.DefaultRequestHeaders.Add("Accept", $"application/json");
            LLMHttpThreadClient.DefaultRequestHeaders.Add("x-api-key", $"{secretKey}");

            LLMHttpThreadClient.DefaultRequestHeaders.Add("anthropic-version", $"2023-06-01");


        }


        public override string ProviderName => "Claude";

        public override string[] AvailableModels => new string[]{"claude-haiku-4-5","claude-sonnet-4-5", "claude-opus-4-6" };


        public override ClaudeTurnMessage LastTurnMessage => ActiveRequest.InputMessages.Last();


        private string JsonFromThreadResponse(ICollection<ClaudeEventContentBlock> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var tr in replies)
            {
                if (tr.delta != null && tr.delta.MessageContentType == "input_json_delta")
                    sb.Append(tr.delta.PartialJson);
            }

            return sb.ToString();
        }

        private string AssistantReplyFromThreadResponse(ICollection<ClaudeEventContentBlock> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var tr in replies)
            {
                if (tr.delta != null && tr.delta.MessageContentType == "text_delta")
                    sb.Append(tr.delta.Text);
            }
            return sb.ToString();
        }

        private string ReasoningFromThreadResponse(ICollection<ClaudeEventContentBlock> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var tr in replies)
            {
                if (tr.delta != null && tr.delta.MessageContentType == "thinking_delta")
                    sb.Append(tr.delta.Thinking);
            }

            return sb.ToString();
        }

        protected Dictionary<string, ClaudeFunctionToolDefinition> DeclaredFunctions = new Dictionary<string, ClaudeFunctionToolDefinition>(StringComparer.OrdinalIgnoreCase);


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



            ClaudeFunctionToolDefinition f = new ClaudeFunctionToolDefinition(method.Name, MethodDescription, methodParameters);
            f.UnderlyingMethod = method;

            DeclaredFunctions.Add(f.Name, f);



            this.DeclareTool(f);
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





        //{"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":""}}
        public record ClaudeEventContentBlock(string type, int index, ClaudeTurnMessageContent content_block, ClaudeTurnMessageContent delta);





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

                    List<ClaudeEventContentBlock> ChunkReplies = new List<ClaudeEventContentBlock>();

                    string? line;


                    var mlog = new MemoryStream();
                    var cllog = new StreamWriter(mlog);



                    ClaudeThreadResponse c = new ClaudeThreadResponse();
                    c.OuputMessages = new List<ClaudeTurnMessageContent>();

                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        cllog.WriteLine(line);
                        ClaudeEventContentBlock cc;
                        string nextLine;
                        string payLoad;
                        ClaudeTurnMessageContent om = c.OuputMessages.LastOrDefault();
                        switch (line)
                        {

                            case "event: message_start":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);
                                payLoad = nextLine.Substring("data:".Length).Trim();


                                break;


                            case "event: content_block_start":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);

                                payLoad = nextLine.Substring("data:".Length).Trim();
                                var cbs = JsonSerializer.Deserialize<ClaudeEventContentBlock>(payLoad);
                                ChunkReplies.Add(cbs);

                                if (cbs.content_block.MessageContentType == "thinking")
                                {
                                    om = new ClaudeTurnMessageContent();
                                    om.MessageContentType = "thinking";
                                    c.OuputMessages.Add(om);
                                    InvokeAssistantReasoningStarted();
                                }
                                else if (cbs.content_block.MessageContentType == "text")
                                {
                                    om = new ClaudeTurnMessageContent();
                                    om.MessageContentType = "text";
                                    c.OuputMessages.Add(om);
                                    InvokeAssistantReplyStarted();
                                }
                                else if (cbs.content_block.MessageContentType == "tool_use")
                                {
                                    om = new ClaudeTurnMessageContent();
                                    om.MessageContentType = "tool_use";
                                    om.Id = cbs.content_block.Id;
                                    om.Name = cbs.content_block.Name;
                                    c.OuputMessages.Add(om);
                                    InvokeToolCallingStarted(cbs.content_block.Name);
                                }
                                else
                                    throw new LLMUnknownEventException(cbs.content_block.MessageContentType);
                                break;


                            case "event: content_block_delta":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);

                                payLoad = nextLine.Substring("data:".Length).Trim();
                                cc = JsonSerializer.Deserialize<ClaudeEventContentBlock>(payLoad);
                                ChunkReplies.Add(cc);

                                if (cc.delta.MessageContentType == "text_delta")
                                {
                                    om.Text += cc.delta.Text;
                                    InvokeAssistantReplyChunkReceived(cc.delta.Text);
                                }
                                else if (cc.delta.MessageContentType == "thinking_delta")
                                {
                                    om.Thinking += cc.delta.Thinking;
                                    InvokeAssistantReasoningChunkReceived(cc.delta.Thinking);
                                }
                                else if (cc.delta.MessageContentType == "input_json_delta")
                                {
                                    om.PartialJson += cc.delta.PartialJson;
                                    InvokeToolCallingParameterChunkReceived(cc.delta.PartialJson);
                                }
                                else if(cc.delta.MessageContentType == "signature_delta")
                                {
                                    om.Signature += cc.delta.Signature;
                                }
                                else
                                {
                                    //throw new LLMUnknownEventException(cc.delta.MessageContentType);
                                }

                                break;


                            case "event: content_block_stop":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);

                                payLoad = nextLine.Substring("data:".Length).Trim();
                                var cbc = JsonSerializer.Deserialize<ClaudeEventContentBlock>(payLoad);
                                ChunkReplies.Add(cbc);
                                if (IsReasoning)
                                {
                                    InvokeAssistantReasoningEnded();
                                }
                                else if (IsReplying)
                                {
                                    InvokeAssistantReplyEnded();
                                }
                                else if (IsToolCalling)
                                {
                                    // here make the PartialJson we accumulated into Input JsonElement
                                    om.Input = JsonDocument.Parse(om.PartialJson).RootElement;
                                    om.PartialJson = null;  // clear partialjson here
                                    InvokeToolCallingEnded();
                                }
                                else
                                {
                                    //throw new LLMUnknownEventException(cbc.content_block.MessageContentType);
                                }
                                break;


                            case "event: message_delta":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);
                                payLoad = nextLine.Substring("data:".Length).Trim();
                                break;


                            case "event: message_stop":
                                nextLine = await reader.ReadLineAsync();
                                cllog.WriteLine(nextLine);

                                payLoad = nextLine.Substring("data:".Length).Trim();

                                break;



                        }



                    }


                   

                    if (c == null || c.OuputMessages == null || c.OuputMessages.Count == 0)
                    {
                        IsToolReplyPending = false;
                        return;
                    }

                    ActiveRequest.AssistantMessages(c.OuputMessages!);

                    IsToolReplyPending = false;
                    string toolname = "";

                    foreach (var om in c.OuputMessages)
                    {
                        if (om.MessageContentType == "tool_use")
                        {
                            FunctionCall fc = new FunctionCall { Id = om.Id, Name = om.Name, Arguments = om.Input };
                            var result = ExecuteFunctionCall(fc);

                            _ThreadToolsResults.Add(new ThreadToolCallResult(fc.Name, result));

                            ActiveRequest.FunctionToolReply(fc.Id, result);

                            _LLMLogger?.LogInformation($"Function {fc.Name}({fc.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;
                            toolname += fc.Name;
                        }
                    }

                    if (LogEvents)
                    {
                        // IMPORTANT
                        cllog.Flush();          // push text into MemoryStream
                        mlog.Position = 0;      // rewind stream

                        // Write MemoryStream to file
                        string filename = "claude_events_log.txt";

                        if (IsToolReplyPending) filename = $"claude_events_log_{toolname}.txt";

                        using (var file = File.Create(filename))
                        {
                            mlog.CopyTo(file);
                        }
                    }


                    cllog.Dispose();
                    mlog.Dispose();

                    string reasoning = c.MessageThinking;
                    _LastReply = c.MessageContent;




                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send assistant reply to channel
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
                    foreach (var msg in BufferedRequests.InputMessages)
                        ActiveRequest.InputMessages.Add(msg);

                    BufferedRequests.InputMessages.Clear();
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

                    ClaudeThreadResponse c;
                    if (LogResponses)
                    {
                        var tt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        using (var cllog = new StreamWriter("claude_log.txt", false))
                        {
                            cllog.Write(tt);
                        }
                        c = JsonSerializer.Deserialize<ClaudeThreadResponse>(tt);

                    }
                    else
                    {
                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        c = await JsonSerializer.DeserializeAsync<ClaudeThreadResponse>(stream).ConfigureAwait(false);
                    }

                    if (c == null || c.OuputMessages == null || c.OuputMessages.Count == 0)
                    {
                        IsToolReplyPending = false;
                        return;
                    }

                    ActiveRequest.AssistantMessages(c.OuputMessages!);

                    IsToolReplyPending = false;

                    foreach (var om in c.OuputMessages)
                    {
                        if (om.MessageContentType == "tool_use")
                        {
                            FunctionCall fc = new FunctionCall { Id = om.Id, Name = om.Name, Arguments = om.Input };
                            var result = ExecuteFunctionCall(fc);

                            ActiveRequest.FunctionToolReply(fc.Id, result);

                            _LLMLogger?.LogInformation($"Function {fc.Name}({fc.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;
                        }
                    }

                    string reasoning = c.MessageThinking;
                    _LastReply = c.MessageContent;




                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send assistant reply to channel
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
                    foreach (var msg in BufferedRequests.InputMessages)
                        ActiveRequest.InputMessages.Add(msg);

                    BufferedRequests.InputMessages.Clear();
                    OnGoingCall = false;
                }
            }).ConfigureAwait(false);

        }

    }

}