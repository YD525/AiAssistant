using System;
using System.Windows;
using AiAssistant.ExecuteSandbox;
using AiAssistant.ExecuteUnit;
using AiAssistant.Platform;
using static AiAssistant.ExecuteSandbox.Sandbox;

namespace AiAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public LMStudio LocalAI = new LMStudio();
        public MainWindow()
        {
            InitializeComponent();

            //AIAssistance("Please check today's weather for me.");
        }


        public void AIAssistance(string Input)
        {
            var Pipe = new UnitPipe();
            string UserInput = Input;
            string Prompt = Pipe.BuildUserPrompt(UserInput);
            do
            {
                string AiReply = LocalAI.QueryAI(UserInput);

                ExecutionResult Result = Pipe.AnalysisAndExecuteCapabilities(AiReply);
                if (!Result.Continue)
                {
                    Console.WriteLine(Result.ReturnValue);
                    break;
                }
                Prompt = Pipe.BuildResultPrompt(UserInput, Result);
            } while (true);
        }

        private void CallAI(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AIAssistance(InputBox.Text);
        }

        public void SyncConfig()
        {
            if (CSandbox.IsChecked == true)
            {
                Sandbox.CheckSafeFunc += new CheckSafe((Func,Args) => 
                {
                    return SafeResult.Deny("");
                });
            }
            else
            {
                Sandbox.CheckSafeFunc = null;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncConfig();
        }

        private void CSandbox_Click(object sender, RoutedEventArgs e)
        {
            SyncConfig();
        }
    }
}
