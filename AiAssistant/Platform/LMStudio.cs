using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using AiAssistant.Request;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AiAssistant.Platform
{
     public class OpenAIItem
    {
        public string model { get; set; }
        public bool store { get; set; }
        public List<OpenAIMessage> messages { get; set; } = new List<OpenAIMessage>();

        public OpenAIItem(string model)
        {
            this.model = model;
        }
    }

    public class OpenAIMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public OpenAIMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    public class OpenAIResponse
    {
        public string id { get; set; }
        public string _object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public OpenAIChoice[] choices { get; set; }
        public OpenAIUsage usage { get; set; }
        public OpenAIStats stats { get; set; }
        public string system_fingerprint { get; set; }
    }

    public class OpenAIUsage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class OpenAIStats
    {
    }

    public class OpenAIChoice
    {
        public int index { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
        public OpenAIRMessage message { get; set; }
    }

    public class OpenAIRMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    public class LMStudio 
    {
        public int LocalPort { get; set; } = 0;
        public int CustomID { get; set; } = 0;


        public static string CurrentModel = "";
        public void GetCurrentModel()
        {
            if (LMStudio.CurrentModel == "")
            {
                LMStudio.CurrentModel = GetCurrentModelName();
            }
        }
        public OpenAIResponse CallAI(string Msg, ref string Recv)
        {
            if (CurrentModel == string.Empty)
            {
                GetCurrentModel();
            }

            if (CurrentModel == string.Empty)
            {
                return new OpenAIResponse();
            }

            int GetCount = Msg.Length;
            OpenAIItem NOpenAIItem = new OpenAIItem(CurrentModel);
            NOpenAIItem.store = true;
            NOpenAIItem.messages.Add(new OpenAIMessage("user", Msg));
            var GetResult = CallAI(NOpenAIItem, ref Recv);
            return GetResult;
        }

        public string GetCurrentModelName()
        {
            // Construct the URL for the request
            string GenUrl = "http://localhost" + ":" + LocalPort + "/v1/models";

            WebHeaderCollection Headers = new WebHeaderCollection();
            HttpItem Http = new HttpItem()
            {
                URL = GenUrl,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
                Method = "Get",
                Header = Headers,
                Accept = "*/*",
                Postdata = "",
                Cookie = "",
                Timeout = 5000,
                ContentType = "application/json",
                //ProxyIp = ProxyCenter.GlobalProxyIP // Uncomment if a proxy is needed
            };

            try
            {
                string GetResult = new HttpHelper().GetHtml(Http).Html;
                JObject Obj = JObject.Parse(GetResult);

                JArray Models = (JArray)Obj["data"];
                if (Models != null && Models.Count > 0)
                {
                    string ID = (string)Models[0]["id"];
                    return ID ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current model: {ex.Message}");
                return string.Empty;
            }

            return string.Empty;
        }

        public static object SingleLock = new object();
        public OpenAIResponse CallAI(OpenAIItem Item, ref string Recv)
        {
            lock (SingleLock)
            {
                string GenUrl = "http://localhost" + ":" + LocalPort + "/v1/chat/completions";
                string GetJson = JsonConvert.SerializeObject(Item);
                WebHeaderCollection Headers = new WebHeaderCollection();
                //Headers.Add("Authorization", string.Format("Bearer {0}", DeFine.GlobalLocalSetting.LMKey));
                HttpItem Http = new HttpItem()
                {
                    URL = GenUrl,
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
                    Method = "Post",
                    Header = Headers,
                    Accept = "*/*",
                    Postdata = GetJson,
                    Cookie = "",
                    ContentType = "application/json; charset=utf-8",
                    Encoding = Encoding.UTF8
                    //ProxyIp = ProxyCenter.GlobalProxyIP
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
                    return JsonConvert.DeserializeObject<OpenAIResponse>(GetResult);
                }
                catch
                {
                    return null;
                }
            }
        }
        //"Important: When translating, strictly keep any text inside angle brackets (< >) or square brackets ([ ]) unchanged. Do not modify, translate, or remove them.\n\n"
        public string QueryAI(string AIPrompt)
        {
            string Recv = "";
            var GetResult = CallAI(AIPrompt, ref Recv);

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
    }
}
