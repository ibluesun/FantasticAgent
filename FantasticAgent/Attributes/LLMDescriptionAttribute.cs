using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class LLMDescriptionAttribute : Attribute
    {
        public string Description { get; private set; }

        public LLMDescriptionAttribute(string description)
        {
            Description = description;
        }

    }
}
