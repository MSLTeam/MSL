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
using System.Windows;

namespace MSL.utils
{
    #region MSLFRP Api
    internal class MSLFrpApi
    {
        private static readonly string ApiUrl = "https://user.mslmc.cn/api";
        public static string UserToken = string.Empty;

        public static async Task<(int Code, string Data, string Msg)> ApiGet(string route)
        {
            HttpResponse nodeRes = await HttpService.GetAsync(ApiUrl + route, headers =>
            {
                headers.Authorization = new AuthenticationHeaderValue("Bearer", UserToken);
            }, 1);

            if (nodeRes.HttpResponseCode == HttpStatusCode.OK)
            {
                JObject jobj = JObject.Parse((string)nodeRes.HttpResponseContent);
                if (jobj["code"].Value<int>() != 200)
                {
                    return ((int)jobj["code"], null, jobj["msg"].ToString());
                }
                return ((int)jobj["code"], jobj["data"].ToString(), jobj["msg"].ToString());
            }
            else
            {
                return ((int)nodeRes.HttpResponseCode, null, $"({(int)nodeRes.HttpResponseCode}){nodeRes.HttpResponseContent}");
            }
        }

        public static async Task<(int Code, string Msg)> ApiPost(string route, int contentType, object parameterData)
        {
            var headersAction = new Action<HttpRequestHeaders>(headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            });

            HttpResponse res = await HttpService.PostAsync(ApiUrl + route, contentType, parameterData, headersAction);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                var json = JObject.Parse((string)res.HttpResponseContent);
                return ((int)json["code"], (string)json["msg"]);
            }
            return ((int)res.HttpResponseCode, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
        }

        public static async Task<(int Code, string Msg)> UserLogin(string token, string email = "", string password = "", bool saveToken = false)
        {
            if (string.IsNullOrEmpty(token))
            {
                // 获取accesstoken(临时authToken)
                try
                {
                    var body = new JObject
                    {
                        ["email"] = email,
                        ["password"] = password
                    };
                    HttpResponse res = await HttpService.PostAsync(ApiUrl + "/user/login", 0, body);
                    if (res.HttpResponseCode == HttpStatusCode.OK)
                    {
                        JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                        if (JsonUserInfo["code"].Value<int>() != 200)
                        {
                            return ((int)JsonUserInfo["code"], JsonUserInfo["msg"].ToString());
                        }
                        token = JsonUserInfo["data"]["token"].ToString();
                    }
                    else
                    {
                        return ((int)res.HttpResponseCode, res.HttpResponseContent.ToString());
                    }
                }
                catch (Exception ex)
                {
                    return (0, ex.Message);
                }
            }

            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/frp/userInfo", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }, 1);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    UserToken = token;
                    if (saveToken)
                    {
                        Config.Write("MSLUserAccessToken", token);
                    }
                    return (200, (string)res.HttpResponseContent);
                }
                else
                {
                    return ((int)res.HttpResponseCode, res.HttpResponseContent.ToString());
                }
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

        // 隧道相关
        internal class TunnelInfo
        {
            // Tunnel ID
            public int ID { get; set; }
            // Tunnel Name
            public string Name { get; set; }
            // Tunnel Node(隧道节点)
            public string Node { get; set; }
            // Local IP
            public string LIP { get; set; }
            // Local Port
            public string LPort { get; set; }
            // Remote Port
            public string RPort { get; set; }
            // Tunnel Status
            public bool Online { get; set; }
        }

