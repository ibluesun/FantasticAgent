using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent
{
    public class HuggingFaceThread : GPTThread
    {

        public HuggingFaceThread(string secretKey, string model, string systemRole)
            : base("https://router.huggingface.co/v1/responses", secretKey, model, systemRole)
        {

        }


        protected override bool SupportsReasoningItems => false;

    }
}
