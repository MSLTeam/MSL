using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Windows.Storage.Compression;


namespace MSL.pages.frpProviders
{
    /// <summary>
    /// ChmlFrp.xaml 的交互逻辑
    /// </summary>
    public partial class ChmlFrp : Page
    {
        string ChmlFrpApiUrl = "https://panel.chmlfrp.cn";
        string ChmlToken, ChmlID;
        public ChmlFrp()
        {
            InitializeComponent();
        }
        private void Page_Initialized(object sender, EventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            CreateGrid.Visibility = Visibility.Collapsed;
        }

        //使用token登录
        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token;
            token = await Shows.ShowInput(Window.GetWindow(this), "请输入Chml账户Token", "", true);
            if (token != null)
            {
                Task.Run(() => verifyUserToken(token.Trim())); //移除空格，防止笨蛋
            }
            
        }

        //账号密码
        private async void userLogin_Click(object sender, RoutedEventArgs e)
        {
            string frpUser, frpPassword;
           frpUser = await Shows.ShowInput(Window.GetWindow(this), "请输入ChmlFrp的账户名/邮箱/QQ号");
            frpPassword = await Shows.ShowInput(Window.GetWindow(this), "请输入密码","", true);
            Task.Run(() => getUserToken(frpUser, frpPassword));
        }

