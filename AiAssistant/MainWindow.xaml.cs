using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AiAssistant.AI;
using AiAssistant.ConvertManagement;
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

        public bool ExitAIAssistance = false;
        public UnitPipe Pipe = new UnitPipe();

        public Thread ExecuteTrd = null;
        public void AIAssistance(string Input)
        {
            if (ExecuteTrd == null)
            {
                ExecuteTrd = new Thread(() =>
                {
                    ExitAIAssistance = false;
                    ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                    {
                        ExecuteBtn.Content = "Executing";
                    }));
                    try
                    {
                        ClearLog();
                        string UserInput = Input;
                        string Prompt = Pipe.BuildUserPrompt(UserInput);
                        SetLog("Prompt", Prompt);

                        int RetryCount = 0;
                        const int MaxRetry = 10;

                        do
                        {
                            string AiReply = "";
                            if (AICenter.Gemini != null)
                            {
                                AiReply = AICenter.Gemini.QueryAI(Prompt);
                                SetLog("Gemini", AiReply);
                            }
                            else if (AICenter.ChatGpt != null)
                            {
                                AiReply = AICenter.ChatGpt.QueryAI(Prompt);
                                SetLog("ChatGpt", AiReply);
                            }
                            else if (AICenter.LocalAI != null)
                            {
                                AiReply = AICenter.LocalAI.QueryAI(Prompt);
                                SetLog("LocalAI", AiReply);
                            }

                            if (AiReply == "")
                            {
                                ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                                {
                                    ExecuteBtn.Content = "Execute";
                                }));
                                ExecuteTrd = null;

                                SetLog("Cancel", "The AI ​​interrupted the operation.");

                                return;
                            }

                            ExecutionResult Result = Pipe.AnalysisAndExecuteCapabilities(AiReply);
                            SetLog("ExecutionResult", JsonConvert.SerializeObject(Result));

                            if (!Result.Continue)
                            {
                                SetLog("Complete", ConvertHelper.ObjToStr(Result.ReturnValue));
                                //MessageBox.Show(ConvertHelper.ObjToStr(Result.ReturnValue));
                                break;
                            }

                            if (Result.Status == "Failure")
                            {
                                if (ExitAIAssistance)
                                {
                                    SetLog("End", "");
                                    ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                                    {
                                        ExecuteBtn.Content = "Execute";
                                    }));
                                    ExecuteTrd = null;
                                    return;
                                }

                                RetryCount++;
                                SetLog("RetryCount", $"{RetryCount} / {MaxRetry}");

                                if (RetryCount >= MaxRetry)
                                {
                                    SetLog("RetryExceeded", $"Reached max retries ({MaxRetry}), task aborted.");
                                    MessageBox.Show($"Task failed after {MaxRetry} retries.\n\nLast error: {Result.ErrorMessage}");
                                    break;
                                }

                                Prompt = Pipe.BuildErrorRetryPrompt(UserInput, Result);
                                SetLog("ErrorRetryPrompt", Prompt);
                            }
                            else
                            {
                                RetryCount = 0;
                                Prompt = Pipe.BuildResultPrompt(UserInput, Result);
                                SetLog("RePrompt", Prompt);
                            }

                        } while (true);
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show(Ex.Message);
                    }

                    ExecuteBtn.Dispatcher.Invoke(new Action(() =>
                    {
                        ExecuteBtn.Content = "Execute";
                    }));
                    ExecuteTrd = null;
                });
                ExecuteTrd.Start();
            }
        }

        private void CallAI(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AIAssistance(InputBox.Text);
        }
        public void ClearLog()
        {
            Log.Dispatcher.Invoke(new Action(() => {
                Log.Text = string.Empty;
            }));
        }

        public void SetLog(string StepName,string OneLog)
        {
            Log.Dispatcher.Invoke(new Action(() => {
                Log.Text += StepName + "->\r\n" + OneLog + "\r\n" + "\r\n";
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => Log.ScrollToEnd()), DispatcherPriority.Loaded);
            }));
        }

        public void SyncSandBox()
        {
            if (CSandbox.IsChecked == true)
            {
                Sandbox.CheckSafeFunc += new CheckSafe((Func, Args) =>
                {
                    SandBoxView NSandBoxView = null;

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        NSandBoxView = new SandBoxView();

                        var ProcessedArgs = Args.Select(Arg =>
                        {
                            if (Arg is string S)
                            {
                                return S
                                    .Replace("\\r\\n", "\r\n")
                                    .Replace("\\n", "\n")
                                    .Replace("\\r", "\r");
                            }
                            return Arg;
                        }).ToList();

                        string CreateCommand = "Function:" + Func.ToString() + "\r\n" + "Args:" + JsonConvert.SerializeObject(ProcessedArgs, Formatting.Indented);
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
        public void SyncConfig()
        {
            if (AICenter.LocalSetting.EnableCMDUnit)
            {
                Pipe.CmdUnit.Enable = true;
            }
            if (AICenter.LocalSetting.EnableCSharpCodeUnit)
            {
                Pipe.CSharpUnit.Enable = true;
            }
            if (AICenter.LocalSetting.EnableIOUnit)
            {
                Pipe.IoUnit.Enable = true;
            }
            if (AICenter.LocalSetting.EnableMouseUnit)
            {
                Pipe.MouseUnit.Enable = true;
            }
            if (AICenter.LocalSetting.EnableRequestUnit)
            {
                Pipe.RequestUnit.Enable = true;
            }
            if (AICenter.LocalSetting.EnableWinApiUnit)
            {
                Pipe.WinApiUnit.Enable = true;
            }

            SyncSandBox();
            SyncUnitConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DeFine.Init(this);
            SyncConfig();

            this.Title = string.Format("Ai Assistant - {0}", DeFine.Version);
        }

        private void CSandbox_Click(object sender, RoutedEventArgs e)
        {
            SyncSandBox();
        }

        public void SyncUnitConfig()
        {
            foreach (var GetUnit in Units.Children)
            {
                if (GetUnit is Border)
                {
                    if ((GetUnit as Border).Child is StackPanel)
                    {
                        StackPanel GetPanel = (GetUnit as Border).Child as StackPanel;

                        if (GetPanel.Children[0] is Ellipse)
                        {
                            if (GetPanel.Children[1] is Label)
                            {
                                Ellipse StateLight = GetPanel.Children[0] as Ellipse;
                                string GetUnitName = ConvertHelper.ObjToStr((GetPanel.Children[1] as Label).Content);

                                switch (GetUnitName)
                                {
                                    case "CMDUnit":
                                        {
                                            if (Pipe.CmdUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                    case "CSharpUnit":
                                        {
                                            if (Pipe.CSharpUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                    case "IOUnit":
                                        {
                                            if (Pipe.IoUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                    case "MouseUnit":
                                        {
                                            if (Pipe.MouseUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                    case "RequestUnit":
                                        {
                                            if (Pipe.RequestUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                    case "WinApiUnit":
                                        {
                                            if (Pipe.WinApiUnit.Enable)
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                            }
                                            else
                                            {
                                                StateLight.Fill = new SolidColorBrush(Colors.Black);
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }      
            }
        }
       

        private void UnitClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border)
            {
                Border MainBorder = (Border)sender;

                StackPanel GetPanel = MainBorder.Child as StackPanel;

                if (GetPanel.Children[0] is Ellipse)
                {
                    if (GetPanel.Children[1] is Label)
                    {
                        Ellipse StateLight = GetPanel.Children[0] as Ellipse;
                        string GetUnitName = ConvertHelper.ObjToStr((GetPanel.Children[1] as Label).Content);

                        switch (GetUnitName)
                        {
                            case "CMDUnit":
                                {
                                    if (Pipe.CmdUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.CmdUnit.Enable = false;
                                        AICenter.LocalSetting.EnableCMDUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.CmdUnit.Enable = true;
                                        AICenter.LocalSetting.EnableCMDUnit = true;
                                    }
                                }
                            break;
                            case "CSharpUnit":
                                {
                                    if (Pipe.CSharpUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.CSharpUnit.Enable = false;
                                        AICenter.LocalSetting.EnableCSharpCodeUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.CSharpUnit.Enable = true;
                                        AICenter.LocalSetting.EnableCSharpCodeUnit = true;
                                    }
                                }
                            break;
                            case "IOUnit":
                                {
                                    if (Pipe.IoUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.IoUnit.Enable = false;
                                        AICenter.LocalSetting.EnableIOUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.IoUnit.Enable = true;
                                        AICenter.LocalSetting.EnableIOUnit = true;
                                    }
                                }
                            break;
                            case "MouseUnit":
                                {
                                    if (Pipe.MouseUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.MouseUnit.Enable = false;
                                        AICenter.LocalSetting.EnableMouseUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.MouseUnit.Enable = true;
                                        AICenter.LocalSetting.EnableMouseUnit = true;
                                    }
                                }
                            break;
                            case "RequestUnit":
                                {
                                    if (Pipe.RequestUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.RequestUnit.Enable = false;
                                        AICenter.LocalSetting.EnableRequestUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.RequestUnit.Enable = true;
                                        AICenter.LocalSetting.EnableRequestUnit = true;
                                    }
                                }
                            break;
                            case "WinApiUnit":
                                {
                                    if (Pipe.WinApiUnit.Enable)
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Black);
                                        Pipe.WinApiUnit.Enable = false;
                                        AICenter.LocalSetting.EnableWinApiUnit = false;
                                    }
                                    else
                                    {
                                        StateLight.Fill = new SolidColorBrush(Colors.Blue);
                                        Pipe.WinApiUnit.Enable = true;
                                        AICenter.LocalSetting.EnableWinApiUnit = true;
                                    }
                                }
                            break;
                        }
                    }
                }
            }
        }

        private void ShowAIConfig(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            new AIConfig().Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DeFine.CloseAny();
        }
    }
}
