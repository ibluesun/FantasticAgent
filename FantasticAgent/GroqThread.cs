using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent
{
    public class GroqThread : GPTThread
    {
        public GroqThread(string secretKey, string model, string systemRole) 
            : base("https://api.groq.com/openai/v1/responses", secretKey, model, systemRole)
        {

        }


        public override string ProviderName => "Groq";

    }
}
