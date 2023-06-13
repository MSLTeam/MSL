using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static HandyControl.Tools.Interop.InteropValues;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// FrpcPage.xaml 的交互逻辑
    /// </summary>
    public partial class FrpcPage : Page
    {
        public delegate void DelReadStdOutput(string result);
        public static Process FRPCMD = new Process();
        public event DelReadStdOutput ReadStdOutput;
        string _dnfrpc;
        public FrpcPage()
        {
            MainWindow.AutoOpenFrpc += AutoStartFrpc;
            InitializeComponent();
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
        }

        private void StartFrpc()
        {
            try
            {
                startfrpc.Content = "关闭内网映射";
                FRPCMD.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc.exe";
                FRPCMD.StartInfo.Arguments = "-c frpc";
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory + "MSL");
                FRPCMD.StartInfo.CreateNoWindow = true;
                FRPCMD.StartInfo.UseShellExecute = false;
                FRPCMD.StartInfo.RedirectStandardInput = true;
                FRPCMD.StartInfo.RedirectStandardOutput = true;
                FRPCMD.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                FRPCMD.Start();
                FRPCMD.BeginOutputReadLine();
            }
            catch (Exception e)
            {
                MessageBox.Show("出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AutoStartFrpc()
        {
            try
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    startfrpc.Content = "关闭内网映射";
                }));
                FRPCMD.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc.exe";
                FRPCMD.StartInfo.Arguments = "-c frpc";
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory + "MSL");
                FRPCMD.StartInfo.CreateNoWindow = true;
                FRPCMD.StartInfo.UseShellExecute = false;
                FRPCMD.StartInfo.RedirectStandardInput = true;
                FRPCMD.StartInfo.RedirectStandardOutput = true;
                FRPCMD.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                FRPCMD.Start();
                FRPCMD.BeginOutputReadLine();
            }
            catch (Exception e)
            {
                MessageBox.Show("出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }//the same of above
        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }
        private void ReadStdOutputAction(string msg)
        {
            frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
            if (msg.IndexOf("login") + 1 != 0)
            {
                if (msg.IndexOf("failed") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.IndexOf("invalid meta token") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "QQ号密码填写错误或付费资格已过期，请重新配置或续费！\n";
                    }
                    else if (msg.IndexOf("user or meta token can not be empty") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "用户名或密码不能为空！\n";
                    }
                    else if(msg.IndexOf("i/o timeout") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "连接超时，该节点可能下线，请重新配置！\n";
                    }
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                        catch
                        {
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "登录服务器成功！\n";
                }
                if (msg.IndexOf("match") + 1 != 0 && msg.IndexOf("token") + 1 != 0)
                {
                    try
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "重新连接服务器...\n";
                        Thread.Sleep(200);
                        string frpcserver = GetFrpcIP().Substring(0, GetFrpcIP().IndexOf(".")) + "*";
                        int frpcserver2 = GetFrpcIP().IndexOf(".") + 1;
                        WebClient MyWebClient = new WebClient();
                        MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                        byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                        string @string = Encoding.UTF8.GetString(pageData);
                        int IndexofA = @string.IndexOf(frpcserver);
                        string Ru = @string.Substring(IndexofA + frpcserver2);
                        string a111 = Ru.Substring(0, Ru.IndexOf("*"));
                        byte[] pageData2 = new WebClient().DownloadData(a111);
                        string pageHtml = Encoding.UTF8.GetString(pageData2);
                        string aaa = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc");
                        int IndexofA2 = aaa.IndexOf("token = ");
                        string Ru2 = aaa.Substring(IndexofA2);
                        string a112 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                        aaa = aaa.Replace(a112, "token = " + pageHtml);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc", aaa);
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= p_OutputDataReceived;
                        StartFrpc();
                    }
                    catch (Exception aa)
                    {
                        MessageBox.Show("内网映射桥接失败！请查看是否有杀毒软件删除了frpc.exe并重启开服器再试！\n错误代码：" + aa.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                        Growl.Error("内网映射桥接失败！", "");
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            FRPCMD.OutputDataReceived -= p_OutputDataReceived;
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                        catch
                        {
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                    }
                }
            }
            if (msg.IndexOf("reconnect") + 1 != 0 && msg.IndexOf("error") + 1 != 0 && msg.IndexOf("token") + 1 != 0)
            {
                try
                {
                    frpcOutlog.Text = frpcOutlog.Text + "重新连接服务器...\n";
                    Thread.Sleep(200);
                    string frpcserver = GetFrpcIP().Substring(0, GetFrpcIP().IndexOf(".")) + "*";
                    int frpcserver2 = GetFrpcIP().IndexOf(".") + 1;
                    WebClient MyWebClient = new WebClient();
                    MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                    string @string = Encoding.UTF8.GetString(pageData);
                    int IndexofA = @string.IndexOf(frpcserver);
                    string Ru = @string.Substring(IndexofA + frpcserver2);
                    string a111 = Ru.Substring(0, Ru.IndexOf("*"));
                    byte[] pageData2 = new WebClient().DownloadData(a111);
                    string pageHtml = Encoding.UTF8.GetString(pageData2);
                    string aaa = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc");
                    int IndexofA2 = aaa.IndexOf("token = ");
                    string Ru2 = aaa.Substring(IndexofA2);
                    string a112 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                    aaa = aaa.Replace(a112, "token = " + pageHtml);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc", aaa);
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= p_OutputDataReceived;
                    StartFrpc();
                }
                catch (Exception aa)
                {
                    MessageBox.Show("内网映射桥接失败！请重试！\n错误代码：" + aa.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                    catch
                    {
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接成功！\n";
                    Growl.Success("内网映射桥接完成，您可复制IP进入游戏了！");
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.IndexOf("port already used") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "本地端口被占用，请不要频繁开关内网映射或等待一分钟再试。\n若仍然占用，请尝试手动结束frpc.exe或重启电脑再试。\n";
                    }
                    else if (msg.IndexOf("port not allowed") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "远程端口被占用，请不要频繁开关内网映射或等待一分钟再试。\n若仍然占用，请尝试重新配置一下再试！\n";
                    }
                    else if (msg.IndexOf("proxy name") + 1 != 0 && msg.IndexOf("already in use") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "此QQ号已被占用！请不要频繁开关内网映射或等待一分钟再试。\n若仍然占用，请尝试手动结束frpc.exe或重启电脑再试。\n";
                    }
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        //ReadStdOutput = null;
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            //ReadStdOutput = null;
                            FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                        catch
                        {
                            setfrpc.IsEnabled = true;
                            startfrpc.Content = "启动内网映射";
                        }
                    }
                }
            }
            frpcOutlog.ScrollToEnd();
        }

        private void setfrpc_Click(object sender, RoutedEventArgs e)
        {
            SetFrpc fw = new SetFrpc();
            var mainwindow = (MainWindow)Window.GetWindow(this);
            fw.Owner = mainwindow;
            fw.ShowDialog();
            try
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc"))
                {
                    Thread thread = new Thread(GetFrpcInfo);
                    thread.Start();
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试:" + "m0x3");
            }
        }

        void RefreshLink()
        {
            WebClient MyWebClient = new WebClient();
            byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/otherdownload.json");
            string _javaList = Encoding.UTF8.GetString(pageData);

            JObject javaList0 = JObject.Parse(_javaList);
            _dnfrpc = javaList0["frpc"].ToString();
        }
        private void startfrpc_Click(object sender, RoutedEventArgs e)
        {
            if (startfrpc.Content.ToString() == "启动内网映射")
            {
                //内网映射版本检测
                try
                {
                    StreamReader reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json");
                    JObject jobject2 = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();
                    if (jobject2["frpcversion"] == null)
                    {
                        string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject.Add("frpcversion", "5");
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc.exe"))
                        {
                            RefreshLink();
                            var mwindow = (MainWindow)Window.GetWindow(this);
                            DialogShow.ShowDownload(mwindow, _dnfrpc, AppDomain.CurrentDomain.BaseDirectory + "MSL", "frpc.exe", "下载内网映射中...");
                            _dnfrpc = "";
                        }
                    }
                    else if (jobject2["frpcversion"].ToString() != "5")
                    {
                        RefreshLink();
                        var mwindow = (MainWindow)Window.GetWindow(this);
                        DialogShow.ShowDownload(mwindow, _dnfrpc, AppDomain.CurrentDomain.BaseDirectory + "MSL", "frpc.exe", "更新内网映射中...");
                        _dnfrpc = "";
                        JObject jobject3 = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", Encoding.UTF8));
                        jobject3["frpcversion"] = "5";
                        string convertString2 = Convert.ToString(jobject3);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", convertString2, Encoding.UTF8);
                    }
                    else if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc.exe"))
                    {
                        RefreshLink();
                        var mwindow = (MainWindow)Window.GetWindow(this);
                        DialogShow.ShowDownload(mwindow, _dnfrpc, AppDomain.CurrentDomain.BaseDirectory + "MSL", "frpc.exe", "下载内网映射中...");
                        _dnfrpc = "";
                    }
                }
                catch { }
                StartFrpc();
                Growl.Success("内网映射启动成功，请耐心等待桥接完成！");
                setfrpc.IsEnabled = false;
                frpcOutlog.Text = "启动中，请稍后……\n";
                return;
            }
            else
            {
                try
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    Growl.Success("内网映射关闭成功！");
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    setfrpc.IsEnabled = true;
                    startfrpc.Content = "启动内网映射";
                }
                catch
                {
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                    catch
                    {
                        setfrpc.IsEnabled = true;
                        startfrpc.Content = "启动内网映射";
                    }
                }
            }
        }

        private void copyFrpc_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(frplab3.Text.ToString());
        }

        void GetFrpcInfo()
        {
            try
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    copyFrpc.IsEnabled = true;
                    startfrpc.IsEnabled = true;
                    frplab1.Text = "检测节点信息中……";
                }));

                string configText = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc");
                // 读取每一行
                string[] lines = configText.Split('\n');

                // 节点名称
                string nodeName = lines[0].TrimStart('#').Trim();

                // 服务器地址
                string serverAddr = "";
                int serverPort = 0;
                string remotePort = "";
                string frpcType = "";
                bool readServerInfo = true;  // 是否继续读取服务器信息
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("type") && frpcType != "")
                    {
                        // 遇到第二个type时停止读取服务器信息
                        readServerInfo = false;
                        break;
                    }
                    else if (lines[i].StartsWith("type") && readServerInfo)
                    {
                        frpcType = lines[i].Split('=')[1].Trim();
                    }
                    else if (lines[i].StartsWith("server_addr") && readServerInfo)
                    {
                        serverAddr = lines[i].Split('=')[1].Trim();
                    }
                    else if (lines[i].StartsWith("server_port") && readServerInfo)
                    {
                        serverPort = int.Parse(lines[i].Split('=')[1].Trim());
                    }
                    else if (lines[i].StartsWith("remote_port") && readServerInfo)
                    {
                        remotePort = lines[i].Split('=')[1].Trim();
                    }
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    if (!readServerInfo)
                    {
                        frplab3.Text = "Java版：" + serverAddr + ":" + remotePort + "\n基岩版：IP:" + serverAddr + " 端口:" + remotePort;
                    }
                    else
                    {
                        if (frpcType == "udp")
                        {
                            frplab3.Text = "IP:" + serverAddr + " 端口:" + remotePort;
                        }
                        else
                        {
                            frplab3.Text = serverAddr + ":" + remotePort;
                        }
                    }
                }));
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(serverAddr, 2000); // 替换成您要 ping 的 IP 地址
                if (reply.Status == IPStatus.Success)
                {
                    // 节点在线，可以获取延迟等信息
                    int roundTripTime = (int)reply.RoundtripTime;
                    Dispatcher.Invoke(new Action(delegate
                    {
                        frplab1.Text = nodeName + "  延迟：" + roundTripTime + "ms";
                    }));
                }
                else
                {
                    // 节点离线
                    Dispatcher.Invoke(new Action(delegate
                    {
                        frplab1.Text = nodeName + "  节点离线，请重新配置！";
                    }));
                }
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    frplab1.Text = "获取节点信息失败，建议重新配置！";
                }));
            }
        }
        string GetFrpcIP()
        {
            string configText = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc");
            // 读取每一行
            string[] lines = configText.Split('\n');

            // 服务器地址
            string serverAddr = "";
            bool readServerInfo = true;  // 是否继续读取服务器信息
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("server_addr") && readServerInfo)
                {
                    serverAddr = lines[i].Split('=')[1].Trim();
                    break;
                }
            }
            return serverAddr;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc"))
                {
                    Thread thread=new Thread(GetFrpcInfo);
                    thread.Start();
                }
                else
                {
                    copyFrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frplab1.Text = "未检测到内网映射配置，请点击 配置 按钮以配置";
                    frplab3.Text = "无";
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试:" + "m0x3");
            }
        }
    }
}
