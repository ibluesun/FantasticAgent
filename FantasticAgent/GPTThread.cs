using FantasticAgent.Attributes;
using FantasticAgent.Base;
using FantasticAgent.Gemini;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FantasticAgent
{



    public class GPTThread : LLMThread<GPTThreadRequest, GPTThreadResponse, GPTTurnMessage>
    {
        public GPTThread(string secretKey, string gptModel, string systemRole) : base("https://api.openai.com/v1/responses", gptModel, systemRole)
        {


            LLMHttpThreadClient.DefaultRequestHeaders.Add("Accept", $"application/json");
            LLMHttpThreadClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");


        }

        // Protected constructor for subclasses — accepts a custom URL
        protected GPTThread(string url, string secretKey, string gptModel, string systemRole)
            : base(url, gptModel, systemRole)
        {
            LLMHttpThreadClient.DefaultRequestHeaders.Add("Accept", "application/json");
            LLMHttpThreadClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");
        }


        protected virtual bool SupportsReasoningItems => true;

        public override string ProviderName => "ChatGPT";


        public override string[] AvailableModels => new string[] { "gpt-5-nano", "gpt-5-mini", "gpt-5.2" };

        public override GPTTurnMessage LastTurnMessage => ActiveRequest.InputMessages.Last();



        private string AssistantReplyFromThreadResponse(GPTThreadResponse tr)
        {
            StringBuilder sb = new StringBuilder();


            if (tr.OuputMessages != null)
            {
                foreach (var msg in tr.OuputMessages)
                {
                    if (msg.Contents != null)
                    {
                        foreach (var content in msg.Contents)
                        {
                            if (content.MessageContentType == "output_text" && content.Text != null)
                            {
                                sb.Append(content.Text);
                            }
                        }
                    }

                }
            }


            return sb.ToString();
        }

        private string ReasoningFromThreadResponse(GPTThreadResponse tr)
        {
            StringBuilder sb = new StringBuilder();


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


        //{"id":"fc_0950505599b6568300698242b183d481a39128342f60f4c6c9","type":"function_call","status":"in_progress","arguments":"","call_id":"call_BBYjEbFiJKrL9bSoUJSeuela","name":"GetCityCoordinates"}
        public record OutputItemEventDetails (string id, string type, string status, string arguments, string call_id, string name);

        public record OutputItemEvent ( string type, OutputItemEventDetails item, int output_index, int sequence_number);


        //{"type":"response.reasoning_summary_text.delta","delta":" \"","item_id":"rs_013e7dd7ce51acc00069b59a314c608192b615ffdb91737d98","obfuscation":"64X4RKKi48PpxX","output_index":0,"sequence_number":72,"summary_index":0}
        //{"type":"response.output_text.delta","content_index":0,"delta":" the","item_id":"msg_013e7dd7ce51acc00069b59a3951b08192ab9a75a6b40a8cb4","logprobs":[],"obfuscation":"hN8r0osUzlpv","output_index":1,"sequence_number":379}
        public record OutputItemEventDelta(string type, int content_index, string delta, string item_id, string obfuscation, int output_index, int sequence_number, int? summary_index);




        //"part":{"type":"reasoning_text","text":""}
        //"part":{"type":"output_text","annotations":[],"logprobs":[],"text":""}
        public record ContentPartEventDetails(string type, string text);


        //{"type":"response.content_part.added","content_index":0,"item_id":"msg_013e7dd7ce51acc00069b59a3951b08192ab9a75a6b40a8cb4","output_index":1,"part":{"type":"output_text","annotations":[],"logprobs":[],"text":""},"sequence_number":357}
        //{"type":"response.content_part.added","item_id":"rs_bcb7c796228b5e9b37f2d580d5b4214207d5b0995631f30d","output_index":0,"content_index":0,"part":{"type":"reasoning_text","text":""},"sequence_number":3}
        public record ContentPartEvent(string type, string item_id, ContentPartEventDetails part, int sequence_number);



        public record GPTEventResponseCompleted(string type, int sequence_number, GPTThreadResponse response);



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

                    writer.WriteBoolean("stream", true); // disable streaming explicitly
                    writer.WriteBoolean("store", false); // disable streaming explicitly

                    writer.WriteEndObject();
                }

                ms.Position = 0;
                var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");

                if (LogTurns) LogRequest(ActiveRequest.DebugView);

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

                    GPTEventResponseCompleted completedEvent = null;
                    GPTEventResponseCompleted failedEvent = null;

                    string? line;


                    /*
                     
                    it should be noted that old openai models doesn't use reasoning_summary

                    also .. should be noted that some implementations or maybe old ones .. do not emit event: [event type]:  line
                     */

                    while ((line = await reader.ReadLineAsync()) is not null)
                    {
                        if (LogStreamingEvents) LogResponseEvent(line);

                        // ✅ Detect raw error envelope 
                        if (line.StartsWith("{\"error\""))
                        {
                            var error = JsonSerializer.Deserialize<ErrorEnvelope>(line);
                            InvokeAssistantErrorReceived(error!.Error!);
                            IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                            return;
                        }


                        string? eventType = null;
                        string? payLoad = null;

                        if (line.StartsWith("event:"))
                        {
                            eventType = line.Substring("event:".Length).Trim();
                            var nextLine = await reader.ReadLineAsync();
                            if (LogStreamingEvents) LogResponseEvent(nextLine!);
                            payLoad = nextLine!.Substring("data:".Length).Trim();
                        }
                        else if (line.StartsWith("data:"))
                        {
                            payLoad = line.Substring("data:".Length).Trim();
                            // peek type from inside the JSON
                            using var doc = JsonDocument.Parse(payLoad);
                            eventType = doc.RootElement.GetProperty("type").GetString()!;
                        }


                        switch (eventType)
                        {

                            
                            case "response.reasoning_summary_part.added":
                                InvokeAssistantReasoningStarted();
                                break;



                            case "response.reasoning_text.delta":
                            case "response.reasoning_summary_text.delta":
                                var smdelta = JsonSerializer.Deserialize<OutputItemEventDelta>(payLoad!);
                                InvokeAssistantReasoningChunkReceived(smdelta!.delta);
                                break;

                            case "response.reasoning_summary_part.done":
                                InvokeAssistantReasoningEnded();
                                break;

                            


                            case "response.output_item.added":

                                var tcs = JsonSerializer.Deserialize<OutputItemEvent>(payLoad!);
                                if (tcs?.item?.type == "function_call")
                                    InvokeToolCallingStarted(tcs.item.name);
                                break;

                            case "response.function_call_arguments.delta":
                                InvokeToolCallingParameterChunkReceived(JsonSerializer.Deserialize<OutputItemEventDelta>(payLoad!)!.delta);
                                break;

                            case "response.output_item.done":
                                var tce = JsonSerializer.Deserialize<OutputItemEvent>(payLoad!);
                                if (tce?.item?.type == "function_call")
                                    InvokeToolCallingEnded();
                                break;



                            case "response.content_part.added":
                                var prtadded = JsonSerializer.Deserialize<ContentPartEvent>(payLoad!);
                                if (prtadded!.part.type == "reasoning_text")
                                    InvokeAssistantReasoningStarted();
                                else if (prtadded!.part.type == "output_text")
                                    InvokeAssistantReplyStarted();
                                else
                                {
                                    // unknown
                                }
                                break;

                            case "response.output_text.delta":
                                var otdelta = JsonSerializer.Deserialize<OutputItemEventDelta>(payLoad!);
                                InvokeAssistantReplyChunkReceived(otdelta!.delta);
                                break;

                            case "response.content_part.done":
                                var prtdone = JsonSerializer.Deserialize<ContentPartEvent>(payLoad!);
                                if (prtdone!.part.type == "reasoning_text")
                                    InvokeAssistantReasoningEnded();
                                else if (prtdone!.part.type == "output_text")
                                    InvokeAssistantReplyEnded();
                                else
                                {
                                    // unknown
                                }
                                InvokeAssistantReplyEnded();
                                break;





                            case "response.failed":
                                // we need to attempt the request again  for three times for example
                                failedEvent = JsonSerializer.Deserialize<GPTEventResponseCompleted>(payLoad!)!;
                                break;






                            case "response.completed":
                                completedEvent = JsonSerializer.Deserialize<GPTEventResponseCompleted>(payLoad!)!;

                                break;

                        }

                       


                    }


                    if (LogStreamingEvents) LogEventsFinishedFile();


                    if (failedEvent != null)
                    {
                        InvokeAssistantErrorReceived(failedEvent.response!.Error!);
                        IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                        return;
                    }

                    string reasoning = ReasoningFromThreadResponse(completedEvent!.response);  // we don't include thinking in the payload when we send the chat thread again

                    _LastReply = AssistantReplyFromThreadResponse(completedEvent.response);

                    if(!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }


                    foreach (var ot in completedEvent.response.OuputMessages)
                    {
                        if (SupportsReasoningItems == false && ot.MessageType == "reasoning") continue;
                        ot.MessageId = null;
                        ot.MessageStatus = null;
                        ActiveRequest.InputMessages.Add(ot);
                    }

                    IsToolReplyPending = false;

                    string toolname = "";

                    foreach (var ot in completedEvent.response.OuputMessages)
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

                            _ThreadToolsResults.Add(new ThreadToolCallResult(ot.Name, result));

                            ActiveRequest.FunctionToolReply(ot.CallId, result);

                            _LLMLogger?.LogInformation($"Function {ot.Name}({ot.Arguments.ToString()}) executed with result: {result}");

                            IsToolReplyPending = true;

                            toolname += ot.Name;

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
                        writer.WriteBoolean("store", false); // disable streaming explicitly
                        writer.WriteEndObject();
                    }

                    ms.Position = 0;
                    var content = new StringContent(Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8, "application/json");


                    var request = new HttpRequestMessage(HttpMethod.Post, ServerUri)
                    {
                        Content = content
                    };

                    if (LogTurns) LogRequest(ActiveRequest.DebugView);
                    using var response = await LLMHttpThreadClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode == false)
                    {
                        var err = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        LogResponseError(response.StatusCode, err);

                        var error = JsonSerializer.Deserialize<ErrorEnvelope>(err);


                        InvokeAssistantErrorReceived(error!.Error!);
                        IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                        return;

                    }


                    GPTThreadResponse c;
                    if (LogTurns)
                    {
                        var tt = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        LogResponse(tt);

                        c = JsonSerializer.Deserialize<GPTThreadResponse>(tt);

                    }
                    else
                    {


                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        c = await JsonSerializer.DeserializeAsync<GPTThreadResponse>(stream).ConfigureAwait(false);
                    }

                    if (c == null)
                        return;

                    if (c.Error != null)
                    {
                        InvokeAssistantErrorReceived(c.Error!);
                        IsToolReplyPending = false;  // false for now .. but we can make a mechanism to call again with attempts later
                        return;
                    }


                    string reasoning = ReasoningFromThreadResponse(c);
                    _LastReply = AssistantReplyFromThreadResponse(c);

                    if (!string.IsNullOrEmpty(_LastReply))
                    {
                        // Send the assistant reply to channel.
                        await _AssistantReplies.Writer.WriteAsync(_LastReply);
                    }


                    foreach (var ot in c.OuputMessages!)
                    {
                        if (SupportsReasoningItems == false && ot.MessageType == "reasoning") continue;
                        ot.MessageId = null;
                        ot.MessageStatus = null;
                        ActiveRequest.InputMessages.Add(ot);
                    }

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