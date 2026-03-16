using FantasticAgent;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
            
            TerminalColor.Black => ConsoleColor.Black,
            TerminalColor.DarkBlue => ConsoleColor.DarkBlue,
            TerminalColor.DarkGreen => ConsoleColor.DarkGreen,
            TerminalColor.DarkCyan => ConsoleColor.DarkCyan,
            TerminalColor.DarkRed => ConsoleColor.DarkRed,
            TerminalColor.DarkMagenta => ConsoleColor.DarkMagenta,
            TerminalColor.DarkYellow => ConsoleColor.DarkYellow,
            TerminalColor.Gray => ConsoleColor.Gray,
            TerminalColor.DarkGray => ConsoleColor.DarkGray,
            TerminalColor.Blue => ConsoleColor.Blue,
            TerminalColor.Green => ConsoleColor.Green,
            TerminalColor.Cyan => ConsoleColor.Cyan,
            TerminalColor.Red => ConsoleColor.Red,
            TerminalColor.Magenta => ConsoleColor.Magenta,
            TerminalColor.Yellow => ConsoleColor.Yellow,
            TerminalColor.White => ConsoleColor.White,
            _ => ConsoleColor.White
        };

        private static TerminalColor FromConsoleColor(ConsoleColor color) => color switch
        {
            ConsoleColor.Black => TerminalColor.Black,
            ConsoleColor.DarkBlue => TerminalColor.DarkBlue,
            ConsoleColor.DarkGreen => TerminalColor.DarkGreen,
            ConsoleColor.DarkCyan => TerminalColor.DarkCyan,
            ConsoleColor.DarkRed => TerminalColor.DarkRed,
            ConsoleColor.DarkMagenta => TerminalColor.DarkMagenta,
            ConsoleColor.DarkYellow => TerminalColor.DarkYellow,
            ConsoleColor.Gray => TerminalColor.Gray,
            ConsoleColor.DarkGray => TerminalColor.DarkGray,
            ConsoleColor.Blue => TerminalColor.Blue,
            ConsoleColor.Green => TerminalColor.Green,
            ConsoleColor.Cyan => TerminalColor.Cyan,
            ConsoleColor.Red => TerminalColor.Red,
            ConsoleColor.Magenta => TerminalColor.Magenta,
            ConsoleColor.Yellow => TerminalColor.Yellow,
            ConsoleColor.White => TerminalColor.White,
            _ => TerminalColor.White
        };
    }
}
