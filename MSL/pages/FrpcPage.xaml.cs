﻿using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        public static event DeleControl GotoFrpcListPage;
        public readonly Process FrpcProcess = new Process();
        private readonly int frpID;

        public FrpcPage(int frpId, bool autoStart = false)
        {
            InitializeComponent();
            frpID = frpId;
            FrpcProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            if (autoStart)
            {
                AutoStartFrpc();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(@$"MSL\frp\{frpID}\frpc") || File.Exists(@$"MSL\frp\{frpID}\frpc.toml"))
                {
                    await GetFrpcInfo();
                }
                else
                {
                    startfrpc.IsEnabled = false;
                    frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_Failed"];
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试");
            }
        }

        private async Task GetFrpcInfo()
        {
            try
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                if (jobject[frpID.ToString()]["frpcServer"] == null) // 如果frpcServer为null就给他设置为0！
                {
                    jobject[frpID.ToString()]["frpcServer"] = 0;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                }
                //copyFrpc.IsEnabled = true;
                startfrpc.IsEnabled = true;
                frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_Checking"];
                // 如果frpcServer为0（MSL）、2（CHML）、-2（Custom，自己提供frpc）、-1（Custom，官版frpc）时，执行下面函数
                FrpcServer = (int)jobject[frpID.ToString()]["frpcServer"];
                if (FrpcServer == 0 || FrpcServer == 2 || FrpcServer == -2 || FrpcServer == -1)
                {
                    await LoadFrpcInfo(jobject);
                }
                else if (FrpcServer == 1) // 否则，为1时，则为OF节点
                {
                    frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_OpenFrp"];
                }
                else if (FrpcServer == 3) // 否则，为3时，则为SF节点
                {
                    frplab1.Text = "SakuraFrp节点";
                }
            }
            catch
            {
                frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_Failed"];
            }
        }

        private int FrpcServer;
        private string RemoteAddr;
        private string RemoteDomain;
        // 辅助类定义
        private class ProxyConfig
        {
            public string Type { get; set; }
            public string RemotePort { get; set; }
        }

        private async Task LoadFrpcInfo(JObject jobject)
        {
            // 节点名称
            string nodeName = LanguageManager.Instance["Page_FrpcPage_Status_CustomFrp"];
            // 节点配置
            string configText;
            if (FrpcServer == 2) // Load CHML-config
            {
                nodeName = LanguageManager.Instance["Page_FrpcPage_Status_ChmlFrp"];
                configText = File.ReadAllText(@$"MSL\frp\{frpID}\frpc");
            }
            else // others are toml format
            {
                configText = File.ReadAllText(@$"MSL\frp\{frpID}\frpc.toml");
            }

            if (configText.Contains("\r")) // 替换掉\r
            {
                configText = configText.Replace("\r", string.Empty);
            }
            string[] lines = configText.Split('\n'); // 每一行分割开

            if (FrpcServer == 0)
            {
                nodeName = jobject[frpID.ToString()]["name"].ToString();
            }

            // 清空现有内容  
            ProxiesContainer.Children.Clear();

            List<ProxyConfig> proxies = new List<ProxyConfig>();
            ProxyConfig currentProxy = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if ((line.StartsWith("serverAddr") || line.StartsWith("server_addr")))
                {
                    RemoteAddr = lines[i].Split('=')[1].Trim().Trim('"');
                }
                if ((line.StartsWith("metadatas.mslFrpRemoteDomain")))
                {
                    RemoteDomain = lines[i].Split('=')[1].Trim().Trim('"');
                }
                // TOML格式处理
                if (line.StartsWith("[[proxies]]"))
                {
                    if (currentProxy != null) proxies.Add(currentProxy);
                    currentProxy = new ProxyConfig();
                }
                // INI格式处理
                else if (line.StartsWith("[") && !line.StartsWith("[common]"))
                {
                    if (currentProxy != null) proxies.Add(currentProxy);
                    currentProxy = new ProxyConfig();
                }
                else if (currentProxy != null)
                {
                    // 通用属性解析
                    if (line.StartsWith("remotePort") || line.StartsWith("remote_port"))
                    {
                        currentProxy.RemotePort = line.Split('=')[1].Trim().Trim('"');
                    }
                    else if (line.StartsWith("type"))
                    {
                        currentProxy.Type = line.Split('=')[1].Trim().Trim('"');
                    }
                }
            }

            // 处理最后一个代理项
            if (currentProxy != null) proxies.Add(currentProxy);

            // 创建UI元素
            foreach (var proxy in proxies)
            {
                CreateProxyItem(proxy.RemotePort, proxy.Type);
            }
            await Task.Run(() =>
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(RemoteAddr, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 节点在线，可以获取延迟等信息
                        int roundTripTime = (int)reply.RoundtripTime;
                        Dispatcher.Invoke(() =>
                        {
                            frplab1.Text = $"{nodeName} {LanguageManager.Instance["Page_FrpcPage_Status_Ping"]}{roundTripTime}ms";
                        });
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 节点离线
                        frplab1.Text = nodeName + "  " + LanguageManager.Instance["Page_FrpcPage_Status_Offline"];
                    });
                }
            });
        }

        private void CreateProxyItem(string remotePort, string proxyType)
        {
            void AddProxyEntry(string labelText, string address)
            {
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var ipTextBlock = new TextBlock
                {
                    Text = labelText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Foreground = (Brush)FindResource("PrimaryTextBrush")
                };

                var addressTextBlock = new TextBlock
                {
                    Text = address,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    Margin = new Thickness(3, 0, 25, 0)
                };

                var copyButton = new Button
                {
                    Content = LanguageManager.Instance["Page_FrpcPage_Copy"],
                    Width = 80,
                    FontWeight = FontWeights.Normal,
                };

                copyButton.Click += (sender, e) =>
                {
                    try
                    {
                        Clipboard.SetText(addressTextBlock.Text);
                        MagicFlowMsg.ShowMessage("复制成功！", 1);
                    }
                    catch
                    {
                        MagicFlowMsg.ShowMessage("复制失败！", 2);
                    }
                };

                stackPanel.Children.Add(ipTextBlock);
                stackPanel.Children.Add(addressTextBlock);
                stackPanel.Children.Add(copyButton);

                ProxiesContainer.Children.Add(stackPanel);
            }

            if (!string.IsNullOrEmpty(RemoteDomain))
            {
                // 添加第一个地址
                AddProxyEntry(LanguageManager.Instance["Page_FrpcPage_Domain"], $"{RemoteDomain}:{remotePort}");
                // 添加备用地址
                AddProxyEntry(LanguageManager.Instance["Page_FrpcPage_IP2"], $"{RemoteAddr}:{remotePort}");
            }
            else
            {
                AddProxyEntry(LanguageManager.Instance["Page_FrpcPage_IP"], $"{RemoteAddr}:{remotePort}");
            }
        }


        private void AutoStartFrpc()
        {
            Dispatcher.InvokeAsync(async () =>
            {
                await StartFrpc();
            });
        }

        private async Task StartFrpc()
        {
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                MagicFlowMsg.ShowMessage("正在启动内网映射！", 4);
                //Growl.Info("正在启动内网映射！");
                startfrpc.IsEnabled = false;
                frpcOutlog.Text = "启动中，请稍候……\n";
                // 读取配置
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                // 默认的玩意
                int frpcServer = (int)jobject[frpID.ToString()]["frpcServer"];
                string frpcversion = Config.Read("frpcversion")?.ToString() ?? "";
                string frpcExeName; // frpc客户端主程序
                string downloadUrl = ""; // frpc客户端在api的调用位置
                string arguments; // 启动命令
                string downloadFileName;
                string osver = "10";
                if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
                {
                    osver = "6"; // OSVersion.Version win11获取的是6.2 win7是6.1
                }
                switch (frpcServer)
                {
                    case 0:
                        frpcExeName = "frpc.exe"; // frpc客户端主程序
                        arguments = "-c frpc.toml"; // 启动命令
                        downloadFileName = "frpc.exe";
                        if (File.Exists($"MSL\\frp\\{frpcExeName}") && frpcversion != "0611") //mslfrp的特别更新qwq
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/MSLFrp/amd64?os=" + osver))["data"]["url"].ToString();//丢os版本号
                            await MagicShow.ShowDownloader(Window.GetWindow(this), downloadUrl, "MSL\\frp", downloadFileName, LanguageManager.Instance["Update_Frpc_Info"]);
                            Config.Write("frpcversion", "0611");
                            downloadUrl = "";
                        }
                        else if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/MSLFrp/amd64?os=" + osver))["data"]["url"].ToString();//丢os版本号
                        }
                        break;
                    case 1: // openfrp
                        frpcExeName = "frpc_of.exe";
                        arguments = File.ReadAllText($"MSL\\frp\\{frpID}\\frpc");
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "OpenFrp";
                        }
                        downloadFileName = "frpc_of.zip";
                        break;
                    case 2: // chmlfrp
                        frpcExeName = "frpc_chml.exe";
                        arguments = "-c frpc"; // 启动命令
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "ChmlFrp";
                        }
                        downloadFileName = "frpc_chml.zip";
                        break;
                    case 3: // sakura
                        frpcExeName = "frpc_sakura.exe";
                        arguments = File.ReadAllText($"MSL\\frp\\{frpID}\\frpc"); // 启动命令
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "SakuraFrp";
                        }
                        downloadFileName = "frpc_sakura.exe";
                        break;
                    /*
                    case 5: // msl new
                        frpcExeName = "frpc.exe";
                        arguments = "-c frpc.toml"; // 启动命令
                        downloadFileName = "frpc.exe";
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/MSLFrp/amd64?os=" + osver))["data"]["url"].ToString();
                        }
                        break;
                    */
                    case -1: // 自定义frp，使用官版
                        frpcExeName = "frpc_official.exe";
                        arguments = "-c frpc.toml"; //启动命令
                        downloadFileName = "frpc_official.exe";
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/Official/amd64?os=" + osver))["data"]["url"].ToString();
                        }
                        break;
                    case -2: // 自定义frp，使用自己的
                        frpcExeName = "frpc_custom.exe";
                        arguments = "-c frpc.toml"; // 启动命令
                        downloadFileName = "";
                        break;
                    default:
                        frpcExeName = "frpc.exe"; // frpc客户端主程序
                        downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/Official/amd64?os=" + osver))["data"]["url"].ToString();
                        arguments = "-c frpc.toml"; // 启动命令
                        downloadFileName = "frpc.exe";
                        break;
                }

                if (frpcServer != -2) // 检查frpc是否存在，同时-2是用户自己设置frpc客户端，不用管
                {
                    if (downloadUrl == "OpenFrp")
                    {
                        //List<string> downloadSource = new();
                        JObject apiData = (JObject)JObject.Parse((await HttpService.GetContentAsync("https://api.openfrp.net/commonQuery/get?key=software")).ToString())["data"];
                        string latestVer = apiData["latest"].ToString();
                        JArray downSourceList = (JArray)apiData["source"];
                        if (osver == "6")
                        {
                            latestVer = "/OpenFRP_0.54.0_835276e2_20240205/";
                        }
                        foreach (JObject downSource in downSourceList)
                        {
                            //downloadSource.Add(downSource["value"].ToString());
                            int _return = await MagicShow.ShowDownloaderWithIntReturn(Window.GetWindow(this), downSource["value"].ToString() + latestVer + "frpc_windows_amd64.zip", "MSL\\frp", downloadFileName, LanguageManager.Instance["Download_Frpc_Info"], "", true);
                            if (_return == 2)
                            {
                                return;
                            }
                            else if (_return == 1)
                            {
                                break;
                            }
                        }
                    }
                    else if (downloadUrl == "SakuraFrp")
                    {
                        JObject apiData = JObject.Parse((await HttpService.GetContentAsync("https://api.natfrp.com/v4/system/clients")).ToString());
                        await MagicShow.ShowDownloader(Window.GetWindow(this), (string)apiData["frpc"]["archs"]["windows_amd64"]["url"], "MSL\\frp", downloadFileName, LanguageManager.Instance["Download_Frpc_Info"]);
                    }
                    else if (downloadUrl == "ChmlFrp")
                    {
                        JObject apiData = (JObject)JObject.Parse((await HttpService.GetContentAsync("https://cf-v1.uapis.cn/api/dw.php")).ToString());
                        if ((int)apiData["code"] != 200)
                        {
                            Growl.Error("获取ChmlFrp下载地址失败！");
                            return;
                        }
                        string link = apiData["link"].ToString();
                        JArray fileList = (JArray)apiData["system"]["windows"];
                        foreach (JObject file in fileList)
                        {
                            if (file["architecture"].ToString() == "amd64")
                            {
                                await MagicShow.ShowDownloader(Window.GetWindow(this), link + file["route"].ToString(), "MSL\\frp", downloadFileName, LanguageManager.Instance["Download_Frpc_Info"]);
                                break;
                            }
                        }
                    }
                    else if (downloadUrl != "")
                    {
                        await MagicShow.ShowDownloader(Window.GetWindow(this), downloadUrl, "MSL\\frp", downloadFileName, LanguageManager.Instance["Download_Frpc_Info"]);
                    }

                    //只有官方版本+sakura不需要
                    if (downloadUrl == "OpenFrp" || downloadUrl == "ChmlFrp")
                    {
                        //很寻常的解压
                        string fileName = "";
                        using (ZipFile zip = new ZipFile($@"MSL\frp\{downloadFileName}"))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                fileName = entry.Name.Replace("/", "");
                                break;
                            }
                        }
                        FastZip fastZip = new FastZip();
                        fastZip.ExtractZip($@"MSL\frp\{downloadFileName}", "MSL\\frp", "");
                        File.Delete($@"MSL\frp\{downloadFileName}");
                        if (frpcServer == 1) //这是of的解压处理
                        {
                            File.Move("MSL\\frp\\" + fileName, $"MSL\\frp\\{frpcExeName}");
                            File.Delete("MSL\\frp\\" + fileName);
                        }
                        else //这是chml的解压处理
                        {
                            File.Move("MSL\\frp\\" + fileName + $"\\frpc.exe", $"MSL\\frp\\{frpcExeName}");
                            Directory.Delete("MSL\\frp\\" + fileName, true);
                        }
                        //三个服务 三个下载解压方式 我真是太开心了！(p≧w≦q)
                    }

                }
                else if (frpcServer == -2 && !File.Exists($"MSL\\frp\\{frpcExeName}"))
                {
                    //找不到自定义的frp，直接失败
                    throw new FileNotFoundException("Frpc Not Found");
                }

                if (frpcServer == 0)
                {
                    tempStr = jobject[frpID.ToString()]["name"].ToString();
                }
                //该启动了！
                FrpcProcess.StartInfo.WorkingDirectory = $"MSL\\frp\\{frpID}";
                FrpcProcess.StartInfo.FileName = "MSL\\frp\\" + frpcExeName;
                FrpcProcess.StartInfo.Arguments = arguments;
                FrpcProcess.StartInfo.CreateNoWindow = true;
                FrpcProcess.StartInfo.UseShellExecute = false;
                FrpcProcess.StartInfo.RedirectStandardInput = true;
                FrpcProcess.StartInfo.RedirectStandardOutput = true;
                FrpcProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                FrpcProcess.Start();
                FrpcProcess.BeginOutputReadLine();
                FrpcList.RunningFrpc.Add(frpID);
                startfrpc.IsEnabled = true;
                await Task.Run(FrpcProcess.WaitForExit);
                FrpcProcess.CancelOutputRead();
                FrpcList.RunningFrpc.Remove(frpID);
                // 到这里就关掉了
                MagicFlowMsg.ShowMessage("内网映射已关闭！", 4);
                //Growl.Info("内网映射已关闭！");
                startfrpc.IsEnabled = true;
                tempStr = string.Empty;
            }
            catch (Exception e) // 错误处理
            {
                if (e.Message.Contains("Frpc Not Found"))
                {
                    startfrpc.IsEnabled = true;
                    MagicShow.ShowMsg(Window.GetWindow(this), "找不到自定义的Frpc客户端，请重新配置！\n" + e.Message, "错误");
                }
                else
                {
                    startfrpc.IsEnabled = true;
                    MagicShow.ShowMsg(Window.GetWindow(this), "出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误");
                }
            }
            finally
            {
                startfrpc.IsEnabled = true;
                startfrpc.IsChecked = false;
            }
        }

        private string tempStr; // 临时字符串，记录MSL-Frp(NEW)的隧道名字
        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string msg = e.Data;
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
                if (!string.IsNullOrEmpty(tempStr))
                {
                    msg = Regex.Replace(msg, @"\[([a-zA-Z0-9]+)-(\d+\..+?)\]", "[$2]");
                }
                Dispatcher.Invoke(() =>
                {
                    ReadStdOutputAction(msg);
                });
                msg = null;
            }
        }

        private void ReadStdOutputAction(string msg)
        {
            frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
            if (msg.Contains("login"))
            {
                if (msg.Contains("failed"))
                {
                    frpcOutlog.Text += "内网映射桥接失败！\n";
                    MagicFlowMsg.ShowMessage("内网映射桥接失败！", 2);
                    //Growl.Error("内网映射桥接失败！");
                    if (msg.Contains("i/o timeout"))
                    {
                        frpcOutlog.Text += "连接超时，该节点可能下线，请重新配置！\n";
                    }
                    if (!FrpcProcess.HasExited)
                    {
                        FrpcProcess.Kill();
                    }
                }
                if (msg.Contains("success"))
                {
                    frpcOutlog.Text += "登录服务器成功！\n";
                }
            }
            if (msg.Contains("start"))
            {
                if (msg.Contains("success"))
                {
                    frpcOutlog.Text += "内网映射桥接成功！您可复制IP进入游戏了！\n";
                    MagicFlowMsg.ShowMessage("内网映射桥接成功！", 1);
                }
                if (msg.Contains("error"))
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    MagicFlowMsg.ShowMessage("内网映射桥接失败！", 2);
                    if (msg.Contains("port already used"))
                    {
                        frpcOutlog.Text += "本地端口被占用，请检查是否有程序占用或后台是否存在frpc进程，您可尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    else if (msg.Contains("port not allowed"))
                    {
                        frpcOutlog.Text += "远程端口被占用，请尝试重新配置一下再试！\n";
                    }
                    else if (msg.Contains("proxy name") && msg.Contains("already in use"))
                    {
                        frpcOutlog.Text += "隧道名称已被占用！请打开任务管理器检查后台是否存在frpc进程并手动结束！\n若仍然占用，请尝试重启电脑再试。\n";
                    }
                    else if (msg.Contains("proxy") && msg.Contains("already exists"))
                    {
                        frpcOutlog.Text += "隧道已被占用！请打开任务管理器检查后台是否存在frpc进程！您可尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    if (!FrpcProcess.HasExited)
                    {
                        FrpcProcess.Kill();
                    }
                }
            }
            if (msg.Contains("No connection could be made because the target machine actively refused it."))
            {
                frpcOutlog.Text += "无法连接到本地服务器，请检查服务器是否开启，或内网映射本地端口和服务器本地端口是否相匹配！\n";
            }
            if (msg.Contains("发现新版本"))
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                if ((int)jobject[frpID.ToString()]["frpcServer"] == 1) // OF frpc更新
                {
                    Growl.Ask(new GrowlInfo
                    {
                        Message = "发现OpenFrp桥接软件新版本，是否更新？",
                        ActionBeforeClose = isConfirmed =>
                        {
                            if (isConfirmed)
                            {
                                Dispatcher.InvokeAsync(async () =>
                                {
                                    if (!FrpcProcess.HasExited)
                                    {
                                        FrpcProcess.Kill();
                                    }
                                    await Task.Delay(500);
                                    await Task.Run(() => File.Delete("MSL\\frpc_of.exe"));
                                    _ = Task.Run(() => StartFrpc());

                                });

                            }
                            return true;
                        },
                        ShowDateTime = false
                    });
                }
            }
            frpcOutlog.ScrollToEnd();
        }

        private async void startfrpc_Click(object sender, RoutedEventArgs e)
        {
            if (startfrpc.IsChecked == true)
            {
                await StartFrpc();
            }
            else
            {
                startfrpc.IsEnabled = false;
                try
                {
                    await Functions.StopProcess(FrpcProcess); // 尝试使用CTRL+C（有风险不能随便用，调试的时候还会把程序给干掉（怪！））
                }
                catch
                {
                    FrpcProcess.Kill(); // CTRL+C失败后直接Kill
                }
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            GotoFrpcListPage();
        }
    }
}
