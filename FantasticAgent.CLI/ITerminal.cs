using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent
{
    public enum TerminalColor
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }

    public interface ITerminal
    {

        
        TerminalColor ForegroundColor { get; set; }


        void Write(string text);


        void WriteLine(string text = "");


        void Prompt(string prompt);

        string PromptText { get; set; }

        

        string? ReadLine();
    }
}