        public static async Task<(int Code, List<TunnelInfo> Tunnels, string Msg)> GetTunnelList()
        {
            try
            {
                // 先获取节点列表以建立ID与名称的映射
                Dictionary<int, string> nodeDictionary = new Dictionary<int, string>();

                // 获取节点列表
                HttpResponse nodeRes = await HttpService.GetAsync(ApiUrl + "/frp/nodeList", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", UserToken);
                }, 1);

                if (nodeRes.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject nodeJobj = JObject.Parse((string)nodeRes.HttpResponseContent);
                    if (nodeJobj["code"].Value<int>() != 200)
                    {
                        return ((int)nodeJobj["code"], null, "获取节点列表失败！" + nodeJobj["msg"].ToString());
                    }

                    JArray jsonNodes = (JArray)nodeJobj["data"];
                    foreach (var node in jsonNodes)
                    {
                        int nodeId = node["id"].Value<int>();
                        string nodeName = node["node"].Value<string>();
                        nodeDictionary[nodeId] = nodeName;
                    }
                }
                else
                {
                    return ((int)nodeRes.HttpResponseCode, null, $"获取节点列表失败！({(int)nodeRes.HttpResponseCode}){nodeRes.HttpResponseContent}");
                }

                // 绑定对象
                List<TunnelInfo> tunnels = [];

                // 获取隧道列表
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/frp/getTunnelList", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", UserToken);
                }, 1);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jobj_tunn = JObject.Parse((string)res.HttpResponseContent);
                    if (jobj_tunn["code"].Value<int>() != 200)
                    {
                        return ((int)jobj_tunn["code"], null, "获取隧道列表失败！" + jobj_tunn["msg"].ToString());
                    }

                    JArray JsonTunnels = (JArray)jobj_tunn["data"];
                    foreach (var item in JsonTunnels)
                    {
                        int nodeId = item["node_id"].Value<int>();

                        tunnels.Add(new TunnelInfo
                        {
                            ID = item["id"].Value<int>(),
                            Name = $"{item["name"]}",
                            Node = nodeDictionary.ContainsKey(nodeId) ? nodeDictionary[nodeId] : "未知节点",
                            LIP = $"{item["local_ip"]}",
                            LPort = $"{item["local_port"]}",
                            RPort = $"{item["remote_port"]}",
                            Online = (bool)item["status"],
                        });
                    }
                    return (200, tunnels, string.Empty);
                }
                return ((int)res.HttpResponseCode, null, $"获取隧道列表失败！({(int)res.HttpResponseCode}){res.HttpResponseContent}");
            }
            catch (Exception ex)
            {
                return (200, null, ex.Message);
            }
        }

        // 获取某个隧道的配置文件
        public static async Task<(int Code, string Msg)> GetTunnelConfig(int tunnelID)
        {
            try
            {
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });

                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/frp/getTunnelConfig?id=" + tunnelID, headersAction, 1);
                JObject json = JObject.Parse((string)res.HttpResponseContent);
                if (json["code"].Value<int>() != 200)
                {
                    return ((int)json["code"], json["msg"].ToString());
                }
                return (200, (string)json["data"]);

            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

        // 删除隧道
        public static async Task<(int Code, string Msg)> DelTunnel(int id)
        {
            try
            {
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });

                //请求body
                var body = new JObject
                {
                    ["id"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/deleteTunnel", 0, body, headersAction);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    var json = JObject.Parse((string)res.HttpResponseContent);
                    return ((int)json["code"], (string)json["msg"]);
                }
                return ((int)res.HttpResponseCode, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

        // 下面是创建隧道相关

        // 节点信息
        internal class NodeInfo
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Host { get; set; }
            public int MinPort { get; set; }
            public int MaxPort { get; set; }
            public string Remark { get; set; }
            public int Vip { get; set; }
            public string VipName { get; set; }
            public int UDP { get; set; }
            public int Status { get; set; }
            public string Band { get; set; }
        }

        public static async Task<(int Code, List<NodeInfo> NodeList, string Msg)> GetNodeList()
        {
            HttpResponse res = await HttpService.GetAsync(ApiUrl + "/frp/nodeList", headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            }, 1);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                List<NodeInfo> nodes = new List<NodeInfo>();

                JObject json = JObject.Parse((string)res.HttpResponseContent);
                if (json["code"].Value<int>() != 200)
                {
                    return ((int)json["code"], null, json["msg"].ToString());
                }

                //遍历查询
                foreach (var nodeProperty in (JArray)json["data"])
                {
                    int nodeId = (int)nodeProperty["id"];
                    JObject nodeData = (JObject)nodeProperty;
                    int vip = (int)nodeData["allow_user_group"];
                    nodes.Add(new NodeInfo
                    {
                        ID = nodeId,
                        Name = (string)nodeData["node"],
                        Host = (string)nodeData["ip"],
                        MinPort = (int)nodeData["min_open_port"],
                        MaxPort = (int)nodeData["max_open_port"],
                        Remark = (string)nodeData["remarks"],
                        Vip = vip,
                        VipName = (vip == 0 ? "普通节点" : vip == 1 ? "高级节点" : "超级节点"),
                        UDP = (int)nodeData["udp_support"],
                        Status = (int)nodeData["status"],
                        Band = (string)nodeData["bandwidth"]
                    });
                }
                var sortedNodes = nodes.OrderBy(n => n.Vip).ToList();
                return ((int)json["code"], sortedNodes, string.Empty);
            }
            return ((int)res.HttpResponseCode, null, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
        }

        public static async Task<(int Code, string Msg)> CreateTunnel(int nodeID, string tunnelName, string tunnelType, string tunnelRemark, string localIP, int localPort, int remotePort)
        {
            //请求头 token
            var headersAction = new Action<HttpRequestHeaders>(headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            });

            //请求body
            var body = new JObject
            {
                ["id"] = nodeID,
                ["name"] = tunnelName,
                ["type"] = tunnelType,
                ["remarks"] = tunnelRemark,
                ["local_ip"] = localIP,
                ["local_port"] = localPort,
                ["remote_port"] = remotePort,
            };
            HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/addTunnel", 0, body, headersAction);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                if (jsonres["code"].Value<int>() == 200)
                {
                    return (200, jsonres["msg"].ToString());
                }
                else
                {
                    return (jsonres["code"].Value<int>(), jsonres["msg"].ToString());
                }
            }
            else
            {
                return ((int)res.HttpResponseCode, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
            }
        }
    }
    #endregion



    #region OpenFrp Api
    internal class OpenFrpApi
    {
        public static string AuthId = string.Empty;

        public static Dictionary<string, string> GetUserNodes()
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                "Authorization: " + AuthId
            };
            var responseMessage = HttpService.Post("getUserProxies", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);
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
                        }
                    }
                }
                return null;
            }
        }

        public static (Dictionary<string, string>, JArray) GetNodeList(Window window)
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                "Authorization: " + AuthId
            };
            var responseMessage = HttpService.Post("getNodeList", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);

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
                            MagicShow.ShowMsgDialog(window, error, "获取用户信息失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        /*
        public static void UserSign(Window window)
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            string responseMessage = "";
            try
            {
                HttpService.Post("userSign", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);
            }
            catch
            {
                MagicShow.ShowMsgDialog(window, "签到失败！请登录OpenFrp官网进行签到！", "错误");
                return;
            }
            try
            {
                if ((bool)JObject.Parse(responseMessage)["flag"] == true && JObject.Parse(responseMessage)["msg"].ToString() == "OK")
                {
                    MagicShow.ShowMsgDialog(window, JObject.Parse(responseMessage)["data"].ToString(), "签到成功");
                }
                else
                {
                    MagicShow.ShowMsgDialog(window, "签到失败", "签到失败");
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(window, "签到失败,产生的错误:\n" + ex.Message, "签到失败");
            }
        }
        */

        public static async Task<utils.HttpResponse> GetUserInfo()
        {
            var headersAction = new Action<HttpRequestHeaders>(headers =>
            {
                headers.Add("Authorization", AuthId);
            });
            var responseMessage = await HttpService.PostAsync("https://of-dev-api.bfsea.xyz/frp/api/getUserInfo", 0, string.Empty, headersAction);
            return responseMessage;
        }

        public static async Task<string> Login(string account, string password)
        {
            HttpClient client = new HttpClient();
            JObject logininfo = new JObject
            {
                ["user"] = account,
                ["password"] = password
            };
            string json = JsonConvert.SerializeObject(logininfo);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            // 发送 POST 请求到登录 API 地址
            HttpResponseMessage loginResponse = await client.PostAsync("https://openid.17a.ink/api/public/login", content);
            // 检查响应状态码是否为 OK
            if (loginResponse.IsSuccessStatusCode)
            {
                await loginResponse.Content.ReadAsStringAsync();
                string authUrl;
                try
                {
                    WebClient webClient = new WebClient
                    {
                        Credentials = CredentialCache.DefaultCredentials
                    };
                    byte[] pageData = await webClient.DownloadDataTaskAsync("https://of-dev-api.bfsea.xyz/oauth2/login");
                    authUrl = JObject.Parse(Encoding.UTF8.GetString(pageData))["data"].ToString();
                    if (!authUrl.Contains("https://openid.17a.ink/api/") && authUrl.Contains("https://openid.17a.ink/"))
                    {
                        authUrl = authUrl.Replace("https://openid.17a.ink/", "https://openid.17a.ink/api/");
                    }
                }
                catch (Exception ex)
                {
                    return $"Get-Login-Url request failed: {ex.Message}";
                }
                HttpResponseMessage authResponse = await client.GetAsync(authUrl);
                // 检查响应状态码是否为 OK
                if (authResponse.IsSuccessStatusCode)
                {
                    // 读取响应内容
                    string authData = await authResponse.Content.ReadAsStringAsync();
                    // 显示响应内容
                    //MessageBox.Show(authData);
                    // 从响应内容中提取 code
                    AuthId = JObject.Parse(authData)["data"]["code"].ToString();

                    HttpResponseMessage _loginResponse = await client.GetAsync("https://of-dev-api.bfsea.xyz/oauth2/callback?code=" + AuthId);
                    // 检查响应状态码是否为 OK
                    if (authResponse.IsSuccessStatusCode)
                    {
                        AuthId = _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:"), _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:")).IndexOf("\n") - 1);
                        if (AuthId.Contains("Authorization: "))
                        {
                            AuthId = AuthId.Replace("Authorization: ", "");
                        }
                        //MessageBox.Show(authId);
                        var ret = await GetUserInfo();
                        if (ret.HttpResponseCode == HttpStatusCode.OK)
                        {
                            return ret.HttpResponseContent.ToString();
                        }
                        else
                        {
                            return $"Login request failed: {ret.HttpResponseCode}";
                        }

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

        public static bool CreateProxy(string type, string port, bool EnableZip, int nodeid, string remote_port, string proxy_name, out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/newProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization: " + AuthId);
            string json = JsonConvert.SerializeObject(new JObject()
            {
                ["node_id"] = nodeid,
                ["name"] = proxy_name,
                ["type"] = type,
                ["local_addr"] = "127.0.0.1",
                ["local_port"] = port,
                ["remote_port"] = remote_port,
                ["domain_bind"] = "",
                ["dataGzip"] = EnableZip,
                ["dataEncrypt"] = false,
                ["url_route"] = "",
                ["host_rewrite"] = "",
                ["request_from"] = "",
                ["request_pass"] = "",
                ["custom"] = ""
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

        public static bool DeleteProxy(string id, out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/removeProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization: " + AuthId);
            JObject json = new JObject()
            {
                ["proxy_id"] = id,
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
