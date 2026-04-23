using System.Collections.Generic;
using System.Net;
using System.Text;
using AiAssistant.Request;
using Newtonsoft.Json;

namespace AiAssistant.Platform
{
    // ── Request ──────────────────────────────────────────────────────────────

    public class ClaudeItem
    {
        public string model { get; set; }
        public int max_tokens { get; set; } = 4096;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string system { get; set; }

        public List<ClaudeMessage> messages { get; set; } = new List<ClaudeMessage>();

        public ClaudeItem(string model) { this.model = model; }
    }

    public class ClaudeMessage
    {
        public string role { get; set; }
        public object content { get; set; }

        public ClaudeMessage(string role, string text)
        {
            this.role = role;
            this.content = text;
        }

        public ClaudeMessage(string role, string text, string base64Image, string mimeType = "image/jpeg")
        {
            this.role = role;
            this.content = new object[]
            {
                new
                {
                    type   = "image",
                    source = new
                    {
                        type       = "base64",
                        media_type = mimeType,
                        data       = base64Image
                    }
                },
                new { type = "text", text = text }
            };
        }
    }

    // ── Response ─────────────────────────────────────────────────────────────

    public class ClaudeResponse
    {
        public string id { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public string model { get; set; }
        public string stop_reason { get; set; }
        public ClaudeContent[] content { get; set; }
        public ClaudeUsage usage { get; set; }
    }

    public class ClaudeContent
    {
        public string type { get; set; }
        public string text { get; set; }
    }

    public class ClaudeUsage
    {
        public int input_tokens { get; set; }
        public int output_tokens { get; set; }
    }

    // ── API Client ────────────────────────────────────────────────────────────

    public class ClaudeApi
    {
        public string Model { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public WebProxy ProxyRef { get; set; } = null;

        public void Init(string model, string apiKey, WebProxy proxy = null)
        {
            Model = model;
            ApiKey = apiKey;
            ProxyRef = proxy;
        }

        public ClaudeResponse CallAI(ClaudeItem item, ref string recv)
        {
            string json = JsonConvert.SerializeObject(item);

            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("x-api-key", ApiKey);
            headers.Add("anthropic-version", "2023-06-01");

            HttpItem http = new HttpItem()
            {
                URL = "https://api.anthropic.com/v1/messages",
                UserAgent = "Mozilla/5.0",
                Method = "Post",
                Header = headers,
                Accept = "application/json",
                Postdata = json,
                ContentType = "application/json; charset=utf-8",
                Encoding = Encoding.UTF8,
                WebProxy = ProxyRef,
                Timeout = 60000
            };

            try { http.Header.Add("Accept-Encoding", "gzip"); } catch { }

            string result = new HttpHelper().GetHtml(http).Html;
            recv = result;

            try { return JsonConvert.DeserializeObject<ClaudeResponse>(result); }
            catch { return null; }
        }

        public string QueryAI(string prompt, string systemPrompt = null)
        {
            var item = new ClaudeItem(Model) { system = systemPrompt };
            item.messages.Add(new ClaudeMessage("user", prompt));

            string recv = "";
            var result = CallAI(item, ref recv);
            return ExtractText(result);
        }

        private string QueryAIWithImage(string text, string base64, string mimeType, string systemPrompt = null)
        {
            var item = new ClaudeItem(Model) { system = systemPrompt };
            item.messages.Add(new ClaudeMessage("user", text, base64, mimeType));

            string recv = "";
            var result = CallAI(item, ref recv);
            return ExtractText(result);
        }


        private string ExtractText(ClaudeResponse result)
        {
            if (result?.content == null || result.content.Length == 0)
                return string.Empty;

            foreach (var block in result.content)
                if (block.type == "text" && !string.IsNullOrEmpty(block.text))
                    return block.text.Trim();

            return string.Empty;
        }
    }
}
