using System;
using System.Threading;
using System.Windows;
using AiAssistant.AI;
using AiAssistant.ExecuteSandbox;
using AiAssistant.ExecuteUnit;
using AiAssistant.Platform;
using Newtonsoft.Json;
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

        public Thread ExcuteTrd = null;
        public void AIAssistance(string Input)
        {
            if (ExcuteTrd == null)
            {
                ExcuteTrd = new Thread(() =>
                {
                    ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                    {
                        ExecuteBtn.Content = "Executing";
                    }));

                    try
                    {
                        var Pipe = new UnitPipe();
                        string UserInput = Input;
                        string Prompt = Pipe.BuildUserPrompt(UserInput);
                        do
                        {
                            string AiReply = "";

                            if (AICenter.Gemini != null)
                            {
                                AiReply = AICenter.Gemini.QueryAI(UserInput);
                            }
                            else
                            if (AICenter.ChatGpt != null)
                            {
                                AiReply = AICenter.ChatGpt.QueryAI(UserInput);
                            }
                            else
                            if (AICenter.LocalAI != null)
                            {
                                AiReply = AICenter.LocalAI.QueryAI(UserInput);
                            }

                            if (AiReply == "")
                            {
                                MessageBox.Show("You have not enabled AI or there is a network error.");
                                return;
                            }

                            ExecutionResult Result = Pipe.AnalysisAndExecuteCapabilities(AiReply);

                            if (!Result.Continue)
                            {
                                Console.WriteLine(Result.ReturnValue);
                                break;
                            }
                            Prompt = Pipe.BuildResultPrompt(UserInput, Result);
                        } while (true);

                    }
                    catch(Exception Ex) 
                    {
                        MessageBox.Show(Ex.Message);
                    }

                    ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                    {
                        ExecuteBtn.Content = "Execute";
                    }));

                    ExcuteTrd = null;
                });

                ExcuteTrd.Start();
            }



        }

        private void CallAI(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AIAssistance(InputBox.Text);
        }

        public void SyncConfig()
        {
            if (CSandbox.IsChecked == true)
            {
                Sandbox.CheckSafeFunc += new CheckSafe((Func, Args) =>
                {
                    SandBoxView NSandBoxView = null;

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        NSandBoxView = new SandBoxView();
                        string CreateCommand = "Function:" + Func.ToString() + "\r\n" + "Args:" + JsonConvert.SerializeObject(Args);
                        NSandBoxView.SetCommand(CreateCommand);
                    }));

                    while (NSandBoxView.Pass == null)
                    {
                        Thread.Sleep(100);
                    }

                    if (NSandBoxView.Pass == true)
                    {
                        NSandBoxView.Pass = null;

                        return SafeResult.Ok();
                    }
                    if (NSandBoxView.Pass == false)
                    {
                        NSandBoxView.Pass = null;

                        return SafeResult.Deny("The user refused to perform this operation.");
                    }

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
            DeFine.Init(this);
            SyncConfig();
        }

        private void CSandbox_Click(object sender, RoutedEventArgs e)
        {
            SyncConfig();
        }
    }
}
