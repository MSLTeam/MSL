using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MSL.utils
{
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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = "https://api." + MainWindow.serverLink;
            if (customUrl == "")
            {
                if (MainWindow.serverLink == null)
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
                string serverLink = MainWindow.serverLink;
                if (serverLink.Contains("/"))
                {
                    serverLink = serverLink.Substring(0, serverLink.IndexOf("/"));
                }
                if (url.Contains(serverLink))
                {
                    webClient.Headers.Add("User-Agent", "MSL/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                }
            }
            else if (headerMode == 2)
            {
                webClient.Headers.Add("User-Agent", "MSL/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            }
            else if (headerMode == 3)
            {
                webClient.Headers.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            }
            webClient.Credentials = CredentialCache.DefaultCredentials;
            byte[] pageData = webClient.DownloadData(url + "/" + path);
            return Encoding.UTF8.GetString(pageData);
        }

        /// <summary>
        /// 异步HttpGet
        /// </summary>
        /// <param name="path">位置（默认为软件在线链接的位置）</param>
        /// <param name="customUrl">自定义url，更改后上面的位置将使用此设置的url</param>
        /// <param name="headerMode">UA标识：0等于自动检测（MSL或无Header），1等于无Header，2等于MSL，3等于伪装浏览器Header</param>
        /// <param name="getSha256">是否获取sha256（特定功能需要）</param>
        /// <returns>strings[0]=0出现错误，此时strings[1]=错误信息；strings[0]=1请求成功，此时strings[1]=页内容，strings[2]=sha256（若开启getSha256）</returns>
        public static async Task<string[]> GetAsync(string path, string customUrl = "", int headerMode = 0, bool getSha256 = false)
        {
            string[] strings = new string[3];
            string url = "https://api." + MainWindow.serverLink;

            if (string.IsNullOrEmpty(customUrl))
            {
                if (MainWindow.serverLink == null)
                {
                    strings[0] = string.Empty;
                    strings[1] = string.Empty;
                    return strings;
                }
            }
            else
            {
                url = customUrl;
            }
            using (HttpClient client = new HttpClient())
            {
                if (headerMode == 0)
                {
                    string serverLink = MainWindow.serverLink;
                    if (serverLink.Contains("/"))
                    {
                        serverLink = serverLink.Substring(0, serverLink.IndexOf("/"));
                    }
                    if (url.Contains(serverLink))
                    {
                        client.DefaultRequestHeaders.UserAgent.TryParseAdd("MSL/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                    }
                }
                else if (headerMode == 2)
                {
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd("MSL/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                }
                else if (headerMode == 3)
                {
                    client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                }
                try
                {
                    strings[0] = "1";
                    HttpResponseMessage response = await client.GetAsync(url + "/" + path);
                    response.EnsureSuccessStatusCode();
                    strings[1] = await response.Content.ReadAsStringAsync();
                    if (getSha256 && response.Headers.Contains("sha256"))
                    {
                        strings[2] = response.Headers.GetValues("sha256").FirstOrDefault();
                    }
                    else
                    {
                        strings[2] = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    strings[0] = "0";
                    strings[1] = ex.Message;
                    // 这里可以记录日志或者处理异常
                }
            }
            return strings;
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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = "https://api." + MainWindow.serverLink;
            if (customUrl == "")
            {
                if (MainWindow.serverLink == null)
                {
                    return string.Empty;
                }
            }
            else
            {
                url = customUrl;
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url + "/" + path);
            byte[] buf = Encoding.GetEncoding("UTF-8").GetBytes(parameterData);
            if (contentType == 0)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "application/json";
                myRequest.ContentType = "application/json; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }
            else if (contentType == 1)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "text/plain";
                myRequest.ContentType = "text/plain; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }
            else if (contentType == 2)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "application/x-www-form-urlencoded";
                myRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
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
    }
}
