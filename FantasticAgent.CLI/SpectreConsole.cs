using FantasticAgent;
using FantasticAgent.Base;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Text;



namespace FantasticAgent.CLI
{

    public sealed class SpectreTerminal : ITerminal
    {

        private LiveDisplayContext? _liveCtx;

        public void AttachLiveContext(LiveDisplayContext ctx)
        {
            _liveCtx = ctx;
        }

        const int BottomSize = 5;


        readonly Layout TLayout = new Layout("Root")
            .SplitRows(
                new Layout("Top").SplitColumns(
                    new Layout("Left").Ratio(1),
                    new Layout("Middle").Ratio(1),
                    new Layout("Right").Ratio(1)
                ),
                new Layout("Bottom").Size(BottomSize) // Fixed height for prompt area
            );

        public Layout Layout => TLayout;

        readonly LineBufferWriter LeftLineBuffer = new LineBufferWriter();



        private Style _currentStyle = Style.Plain;

        public TerminalColor ForegroundColor
        {
            get => _currentColor;
            set
            {
                _currentColor = value;
                _currentStyle = value switch
                {
                    TerminalColor.DarkGray => new Style(Color.Grey),
                    TerminalColor.Yellow => new Style(Color.Yellow),
                    TerminalColor.Red => new Style(Color.Red),
                    TerminalColor.White => new Style(Color.White),
                    _ => Style.Plain
                };
            }
        }
        public string PromptText { get; set; } = "sterm";

        private TerminalColor _currentColor = TerminalColor.Default;

        public void Write(string text)
        {
            LeftLineBuffer.Write(text);


            UpdateLayout();

            //AnsiConsole.Write(new Text(text, _currentStyle));
        }

        public void WriteLine(string text = "")
        {
            LeftLineBuffer.WriteLine(text);

            UpdateLayout();

            //if (!string.IsNullOrEmpty(text))
            //    AnsiConsole.Write(new Text(text, _currentStyle));

            //AnsiConsole.WriteLine();
        }


        void UpdateLeft()
        {

            List<Text> tlines = new List<Text>();
            foreach (var l in LeftLineBuffer.Lines) tlines.Add(new Text(l));
            var rrs = new Rows(tlines);

            var leftPanel = new Panel(rrs)
                .Expand()
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Header("[bold green]Ollama[/]");

            TLayout["Left"].Update(leftPanel);
        }

        


        private volatile bool _isReadingInput;
        private string _inputBuffer = "";


        void UpdateLayout()
        {


            UpdateLeft();
            UpdateBottom();


            _liveCtx?.Refresh();
        }


        void UpdateBottom()
        {
            var content = _isReadingInput
                ? new Markup($"[yellow]> {_inputBuffer}[/]")
                : new Markup("[grey]Press Enter to type...[/]");

            var bottomPanel = new Panel(content)
                .Expand()
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow)
                .Header($"[bold yellow]User[/]");

            TLayout["Bottom"].Update(bottomPanel);
        }


        public void Prompt(string prompt)
        {
            // do nothing
        }

        public string? ReadLine()
        {
            _isReadingInput = true;
            _inputBuffer = "";

            UpdateLayout();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                    break;

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (_inputBuffer.Length > 0)
                        _inputBuffer = _inputBuffer[..^1];
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    _inputBuffer += key.KeyChar;
                }

                UpdateLayout(); // re-render bottom panel
            }

            _isReadingInput = false;

            var input = _inputBuffer;
            _inputBuffer = "";

            LeftLineBuffer.WriteLine($"User: {input}");
            UpdateLayout();

            return input;
        }


        public async Task StartAsync(ILLMEvaluator evl)
        {

            await AnsiConsole.Live(this.Layout)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    this.AttachLiveContext(ctx);

                    await evl.ConsoleStreamRun();
                });
        }

    }


}
