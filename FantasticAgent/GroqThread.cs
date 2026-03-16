using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent
{
    public class GroqThread : GPTThread
    {
        public GroqThread(string secretKey, string gptModel, string systemRole) 
            : base("https://api.groq.com/openai/v1/responses", secretKey, gptModel, systemRole)
        {

        }
    }
}
