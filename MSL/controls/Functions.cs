using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Windows.UI.ViewManagement;
using System.Net.Mime;
using System.Security.Policy;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

namespace MSL.controls
{
    internal class Functions
    {
        public static string Get(string path, string customUrl = "")
        {
            string url = "https://api.waheal.top";
            if (customUrl == "")
            {
                if (MainWindow.serverLink != "https://msl.waheal.top")
                {
                    url = MainWindow.serverLink;// + "/api";
                }
            }
            else
            {
                url = customUrl;
            }
            WebClient webClient = new WebClient();
            webClient.Credentials = CredentialCache.DefaultCredentials;
            byte[] pageData = webClient.DownloadData(url + "/" + path);
            return Encoding.UTF8.GetString(pageData);
        }

        public static string Post(string path, int contentType = 0, string parameterData = "", string customUrl = "",WebHeaderCollection header=null)
        {
            string url = "https://api.waheal.top";
            if (customUrl == "")
            {
                if (MainWindow.serverLink != "https://msl.waheal.top")
                {
                    url = MainWindow.serverLink;// + "/api";
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

            if (header != null)
            {
                myRequest.Headers=header;
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

        public static Tuple<int, int, int, int> VersionCompare(string version)
        {
            if (version.StartsWith("*"))
            {
                return Tuple.Create(100, 100, 100, 100);
            }

            // 使用正则表达式从版本号中提取主要版本号
            Regex regex = new Regex(@"(\d+(\.\d+)+)");
            Match match = regex.Match(version);
            if (match.Success)
            {
                version = match.Groups[1].Value;
            }

            // 将版本号中的每个部分转换为整数，并进行比较
            string[] versionParts = version.Split('.');
            List<int> versionIntParts = new List<int>();
            foreach (string part in versionParts)
            {
                if (int.TryParse(part, out int parsedPart))
                {
                    versionIntParts.Add(parsedPart);
                }
            }

            // 添加0，以便对不完整的版本号进行比较（如1.7）
            while (versionIntParts.Count < 4)
            {
                versionIntParts.Add(0);
            }

            return Tuple.Create(versionIntParts[0], versionIntParts[1], versionIntParts[2], versionIntParts[3]);
        }

        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    //采用递归的方法实现
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
    }

    #region OpenFrp Api

    class CreateProxy
    {
        public string session { get; set; }
        public int node_id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string local_addr { get; set; }
        public string local_port { get; set; }
        public string remote_port { get; set; }
        public string domain_bind { get; set; }
        public bool dataGzip { get; set; }
        public bool dataEncrypt { get; set; }
        public string url_route { get; set; }
        public string host_rewrite { get; set; }
        public string request_from { get; set; }
        public string request_pass { get; set; }
        public string custom { get; set; }
    }

    internal class APIControl
    {
        public static string userAccount = "";
        public static string userPass = "";
        public static string sessionId="";
        public static string authId = "";

        public Dictionary<string, string> GetUserNodes()
        {
            JObject userinfo = new JObject
            {
                ["session"] = sessionId
            };
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            var responseMessage = Functions.Post("getUserProxies", 0, JsonConvert.SerializeObject(userinfo), "https://of-dev-api.bfsea.xyz/frp/api", header);
            try
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (JToken node in jArray)
                {
                    Nodes.Add(node["proxyName"].ToString(), node["id"].ToString());
                }
                return Nodes;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var _reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = _reader.ReadToEnd();
                            //DialogShow.ShowMsg(window, error, "获取用户信息失败");
                        }
                    }
                }
                return null;
            }
        }

