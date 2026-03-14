
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Base
{
    public interface ILLMEvaluator
    {

        bool LogTurns { get; set; }

        bool LogEvents { get; set; }

        Task ConsoleStreamRun();

        Task ConsoleRun();
    }
}
