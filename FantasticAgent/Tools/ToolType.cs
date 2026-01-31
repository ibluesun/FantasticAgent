using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace FantasticAgent.Tools
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ToolType
    {
        Unknown = 0, 

        [JsonStringEnumMemberName("function")]
        Function,

    }
   


}
