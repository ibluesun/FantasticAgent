using FantasticAgent.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.GPT.Tools
{

    public class GPTFunctionToolDefinition : ToolDefinition
    {
        [JsonPropertyName("type")]
        public override ToolType ToolType => ToolType.Function;


        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public ObjectDefinition ParametersSchema { get; set; }

        public GPTFunctionToolDefinition(string name, string description, ObjectDefinition parametersSchema)
        {
            Name = name;
            Description = description;
            ParametersSchema = parametersSchema;
        }


        [JsonIgnore]
        public MethodInfo UnderlyingMethod { get; internal set; }

    }




}
