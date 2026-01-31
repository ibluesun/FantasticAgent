using FantasticAgent.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Claude.Tools
{

    public class ClaudeFunctionToolDefinition : ToolDefinition
    {
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("input_schema")]
        public ObjectDefinition ParametersSchema { get; set; }

        public ClaudeFunctionToolDefinition(string name, string description, ObjectDefinition parametersSchema)
        {
            Name = name;
            Description = description;
            ParametersSchema = parametersSchema;
        }


        [JsonIgnore]
        public MethodInfo UnderlyingMethod { get; internal set; }

    }




}