        public (Dictionary<string, string>, JArray) GetNodeList(Window window)
        {
            JObject userinfo = new JObject
            {
                ["session"] = sessionId
            };
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            var responseMessage = Functions.Post("getNodeList", 0, JsonConvert.SerializeObject(userinfo), "https://of-dev-api.bfsea.xyz/frp/api", header);
            
            try
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                var jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (var node in jArray)
                {
                    if (node["port"].ToString() != "您无权查询此节点的地址" && Convert.ToInt16(node["status"]) == 200 && !Convert.ToBoolean(node["fullyLoaded"]))
                    {
                        string[] targetGroup = node["group"].ToString().Split(';');
                        string nodename = "";
                        if (node["comments"].ToString() == "")
                        {
                            nodename = $"{node["name"]}";

                        }
                        else
                        {
                            nodename = $"[{node["comments"]}]{node["name"]}";
                        }
                        Nodes.Add(nodename, node["id"].ToString());
                    }
                }
                return (Nodes, jArray);
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = reader.ReadToEnd();
                            DialogShow.ShowMsg(window, error, "获取用户信息失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        public void UserSign(Window window)
        {
            JObject userinfo = new JObject
            {
                ["session"] = sessionId
            };
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            var responseMessage = Functions.Post("userSign", 0, JsonConvert.SerializeObject(userinfo), "https://of-dev-api.bfsea.xyz/frp/api", header);
            try
            {
                if ((bool)JObject.Parse(responseMessage)["flag"] == true&&JObject.Parse(responseMessage)["msg"].ToString() == "OK")
                {
                    DialogShow.ShowMsg(window, JObject.Parse(responseMessage)["data"].ToString(), "签到成功");
                }
                else
                {
                    DialogShow.ShowMsg(window, "签到失败", "签到失败");
                }
            }
            catch(Exception ex)
            {
                DialogShow.ShowMsg(window, "签到失败,产生的错误:\n" + ex.Message, "签到失败");
            }
        }

        public string GetUserInfo()
        {
            JObject userinfo = new JObject
            {
                ["session"] = sessionId
            };
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            string responseMessage = Functions.Post("getUserInfo", 0, JsonConvert.SerializeObject(userinfo), "https://of-dev-api.bfsea.xyz/frp/api", header);
            return responseMessage;
        }

        public async Task<string> Login(string account, string password)
        {
            // 创建一个 HttpClient 对象
            HttpClient client = new HttpClient();
            // 准备登录信息
            JObject logininfo = new JObject
            {
                ["user"] = account,
                ["password"] = password
            };
            // 将登录信息序列化为 JSON 字符串
            string json = JsonConvert.SerializeObject(logininfo);
            // 创建一个 StringContent 对象，指定内容类型为 application/json
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            //MessageBox.Show("111");
            // 发送 POST 请求到登录 API 地址
            HttpResponseMessage loginResponse = await client.PostAsync("https://openid.17a.ink/api/public/login", content);
            //MessageBox.Show("111");
            // 检查响应状态码是否为 OK
            if (loginResponse.IsSuccessStatusCode)
            {
                // 读取响应内容
                //string loginData = 
                await loginResponse.Content.ReadAsStringAsync();
                // 显示响应内容
                //MessageBox.Show(loginData);
                string authUrl;
                try
                {
                    WebClient webClient = new WebClient
                    {
                        Credentials = CredentialCache.DefaultCredentials
                    };
                    byte[] pageData = await webClient.DownloadDataTaskAsync("https://of-dev-api.bfsea.xyz/oauth2/login");
                    authUrl = JObject.Parse(Encoding.UTF8.GetString(pageData))["data"].ToString();
                    if (!authUrl.Contains("https://openid.17a.ink/api/")&& authUrl.Contains("https://openid.17a.ink/"))
                    {
                        authUrl = authUrl.Replace("https://openid.17a.ink/", "https://openid.17a.ink/api/");
                    }
                }
                catch(Exception ex)
                {
                    return $"Pre-Login request failed: {ex.Message}";
                }
                //MessageBox.Show(authUrl);
                // 发送 GET 请求到授权 API 地址
                HttpResponseMessage authResponse = await client.GetAsync(authUrl);
                // 检查响应状态码是否为 OK
                if (authResponse.IsSuccessStatusCode)
                {
                    // 读取响应内容
                    string authData = await authResponse.Content.ReadAsStringAsync();
                    // 显示响应内容
                    //MessageBox.Show(authData);
                    // 从响应内容中提取 code
                    authId = JObject.Parse(authData)["data"]["code"].ToString();

                    HttpResponseMessage _loginResponse = await client.GetAsync("https://of-dev-api.bfsea.xyz/oauth2/callback?code=" + authId);
                    // 检查响应状态码是否为 OK
                    if (authResponse.IsSuccessStatusCode)
                    {
                        // 读取响应内容
                        string _loginData = await _loginResponse.Content.ReadAsStringAsync();
                        if ((bool)JObject.Parse(_loginData)["flag"] == false)
                        {
                            return JObject.Parse(_loginData)["msg"].ToString();
                        }
                        // 显示响应内容
                        //MessageBox.Show(_loginData);
                        sessionId = JObject.Parse(_loginData)["data"].ToString();
                        //MessageBox.Show(_loginResponse.Headers.ToString());

                        authId= _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:"), _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:")).IndexOf("\n")-1);
                        //MessageBox.Show(authId);
                        string ret= GetUserInfo();
                        return ret;
                    }
                    else
                    {
                        // 如果响应状态码不是 OK，抛出异常
                        return $"Login request failed: {authResponse.StatusCode}";
                    }

                }
                else
                {
                    // 如果响应状态码不是 OK，抛出异常
                    return $"Auth request failed: {authResponse.StatusCode}";
                }
            }
            else
            {
                // 如果响应状态码不是 OK，抛出异常
                return $"Pre-Login request failed: {loginResponse.StatusCode}";
            }
        }

        public bool CreateProxy(string type, string port, bool EnableZip, int nodeid, string remote_port, string proxy_name,out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/newProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(authId);
            string json = JsonConvert.SerializeObject(new CreateProxy()
            {
                session = sessionId,
                node_id = nodeid,
                name = proxy_name,
                type = type,
                local_addr = "127.0.0.1",
                local_port = port,
                remote_port = remote_port,
                domain_bind = "",
                dataGzip = EnableZip,
                dataEncrypt = false,
                url_route = "",
                host_rewrite = "",
                request_from = "",
                request_pass = "",
                custom = ""
            });//转换json格式
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseMessage = reader.ReadToEnd();
                var deserializedMessage = JObject.Parse(responseMessage);
                reader.Close();
                dataStream.Close();
                response.Close();
                if ((bool)deserializedMessage["flag"] == true)
                {
                    returnMsg = "";
                    return true;
                }
                else
                {
                    returnMsg = deserializedMessage["msg"].ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                returnMsg = ex.Message;
                return false;
            }
        }

        public bool DeleteProxy(string id, out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/removeProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(authId);
            JObject json =new JObject()
            {
                ["proxy_id"]=id,
                ["session"] = sessionId
            };//转换json格式
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json));
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseMessage = reader.ReadToEnd();
                var deserializedMessage = JObject.Parse(responseMessage);
                reader.Close();
                dataStream.Close();
                response.Close();
                if ((bool)deserializedMessage["flag"] == true)
                {
                    returnMsg = "";
                    return true;
                }
                else
                {
                    returnMsg = deserializedMessage["msg"].ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                returnMsg = ex.Message;
                return false;
            }
        }
    }
    #endregion
}
