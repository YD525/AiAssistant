using System.Text;

namespace AiAssistant.AI
{
    public class AISetting
    {
        public bool EnableChatGpt { get; set; } = false;
        public bool EnableGemini { get; set; } = false;
        public bool EnableLMStudio { get; set; } = false;

        public string ChatGptKey { get; set; } = "";

        public string GeminiKey { get; set; } = "";

        public int LMStudioPort { get; set; } = 1234;

    }
    public class AICenter
    {
        public AISetting LocalSetting = new AISetting();


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


    }
}
