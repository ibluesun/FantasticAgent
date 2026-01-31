using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent
{
    public enum TerminalColor
    {
        Default,
        White,
        Yellow,
        DarkGray,
        Red
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
