using System.IO;
using System.Net.Configuration;
using System.Security.Permissions;
using System.Text;
using AiAssistant.FileManagement;
using AiAssistant.Platform;
using Newtonsoft.Json;

namespace AiAssistant.AI
{
    public class AISetting
    {
        public bool EnableChatGpt { get; set; } = false;
        public bool EnableGemini { get; set; } = false;
        public bool EnableLMStudio { get; set; } = false;

        public string ChatGptModel { get; set; } = "gpt-4.1-nano";
        public string GeminiModel { get; set; } = "gemini-2.5-flash";

        public byte[] ChatGptKey { get; set; }
        public byte[] GeminiKey { get; set; }

        public int LMStudioPort { get; set; } = 1234;

        public bool EnableCMDUnit { get; set; } = false;
        public bool EnableCSharpCodeUnit { get; set; } = false;
        public bool EnableIOUnit { get; set; } = false;
        public bool EnableMouseUnit { get; set; } = false;
        public bool EnableRequestUnit { get; set; } = false;
        public bool EnableWinApiUnit { get; set; } = false;

        public string GetChatGptKey()
        {
            if (ChatGptKey != null && ChatGptKey.Length > 0)
            {
                return Encoding.UTF8.GetString(ChatGptKey);
            }
            return string.Empty;
        }

        public string GetGeminiKey()
        {
            if (GeminiKey != null && GeminiKey.Length > 0)
            {
                return Encoding.UTF8.GetString(GeminiKey);
            }
            return string.Empty;
        }

        public AISetting Clone()
        {
            return new AISetting
            {
                EnableChatGpt = this.EnableChatGpt,
                EnableGemini = this.EnableGemini,
                EnableLMStudio = this.EnableLMStudio,
                LMStudioPort = this.LMStudioPort,

                ChatGptKey = this.ChatGptKey != null ? (byte[])this.ChatGptKey.Clone() : null,
                GeminiKey = this.GeminiKey != null ? (byte[])this.GeminiKey.Clone() : null
            };
        }
    }
    public class AICenter
    {
        public static AISetting LocalSetting = new AISetting();

        private static readonly byte[] XorKey = Encoding.UTF8.GetBytes("AiAssistant");
        private static byte[] XOREncrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ XorKey[i % XorKey.Length]);
            }
            return result;
        }

        private static byte[] XORDecrypt(byte[] data)
        {
            return XOREncrypt(data);
        }

        public static ChatGptApi ChatGpt = null;
        public static GeminiApi Gemini = null;
        public static LMStudio LocalAI = null;

        public static void Init()
        {
            if (LocalSetting.EnableChatGpt && LocalSetting.GetChatGptKey().Length > 0)
            {
                ChatGpt = new ChatGptApi();
                ChatGpt.Init(LocalSetting.ChatGptModel,LocalSetting.GetChatGptKey(),null);
            }

            if (LocalSetting.EnableGemini && LocalSetting.GetGeminiKey().Length > 0)
            {
                Gemini = new GeminiApi();
                Gemini.Init(LocalSetting.GeminiModel, LocalSetting.GetGeminiKey(), null);
            }

            if (LocalSetting.EnableLMStudio)
            {
                LocalAI = new LMStudio();
                LocalAI.Init(LocalAI.LocalPort);
            }
        }

        public static void Load()
        {
            string ConfigPath = DeFine.GetFullPath(@"\setting.config");

            if (!File.Exists(ConfigPath))
            {
                DataHelper.WriteFile(ConfigPath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new AISetting())));
            }

            LocalSetting = JsonConvert.DeserializeObject<AISetting>(
                Encoding.UTF8.GetString(
                    DataHelper.ReadFile(
                        ConfigPath))) ;

            if (LocalSetting.ChatGptKey.Length > 0)
            {
                LocalSetting.ChatGptKey = XORDecrypt(LocalSetting.ChatGptKey);
            }

            if (LocalSetting.GeminiKey.Length > 0)
            {
                LocalSetting.GeminiKey = XORDecrypt(LocalSetting.GeminiKey);
            }
        }

        public static void Save()
        {
            string ConfigPath = DeFine.GetFullPath(@"\setting.config");

            var TempLocalSetting = LocalSetting.Clone();

            if (TempLocalSetting.ChatGptKey.Length > 0)
            {
                TempLocalSetting.ChatGptKey = XOREncrypt(TempLocalSetting.ChatGptKey);
            }

            if (TempLocalSetting.GeminiKey.Length > 0)
            {
                TempLocalSetting.GeminiKey = XOREncrypt(TempLocalSetting.GeminiKey);
            }

            string GetJson = JsonConvert.SerializeObject(LocalSetting);

            DataHelper.WriteFile(ConfigPath,Encoding.UTF8.GetBytes(GetJson));
        }
    }
}
