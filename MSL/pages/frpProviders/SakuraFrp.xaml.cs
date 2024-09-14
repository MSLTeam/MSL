using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
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
    /// SakuraFrp.xaml 的交互逻辑
    /// </summary>
    public partial class SakuraFrp : Page
    {
        string ApiUrl = "https://api.natfrp.com/v4";
        string UserToken = null;
        int UserLevel = 0;

        public SakuraFrp()
        {
            InitializeComponent();
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Collapsed;
            var token = Config.Read("SakuraFrpToken")?.ToString() ?? "";
            if (token != "")
            {
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token, false); //移除空格，防止笨蛋
                MagicDialog.CloseTextDialog();
            }
        }

        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入Sakura账户Token", "", true);
            if (token != null)
            {
                bool save = (bool)SaveToken.IsChecked;
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token.Trim(), save); //移除空格，防止笨蛋
                MagicDialog.CloseTextDialog();
            }
        }

        private async Task VerifyUserToken(string token, bool save)
        {
            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/user/info?token=" + token);
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    UserToken = token;
                    if (save)
                    {
                        Config.Write("SakuraFrpToken", token);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        //显示main页面
                        LoginGrid.Visibility = Visibility.Collapsed; ;
                        MainGrid.Visibility = Visibility.Visible;
                        CreateGrid.Visibility = Visibility.Collapsed;
                    });
                    JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                    Dispatcher.Invoke(() =>
                    {
                        UserInfo.Text = $"用户名: {JsonUserInfo["name"]}\n用户类型: {JsonUserInfo["group"]["name"]}\n限速: {JsonUserInfo["speed"]}";
                    });
                    UserLevel = int.Parse((string)JsonUserInfo["group"]["level"]);
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
                //绑定对象
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/tunnels?token=" + token);
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JArray JsonTunnels = JArray.Parse((string)res.HttpResponseContent);
                    foreach (var item in JsonTunnels)
                    {
                        tunnels.Add(new TunnelInfo
                        {
                            Name = $"{item["name"]}", //隧道名字
                            Node = $"{item["node"]}", //节点id
                            ID = item["id"].Value<int>(), //隧道id
                            LIP = $"{item["local_ip"]}", //本地ip
                            LPort = $"{item["local_port"]}", //本地端口
                            RPort = $"{item["remote"]}", //远程端口
                            Online = (bool)item["online"], //在线吗亲
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！" + ex.Message, "错误");
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

                //请求body
                var body = new JObject
                {
                    ["query"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/tunnel/config", 0, body, headersAction);
                return (string)res.HttpResponseContent;

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
                //string content = await Task.Run(() => GetTunnelConfig(UserToken,selectedTunnel.ID));
                //输出配置文件
                if (Config.WriteFrpcConfig(3, $"SakuraFrp - {selectedTunnel.Name}", $"-f {UserToken}:{selectedTunnel.ID}","") == true)
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

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GetTunnelList(UserToken));
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
                    ["ids"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/tunnel/delete", 0, body, headersAction);
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
            Process.Start("https://www.natfrp.com/user/");
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Collapsed;
            UserToken = null;
            Config.Write("SakuraFrpToken", "");
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
            HttpResponse res = await HttpService.GetAsync(ApiUrl + "/nodes?token=" + UserToken);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
                NodeList.ItemsSource = nodes;
                JObject json = JObject.Parse((string)res.HttpResponseContent);

                //遍历查询
                foreach (var nodeProperty in json.Properties())
                {
                    int nodeId = int.Parse(nodeProperty.Name);
                    JObject nodeData = (JObject)nodeProperty.Value;
                    if (UserLevel >= (int)nodeData["vip"])
                    {
                        nodes.Add(new NodeInfo
                        {
                            ID = nodeId,
                            Name = (string)nodeData["name"],
                            Host = (string)nodeData["host"],
                            Description = (string)nodeData["description"],
                            Vip = (int)nodeData["vip"],
                            VipName = ((int)nodeData["vip"] == 0 ? "普通节点" : ((int)nodeData["vip"] == 3 ? "青铜节点" : "白银节点")),
                            Flag = (int)nodeData["flag"],
                            Band = (string)nodeData["band"]
                        });
                    }

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
                    ["node"] = selectedNode.ID,
                    ["name"] = Create_Name.Text,
                    ["type"] = Create_Protocol.Text,
                    ["note"] = "Create By MSL",
                    ["extra"] = "",
                    ["local_ip"] = Create_LocalIP.Text,
                    ["local_port"] = Create_LocalPort.Text,
                    ["remote"] = Create_BindDomain.Text,
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/tunnels", 0, body, headersAction);
                if (res.HttpResponseCode == HttpStatusCode.Created)
                {
                    JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"{jsonres["name"]}隧道创建成功！\nID: {jsonres["id"]} 远程端口: {jsonres["remote"]}", "成功");
                    //显示main页面
                    LoginGrid.Visibility = Visibility.Collapsed; ;
                    MainGrid.Visibility = Visibility.Visible;
                    CreateGrid.Visibility = Visibility.Collapsed;
                    await GetTunnelList(UserToken);
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
            Process.Start("https://www.natfrp.com/user/profile");
        }
    }
}
