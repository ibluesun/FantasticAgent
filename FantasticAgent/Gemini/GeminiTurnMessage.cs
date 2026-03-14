using FantasticAgent.Base;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FantasticAgent.Gemini
{
    public class GeminiTurnMessage : LLMTurnMessage
    {




        [JsonPropertyName("parts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GeminiPart>? Parts { get; set; } = null;



        [JsonIgnore]
        public override string? MessageTextContent 
        {
            get
            {
                if (Parts != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var contentPart in Parts)
                    {
                        if (contentPart.Thought.HasValue)
                        {
                            if (contentPart.Thought.Value == false)
                                sb.Append(contentPart.Text);

                        }
                        else
                            sb.Append(contentPart.Text);
                    }
                    return sb.ToString();
                }
                return null;
            }
        }

        [JsonIgnore]
        public override string? MessageReasoningOrThinking
        {
            get
            {
                if (Parts != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var contentPart in Parts)
                    {
                        if (contentPart.Text != null)
                        {
                            if (contentPart.Thought.HasValue == true && contentPart.Thought == true)
                                sb.Append(contentPart.Text);
                        }
                    }
                    return sb.ToString();
                }
                return null;
            }
        }

    }
}
