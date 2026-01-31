using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Base
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReasoningType
    {
        [JsonStringEnumMemberName("disabled")]
        Disabled,

        [JsonStringEnumMemberName("enabled")]
        Enabled,

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReasoningEffortLevel
    {
        [JsonStringEnumMemberName("none")]
        None,

        [JsonStringEnumMemberName("low")]
        Low,

        [JsonStringEnumMemberName("medium")]
        Medium,

        [JsonStringEnumMemberName("high")]
        High,

        [JsonStringEnumMemberName("max")]
        Max
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReasoningSummary
    {
        [JsonStringEnumMemberName("auto")]
        Auto,

        [JsonStringEnumMemberName("concise")]
        Concise,

        [JsonStringEnumMemberName("detailed")]
        Detailed,


    }

}
