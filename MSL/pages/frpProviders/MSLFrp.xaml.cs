using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Page = System.Windows.Controls.Page;
using Window = System.Windows.Window;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MSLFrp.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrp : Page
    {
        private readonly List<string> list1 = new List<string>();
        private readonly List<string> list2 = new List<string>();
        private readonly List<string> list3 = new List<string>();
        private readonly List<string> list4 = new List<string>();

        public MSLFrp()
        {
            InitializeComponent();
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            serversList.Items.Clear();
            gonggao.Text = "加载中……";
            try
            {
                HttpResponse mslFrpInfo = await HttpService.GetApiAsync("query/frp/MSLFrps");
                if (mslFrpInfo.HttpResponseCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("获取节点信息失败！");
                }
                JObject valuePairs = (JObject)((JObject)JsonConvert.DeserializeObject(mslFrpInfo.HttpResponseContent.ToString()))["data"];
                int id = 0;
                foreach (var valuePair in valuePairs)
                {
                    string serverInfo = valuePair.Key;
                    JObject serverDetails = (JObject)valuePair.Value;
                    int freeC = 0;
                    foreach (var value in serverDetails)
                    {
                        if (serverInfo.Contains("免费"))
                        {
                            freeC++;
                        }
                    }
                    Random random = new Random();
                    int free = random.Next(1, freeC + 1); //确保出来一个免费节点
                    if (free != 0)
                    {
                        foreach (var value in serverDetails)
                        {
                            free--;
                            if (free != 0)
                            {
                                continue;
                            }
                            string serverName = value.Key;
                            string serverAddress = value.Value["server_addr"].ToString();
                            string serverPort = value.Value["server_port"].ToString();
                            string minPort = value.Value["min_open_port"].ToString();
                            string maxPort = value.Value["max_open_port"].ToString();

                            list1.Add(serverAddress);
                            list2.Add(serverPort);
                            list3.Add(minPort);
                            list4.Add(maxPort);

                            string _serverName = "[" + serverInfo + "]" + serverName;
                            ServerPingTest(_serverName, serverAddress, id);
                            id++;

                            //移除已显示的免费节点 不然付费会重复一个（）
                            serverDetails.Remove(serverName);
                            break;
                        }
                    }
                    foreach (var value in serverDetails)
                    {
                        if (!serverInfo.Contains("付费"))
                        {
                            continue;
                        }
                        string serverName = value.Key;
                        string serverAddress = value.Value["server_addr"].ToString();
                        string serverPort = value.Value["server_port"].ToString();
                        string minPort = value.Value["min_open_port"].ToString();
                        string maxPort = value.Value["max_open_port"].ToString();

                        list1.Add(serverAddress);
                        list2.Add(serverPort);
                        list3.Add(minPort);
                        list4.Add(maxPort);

                        string _serverName = "[" + serverInfo + "]" + serverName;
                        ServerPingTest(_serverName, serverAddress, id);
                        id++;
                    }
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "连接服务器失败！" + ex.Message, "错误");
            }
            try
            {
                gonggao.Text = (await HttpService.GetApiContentAsync("query/frp/MSLFrps?query=notice"))["data"]["notice"].ToString();
            }
            catch
            {
                gonggao.Text = "无公告";
            }
            if (File.Exists(@"MSL\frp\frpc.toml"))
            {
                string text = File.ReadAllText(@"MSL\frp\frpc.toml");
                string pattern = @"user\s*=\s*""(\w+)""\s*metadatas\.token\s*=\s*""(\w+)""";
                Match match = Regex.Match(text, pattern);

                if (match.Success)
                {
                    accountBox.Text = match.Groups[1].Value;
                    passwordBox.Password = match.Groups[2].Value;
                }
            }
            if (Directory.Exists("MSL\\frp"))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(@"MSL\frp");
                DirectoryInfo[] dirInfo = directoryInfo.GetDirectories();
                foreach (DirectoryInfo dir in dirInfo)
                {
                    if (File.Exists(dir.FullName + @"\frpc.toml"))
                    {
                        string text = File.ReadAllText(dir.FullName + @"\frpc.toml");
                        string pattern = @"user\s*=\s*""(\w+)""\s*metadatas\.token\s*=\s*""(\w+)""";
                        Match match = Regex.Match(text, pattern);

                        if (match.Success)
                        {
                            accountBox.Text = match.Groups[1].Value;
                            passwordBox.Password = match.Groups[2].Value;
                            break;
                        }
                    }
                }
            }
            if (File.Exists(@"MSL\frp\config.json"))
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                if (jobject["MSLFrpAccount"] != null)
                {
                    accountBox.Text = jobject["MSLFrpAccount"].ToString();
                }
                if (jobject["MSLFrpPasswd"] != null)
                {
                    passwordBox.Password = jobject["MSLFrpPasswd"].ToString();
                }
            }
        }

        private async void ServerPingTest(string serverName, string serverAddr, int id)
        {
            try
            {
                serversList.Items.Add(serverName);
                await Task.Run(() =>
                {
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(serverAddr, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        // 节点在线，可以获取延迟等信息
                        int roundTripTime = (int)reply.RoundtripTime;
                        Dispatcher.Invoke(() =>
                        {
                            serversList.Items[id] = serversList.Items[id].ToString() + "（延迟：" + roundTripTime + "ms）";
                        });

                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            serversList.Items[id] = serversList.Items[id].ToString() + "（延迟检测失败）";
                        });
                    }
                });
            }
            catch
            {
                serversList.Items.Add(serverAddr + "（延迟检测失败）");
            }
        }

        public static bool IsValidQQFormat(string qq)
        {
            //6-10位
            if (qq.Length < 6 || qq.Length > 10)
            {
                return false;
            }

            //数字？
            if (!qq.All(char.IsDigit))
            {
                return false;
            }

            //全都是1-2个数字肯定是瞎写
            if (qq.Distinct().Count() <= 2)
            {
                return false;
            }

            return true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (serversList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(window, "请确保您选择了一个节点！", "信息");
                return;
            }
            if (!IsValidQQFormat(accountBox.Text))
            {
                MagicShow.ShowMsgDialog(window, "请填写正确的QQ号！", "错误");
                return;
            }
            //MSL-FRP
            string frptype = "";
            string frpc;
            try
            {
                if (!serversList.SelectedValue.ToString().Contains("付费"))
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        MagicShow.ShowMsgDialog(window, "请确保内网端口和QQ号不为空", "错误");
                        return;
                    }
                    //string frptype = "";
                    string serverName = serversList.Items[serversList.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true) compressionArg = "transport.useCompression = true\n";
                    if (serverName.Contains("（")) serverName = serverName.Substring(0, serverName.IndexOf("（"));
                    if (frpcType.SelectedIndex == 0) frptype = "tcp";
                    else if (frpcType.SelectedIndex == 1) frptype = "udp";

                    frpc = "#" + serverName + "\n";
                    frpc += "serverAddr = \"" + list1[a].ToString() + "\"\n";
                    frpc += "serverPort = " + list2[a].ToString() + "\n";
                    frpc += "user = \"" + accountBox.Text + "\"\n";
                    //frpc += "auth.token = \"\"\n";
                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = portBox.Text.Substring(0, portBox.Text.IndexOf("|"));
                        string Ru2 = portBox.Text.Substring(portBox.Text.IndexOf("|"));
                        string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);
                        frpc += "\n[[proxies]]\nname = \"tcp\"\n";
                        frpc += "type = \"tcp\"\n";
                        frpc += "localIP = \"127.0.0.1\"\n";
                        frpc += "localPort = " + a100 + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[[proxies]]\nname = \"udp\"\n";
                        frpc += "type = \"udp\"\n";
                        frpc += "localIP = \"127.0.0.1\"\n";
                        frpc += "localPort = " + a200 + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg;
                    }
                    else
                    {
                        frpc += "\n[[proxies]]\nname = \"" + frptype + "\"\n";
                        frpc += "type = \"" + frptype + "\"\n";
                        frpc += "localIP = \"127.0.0.1\"\n";
                        frpc += "localPort = " + portBox.Text + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg;
                    }
                }
                else
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        MagicShow.ShowMsgDialog(window, "请确保没有漏填信息", "错误");
                        return;
                    }
                    //string frptype = "";
                    string protocol = "";
                    string frpPort = (int.Parse(list2[a].ToString())).ToString();

                    switch (frpcType.SelectedIndex)
                    {
                        case 0:
                            frptype = "tcp";
                            break;

                        case 1:
                            frptype = "udp";
                            break;
                    }

                    switch (usePaidProtocol.SelectedIndex)
                    {
                        case 0:
                            protocol = "quic";
                            frpPort = (int.Parse(list2[a].ToString()) + 1).ToString();
                            break;

                        case 1:
                            protocol = "kcp";
                            break;
                    }

                    string serverName = serversList.Items[serversList.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true) compressionArg = "transport.useCompression = true\n";
                    if (serverName.Contains("（")) serverName = serverName.Substring(0, serverName.IndexOf("（"));
                    frpc = "#" + serverName + "\n";
                    frpc += "serverAddr = \"" + list1[a].ToString() + "\"\n";
                    frpc += "serverPort = " + frpPort + "\n";
                    frpc += "user = \"" + accountBox.Text + "\"\n";
                    frpc += "metadatas.token = \"" + passwordBox.Password + "\"\n";
                    if (protocol != "") frpc += "transport.protocol = \"" + protocol + "\"\n";

                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = portBox.Text.Substring(0, portBox.Text.IndexOf("|"));
                        string Ru2 = portBox.Text.Substring(portBox.Text.IndexOf("|"));
                        string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);
                        frpc += "\n[[proxies]]\nname = \"tcp\"\n";
                        frpc += "type = \"tcp\"\n";
                        frpc += "localIp = \"127.0.0.1\"\n";
                        frpc += "localPort = " + a100 + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[[proxies]]\nname = \"udp\"\n";
                        frpc += "type = \"udp\"\n";
                        frpc += "localIp = \"127.0.0.1\"\n";
                        frpc += "localPort = " + a200 + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg;
                    }
                    else
                    {
                        frpc += "\n[[proxies]]\nname = \"" + frptype + "\"\n";
                        frpc += "type = \"" + frptype + "\"\n";
                        frpc += "localIp = \"127.0.0.1\"\n";
                        frpc += "localPort = " + portBox.Text + "\n";
                        frpc += "remotePort = " + n + "\n";
                        frpc += compressionArg;
                    }
                }
            }
            catch (Exception a)
            {
                MagicShow.ShowMsgDialog(window, "出现错误，请确保选择节点后再试：" + a, "错误");
                return;
            }
            string sn = await MagicShow.ShowInput(window, "给此隧道取一个名字吧：", "我的MSLFrp节点");
            if (sn == null)
            {
                return;
            }
            Directory.CreateDirectory("MSL\\frp");
            int number = Functions.Frpc_GenerateRandomInt();
            if (!File.Exists(@"MSL\frp\config.json"))
            {
                File.WriteAllText(@"MSL\frp\config.json", string.Format("{{{0}}}", "\n"));
            }
            Directory.CreateDirectory("MSL\\frp\\" + number);
            File.WriteAllText($"MSL\\frp\\{number}\\frpc.toml", frpc);
            JObject keyValues = new JObject()
            {
                ["frpcServer"] = "0",
                ["name"] = "MSLFrp - " + sn
            };
            JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
            jobject.Add(number.ToString(), keyValues);
            if (rememberPasswd.IsChecked == true)
            {
                if (jobject["MSLFrpAccount"] == null)
                {
                    jobject.Add("MSLFrpAccount", accountBox.Text);
                }
                else
                {
                    jobject["MSLFrpPasswd"] = accountBox.Text;
                }
                if (jobject["MSLFrpPasswd"] == null)
                {
                    jobject.Add("MSLFrpPasswd", passwordBox.Password);
                }
                else
                {
                    jobject["MSLFrpPasswd"] = passwordBox.Password;
                }
            }
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
            await MagicShow.ShowMsgDialogAsync(window, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            window.Close();
        }

        private void serversList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serversList.SelectedIndex == -1)
            {
                return;
            }
            if (serversList.SelectedItem.ToString().Contains("付费"))
            {
                paidPasswordPannel.Visibility = Visibility.Visible;
                paidProtocolPannel.Visibility = Visibility.Visible;
                return;
            }
            paidPasswordPannel.Visibility = Visibility.Collapsed;
            paidProtocolPannel.Visibility = Visibility.Collapsed;
        }

        private void frpcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frpcType.SelectedIndex == 0)
            {
                portBox.Text = "25565";
            }
            if (frpcType.SelectedIndex == 1)
            {
                portBox.Text = "19132";
            }
            if (frpcType.SelectedIndex == 2)
            {
                portBox.Text = "25565|19132";
            }
        }

        private async void gotoWeb_Click(object sender, RoutedEventArgs e)
        {
            if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您是否已经购买了MSLFrp？", "购买/激活MSLFrp服务", true, "我已购买，点击激活", "我未购买，点击购买"))
            {
                //直接激活
                ActiveOrder();
            }
            else
            {
                //购买
                Process.Start("https://afdian.com/a/makabaka123");
                if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
                {
                    return;
                }
                else
                {
                    //买了，继续
                    ActiveOrder();
                }
            }
        }

        //激活方法
        private async void ActiveOrder()
        {
            string order = await MagicShow.ShowInput(Window.GetWindow(this), "输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）");
            if (order == null)
            {
                return;
            }
            if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                return;
            }
            string qq = await MagicShow.ShowInput(Window.GetWindow(this), "输入账号(QQ号)：");
            if (qq == null)
            {
                return;
            }
            if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
                return;
            }
            MagicDialog _dialog = new MagicDialog();
            try
            {
                _dialog.ShowTextDialog(Window.GetWindow(this), "发送请求中，请稍等……");
                JObject keyValuePairs = new JObject()
                {
                    ["order"] = order,
                    ["qq"] = qq,
                };
                var ret = await Task.Run(async () => HttpService.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), (await HttpService.GetApiContentAsync("query/frp/MSLFrps?query=orderapi"))["data"]["url"].ToString()));
                _dialog.CloseTextDialog();
                JObject keyValues = JObject.Parse(ret);
                if (keyValues != null && (int)keyValues["status"] == 0)
                {
                    string passwd = keyValues["password"].ToString();
                    passwordBox.Password = passwd;
                    bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您的付费密码为：" + passwd + "\n已自动填入到密码栏中！\n注册时间：" + keyValues["registration"].ToString() + "\n付费时长：" + keyValues["days"].ToString() + "天\n到期时间：" + keyValues["expiration"].ToString(), "购买成功！", true, "确定", "复制密码");
                    if (dialog)
                    {
                        Clipboard.SetDataObject(passwd);
                    }
                }
                else if (keyValues != null)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), keyValues["reason"].ToString(), "获取失败！");
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "返回内容为空！", "获取失败！");
                }
            }
            catch
            {
                _dialog.CloseTextDialog();
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取失败，请添加QQ：483232994（昵称：MSL-FRP），\n并发送发电成功截图+订单号来手动获取密码\n（注：回复消息不一定及时，请耐心等待！\n如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
            }
        }
    }
}
