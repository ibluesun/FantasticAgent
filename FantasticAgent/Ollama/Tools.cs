using FantasticAgent.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Ollama.Tools
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




    public class OllamaFunctionToolDefinition : ToolDefinition
    {

        [JsonPropertyName("type")]
        public override ToolType ToolType => ToolType.Function;


        [JsonPropertyName("function")]
        public FunctionDefinition FunctionDefinition { get; set; }


        public OllamaFunctionToolDefinition(FunctionDefinition functionDefinition)
        {
            FunctionDefinition = functionDefinition;
        }

    }


}
