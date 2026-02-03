using FantasticAgent;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.CLI
{
    public sealed class ConsoleTerminal : ITerminal
    {

        public string PromptText { get; set; } = "term";

        public TerminalColor ForegroundColor
        {
            get => FromConsoleColor(Console.ForegroundColor);
            set => Console.ForegroundColor = ToConsoleColor(value);
        }

        public void Write(string text) => Console.Write(text);

        public void WriteLine(string text = "") => Console.WriteLine(text);

        public void Prompt(string prompt) => Console.Write(prompt);

        public string? ReadLine() => Console.ReadLine();

        private static ConsoleColor ToConsoleColor(TerminalColor color) => color switch
        {
            TerminalColor.White => ConsoleColor.White,
            TerminalColor.Yellow => ConsoleColor.Yellow,
            TerminalColor.DarkGray => ConsoleColor.DarkGray,
            TerminalColor.Red => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        private static TerminalColor FromConsoleColor(ConsoleColor color) => color switch
        {
            ConsoleColor.White => TerminalColor.White,
            ConsoleColor.Yellow => TerminalColor.Yellow,
            ConsoleColor.DarkGray => TerminalColor.DarkGray,
            ConsoleColor.Red => TerminalColor.Red,
            _ => TerminalColor.Default
        };
    }
}
