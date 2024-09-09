using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Data.Json;

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

        private void Page_Initialized(object sender, EventArgs e)
        {
            //显示登录页面
            LoginGrid.Visibility=Visibility.Visible;
            MainGrid.Visibility=Visibility.Collapsed;
            CreateGrid.Visibility=Visibility.Collapsed;
        }

        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token;
            token = await Shows.ShowInput(Window.GetWindow(this), "请输入Sakura账户Token", "", true);
            if (token != null)
            {
                bool save = (bool)SaveToken.IsChecked;
                ShowDialogs showDialogs = new ShowDialogs();
                showDialogs.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await Task.Run(() => VerifyUserToken(token.Trim(), save)); //移除空格，防止笨蛋
                showDialogs.CloseTextDialog();
            }
        }

        private async void VerifyUserToken(string token, bool save)
        {
            try
            {
                HttpResponse res = await HttpService.GetAsync(ApiUrl+"/user/info?token="+token);
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
                    Dispatcher.Invoke(() =>
                    {
                        UserInfo.Text = $"用户名: {JsonUserInfo["name"]}\n用户类型: {JsonUserInfo["group"]["name"]}\n限速: {JsonUserInfo["speed"]}";
                    });
                    UserLevel = int.Parse((string)JsonUserInfo["group"]["level"]);
                    //获取隧道
                    Task.Run(() => GetTunnelList(token));
                }
                else
                {
                    await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                }
            }
            catch (Exception ex)
            {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！"+ex.Message, "错误");
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

        private async void GetTunnelList(string token)
        {
            try
            {
                //绑定对象
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                Dispatcher.Invoke(() =>
                {
                    FrpList.ItemsSource = tunnels;
                });
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/tunnels?token=" + token);
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK) {
                    JArray JsonTunnels = JArray.Parse((string)res.HttpResponseContent);
                    foreach (var item in JsonTunnels)
                    {
                        Dispatcher.Invoke(() =>
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
                        });
                      
                    }
                }
            }
            catch (Exception ex) {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！"+ex.Message, "错误");
            } 
        }

        //获取某个隧道的配置文件
        private async Task<string> GetTunnelConfig(string token,int id)
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
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/tunnel/config",0,body,headersAction);
                return (string)res.HttpResponseContent;

            }
            catch (Exception ex) { 
                return "MSL-ERR:"+ex.Message;
            }
        }

        //显示隧道信息
        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                TunnelInfo_Text.Text = $"隧道名: {selectedTunnel.Name}" +
                    $"\n隧道ID: {selectedTunnel.ID}"+ $"\n远程端口: {selectedTunnel.RPort}" + $"\n隧道状态: {(selectedTunnel.Online? "在线" : "离线")}";
                LocalIp.Text = selectedTunnel.LIP;
                LocalPort.Text = selectedTunnel.LPort;
            }
        }

        //确定 输出config
        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
               //string content = await Task.Run(() => GetTunnelConfig(UserToken,selectedTunnel.ID));
                    //输出配置文件
                   if( Config.WriteFrpcConfig(3, $"-f {UserToken}:{selectedTunnel.ID}", $"SakuraFrp - {selectedTunnel.Name}") == true)
                    {
                        await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                        Window.GetWindow(this).Close();
                }
                else
                {
                    await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "配置输出失败！", "错误");
                }
            }
            else
            {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(()=> GetTunnelList(UserToken));
        }

        //获取某个隧道的配置文件
        private async void DelTunnel(string token, int id)
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
                Task.Run(() => GetTunnelList(UserToken));
            }
            catch (Exception ex)
            {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "删除失败！"+ex.Message, "错误");
            }
        }

        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                Task.Run(() => DelTunnel(UserToken,selectedTunnel.ID));
            }
            else
            {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
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
        }

        //下面是创建隧道相关

        internal class NodeInfo
        {
            public int ID {  get; set; }
            public string Name { get; set; }
            public string Host {  get; set; }
            public string Description { get; set; }
            public int Vip {  get; set; }
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

            Task.Run(() => GetNodeList());
        }

        private async void GetNodeList()
        {
            HttpResponse res = await HttpService.GetAsync(ApiUrl + "/nodes?token=" + UserToken);
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
                Dispatcher.Invoke(() =>
                {
                    NodeList.ItemsSource = nodes;
                });
                JObject json = JObject.Parse((string)res.HttpResponseContent);

                //遍历查询
                foreach (var nodeProperty in json.Properties())
                {
                    int nodeId = int.Parse(nodeProperty.Name);
                    JObject nodeData = (JObject)nodeProperty.Value;
                    if(UserLevel >= (int)nodeData["vip"])
                    {
                        Dispatcher.Invoke(() =>
                        {
                            nodes.Add(new NodeInfo
                            {
                                ID = nodeId,
                                Name = (string)nodeData["name"],
                                Host = (string)nodeData["host"],
                                Description = (string)nodeData["description"],
                                Vip = (int)nodeData["vip"],
                                VipName = ((int)nodeData["vip"] == 0 ? "普通节点" :((int)nodeData["vip"] == 3 ? "青铜节点": "白银节点")),
                                Flag = (int)nodeData["flag"],
                                Band = (string)nodeData["band"]
                            });
                        });
                    }
                   
                }


            }

        }
    }
}
