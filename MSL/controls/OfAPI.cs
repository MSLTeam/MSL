using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace MSL.controls.OfAPI
{
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

    /*
    class Protocol
    {
        public bool tcp { get; set; }
        public bool udp { get; set; }
		public bool xtcp { get; set; }
		public bool stcp { get; set; }
		public bool http { get; set; }
		public bool https { get; set; }
    }
    
    class NodeInfo
    {
        public bool allowEc { get; set; }
        public int bandwidth { get; set; }
        public int bandwidthMagnification { get; set; }
        public string classify { get; set; }
        public string comments { get; set; }
        public string group { get; set; }
        public string hostname { get; set; }
        public string id { get; set; }
        public int maxOnlineMagnification { get; set; }
        public string name { get; set; }
        public int needRealname { get; set; }
        public string port { get; set; }
        public int status { get; set; }
        public int unitcostEc { get; set; }
        public string description { get; set; }
        public Protocol protocolSupport { get; set; }
        public string allowPort { get; set; }
        public bool fullyLoaded { get; set; }
    }
    
    class NodeList
    { 
        public int total { get; set; }
        public NodeInfo list { get; set; }
    }

    class Node : baseReturn
    {
        public string data { get; set; }
    }
    */

    class APIControl
    {
        public string GetUserNodeId(string id, string auth, string name, Window window)
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
                /*
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                JObject jod = (JObject)JsonConvert.DeserializeObject(jo["data"].ToString());
                DialogShow.ShowMsg(window, jo["data"].ToString(), "debug");
                JArray jArray = (JArray)JsonConvert.DeserializeObject(jod["list"].ToString());
                DialogShow.ShowMsg(window, jod["list"].ToString(), "debug");
                */
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                _ = DialogShow.ShowMsg(window, jo["data"]["list"].ToString(), "debug");
                JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (var node in jArray)
                {
                    if (node["proxyName"] != null)
                    {
                        if (node["proxyName"].ToString() == name)
                        {
                            return node["id"].ToString();
                        }
                    }
                    else _ = DialogShow.ShowMsg(window, "node[proxyName] = null", "debug");
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
                            _ = DialogShow.ShowMsg(window, error, "获取用户信息失败");
                        }
                    }
                }
                return null;
            }
        }

        public (Dictionary<string, string>, JArray) GetNodeList(string id, string auth, Window window)
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
                        string nodename = $"[{node["comments"]}]{node["name"]}";
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
                            _ = DialogShow.ShowMsg(window, error, "获取用户信息失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        public UserData GetUserInfo(string id, string auth, Window window)
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
                _ = DialogShow.ShowMsg(window, showusrinfo, "用户信息", true, "确定", "点击签到");
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
                            _ = DialogShow.ShowMsg(window, error, "获取用户信息失败");
                        }
                    }
                }
                return null;
            }
        }

        public (UserwithSessionID, string) Login(string account, string password, Window window)
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
                    _ = DialogShow.ShowMsg(window, "登录失败\n" + responseMessage, "登录失败");
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
                            _ = DialogShow.ShowMsg(window, error, "登录失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        public (LoginMessage, string) CreateProxy(string id, string auth, string type, string port, bool EnableZip, int nodeid, JArray jArray, Window window)
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
                        _ = DialogShow.ShowMsg(window, "创建隧道成功\n", "创建成功");
                        return (deserializedMessage, proxy_name);
                    }
                    else
                    {
                        _ = DialogShow.ShowMsg(window, "创建隧道失败\n" + responseMessage, "创建失败");
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
                                _ = DialogShow.ShowMsg(window, error, "创建隧道失败");
                            }
                        }
                    }
                    return (null, null);
                }
            }
            else
            {
                _ = DialogShow.ShowMsg(window, "请确保输入了隧道名称", "创建失败");
                return (null, null);
            }
        }
    }

}