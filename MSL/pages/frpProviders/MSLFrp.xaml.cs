using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using System.IO;
using Window = System.Windows.Window;
using System.Threading;
using Newtonsoft.Json;
using System.Web.UI;
using Page = System.Windows.Controls.Page;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MSLFrp.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrp : Page
    {
        private CancellationTokenSource cts;
        List<string> list1 = new List<string>();
        List<string> list2 = new List<string>();
        List<string> list3 = new List<string>();
        List<string> list4 = new List<string>();

        public MSLFrp()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            //await GetFrpsInfo(cts.Token);
            Task.Run(() => GetFrpsInfo(cts.Token));
        }
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
        private async Task GetFrpsInfo(CancellationToken ct)
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
                {}
                serversList.Items.Clear();
                gonggao.Content = "加载中……";
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
                                    serversList.Items.Add(serverAddress + "(延迟：" + roundTripTime + "ms)");
                                }
                                else
                                {
                                    serversList.Items.Add(serverAddress + "(检测失败,可能被DDos或下线)");
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
                    gonggao.Content = Functions.Get("query/MSLFrps/notice");
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    gonggao.Content = "无公告";
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
            if (ct.IsCancellationRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    serversList.Items.Clear();
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (serversList.SelectedIndex == -1)
            {
                DialogShow.ShowMsg(window, "请确保您选择了一个节点！", "信息");
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
                        DialogShow.ShowMsg(window, "请确保内网端口和QQ号不为空", "错误");
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
                        frpc += "\n[tcp]\n\ttype = tcp\n";
                        frpc += "local_ip = 127.0.0.1\n";
                        frpc += "local_port = " + a100 + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[udp]\n\ttype = udp\n";
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
                    DialogShow.ShowMsg(window, "Frpc配置已保存", "信息", false, "确定");
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
                        DialogShow.ShowMsg(window, "请确保没有漏填信息", "错误");
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
                    DialogShow.ShowMsg(window, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息", false, "确定");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请确保选择节点后再试：" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            window.Close();
        }

        //这里是toml格式配置文件的代码（后续版本更新可能会启用）
        /*
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (serversList.SelectedIndex == -1)
            {
                DialogShow.ShowMsg(window, "请确保您选择了一个节点！", "信息");
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
                        DialogShow.ShowMsg(window, "请确保内网端口和QQ号不为空", "错误");
                        return;
                    }
                    //string frptype = "";
                    string serverName = serversList.Items[serversList.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true) compressionArg = "useCompression = true\n";
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    if (frpcType.SelectedIndex == 0) frptype = "tcp";
                    else if (frpcType.SelectedIndex == 1) frptype = "udp";

                    string frpc = "#" + serverName + "\n";
                    frpc += "serverAddr = \"" + list1[a].ToString() + "\"\n";
                    frpc += "serverPort = " + list2[a].ToString() + "\n";
                    frpc += "user = \"" + accountBox.Text + "\"\n";
                    frpc += "token = \"\"\n";
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
                    File.WriteAllText(@"MSL\frpc.toml", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    DialogShow.ShowMsg(window, "Frpc配置已保存", "信息", false, "确定");
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
                        DialogShow.ShowMsg(window, "请确保没有漏填信息", "错误");
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
                    if (enableCompression.IsChecked == true) compressionArg = "useCompression = true\n";
                    if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                    string frpc = "#" + serverName + "\n";
                    frpc += "serverAddr = \"" + list1[a].ToString() + "\"\n";
                    frpc += "serverPort = " + frpPort + "\n";
                    frpc += "user = \"" + accountBox.Text + "\"\n";
                    frpc += "metaToken = \"" + passwordBox.Password + "\"\n";
                    if (protocol != "") frpc += "protocol = \"" + protocol + "\"\n";

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
                        frpc += "local_ip = \"127.0.0.1\"\n";
                        frpc += "local_port = " + portBox.Text + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }

                    File.WriteAllText(@"MSL\frpc.toml", frpc);
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    DialogShow.ShowMsg(window, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息", false, "确定");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请确保选择节点后再试：" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            window.Close();
        }
        */

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
            if (gotoAifadian.Content.ToString() == "购买付费节点")
            {
                Window window= Window.GetWindow(this);
                Process.Start("https://afdian.net/a/makabaka123");
                if(!DialogShow.ShowMsg(window, "请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
                {
                    return;
                }
                
                bool input = DialogShow.ShowInput(window, "输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）", out string order);
                if (!input)
                {
                    return;
                }
                if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
                {
                    DialogShow.ShowMsg(window, "请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                    return;
                }
                bool _input = DialogShow.ShowInput(window, "输入账号(QQ号)：", out string qq);
                if (!_input)
                {
                    return;
                }
                if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
                {
                    DialogShow.ShowMsg(window, "请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
                    return;
                }
                Dialog _dialog = null;
                try
                {
                    _dialog = Dialog.Show(new TextDialog("发送请求中，请稍等……"));
                    JObject keyValuePairs = new JObject()
                    {
                        ["order"] = order,
                        ["qq"] = qq,
                    };
                    var ret = await Task.Run(() => Functions.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), "http://111.180.189.249:7004"));
                    window.Focus();
                    _dialog.Close();
                    JObject keyValues = JObject.Parse(ret);
                    if(keyValues != null && int.Parse(keyValues["status"].ToString()) == 0)
                    {
                        string passwd= keyValues["password"].ToString();
                        bool dialog = DialogShow.ShowMsg(window, "您的付费密码为：" + passwd + "\n注册时间："+keyValues["registration"].ToString()+"\n付费时长："+ keyValues["days"].ToString() + "天\n到期时间："+ keyValues["expiration"].ToString(), "购买成功！", true, "确定", "复制密码");
                        if (dialog)
                        {
                            //passwordBox.Password = passwd;
                            Clipboard.SetDataObject(passwd);
                        }
                    }
                    else if(keyValues != null)
                    {
                        DialogShow.ShowMsg(window, keyValues["reason"].ToString(), "获取失败！");
                    }
                    else
                    {
                        DialogShow.ShowMsg(window, "返回内容为空！", "获取失败！");
                    }
                }
                catch
                {
                    window.Focus();
                    _dialog.Close();
                    DialogShow.ShowMsg(window, "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送发电成功截图+订单号来手动获取密码\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                }
            }
            else
            {
                Process.Start("https://www.openfrp.net/");
            }
        }
    }
}
