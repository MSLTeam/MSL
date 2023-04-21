using HandyControl.Controls;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using Window = System.Windows.Window;
using MessageBox = System.Windows.MessageBox;
using MSL.controls;

namespace MSL.pages
{
    /// <summary>
    /// OnlinePage.xaml 的交互逻辑
    /// </summary>
    public partial class OnlinePage : Page
    {
        public delegate void DelReadStdOutput(string result);
        public static Process FRPCMD = new Process();
        public event DelReadStdOutput ReadStdOutput;
        string _dnfrpc;
        bool isMaster;
        public OnlinePage()
        {
            InitializeComponent();
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc");
                if(a.IndexOf("role = visitor")+1!=0)
                {
                    visiterBtn.IsChecked = true;
                }
                else
                {
                    masterBtn.IsChecked = true;
                }
            }
            else
            {
                var mainwindow=(MainWindow)Window.GetWindow(this);
                DialogShow.ShowMsg(mainwindow, "注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！\r\n该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！", "警告");
                masterBtn.IsChecked = true;
            }
        }

        private void masterBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 == 0)
                {
                    masterQQ.Text = a.Substring(a.LastIndexOf("[") + 1, a.LastIndexOf("]") - (a.LastIndexOf("[") + 1));
                    masterKey.Text = a.Substring(a.IndexOf("sk = ") + 5, a.LastIndexOf("\r\n") - (a.IndexOf("sk = ") + 5));
                    string port = a.Substring(a.IndexOf("local_port = ") + 13);
                    masterPort.Text = port.Substring(0, a.IndexOf("\r\n") - 3);
                }
                
                masterQQ.IsEnabled = true;
                masterKey.IsEnabled=true;
                masterPort.IsEnabled=true;
                createRoom.IsEnabled = true;

                visiterQQ.IsEnabled = false;
                visiterKey.IsEnabled = false;
                visiterPort.IsEnabled = false;
                joinRoom.IsEnabled = false;
            }
            else
            {
                masterQQ.IsEnabled = true;
                masterKey.IsEnabled = true;
                masterPort.IsEnabled = true;
                createRoom.IsEnabled = true;

                visiterQQ.IsEnabled = false;
                visiterKey.IsEnabled = false;
                visiterPort.IsEnabled = false;
                joinRoom.IsEnabled = false;
            }
        }

        private void visiterBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    string qq = a.Substring(a.IndexOf("server_name = ") + 14);
                    visiterQQ.Text = qq.Substring(0, a.IndexOf("\r\n") - 3);
                    visiterKey.Text = a.Substring(a.IndexOf("sk = ") + 5, a.LastIndexOf("\r\n") - (a.IndexOf("sk = ") + 5));
                    string port = a.Substring(a.IndexOf("bind_port = ") + 12);
                    visiterPort.Text = port.Substring(0, a.IndexOf("\r\n") - 3);
                }

                visiterQQ.IsEnabled = true;
                visiterKey.IsEnabled = true;
                visiterPort.IsEnabled = true;
                joinRoom.IsEnabled = true;
                

                masterQQ.IsEnabled = false;
                masterKey.IsEnabled = false;
                masterPort.IsEnabled = false;
                createRoom.IsEnabled = false;
            }
            else
            {
                visiterQQ.IsEnabled = true;
                visiterKey.IsEnabled = true;
                visiterPort.IsEnabled = true;
                joinRoom.IsEnabled = true;

                masterQQ.IsEnabled = false;
                masterKey.IsEnabled = false;
                masterPort.IsEnabled = false;
                createRoom.IsEnabled = false;
            }
        }

        private void createRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (createRoom.Content.ToString() != "关闭房间")
                {
                    string a = "[common]\r\nserver_port = 7000\r\nserver_addr = 8.218.191.134\r\n\r\n[" + masterQQ.Text + "]\r\ntype = xtcp\r\nlocal_ip = 127.0.0.1\r\nlocal_port = " + masterPort.Text + "\r\nsk = " + masterKey.Text + "\r\n";
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc", a);
                    isMaster = true;
                    visiterBtn.IsEnabled = false;
                    StartFrpc();
                }
                else
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    visiterBtn.IsEnabled = true;
                    Growl.Success("关闭成功！");
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    createRoom.Content = "点击创建房间";
                }
            }
            catch
            {
                try
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                }
                catch
                {
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    { }
                }
                createRoom.Content = "点击创建房间";
            }
        }

        private void joinRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (joinRoom.Content.ToString() != "退出房间")
                {
                    string a = "[common]\r\nserver_port = 7000\r\nserver_addr = 8.218.191.134\r\n\r\n[p2p_ssh_visitor]\r\ntype = xtcp\r\nrole = visitor\r\nbind_addr = 127.0.0.1\r\nbind_port = " + visiterPort.Text + "\r\nserver_name = " + visiterQQ.Text + "\r\nsk = " + visiterKey.Text + "\r\n";
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\P2Pfrpc", a);
                    isMaster = false;
                    masterBtn.IsEnabled = false;
                    StartFrpc();
                }
                else
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    masterBtn.IsEnabled = true;
                    Growl.Success("关闭成功！");
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    joinRoom.Content = "点击加入房间";
                }
            }
            catch
            {
                try
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    FRPCMD.CancelOutputRead();
                    FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                }
                catch
                {
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    { }
                }
                joinRoom.Content = "点击加入房间";
            }
        }

        private void StartFrpc()
        {
            try
            {//内网映射版本检测
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
                catch
                {
                    return;
                }
                if (isMaster)
                {
                    createRoom.Content = "关闭房间";
                }
                else
                {
                    joinRoom.Content = "退出房间";
                }
                frpcOutlog.Text = "";
                FRPCMD.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc.exe";
                FRPCMD.StartInfo.Arguments = "-c P2Pfrpc";
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
        void RefreshLink()
        {
            WebClient MyWebClient = new WebClient();
            byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/otherdownload.json");
            string _javaList = Encoding.UTF8.GetString(pageData);

            JObject javaList0 = JObject.Parse(_javaList);
            _dnfrpc = javaList0["frpc"].ToString();
        }

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
                    Growl.Error("桥接失败！");
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        }
                        catch
                        {}
                    }
                    if (isMaster)
                    {
                        createRoom.Content = "点击创建房间";
                        visiterBtn.IsEnabled = true;
                    }
                    else
                    {
                        joinRoom.Content = "点击加入房间";
                        masterBtn.IsEnabled = true;
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "登录服务器成功！\n";
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "桥接成功！\n";
                    Growl.Success("桥接完成！");
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "桥接失败！\n";
                    Growl.Error("桥接失败！");
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        //ReadStdOutput = null;
                        FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            //ReadStdOutput = null;
                            FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        }
                        catch
                        {}
                    }
                    if (isMaster)
                    {
                        createRoom.Content = "点击创建房间";
                        visiterBtn.IsEnabled = true;
                    }
                    else
                    {
                        joinRoom.Content = "点击加入房间";
                        masterBtn.IsEnabled = true;
                    }
                }
            }
            frpcOutlog.ScrollToEnd();
        }

        private void onlineHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.waheal.top/#/?id=msl%e7%82%b9%e5%af%b9%e7%82%b9%e8%81%94%e6%9c%ba%e6%95%99%e7%a8%8b");
        }
    }
}
