using HandyControl.Controls;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL
{
    /// <summary>
    /// SetFrpc.xaml 的交互逻辑
    /// </summary>
    public partial class SetFrpc : Window
    {
        List<string> list1=new List<string>();
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
            LoadingCircle loadingCircle = new LoadingCircle();
            loadingCircle.VerticalAlignment = VerticalAlignment.Top;
            loadingCircle.HorizontalAlignment = HorizontalAlignment.Left;
            loadingCircle.Margin=new Thickness(120,150,0,0);
            BodyGrid.Children.Add(loadingCircle);
            BodyGrid.RegisterName("loadingBar", loadingCircle);
            Thread thread = new Thread(GetFrpsInfo);
            thread.Start();
        }
        void GetFrpsInfo()
        {
            try
            {
                /*
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                pageHtml = Encoding.UTF8.GetString(pageData);
                */
                pageHtml = Functions.Get("frplist");
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    MessageBox.Show("连接服务器失败！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }));
                /*
                try
                {
                    MainWindow.serverLink = "http://msl.waheal.top";
                    WebClient MyWebClient = new WebClient();
                    MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                    pageHtml = Encoding.UTF8.GetString(pageData);
                }
                catch
                {
                    MessageBox.Show("连接服务器失败！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    pageHtml = "";
                    Close();
                }
                */
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

                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(a101, 2000); // 替换成您要 ping 的 IP 地址
                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (reply.Status == IPStatus.Success)
                        {
                            // 节点在线，可以获取延迟等信息
                            int roundTripTime = (int)reply.RoundtripTime;
                            listBox1.Items.Add(a100 + "(延迟：" + roundTripTime + "ms)");
                        }
                        else
                        {
                            listBox1.Items.Add(a100 + "(已下线，检测失败)");
                        }
                    }));

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

                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(a101, 2000); // 替换成您要 ping 的 IP 地址
                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (reply.Status == IPStatus.Success)
                        {
                            // 节点在线，可以获取延迟等信息
                            int roundTripTime = (int)reply.RoundtripTime;
                            listBox1.Items.Add(a100 + "(延迟：" + roundTripTime + "ms)");
                        }
                        else
                        {
                            listBox1.Items.Add(a100 + "(已下线，检测失败)");
                        }
                    }));

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
            try
            {
                WebClient MyWebClient1 = new WebClient();
                MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                Byte[] pageData1 = MyWebClient1.DownloadData(MainWindow.serverLink + @"/msl/frpcgg.txt");
                Dispatcher.Invoke(new Action(delegate
                {
                    gonggao.Content = Encoding.UTF8.GetString(pageData1);
                }));
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    gonggao.Content = "无公告";
                }));
            }
            Dispatcher.Invoke(new Action(delegate
            {
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                BodyGrid.Children.Remove(loadingCircle);
                BodyGrid.UnregisterName("loadingBar");
            }));
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
                        MessageBox.Show("出现错误，请确保内网端口和QQ号不为空后再试!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (frpcType.SelectedIndex == 0)
                    {
                        frptype = "tcp";
                    }
                    if (frpcType.SelectedIndex == 1)
                    {
                        frptype = "udp";
                    }
                    if (frpcType.SelectedIndex == 2)
                    {
                        string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                        string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|"));
                        string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);

                        if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                        {
                            string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "token = \n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine(frpc);
                            sw.Flush();
                            sw.Dispose();
                            sw.Close();
                            fs.Close();
                        }
                        else
                        {
                            string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "token = \n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine(frpc);
                            sw.Flush();
                            sw.Dispose();
                            sw.Close();
                            fs.Close();
                        }
                    }
                    else
                    {
                        if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")== -1)
                        {
                            string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "token = \n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine(frpc);
                            sw.Flush();
                            sw.Dispose();
                            sw.Close();
                            fs.Close();
                        }
                        else
                        {
                            string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "token = \n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine(frpc);
                            sw.Flush();
                            sw.Dispose();
                            sw.Close();
                            fs.Close();
                        }
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
                    if (textBox1.Text == "" || textBox2.Text == "")
                    {
                        MessageBox.Show("出现错误，请确保内网端口和QQ号不为空后再试：" + "w3x2", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (frpcType.SelectedIndex == 0)
                    {
                        frptype = "tcp";
                    }
                    if (frpcType.SelectedIndex == 1)
                    {
                        frptype = "udp";
                    }
                    //如果选择的是双协议
                    if (frpcType.SelectedIndex == 2)
                    {
                        //付费协议=quic
                        if (usePaidProtocol.SelectedIndex == 0)
                        {
                            string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                            string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|"));
                            string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);

                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + (int.Parse(list2[a].ToString()) + 1).ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = quic\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + (int.Parse(list2[a].ToString()) + 1).ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = quic\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                        //付费协议=kcp
                        if (usePaidProtocol.SelectedIndex == 1)
                        {
                            string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                            string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|"));
                            string a200 = Ru2.Substring(Ru2.IndexOf("|") + 1);

                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = kcp\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = kcp\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                        //无付费协议
                        if (usePaidProtocol.SelectedIndex == 2)
                        {
                            string a100 = textBox1.Text.Substring(0, textBox1.Text.IndexOf("|"));
                            string Ru2 = textBox1.Text.Substring(textBox1.Text.IndexOf("|") + 1);
                            string a200 = Ru2.Substring(Ru2.IndexOf("|"));

                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\n" + "\n[" + textBox2.Text + "TCP]\ntype = tcp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a100 + "\nremote_port = " + n + "\n\n[" + textBox2.Text + "UDP]\ntype = udp" + "\nlocal_ip = 127.0.0.1\nlocal_port = " + a200 + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                    }
                    else //不是双协议的情况
                    {
                        //付费协议=quic
                        if (usePaidProtocol.SelectedIndex == 0)
                        {
                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + (int.Parse(list2[a].ToString()) + 1).ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = quic\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + (int.Parse(list2[a].ToString()) + 1).ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = quic\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                        //付费协议=kcp
                        if (usePaidProtocol.SelectedIndex == 1)
                        {
                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = kcp\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\nprotocol = kcp\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                        //无付费协议
                        if (usePaidProtocol.SelectedIndex == 2)
                        {
                            if (listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(") == -1)
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString() + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                            else
                            {
                                string frpc = "#" + listBox1.Items[listBox1.SelectedIndex].ToString().Substring(0, listBox1.Items[listBox1.SelectedIndex].ToString().IndexOf("(")) + "\n[common]\nserver_port = " + list2[a].ToString() + "\nserver_addr = " + list1[a].ToString() + "\n" + "user = " + textBox2.Text + "\n" + "meta_token = " + textBox3.Text + "\n" + "\n[" + textBox2.Text + "]\ntype = " + frptype + "\nlocal_ip = 127.0.0.1\nlocal_port = " + textBox1.Text + "\nremote_port = " + n;
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", frpc);
                            }
                        }
                    }
                    /*string frpc1 = list1[a].ToString() + ":" + n;
                    MainWindow.frpc = frpc1.Replace("\r", "");
                    StreamReader reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json");
                    JsonTextReader jsonTextReader = new JsonTextReader(reader);
                    JObject jsonObject = (JObject)JToken.ReadFrom(jsonTextReader);
                    jsonObject["frpc"] = MainWindow.frpc;
                    reader.Close();
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", output);
                    //MessageBox.Show("映射配置成功，请您点击“启动内网映射”以启动映射！\n连接IP为：\n" + MainWindow.frpc, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    */
                    DialogShow.ShowMsg(this, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息", false, "确定");
                    //MessageDialog messageDialog = new MessageDialog();
                    //messageDialog.Owner = this;
                    //messageDialog.ShowDialog();
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

        private void gotoWeb_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://afdian.net/a/makabaka123");
        }
    }
}
