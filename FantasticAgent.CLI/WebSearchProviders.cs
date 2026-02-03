using FantasticAgent.Attributes;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace FantasticAgent.CLI
{
    public static class WebSearchProviders
    {
        static readonly HttpClient _http = new HttpClient();


        [LLMDescription("Web Search through duck duck go.")]
        public static JsonNode DuckDuckSearch(string query)
        {
            var url =
                "https://api.duckduckgo.com/?" +
                $"q={Uri.EscapeDataString(query)}" +
                "&format=json&no_html=1&skip_disambig=1";

            var result = _http.GetStringAsync(url).GetAwaiter().GetResult();
            return JsonNode.Parse(result);
            
        }

        static string _BraveApiKey;

        static WebSearchProviders()
        {
            _BraveApiKey = Environment.GetEnvironmentVariable("BraveApiKey");
        }


        public static string BraveApiKey => _BraveApiKey;


        public static JsonNode BraveSearch(string query)
        {
            using var client = new HttpClient();

            // 1. Set required headers
            client.DefaultRequestHeaders.Add("X-Subscription-Token", BraveApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 2. Build the URL with parameters
            // 'count' limits results (max 20), 'safesearch' is optional
            string baseUrl = "https://api.search.brave.com/res/v1/web/search";
            string url = $"{baseUrl}?q={Uri.EscapeDataString(query)}&count=10";

            try
            {
                // 3. Make the request
                HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                // 4. Return as raw JSON string
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonNode.Parse(result);

            }
            catch (HttpRequestException e)
            {
                return JsonNode.Parse($"{{\"error\": \"Request failed: {e.Message}\"}}");
            }
        }

    }
}
