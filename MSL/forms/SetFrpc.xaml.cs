using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MSL
{
    /// <summary>
    /// SetFrpc.xaml 的交互逻辑
    /// </summary>
    public partial class SetFrpc : HandyControl.Controls.Window
    {
        List<string> list1 = new List<string>();
        List<string> list2 = new List<string>();
        List<string> list3 = new List<string>();
        List<string> list4 = new List<string>();
        public SetFrpc()
        {
            InitializeComponent();
        }
        string pageHtml;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GetFrpsInfo());
        }
        void GetFrpsInfo(string path = "frplist", string url = "")
        {
            Dispatcher.Invoke(() =>
            {
                LoadingCircle loadingCircle = new LoadingCircle();
                loadingCircle.VerticalAlignment = VerticalAlignment.Top;
                loadingCircle.HorizontalAlignment = HorizontalAlignment.Left;
                loadingCircle.Margin = new Thickness(120, 150, 0, 0);
                MainGrid.Children.Add(loadingCircle);
                MainGrid.RegisterName("loadingBar", loadingCircle);
            });

            try
            {
                if (url == "")
                {
                    pageHtml = Functions.Get(path);
                }
                else
                {
                    pageHtml = Functions.Get(path, url);
                }
                if (pageHtml.IndexOf("\r\n") != -1)
                {
                    while (pageHtml.IndexOf("#") != -1)
                    {
                        string strtempa = "#";
                        int IndexofA = pageHtml.IndexOf(strtempa);
                        string Ru = pageHtml.Substring(IndexofA + 1);
                        string a100 = Ru.Substring(0, Ru.IndexOf("\r\n"));

                        int IndexofA3 = pageHtml.IndexOf("#");
                        string Ru3 = pageHtml.Substring(IndexofA3 + 1);
                        pageHtml = Ru3;

                        string strtempa1 = "server_addr=";
                        int IndexofA1 = pageHtml.IndexOf(strtempa1);
                        string Ru1 = pageHtml.Substring(IndexofA1 + 12);
                        string a101 = Ru1.Substring(0, Ru1.IndexOf("\r\n"));
                        list1.Add(a101);

                        string strtempa2 = "server_port=";
                        int IndexofA2 = pageHtml.IndexOf(strtempa2);
                        string Ru2 = pageHtml.Substring(IndexofA2 + 12);
                        string a102 = Ru2.Substring(0, Ru2.IndexOf("\r\n"));
                        list2.Add(a102);

                        try
                        {
                            Ping pingSender = new Ping();
                            PingReply reply = pingSender.Send(a101, 2000); // 替换成您要 ping 的 IP 地址
                            Dispatcher.Invoke(() =>
                            {
                                if (reply.Status == IPStatus.Success)
                                {
                                    // 节点在线，可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    listBox1.Items.Add(a100 + "(延迟：" + roundTripTime + "ms)");
                                }
                                else
                                {
                                    listBox1.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                                }
                            });
                        }
                        catch
                        {
                            listBox1.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                        }

                        string strtempa3 = "min_open_port=";
                        int IndexofA03 = pageHtml.IndexOf(strtempa3);
                        string Ru03 = pageHtml.Substring(IndexofA03 + 14);
                        string a103 = Ru03.Substring(0, Ru03.IndexOf("\r\n"));
                        //MessageBox.Show(a103);
                        list3.Add(a103);

                        string strtemp4 = "max_open_port=";
                        int IndexofA4 = pageHtml.IndexOf(strtemp4);
                        string Ru4 = pageHtml.Substring(IndexofA4 + 14);
                        string a104 = Ru4.Substring(0, Ru4.IndexOf("\r\n"));
                        //MessageBox.Show(a104);
                        list4.Add(a104);
                    }
                }
                else
                {
                    while (pageHtml.IndexOf("#") != -1)
                    {
                        string strtempa = "#";
                        int IndexofA = pageHtml.IndexOf(strtempa);
                        string Ru = pageHtml.Substring(IndexofA + 1);
                        string a100 = Ru.Substring(0, Ru.IndexOf("\n"));

                        int IndexofA3 = pageHtml.IndexOf("#");
                        string Ru3 = pageHtml.Substring(IndexofA3 + 1);
                        pageHtml = Ru3;

                        string strtempa1 = "server_addr=";
                        int IndexofA1 = pageHtml.IndexOf(strtempa1);
                        string Ru1 = pageHtml.Substring(IndexofA1 + 12);
                        string a101 = Ru1.Substring(0, Ru1.IndexOf("\n"));
                        list1.Add(a101);

                        string strtempa2 = "server_port=";
                        int IndexofA2 = pageHtml.IndexOf(strtempa2);
                        string Ru2 = pageHtml.Substring(IndexofA2 + 12);
                        string a102 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                        list2.Add(a102);

                        try
                        {
                            Ping pingSender = new Ping();
                            PingReply reply = pingSender.Send(a101, 2000); // 替换成您要 ping 的 IP 地址
                            Dispatcher.Invoke(() =>
                            {
                                if (reply.Status == IPStatus.Success)
                                {
                                    // 节点在线，可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    listBox1.Items.Add(a100 + "(延迟：" + roundTripTime + "ms)");
                                }
                                else
                                {
                                    listBox1.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                                }
                            });
                        }
                        catch
                        {
                            listBox1.Items.Add(a100 + "(检测失败,可能被DDos或下线)");
                        }

                        string strtempa3 = "min_open_port=";
                        int IndexofA03 = pageHtml.IndexOf(strtempa3);
                        string Ru03 = pageHtml.Substring(IndexofA03 + 14);
                        string a103 = Ru03.Substring(0, Ru03.IndexOf("\n"));
                        //MessageBox.Show(a103);
                        list3.Add(a103);

                        string strtemp4 = "max_open_port=";
                        int IndexofA4 = pageHtml.IndexOf(strtemp4);
                        string Ru4 = pageHtml.Substring(IndexofA4 + 14);
                        string a104 = Ru4.Substring(0, Ru4.IndexOf("\n"));
                        //MessageBox.Show(a104);
                        list4.Add(a104);
                    }
                }
            }
            catch(Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("连接服务器失败！"+ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            try
            {
                if (url == "")
                {
                    WebClient MyWebClient1 = new WebClient();
                    MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData1 = MyWebClient1.DownloadData(MainWindow.serverLink + "/msl/frpcgg.txt");
                    Dispatcher.Invoke(() =>
                    {
                        gonggao.Content = Encoding.UTF8.GetString(pageData1);
                    });
                }
                else
                {
                    WebClient MyWebClient1 = new WebClient();
                    MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData1 = MyWebClient1.DownloadData(url+"/frpnotice.txt");
                    Dispatcher.Invoke(() =>
                    {
                        gonggao.Content = Encoding.UTF8.GetString(pageData1);
                    });
                }
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
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc"))
                {
                    string text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc");
                    string pattern = @"user\s*=\s*(\w+)\s*meta_token\s*=\s*(\w+)";
                    Match match = Regex.Match(text, pattern);

                    if (match.Success)
                    {
                        textBox2.Text = match.Groups[1].Value;
                        textBox3.Password = match.Groups[2].Value;
                    }
                }
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                MainGrid.Children.Remove(loadingCircle);
                MainGrid.UnregisterName("loadingBar");
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                DialogShow.ShowMsg(this, "您没有选择节点哦，请先选择一个节点！", "信息");
                return;
            }
            string frptype = "";
            if (textBox3.Visibility == Visibility.Hidden)
            {
                try
                {
                    int a = listBox1.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (textBox1.Text == "" || textBox2.Text == "")
                    {
                        DialogShow.ShowMsg(this, "请确保内网端口和QQ号不为空后再试！", "错误");
                        return;
                    }
                    //string frptype = "";
                    string serverName = listBox1.Items[listBox1.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true)
                    {
                        compressionArg = "use_compression = true\n";
                    }
                    if (serverName.Contains("("))
                    {
                        serverName = serverName.Substring(0, serverName.IndexOf("("));
                    }
                    if (frpcType.SelectedIndex == 0)
                    {
                        frptype = "tcp";
                    }
                    else if (frpcType.SelectedIndex == 1)
                    {
                        frptype = "udp";
                    }

                    string frpc = "#" + serverName + "\n[common]\n";
                    frpc += "server_port = " + list2[a].ToString() + "\n";
                    frpc += "server_addr = " + list1[a].ToString() + "\n";
                    frpc += "user = " + textBox2.Text + "\n";
                    frpc += "token = \n";
                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                        string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|"));
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
                        frpc += "local_port = " + textBox1.Text + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }
                    using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(frpc);
                    }

                    DialogShow.ShowMsg(this, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息", false, "确定");
                    this.Close();
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误：" + a.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    int a = listBox1.SelectedIndex;
                    Random ran = new Random();
                    int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                    if (textBox1.Text == "" || textBox2.Text == "" || textBox3.Password == "")
                    {
                        DialogShow.ShowMsg(this, "请确保内网端口、QQ号和密码不为空后再试！", "错误");
                        return;
                    }
                    //string frptype = "";
                    string protocol = "";
                    string frpPort = (int.Parse(list2[a].ToString())).ToString();

                    if (frpcType.SelectedIndex == 0)
                    {
                        frptype = "tcp";
                    }
                    else if (frpcType.SelectedIndex == 1)
                    {
                        frptype = "udp";
                    }
                    // 付费协议=quic
                    if (usePaidProtocol.SelectedIndex == 0)
                    {
                        protocol = "quic";
                        frpPort = (int.Parse(list2[a].ToString()) + 1).ToString();
                    }
                    // 付费协议=kcp
                    else if (usePaidProtocol.SelectedIndex == 1)
                    {
                        protocol = "kcp";
                    }

                    string serverName = listBox1.Items[listBox1.SelectedIndex].ToString();
                    string compressionArg = "";
                    if (enableCompression.IsChecked == true)
                    {
                        compressionArg = "use_compression = true\n";
                    }
                    if (serverName.Contains("("))
                    {
                        serverName = serverName.Substring(0, serverName.IndexOf("("));
                    }
                    string frpc = "#" + serverName + "\n[common]\n";
                    frpc += "server_port = " + frpPort + "\n";
                    frpc += "server_addr = " + list1[a].ToString() + "\n";
                    frpc += "user = " + textBox2.Text + "\n";
                    frpc += "meta_token = " + textBox3.Password + "\n";
                    if (protocol != "")
                    {
                        frpc += "protocol = " + protocol + "\n";
                    }

                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                        string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|"));
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
                        frpc += "local_port = " + textBox1.Text + "\n";
                        frpc += "remote_port = " + n + "\n";
                        frpc += compressionArg;
                    }

                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);

                    DialogShow.ShowMsg(this, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息", false, "确定");
                    this.Close();
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请确保选择节点后再试：" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                if (listBox1.SelectedItem.ToString().IndexOf("付费") + 1 != 0)
                {
                    if (listBox1.SelectedItem.ToString().IndexOf("无加速协议") + 1 != 0)
                    {
                        paidProtocolLabel.Visibility = Visibility.Hidden;
                        usePaidProtocol.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        paidProtocolLabel.Visibility = Visibility.Visible;
                        usePaidProtocol.Visibility = Visibility.Visible;
                    }
                    lab1.Margin = new Thickness(290, 30, 0, 0);
                    textBox1.Margin = new Thickness(330, 60, 0, 0);
                    lab2.Margin = new Thickness(290, 90, 0, 0);
                    textBox2.Margin = new Thickness(330, 120, 0, 0);
                    paidProtocolLabel.Visibility = Visibility.Visible;
                    usePaidProtocol.Visibility = Visibility.Visible;
                    paidPasswordLabel.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                }
                else
                {
                    lab1.Margin = new Thickness(290, 50, 0, 0);
                    textBox1.Margin = new Thickness(330, 85, 0, 0);
                    lab2.Margin = new Thickness(290, 135, 0, 0);
                    textBox2.Margin = new Thickness(330, 170, 0, 0);
                    paidProtocolLabel.Visibility = Visibility.Hidden;
                    usePaidProtocol.Visibility = Visibility.Hidden;
                    paidPasswordLabel.Visibility = Visibility.Hidden;
                    textBox3.Visibility = Visibility.Hidden;
                }
            }
        }
        private void frpcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frpcType.SelectedIndex == 0)
            {
                textBox1.Text = "25565";
            }
            if (frpcType.SelectedIndex == 1)
            {
                textBox1.Text = "19132";
            }
            if (frpcType.SelectedIndex == 2)
            {
                textBox1.Text = "25565|19132";
            }
        }

        private async void gotoWeb_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "点击确定后，开服器会弹出一个输入框，同时为您打开爱发电网站，您需要在爱发电购买的时候备注自己的QQ号（纯数字，不要夹带其他内容），购买完毕后，返回开服器，将您的QQ号输入进弹出的输入框中，开服器会自动为您获取密码。\n（注：付费密码在购买后会在服务器保存30分钟，请及时返回开服器进行操作，如果超时，请自行添加QQ：483232994来手动获取）", "购买须知");
            Process.Start("https://afdian.net/a/makabaka123");
            string text = "";
            bool input = DialogShow.ShowInput(this, "输入您在爱发电备注的QQ号：", out text);
            if (input)
            {
                Dialog _dialog = null;
                try
                {
                    _dialog = Dialog.Show(new TextDialog("获取密码中，请稍等……"));
                    JObject patientinfo = new JObject
                    {
                        ["qq"] = text
                    };
                    string sendData = JsonConvert.SerializeObject(patientinfo);
                    string ret = await Task.Run(() => Functions.Post("getpassword", 0, sendData, "https://aifadian.waheal.top"));
                    this.Focus();
                    _dialog.Close();
                    if (ret != "Err")
                    {
                        bool dialog = DialogShow.ShowMsg(this, "您的付费密码为：" + ret + " 请牢记！", "获取成功！", true, "确定", "复制&确定");
                        if (dialog)
                        {
                            Clipboard.SetDataObject(ret);
                        }
                    }
                    else
                    {
                        DialogShow.ShowMsg(this, "您的密码可能长时间无人获取，已经超时！请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                    }
                }
                catch
                {
                    this.Focus();
                    _dialog.Close();
                    DialogShow.ShowMsg(this, "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                }
            }
        }
    }
}
