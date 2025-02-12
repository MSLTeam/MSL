using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MSL.utils
{
    #region MSLFRP Api
    internal class MSLFrpApi
    {
        private static readonly string ApiUrl = "https://user.mslmc.cn/api";
        public static string UserToken { get; set; }

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
                    var loginRes = JObject.Parse(res.HttpResponseContent.ToString());
                    if ((int)loginRes["code"] != 200)
                    {
                        return ((int)loginRes["code"], loginRes["msg"].ToString());
                    }
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
        private static readonly string ApiUrl = "https://of-dev-api.bfsea.xyz";
        public static string AuthId { get; set; }

        public static async Task<(int Code, string Msg)> Login(string account, string password, string authToken = null, bool save = false)
        {
            if (string.IsNullOrEmpty(authToken)) // 检测用户是否传入auth Token，若没有，就使用账户密码登录方式
            {
                // OpenFrp的API真的折磨人，这部分代码就这样了，以后再也不碰了
                // 真的是倒爷，倒过来倒过去，比我还能倒😡😡😡
                // 首先是登录第一步，传账户密码，然后获取Cookies里的17a

                JObject logininfo = new JObject
                {
                    ["user"] = account,
                    ["password"] = password
                };
                //var loginRes = await HttpService.PostAsync("https://openid.17a.ink/api/public/login", 0, logininfo, headerUAMode: 1);

                string domainUrl = "https://openid.17a.ink";
                string auth17a_name = string.Empty;
                string auth17a_token = string.Empty;

                HttpClientHandler handler = new HttpClientHandler();
                CookieContainer cookieContainer = new CookieContainer();
                handler.CookieContainer = cookieContainer;

                HttpClient httpClient = new HttpClient(handler);
                HttpResponse httpResponse = new HttpResponse();

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpContent content = new StringContent(JsonConvert.SerializeObject(logininfo), Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{MainWindow.MSLVersion}");

                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync(domainUrl + "/api/public/login", content);
                    httpResponse.HttpResponseCode = response.StatusCode;
                    httpResponse.HttpResponseContent = response.IsSuccessStatusCode
                        ? await response.Content.ReadAsStringAsync()
                        : response.ReasonPhrase;

                    Uri uri = new Uri(domainUrl);
                    var cookies = cookieContainer.GetCookies(uri);

                    foreach (Cookie cookie in cookies)
                    {
                        if (cookie.Name == "17a")
                        {
                            auth17a_name = cookie.Name;
                            auth17a_token = cookie.Value;
                            //Console.WriteLine(auth17a_name + "=" + auth17a_token);
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(auth17a_name))
                    {
                        auth17a_name = cookies[0].Name;
                        auth17a_token = cookies[0].Value;
                    }
                    //Console.WriteLine(auth17a_name + "=" + auth17a_token);
                }
                catch (Exception ex)
                {
                    httpResponse.HttpResponseCode = 0;
                    httpResponse.HttpResponseContent = ex.Message;
                }

                if (httpResponse.HttpResponseCode == HttpStatusCode.OK)
                {
                    // 获取成功后，是第二步，将17a设为cookies请求第二个接口，获取json-data里的code

                    handler = new HttpClientHandler();
                    cookieContainer = new CookieContainer();
                    handler.CookieContainer = cookieContainer;

                    httpClient = new HttpClient(handler);
                    httpResponse = new HttpResponse();

                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{MainWindow.MSLVersion}");

                    Uri uri = new Uri(domainUrl);
                    cookieContainer.Add(uri, new Cookie(auth17a_name, auth17a_token));

                    try
                    {
                        HttpResponseMessage response = await httpClient.PostAsync(domainUrl + $"/api/oauth2/authorize?response_type=code&redirect_uri={ApiUrl}/oauth_callback&client_id=openfrp", null);
                        httpResponse.HttpResponseCode = response.StatusCode;
                        httpResponse.HttpResponseContent = response.IsSuccessStatusCode
                            ? await response.Content.ReadAsStringAsync()
                            : response.ReasonPhrase;
                    }
                    catch (Exception ex)
                    {
                        httpResponse.HttpResponseCode = 0;
                        httpResponse.HttpResponseContent = ex.Message;
                    }

                    if (httpResponse.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(httpResponse.HttpResponseContent.ToString())["flag"] == true)
                    {
                        // 然后是第三步，将上述得到的code与这个地址拼接：https://of-dev-api.bfsea.xyz/oauth2/callback?code=
                        // 然后获取到返回header里的authID（到这里才真正获取到AuthID！！！！！！！！）

                        string authCode = JObject.Parse(httpResponse.HttpResponseContent.ToString())["data"]["code"].ToString();
                        HttpResponseHeaders headers = null;

                        httpClient = new HttpClient();
                        httpResponse = new HttpResponse();

                        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{MainWindow.MSLVersion}");

                        try
                        {
                            HttpResponseMessage response = await httpClient.GetAsync(ApiUrl + "/oauth2/callback?code=" + authCode);
                            headers = response.Headers;
                            httpResponse.HttpResponseCode = response.StatusCode;
                            httpResponse.HttpResponseContent = response.IsSuccessStatusCode
                                ? await response.Content.ReadAsStringAsync()
                                : response.ReasonPhrase;
                        }
                        catch (Exception ex)
                        {
                            httpResponse.HttpResponseCode = 0;
                            httpResponse.HttpResponseContent = ex.Message;
                        }
                        handler.Dispose();
                        httpClient.Dispose();


                        if (httpResponse.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(httpResponse.HttpResponseContent.ToString())["flag"] == true)
                        {
                            // 第四步，最后一步，获取返回的header里的AuthID，然后就可以正常请求API辣！！！

                            foreach (var header in headers)
                            {
                                if (header.Key == "Authorization")
                                {
                                    AuthId = header.Value.FirstOrDefault();
                                }
                            }
                        }
                        else
                        {
                            return (0, httpResponse.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(httpResponse.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
                        }
                    }
                    else
                    {
                        return (0, httpResponse.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(httpResponse.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
                    }
                }
                else
                {
                    return (0, httpResponse.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(httpResponse.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
                }
            }
            else
            {
                AuthId = authToken;
            }
            // 获取用户信息！
            try
            {
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", AuthId);
                });
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/api/getUserInfo", configureHeaders: headersAction, headerUAMode: 1);
                //Console.WriteLine(res.HttpResponseContent.ToString());
                if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
                {
                    if (save)
                    {
                        Config.Write("OpenFrpToken", AuthId);
                    }
                    return (200, (string)res.HttpResponseContent);
                }
                else
                {
                    AuthId = string.Empty;
                    return (0, res.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(res.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
                }
            }
            catch (Exception ex)
            {
                AuthId = string.Empty;
                return (0, ex.Message);
            }
        }

        public static async Task<(int Code, Dictionary<string, string> Data, string Msg)> GetUserNodes()
        {
            HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/api/getUserProxies", configureHeaders: headers =>
            {
                headers.Add("Authorization", AuthId);
            }, headerUAMode: 1);
            if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(res.HttpResponseContent.ToString());
                if (jo["data"]["list"] != null)
                {
                    JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                    foreach (JToken node in jArray)
                    {
                        if (node == null) continue;
                        if (node["proxyName"] == null) continue;
                        if (node["id"] == null) continue;
                        Nodes.Add(node["proxyName"].ToString(), node["id"].ToString());
                    }
                }
                return (200, Nodes, string.Empty);
            }
            return ((int)res.HttpResponseCode, null, res.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(res.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
        }

        public static async Task<(Dictionary<string, string>, JArray)> GetNodeList()
        {
            HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/api/getNodeList", configureHeaders: headers =>
            {
                headers.Add("Authorization", AuthId);
            }, headerUAMode: 1);
            if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(res.HttpResponseContent.ToString());
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
            return (null, null);
        }

        public static async Task<HttpResponse> GetUserInfo()
        {
            var headersAction = new Action<HttpRequestHeaders>(headers =>
            {
                headers.Add("Authorization", AuthId);
            });
            var responseMessage = await HttpService.PostAsync(ApiUrl + "/frp/api/getUserInfo", 0, string.Empty, headersAction);
            return responseMessage;
        }

        public static async Task<(bool Success, string Msg)> CreateProxy(string type, string port, bool EnableZip, int nodeid, string remote_port, string proxy_name)
        {
            var json = new JObject()
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
            };
            var res = await HttpService.PostAsync(ApiUrl + "/frp/api/newProxy", 0, json, header =>
            {
                header.Add("Authorization", AuthId);
            }, 1);

            if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
            {
                return (true, string.Empty);
            }
            else
            {
                return (false, res.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(res.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
            }
        }

        public static async Task<(bool Success, string Msg)> DeleteProxy(string id)
        {
            JObject json = new JObject()
            {
                ["proxy_id"] = id,
            };
            var res = await HttpService.PostAsync(ApiUrl + "/frp/api/removeProxy", 0, json, header =>
            {
                header.Add("Authorization", AuthId);
            }, 1);

            if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
            {
                return (true, string.Empty);
            }
            else
            {
                return (false, res.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(res.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
            }
        }
    }
    #endregion
}
