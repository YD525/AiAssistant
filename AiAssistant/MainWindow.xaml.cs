using System;
using System.Windows;
using AiAssistant.ExecuteUnit;
using AiAssistant.Platform;

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

            AIAssistance("Please check today's weather for me.");
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
    }
}
