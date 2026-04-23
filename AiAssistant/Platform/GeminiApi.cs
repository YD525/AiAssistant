using System.Collections.Generic;
using System.Net;
using System.Text;
using AiAssistant.Request;
using Newtonsoft.Json;

namespace AiAssistant.Platform
{
    public class GeminiItem
    {
        public List<GeminiContent> contents { get; set; } = new List<GeminiContent>();
    }

    public class GeminiContent
    {
        public List<GeminiPart> parts { get; set; } = new List<GeminiPart>();
    }

    public class GeminiPart
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string text { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public GeminiInlineData inlineData { get; set; }
    }

    public class GeminiInlineData
    {
        public string mimeType { get; set; }
        public string data { get; set; }  
    }


    public class GeminiRootobject
    {
        public GeminiCandidate[] candidates { get; set; }
        public GeminiUsagemetadata usageMetadata { get; set; }
        public string modelVersion { get; set; }
    }

    public class GeminiUsagemetadata
    {
        public int promptTokenCount { get; set; }
        public int candidatesTokenCount { get; set; }
        public int totalTokenCount { get; set; }
        public GeminiPrompttokensdetail[] promptTokensDetails { get; set; }
        public GeminiCandidatestokensdetail[] candidatesTokensDetails { get; set; }
    }

    public class GeminiPrompttokensdetail
    {
        public string modality { get; set; }
        public int tokenCount { get; set; }
    }

    public class GeminiCandidatestokensdetail
    {
        public string modality { get; set; }
        public int tokenCount { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent content { get; set; }
        public string finishReason { get; set; }
        public float avgLogprobs { get; set; }
    }

    public class GeminiRContent
    {
        public GeminiRPart[] parts { get; set; }
        public string role { get; set; }
    }

    public class GeminiRPart
    {
        public string text { get; set; }
    }

    public class GeminiApi
    {
        public string Model { get; set; } = "";
        public WebProxy ProxyRef { get; set; } = null;

        public string ApiKey = "";
        public void Init(string Model,string ApiKey,WebProxy Proxy)
        {
            this.Model = Model;
            this.ApiKey = ApiKey;
            this.ProxyRef = Proxy;
        }

        private string QueryAIWithImage(string text, string base64, string mimeType)
        {
            var Content = new GeminiContent();

            Content.parts.Add(new GeminiPart { text = text });

            Content.parts.Add(new GeminiPart
            {
                inlineData = new GeminiInlineData
                {
                    mimeType = mimeType,
                    data = base64
                }
            });

            var Item = new GeminiItem();
            Item.contents.Add(Content);

            string Recv = "";
            var Result = CallAI(ApiKey, Item, ref Recv);

            if (Result?.candidates != null && Result.candidates.Length > 0)
            {
                try
                {
                    return Result.candidates[0].content.parts[0].text?.Trim() ?? string.Empty;
                }
                catch { return string.Empty; }
            }

            return string.Empty;
        }

        public string QueryAI(string AIPrompt)
        {
            string Recv = "";
            var GetResult = CallAI(ApiKey, AIPrompt, ref Recv);

            if (GetResult != null)
            {
                try
                {
                    if (GetResult.candidates != null)
                    {
                        string GetStr = "";
                        if (GetResult.candidates.Length > 0)
                        {
                            if (GetResult.candidates[0].content.parts.Count > 0)
                            {
                                GetStr = GetResult.candidates[0].content.parts[0].text.Trim();
                            }
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
                catch { return string.Empty; }
            }
            return string.Empty;
        }

        public GeminiRootobject CallAI(string ApiKey, string Msg, ref string Recv)
        {
            int GetCount = Msg.Length;
            GeminiItem NGeminiItem = new GeminiItem();
            NGeminiItem.contents.Add(new GeminiContent());
            NGeminiItem.contents[0].parts.Add(new GeminiPart());
            NGeminiItem.contents[0].parts[0].text = Msg;
            var GetResult = CallAI(ApiKey, NGeminiItem, ref Recv);
            return GetResult;
        }

        public GeminiRootobject CallAI(string ApiKey, GeminiItem Item, ref string Recv)
        {
            string GetJson = JsonConvert.SerializeObject(Item);
            WebHeaderCollection Headers = new WebHeaderCollection();
            HttpItem Http = new HttpItem()
            {
                URL = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}",
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
                return JsonConvert.DeserializeObject<GeminiRootobject>(GetResult);
            }
            catch
            {
                return null;
            }
        }
    }
}
