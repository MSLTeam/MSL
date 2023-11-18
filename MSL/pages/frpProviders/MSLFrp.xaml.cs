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
                string mslFrpInfo= Functions.Get("frplist");
                if (mslFrpInfo.IndexOf("\n") != -1)
                {
                    while (mslFrpInfo.IndexOf("#") != -1)
                    {
                        string strtempa = "#";
                        int IndexofA = mslFrpInfo.IndexOf(strtempa);
                        string Ru = mslFrpInfo.Substring(IndexofA + 1);
                        string a100 = Ru.Substring(0, Ru.IndexOf("\n"));

                        int IndexofA3 = mslFrpInfo.IndexOf("#");
                        string Ru3 = mslFrpInfo.Substring(IndexofA3 + 1);
                        mslFrpInfo = Ru3;

                        string strtempa1 = "server_addr=";
                        int IndexofA1 = mslFrpInfo.IndexOf(strtempa1);
                        string Ru1 = mslFrpInfo.Substring(IndexofA1 + 12);
                        string a101 = Ru1.Substring(0, Ru1.IndexOf("\n"));
                        list1.Add(a101);

                        string strtempa2 = "server_port=";
                        int IndexofA2 = mslFrpInfo.IndexOf(strtempa2);
                        string Ru2 = mslFrpInfo.Substring(IndexofA2 + 12);
                        string a102 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                        list2.Add(a102);

                        try
                        {
                            Ping pingSender = new Ping();
                            PingReply reply = pingSender.Send(a101, 2000); // 替换成您要 ping 的 IP 地址
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (reply.Status == IPStatus.Success)
                                {
                                    // 节点在线，可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    serversList.Items.Add(a100 + "(延迟：" + roundTripTime + "ms)");
                                }
                                else
                                {
                                    serversList.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                                }
                            });
                        }
                        catch
                        {
                            serversList.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                        }

                        string strtempa3 = "min_open_port=";
                        int IndexofA03 = mslFrpInfo.IndexOf(strtempa3);
                        string Ru03 = mslFrpInfo.Substring(IndexofA03 + 14);
                        string a103 = Ru03.Substring(0, Ru03.IndexOf("\n"));
                        //MessageBox.Show(a103);
                        list3.Add(a103);

                        string strtemp4 = "max_open_port=";
                        int IndexofA4 = mslFrpInfo.IndexOf(strtemp4);
                        string Ru4 = mslFrpInfo.Substring(IndexofA4 + 14);
                        string a104 = Ru4.Substring(0, Ru4.IndexOf("\n"));
                        //MessageBox.Show(a104);
                        list4.Add(a104);
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
                WebClient MyWebClient1 = new WebClient();
                MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData1 = MyWebClient1.DownloadData(MainWindow.serverLink + "/msl/frpnotice");
                Dispatcher.Invoke(() =>
                {
                    gonggao.Content = Encoding.UTF8.GetString(pageData1);
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
                        frpc += "\tlocal_ip = 127.0.0.1\n";
                        frpc += "\tlocal_port = " + a100 + "\n";
                        frpc += "\tremote_port = " + n + "\n";
                        frpc += compressionArg + "\n";
                        frpc += "\n[udp]\n\ttype = udp\n";
                        frpc += "\tlocal_ip = 127.0.0.1\n";
                        frpc += "\tlocal_port = " + a200 + "\n";
                        frpc += "\tremote_port = " + n + "\n";
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
                DialogShow.ShowMsg(window, "点击确定后，开服器会弹出一个输入框，同时为您打开爱发电网站，您需要在爱发电购买的时候备注自己的QQ号（纯数字，不要夹带其他内容），购买完毕后，返回开服器，将您的QQ号输入进弹出的输入框中，开服器会自动为您获取密码。\n（注：付费密码在购买后会在服务器保存30分钟，请及时返回开服器进行操作，如果超时，请自行添加QQ：483232994来手动获取）", "购买须知");
                Process.Start("https://afdian.net/a/makabaka123");
                string text = "";
                bool input = DialogShow.ShowInput(window, "输入您在爱发电备注的QQ号：", out text);
                if (input)
                {
                    Dialog _dialog = null;
                    try
                    {
                        _dialog = Dialog.Show(new TextDialog("获取密码中，请稍等……"));
                        var ret = await Task.Run(() => Functions.Post("getpassword", 1, text, "http://111.180.189.249:7004"));
                        window.Focus();
                        _dialog.Close();
                        if (ret != "Err")
                        {
                            bool dialog = DialogShow.ShowMsg(window, "您的付费密码为：" + ret + " 请牢记！\n是否将其填入密码框内？", "获取成功！", true, "取消", "确定");
                            if (dialog)
                            {
                                passwordBox.Password = ret;
                                //Clipboard.SetDataObject(ret);
                            }
                        }
                        else
                        {
                            DialogShow.ShowMsg(window, "您的密码可能长时间无人获取，已经超时！请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                        }
                    }
                    catch
                    {
                        window.Focus();
                        _dialog.Close();
                        DialogShow.ShowMsg(window, "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                    }
                }
            }
            else
            {
                Process.Start("https://www.openfrp.net/");
            }
        }
    }
}
