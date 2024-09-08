using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

                    //获取隧道
                    Task.Run(() => GetTunnelList(token));
                }
                else
                {
                    MessageBox.Show(res.HttpResponseContent.ToString());
                }
            }
            catch (Exception ex)
            {

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
            public string Token { get; set; }
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
                                Token = token //用户token
                            });
                        });
                      
                    }
                }
            }
            catch (Exception ex) {

            } 
        }

        //获取某个隧道的配置文件
        private async void GetTunnelConfig(string token,int id)
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
                MessageBox.Show((string)res.HttpResponseContent);

                //输出配置文件
                //我也不知道为啥这么写
                //反正就是cv O(∩_∩)O
                Directory.CreateDirectory("MSL\\frp");
                int number = Functions.Frpc_GenerateRandomInt();
                if (!File.Exists(@"MSL\frp\config.json"))
                {
                    File.WriteAllText(@"MSL\frp\config.json", string.Format("{{{0}}}", "\n"));
                }
                Directory.CreateDirectory("MSL\\frp\\" + number);
                File.WriteAllText($"MSL\\frp\\{number}\\frpc", (string)res.HttpResponseContent);
                JObject keyValues = new JObject()
                {
                    ["frpcServer"] = "3", //3在msl代表sakuarfrp
                };
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                jobject.Add(number.ToString(), keyValues);
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                Dispatcher.Invoke(() =>
                {
                    Window.GetWindow(this).Close();
                });
            }
            catch (Exception ex) { 
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
        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                Task.Run(() => GetTunnelConfig(selectedTunnel.Token,selectedTunnel.ID));
            }
        }
    }
}
