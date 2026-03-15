using FantasticAgent.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini.Tools
{

    public class FunctionDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public ObjectDefinition ParametersSchema { get; set; }

        public FunctionDefinition(string name, string description, ObjectDefinition parametersSchema)
        {
            Name = name;
            Description = description;
            ParametersSchema = parametersSchema;
        }


        [JsonIgnore]
        public MethodInfo UnderlyingMethod { get; internal set; }

    }




    public class GeminiFunctionsDeclarationsToolDefinition : ToolDefinition
    {

        [JsonPropertyName("functionDeclarations")]
        public List<FunctionDefinition> FunctionDeclarations { get; set; } = new List<FunctionDefinition>();

        public void AddFunctionDefinition(FunctionDefinition fd)
        {
            FunctionDeclarations.Add(fd);
        }

    }


}
