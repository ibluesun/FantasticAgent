using FantasticAgent.Base;
using FantasticAgent.Claude;
using FantasticAgent.GPT;
using FantasticAgent.Tools;
using System.Globalization;
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





        private async Task ProcessUserMessage(string message)
        {
            OllamaThreadUC.ProcessUserMessage(message);
            ClaudeThreadUC.ProcessUserMessage(message);
            GptThreadUC.ProcessUserMessage(message);



        }

        private async void ConsoleInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Check if Shift is NOT pressed
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    // Mark as handled so the TextBox doesn't add an extra newline
                    e.Handled = true;

                    var textBox = sender as TextBox;
                    string userMessage = textBox.Text;

                    if (!string.IsNullOrWhiteSpace(userMessage))
                    {
                        // 1. Process the multi-line command
                        await ProcessUserMessage(userMessage);

                        // 2. Clear for the next input
                        textBox.Clear();
                    }
                }
                // If Shift IS pressed, we do nothing. 
                // AcceptsReturn="True" will handle adding the newline for us.
            }
        }

        private void ConsoleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // This forces the scrollbar to the very bottom
                //textBox.ScrollToEnd();
            }
        }
    }




    public class PercentageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double actualHeight && parameter != null)
                {
                    if (double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double percentage))
                    {
                        return actualHeight * percentage;
                    }
                }
                return double.NaN;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    
}