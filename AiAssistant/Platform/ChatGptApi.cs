using System.Collections.Generic;
using System.Net;
using System.Text;
using AiAssistant.Request;
using Newtonsoft.Json;

namespace AiAssistant.Platform
{
    public class ChatGptItem
    {
        public string model { get; set; }
        public bool store { get; set; }
        public List<ChatGptMessage> messages { get; set; }
    }

    public class ChatGptMessage
    {
        public string role { get; set; }
        public object content { get; set; }

        public ChatGptMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public ChatGptMessage(string role, string text, string base64Image, string mimeType = "image/jpeg")
        {
            this.role = role;
            this.content = new object[]
            {
            new { type = "text", text = text },
            new
            {
                type = "image_url",
                image_url = new { url = $"data:{mimeType};base64,{base64Image}" }
            }
            };
        }
    }


    public class ChatGptApi 
    {
        public string Model { get; set; } = "";
        public WebProxy ProxyRef { get; set; } = null;
        public string ApiKey = "";
        public void Init(string Model, string ApiKey, WebProxy Proxy)
        {
            this.Model = Model;
            this.ApiKey = ApiKey;
            this.ProxyRef = Proxy;
        }
        public ChatGptRootobject CallAI(string ApiKey, string Msg, ref string Recv)
        {
            int GetCount = Msg.Length;
            ChatGptItem NChatGptItem = new ChatGptItem();
            NChatGptItem.model = Model;
            NChatGptItem.store = true;
            NChatGptItem.messages = new List<ChatGptMessage>();
            NChatGptItem.messages.Add(new ChatGptMessage("user", Msg));
            var GetResult = CallAI(ApiKey, NChatGptItem, ref Recv);
            return GetResult;
        }
        public void GetModes(string ApiKey)
        {
            WebHeaderCollection Headers = new WebHeaderCollection();
            Headers.Add("Authorization", string.Format("Bearer {0}", ApiKey));
            HttpItem Http = new HttpItem()
            {
                URL = "https://api.openai.com/v1/models",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
                Method = "Get",
                Header = Headers,
                Accept = "*/*",
                Postdata = "",
                Cookie = "",
                ContentType = "application/json",
                WebProxy = ProxyRef
            };
            try
            {
                Http.Header.Add("Accept-Encoding", " gzip");
            }
            catch { }

            string GetResult = new HttpHelper().GetHtml(Http).Html;
        }
        public ChatGptRootobject CallAI(string ApiKey, ChatGptItem Item, ref string Recv)
        {
            //GetModes();
            string GetJson = JsonConvert.SerializeObject(Item);
            WebHeaderCollection Headers = new WebHeaderCollection();
            Headers.Add("Authorization", string.Format("Bearer {0}", ApiKey));
            HttpItem Http = new HttpItem()
            {
                URL = "https://api.openai.com/v1/chat/completions",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
                Method = "Post",
                Header = Headers,
                Accept = "*/*",
                Postdata = GetJson,
                Cookie = "",
                ContentType = "application/json; charset=utf-8",
                Encoding = Encoding.UTF8,
                WebProxy = ProxyRef
            };
            try
            {
                Http.Header.Add("Accept-Encoding", " gzip");
            }
            catch { }

            string GetResult = new HttpHelper().GetHtml(Http).Html;

            Recv = GetResult;
            try
            {
                return JsonConvert.DeserializeObject<ChatGptRootobject>(GetResult);
            }
            catch
            {
                return null;
            }
        }

        private string QueryAIWithImage(string text, string base64, string mimeType)
        {
            var Item = new ChatGptItem
            {
                model = Model,
                store = true,
                messages = new List<ChatGptMessage>
            {
                new ChatGptMessage("user", text, base64, mimeType)
            }
            };

            string Recv = "";
            var Result = CallAI(ApiKey, Item, ref Recv);

            if (Result?.choices != null && Result.choices.Length > 0)
                return Result.choices[0].message.content?.Trim() ?? string.Empty;

            return string.Empty;
        }

        //"Important: When translating, strictly keep any text inside angle brackets (< >) or square brackets ([ ]) unchanged. Do not modify, translate, or remove them.\n\n"
        public string QueryAI(string AIPrompt)
        {
            string Recv = "";
            var GetResult = CallAI(ApiKey, AIPrompt, ref Recv);

            if (GetResult != null)
            {
                if (GetResult.choices != null)
                {
                    string GetStr = "";
                    if (GetResult.choices.Length > 0)
                    {
                        GetStr = GetResult.choices[0].message.content.Trim();
                    }
                    if (GetStr.Trim().Length > 0)
                    {
                        return GetStr;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            return string.Empty;
        }



        public class ChatGptRootobject
        {
            public string id { get; set; }
            public string _object { get; set; }
            public int created { get; set; }
            public string model { get; set; }
            public ChatChoice[] choices { get; set; }
            public ChatUsage usage { get; set; }
            public string service_tier { get; set; }
            public string system_fingerprint { get; set; }
        }

        public class ChatUsage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
            public ChatPrompt_Tokens_Details prompt_tokens_details { get; set; }
            public ChatCompletion_Tokens_Details completion_tokens_details { get; set; }
        }

        public class ChatPrompt_Tokens_Details
        {
            public int cached_tokens { get; set; }
            public int audio_tokens { get; set; }
        }

        public class ChatCompletion_Tokens_Details
        {
            public int reasoning_tokens { get; set; }
            public int audio_tokens { get; set; }
            public int accepted_prediction_tokens { get; set; }
            public int rejected_prediction_tokens { get; set; }
        }

        public class ChatChoice
        {
            public int index { get; set; }
            public ChatMessage message { get; set; }
            public object logprobs { get; set; }
            public string finish_reason { get; set; }
        }

        public class ChatMessage
        {
            public string role { get; set; }
            public string content { get; set; }
            public object refusal { get; set; }
        }
    }
}
