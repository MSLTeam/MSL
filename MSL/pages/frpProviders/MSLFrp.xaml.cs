using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using Page = System.Windows.Controls.Page;
using Window = System.Windows.Window;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MSLFrp.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrp : Page
    {
        //private CancellationTokenSource cts;
        private List<string> list1 = new List<string>();
        private List<string> list2 = new List<string>();
        private List<string> list3 = new List<string>();
        private List<string> list4 = new List<string>();

        public MSLFrp()
        {
            InitializeComponent();
            //cts = new CancellationTokenSource();
            //await GetFrpsInfo(cts.Token);
            //Task.Run(() => GetFrpsInfo(cts.Token));
            Task.Run(() => GetFrpsInfo());
        }

        /*
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            try
            {
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                MainGrid.Children.Remove(loadingCircle);
                MainGrid.UnregisterName("loadingBar");
            }
            catch
            { }
        }
        */

        private async Task GetFrpsInfo()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    LoadingCircle loadingCircle = new LoadingCircle();
                    loadingCircle.VerticalAlignment = VerticalAlignment.Top;
                    loadingCircle.HorizontalAlignment = HorizontalAlignment.Left;
                    loadingCircle.Margin = new Thickness(130, 150, 0, 0);
                    MainGrid.Children.Add(loadingCircle);
                    MainGrid.RegisterName("loadingBar", loadingCircle);
                }
                catch
                { }
                serversList.Items.Clear();
                gonggao.Text = "加载中……";
            });

            try
            {
                string mslFrpInfo = Functions.Get("query/MSLFrps");
                JObject valuePairs = (JObject)JsonConvert.DeserializeObject(mslFrpInfo);
                foreach (var valuePair in valuePairs)
                {
                    string serverInfo = valuePair.Key;
                    JObject serverDetails = (JObject)valuePair.Value;
                    foreach (var value in serverDetails)
                    {
                        string serverName = value.Key;
                        string serverAddress = value.Value["server_addr"].ToString();
                        string serverPort = value.Value["server_port"].ToString();
                        string minPort = value.Value["min_open_port"].ToString();
                        string maxPort = value.Value["max_open_port"].ToString();

                        list1.Add(serverAddress);
                        list2.Add(serverPort);
                        list3.Add(minPort);
                        list4.Add(maxPort);

                        try
                        {
                            Ping pingSender = new Ping();
                            PingReply reply = pingSender.Send(serverAddress, 2000); // 替换成您要 ping 的 IP 地址
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (reply.Status == IPStatus.Success)
                                {
                                    // 节点在线，可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    serversList.Items.Add("[" + serverInfo + "]" + serverName + "(延迟：" + roundTripTime + "ms)");
                                }
                                else
                                {
                                    serversList.Items.Add("[" + serverInfo + "]" + serverName + "(检测失败,可能被DDos或下线)");
                                }
                            });
                        }
                        catch
                        {
                            serversList.Items.Add(serverAddress + "(检测失败,可能被DDos或下线)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("连接服务器失败！" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            try
            {
                Dispatcher.Invoke(() =>
                {
                    gonggao.Text = Functions.Get("query/MSLFrps/notice");
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    gonggao.Text = "无公告";
                });
            }
            Dispatcher.Invoke(() =>
            {
                if (File.Exists(@"MSL\frpc"))
                {
                    string text = File.ReadAllText(@"MSL\frpc");
                    string pattern = @"user\s*=\s*(\w+)\s*meta_token\s*=\s*(\w+)";
                    Match match = Regex.Match(text, pattern);

                    if (match.Success)
                    {
                        accountBox.Text = match.Groups[1].Value;
                        passwordBox.Password = match.Groups[2].Value;
                    }
                }
                try
                {
                    LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                    MainGrid.Children.Remove(loadingCircle);
                    MainGrid.UnregisterName("loadingBar");
                }
                catch
                { }
            });
        }

        /*
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serversList.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请确保您选择了一个节点！", "信息");
                return;
            }
            //MSL-FRP
            string frptype = "";
            if (!serversList.SelectedValue.ToString().Contains("付费"))
            {
                try
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "请确保内网端口和QQ号不为空", "错误");
                        return;
                    }
                    //string frptype = "";
                    string serverName = serversList.Items[serversList.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true) compressionArg = "use_compression = true\n";
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    if (frpcType.SelectedIndex == 0) frptype = "tcp";
                    else if (frpcType.SelectedIndex == 1) frptype = "udp";

                    string frpc = "#" + serverName + "\n[common]\n";
                    frpc += "server_port = " + list2[a].ToString() + "\n";
                    frpc += "server_addr = " + list1[a].ToString() + "\n";
                    frpc += "user = " + accountBox.Text + "\n";
                    frpc += "token = \n";
                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = portBox.Text.Substring(0, portBox.Text.IndexOf("|"));
                        string Ru2 = portBox.Text.Substring(portBox.Text.IndexOf("|"));
                        string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);
                        frpc += "\n[tcp]\ntype = tcp\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + a100 + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[udp]\ntype = udp\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + a200 + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }
                    else
                    {
                        frpc += "\n[" + frptype + "]\ntype = " + frptype + "\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + portBox.Text + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }
                    File.WriteAllText(@"MSL\frpc", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                }
                catch (Exception a) { MessageBox.Show(a.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            }
            else
            {
                try
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "请确保没有漏填信息", "错误");
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
                    if (enableCompression.IsChecked == true) compressionArg = "use_compression = true\n";
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    string frpc = "#" + serverName + "\n[common]\n";
                    frpc += "server_port = " + frpPort + "\n";
                    frpc += "server_addr = " + list1[a].ToString() + "\n";
                    frpc += "user = " + accountBox.Text + "\n";
                    frpc += "meta_token = " + passwordBox.Password + "\n";
                    if (protocol != "") frpc += "protocol = " + protocol + "\n";

                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = portBox.Text.Substring(0, portBox.Text.IndexOf("|"));
                        string Ru2 = portBox.Text.Substring(portBox.Text.IndexOf("|"));
                        string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);
                        frpc += "\n[tcp]\ntype = tcp\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + a100 + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[udp]\ntype = udp\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + a200 + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }
                    else
                    {
                        frpc += "\n[" + frptype + "]\ntype = " + frptype + "\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + portBox.Text + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }

                    File.WriteAllText(@"MSL\frpc", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请确保选择节点后再试：" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            Window.GetWindow(this).Close();
        }
        */

        //这里是toml格式配置文件的代码（后续版本更新可能会启用）

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (serversList.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(window, "请确保您选择了一个节点！", "信息");
                return;
            }
            //MSL-FRP
            string frptype = "";
            if (!serversList.SelectedValue.ToString().Contains("付费"))
            {
                try
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        Shows.ShowMsgDialog(window, "请确保内网端口和QQ号不为空", "错误");
                        return;
                    }
                    //string frptype = "";
                    string serverName = serversList.Items[serversList.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true) compressionArg = "transport.useCompression = true\n";
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    if (frpcType.SelectedIndex == 0) frptype = "tcp";
                    else if (frpcType.SelectedIndex == 1) frptype = "udp";

                    string frpc = "#" + serverName + "\n";
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
                        frpc += "\ttype = \"tcp\"\n";
                        frpc += "\tlocalIP = \"127.0.0.1\"\n";
                        frpc += "\tlocalPort = " + a100 + "\n";
                        frpc += "\tremotePort = " + n + "\n";
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
                    Directory.CreateDirectory("MSL\\frp");
                    File.WriteAllText(@"MSL\frp\frpc.toml", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                }
                catch (Exception a) { MessageBox.Show(a.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            }
            else
            {
                try
                {
                    int a = serversList.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (portBox.Text == "" || accountBox.Text == "")
                    {
                        Shows.ShowMsgDialog(window, "请确保没有漏填信息", "错误");
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
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    string frpc = "#" + serverName + "\n";
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

                    Directory.CreateDirectory("MSL\\frp");
                    File.WriteAllText(@"MSL\frp\frpc.toml", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    await Shows.ShowMsgDialogAsync(window, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请确保选择节点后再试：" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            window.Close();
        }


        private void serversList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serversList.SelectedIndex != -1)
            {
                if (serversList.SelectedItem.ToString().IndexOf("付费") + 1 != 0)
                {
                    if (serversList.SelectedItem.ToString().IndexOf("无加速协议") + 1 != 0)
                    {
                        paidProtocolLabel.Visibility = Visibility.Hidden;
                        usePaidProtocol.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        paidProtocolLabel.Visibility = Visibility.Visible;
                        usePaidProtocol.Visibility = Visibility.Visible;
                    }
                    lab2.Margin = new Thickness(290, 90, 0, 0);
                    accountBox.Margin = new Thickness(330, 120, 0, 0);
                    paidProtocolLabel.Visibility = Visibility.Visible;
                    usePaidProtocol.Visibility = Visibility.Visible;
                    paidPasswordLabel.Visibility = Visibility.Visible;
                    passwordBox.Visibility = Visibility.Visible;
                    return;
                }
            }
            lab2.Margin = new Thickness(290, 115, 0, 0);
            accountBox.Margin = new Thickness(330, 150, 0, 0);
            paidProtocolLabel.Visibility = Visibility.Hidden;
            usePaidProtocol.Visibility = Visibility.Hidden;
            paidPasswordLabel.Visibility = Visibility.Hidden;
            passwordBox.Visibility = Visibility.Hidden;
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
            if (!await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您是否已经购买了MSLFrp？", "购买/激活MSLFrp服务", true, "我已购买，点击激活", "我未购买，点击购买"))
            {
                //直接激活
                ActiveOrder();
            }
            else
            {
                //购买
                Process.Start("https://afdian.net/a/makabaka123");
                if (!await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
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
            string order = await Shows.ShowInput(Window.GetWindow(this), "输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）");
            if (order == null)
            {
                return;
            }
            if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                return;
            }
            string qq = await Shows.ShowInput(Window.GetWindow(this), "输入账号(QQ号)：");
            if (qq == null)
            {
                return;
            }
            if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
                return;
            }
            ShowDialogs _dialog = new ShowDialogs();
            try
            {
                _dialog.ShowTextDialog(Window.GetWindow(this), "发送请求中，请稍等……");
                JObject keyValuePairs = new JObject()
                {
                    ["order"] = order,
                    ["qq"] = qq,
                };
                var ret = await Task.Run(() => Functions.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), Functions.Get("query/MSLFrps/orderapi")));
                _dialog.CloseTextDialog();
                JObject keyValues = JObject.Parse(ret);
                if (keyValues != null && (int)keyValues["status"] == 0)
                {
                    string passwd = keyValues["password"].ToString();
                    passwordBox.Password = passwd;
                    bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您的付费密码为：" + passwd + "\n已自动填入到密码栏中！\n注册时间：" + keyValues["registration"].ToString() + "\n付费时长：" + keyValues["days"].ToString() + "天\n到期时间：" + keyValues["expiration"].ToString(), "购买成功！", true, "确定", "复制密码");
                    if (dialog)
                    {
                        Clipboard.SetDataObject(passwd);
                    }
                }
                else if (keyValues != null)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), keyValues["reason"].ToString(), "获取失败！");
                }
                else
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "返回内容为空！", "获取失败！");
                }
            }
            catch
            {
                _dialog.CloseTextDialog();
                Shows.ShowMsgDialog(Window.GetWindow(this), "获取失败，请添加QQ：483232994（昵称：MSL-FRP），\n并发送发电成功截图+订单号来手动获取密码\n（注：回复消息不一定及时，请耐心等待！\n如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
            }
        }
    }
}
