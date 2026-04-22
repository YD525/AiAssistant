using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using AiAssistant.ExecuteSandbox;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Provides HTTP request capabilities: GET and POST with configurable timeout.
    /// All methods are sandboxed via Sandbox.Exec.
    /// </summary>
    public class RequestUnit
    {
        public bool Enable = false;
        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
            new CapabilityInfo
            {
                Name        = "HttpGet",
                Description = "Send an HTTP GET request and return the response body as a string",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Url",       Type = "string", Description = "Target URL to request" },
                    new ParameterInfo { Name = "TimeoutMs", Type = "int",    Description = "Request timeout in milliseconds" }
                }
            },
            new CapabilityInfo
            {
                Name        = "HttpPost",
                Description = "Send an HTTP POST request with a body and return the response body as a string",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Url",         Type = "string", Description = "Target URL to post to" },
                    new ParameterInfo { Name = "Body",        Type = "string", Description = "Request body content" },
                    new ParameterInfo { Name = "ContentType", Type = "string", Description = "Content-Type header value, e.g. application/json" },
                    new ParameterInfo { Name = "TimeoutMs",   Type = "int",    Description = "Request timeout in milliseconds" }
                }
            }
        };

        #endregion

        #region HTTP GET

        /// <summary>
        /// Sends a synchronous HTTP GET request and returns the response body.
        /// Throws on non-2xx status codes or timeout.
        /// </summary>
        public string HttpGet(string Url, int TimeoutMs = 10000)
            => Sandbox.Exec(nameof(HttpGet), () =>
            {
                var Request = (HttpWebRequest)WebRequest.Create(Url);
                Request.Method           = "GET";
                Request.Timeout          = TimeoutMs;
                Request.ReadWriteTimeout = TimeoutMs;

                using (var Response = (HttpWebResponse)Request.GetResponse())
                using (var ResponseStream = Response.GetResponseStream())
                using (var Reader = new StreamReader(ResponseStream, Encoding.UTF8))
                {
                    return Reader.ReadToEnd();
                }
            }, Url, TimeoutMs);

        #endregion

        #region HTTP POST

        /// <summary>
        /// Sends a synchronous HTTP POST request with a UTF-8 encoded body and returns the response body.
        /// Throws on non-2xx status codes or timeout.
        /// </summary>
        public string HttpPost(string Url, string Body, string ContentType = "application/json", int TimeoutMs = 10000)
            => Sandbox.Exec(nameof(HttpPost), () =>
            {
                var Request = (HttpWebRequest)WebRequest.Create(Url);
                Request.Method           = "POST";
                Request.ContentType      = ContentType;
                Request.Timeout          = TimeoutMs;
                Request.ReadWriteTimeout = TimeoutMs;

                byte[] BodyBytes = Encoding.UTF8.GetBytes(Body ?? "");
                Request.ContentLength = BodyBytes.Length;

                using (var RequestStream = Request.GetRequestStream())
                {
                    RequestStream.Write(BodyBytes, 0, BodyBytes.Length);
                }

                using (var Response = (HttpWebResponse)Request.GetResponse())
                using (var ResponseStream = Response.GetResponseStream())
                using (var Reader = new StreamReader(ResponseStream, Encoding.UTF8))
                {
                    return Reader.ReadToEnd();
                }
            }, Url, Body, ContentType, TimeoutMs);

        #endregion
    }
}
