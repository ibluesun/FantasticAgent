using FantasticAgent.Base;
using FantasticAgent.Claude;
using FantasticAgent.GPT;
using FantasticAgent.Tools;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FantasticAgent.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string? GPTSecret = Environment.GetEnvironmentVariable("GPTSecret");
        static string? ClaudeSecret = Environment.GetEnvironmentVariable("ClaudeSecret");


        ClaudeThread claude;
        GPTThread gpt;
        OllamaThread ollama;


        string OllamaModel = "qwen3";

        private bool OllamaExists(string url = "http://localhost:11434")
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set a short timeout (e.g., 2 seconds) so the UI doesn't freeze if it's down
                    client.Timeout = System.TimeSpan.FromSeconds(2);

                    // Ollama's root URL returns "Ollama is running"
                    var response =  client.GetAsync(url).GetAwaiter().GetResult();

                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                // If connection fails, timeout, or DNS error -> It's not running
                return false;
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            DotNetEnv.Env.Load();


            PrepareLLMs();



        }

        private async void ConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the user pressed Enter
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                string command = textBox.Text;

                // 1. Process the command here
                await ProcessCommand(command);

                // 2. Clear the line for the next input
                textBox.Clear();

                // 3. Mark event as handled to prevent the "Ding" sound or new line
                e.Handled = true;
            }
        }


        private void ConsoleWrapper_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ConsoleInput.Focus();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                btnMaximize.Content = "[=]"; // Symbol for "Restore" (two layers)
            }
            else
            {
                btnMaximize.Content = "[ ]"; // Symbol for "Maximize" (one big box)
            }
        }


        void PrepareClaude()
        {
            if (!string.IsNullOrEmpty(ClaudeSecret))
            {
                claude = new ClaudeThread(ClaudeSecret, "claude-sonnet-4-5", "You are a helpful assistant.");
                claude.ActiveRequest.Reasoning = new ClaudeReasoning();
                claude.DeclareFunctionTool(typeof(WebSearchProviders).GetMethod("BraveSearch"));



                ClaudeThreadUC.ActiveLLMThread = claude;


            }
            else
            {
                ClaudeThreadUC.Warning("[ClaudeSecret] Environment Variable is not Present.");
            }

        }

        void PrepareGPT()
        {
            if (!string.IsNullOrEmpty(GPTSecret))
            {
                gpt = new GPTThread(GPTSecret, "gpt-5-nano", "You are a helpful assistant.");
                gpt.ActiveRequest.Reasoning = new GPTReasoning() { Summary = ReasoningSummary.Auto };
                gpt.DeclareFunctionTool(typeof(WebSearchProviders).GetMethod("BraveSearch"));


                GptThreadUC.ActiveLLMThread = gpt;


            }
            else 
            {
                GptThreadUC.Warning("[GPTSecret] Environment Variable is not Present.");

            }

        }

        void PrepareOllama()
        {
            var ollamaExists = OllamaExists();

            if (ollamaExists)
            {
                ollama = new OllamaThread("localhost", 11434, OllamaModel, "You are a helpful assistant.");
                ollama.DeclareFunctionTool(typeof(WebSearchProviders).GetMethod("BraveSearch"));


                OllamaThreadUC.ActiveLLMThread = ollama;


            }
            else
            {
                OllamaThreadUC.Warning("Ollama is not running locally in this device.");
            }

        }

        private void PrepareLLMs()
        {

            PrepareClaude();
            PrepareGPT();
            PrepareOllama();

        }



        private async Task ProcessOllama(string text)
        {
            if (ollama != null)
            {
                OllamaThreadUC.IsBusy = true;
                OllamaThreadUC.UserInput(text);
                ollama.UserMessage(text);

                ollama.UserMessage(text);
                await ollama.SendToLLMThread();


                while (ollama.IsToolReplyPending)
                {
                    // send replies now
                    await ollama.SendToLLMThread();
                }
                OllamaThreadUC.IsBusy = false;
            }
        }

        private async Task ProcessClaude(string text)
        {
            if (claude != null)
            {
                ClaudeThreadUC.IsBusy = true;
                ClaudeThreadUC.UserInput(text);
                claude.UserMessage(text);

                claude.UserMessage(text);
                await claude.SendToLLMThread();


                while (claude.IsToolReplyPending)
                {
                    // send replies now
                    await claude.SendToLLMThread();
                }
                ClaudeThreadUC.IsBusy = false;
            }
        }

        private async Task ProcessGPT(string text)
        {
            if (gpt != null)
            {
                GptThreadUC.IsBusy = true;

                GptThreadUC.UserInput(text);
                gpt.UserMessage(text);

                gpt.UserMessage(text);
                await gpt.SendToLLMThread();


                while (gpt.IsToolReplyPending)
                {
                    // send replies now
                    await gpt.SendToLLMThread();
                }

                GptThreadUC.IsBusy = false;
            }
        }


        private async Task ProcessCommand(string command)
        {

            var o = ProcessOllama(command);
            var c = ProcessClaude(command);
            var g = ProcessGPT(command);


        }






    }
}