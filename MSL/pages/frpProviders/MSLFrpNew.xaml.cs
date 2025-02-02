using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MSLFrpNew.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrpNew : Page
    {

        string ApiUrl = "https://user.mslmc.cn";
        string UserToken = null;
        int UserLevel = 0;

        public MSLFrpNew()
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
                MainGrid.Visibility = Visibility.Collapsed;
                CreateGrid.Visibility = Visibility.Collapsed;
                var token = Config.Read("MSLUserAccessToken")?.ToString() ?? "";
                if (token != "")
                {
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await VerifyUserToken(token, false); //移除空格，防止笨蛋
                    MagicDialog.CloseTextDialog();
                }
            }
        }

        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = await MagicShow.ShowInput(Window.GetWindow(this), "请输入MSL账户的邮箱", "", false);
            if (email != null)
            {
                string password = await MagicShow.ShowInput(Window.GetWindow(this), "请输入MSL账户的密码", "", true);
                if (password != null)
                {
                    bool save = (bool)SaveToken.IsChecked;
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await VerifyUserToken(null, save, email, password); //移除空格，防止笨蛋
                    MagicDialog.CloseTextDialog();
                }

            }
        }

        private async Task VerifyUserToken(string token, bool save, string email = "", string password = "")
        {
            if (token == null)
            {
                //获取accesstoken
                try
                {
                    var body = new JObject
                    {
                        ["email"] = email,
                        ["password"] = password
                    };
                    HttpResponse res = await HttpService.PostAsync(ApiUrl + "/api/user/login", 0, body);
                    if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                    {

                        JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                        if (JsonUserInfo["code"].Value<int>() != 200)
                        {
                            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + JsonUserInfo["msg"], "错误");
                            return;
                        }
                        token = (string)JsonUserInfo["data"]["token"];
                        if (save)
                        {
                            Config.Write("MSLUserAccessToken", token);
                        }
                    }
                    else
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + ex.Message, "错误");
                    return;
                }
            }

            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/api/frp/userInfo", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    UserToken = token;

                    Dispatcher.Invoke(() =>
                    {
                        //显示main页面
                        LoginGrid.Visibility = Visibility.Collapsed; ;
                        MainGrid.Visibility = Visibility.Visible;
                        CreateGrid.Visibility = Visibility.Collapsed;
                    });
                    JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                    UserLevel = Functions.GetCurrentUnixTimestamp() < (long)JsonUserInfo["data"]["outdated"] ? int.Parse((string)JsonUserInfo["data"]["user_group"]) : 0;
                    string userGroup = UserLevel == 6 ? "超级管理员" : UserLevel == 0 ? "普通用户" : "赞助用户";
                    Dispatcher.Invoke(() =>
                    {
                        UserInfo.Text = $"用户名: {JsonUserInfo["data"]["name"]}\n用户组: {userGroup}\n到期时间: {Functions.ConvertUnixTimeSeconds((long)JsonUserInfo["data"]["outdated"])}";
                    });

                    //获取隧道
                    await GetTunnelList(token);
                }
                else
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                }
            }
            catch (Exception ex)
            {
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
            public bool Online { get; set; }
        }

        private async Task GetTunnelList(string token)
        {
            try
            {
                // 先获取节点列表以建立ID与名称的映射
                Dictionary<int, string> nodeDictionary = new Dictionary<int, string>();

                // 获取节点列表
                HttpResponse nodeRes = await HttpService.GetAsync(ApiUrl + "/api/frp/nodeList", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });

                if (nodeRes.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JObject nodeJobj = JObject.Parse((string)nodeRes.HttpResponseContent);
                    if (nodeJobj["code"].Value<int>() != 200)
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取节点列表失败！" + nodeJobj["msg"], "错误");
                        return;
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
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取节点列表失败！HTTP状态码：" + nodeRes.HttpResponseCode, "错误");
                    return;
                }

                // 绑定对象
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;

                // 获取隧道列表
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/api/frp/getTunnelList", headers =>
                {
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JObject jobj_node = JObject.Parse((string)res.HttpResponseContent);
                    if (jobj_node["code"].Value<int>() != 200)
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！" + jobj_node["msg"], "错误");
                        return;
                    }

                    JArray JsonTunnels = (JArray)jobj_node["data"];
                    foreach (var item in JsonTunnels)
                    {
                        int nodeId = item["node_id"].Value<int>();

                        tunnels.Add(new TunnelInfo
                        {
                            Name = $"{item["name"]}", //隧道名字
                            Node = nodeDictionary.ContainsKey(nodeId) ? nodeDictionary[nodeId] : "未知节点", // 使用节点名称
                            ID = item["id"].Value<int>(), //隧道id
                            LIP = $"{item["local_ip"]}", //本地ip
                            LPort = $"{item["local_port"]}", //本地端口
                            RPort = $"{item["remote_port"]}", //远程端口
                            Online = (bool)item["status"], //在线状态
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "操作失败！" + ex.Message, "错误");
            }
        }

        //获取某个隧道的配置文件
        private async Task<string> GetTunnelConfig(string token, int id)
        {
            try
            {
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {token}");
                });

                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/api/frp/getTunnelConfig?id=" + id, headersAction);
                JObject json = JObject.Parse((string)res.HttpResponseContent);
                if (json["code"].Value<int>() != 200)
                {
                    return "MSL-ERR:" + json["msg"];
                }
                return (string)json["data"];

            }
            catch (Exception ex)
            {
                return "MSL-ERR:" + ex.Message;
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
                string content = await Task.Run(() => GetTunnelConfig(UserToken, selectedTunnel.ID));
                //输出配置文件
                if (Config.WriteFrpcConfig(0, $"MSLFrp(NEW) - {selectedTunnel.Name}", content) == true)
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
            await GetTunnelList(UserToken);
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
                    ["id"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/api/frp/deleteTunnel", 0, body, headersAction);
                //MessageBox.Show((string)res.HttpResponseContent);
                await GetTunnelList(UserToken);
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "删除失败！" + ex.Message, "错误");
            }
        }

        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                await DelTunnel(UserToken, selectedTunnel.ID);
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }

        }

        private void OpenWeb_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://user.mslmc.cn");
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Collapsed;
            UserToken = null;
            Config.Write("MSLUserAccessToken", "");
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
        private async void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            //显示create页面
            LoginGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Visible;

            await GetNodeList();
            Create_Name.Text = Functions.RandomString("MSL_", 6);
        }

        private async Task GetNodeList()
        {
            HttpResponse res = await HttpService.GetAsync(ApiUrl + "/api/frp/nodeList", headers =>
            {
                headers.Add("Authorization", $"Bearer {UserToken}");
            });
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
                NodeList.ItemsSource = nodes;
                JObject json = JObject.Parse((string)res.HttpResponseContent);
                if (json["code"].Value<int>() != 200)
                {
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取节点列表失败！" + json["msg"], "错误");
                    return;
                }

                //遍历查询
                foreach (var nodeProperty in (JArray)json["data"])
                {
                    int nodeId = (int)nodeProperty["id"];
                    JObject nodeData = (JObject)nodeProperty;
                    //if (UserLevel >= (int)nodeData["allow_user_group"])
                    //{
                    nodes.Add(new NodeInfo
                    {
                        ID = nodeId,
                        Name = (string)nodeData["node"],
                        Host = (string)nodeData["ip"],
                        Description = (string)nodeData["remarks"],
                        Vip = (int)nodeData["allow_user_group"],
                        VipName = ((int)nodeData["allow_user_group"] == 0 ? "普通节点" : ((int)nodeData["allow_user_group"] == 1 ? "付费节点" : "超级节点")),
                        //Flag = (int)nodeData["flag"],
                        Band = (string)nodeData["bandwidth"]
                    });
                    //}

                }
            }
        }

        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = NodeList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                NodeTips.Text = (selectedNode.Description == "" ? "节点没有备注" : selectedNode.Description) + "\n节点带宽: " + selectedNode.Band;
            }
        }

        private void Create_BackBtn_Click(object sender, RoutedEventArgs e)
        {
            //显示main页面
            LoginGrid.Visibility = Visibility.Collapsed; ;
            MainGrid.Visibility = Visibility.Visible;
            CreateGrid.Visibility = Visibility.Collapsed;
        }

        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Create_RemotePort.Text == "")
            {
                Create_RemotePort.Text = Functions.GenerateRandomNumber(10000, 60000).ToString();
            }
            var listBox = NodeList as System.Windows.Controls.ListBox;
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
                    ["id"] = selectedNode.ID,
                    ["name"] = Create_Name.Text,
                    ["type"] = Create_Protocol.Text,
                    ["remarks"] = "Create By MSL Client",
                    ["local_ip"] = Create_LocalIP.Text,
                    ["local_port"] = Create_LocalPort.Text,
                    ["remote_port"] = Create_RemotePort.Text,
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/api/frp/addTunnel", 0, body, headersAction);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                    if (jsonres["code"].Value<int>() == 200)
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"{Create_Name.Text}隧道创建成功！\n 远程端口: {Create_RemotePort.Text}", "成功");
                        //显示main页面
                        LoginGrid.Visibility = Visibility.Collapsed; ;
                        MainGrid.Visibility = Visibility.Visible;
                        CreateGrid.Visibility = Visibility.Collapsed;
                        await GetTunnelList(UserToken);
                    }
                    else
                    {
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建失败！" + jsonres["msg"], "错误");
                    }

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
        }

        private void userRegister_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://user.mslmc.cn");
        }
    }
}
