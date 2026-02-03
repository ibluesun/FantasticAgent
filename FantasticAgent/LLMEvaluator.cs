using FantasticAgent.Base;
using FantasticAgent.Ollama;

using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;



namespace FantasticAgent
{

    public class LLMEvaluator<RQ, RP, TM> : ILLMEvaluator where TM : LLMTurnMessage, new() where RQ : LLMThreadRequest<TM>, new() where RP : LLMThreadResponse<TM>, new()
    {
        readonly LLMThread<RQ, RP, TM> _MainThread;

        public LLMThread<RQ, RP, TM> MainThread => _MainThread;

        readonly string Prompt;
        public string PromptText => Prompt + "[" + _MainThread.LLMModel + "]";

        readonly ITerminal _terminal;


        public bool LogEvents
        {
            get => _MainThread.LogEvents;
            set => _MainThread.LogEvents = value;
        }

        public bool LogResponses
        {
            get => _MainThread.LogResponses;
            set => _MainThread.LogResponses = value;
        }


        public LLMEvaluator(string prompt, LLMThread<RQ, RP, TM> mainThread, ITerminal terminal)
        {
            _MainThread = mainThread;
            Prompt = prompt;
            _terminal = terminal;
            _terminal.PromptText = prompt;
        }


        private void _MainThread_AssistantReplyStarted(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.ForegroundColor = TerminalColor.White;
            _terminal.WriteLine();
        }

        private void _MainThread_AssistantReplyChunkReceived(object? sender, LLMAssistantEventArgs e)
        {

            _terminal.Write(e.Message);
        }

        private void _MainThread_AssistantReplyEnded(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.WriteLine();
        }

        private void _MainThread_AssistantReasoningStarted(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.ForegroundColor = TerminalColor.DarkGray;
            _terminal.WriteLine();
        }

        private void _MainThread_AssistantReasoningChunkReceived(object? sender, LLMAssistantEventArgs e)
        {

            _terminal.Write(e.Message);

        }

        private void _MainThread_AssistantReasoningEnded(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.WriteLine();
        }

        private void _MainThread_ToolRequestStarted(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.ForegroundColor = TerminalColor.Red;
            _terminal.Write($"{e.Message}");
        }

        private void _MainThread_ToolRequestChunkReceived(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.Write(e.Message);
        }

        private void _MainThread_ToolRequestEnded(object? sender, LLMAssistantEventArgs e)
        {
            _terminal.WriteLine(e.Message);
        }




        public event EventHandler<string> ModelChangeRequested;


        void ChangeModel(string model)
        {

            
            ModelChangeRequested?.Invoke(this, model);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandLine">command</param>
        async Task<bool> ProcessCommand(string commandLine)
        {
            var zz = commandLine.Trim().Split();
            if (zz[0].ToUpperInvariant() == "MODEL")
            {
                var tt = Task.Factory.StartNew(() => ChangeModel(zz[1]));
                await tt;
                return true;
            }

            return false;
        }

        public async Task ConsoleStreamRun()
        {


            _MainThread.AssistantReasoningStarted += _MainThread_AssistantReasoningStarted;
            _MainThread.AssistantReasoningChunkReceived += _MainThread_AssistantReasoningChunkReceived;
            _MainThread.AssistantReasoningEnded += _MainThread_AssistantReasoningEnded;

            _MainThread.AssistantReplyStarted += _MainThread_AssistantReplyStarted;
            _MainThread.AssistantReplyChunkReceived += _MainThread_AssistantReplyChunkReceived;
            _MainThread.AssistantReplyEnded += _MainThread_AssistantReplyEnded;


            _MainThread.AssistantToolRequestStarted += _MainThread_ToolRequestStarted;
            _MainThread.AssistantToolRequestChunkReceived += _MainThread_ToolRequestChunkReceived;
            _MainThread.AssistantToolRequestEnded += _MainThread_ToolRequestEnded;

            _terminal.ForegroundColor = TerminalColor.White;
            _terminal.Prompt($"{PromptText}> ");

            _terminal.ForegroundColor = TerminalColor.Yellow;

            string? ll = _terminal.ReadLine();

            while (ll != null && ll.Trim().ToUpper() != "EXIT" && ll.Trim().ToUpper() != "/EXIT" && ll.Trim().ToUpper() != "/QUIT")
            {
                var um = ll.Trim();
                if (um.StartsWith('/'))
                {
                    await ProcessCommand(um.TrimStart('/'));
                }
                else if (string.IsNullOrEmpty(um) == false)
                {
                    _MainThread.UserMessage(ll);
                    await _MainThread.SendToLLMThread();

                    
                    while (_MainThread.IsToolReplyPending)
                    {
                        // send replies now
                        await _MainThread.SendToLLMThread();
                    }

                }

                _terminal.WriteLine();
                _terminal.WriteLine();
                _terminal.ForegroundColor = TerminalColor.White;
                _terminal.Prompt($"{PromptText}> ");
                _terminal.ForegroundColor = TerminalColor.Yellow;
                ll = _terminal.ReadLine();
            }

        }



        private async Task ProcessReplies()
        {
            await foreach (var message in _MainThread.AssistantReplies.ReadAllAsync())
            {

                _terminal.Write(message);
            }

        }

        public async Task ConsoleRun()
        {

            _terminal.ForegroundColor = TerminalColor.White;
            _terminal.Prompt($"{PromptText}> ");

            _terminal.ForegroundColor = TerminalColor.Yellow;
            string? ll = _terminal.ReadLine();

            while (ll != null && ll.Trim().ToUpper() != "EXIT" && ll.Trim().ToUpper() != "/EXIT" && ll.Trim().ToUpper() != "/QUIT")
            {
                var um = ll.Trim();
                if (um.StartsWith('/'))
                {
                    await ProcessCommand(um.TrimStart('/'));
                }
                else if (string.IsNullOrEmpty(um) == false)
                {
                    _MainThread.UserMessage(ll);
                    await _MainThread.SendToLLMThreadNoStream();


                    while (_MainThread.IsToolReplyPending)
                    {
                        // there is a tool reply from the queue that we need to send it the model first


                        // sometimes the model is also replying so we need to show its reply here
                        if (!string.IsNullOrEmpty(_MainThread.LastReply))
                        {
                            _terminal.ForegroundColor = TerminalColor.White;


                            _terminal.WriteLine();
                            _terminal.Write(_MainThread.LastReply);

                            _terminal.WriteLine();
                            _terminal.WriteLine();

                        }

                        // send replies now
                        await _MainThread.SendToLLMThreadNoStream();
                    }

                }

                _terminal.ForegroundColor = TerminalColor.White;

                
                _terminal.WriteLine();
                _terminal.Write(_MainThread.LastTurnMessage.MessageTextContent);

                _terminal.WriteLine();
                _terminal.WriteLine();
                _terminal.ForegroundColor = TerminalColor.White;
                _terminal.Prompt($"{PromptText}> ");

                _terminal.ForegroundColor = TerminalColor.Yellow;
                ll = _terminal.ReadLine();
            }

        }


    }
}
