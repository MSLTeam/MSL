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
            if (File.Exists("MSL\\frp\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\frp\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    visiterExp.IsExpanded = true;
                }
                else
                {
                    masterExp.IsExpanded = true;
                }
            }
            else
            {
                //MagicShow.ShowMsgDialog(Window.GetWindow(this),"注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！\r\n该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！", "警告");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), LanguageManager.Instance["Page_OnlinePage_Announce"], LanguageManager.Instance["Warning"]);
                masterExp.IsExpanded = true;
            }
            await GetFrpcInfo();
        }

        private async Task GetFrpcInfo()
        {
            try
            {
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
                        break;
                    }
                    break;
                }
                await Task.Run(() =>
                {
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(ipAddress, 2000);
                    if (reply.Status == IPStatus.Success)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            //服务器活着，太好了！
                            serverState.Text = LanguageManager.Instance["Page_OnlinePage_ServerStatusOK"];
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            //跑路了.jpg
                            serverState.Text = LanguageManager.Instance["Page_OnlinePage_ServerStatusDown"];
                        });
                    }
                });
            }
            catch
            {
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
                string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[" + masterQQ.Text + "]\r\ntype = xtcp\r\nlocal_ip = 127.0.0.1\r\nlocal_port = " + masterPort.Text + "\r\nsk = " + masterKey.Text + "\r\n";
                Directory.CreateDirectory("MSL\\frp");
                File.WriteAllText("MSL\\frp\\P2Pfrpc", a);
                isMaster = true;
                visiterExp.IsEnabled = false;
                await StartFrpc();
            }
            else
            {
                if (!FrpcProcess.HasExited)
                {
                    createRoom.IsChecked = true;
                    FrpcProcess.Kill();
                }
            }
        }

        private async void joinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (joinRoom.IsChecked == true)
            {
                string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[p2p_ssh_visitor]\r\ntype = xtcp\r\nrole = visitor\r\nbind_addr = 127.0.0.1\r\nbind_port = " + visiterPort.Text + "\r\nserver_name = " + visiterQQ.Text + "\r\nsk = " + visiterKey.Text + "\r\n";
                Directory.CreateDirectory("MSL\\frp");
                File.WriteAllText("MSL\\frp\\P2Pfrpc", a);
                isMaster = false;
                masterExp.IsEnabled = false;
                await StartFrpc();
            }
            else
            {
                if (!FrpcProcess.HasExited)
                {
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
                        string _dnfrpc, os = "10";
                        if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
                        {
                            os = "6";
                        }

                        _dnfrpc = (await HttpService.GetApiContentAsync("/download/frpc/MSLFrp/amd64?os=" + os))["data"]["url"].ToString();
                        await MagicShow.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL\\frp", "frpc.exe", LanguageManager.Instance["Download_Frpc_Info"]);
                    }
                }
                catch
                {
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
                FrpcProcess.Start();
                FrpcProcess.BeginOutputReadLine();
                await Task.Run(FrpcProcess.WaitForExit);
                FrpcProcess.CancelOutputRead();
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
                    Growl.Error(LanguageManager.Instance["Page_OnlinePage_Err"]);
                    if (!FrpcProcess.HasExited)
                    {
                        FrpcProcess.Kill();
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_LoginSuc"] + "\n";
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_Suc"] + "\n";
                    Growl.Success(LanguageManager.Instance["Page_OnlinePage_Suc"]);
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Page_OnlinePage_Err"] + "\n";
                    Growl.Error(LanguageManager.Instance["Page_OnlinePage_Err"]);
                    FrpcProcess.Kill();
                }
            }
            frpcOutlog.ScrollToEnd();
        }
    }
}
