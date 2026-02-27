using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
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
        private string ApiUrl { get; } = "https://api.natfrp.com/v4";
        private string UserToken { get; set; }
        private int UserLevel = 0;

        public SakuraFrp()
        {
            InitializeComponent();
        }

        private bool isInit = false;
        private async void Page_Loaded(object sender, EventArgs e)
        {
            if (!isInit)
            {
                isInit = true;
                LogHelper.Write.Info("SakuraFrp页面已加载。");
                //显示登录页面
                LoginGrid.Visibility = Visibility.Visible;
                MainCtrl.Visibility = Visibility.Collapsed;
                var token = Config.Read("SakuraFrpToken")?.ToString() ?? "";
                if (token != "")
                {
                    LogHelper.Write.Info("检测到已保存的SakuraFrp Token，尝试自动登录。");
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
            LogHelper.Write.Info($"SakuraFrp页面切换到Tab索引: {MainCtrl.SelectedIndex}");
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
            string token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入Sakura账户Token", "", true);
            if (token != null)
            {
                LogHelper.Write.Info("用户点击手动输入Token进行登录。");
                bool save = (bool)SaveToken.IsChecked;
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token.Trim(), save); //移除空格，防止笨蛋
                MagicDialog.CloseTextDialog();
            }
        }

        private async Task VerifyUserToken(string token, bool save)
        {
            LogHelper.Write.Info("开始验证SakuraFrp用户Token。");
            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/user/info?token=" + token);
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    UserToken = token;
                    if (save)
                    {
                        Config.Write("SakuraFrpToken", token);
                        LogHelper.Write.Info("用户选择保存Token，已写入配置。");
                    }

                    //显示main页面
                    LoginGrid.Visibility = Visibility.Collapsed; ;
                    MainCtrl.Visibility = Visibility.Visible;
                    JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                    UserInfo.Text = $"用户名: {JsonUserInfo["name"]}\n用户类型: {JsonUserInfo["group"]["name"]}\n限速: {JsonUserInfo["speed"]}";
                    UserLevel = int.Parse((string)JsonUserInfo["group"]["level"]);
                    LogHelper.Write.Info($"SakuraFrp用户Token验证成功。用户名: {JsonUserInfo["name"]}");
                    //获取隧道
                    await GetTunnelList();
                }
                else
                {
                    LogHelper.Write.Error($"SakuraFrp Token验证失败，HTTP状态码: {res.HttpResponseCode}");
                    if (Config.Read("SakuraFrpToken") != null)
                        Config.Remove("SakuraFrpToken");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"SakuraFrp Token验证过程中发生异常: {ex.ToString()}");
                if (Config.Read("SakuraFrpToken") != null)
                    Config.Remove("SakuraFrpToken");
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

        private async Task GetTunnelList()
        {
            LogHelper.Write.Info("开始获取SakuraFrp隧道列表。");
            try
            {
                //绑定对象
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/tunnels?token=" + UserToken);
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
                    LogHelper.Write.Info($"成功获取到 {tunnels.Count} 个隧道。");
                }
                else
                {
                    LogHelper.Write.Error($"获取隧道列表API请求失败，HTTP状态码: {res.HttpResponseCode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取SakuraFrp隧道列表时发生异常: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！" + ex.Message, "错误");
            }
        }

        /*
        //获取某个隧道的配置文件 ***疑似弃用，故注释
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
        */

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
                LogHelper.Write.Info($"尝试为隧道 '{selectedTunnel.Name}' (ID: {selectedTunnel.ID}) 生成配置文件。");
                //输出配置文件
                if (Config.WriteFrpcConfig(3, $"SakuraFrp - {selectedTunnel.Name}", $"-f {UserToken}:{selectedTunnel.ID}", "") == true)
                {
                    LogHelper.Write.Info("SakuraFrp配置文件写入成功。");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                    Window.GetWindow(this).Close();
                }
                else
                {
                    LogHelper.Write.Error("SakuraFrp配置文件写入失败。");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "配置输出失败！", "错误");
                }
            }
            else
            {
                LogHelper.Write.Warn("用户尝试生成配置文件但未选择任何隧道。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户点击刷新隧道列表。");
            (sender as Button).IsEnabled = false;
            await GetTunnelList();
            (sender as Button).IsEnabled = true;
        }

        //获取某个隧道的配置文件
        private async Task DelTunnel(string token, int id)
        {
            LogHelper.Write.Info($"尝试删除SakuraFrp隧道，ID: {id}");
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
                LogHelper.Write.Info($"隧道删除请求完成，ID: {id}，响应内容: {(string)res.HttpResponseContent}");
                await GetTunnelList();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"删除SakuraFrp隧道 (ID: {id}) 时发生异常: {ex.ToString()}");
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
                LogHelper.Write.Warn("用户尝试删除隧道但未选择任何隧道。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户退出SakuraFrp登录。");
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainCtrl.Visibility = Visibility.Collapsed;
            UserToken = null;
            Config.Remove("SakuraFrpToken");
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
            LogHelper.Write.Info("开始获取SakuraFrp节点列表。");
            try
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
                    LogHelper.Write.Info($"成功获取到 {nodes.Count} 个可用节点。");
                }
                else
                {
                    LogHelper.Write.Error($"获取节点列表API请求失败，HTTP状态码: {res.HttpResponseCode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取SakuraFrp节点列表时发生异常: {ex.ToString()}");
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

        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = NodeList;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                LogHelper.Write.Info($"尝试创建新的SakuraFrp隧道。名称: {Create_Name.Text}, 节点ID: {selectedNode.ID}, 协议: {Create_Protocol.Text}");
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
                        ["node"] = selectedNode.ID,
                        ["name"] = Create_Name.Text,
                        ["type"] = Create_Protocol.Text,
                        ["note"] = "Create By MSL",
                        ["extra"] = "",
                        ["local_ip"] = Create_LocalIP.Text,
                        ["local_port"] = Create_LocalPort.Text,
                        ["remote"] = Create_BindDomain.Text,
                    };
                    (sender as Button).IsEnabled = false;
                    HttpResponse res = await HttpService.PostAsync(ApiUrl + "/tunnels", 0, body, headersAction);
                    (sender as Button).IsEnabled = true;
                    if (res.HttpResponseCode == HttpStatusCode.Created)
                    {
                        JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                        LogHelper.Write.Info($"成功创建隧道: {jsonres["name"]} (ID: {jsonres["id"]})");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"{jsonres["name"]}隧道创建成功！\nID: {jsonres["id"]} 远程端口: {jsonres["remote"]}", "成功");
                        MainCtrl.SelectedIndex = 0;
                    }
                    else
                    {
                        LogHelper.Write.Error($"创建隧道失败，API返回码: {res.HttpResponseCode}。返回内容: {(string)res.HttpResponseContent}");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建失败！请尝试更换隧道名称/节点！", "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"创建隧道时发生异常: {ex.ToString()}");
                    (sender as Button).IsEnabled = true;
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"创建失败！发生异常: {ex.Message}", "错误");
                }
            }
            else
            {
                LogHelper.Write.Warn("用户尝试创建隧道但未选择任何节点。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何节点！", "错误");
            }
        }
    }
}