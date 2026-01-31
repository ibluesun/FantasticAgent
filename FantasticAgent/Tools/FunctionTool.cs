using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace FantasticAgent.Tools
{


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MemberDataType
    {
        [JsonStringEnumMemberName("string")]
        String,

        [JsonStringEnumMemberName("number")]
        Number,

        [JsonStringEnumMemberName("object")]
        Object

    }



    public class ObjectPropertyDescriptor
    {

        [JsonPropertyName("type")]
        public MemberDataType Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        public ObjectPropertyDescriptor(MemberDataType type, string description)
        {
            Type = type;
            Description = description;
        }

    }


    

    public class ObjectDefinition
    {
        [JsonPropertyName("type")]
        public MemberDataType Type => MemberDataType.Object;

        /// <summary>
        /// the required properties of the given <see cref="Properties"/> of this object
        /// </summary>
        [JsonPropertyName("required")]
        public List<string> Required {  get; private set; } = new List<string>();

        [JsonPropertyName("properties")]
        public Dictionary<string, ObjectPropertyDescriptor> Properties { get; private set; } = new Dictionary<string, ObjectPropertyDescriptor>();

        public void AddProperty(string name, MemberDataType type, bool required, string description)
        {
            if(required) Required.Add(name.ToLowerInvariant());

            var opd = new ObjectPropertyDescriptor(type, description);
            Properties[name.ToLowerInvariant()] = opd;
        }


        


    }
    


    


}
