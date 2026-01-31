using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Base
{
    public class LLMException : Exception
    {
        public LLMException(string? message) : base(message)
        {
        }
    }




    public class LLMUnSupportedFeatureException : LLMException
    {
        public LLMUnSupportedFeatureException(string? message) : base(message)
        {
        }
    }


    public class LLMUnknownEventException : LLMException
    {
        public LLMUnknownEventException(string? message) : base(message)
        {
        }
    }


}
