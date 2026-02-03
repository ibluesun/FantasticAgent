using FantasticAgent;
using FantasticAgent.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasticAgent.CLI
{
    public static class WeatherTools
    {
        public record Coordinates(double? Latitude, double? Longitude);

        public class WeatherInfo
        {
            public double? TemperatureC { get; set; }
            public int Humidity { get; set; }
            public double WindKph { get; set; }
            public string Condition { get; set; } = "";
        }

        private static readonly Dictionary<string, Coordinates> _coords =
            new(StringComparer.OrdinalIgnoreCase)
            {
            { "Cairo",    new Coordinates(30.0444, 31.2357) },
            { "London",   new Coordinates(51.5074, -0.1278) },
            { "New York", new Coordinates(40.7128, -74.0060) },
            { "Tokyo",    new Coordinates(35.6895, 139.6917) },
            { "Paris",    new Coordinates(48.8566, 2.3522) },
            };

        private static readonly List<(double MinLat, double MaxLat,
                                       double MinLon, double MaxLon,
                                       WeatherInfo Weather)> _weather =
            new()
            {
            (29, 31, 30, 32, new WeatherInfo { TemperatureC = 18, Humidity = 55, WindKph = 8, Condition = "Clear" }),       // Cairo
            (50, 53, -3, 1, new WeatherInfo { TemperatureC = 7,  Humidity = 80, WindKph = 12, Condition = "Cloudy" }),      // London
            (40, 42, -76, -72, new WeatherInfo { TemperatureC = 10, Humidity = 60, WindKph = 15, Condition = "Windy" }),    // New York
            (34, 37, 138, 142, new WeatherInfo { TemperatureC = 16, Humidity = 65, WindKph = 10, Condition = "Rainy" }),    // Tokyo
            (48, 50, 1, 4, new WeatherInfo { TemperatureC = 12, Humidity = 70, WindKph = 9,  Condition = "Partly cloudy" }) // Paris
            };


        [LLMDescription("Gets the geographical coordinates (latitude and longitude) for a given city.")]
        public static Coordinates GetCityCoordinates(
            [LLMDescription("The name of the city to get coordinates for without country or any additives.")]
            string city
            )
        {
            if (_coords.TryGetValue(city, out var c))
                return c;

            return new Coordinates(null, null);
        }

        public static WeatherInfo GetWeatherAtCoordinates(double latitude, double longitude)
        {
            foreach (var entry in _weather)
            {
                if (latitude >= entry.MinLat && latitude <= entry.MaxLat &&
                    longitude >= entry.MinLon && longitude <= entry.MaxLon)
                {
                    return entry.Weather;
                }
            }

            return new WeatherInfo
            {
                TemperatureC = null,
                Humidity = 0,
                WindKph = 0,
                Condition = "Unknown"
            };
        }
    }

}
