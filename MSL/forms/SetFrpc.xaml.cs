using HandyControl.Controls;
using MSL.controls;
using MSL.controls.OfAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
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
        readonly List<string> list1 = new List<string>();
        readonly List<string> list2 = new List<string>();
        readonly List<string> list3 = new List<string>();
        readonly List<string> list4 = new List<string>();
        public SetFrpc() { InitializeComponent(); }
        string pageHtml;
        public string frpchome = $@"{AppDomain.CurrentDomain.BaseDirectory}MSL\frpc.ini";

        public string id;
        public string auth;
        public string token;
        public string proxy;
        public string Password;
        public JArray jArray;
        public Dictionary<string, string> nodelist;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WebClient MyWebClient = new WebClient
                {
                    Credentials = CredentialCache.DefaultCredentials
                };
                byte[] p = await MyWebClient.DownloadDataTaskAsync(MainWindow.serverLink + "/msl/frp");
                //if (Encoding.UTF8.GetString(p) == "0") frpProvider.Items.Remove(frpProvider.Items[1]);
            }
            catch { }
            await Task.Run(() => GetFrpsInfo());
        }
        void GetFrpsInfo(string path = "frplist", string url = "")
        {
            Dispatcher.Invoke(() =>
            {
                //frpProvider.IsEnabled = false;
                LoadingCircle loadingCircle = new LoadingCircle
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                _ = MainGrid.Children.Add(loadingCircle);
                MainGrid.RegisterName("loadingBar", loadingCircle);
            });

            try
            {
                if (url == "") pageHtml = Functions.Get(path);
                else pageHtml = Functions.Get(path, url);
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
                                    // 节点在线,可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    _ = listNodes.Items.Add(a100 + "(延迟:" + roundTripTime + "ms)");
                                }
                                //else _ = listNodes.Items.Add(a100 + "(已下线,检测失败)");
                            });
                        }
                        //catch { _ = listNodes.Items.Add(a100 + "(已下线,检测失败)"); }
                        catch { }

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
                                    // 节点在线,可以获取延迟等信息
                                    int roundTripTime = (int)reply.RoundtripTime;
                                    _ = listNodes.Items.Add(a100 + "(延迟:" + roundTripTime + "ms)");
                                }
                                /*
                                else
                                {
                                    _ = listNodes.Items.Add(a100 + "(已下线,检测失败)");
                                }
                                */
                            });
                        }
                        //catch { _ = listNodes.Items.Add(a100 + "(已下线,检测失败)"); }
                        catch { }

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
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    _ = MessageBox.Show("连接服务器失败" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            Dispatcher.Invoke(() =>
            {
                if (File.Exists(frpchome))
                {
                    string text = File.ReadAllText(frpchome);
                    string pattern = @"user\s*=\s*(\w+)\s*meta_token\s*=\s*(\w+)";
                    Match match = Regex.Match(text, pattern);

                    if (match.Success)
                    {
                        textBoxQQ.Text = match.Groups[1].Value;
                    }
                }
                frpProvider.IsEnabled = true;
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                MainGrid.Children.Remove(loadingCircle);
                MainGrid.UnregisterName("loadingBar");
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listNodes.SelectedIndex == -1)
            {
                _ = DialogShow.ShowMsg(this, "需要选择一个节点", "信息");
                return;
            }
            string frptype = "";
            if (frpProvider.SelectedIndex == 0)
            {
                #region 创建传统的Frpc配置文件
                #region 付费节点
                if (listNodes.SelectedIndex > 1)
                {
                    try
                    {
                        bool input_passwd = DialogShow.ShowInput(this, "请输入密码", out string Password);
                        if (input_passwd)
                        {
                            int a = listNodes.SelectedIndex;
                            Random ran = new Random();
                            int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                            if (textBoxPort.Text == "" || textBoxQQ.Text == "")
                            {
                                _ = DialogShow.ShowMsg(this, "请确保内网端口和QQ号不为空", "错误");
                                return;
                            }
                            //string frptype = "";
                            string serverName = listNodes.Items[listNodes.SelectedIndex].ToString();
                            string compressionArg = "";
                            if (enableCompression.IsChecked == true) compressionArg = "use_compression = true\n";
                            if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                            if (frpcType.SelectedIndex == 0) frptype = "tcp";
                            else if (frpcType.SelectedIndex == 1) frptype = "udp";

                            string frpc = "#" + serverName + "\n[common]\n";
                            frpc += "server_port = " + list2[a].ToString() + "\n";
                            frpc += "server_addr = " + list1[a].ToString() + "\n";
                            frpc += "user = " + textBoxQQ.Text + "\n";
                            frpc += "token = \n";
                            if (frpcType.SelectedIndex == 2)
                            {
                                string a100 = textBoxPort.Text.Substring(0, textBoxPort.Text.IndexOf("|"));
                                string Ru2 = textBoxPort.Text.Substring(textBoxPort.Text.IndexOf("|"));
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
                                frpc += "local_port = " + textBoxPort.Text + "\n";
                                frpc += "remote_port = " + n + "\n";
                                frpc += compressionArg;
                            }
                            using (FileStream fs = new FileStream(frpchome, FileMode.Create, FileAccess.Write))
                            using (StreamWriter sw = new StreamWriter(fs)) sw.Write(frpc);

                            _ = DialogShow.ShowMsg(this, "Frpc配置已保存", "信息", false, "确定");
                            Close();
                        }
                        else
                        {
                            _ = DialogShow.ShowMsg(this, "请输入密码", "错误");
                        }
                    }
                    catch (Exception a) { _ = MessageBox.Show(a.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                #endregion
                #region 免费节点
                else
                {
                    try
                    {
                        int a = listNodes.SelectedIndex;
                        Random ran = new Random();
                        int n = ran.Next(int.Parse(list3[a].ToString()), int.Parse(list4[a].ToString()));
                        if (textBoxPort.Text == "" || textBoxQQ.Text == "")
                        {
                            _ = DialogShow.ShowMsg(this, "请确保没有漏填信息", "错误");
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

                        string serverName = listNodes.Items[listNodes.SelectedIndex].ToString();
                        string compressionArg = "";
                        if (enableCompression.IsChecked == true) compressionArg = "use_compression = true\n";
                        if (serverName.Contains("(")) serverName = serverName.Substring(0, serverName.IndexOf("("));
                        string frpc = "#" + serverName + "\n[common]\n";
                        frpc += "server_port = " + frpPort + "\n";
                        frpc += "server_addr = " + list1[a].ToString() + "\n";
                        frpc += "user = " + textBoxQQ.Text + "\n";
                        frpc += "meta_token = " + Password + "\n";
                        if (protocol != "") frpc += "protocol = " + protocol + "\n";

                        if (frpcType.SelectedIndex == 2)
                        {
                            string a100 = textBoxPort.Text.Substring(0, textBoxPort.Text.IndexOf("|"));
                            string Ru2 = textBoxPort.Text.Substring(textBoxPort.Text.IndexOf("|"));
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
                            frpc += "local_port = " + textBoxPort.Text + "\n";
                            frpc += "remote_port = " + n + "\n";
                            frpc += compressionArg;
                        }

                        File.WriteAllText(frpchome, frpc);

                        _ = DialogShow.ShowMsg(this, "映射配置成功", "信息", false, "确定");
                        Close();
                    }
                    catch (Exception a)
                    {
                        _ = MessageBox.Show("请确保选择了节点" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                #endregion
                #endregion
            }
            else if (frpProvider.SelectedIndex == 1)
            {
                #region 写入OpenFrp的配置信息
                if (textBoxPort.Text == "")
                {
                    _ = DialogShow.ShowMsg(this, "请确保内网端口不为空", "错误");
                    return;
                }
                string type = "tcp";
                if (frpcType.SelectedIndex == 0) type = "tcp";
                else if (frpcType.SelectedIndex == 1) type = "udp";
                bool zip;
                if ((bool)enableCompression.IsChecked) zip = true;
                else zip = false;
                string selected_node = listNodes.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(nodelist[selected_node]);
                else
                {
                    _ = MessageBox.Show("请确保选择了节点", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string port = textBoxPort.Text;
                APIControl control = new APIControl();
                (LoginMessage, string) proxy_name = control.CreateProxy(id, auth, type, port, zip, selected_node_id, jArray, this);
                try
                {
                    if (proxy_name.Item1 != null)
                    {
                        if (proxy_name.Item1.flag)
                        {
                            string proxy = control.GetUserNodeId(id, auth, proxy_name.Item2, this);
                            File.WriteAllText(frpchome, $"-u {token} -p {proxy}");
                        }
                        else
                        {
                            _ = DialogShow.ShowMsg(this, "创建隧道失败\n" + proxy_name.Item1.msg, "失败");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = DialogShow.ShowMsg(this, "创建隧道失败\n" + ex.Message, "失败");
                }

                #endregion
            }
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listNodes.SelectedIndex != -1)
            {
                if (listNodes.SelectedItem.ToString().IndexOf("付费") + 1 != 0)
                {
                    if (listNodes.SelectedItem.ToString().IndexOf("无加速协议") + 1 != 0)
                    {
                        usePaidProtocol.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        usePaidProtocol.Visibility = Visibility.Visible;
                    }
                    usePaidProtocol.Visibility = Visibility.Visible;
                }
                else
                {
                    usePaidProtocol.Visibility = Visibility.Hidden;
                }
            }
        }
        private void frpcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frpcType.SelectedIndex == 0) textBoxPort.Text = "25565";
            if (frpcType.SelectedIndex == 1) textBoxPort.Text = "19132";
            if (frpcType.SelectedIndex == 2) textBoxPort.Text = "25565|19132";
        }

        private async void gotoWeb_Click(object sender, RoutedEventArgs e)
        {
            if (frpProvider.SelectedIndex == 0)
            {
                _ = DialogShow.ShowMsg(this, "点击确定后,开服器会弹出一个输入框,同时为您打开爱发电网站,您需要在爱发电购买的时候备注自己的QQ号（纯数字,不要夹带其他内容）,购买完毕后,返回开服器,将您的QQ号输入进弹出的输入框中,开服器会自动为您获取密码。\n（注：付费密码在购买后会在服务器保存30分钟,请及时返回开服器进行操作,如果超时,请自行添加QQ：483232994来手动获取）", "购买须知");
                _ = Process.Start("https://afdian.net/a/makabaka123");
                bool input = DialogShow.ShowInput(this, "输入备注的QQ号", out string text);
                if (input)
                {
                    Dialog _dialog = null;
                    try
                    {
                        _dialog = Dialog.Show(new TextDialog("获取密码中"));
                        JObject patientinfo = new JObject
                        {
                            ["qq"] = text
                        };
                        string sendData = JsonConvert.SerializeObject(patientinfo);
                        string ret = await Task.Run(() => Functions.Post("getpassword", 0, sendData, "https://aifadian.waheal.top"));
                        _ = Focus();
                        _dialog.Close();
                        if (ret != "Err")
                        {
                            bool dialog = DialogShow.ShowMsg(this, "密码是" + ret + " 请妥善保存", "获取成功", true, "确定", "复制&确定");
                            if (dialog) Clipboard.SetDataObject(ret);
                            Password = ret;
                        }
                        else _ = DialogShow.ShowMsg(this, "您的密码可能长时间未被获取,已经超时", "获取失败");
                    }
                    catch
                    {
                        _ = Focus();
                        _dialog.Close();
                        _ = DialogShow.ShowMsg(this, "获取失败", "获取失败");
                    }
                }
            }
        }

        void frpProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                listNodes.SelectedIndex = -1;
                if (frpProvider.SelectedIndex == 0)
                {
                    usePaidProtocol.IsEnabled = true;
                    usePaidProtocol.SelectedIndex = 0;
                    listNodes.Items.Clear();
                    list1.Clear();
                    list2.Clear();
                    list3.Clear();
                    list4.Clear();
                    textBoxQQ.IsEnabled = true;
                    usePaidProtocol.IsEnabled= true;
                    _ = Task.Run(() => GetFrpsInfo());
                }
                else if (frpProvider.SelectedIndex == 1) //OpenFrp的登陆处理
                {
                    usePaidProtocol.SelectedIndex = 2;
                    usePaidProtocol.IsEnabled = false;
                    bool input_account = DialogShow.ShowInput(this, "OpenFrp的账户名/邮箱", out string account);
                    if (input_account)
                    {
                        bool input_paswd = DialogShow.ShowInput(this, account + "的密码", out string password);
                        if (input_paswd)
                        {
                            APIControl control = new APIControl();
                            (UserwithSessionID, string) login = control.Login(account, password, this);
                            UserwithSessionID userwithSessionID = login.Item1;
                            token = login.Item2;
                            auth = userwithSessionID.auth;
                            id = userwithSessionID.session;
                            if (userwithSessionID != null)
                            {
                                (Dictionary<string, string>, JArray) process = control.GetNodeList(userwithSessionID.session, userwithSessionID.auth, this);
                                Dictionary<string, string> item1 = process.Item1;
                                nodelist = item1;
                                jArray = process.Item2;
                                listNodes.Items.Clear();
                                list1.Clear();
                                list2.Clear();
                                list3.Clear();
                                list4.Clear();
                                textBoxQQ.IsEnabled = false;
                                usePaidProtocol.IsEnabled = false;
                                foreach (var node in item1)
                                {
                                    _ = listNodes.Items.Add(node.Key);
                                }
                            }
                        }
                        else
                        {
                            _ = DialogShow.ShowMsg(this, "请确保输入了密码", "登录失败");
                            frpProvider.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        _ = DialogShow.ShowMsg(this, "请确保输入了账户", "登录失败");
                        frpProvider.SelectedIndex = 0;
                    }
                }
                else if (frpProvider.SelectedIndex == 2)
                {
                    bool input_account = DialogShow.ShowInput(this, "账户Token", out string account_token);
                    if (input_account)
                    {
                        bool input_id = DialogShow.ShowInput(this, "隧道ID", out string proxy_id);
                        if (input_id)
                        {
                            File.WriteAllText(frpchome, $"-u {account_token} -p {proxy_id}");
                            _ = DialogShow.ShowMsg(this, "Frpc配置已保存", "信息");
                            frpProvider.SelectedIndex = 0;
                        }
                        else
                        {
                            _ = DialogShow.ShowMsg(this, "请输入隧道ID", "错误");
                            frpProvider.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        _ = DialogShow.ShowMsg(this, "请输入账户token", "错误");
                        frpProvider.SelectedIndex = 0;
                    }
                }
            }
        }
    }
}
