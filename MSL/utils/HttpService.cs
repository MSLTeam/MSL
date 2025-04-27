using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MSL.utils
{
    internal class HttpResponse
    {
        public HttpStatusCode HttpResponseCode { get; set; }
        public object HttpResponseContent { get; set; }
        public object HttpResponseException { get; set; }
        public HttpResponse()
        {
            HttpResponseCode = default;
            HttpResponseContent = string.Empty;
            HttpResponseException = null;
        }
    }

    internal class HttpService
    {
        /// <summary>
        /// WebGet
        /// </summary>
        /// <param name="path">位置（默认为软件在线链接的位置）</param>
        /// <param name="customUrl">自定义url，更改后上面的位置将使用此设置的url</param>
        /// <param name="headerMode">UA标识：0等于自动检测（MSL或无Header），1等于无Header，2等于MSL，3等于伪装浏览器Header</param>
        /// <returns>页内容</returns>
        public static string Get(string path, string customUrl = "", int headerMode = 0)
        {
            string url = ConfigStore.ApiLink;
            if (customUrl == "")
            {
                if (ConfigStore.ApiLink == null)
                {
                    return string.Empty;
                }
            }
            else
            {
                url = customUrl;
            }
            WebClient webClient = new WebClient();
            if (headerMode == 0)
            {
                if (ConfigStore.ApiLink != null)
                {
                    string ServerLink = ConfigStore.ApiLink;
                    if (ServerLink?.Contains("/") == true)
                    {
                        ServerLink = ServerLink.Substring(0, ServerLink.IndexOf("/"));
                    }
                    if (url.Contains(ServerLink))
                    {
                        webClient.Headers.Add("User-Agent", "MSLTeam-MSL/" + ConfigStore.MSLVersion);
                    }
                }
            }
            else if (headerMode == 2)
            {
                webClient.Headers.Add("User-Agent", "MSLTeam-MSL/" + ConfigStore.MSLVersion);
            }
            else if (headerMode == 3)
            {
                webClient.Headers.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            }
            webClient.Credentials = CredentialCache.DefaultCredentials;
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            byte[] pageData = webClient.DownloadData(url + "/" + path);
            return Encoding.UTF8.GetString(pageData);
        }

        /// <summary>
        /// 异步HttpGet页内容（MSL Api）
        /// </summary>
        /// <param name="path">位置（MSLApi位置），如"/notice"</param>
        /// <returns></returns>
        public static async Task<JObject> GetApiContentAsync(string path)
        {
            HttpResponse _response = await GetApiAsync(path);
            if (_response.HttpResponseCode == default && _response.HttpResponseException != null)
                throw _response.HttpResponseException as Exception;
            if (_response.HttpResponseCode == HttpStatusCode.OK)
            {
                try
                {
                    return JObject.Parse(_response.HttpResponseContent.ToString());
                }
                catch
                {
                    throw new JsonException($"{_response.HttpResponseContent}", new Exception(_response.HttpResponseCode.ToString()));
                }
            }
            else
            {
                throw new HttpRequestException($"{_response.HttpResponseContent}", new Exception(_response.HttpResponseCode.ToString()));
            }
        }

        /// <summary>
        /// 异步HttpGet（MSL Api）
        /// </summary>
        /// <param name="path">位置（MSLApi位置），如"/notice"，如不以“/”开头，此函数内会自动在路径前添加“/”</param>
        /// <returns>strings[0]=0出现错误，此时strings[1]=错误信息；strings[0]=1请求成功，此时strings[1]=页内容，strings[2]=sha256（若开启getSha256）</returns>
        public static async Task<HttpResponse> GetApiAsync(string path)
        {
            string url = ConfigStore.ApiLink;
            if (!path.StartsWith("/"))
                path = "/" + path;
            return await GetAsync(url + path, headers =>
            {
                headers.Add("DeviceID", ConfigStore.DeviceID);
            }, 1);
        }

        /// <summary>
        /// 异步HttpGet页内容
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="headerUAMode">UA标识：0等于无Header，1等于MSL，2等于伪装浏览器Header，3等于自己设置header</param>
        /// <param name="headerUA">若headerMode=4，此处设置header</param>
        /// <returns>string 页内容</returns>
        public static async Task<object> GetContentAsync(string url, Action<HttpRequestHeaders> configureHeaders = null, int headerUAMode = 0, string headerUA = null)
        {
            HttpResponse _response = await GetAsync(url, configureHeaders, headerUAMode, headerUA);
            if(_response.HttpResponseCode == HttpStatusCode.OK)
                return _response.HttpResponseContent;
            else
                throw new HttpRequestException($"{_response.HttpResponseContent}", new Exception(_response.HttpResponseCode.ToString()));
        }

        /// <summary>
        /// 异步HttpGet（响应代码+页内容）
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="headerUAMode">UA标识：0等于无Header，1等于MSL，2等于伪装浏览器Header，3等于自己设置header</param>
        /// <param name="headerUA">若headerMode=4，此处设置header</param>
        /// <returns>HttpGet 包含响应代码和页内容</returns>
        public static async Task<HttpResponse> GetAsync(string url, Action<HttpRequestHeaders> configureHeaders = null, int headerUAMode = 1, string headerUA = null)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponse httpResponse = new HttpResponse();
            configureHeaders?.Invoke(httpClient.DefaultRequestHeaders);
            if (headerUAMode == 1)
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{ConfigStore.MSLVersion}");
            }
            else if (headerUAMode == 2)
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            }
            else if (headerUAMode == 3 && !string.IsNullOrEmpty(headerUA))
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(headerUA);
            }

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                httpResponse.HttpResponseCode = response.StatusCode;
                httpResponse.HttpResponseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponse.HttpResponseException = ex;
            }
            httpClient.Dispose();
            return httpResponse;
        }

        /// <summary>
        /// WebPost
        /// </summary>
        /// <param name="path">位置（默认为软件在线链接的位置）</param>
        /// <param name="contentType">0为json，1为text，2为x-www-form-urlencoded</param>
        /// <param name="parameterData">Post参数</param>
        /// <param name="customUrl">自定义url，更改后上面的位置将使用此设置的url</param>
        /// <param name="header">Header</param>
        /// <returns>post后，返回的内容</returns>
        public static string Post(string path, int contentType = 0, string parameterData = "", string customUrl = "", WebHeaderCollection header = null)
        {
            string url = ConfigStore.ApiLink;
            if (customUrl == "")
            {
                if (ConfigStore.ApiLink == null)
                {
                    return string.Empty;
                }
            }
            else
            {
                url = customUrl;
            }

            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url + "/" + path);
            byte[] buf = Encoding.GetEncoding("UTF-8").GetBytes(parameterData);
            myRequest.Method = "POST";
            myRequest.ContentLength = buf.Length;
            myRequest.MaximumAutomaticRedirections = 1;
            myRequest.AllowAutoRedirect = true;

            if (contentType == 0)
            {
                myRequest.Accept = "application/json";
                myRequest.ContentType = "application/json; charset=UTF-8";
            }
            else if (contentType == 1)
            {
                myRequest.Accept = "text/plain";
                myRequest.ContentType = "text/plain; charset=UTF-8";

            }
            else if (contentType == 2)
            {

                myRequest.Accept = "application/x-www-form-urlencoded";
                myRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            }

            if (header != null)
            {
                myRequest.Headers = header;
            }

            // 发送请求
            using (Stream stream = myRequest.GetRequestStream())
            {
                stream.Write(buf, 0, buf.Length);
            }

            // 获取响应
            using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
            using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8))
            {
                string returnData = reader.ReadToEnd();
                return returnData;
            }
        }

        /// <summary>
        /// WebPost
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="contentType">0为json，1为text，2为x-www-form-urlencoded，3为none（parameterData强制null）</param>
        /// <param name="parameterData">Post参数</param>
        /// <param name="configureHeaders">Headers</param>
        /// <returns>HttpResponse</returns>
        public static async Task<HttpResponse> PostAsync(string url, int contentType = 0, object parameterData = null, Action<HttpRequestHeaders> configureHeaders = null, int headerUAMode = 1, string headerUA = null)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponse httpResponse = new HttpResponse();
            HttpContent content;
            switch (contentType)
            {
                case 0:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    content = new StringContent(JsonConvert.SerializeObject(parameterData), Encoding.UTF8, "application/json");
                    break;
                case 1:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    content = new StringContent(parameterData as string, Encoding.UTF8, "text/plain");
                    break;
                case 2:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    if (parameterData is IDictionary<string, string> data)
                    {
                        content = new FormUrlEncodedContent(data);
                    }
                    else
                    {
                        var keyValuePairs = parameterData?.ToString().Split('&')
                            .Select(p => p.Split('='))
                            .ToDictionary(p => p[0], p => p[1]);
                        content = new FormUrlEncodedContent(keyValuePairs);
                    }
                    break;
                case 3:
                    content = null;
                    break;
                default:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    content = new StringContent(parameterData as string, Encoding.UTF8, "text/plain");
                    break;
            }

            configureHeaders?.Invoke(httpClient.DefaultRequestHeaders);
            if (headerUAMode == 1)
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{ConfigStore.MSLVersion}");
            }
            else if (headerUAMode == 2)
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            }
            else if (headerUAMode == 3 && !string.IsNullOrEmpty(headerUA))
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(headerUA);
            }

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(url, content);
                httpResponse.HttpResponseCode = response.StatusCode;
                httpResponse.HttpResponseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponse.HttpResponseCode = 0;
                httpResponse.HttpResponseException = ex.Message;
            }
            httpClient.Dispose();
            return httpResponse;
        }
    }
}
