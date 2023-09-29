using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
        //int paidServerCooldown = 0;
        private string _dnfrpc;
        public FrpcPage()
        {
            InitializeComponent();
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            FRPCMD.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            MainWindow.AutoOpenFrpc += AutoStartFrpc;
        }

        private void StartFrpc()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    startfrpc.Content = "关闭内网映射";
                });
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory + "MSL");
                FRPCMD.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc.exe";
                FRPCMD.StartInfo.Arguments = "-c frpc";
                FRPCMD.StartInfo.CreateNoWindow = true;
                FRPCMD.StartInfo.UseShellExecute = false;
                FRPCMD.StartInfo.RedirectStandardInput = true;
                FRPCMD.StartInfo.RedirectStandardOutput = true;
                FRPCMD.Start();
                FRPCMD.BeginOutputReadLine();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                FRPCMD.WaitForExit();
                FRPCMD.CancelOutputRead();
            }
            catch (Exception e)
            {
                MessageBox.Show("出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    setfrpc.IsEnabled = true;
                    startfrpc.Content = "启动内网映射";
                });
            }
        }
        private void AutoStartFrpc()
        {
            Task.Run(() => StartFrpc());
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
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.IndexOf("invalid meta token") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "QQ或密码填写错误或付费资格已过期，请重新配置或续费！\n";
                        Thread thread = new Thread(BuyPaidServe);
                        thread.Start();
                    }
                    else if (msg.IndexOf("user or meta token can not be empty") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "用户名或密码不能为空！\n";
                    }
                    else if (msg.IndexOf("i/o timeout") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "连接超时，该节点可能下线，请重新配置！\n";
                    }
                    if (!FRPCMD.HasExited)
                    {
                        Task.Run(() => StopProcess(FRPCMD));
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
                        if (!FRPCMD.HasExited)
                        {
                            Task.Run(() => StopProcess(FRPCMD));
                        }
                        frpcOutlog.Text = frpcOutlog.Text + "重新连接服务器...\n";
                        Thread.Sleep(200);
                        string frpcserver = GetFrpcIP().Substring(0, GetFrpcIP().IndexOf(".")) + "*";
                        int frpcserver2 = GetFrpcIP().IndexOf(".") + 1;
                        WebClient MyWebClient = new WebClient();
                        MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                        byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                        string _string = Encoding.UTF8.GetString(pageData);
                        int IndexofA = _string.IndexOf(frpcserver);
                        string Ru = _string.Substring(IndexofA + frpcserver2);
                        string a111 = Ru.Substring(0, Ru.IndexOf("*"));
                        byte[] pageData2 = new WebClient().DownloadData(a111);
                        string pageHtml = Encoding.UTF8.GetString(pageData2);
                        string aaa = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc");
                        int IndexofA2 = aaa.IndexOf("token = ");
                        string Ru2 = aaa.Substring(IndexofA2);
                        string a112 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                        aaa = aaa.Replace(a112, "token = " + pageHtml);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc", aaa);
                        Task.Run(() => StartFrpc());
                    }
                    catch
                    {
                        Growl.Error("内网映射桥接失败！");
                    }
                }
            }
            if (msg.IndexOf("reconnect") + 1 != 0 && msg.IndexOf("error") + 1 != 0 && msg.IndexOf("token") + 1 != 0)
            {
                try
                {
                    if (!FRPCMD.HasExited)
                    {
                        Task.Run(() => StopProcess(FRPCMD));
                    }
                    frpcOutlog.Text = frpcOutlog.Text + "重新连接服务器...\n";
                    Thread.Sleep(200);
                    string frpcserver = GetFrpcIP().Substring(0, GetFrpcIP().IndexOf(".")) + "*";
                    int frpcserver2 = GetFrpcIP().IndexOf(".") + 1;
                    WebClient MyWebClient = new WebClient();
                    MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/CC/frpcserver.txt");
                    string _string = Encoding.UTF8.GetString(pageData);
                    int IndexofA = _string.IndexOf(frpcserver);
                    string Ru = _string.Substring(IndexofA + frpcserver2);
                    string a111 = Ru.Substring(0, Ru.IndexOf("*"));
                    byte[] pageData2 = new WebClient().DownloadData(a111);
                    string pageHtml = Encoding.UTF8.GetString(pageData2);
                    string aaa = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc");
                    int IndexofA2 = aaa.IndexOf("token = ");
                    string Ru2 = aaa.Substring(IndexofA2);
                    string a112 = Ru2.Substring(0, Ru2.IndexOf("\n"));
                    aaa = aaa.Replace(a112, "token = " + pageHtml);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc", aaa);
                    Task.Run(() => StartFrpc());
                }
                catch
                {
                    Growl.Error("内网映射桥接失败！");
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接成功！您可复制IP进入游戏了！\n";
                    Growl.Success("内网映射桥接成功！");
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.IndexOf("port already used") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "本地端口被占用，请不要频繁开关内网映射并等待一分钟再试。\n若一分钟后仍然占用，请尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    else if (msg.IndexOf("port not allowed") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "远程端口被占用，请尝试重新配置一下再试！\n";
                    }
                    else if (msg.IndexOf("proxy name") + 1 != 0 && msg.IndexOf("already in use") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "隧道名称已被占用！请打开任务管理器检查后台是否存在frpc进程并手动结束！\n若仍然占用，请尝试重启电脑再试。\n";
                    }
                    else if (msg.IndexOf("proxy") + 1 != 0 && msg.IndexOf("already exists") + 1 != 0)
                    {
                        frpcOutlog.Text = frpcOutlog.Text + "隧道已被占用！请不要频繁开关内网映射并等待一分钟再试。\n若一分钟后仍然占用，请尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    if (!FRPCMD.HasExited)
                    {
                        Task.Run(() => StopProcess(FRPCMD));
                    }
                }
            }
            frpcOutlog.ScrollToEnd();
        }

        private void BuyPaidServe()
        {
            string userAccount = "";
            string userPassword = "";

            string _text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc");
            string pattern = @"user\s*=\s*(\w+)\s*meta_token\s*=\s*(\w+)";
            Match match = Regex.Match(_text, pattern);

            if (match.Success)
            {
                userAccount = match.Groups[1].Value;
                userPassword = match.Groups[2].Value;
            }
            bool dialog = false;
            Dispatcher.Invoke(() =>
            {
                dialog = DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "映射启动失败！可能是您的QQ或密码填写错误或付费资格已过期，请重新配置或进行续费！\n点击确定以进行付费节点续费步骤。", "提示", true, "取消");
            });
            if (!dialog)
            {
                return;
            }

            Process.Start("https://afdian.net/a/makabaka123");
            string text = "";
            bool input = false;
            Dispatcher.Invoke(() => { input = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "在爱发电购买完毕后，在此输入QQ号，以获取您的密码：", out text, userAccount); });
            if (input)
            {
                Dialog _dialog = null;
                try
                {
                    Dispatcher.Invoke(() => { _dialog = Dialog.Show(new TextDialog("获取密码中，请稍等……")); });

                    JObject patientinfo = new JObject
                    {
                        ["qq"] = text
                    };
                    string sendData = JsonConvert.SerializeObject(patientinfo);
                    string ret = Functions.Post("getpassword", 0, sendData, "http://http://111.180.189.249:6000");
                    Dispatcher.Invoke(() =>
                    {
                        this.Focus();
                        _dialog.Close();
                    });
                    if (ret != "Err")
                    {
                        _text = _text.Replace("meta_token = " + userPassword, "meta_token = " + ret);
                        using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write))
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.Write(_text);
                        }
                        Dispatcher.Invoke(() => { DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "您的付费密码为：" + ret + " 旧密码已自动替换为新密码，您现在可以启动映射了！", "获取成功！"); });
                        return;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => { DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "您的密码可能长时间无人获取，已经超时！请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！"); });
                    }
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.Focus();
                        _dialog.Close();
                        DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送赞助图片来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                    });
                }
                bool _input = false;
                string password = "";
                Dispatcher.Invoke(() => { _input = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "在此输入您手动获取到的密码", out password); });
                if (_input)
                {
                    _text = _text.Replace("meta_token = " + userPassword, "meta_token = " + password);
                    using (FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\frpc", FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(_text);
                    }
                    Growl.Success("您的密码已更新！");
                }
            }
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
            string url = "https://api.waheal.top";
            if (MainWindow.serverLink != "https://msl.waheal.top")
            {
                url = MainWindow.serverLink + ":5000";
            }
            WebClient MyWebClient = new WebClient();
            byte[] pageData = MyWebClient.DownloadData(url + "/otherdownloads");
            string _javaList = Encoding.UTF8.GetString(pageData);

            JObject javaList0 = JObject.Parse(_javaList);
            _dnfrpc = javaList0["frpc"].ToString();
        }
        private async void startfrpc_Click(object sender, RoutedEventArgs e)
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
                        jobject.Add("frpcversion", "6");
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
                    else if (jobject2["frpcversion"].ToString() != "6")
                    {
                        RefreshLink();
                        var mwindow = (MainWindow)Window.GetWindow(this);
                        DialogShow.ShowDownload(mwindow, _dnfrpc, AppDomain.CurrentDomain.BaseDirectory + "MSL", "frpc.exe", "更新内网映射中...");
                        _dnfrpc = "";
                        JObject jobject3 = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", Encoding.UTF8));
                        jobject3["frpcversion"] = "6";
                        string convertString2 = Convert.ToString(jobject3);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", convertString2, Encoding.UTF8);
                    }
                    //内网映射检测
                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\frpc.exe"))
                    {
                        RefreshLink();
                        var mwindow = (MainWindow)Window.GetWindow(this);
                        DialogShow.ShowDownload(mwindow, _dnfrpc, AppDomain.CurrentDomain.BaseDirectory + "MSL", "frpc.exe", "下载内网映射中...");
                        _dnfrpc = "";
                    }
                    Growl.Info("正在启动内网映射！");
                    _ = Task.Run(() => StartFrpc());
                    setfrpc.IsEnabled = false;
                    frpcOutlog.Text = "启动中，请稍候……\n";
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("出现错误，请检查是否有杀毒软件误杀并重试:" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    startfrpc.IsEnabled = false;
                    Growl.Info("正在关闭内网映射！");
                    await Task.Run(() => StopProcess(FRPCMD));
                    Growl.Success("内网映射已关闭！");
                    startfrpc.IsEnabled = true;
                }
                catch
                {
                    Growl.Error("关闭失败！请尝试手动结束frpc进程！");
                }
            }
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }

        public static async Task StopProcess(Process process)
        {
            if (AttachConsole((uint)process.Id))
            {
                // NOTE: each of these functions could fail. Error-handling omitted
                // for clarity. A real-world program should check the result of each
                // call and handle errors appropriately.
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, 0);
                await Task.Run(() => ProcessExited());
                SetConsoleCtrlHandler(null, false);
                FreeConsole();
            }
            else
            {
                int hresult = Marshal.GetLastWin32Error();
                Exception e = Marshal.GetExceptionForHR(hresult);

                throw new InvalidOperationException(
                    $"ERROR: failed to attach console to process {process.Id}: {e?.Message ?? hresult.ToString()}");
            }
        }

        static void ProcessExited()
        {
            while (!FRPCMD.HasExited)
            {
                Thread.Sleep(1000);
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
                Dispatcher.Invoke(() =>
                {
                    copyFrpc.IsEnabled = true;
                    startfrpc.IsEnabled = true;
                    frplab1.Text = "检测节点信息中……";
                });

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

                Dispatcher.Invoke(() =>
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
                });
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(serverAddr, 2000); // 替换成您要 ping 的 IP 地址
                if (reply.Status == IPStatus.Success)
                {
                    // 节点在线，可以获取延迟等信息
                    int roundTripTime = (int)reply.RoundtripTime;
                    Dispatcher.Invoke(() =>
                    {
                        frplab1.Text = nodeName + "  延迟：" + roundTripTime + "ms";
                    });
                }
                else
                {
                    // 节点离线
                    Dispatcher.Invoke(() =>
                    {
                        frplab1.Text = nodeName + "  节点离线，请重新配置！";
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    frplab1.Text = "获取节点信息失败，建议重新配置！";
                });
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
                    Thread thread = new Thread(GetFrpcInfo);
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
