using System.IO;
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

        public bool EnableCMDUnit { get; set; } = false;//Enabling CMD means that AI can execute DOS commands. AI can do everything that CMD can do. Risk Level: Shell, It is strongly recommended to enable the sandbox when using this feature.
        public bool EnableCSharpCodeUnit { get; set; } = false; //Enabling script generation means that AI can almost completely control your computer. Risk Level: Shell，It is strongly recommended to enable the sandbox when using this feature.
        public bool EnableIOUnit { get; set; } = false;//Enabling file operations means that AI can access, add, modify, and delete any of your disk files. Risk Level: Medium，It is strongly recommended to enable the sandbox when using this feature.
        public bool EnableMouseUnit { get; set; } = false;
        public bool EnableRequestUnit { get; set; } = true;
        public bool EnableWinApiUnit { get; set; } = true;

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
        public static byte[] XOREncrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ XorKey[i % XorKey.Length]);
            }
            return result;
        }

        public static byte[] XORDecrypt(byte[] data)
        {
            return XOREncrypt(data);
        }

        public static ChatGptApi ChatGpt = null;
        public static GeminiApi Gemini = null;
        public static LMStudio LocalAI = null;

        public static void SyncAIConfig()
        {
            if (LocalSetting.EnableChatGpt && LocalSetting.GetChatGptKey().Length > 0)
            {
                ChatGpt = new ChatGptApi();
                ChatGpt.Init(LocalSetting.ChatGptModel, LocalSetting.GetChatGptKey(), null);
            }
            else
            {
                ChatGpt = null;
            }

            if (LocalSetting.EnableGemini && LocalSetting.GetGeminiKey().Length > 0)
            {
                Gemini = new GeminiApi();
                Gemini.Init(LocalSetting.GeminiModel, LocalSetting.GetGeminiKey(), null);
            }
            else
            {
                Gemini = null;
            }

            if (LocalSetting.EnableLMStudio)
            {
                LocalAI = new LMStudio();
                LocalAI.Init(LocalSetting.LMStudioPort);
            }
            else
            {
                LocalAI = null;
            }
        }
        public static void Init()
        {
            SyncAIConfig();
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
                        ConfigPath)));

            if (LocalSetting.ChatGptKey != null)
                if (LocalSetting.ChatGptKey.Length > 0)
                {
                    LocalSetting.ChatGptKey = XORDecrypt(LocalSetting.ChatGptKey);
                }

            if (LocalSetting.GeminiKey != null)
                if (LocalSetting.GeminiKey.Length > 0)
                {
                    LocalSetting.GeminiKey = XORDecrypt(LocalSetting.GeminiKey);
                }
        }

        public static void Save()
        {
            string ConfigPath = DeFine.GetFullPath(@"\setting.config");

            var TempLocalSetting = LocalSetting.Clone();

            if (LocalSetting.ChatGptKey != null)
                if (TempLocalSetting.ChatGptKey.Length > 0)
                {
                    TempLocalSetting.ChatGptKey = XOREncrypt(TempLocalSetting.ChatGptKey);
                }

            if (LocalSetting.GeminiKey != null)
                if (TempLocalSetting.GeminiKey.Length > 0)
                {
                    TempLocalSetting.GeminiKey = XOREncrypt(TempLocalSetting.GeminiKey);
                }

            string GetJson = JsonConvert.SerializeObject(LocalSetting);

            DataHelper.WriteFile(ConfigPath, Encoding.UTF8.GetBytes(GetJson));
        }
    }
}
