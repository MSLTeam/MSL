using MSL.langs;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace MSL.pages.frpProviders
{
    /// <summary>
    /// ChmlFrp.xaml 的交互逻辑
    /// </summary>
    public partial class ChmlFrp : Page
    {
        private readonly string ChmlFrpApiUrl = "https://cf-v1.uapis.cn";
        private string ChmlToken, ChmlID;

        public ChmlFrp()
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
                MainCtrl.Visibility = Visibility.Collapsed;
                LoginGrid.Visibility = Visibility.Visible;
                //自动登录
                var token = Config.Read("ChmlToken")?.ToString() ?? "";
                if (token != "")
                {
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await VerifyUserToken(token, false);
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
                    await GetFrpList();
                    break;
                case 1:
                    await GetNodeList();
                    //随机一些数据
                    Random rand = new Random();
                    int randomNumber = rand.Next(10000, 65536);
                    Create_RemotePort.Text = randomNumber.ToString();
                    Create_Name.Text = Functions.RandomString("MSL_", 5);
                    break;
            }
        }

        //使用token登录
        private async void UserTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token;
            token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入Chml账户Token", "", true);
            if (token != null)
            {
                bool save = (bool)SaveToken.IsChecked;
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token.Trim(), save);
                MagicDialog.CloseTextDialog();
            }
        }

        //账号密码
        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            string frpUser, frpPassword;
            frpUser = await MagicShow.ShowInput(Window.GetWindow(this), "请输入ChmlFrp的账户名/邮箱/QQ号");
            if (frpUser == null)
            {
                return;
            }
            frpPassword = await MagicShow.ShowInput(Window.GetWindow(this), "请输入密码", "", true);
            if (frpPassword == null)
            {
                return;
            }
            bool save = (bool)SaveToken.IsChecked;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            await GetUserToken(frpUser, frpPassword, save);
            MagicDialog.CloseTextDialog();
        }

        //异步登录，获取到用户token
        private async Task GetUserToken(string user, string pwd, bool save)
        {
            try
            {
                string response = (await HttpService.PostAsync($"{ChmlFrpApiUrl}/api/login.php", 2, $"username={user}&password={pwd}")).HttpResponseContent.ToString();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (jsonResponse.ContainsKey("code"))
                {
                    if (jsonResponse["code"].ToString() == "200")
                    {
                        string token = jsonResponse["token"].ToString();
                        ChmlID = jsonResponse["userid"].ToString();//id丢全局
                        if (save == true) //保存？写到配置
                        {
                            Config.Write("ChmlToken", token);
                        }
                        ChmlToken = token;
                        MainCtrl.Visibility = Visibility.Visible;
                        LoginGrid.Visibility = Visibility.Collapsed;
                        await GetFrpList();
                        return;
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "登陆失败！" + jsonResponse["message"].ToString(), LanguageManager.Instance["Error"]);
                    }
                }
                else
                {
                    if (jsonResponse.ContainsKey("error"))
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "登陆失败！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Error"]);
                    }
                }
            }
            catch (Exception e)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登陆失败！\n" + e.Message, LanguageManager.Instance["Error"]);
            }
            if (Config.Read("ChmlToken") != null)
                Config.Remove("ChmlToken");
        }

        //直接token登录，那么验证下咯~
        private async Task VerifyUserToken(string userToken, bool save)
        {
            try
            {
                string response = (await HttpService.GetAsync($"{ChmlFrpApiUrl}/api/userinfo.php?usertoken={userToken}")).HttpResponseContent.ToString();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (jsonResponse.ContainsKey("userid"))
                {
                    ChmlID = jsonResponse["userid"].ToString();//id丢全局
                    if (save == true) //保存？写到配置
                    {
                        Config.Write("ChmlToken", userToken);
                    }
                    ChmlToken = userToken;
                    MainCtrl.Visibility = Visibility.Visible;
                    LoginGrid.Visibility = Visibility.Collapsed;
                    await GetFrpList();
                    return;

                }
                else
                {
                    if (jsonResponse.ContainsKey("error"))
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "Token登陆失败！\n可以尝试账号密码登录！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Error"]);
                    }
                }
            }
            catch (Exception e)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "Token登陆失败！\n可以尝试账号密码登录！\n" + e.Message, LanguageManager.Instance["Error"]);
            }
            if (Config.Read("ChmlToken") != null)
                Config.Remove("ChmlToken");
        }

        internal class TunnelInfo
        {
            public string ID { get; set; }
            public string Type { get; set; }
            public string LPort { get; set; }
            public string RPort { get; set; }
            public string LIP { get; set; }
            public string Name { get; set; }
            public string Node { get; set; }
            public string Addr { get; set; }
            public string Token { get; set; }
            public string Encryption { get; set; }
            public string Compression { get; set; }
        }

        //登录成功了，然后就是获取隧道
        private async Task GetFrpList()
        {
            try
            {
                //获取userinfo
                var jsonUserInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>
                    ((await HttpService.GetAsync($"{ChmlFrpApiUrl}/api/userinfo.php?usertoken={ChmlToken}")).HttpResponseContent.ToString());

                UserInfo.Text = $"用户：#{jsonUserInfo["userid"]} {jsonUserInfo["username"]}\n" +
                $"邮箱：{jsonUserInfo["email"]}\n" +
                $"会员类型：{jsonUserInfo["usergroup"]}\n" +
                $"隧道数：{jsonUserInfo["tunnelstate"]}/{jsonUserInfo["tunnel"]}";

                //获取隧道
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;
                string response = (await HttpService.GetAsync($"{ChmlFrpApiUrl}/api/usertunnel.php?token={ChmlToken}")).HttpResponseContent.ToString();
                try
                {
                    var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                    foreach (var item in jsonArray)
                    {
                        tunnels.Add(new TunnelInfo
                        {
                            Name = $"{item["name"]}",
                            Node = $"{item["node"]}",
                            ID = $"{item["id"]}",
                            Type = $"{item["type"]}",
                            LIP = $"{item["localip"]}",
                            LPort = $"{item["nport"]}",
                            RPort = $"{item["dorp"]}",
                            //ip为什么会没有呢
                            Addr = item.ContainsKey("ip") ? $"{item["ip"]}" : string.Empty, //ip没的时候 empty！
                            Token = ChmlToken,
                            Compression = $"{item["compression"]}",
                            Encryption = $"{item["encryption"]}"
                        });
                    }

                }
                catch (JsonSerializationException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(ex.Message, "错误");
                }
            }
            catch (Exception e)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), e.Message, "出错了！");
            }
        }

        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                TunnelInfo_Text.Text = $"隧道名:{selectedTunnel.Name}\n" +
                    $"隧道ID:{selectedTunnel.ID}\n协议:{selectedTunnel.Type}\n" +
                    $"地域:{selectedTunnel.Node}\n远程端口:{selectedTunnel.RPort}";
                LocalIp.Text = selectedTunnel.LIP;
                LocalPort.Text = selectedTunnel.LPort;
            }
        }

        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            string FrpcConfig, FrpsPort = "7000", FrpsToken = "ChmlFrpToken";
            var listBox = FrpList;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                try
                {
                    (sender as Button).IsEnabled = false;
                    //获取frps端口
                    JArray frps = JArray.Parse((await HttpService.GetAsync(ChmlFrpApiUrl + "/api/unode.php")).HttpResponseContent.ToString());
                    foreach (JObject frp in frps.Cast<JObject>())
                    {
                        if (frp.ContainsKey("name"))
                        {
                            if (frp["name"].ToString() == selectedTunnel.Node)
                            {
                                FrpsPort = frp["port"].ToString();
                                FrpsToken = frp["nodetoken"].ToString();
                                break;
                            }
                        }
                    }
                    (sender as Button).IsEnabled = true;

                    //根据类型选择rport
                    string conf_rport;
                    switch (selectedTunnel.Type)
                    {
                        case "http":
                            conf_rport = "80";
                            break;
                        case "https":
                            conf_rport = "443";
                            break;
                        default:
                            conf_rport = selectedTunnel.RPort;
                            break;

                    }
                    //输出配置文件
                    Uri host = new Uri("http://" + selectedTunnel.Addr);
                    FrpcConfig = $"[common]\r\nserver_addr = {host.Host}\r\n" +
                        $"server_port = {FrpsPort}\r\ntcp_mux = true\r\nprotocol = tcp\r\n" +
                        $"user = {selectedTunnel.Token}\r\ntoken = {FrpsToken}\r\n" +
                        $"dns_server = 223.6.6.6\r\ntls_enable = false\r\n" +
                        $"[{selectedTunnel.Name}]\r\nprivilege_mode = true\r\n" +
                        $"type = {selectedTunnel.Type}\r\nlocal_ip = {LocalIp.Text}\r\n" +
                        $"local_port = {LocalPort.Text}\r\nremote_port = {conf_rport}\r\n" +
                        ((conf_rport == "80" || conf_rport == "443") ? $"custom_domains = {selectedTunnel.RPort}\r\n" : "") +
                        $"use_encryption = {selectedTunnel.Encryption}\r\n" +
                        $"use_compression = {selectedTunnel.Compression}\r\n \r\n";
                    //输出配置
                    Config.WriteFrpcConfig(2, $"ChmlFrp - {selectedTunnel.Name}({selectedTunnel.Node})", FrpcConfig, "");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                    Window.GetWindow(this).Close();
                }
                catch (Exception ex)
                {
                    (sender as Button).IsEnabled = true;
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "写入Frpc配置失败！\n" + ex.Message, "出错");
                }
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "请您选择一个隧道再按确定哦~", "隧道呢？");
            }
        }

        //删除隧道的
        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "确定删除所选隧道吗？", "删除隧道", true);
            if (dialog == true)
            {
                try
                {
                    var listBox = FrpList;
                    if (listBox.SelectedItem is TunnelInfo selectedTunnel)
                    {
                        string res = (await HttpService.GetAsync($"{ChmlFrpApiUrl}/api/deletetl.php?token={ChmlToken}&nodeid={selectedTunnel.ID}&userid={ChmlID}")).HttpResponseContent.ToString();
                        //处理结果
                        var PostResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                        if (PostResponse["code"].ToString() == "200")
                        {
                            //好了
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道删除成功！", "删除");
                            _ = GetFrpList();//刷新下列表
                        }
                        else
                        {
                            //创建失败的处理
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), $"隧道删除失败！\n{PostResponse["error"]}", "失败！");

                        }
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), $"请选择一个隧道再操作！", "失败！");
                    }
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), ex.Message, "失败！");
                }
            }
            (sender as Button).IsEnabled = true;
        }

        public class NodeInfo
        {
            public string Area { get; set; }
            public string Name { get; set; }
            public string Notes { get; set; }
            public string NodeGroup { get; set; }
            public string NodeGroupName { get; set; }
        }
        //获取节点列表
        private async Task GetNodeList()
        {
            ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
            NodeList.ItemsSource = nodes;
            //从api获取节点列表
            var _response = await HttpService.GetAsync($"{ChmlFrpApiUrl}/api/unode.php");
            if (_response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建失败！\n" + _response.HttpResponseContent, "创建失败！");
                return;
            }
            string response = _response.HttpResponseContent.ToString();
            try
            {
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                foreach (var item in jsonArray)
                {
                    if (item["nodegroup"].ToString() == "vip")
                    {
                        nodes.Add(new NodeInfo
                        {
                            Name = $"{item["name"]}",
                            Area = $"{item["area"]}",
                            Notes = $"{item["notes"]}",
                            NodeGroup = $"{item["nodegroup"]}",
                            NodeGroupName = "VIP节点",
                        });
                    }
                    else
                    {
                        nodes.Add(new NodeInfo
                        {
                            Name = $"{item["name"]}",
                            Area = $"{item["area"]}",
                            Notes = $"{item["notes"]}",
                            NodeGroup = $"{item["nodegroup"]}",
                            NodeGroupName = "普通节点",
                        });
                    }
                }
            }
            catch (JsonSerializationException)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "无法加载节点信息！", "错误");
            }
        }

        //处理信息显示
        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                NodeTips.Text = selectedNode.Notes;
            }
        }

        //确定创建摁下去了
        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var listBox = NodeList;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                string enc, comp;
                if (Create_Encryption.IsChecked == true)
                {
                    enc = "";
                }
                else
                {
                    enc = "false";
                }
                if (Create_Compression.IsChecked == true)
                {
                    comp = "";
                }
                else
                {
                    comp = "false";
                }
                string lip = Create_LocalIP.Text;
                string name = Create_Name.Text;
                string proc = Create_Protocol.Text;
                string lport = Create_LocalPort.Text;
                string rport = Create_RemotePort.Text;
                await PostCreate(lip, name, selectedNode.Name, proc, lport, rport, enc, comp);
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "您似乎没有选择节点！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        //post把数据丢过去
        private async Task PostCreate(string localip, string name, string node, string type, string nport, string dorp, string encryption, string compression)
        {
            var body = new JObject
            {
                ["token"] = ChmlToken,
                ["userid"] = ChmlID,
                ["localip"] = localip,
                ["name"] = name,
                ["node"] = node,
                ["type"] = type,
                ["nport"] = nport,
                ["dorp"] = dorp,
                ["ap"] = string.Empty,
                ["encryption"] = encryption,
                ["compression"] = compression
            };

            var _response = await HttpService.PostAsync($"{ChmlFrpApiUrl}/api/tunnel.php", 0, body);
            if (_response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建失败！\n" + _response.HttpResponseContent, "创建失败！");
                return;
            }
            string response = _response.HttpResponseContent.ToString();
            //处理结果
            var PostResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (PostResponse["code"].ToString() == "200")
            {
                //好了
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！\n即将返回主页···", "创建成功！");
                MainCtrl.SelectedIndex = 0;
            }
            else
            {
                //创建失败的处理
                MagicShow.ShowMsgDialog(Window.GetWindow(this), $"隧道创建失败！\n{PostResponse["error"]}", "创建失败！");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            await GetFrpList();
            (sender as Button).IsEnabled = true;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            MainCtrl.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            //清理保存的token
            JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            jobject["ChmlToken"] = "";
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
        }
    }
}
