using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MeFrp.xaml 的交互逻辑
    /// </summary>
    public partial class MeFrp : Page
    {
        private string ApiUrl { get; } = "https://api.mefrp.com/api";
        private string UserToken { get; set; }

        public MeFrp()
        {
            InitializeComponent();
        }

        private bool isInit = false;
        private async void Page_Loaded(object sender, EventArgs e)
        {
            if (!isInit)
            {
                isInit = true;
                //显示登录页面
                LoginGrid.Visibility = Visibility.Visible;
                MainCtrl.Visibility = Visibility.Collapsed;
                var token = Config.Read("MeFrpToken")?.ToString() ?? "";
                if (token != "")
                {
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await VerifyUserToken(token, false); //移除空格，防止笨蛋
                    MagicDialog.CloseTextDialog();
                }
            }
        }

        private async void MainCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }
            if (!ReferenceEquals(e.OriginalSource, this.MainCtrl))
            {
                return;
            }
            switch (MainCtrl.SelectedIndex)
            {
                case 0:
                    await GetTunnelList();
                    break;
                case 1:
                    await GetNodeList();
                    Create_Name.Text = Functions.RandomString("MSL_", 6);
                    break;
            }
        }

        private async void UserTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入MeFrp账户Token", "", true);
            if (token != null)
            {
                bool save = (bool)SaveToken.IsChecked;
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token.Trim(), save); //移除空格，防止笨蛋
                MagicDialog.CloseTextDialog();
            }
        }

        private async void userPasswordLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = await MagicShow.ShowInput(Window.GetWindow(this), "请输入MeFrp账户的邮箱", "");
            if (user != null)
            {
                string password = await MagicShow.ShowInput(Window.GetWindow(this), "请输入MeFrp账户的密码", "", true);
                if (password != null)
                {
                    bool save = (bool)SaveToken.IsChecked;
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await GetUserToken(user.Trim(), password.Trim(), save); //移除空格，防止笨蛋
                    MagicDialog.CloseTextDialog();
                }

            }
        }

        private async Task GetUserToken(string user, string password, bool save)
        {
            try
            {
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/public/login", 0, new JObject
                {
                    ["username"] = user,
                    ["password"] = password
                });
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jres = JObject.Parse((string)res.HttpResponseContent);
                    if (jres["code"].Value<int>() == 200)
                    {
                        await VerifyUserToken(jres["data"]["token"].ToString(), save);
                    }
                    else
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + jres["message"], "错误");
                    }
                }
                else
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！请检查账号密码！", "错误");
                }
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + ex.Message, "错误");
            }
        }

        private async Task VerifyUserToken(string token, bool save)
        {
            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/user/info", headers =>
                {
                    headers.Add("Authorization", $"Bearer {token}");
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    UserToken = token;
                    if (save)
                    {
                        Config.Write("MeFrpToken", token);
                    }

                    //显示main页面
                    LoginGrid.Visibility = Visibility.Collapsed; ;
                    MainCtrl.Visibility = Visibility.Visible;
                    JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                    if (JsonUserInfo["data"]["todaySigned"].Value<bool>() == true)
                    {
                        SignBtn.IsEnabled = false;
                    }
                    if (JsonUserInfo["data"]["friendlyGroup"].Value<string>() != "未实名")
                    {
                        RealNameTips.Visibility = Visibility.Collapsed;
                    }
                    UserInfo.Text = $"用户名: {JsonUserInfo["data"]["username"]}\n用户类型: {JsonUserInfo["data"]["friendlyGroup"]}\n限速: {int.Parse(JsonUserInfo["data"]["outBound"]?.ToString() ?? "") / 128} Mbps\n隧道数: {JsonUserInfo["data"]["usedProxies"]} / {JsonUserInfo["data"]["maxProxies"]}\n剩余流量: {int.Parse(JsonUserInfo["data"]["traffic"]?.ToString() ?? "") / 1024} GB";
                    //UserLevel = (string)JsonUserInfo["data"]["group"];
                    //获取隧道
                    await GetTunnelList();
                }
                else
                {
                    if (Config.Read("MeFrpToken") != null)
                        Config.Remove("MeFrpToken");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                }
            }
            catch (Exception ex)
            {
                if (Config.Read("MeFrpToken") != null)
                    Config.Remove("MeFrpToken");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + ex.Message, "错误");
            }
        }

        //隧道相关
        internal class TunnelInfo
        {
            public int ID { get; set; }
            public string LPort { get; set; }
            public string RPort { get; set; }
            public string LIP { get; set; }
            public string Name { get; set; }
            public string Node { get; set; }
            public int NodeID { get; set; }
            public bool Online { get; set; }
        }

        private async Task GetTunnelList()
        {
            try
            {
                // 获取节点ID到名称的映射
                Dictionary<int, string> nodeDict = new Dictionary<int, string>();
                try
                {
                    HttpResponse nodeRes = await HttpService.GetAsync(ApiUrl + "/auth/node/nameList", headers =>
                    {
                        headers.Add("Authorization", $"Bearer {UserToken}");
                    });
                    if (nodeRes.HttpResponseCode == System.Net.HttpStatusCode.OK)
                    {
                        JObject nodeJson = JObject.Parse((string)nodeRes.HttpResponseContent);
                        JArray nodeList = (JArray)nodeJson["data"];
                        foreach (var node in nodeList)
                        {
                            int nodeId = node["nodeId"].Value<int>();
                            string name = node["name"].ToString();
                            nodeDict[nodeId] = name;
                        }
                    }
                    else
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取节点列表失败，将显示节点ID。", "警告");
                    }
                }
                catch (Exception nodeEx)
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取节点列表出错: " + nodeEx.Message, "警告");
                }

                // 获取隧道列表
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;

                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/proxy/list", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JObject keyValuePairs = JObject.Parse((string)res.HttpResponseContent);
                    JArray JsonTunnels = (JArray)keyValuePairs["data"];
                    foreach (var item in JsonTunnels)
                    {
                        int nodeId = item["nodeId"].Value<int>();
                        string nodeName = nodeDict.TryGetValue(nodeId, out var name) ? name : nodeId.ToString();

                        tunnels.Add(new TunnelInfo
                        {
                            Name = item["proxyName"].ToString(),
                            NodeID = nodeId,
                            Node = nodeName, // 显示节点名称或ID
                            ID = item["proxyId"].Value<int>(),
                            LIP = item["localIp"].ToString(),
                            LPort = item["localPort"].ToString(),
                            RPort = item["remotePort"].ToString(),
                            Online = item["isOnline"].Value<bool>(),
                        });
                    }
                }
                else
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！HTTP状态码: " + res.HttpResponseCode, "错误");
                }
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败: " + ex.Message, "错误");
            }
        }

        //显示隧道信息
        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                TunnelInfo_Text.Text = $"隧道名: {selectedTunnel.Name}" +
                    $"\n隧道ID: {selectedTunnel.ID}" + $"\n远程端口: {selectedTunnel.RPort}" + $"\n隧道状态: {(selectedTunnel.Online ? "在线" : "离线")}";
                LocalIp.Text = selectedTunnel.LIP;
                LocalPort.Text = selectedTunnel.LPort;
            }
        }

        //确定 输出config
        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                //输出配置文件
                if (Config.WriteFrpcConfig(4, $"MEFrp - {selectedTunnel.Name}", $"-t {UserToken} -p {selectedTunnel.ID}", "") == true)
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                    Window.GetWindow(this).Close();
                }
                else
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "配置输出失败！", "错误");
                }
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            await GetTunnelList();
            (sender as Button).IsEnabled = true;
        }

        //获取某个隧道的配置文件
        private async Task DelTunnel(string token, int id)
        {
            try
            {
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {token}");
                });

                //请求body
                var body = new JObject
                {
                    ["proxyId"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/auth/proxy/delete", 0, body, headersAction);
                //MessageBox.Show((string)res.HttpResponseContent);
                await GetTunnelList();
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "删除失败！" + ex.Message, "错误");
            }
        }

        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var listBox = FrpList;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                await DelTunnel(UserToken, selectedTunnel.ID);
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainCtrl.Visibility = Visibility.Collapsed;
            UserToken = null;
            Config.Remove("MeFrpToken");
        }

        //下面是创建隧道相关

        internal class NodeInfo
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Host { get; set; }
            public string Description { get; set; }
            public int Vip { get; set; }
            public string VipName { get; set; }
            public int Flag { get; set; }
            public string Band { get; set; }
        }

        private async Task GetNodeList()
        {
            HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/node/list", headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            });
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
                NodeList.ItemsSource = nodes;
                JObject json = JObject.Parse((string)res.HttpResponseContent);

                //遍历查询
                foreach (var nodeProperty in (JArray)json["data"])
                {
                    JObject nodeData = (JObject)nodeProperty;

                    nodes.Add(new NodeInfo
                    {
                        ID = int.Parse((string)nodeData["nodeId"]),
                        Name = (string)nodeData["name"],
                        Host = (string)nodeData["hostname"],
                        Description = (string)nodeData["description"],
                    });
                }
            }
        }

        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = NodeList;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                NodeTips.Text = (selectedNode.Description == "" ? "节点没有备注" : selectedNode.Description);
            }
            Create_RemotePort.Text = Functions.GenerateRandomNumber(10000, 65535).ToString();
        }

        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var listBox = NodeList;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });

                //请求body
                var body = new JObject
                {
                    ["accessKey"] = "",
                    ["headerXFromWhere"] = "",
                    ["hostHeaderRewrite"] = "",
                    ["proxyProtocolVersion"] = "",
                    ["nodeId"] = selectedNode.ID,
                    ["proxyName"] = Create_Name.Text,
                    ["proxyType"] = Create_Protocol.Text,
                    ["localIp"] = Create_LocalIP.Text,
                    ["localPort"] = int.Parse(Create_LocalPort.Text),
                    ["remotePort"] = int.Parse(Create_RemotePort.Text),
                    ["domain"] = Create_BindDomain.Text,
                    ["useCompression"] = false,
                    ["useEncryption"] = false,
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/auth/proxy/create", 0, body, headersAction);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"{jsonres["name"]}隧道创建成功！\n远程端口: {Create_RemotePort.Text}", "成功");
                    MainCtrl.SelectedIndex = 0;
                }
                else
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建失败！请尝试更换隧道名称/节点！", "错误");
                }
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何节点！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void SignBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/user/info", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"签到成功！\n获得流量: {jsonres["data"]["extraTraffic"]} G", "签到成功");
                    SignBtn.IsEnabled = false;
                }
            }
            catch
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "签到失败！", "错误");
            }
        }


    }
}
