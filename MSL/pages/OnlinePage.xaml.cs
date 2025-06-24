using HandyControl.Controls;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// OnlinePage.xaml 的交互逻辑
    /// </summary>
    public partial class OnlinePage : Page
    {
        public static Process FrpcProcess = new Process();
        private bool isMaster;
        private string ipAddress = "";
        private string ipPort = "";

        public OnlinePage()
        {
            InitializeComponent();
            FrpcProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("联机页面已加载，开始检查本地P2P配置。");
            if (File.Exists("MSL\\frp\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\frp\\P2Pfrpc");
                if (string.IsNullOrEmpty(a))
                {
                    LogHelper.Write.Info("P2P配置文件存在但为空，将展开房主设置。");
                    masterExp.IsExpanded = true;
                    return;
                }
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    LogHelper.Write.Info("检测到P2P配置文件为访客(visitor)角色。");
                    visiterExp.IsExpanded = true;
                }
                else
                {
                    LogHelper.Write.Info("检测到P2P配置文件为房主(master)角色。");
                    masterExp.IsExpanded = true;
                }
            }
            else
            {
                //MagicShow.ShowMsgDialog(Window.GetWindow(this),"注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！\r\n该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！", "警告");
                LogHelper.Write.Info("未找到P2P配置文件，判定为首次使用，弹出提示。");
                if (await MagicShow.ShowMsgDialogAsync(LanguageManager.Instance["Page_OnlinePage_Announce"], LanguageManager.Instance["Warning"], true, "确定", "不再提示"))
                {
                    Directory.CreateDirectory("MSL\\frp");
                    File.WriteAllText("MSL\\frp\\P2Pfrpc", string.Empty);
                    LogHelper.Write.Info("用户确认提示，已创建空的P2P配置文件。");
                }
                masterExp.IsExpanded = true;
            }
            await GetFrpcInfo();
        }

        private async Task GetFrpcInfo()
        {
            try
            {
                LogHelper.Write.Info("开始从API获取FRP服务器信息。");
                string mslFrpInfo = (await HttpService.GetApiContentAsync("query/frp/MSLFrps"))["data"].ToString();
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

                        ipAddress = serverAddress;
                        ipPort = serverPort;
                        LogHelper.Write.Info($"成功解析到FRP服务器地址: {ipAddress}:{ipPort}");
                        break;
                    }
                    break;
                }
                await Task.Run(() =>
                {
                    LogHelper.Write.Info($"正在 Ping 服务器: {ipAddress}");
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(ipAddress, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        LogHelper.Write.Info($"Ping 服务器 {ipAddress} 成功，延迟: {reply.RoundtripTime}ms。");
                        Dispatcher.Invoke(() =>
                        {
                            //服务器活着，太好了！
                            serverState.Text = LanguageManager.Instance["Page_OnlinePage_ServerStatusOK"];
                        });
                    }
                    else
                    {
                        LogHelper.Write.Error($"Ping 服务器 {ipAddress} 失败，状态: {reply.Status}。");
                        Dispatcher.Invoke(() =>
                        {
                            //跑路了.jpg
                            serverState.Text = LanguageManager.Instance["Page_OnlinePage_ServerStatusDown"];
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取FRP服务器信息时发生错误: {ex.ToString()}");
                serverState.Text = LanguageManager.Instance["Page_OnlinePage_ServerStatusDown"];
            }
        }

        private void masterExp_Expanded(object sender, RoutedEventArgs e)
        {
            visiterExp.IsExpanded = false;
            if (File.Exists("MSL\\frp\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\frp\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 == 0)
                {
                    string pattern = @"\[(\w+)\]\s*type\s*=\s*xtcp\s*local_ip\s*=\s*(\S+)\s*local_port\s*=\s*(\d+)\s*sk\s*=\s*(\S+)";
                    Match match = Regex.Match(a, pattern);
                    if (match.Success)
                    {
                        masterQQ.Text = match.Groups[1].Value;
                        masterKey.Text = match.Groups[4].Value;
                        masterPort.Text = match.Groups[3].Value;
                    }
                }
            }
        }
        private void visiterExp_Expanded(object sender, RoutedEventArgs e)
        {
            masterExp.IsExpanded = false;
            if (File.Exists("MSL\\frp\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\frp\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    string pattern = @"server_name\s*=\s*(\S+)\s*sk\s*=\s*(\S+)\s*bind_port\s*=\s*(\d+)";
                    Match match = Regex.Match(a, pattern);
                    if (match.Success)
                    {
                        visiterQQ.Text = match.Groups[1].Value;
                        visiterKey.Text = match.Groups[2].Value;
                        visiterPort.Text = match.Groups[3].Value;
                    }
                }
            }
        }

        private async void createRoom_Click(object sender, RoutedEventArgs e)
        {
            if (createRoom.IsChecked == true)
            {
                LogHelper.Write.Info("用户点击“创建房间”，准备作为房主启动。");
                string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[" + masterQQ.Text + "]\r\ntype = xtcp\r\nlocal_ip = 127.0.0.1\r\nlocal_port = " + masterPort.Text + "\r\nsk = " + masterKey.Text + "\r\n";
                Directory.CreateDirectory("MSL\\frp");
                File.WriteAllText("MSL\\frp\\P2Pfrpc", a);
                LogHelper.Write.Info("已生成并写入房主P2P配置文件。");
                isMaster = true;
                visiterExp.IsEnabled = false;
                await StartFrpc();
            }
            else
            {
                if (!FrpcProcess.HasExited)
                {
                    LogHelper.Write.Warn("用户取消“创建房间”，准备终止frpc进程。");
                    createRoom.IsChecked = true;
                    FrpcProcess.Kill();
                }
            }
        }

        private async void joinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (joinRoom.IsChecked == true)
            {
                LogHelper.Write.Info("用户点击“加入房间”，准备作为访客启动。");
                string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[p2p_ssh_visitor]\r\ntype = xtcp\r\nrole = visitor\r\nbind_addr = 127.0.0.1\r\nbind_port = " + visiterPort.Text + "\r\nserver_name = " + visiterQQ.Text + "\r\nsk = " + visiterKey.Text + "\r\n";
                Directory.CreateDirectory("MSL\\frp");
                File.WriteAllText("MSL\\frp\\P2Pfrpc", a);
                LogHelper.Write.Info("已生成并写入访客P2P配置文件。");
                isMaster = false;
                masterExp.IsEnabled = false;
                await StartFrpc();
            }
            else
            {
                if (!FrpcProcess.HasExited)
                {
                    LogHelper.Write.Warn("用户取消“加入房间”，准备终止frpc进程。");
                    joinRoom.IsChecked = true;
                    FrpcProcess.Kill();
                }
            }
        }

        private async Task StartFrpc()
        {
            try
            {
                //内网映射版本检测
                try
                {
                    Directory.CreateDirectory("MSL\\frp");
                    if (!File.Exists("MSL\\frp\\frpc.exe"))
                    {
                        LogHelper.Write.Warn("frpc.exe 文件不存在，开始下载。");
                        string _dnfrpc, os = "10";
                        if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
                        {
                            os = "6";
                        }

                        _dnfrpc = (await HttpService.GetApiContentAsync("/download/frpc/MSLFrp/amd64?os=" + os))["data"]["url"].ToString();
                        LogHelper.Write.Info($"获取到frpc下载地址: {_dnfrpc}");
                        await MagicShow.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL\\frp", "frpc.exe", LanguageManager.Instance["Download_Frpc_Info"]);
                        LogHelper.Write.Info("frpc.exe 下载完成。");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"下载 frpc.exe 过程中发生错误: {ex.ToString()}");
                    return;
                }
                frpcOutlog.Text = string.Empty;
                FrpcProcess.StartInfo.WorkingDirectory = "MSL\\frp";
                FrpcProcess.StartInfo.FileName = "MSL\\frp\\" + "frpc.exe";
                FrpcProcess.StartInfo.Arguments = "-c P2Pfrpc";
                FrpcProcess.StartInfo.CreateNoWindow = true;
                FrpcProcess.StartInfo.UseShellExecute = false;
                FrpcProcess.StartInfo.RedirectStandardInput = true;
                FrpcProcess.StartInfo.RedirectStandardOutput = true;
                LogHelper.Write.Info($"准备启动frpc进程，参数: {FrpcProcess.StartInfo.Arguments}");
                FrpcProcess.Start();
                FrpcProcess.BeginOutputReadLine();
                LogHelper.Write.Info($"frpc进程已启动，进程ID: {FrpcProcess.Id}");
                await Task.Run(FrpcProcess.WaitForExit);
                FrpcProcess.CancelOutputRead();
                LogHelper.Write.Info($"frpc进程(ID: {FrpcProcess.Id})已退出。");
                if (isMaster)
                {
                    createRoom.IsChecked = false;
                    visiterExp.IsEnabled = true;
                }
                else
                {
                    joinRoom.IsChecked = false;
                    masterExp.IsEnabled = true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Write.Fatal($"启动或运行frpc进程时发生致命错误: {e.ToString()}");
                MessageBox.Show(LanguageManager.Instance["Page_OnlinePage_ErrMsg1"] + e.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() =>
                {
                    ReadStdOutputAction(e.Data);
                });
            }
        }

        private void ReadStdOutputAction(string msg)
        {
            // 此处原始日志会显示在UI上，可以只记录关键解析事件，避免日志重复
            if (msg.Contains("\x1B"))
            {
                string[] splitMsg = msg.Split(new[] { '\x1B' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var everyMsg in splitMsg)
                {
                    if (everyMsg == string.Empty)
                    {
                        continue;
                    }

                    // 提取ANSI码和文本内容
                    int mIndex = everyMsg.IndexOf('m');
                    if (mIndex == -1)
                    {
                        continue;
                    }

                    msg = everyMsg.Substring(mIndex + 1);
                }
            }
            frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
            if (msg.IndexOf("login") + 1 != 0)
            {
                if (msg.IndexOf("failed") + 1 != 0)
                {
                    LogHelper.Write.Warn($"[FRPC] 登录服务器失败: {msg}");
                    Growl.Error(LanguageManager.Instance["Page_OnlinePage_Err"]);
                    if (!FrpcProcess.HasExited)
                    {
                        FrpcProcess.Kill();
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    LogHelper.Write.Info($"[FRPC] 登录服务器成功: {msg}");
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_LoginSuc"] + "\n";
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    LogHelper.Write.Info($"[FRPC] 代理启动成功: {msg}");
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_Suc"] + "\n";
                    Growl.Success(LanguageManager.Instance["Page_OnlinePage_Suc"]);
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    LogHelper.Write.Warn($"[FRPC] 代理启动失败: {msg}");
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_Err"] + "\n";
                    Growl.Error(LanguageManager.Instance["Page_OnlinePage_Err"]);
                    FrpcProcess.Kill();
                }
            }
            frpcOutlog.ScrollToEnd();
        }
    }
}