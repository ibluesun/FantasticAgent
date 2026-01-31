using FantasticAgent.Attributes;
using FantasticAgent.Base;
using FantasticAgent.GPT;
using FantasticAgent.GPT.Tools;
using FantasticAgent.Ollama;
using FantasticAgent.Ollama.Tools;
using FantasticAgent.Tools;

using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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



    public class GPTThread : LLMThread<GPTThreadRequest, GPTThreadResponse, GPTTurnMessage>
    {
        public GPTThread(string secretKey, string gptModel, string systemRole) : base("https://api.openai.com/v1/responses", gptModel, systemRole)
        {


            LLMHttpThreadClient.DefaultRequestHeaders.Add("Accept", $"application/json");
            LLMHttpThreadClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");


        }





        public override GPTTurnMessage LastTurnMessage => ActiveRequest.InputMessages.Last();



        private string AssistantReplyFromThreadResponse(ICollection<GPTThreadResponse> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (GPTThreadResponse tr in replies)
            {
                if (tr.OuputMessages != null)
                {
                    foreach (var msg in tr.OuputMessages)
                    {
                        if (msg.Contents != null)
                        {
                            foreach (var content in msg.Contents)
                            {
                                sb.Append(content.Text);
                            }
                        }

                    }
                }

            }
            return sb.ToString();
        }

        private string ReasoningFromThreadResponse(ICollection<GPTThreadResponse> replies)
        {
            StringBuilder sb = new StringBuilder();

            foreach (GPTThreadResponse tr in replies)
            {
                if (tr.OuputMessages != null)
                {
                    foreach (var msg in tr.OuputMessages)
                    {
                        if (msg.Summaries != null)
                        {
                            foreach (var summary in msg.Summaries)
                            {
                                sb.Append(summary.Text);
                            }
                        }

                    }
                }

            }
            return sb.ToString();
        }



        protected Dictionary<string, GPTFunctionToolDefinition> DeclaredFunctions = new Dictionary<string, GPTFunctionToolDefinition>(StringComparer.OrdinalIgnoreCase);


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



            GPTFunctionToolDefinition f = new GPTFunctionToolDefinition(method.Name, MethodDescription, methodParameters);
            f.UnderlyingMethod = method;

            DeclaredFunctions.Add(f.Name, f);



            this.DeclareTool(f);
        }

        public string ExecuteFunctionCall(GPTTurnMessage call)
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





        public record GPTEventResponseChunk ( string type,  int sequence_number, string item_id, string delta);

        public record GPTEventResponseCompleted(string type, int sequence_number, GPTThreadResponse response);



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

                    GPTEventResponseCompleted c = null;

                    string? line;


                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        GPTEventResponseChunk cc;
                        string nextLine;
                        string payLoad;
                        switch (line)
                        {
                            case "event: response.reasoning_summary_part.added":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();

                                InvokeAssistantReasoningStarted();
                                break;

                            case "event: response.reasoning_summary_text.delta":
                                // {"type":"response.reasoning_summary_text.delta","sequence_number":452,"item_id":"rs_00fca9402f72847a00692f621c3c14819ea599984176bb46d9","output_index":0,"summary_index":3,"delta":"}","obfuscation":"yw1XOC8TxIXjaDT"}
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();

                                cc = JsonSerializer.Deserialize<GPTEventResponseChunk>(payLoad);
                                InvokeAssistantReasoningChunkReceived(cc.delta);
                                break;

                            case "event: response.reasoning_summary_part.done":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();

                                InvokeAssistantReasoningEnded();
                                break;

                            case "event: response.content_part.added":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();
                                InvokeAssistantReplyStarted();
                                break;

                            case "event: response.output_text.delta":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();
                                cc = JsonSerializer.Deserialize<GPTEventResponseChunk>(payLoad);
                                InvokeAssistantReplyChunkReceived(cc.delta);
                                break;

                            case "event: response.content_part.done":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();
                                InvokeAssistantReplyEnded();
                                break;



                            case "event: response.completed":
                                nextLine = await reader.ReadLineAsync();
                                payLoad = nextLine.Substring("data:".Length).Trim();
                                c = JsonSerializer.Deserialize<GPTEventResponseCompleted>(payLoad);

                                break;

                        }



                    }

                    string reasoning = ReasoningFromThreadResponse([c.response]);  // we don't include thinking in the payload when we send the chat thread again

                    _LastReply = AssistantReplyFromThreadResponse([c.response]);

                    if(!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }


                    foreach (var ot in c.response.OuputMessages)
                    {
                        ActiveRequest.InputMessages.Add(ot);
                    }

                    IsToolReplyPending = false;

                    foreach (var ot in c.response.OuputMessages)
                    {
                        if (ot.CallId != null)
                        {
                            var result = ExecuteFunctionCall(ot);

                            ActiveRequest.FunctionToolReply(ot.CallId, result);

                            _LLMLogger?.LogInformation($"Function {ot.Name}({ot.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;

                        }
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





                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    GPTThreadResponse c = await JsonSerializer.DeserializeAsync<GPTThreadResponse>(stream).ConfigureAwait(false);
                    //var tt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    //GPTThreadResponse c = JsonSerializer.Deserialize<GPTThreadResponse>(tt);


                    if (c == null)
                        return;

                    string reasoning = ReasoningFromThreadResponse([c]);
                    _LastReply = AssistantReplyFromThreadResponse([c]);

                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }


                    foreach (var ot in c.OuputMessages)
                    {
                        ActiveRequest.InputMessages.Add(ot);
                    }

                    IsToolReplyPending = false;

                    foreach (var ot in c.OuputMessages)
                    {
                        if (ot.CallId != null)
                        {
                            var result = ExecuteFunctionCall(ot);

                            ActiveRequest.FunctionToolReply(ot.CallId, result);

                            _LLMLogger?.LogInformation($"Function {ot.Name}({ot.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;

                        }
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