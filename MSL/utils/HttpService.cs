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
            LogHelper.WriteLog($"HTTP GET: {url}", LogLevel.INFO);
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                httpResponse.HttpResponseCode = response.StatusCode;
                httpResponse.HttpResponseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponse.HttpResponseException = ex;
                LogHelper.WriteLog($"HTTP GET异常: ", LogLevel.ERROR);
            }
            httpClient.Dispose();
            return httpResponse;
        }

        public enum PostContentType
        {
            Json,
            Text,
            FormUrlEncoded,
            None
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
        public static string Post(string path, PostContentType contentType = PostContentType.Json, string parameterData = "", string customUrl = "", WebHeaderCollection header = null)
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

            switch(contentType)
            {
                case PostContentType.Json:
                    myRequest.Accept = "application/json";
                    myRequest.ContentType = "application/json; charset=UTF-8";
                    break;
                case PostContentType.Text:
                    myRequest.Accept = "text/plain";
                    myRequest.ContentType = "text/plain; charset=UTF-8";
                    break;
                case PostContentType.FormUrlEncoded:
                    myRequest.Accept = "application/x-www-form-urlencoded";
                    myRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    break;
                case PostContentType.None:
                    myRequest.Accept = "text/plain";
                    myRequest.ContentType = "text/plain; charset=UTF-8";
                    break;
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
        public static async Task<HttpResponse> PostAsync(string url, PostContentType contentType = PostContentType.Json, object parameterData = null, Action<HttpRequestHeaders> configureHeaders = null, int headerUAMode = 1, string headerUA = null)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponse httpResponse = new HttpResponse();
            HttpContent content;
            switch (contentType)
            {
                case PostContentType.Json:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    content = new StringContent(JsonConvert.SerializeObject(parameterData), Encoding.UTF8, "application/json");
                    break;
                case PostContentType.Text:
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    content = new StringContent(parameterData as string, Encoding.UTF8, "text/plain");
                    break;
                case PostContentType.FormUrlEncoded:
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
                case PostContentType.None:
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

        #region --- 新增的日志上传函数 ---

        // 用于反序列化API响应的辅助类
        private class LogUploadApiResponse
        {
            public int code { get; set; }
            public string message { get; set; }
            public LogUploadData data { get; set; }
        }

        private class LogUploadData
        {
            public int log_id { get; set; }
        }

        /// <summary>
        /// 上传软件崩溃日志
        /// </summary>
        /// <param name="logContent">异常日志的详细内容</param>
        /// <returns>成功时返回服务器分配的 log_id</returns>
        /// <exception cref="Exception">当网络请求失败、服务器返回非200状态码或API业务逻辑错误时抛出</exception>
        public static async Task<int> UploadCrashLogAsync(string logContent)
        {
            string apiUrl = ConfigStore.ApiLink + "/log/upload";

            // 获取设备信息
            string deviceInfo = $"操作系统: {Environment.OSVersion.VersionString}; " +
                                $"CLR版本: {Environment.Version}";

            var postData = new Dictionary<string, string>
        {
            { "log", logContent },
            { "deviceInfo", deviceInfo }
        };

            HttpResponse response = await PostAsync(
                url: apiUrl,
                contentType: PostContentType.FormUrlEncoded,
                parameterData: postData,
                configureHeaders: headers =>
                {
                    headers.Add("DeviceID", ConfigStore.DeviceID);
                }
            );

            if (response.HttpResponseCode != HttpStatusCode.OK || !string.IsNullOrEmpty(response.HttpResponseException?.ToString()))
            {
                throw new Exception($"日志上传请求失败。状态码: {response.HttpResponseCode}。内部错误: {response.HttpResponseException ?? "无"}");
            }

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<LogUploadApiResponse>(response.HttpResponseContent?.ToString());

                if (apiResponse != null && apiResponse.code == 200 && apiResponse.data != null)
                {
                    // 成功，返回 log_id
                    return apiResponse.data.log_id;
                }
                else
                {
                    string errorMessage = apiResponse?.message ?? "未知的API错误";
                    int errorCode = apiResponse?.code ?? -1;
                    throw new Exception($"API返回错误。代码: {errorCode}, 消息: '{errorMessage}'");
                }
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"无法解析服务器响应: {jsonEx.Message}。原始响应内容: {response.HttpResponseContent}", jsonEx);
            }
        }

        /// <summary>
        /// 同步上传软件崩溃日志。此方法会阻塞当前线程直到完成。
        /// 主要用于 AppDomain.UnhandledException 等无法使用 await 的场景。
        /// </summary>
        /// <param name="logContent">异常日志的详细内容</param>
        /// <returns>成功时返回服务器分配的 log_id</returns>
        /// <exception cref="Exception">当上传失败时抛出</exception>
        public static int UploadCrashLog(string logContent)
        {
            try
            {
                // 调用异步方法并阻塞等待结果
                // GetAwaiter().GetResult() 在非 async 方法中调用 async 方法
                return UploadCrashLogAsync(logContent).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception("同步日志上传失败。", ex);
            }
        }
        #endregion
    }
}
