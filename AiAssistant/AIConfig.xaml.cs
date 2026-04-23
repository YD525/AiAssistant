using System.Text;
using System.Windows;
using System.Windows.Controls;
using AiAssistant.AI;
using AiAssistant.ConvertManagement;

namespace AiAssistant
{
    /// <summary>
    /// Interaction logic for AIConfig.xaml
    /// </summary>
    public partial class AIConfig : Window
    {
        public AIConfig()
        {
            InitializeComponent();
        }

        public void SyncEnableConfig()
        {
            if (AICenter.LocalSetting.EnableChatGpt)
            { 
               ChatGptCheck.IsChecked = true;
            }
            if (AICenter.LocalSetting.EnableGemini)
            {
                GeminiCheck.IsChecked = true;
            }
            if (AICenter.LocalSetting.EnableClaude)
            {
                ClaudeCheck.IsChecked = true;
            }
            if (AICenter.LocalSetting.EnableLMStudio)
            {
                LMStudioCheck.IsChecked = true;
            }

            ChatGptKey.Text = AICenter.LocalSetting.GetChatGptKey();
            GeminiKey.Text = AICenter.LocalSetting.GetGeminiKey();
            ClaudeKey.Text = AICenter.LocalSetting.GetClaudeKey();

            LMPort.Text = AICenter.LocalSetting.LMStudioPort.ToString();

            ChatGptModel.Text = AICenter.LocalSetting.ChatGptModel;
            GeminiModel.Text = AICenter.LocalSetting.GeminiModel;
            ClaudeModel.Text = AICenter.LocalSetting.ClaudeModel;
        }
        private void ChatGptCheck_Click(object sender, RoutedEventArgs e)
        {
            if (ChatGptCheck.IsChecked == true)
            {
                AICenter.LocalSetting.EnableChatGpt = true;

                ClaudeCheck.IsChecked = false;
                AICenter.LocalSetting.EnableClaude = false;
                GeminiCheck.IsChecked = false;
                AICenter.LocalSetting.EnableGemini = false;
                LMStudioCheck.IsChecked = false;
                AICenter.LocalSetting.EnableLMStudio = false;
            }

            AICenter.SyncAIConfig();
        }

        private void GeminiCheck_Click(object sender, RoutedEventArgs e)
        {
            if (GeminiCheck.IsChecked == true)
            {
                AICenter.LocalSetting.EnableGemini = true;

                ClaudeCheck.IsChecked = false;
                AICenter.LocalSetting.EnableClaude = false;
                ChatGptCheck.IsChecked = false;
                AICenter.LocalSetting.EnableChatGpt = false;
                LMStudioCheck.IsChecked = false;
                AICenter.LocalSetting.EnableLMStudio = false;
            }

            AICenter.SyncAIConfig();
        }

        private void ClaudeCheck_Click(object sender, RoutedEventArgs e)
        {
            if (ClaudeCheck.IsChecked == true)
            {
                AICenter.LocalSetting.EnableClaude = true;

                ChatGptCheck.IsChecked = false;
                AICenter.LocalSetting.EnableChatGpt = false;
                GeminiCheck.IsChecked = false;
                AICenter.LocalSetting.EnableGemini = false;
                LMStudioCheck.IsChecked = false;
                AICenter.LocalSetting.EnableLMStudio = false;
            }

            AICenter.SyncAIConfig();
        }

        private void LMStudioCheck_Click(object sender, RoutedEventArgs e)
        {
            if (LMStudioCheck.IsChecked == true)
            {
                AICenter.LocalSetting.EnableLMStudio = true;

                ClaudeCheck.IsChecked = false;
                AICenter.LocalSetting.EnableClaude = false;
                ChatGptCheck.IsChecked = false;
                AICenter.LocalSetting.EnableChatGpt = false;
                GeminiCheck.IsChecked = false;
                AICenter.LocalSetting.EnableGemini = false;
            }

            AICenter.SyncAIConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncEnableConfig();
        }

        private void ChatGptKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            { 
                AICenter.LocalSetting.ChatGptKey = Encoding.UTF8.GetBytes(ChatGptKey.Text);
            }
            catch { }

            AICenter.SyncAIConfig();
        }

        private void ChatGptModel_TextChanged(object sender, TextChangedEventArgs e)
        {
            AICenter.LocalSetting.ChatGptModel = ChatGptModel.Text;
        }

        private void GeminiModel_TextChanged(object sender, TextChangedEventArgs e)
        {
            AICenter.LocalSetting.GeminiModel = GeminiModel.Text;
        }

        private void GeminiKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                AICenter.LocalSetting.GeminiKey = Encoding.UTF8.GetBytes(GeminiKey.Text);
            }
            catch { }

            AICenter.SyncAIConfig();
        }

        private void ClaudeModel_TextChanged(object sender, TextChangedEventArgs e)
        {
            AICenter.LocalSetting.ClaudeModel = ClaudeModel.Text;
        }

        private void ClaudeKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                AICenter.LocalSetting.ClaudeKey = Encoding.UTF8.GetBytes(ClaudeKey.Text);
            }
            catch { }

            AICenter.SyncAIConfig();
        }

        private void LMPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            AICenter.LocalSetting.LMStudioPort = ConvertHelper.ObjToInt(LMPort.Text);
        }

       

      
    }
}
