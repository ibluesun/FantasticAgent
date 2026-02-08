using FantasticAgent.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static System.Net.Mime.MediaTypeNames;

namespace FantasticAgent.WPF
{
    /// <summary>
    /// Interaction logic for ChatThreadUserControl.xaml
    /// </summary>
    public partial class LLMThreadUserControl : UserControl
    {
        public LLMThreadUserControl()
        {
            InitializeComponent();


        }

        // 1. The Wrapper Property (This lets you do myControl.Title = "...")
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // 2. The Dependency Property Registration (The Magic)
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),                     // Property Name
                typeof(string),                    // Property Type
                typeof(LLMThreadUserControl),      // Owner Type
                new PropertyMetadata("TERMINAL SESSION: ACTIVE")); // Default Value



        // 1. The Wrapper Property
        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        // 2. The Dependency Property Registration
        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(
                nameof(IconBrush),
                typeof(Brush),
                typeof(LLMThreadUserControl),
                new PropertyMetadata(System.Windows.Media.Brushes.Magenta)); // Default color


        // 1. The Property Wrapper
        public int CallCount
        {
            get { return (int)GetValue(CallCountProperty); }
            set { SetValue(CallCountProperty, value); }
        }

        // 2. The Dependency Property Registration
        public static readonly DependencyProperty CallCountProperty =
            DependencyProperty.Register(
                nameof(CallCount),
                typeof(int),
                typeof(LLMThreadUserControl),
                new PropertyMetadata(0)); // Default value is 0


        //

        public double RemainingBalance
        {
            get { return (double)GetValue(RemainingBalanceProperty); }
            set { SetValue(RemainingBalanceProperty, value); }
        }

        // 2. The Dependency Property Registration
        public static readonly DependencyProperty RemainingBalanceProperty =
            DependencyProperty.Register(
                nameof(RemainingBalance),
                typeof(double),
                typeof(LLMThreadUserControl),
                new PropertyMetadata(-1.0)); // Default value is 0



        // 1. The Property Wrapper
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        // 2. The Dependency Property Registration
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                nameof(IsBusy),
                typeof(bool),
                typeof(LLMThreadUserControl),
                new PropertyMetadata(false)); // Default is false (Not busy)


        // --- AvailableModels Property ---
        public static readonly DependencyProperty AvailableModelsProperty =
            DependencyProperty.Register(nameof(AvailableModels), typeof(ObservableCollection<string>), typeof(LLMThreadUserControl),
                new PropertyMetadata(new ObservableCollection<string>()));

        public ObservableCollection<string> AvailableModels
        {
            get => (ObservableCollection<string>)GetValue(AvailableModelsProperty);
            set => SetValue(AvailableModelsProperty, value);
        }

        // --- SelectedModel Property ---
        public static readonly DependencyProperty SelectedModelProperty =
            DependencyProperty.Register(nameof(SelectedModel), typeof(string), typeof(LLMThreadUserControl),
                new PropertyMetadata(string.Empty));

        public string SelectedModel
        {
            get => (string)GetValue(SelectedModelProperty);
            set
            {
                SetValue(SelectedModelProperty, value);
            }
        }



        private ILLMThread _ActiveLLMThread;

        public ILLMThread ActiveLLMThread
        {
            get => _ActiveLLMThread;
            set
            {
                _ActiveLLMThread = value;

                Title = _ActiveLLMThread.Title;

                AvailableModels = new ObservableCollection<string>( _ActiveLLMThread.AvailableModels);

                _ActiveLLMThread.AssistantReasoningStarted += (s, e) =>
                {
                    this.NewReasoningParagraph();
                    this.NewLine();
                };

                _ActiveLLMThread.AssistantReasoningChunkReceived += (s, e) => this.WriteText(e.Message);


                _ActiveLLMThread.AssistantReplyStarted += (s, e) =>
                {
                    this.NewReplyParagraph();
                    this.NewLine();
                };

                _ActiveLLMThread.AssistantReplyChunkReceived += (s, e) => this.WriteText(e.Message);



                _ActiveLLMThread.AssistantToolRequestStarted += (s, e) =>
                {
                    this.NewToolCallParagraph();
                    this.NewLine();
                    this.WriteText(e.Message);
                };
                _ActiveLLMThread.AssistantToolRequestChunkReceived += (s, e) => this.WriteText(e.Message);
                _ActiveLLMThread.AssistantToolRequestEnded += (s, e) =>
                {
                    this.WriteLine(e.Message);
                    this.IncreaseCallsCount();

                };
            }
        }



        public async Task ProcessUserMessage(string text)
        {
            if (_ActiveLLMThread != null)
            {
                this.IsBusy = true;
                this.UserInput(text);
                _ActiveLLMThread.UserMessage(text);

                await _ActiveLLMThread.SendToLLMThread();


                while (_ActiveLLMThread.IsToolReplyPending)
                {
                    // send replies now
                    await _ActiveLLMThread.SendToLLMThread();
                }
                this.IsBusy = false;
            }
        }



        Run DefaultRun => new Run()
        {
            Foreground = Brushes.WhiteSmoke,
            FontWeight = FontWeights.Normal,
            
        };

        Run UserRun => new Run()
        {
            Foreground = Brushes.Yellow,
            FontWeight = FontWeights.Normal
        };


        Run ContentRun => new Run()
        {
            Foreground = System.Windows.Media.Brushes.LimeGreen,
            FontWeight = FontWeights.SemiBold
        };


        Run ReasoningRun => new Run()
        {
            Foreground = System.Windows.Media.Brushes.Gray
        };

        Run WarningRun => new Run()
        {
            Foreground = System.Windows.Media.Brushes.OrangeRed
        };

        Run ToolRun => new Run()
        {
            Foreground = System.Windows.Media.Brushes.Coral
        };


        public void NewParagraph(Run? run)
        {

            Dispatcher.Invoke(() =>
            {
                Run NewRun;
                if (run == null)
                    NewRun = DefaultRun;
                else
                    NewRun = run;

                OutputBox.Inlines.Add(new Run("\n"));
                OutputBox.Inlines.Add(run);

                TerminalScroll.ScrollToEnd();


            });

        }



        public void IncreaseCallsCount()
        {
            Dispatcher.Invoke(() =>
            {
                CallCount = CallCount + 1;
            });
        }

        public void WriteText(string text)
        {
            Dispatcher.Invoke(() =>
            {

                ((Run)OutputBox.Inlines.LastInline).Text += text;

                TerminalScroll.ScrollToEnd();


            });
        }

        public void NewLine()
        {
            Dispatcher.Invoke(() =>
            {
                ((Run)OutputBox.Inlines.LastInline).Text += "\n";

                TerminalScroll.ScrollToEnd();


            });
        }

        public void WriteLine(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ((Run)OutputBox.Inlines.LastInline).Text += text + "\n";

                TerminalScroll.ScrollToEnd();


            });
        }





        public void Warning(string text)
        {

            Dispatcher.Invoke(() =>
            {

                NewParagraph(WarningRun);
                WriteLine(text);
            });

        }

        public void NewReasoningParagraph()
        {
            Dispatcher.Invoke(() =>
            {
                NewParagraph(ReasoningRun);
            });
        }

        public void NewReplyParagraph()
        {
            Dispatcher.Invoke(() =>
            {
                NewParagraph(ContentRun);
            });
        }


        public void UserInput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                NewParagraph(UserRun);
                NewLine();
                WriteLine(text);
                NewLine();
            });
        }



        public void NewToolCallParagraph()
        {
            Dispatcher.Invoke(() =>
            {
                NewParagraph(ToolRun);
            });
        }


        
    }
}