        //注册一个可爱的账户
        private void userRegister_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://panel.chmlfrp.cn/register");
        }


        //异步登录，获取到用户token
        private void getUserToken(string user,string pwd)
        {
            string response = Functions.Post("api/login.php", 2, $"username={user}&password={pwd}", ChmlFrpApiUrl);
            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (jsonResponse.ContainsKey("code"))
            {
                if (jsonResponse["code"].ToString() == "200")
                {
                    string token = jsonResponse["token"].ToString();
                    ChmlID = jsonResponse["userid"].ToString();//id丢全局
                    //这里就拿到token了
                    Task.Run(() => GetFrpList(token));
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "登陆失败！" + jsonResponse["message"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
                    
                }
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    Dispatcher.Invoke(() =>
                    {

                        Shows.ShowMsgDialog(Window.GetWindow(this), "登陆失败！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
                    }
                    
            }
            
            
        }

        //直接token登录，那么验证下咯~
        private void verifyUserToken(string userToken)
        {
            string response = Functions.Get($"api/userinfo.php?usertoken={userToken}", ChmlFrpApiUrl);
            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (jsonResponse.ContainsKey("userid"))
            {
                ChmlID = jsonResponse["userid"].ToString();//id丢全局
                //这里就拿到token了
                Task.Run(() => GetFrpList(userToken));
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "Token登陆失败！\n可以尝试账号密码登录！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
                    }

            }
        }

        public class TunnelInfo
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

        //登录成功了，然后就是获取隧道,丢到ui去
        private void GetFrpList(String token )
        {
            ChmlToken=token;//丢到全局
            //处理ui界面交接
            Dispatcher.Invoke(() =>
            {
                MainGrid.Visibility = Visibility.Visible;
                LoginGrid.Visibility = Visibility.Collapsed;
                CreateGrid.Visibility = Visibility.Collapsed;
            });
            //获取userinfo
            var jsonUserInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(Functions.Get($"api/userinfo.php?usertoken={token}", ChmlFrpApiUrl));
            Dispatcher.Invoke(() =>
            {
                UserInfo.Text= $"用户ID:{jsonUserInfo["userid"]}  " +
                $"用户名:{jsonUserInfo["username"]}\n" +
                $"邮箱:{jsonUserInfo["email"]}  " +
                $"会员类型:{jsonUserInfo["usergroup"]}\n" +
                $"隧道数:{jsonUserInfo["tunnelstate"]}/{jsonUserInfo["tunnel"]}";
            });

            //获取隧道
            ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
            Dispatcher.Invoke(() =>
            {
                FrpList.ItemsSource = tunnels;
            });
            string response = Functions.Get($"api/usertunnel.php?token={token}", ChmlFrpApiUrl);
            try
            {
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                foreach (var item in jsonArray)
                {
                    Dispatcher.Invoke(() =>
                    {
                        tunnels.Add(new TunnelInfo { Name = $"{item["name"]}", 
                            Node = $"{item["node"]}" ,ID= $"{item["id"]}" ,
                            Type= $"{item["type"]}",LIP= $"{item["localip"]}",
                            LPort= $"{item["nport"]}",RPort= $"{item["dorp"]}" ,
                            Addr = $"{item["ip"]}" ,Token= token ,
                            Compression= $"{item["compression"]}" ,Encryption = $"{item["encryption"]}" });
                    });
                }
            }
            catch (JsonSerializationException)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "建议创建一个哦~", "您似乎没有隧道");
                });
            }

        }



        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                TunnelInfo_Text.Text=$"隧道名:{selectedTunnel.Name}\n" +
                    $"隧道ID:{selectedTunnel.ID}\n协议:{selectedTunnel.Type}\n" +
                    $"地域:{selectedTunnel.Node}\n远程端口:{selectedTunnel.RPort}";
                LocalIp.Text=selectedTunnel.LIP;
                LocalPort.Text=selectedTunnel.LPort;
            }
        }

        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            string FrpcConfig;
            var listBox = FrpList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                //输出配置文件
                Uri host = new Uri("http://" + selectedTunnel.Addr);
                FrpcConfig = $"#ChmlFrp节点-{selectedTunnel.Name}\r\n[common]\r\nserver_addr = {host.Host}\r\n" +
                    $"server_port = 7000\r\ntcp_mux = true\r\nprotocol = tcp\r\n" +
                    $"user = {selectedTunnel.Token}\r\ntoken = ChmlFrpToken\r\n" +
                    $"dns_server = 223.6.6.6\r\ntls_enable = false\r\n" +
                    $"[{selectedTunnel.Name}]\r\nprivilege_mode = true\r\n" +
                    $"type = {selectedTunnel.Type}\r\nlocal_ip = {LocalIp.Text}\r\n" +
                    $"local_port = {LocalPort.Text}\r\nremote_port = {selectedTunnel.RPort}\r\n" +
                    $"use_encryption = {selectedTunnel.Encryption}\r\n" +
                    $"use_compression = {selectedTunnel.Compression}\r\n \r\n";
                File.WriteAllText(@"MSL\frpc", FrpcConfig);
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                jobject["frpcServer"] = "2";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "ChmlFrp隧道配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                Window.GetWindow(this).Close();
            }
        }

        private void OpenWeb_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://panel.chmlfrp.cn/tunnelm/manage");
        }

        //删除隧道的
        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "确定删除所选隧道吗？", "删除隧道", true);
            if (dialog == true)
            {
                var listBox = FrpList as System.Windows.Controls.ListBox;
                if (listBox.SelectedItem is TunnelInfo selectedTunnel)
                {
                    string res = Functions.Get($"api/deletetl.php?token={ChmlToken}&nodeid={selectedTunnel.ID}&userid={ChmlID}", ChmlFrpApiUrl);
                    //处理结果
                    var PostResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                    if (PostResponse["code"].ToString() == "200")
                    {
                        //好了
                        Dispatcher.Invoke(() =>
                        {
                            Shows.ShowMsgDialog(Window.GetWindow(this), "隧道删除成功！", "删除");
                        });
                        Task.Run(() => GetFrpList(ChmlToken));//刷新下列表
                    }
                    else
                    {
                        //创建失败的处理
                        Dispatcher.Invoke(() =>
                        {
                            Shows.ShowMsgDialog(Window.GetWindow(this), $"隧道删除失败！\n{PostResponse["error"]}", "失败！");
                        });

                    }
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), $"请选择一个隧道再操作！", "失败！");
                    });
                }
            }
           
        }


        //创建隧道相关
        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Visible;
            Task.Run(()=> GetNodeList() );//获取列表
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
        private void GetNodeList()
        {
            ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
            Dispatcher.Invoke(() =>
            {
                NodeList.ItemsSource = nodes;
            });
            //从api获取节点列表
            string response = Functions.Get($"api/unode.php", ChmlFrpApiUrl);
            try
            {
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                foreach (var item in jsonArray)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (item["nodegroup"].ToString() == "vip")
                        {
                            nodes.Add(new NodeInfo
                            {
                                Name = $"{item["name"]}",
                                Area = $"{item["area"]}",
                                Notes = $"{item["notes"]}",
                                NodeGroup = $"{item["nodegroup"]}",
                                NodeGroupName="VIP节点",
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

                    });
                }
            }
            catch (JsonSerializationException)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "无法加载节点信息！", "错误");
                });
            }
        }

        //处理信息显示
        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                NodeTips.Text=selectedNode.Notes;
            }
        }

        //确定创建摁下去了
        private void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = NodeList as System.Windows.Controls.ListBox;
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
                Task.Run(() => PostCreate(lip, name, selectedNode.Name,proc ,lport ,rport , enc, comp));
            }
           
        }

        //post把数据丢过去
        private void PostCreate(string localip,string name,string node,string type,string nport,string dorp,string encryption,string compression)
        {

            string parameterData = $@"
{{
    ""token"": ""{ChmlToken}"",
    ""userid"": ""{ChmlID}"",
    ""localip"": ""{localip}"",
    ""name"": ""{name}"",
    ""node"": ""{node}"",
    ""type"": ""{type}"",
    ""nport"": {nport},
    ""dorp"": {dorp},
    ""ap"": """",
    ""encryption"": ""{encryption}"",
    ""compression"": ""{compression}""
}}";
            string response = Functions.Post("api/tunnel.php", 0, parameterData, ChmlFrpApiUrl, null);
            //处理结果
            var PostResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (PostResponse["code"].ToString() == "200")
            {
                //好了
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！\n即将返回主页···", "创建成功！");
                    MainGrid.Visibility = Visibility.Visible;
                    LoginGrid.Visibility = Visibility.Collapsed;
                    CreateGrid.Visibility = Visibility.Collapsed;
                });
                Task.Run(() => GetFrpList(ChmlToken));//刷新下列表
            }
            else
            {
                //创建失败的处理
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), $"隧道创建失败！\n{PostResponse["error"]}", "创建失败！");
                });
                
            }

        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(()=> GetFrpList(ChmlToken));
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            CreateGrid.Visibility = Visibility.Collapsed;
        }



        //返回按钮
        private void Create_BackBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Collapsed;
            CreateGrid.Visibility = Visibility.Collapsed;
        }
    }
}
