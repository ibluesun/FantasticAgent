using FantasticAgent.Base;
using FantasticAgent.Claude;
using FantasticAgent.GPT;
using FantasticAgent.Ollama;

namespace FantasticAgent.CLI
{
    internal class Program
    {

        //static ITerminal term = new SpectreTerminal();
        static ITerminal term = new ConsoleTerminal();

        //string OllamaModel = "deepseek-r1";  // no tools produce error that needs to be handled

        //string OllamaModel = "functiongemma";
        //string OllamaModel = "ministral-3";
        //string OllamaModel = "gpt-oss";
        const string OllamaModel = "qwen3";
        //const string OllamaModel = "mistral";

        static ILLMEvaluator GetOllamaEvaluator(string model = OllamaModel)
        {


            OllamaThread oth = new OllamaThread("localhost", 11434, model, "You are a helpful assistant.");

            var mm = oth.GetModelInformation();

            var ee = new LLMEvaluator<OllamaThreadRequest, OllamaThreadResponse, OllamaTurnMessage>($"Ollama", oth, term);


            ee.ModelChangeRequested += (s,e)=>
            {

                oth.ActiveModelName = e;
            };


            if (mm.Capabilities.Contains("tools"))
            {

                ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetCityCoordinates")!);
                ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetWeatherAtCoordinates")!);

                ee.MainThread.DeclareFunctionTool(typeof(WebSearchProviders).GetMethod("BraveSearch"));
            }

            return ee;
        }



        static ILLMEvaluator GetChatGPTEvaluator()
        {
            var secretKey = Environment.GetEnvironmentVariable("FMGPTSecret");

            GPTThread gpt = new GPTThread(secretKey, "gpt-5-nano", "You are a helpful assistant.");
            //GPTThread gpt = new GPTThread(secretKey, "gpt-5.1", "You are a helpful pirate assistant.");
            gpt.ActiveRequest.Reasoning = new GPTReasoning() { Summary = ReasoningSummary.Auto };

            var ee = new LLMEvaluator<GPTThreadRequest, GPTThreadResponse, GPTTurnMessage>("GPT", gpt, term);

            ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetCityCoordinates")!);
            ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetWeatherAtCoordinates")!);
                ee.MainThread.DeclareFunctionTool(typeof(WebSearchProviders).GetMethod("BraveSearch"));

            return ee;

        }

        static ILLMEvaluator GetClaudeEvaluator()
        {
            var secretKey = Environment.GetEnvironmentVariable("ClaudeSecret");



            ClaudeThread claude = new ClaudeThread(secretKey, "claude-sonnet-4-5", "You are a helpful assistant.");

            //ClaudeThread claude = new ClaudeThread(secretKey, "claude-opus-4-5", "You are a helpful assistant.");

            claude.ActiveRequest.Reasoning = new ClaudeReasoning();

            var ee = new LLMEvaluator<ClaudeThreadRequest, ClaudeThreadResponse, ClaudeTurnMessage>("Claude", claude, term);

            ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetCityCoordinates")!);
            ee.MainThread.DeclareFunctionTool(typeof(WeatherTools).GetMethod("GetWeatherAtCoordinates")!);


            return ee;

        }


        static void Main(string[] args)
        {

            DotNetEnv.Env.Load();


            var evl = GetOllamaEvaluator();
            //evl.LogEvents = true;

            var tt = evl.ConsoleStreamRun();
            tt.Wait();


            //var g = (SpectreTerminal)term;
            //g.StartAsync(evl).Wait();

        }
    }
}
