using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MSL.utils
{
    #region MSLFRP Api
    internal class MSLFrpApi
    {
        private static string ApiUrl { get; } = "https://user.mslmc.net/api";
        public static string UserToken { get; set; }

        public static async Task<(int Code, JToken Data, string Msg)> ApiGet(string route)
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
                return ((int)jobj["code"], jobj["data"]?.Type == JTokenType.Null ? null : jobj["data"], jobj["msg"]?.ToString());
            }
            else
            {
                return ((int)nodeRes.HttpResponseCode, null, $"({(int)nodeRes.HttpResponseCode}){nodeRes.HttpResponseContent}");
            }
        }

        public static async Task<(int Code, JToken Data, string Msg)> ApiPost(string route, int contentType, object parameterData)
        {
            var headersAction = new Action<HttpRequestHeaders>(headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            });

            HttpResponse res = await HttpService.PostAsync(ApiUrl + route, contentType, parameterData, headersAction);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                var json = JObject.Parse((string)res.HttpResponseContent);
                return ((int)json["code"], json["data"]?.Type == JTokenType.Null ? null : json["data"], json["msg"]?.ToString());
            }
            return ((int)res.HttpResponseCode, null, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
        }

        public static async Task<(int Code, string Msg, string UserInfo)> UserLogin(string token, string email = "", string password = "", string auth2fa = "", bool saveToken = false)
        {
            if (string.IsNullOrEmpty(token))
            {
                // 发送邮箱和密码，请求登录，获取MSL-User-Token
                try
                {
                    JObject body;
                    if (string.IsNullOrEmpty(auth2fa))
                    {
                        body = new JObject
                        {
                            ["email"] = email,
                            ["password"] = password
                        };
                    }
                    else
                    {
                        body = new JObject
                        {
                            ["email"] = email,
                            ["password"] = password,
                            ["twoFactorAuthKey"] = auth2fa
                        };
                    }
                    HttpResponse res = await HttpService.PostAsync(ApiUrl + "/user/login", 0, body);
                    if (res.HttpResponseCode == HttpStatusCode.OK)
                    {
                        JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                        if (JsonUserInfo["code"].Value<int>() != 200)
                        {
                            return ((int)JsonUserInfo["code"], JsonUserInfo["msg"].ToString(), string.Empty);
                        }
                        token = JsonUserInfo["data"]["token"].ToString();
                    }
                    else
                    {
                        return ((int)res.HttpResponseCode, res.HttpResponseContent.ToString(), string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    return (0, ex.Message, string.Empty);
                }
            }

            try
            {
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/frp/userInfo", headersAction, 1);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    var loginRes = JObject.Parse(res.HttpResponseContent.ToString());
                    if ((int)loginRes["code"] != 200)
                    {
                        if (Config.Read("MSLUserAccessToken") != null)
                            Config.Remove("MSLUserAccessToken");
                        return ((int)loginRes["code"], loginRes["msg"].ToString(), string.Empty);
                    }
                    UserToken = token;
                    if (saveToken)
                    {
                        Config.Write("MSLUserAccessToken", token);
                    }

                    // 用户登陆成功后，发送POST请求续期Token
                    _ = await HttpService.PostAsync(ApiUrl + "/user/renewToken", 3, configureHeaders: headersAction, headerUAMode: 1);
                    return (200, string.Empty, (string)res.HttpResponseContent);
                }
                else
                {
                    if (Config.Read("MSLUserAccessToken") != null)
                        Config.Remove("MSLUserAccessToken");
                    return ((int)res.HttpResponseCode, res.HttpResponseContent.ToString(), string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (0, ex.Message, string.Empty);
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
            public int KCP { get; set; }
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
                        KCP = (int)nodeData["kcp_support"],
                        Status = (int)nodeData["status"],
                        Band = (string)nodeData["bandwidth"]
                    });
                }
                var sortedNodes = nodes.OrderBy(n => n.Vip).ToList();
                return ((int)json["code"], sortedNodes, string.Empty);
            }
            return ((int)res.HttpResponseCode, null, $"({(int)res.HttpResponseCode}){res.HttpResponseContent}");
        }

        public static async Task<(int Code, string Msg)> CreateTunnel(int nodeID, string tunnelName, string tunnelType, string tunnelRemark, string localIP, int localPort, int remotePort, bool useKcp)
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
                ["use_kcp"] = useKcp,
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
        private static string ApiUrl { get; } = "https://of-dev-api.bfsea.xyz";
        public static string AuthId { get; set; }

        public static async Task<(int Code, string Msg)> Login(string authToken = null, bool save = false)
        {
            AuthId = authToken;
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
            try
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
                            Nodes.Add(node["proxyName"].ToString(), node["id"].ToString());
                        }
                    }
                    return (200, Nodes, string.Empty);
                }
                return ((int)res.HttpResponseCode, null, res.HttpResponseCode == HttpStatusCode.OK ? JObject.Parse(res.HttpResponseContent.ToString())["msg"].ToString() : string.Empty);
            }
            catch (Exception ex)
            {
                return (0, null, ex.Message);
            }
        }

        internal class NodeInfo
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public ObservableCollection<TagItem> Tags { get; set; }
            public string Host { get; set; }
            public (int,int) AllowPorts { get; set; }
            public string Remark { get; set; }
            public JObject Protocol { get; set; }
            public string Band { get; set; }
        }

        internal class TagItem
        {
            public string Text { get; set; }
            public bool IsStatusTag { get; set; }
            public int StatusCode { get; set; }
        }

        public static async Task<(bool Flag, List<NodeInfo>)> GetNodeList()
        {
            try
            {
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/frp/api/getNodeList", configureHeaders: headers =>
                {
                    headers.Add("Authorization", AuthId);
                }, headerUAMode: 1);
                if (res.HttpResponseCode == HttpStatusCode.OK && (bool)JObject.Parse(res.HttpResponseContent.ToString())["flag"] == true)
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(res.HttpResponseContent.ToString());
                    var jArray = JArray.Parse(jo["data"]["list"].ToString());
                    List<NodeInfo> nodeInfos = new List<NodeInfo>();
                    foreach (var node in jArray)
                    {
                        nodeInfos.Add(new NodeInfo
                        {
                            ID = node["id"].Value<int>(),
                            Name = node["name"].Value<string>(),
                            Host = node["hostname"].Value<string>(),
                            AllowPorts = string.IsNullOrEmpty(node["allowPort"].ToString()) ? (10000, 99999) : 
                                (int.Parse(node["allowPort"].Value<string>().Trim('(', ')', ' ').Split(',')[0]),
                                int.Parse(node["allowPort"].Value<string>().Trim('(', ')', ' ').Split(',')[1])),
                            Remark = node["description"].Value<string>(),
                            Protocol = (JObject)node["protocolSupport"],
                            Band = node["bandwidth"].Value<string>(),
                            Tags = [.. new[]
                            { new TagItem
                                {
                                    Text = node["status"].Value<int>() == 200 ? "在线" : "离线",
                                    IsStatusTag = true,
                                    StatusCode = node["status"].Value<int>()
                                }
                            }.Concat(
                                node["group"].ToString().Split([';'],
                                StringSplitOptions.RemoveEmptyEntries).Select(
                                    s => s.Trim()).Where(s => s != "admin" && s != "dev").Select(
                                    s => new TagItem
                                    {
                                        Text = s,
                                        IsStatusTag = false,
                                        StatusCode = node["status"].Value<int>()
                                    })
                                )
                            ]
                        });
                        /*
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
                        */
                    }
                    return (true, nodeInfos);
                }
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
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
