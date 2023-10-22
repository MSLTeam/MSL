using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string Post(string path, int contentType = 0, string parameterData = "", string customUrl = "")
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

            // 发送请求
            using (Stream stream = myRequest.GetRequestStream())
            {
                stream.Write(buf, 0, buf.Length);
            }

            // 获取响应
            using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
            using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8))
            {
                //string returnData = Regex.Unescape(reader.ReadToEnd());
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

    class baseReturn
    {
        public bool flag { get; set; }
        public string msg { get; set; }
    }

    class LoginInfo
    {
        public string user { get; set; }
        public string password { get; set; }
    }

    class LoginMessage : baseReturn
    {
        public string data { get; set; }
    };

    class SessionID
    {
        public string session { get; set; }
    }

    class UserwithSessionID : SessionID
    {
        public string auth { get; set; }
    }

    class UserData
    {
        public int outLimit { get; set; }
        public int used { get; set; }
        public string token { get; set; }
        public bool realname { get; set; }
        public double regTime { get; set; }
        public int inLimit { get; set; }
        public string friendlyGroup { get; set; }
        public int id { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string group { get; set; }
        public string traffic { get; set; }
    }

    class UserInfo : baseReturn
    {
        public UserData data { get; set; }
    }

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

    class APIControl
    {
        public Dictionary<string, string> GetUserNodes(string id, string auth, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/getUserProxies");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", auth);
            SessionID userinfo = new SessionID
            {
                session = id
            };
            string json = JsonConvert.SerializeObject(userinfo);//转换json格式
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
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (JToken node in jArray)
                {
                    Nodes.Add(node["proxyName"].ToString(), node["id"].ToString());
                    //DialogShow.ShowMsg(window, node["proxyName"].ToString(), node["id"].ToString());
                }
                return Nodes;
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
                return null;
            }
        }

        public string GetUserNodeId(string id, string auth, string name, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/getUserProxies");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", auth);
            SessionID userinfo = new SessionID
            {
                session = id
            };
            string json = JsonConvert.SerializeObject(userinfo);//转换json格式
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
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (JToken node in jArray)
                {
                    if (node["proxyName"] != null)
                    {
                        if (node["proxyName"].ToString() == name)
                        {
                            return node["id"].ToString();
                        }
                    }
                    //else DialogShow.ShowMsg(window, "node[proxyName] = null", "debug");
                }
                reader.Close();
                dataStream.Close();
                response.Close();
                return null;
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
                return null;
            }
        }

        public (Dictionary<string, string>, JArray) GetNodeList(string id, string auth, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/getNodeList");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", auth);
            SessionID userinfo = new SessionID
            {
                session = id
            };
            string json = JsonConvert.SerializeObject(userinfo);//转换json格式
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
                reader.Close();
                dataStream.Close();
                response.Close();
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

        public void UserSign(string id, string auth, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/userSign");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", auth);
            SessionID userinfo = new SessionID
            {
                session = id
            };
            string json = JsonConvert.SerializeObject(userinfo);//转换json格式用于登录API
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
                if (JsonConvert.DeserializeObject<LoginMessage>(responseMessage) != null && JsonConvert.DeserializeObject<LoginMessage>(responseMessage).flag)
                {
                    DialogShow.ShowMsg(window, JsonConvert.DeserializeObject<LoginMessage>(responseMessage).data, "签到成功");
                }
                else
                {
                    DialogShow.ShowMsg(window, "签到失败", "签到失败");
                }
            }
            catch (Exception ex) { DialogShow.ShowMsg(window, "签到失败,产生的错误:\n" + ex.Message, "签到失败"); }
        }

        public UserData GetUserInfo(string id, string auth, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/getUserInfo");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", auth);
            SessionID userinfo = new SessionID
            {
                session = id
            };
            string json = JsonConvert.SerializeObject(userinfo);//转换json格式用于登录API
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
                UserInfo deserializedMessage = JsonConvert.DeserializeObject<UserInfo>(responseMessage);
                UserData userdata = deserializedMessage.data;
                string welcome = $"欢迎,{userdata.username}\n";
                string limit = $"带宽限制: {userdata.outLimit}↑ | ↓ {userdata.inLimit}\n";
                string used = $"已用隧道:{userdata.used}条\n";
                string group = $"用户组:{userdata.friendlyGroup}\n";
                string userid = $"ID:{userdata.id}\n";
                string email = $"邮箱:{userdata.email}\n";
                string traffic = $"剩余流量:{userdata.traffic}Mib";
                string showusrinfo = welcome + traffic + limit + group + userid + email + used;
                bool login = DialogShow.ShowMsg(window, showusrinfo, "用户信息", true, "确定", "点击签到");
                if (login)
                {
                    UserSign(id, auth, window);
                }
                reader.Close();
                dataStream.Close();
                response.Close();
                return userdata;
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
                return null;
            }
        }

        public (UserwithSessionID, string) Login(string account, string password, System.Windows.Window window)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/user/login");
            request.Method = "POST";
            request.ContentType = "application/json";
            LoginInfo logininfo = new LoginInfo
            {
                user = account,
                password = password
            };
            string json = JsonConvert.SerializeObject(logininfo);//转换json格式用于登录API
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
                LoginMessage deserializedMessage = JsonConvert.DeserializeObject<LoginMessage>(responseMessage);
                reader.Close();
                dataStream.Close();
                response.Close();
                if (deserializedMessage.flag == true)
                {
                    string auth = response.Headers.Get("Authorization");
                    string id = deserializedMessage.data.ToString();
                    UserwithSessionID user = new UserwithSessionID
                    {
                        auth = auth,
                        session = id
                    };
                    UserData info = GetUserInfo(id, auth, window);
                    return (user, info.token);
                }
                else
                {
                    DialogShow.ShowMsg(window, deserializedMessage.msg, "登录失败");
                    return (null, null);
                }
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
                            DialogShow.ShowMsg(window, error, "登录失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        public (LoginMessage, string) CreateProxy(string id, string auth, string type, string port, bool EnableZip, int nodeid, JArray jArray, System.Windows.Window window)
        {
            #region 获取节点端口限制
            (int, int) remote_port_limit = (10000, 99999);
            foreach (var node in jArray)
            {
                if (Convert.ToInt32(node["id"]) == nodeid)
                {
                    try
                    {
                        var s = node["allowPort"].ToString().Trim('(', ')', ' ');
                        remote_port_limit = ValueTuple.Create(Array.ConvertAll(s.Split(','), int.Parse)[0], Array.ConvertAll(s.Split(','), int.Parse)[1]);
                    }
                    catch { remote_port_limit = (10000, 99999); }
                    break;
                }
            }
            #endregion
            bool input_name = DialogShow.ShowInput(window, "隧道名称(不支持中文)", out string proxy_name);
            Random random = new Random();
            string remote_port;
            remote_port = random.Next(remote_port_limit.Item1, remote_port_limit.Item2).ToString();
            if (input_name)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/newProxy");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", auth);
                string json = JsonConvert.SerializeObject(new CreateProxy()
                {
                    session = id,
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
                    LoginMessage deserializedMessage = JsonConvert.DeserializeObject<LoginMessage>(responseMessage);
                    reader.Close();
                    dataStream.Close();
                    response.Close();
                    if (deserializedMessage.flag == true)
                    {
                        DialogShow.ShowMsg(window, "创建隧道成功\n", "创建成功");
                        return (deserializedMessage, proxy_name);
                    }
                    else
                    {
                        DialogShow.ShowMsg(window, "创建隧道失败\n" + responseMessage, "创建失败");
                        return (null, null);
                    }
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
                                DialogShow.ShowMsg(window, error, "创建隧道失败");
                            }
                        }
                    }
                    return (null, null);
                }
            }
            else
            {
                DialogShow.ShowMsg(window, "请确保输入了隧道名称", "创建失败");
                return (null, null);
            }
        }
    }
    #endregion
}
