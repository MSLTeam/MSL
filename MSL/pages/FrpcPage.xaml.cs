using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using MSL.controls;
using MSL.i18n;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
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
        public static Process FrpcProcess = new Process();

        public FrpcPage()
        {
            InitializeComponent();
            FrpcProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            MainWindow.AutoOpenFrpc += AutoStartFrpc;
        }

        /*
        *      
        *          ┌─┐       ┌─┐
        *       ┌──┘ ┴───────┘ ┴──┐
        *       │                 │
        *       │       ───       │
        *       │  ─┬┘       └┬─  │
        *       │                 │
        *       │       ─┴─       │
        *       │                 │
        *       └───┐         ┌───┘
        *           │         │
        *           │         │
        *           │         │
        *           │         └──────────────┐
        *           │                        │
        *           │                        ├─┐
        *           │                        ┌─┘    
        *           │                        │
        *           └─┐  ┐  ┌───────┬──┐  ┌──┘         
        *             │ ─┤ ─┤       │ ─┤ ─┤         
        *             └──┴──┘       └──┴──┘ 
        *                 重写了个shitmountain
        *                 神兽保佑 
        *                 代码无BUG! 
        */
        private async void StartFrpc() //以下代码由神兽保佑
        {
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                //ui提示
                Dispatcher.Invoke(() =>
                {
                    startfrpc.Content = LanguageManager.Instance["Pages_Frpc_Close"];
                    Growl.Info("正在启动内网映射！");
                    setfrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frpcOutlog.Text = "启动中，请稍候……\n";
                });
                //读取配置
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (Config.Read("frpcServer") == "")
                {
                    Config.Write("frpcServer", "0");
                }
                //默认的玩意
                string frpcServer = Config.Read("frpcServer");
                string frpcversion = Config.Read("frpcversion");
                string frpcExeName = "frpc.exe"; //frpc客户端主程序
                string downloadUrl = "download/frpc/MSLFrp/amd64"; //frpc客户端在api的调用位置
                string arguments = "-c frpc.toml"; //启动命令
                string osver = "10";
                if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
                {
                    osver = "6"; //OSVersion.Version win11获取的是6.2 win7是6.1
                }
                switch (frpcServer)
                {
                    case "1"://openfrp
                        frpcExeName = "frpc_of.exe";
                        downloadUrl = "download/frpc/OpenFrp/amd64";
                        arguments = File.ReadAllText("MSL\\frp\\frpc");
                        break;
                    case "2"://chmlfrp
                        frpcExeName = "frpc_chml.exe";
                        downloadUrl = "download/frpc/ChmlFrp/amd64";
                        arguments = "-c frpc"; //启动命令
                        break;
                    case "-1"://自定义frp，使用官版
                        frpcExeName = "frpc_official.exe";
                        downloadUrl = "download/frpc/Official/amd64";
                        break;
                    case "-2"://自定义frp，使用自己的
                        frpcExeName = "frpc_custom.exe";
                        break;
                }
                if ((frpcversion == "" || frpcversion != "0581") && frpcServer == "0") //mslfrp的特别更新qwq
                {
                    string _dnfrpc;
                    _dnfrpc = HttpService.Get(downloadUrl + "?os=" + osver);//丢os版本号

                    await Dispatcher.Invoke(async () =>
                    {
                        await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL\\frp", $"{frpcExeName}", "更新MSL内网映射中...");
                    });
                    Config.Write("frpcversion", "0581");
                }

                if (!File.Exists($"MSL\\frp\\{frpcExeName}") && frpcServer != "-2")//检查frpc是否存在，不存在就下崽崽
                {
                    string _dnfrpc;
                    _dnfrpc = HttpService.Get(downloadUrl + "?os=" + osver);//丢os版本号
                    await Dispatcher.Invoke(async () =>
                    {
                        if (frpcServer == "0" || frpcServer == "-1")//下载exe or zip
                        {
                            await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL\\frp", $"{frpcExeName}", "下载内网映射中...");
                        }
                        else
                        {
                            await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL\\frp", $"{frpcExeName}.zip", "下载内网映射中...");
                        }

                    });

                    //只有mslfrp+gh不需要
                    if (frpcServer != "0" && frpcServer != "-1")
                    {
                        //很寻常的解压
                        string fileName = "";
                        using (ZipFile zip = new ZipFile($@"MSL\frp\{frpcExeName}.zip"))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                fileName = entry.Name.Replace("/", "");
                                break;
                            }
                        }
                        FastZip fastZip = new FastZip();
                        fastZip.ExtractZip($@"MSL\frp\{frpcExeName}.zip", "MSL\\frp", "");
                        File.Delete($@"MSL\frp\{frpcExeName}.zip");
                        if (frpcServer == "1") //这是of的解压处理
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
                else if (!File.Exists($"MSL\\frp\\{frpcExeName}") && frpcServer == "-2")
                {
                    //找不到自定义的frp，直接失败
                    throw new FileNotFoundException("Frpc Not Found");
                }
                //该启动了！
                FrpcProcess.StartInfo.WorkingDirectory = "MSL\\frp";
                FrpcProcess.StartInfo.FileName = "MSL\\frp\\" + frpcExeName;
                FrpcProcess.StartInfo.Arguments = arguments;
                FrpcProcess.StartInfo.CreateNoWindow = true;
                FrpcProcess.StartInfo.UseShellExecute = false;
                FrpcProcess.StartInfo.RedirectStandardInput = true;
                FrpcProcess.StartInfo.RedirectStandardOutput = true;
                FrpcProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                FrpcProcess.Start();
                FrpcProcess.BeginOutputReadLine();
                Dispatcher.Invoke(() =>
                {
                    startfrpc.IsEnabled = true;
                });
                FrpcProcess.WaitForExit();
                FrpcProcess.CancelOutputRead();
                //到这里就关掉了
                Dispatcher.Invoke(() =>
                {
                    Growl.Success("内网映射已关闭！");
                    startfrpc.IsEnabled = true;
                });
            }
            catch (Exception e)//错误处理
            {
                if (e.Message.Contains("Frpc Not Found"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        startfrpc.IsEnabled = true;
                        Shows.ShowMsg(Window.GetWindow(this), "找不到自定义的Frpc客户端，请重新配置！\n" + e.Message, "错误");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        startfrpc.IsEnabled = true;
                        Shows.ShowMsg(Window.GetWindow(this), "出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误");
                    });
                }
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    setfrpc.IsEnabled = true;
                    startfrpc.IsEnabled = true;
                    startfrpc.Content = LanguageManager.Instance["Pages_Frpc_Launch"];
                });
            }
        }

        private void AutoStartFrpc()
        {
            Task.Run(() => StartFrpc());
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
                    frpcOutlog.Text += "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.Contains("付费资格已过期"))
                    {
                        Task.Run(PayService);
                    }
                    else if (msg.IndexOf("i/o timeout") + 1 != 0)
                    {
                        frpcOutlog.Text += "连接超时，该节点可能下线，请重新配置！\n";
                    }
                    if (!FrpcProcess.HasExited)
                    {
                        Task.Run(() => Functions.StopProcess(FrpcProcess));
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text += "登录服务器成功！\n";
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text += "内网映射桥接成功！您可复制IP进入游戏了！\n";
                    Growl.Success("内网映射桥接成功！");
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.IndexOf("port already used") + 1 != 0)
                    {
                        frpcOutlog.Text += "本地端口被占用，请不要频繁开关内网映射并等待一分钟再试。\n若一分钟后仍然占用，请尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    else if (msg.IndexOf("port not allowed") + 1 != 0)
                    {
                        frpcOutlog.Text += "远程端口被占用，请尝试重新配置一下再试！\n";
                    }
                    else if (msg.IndexOf("proxy name") + 1 != 0 && msg.IndexOf("already in use") + 1 != 0)
                    {
                        frpcOutlog.Text += "隧道名称已被占用！请打开任务管理器检查后台是否存在frpc进程并手动结束！\n若仍然占用，请尝试重启电脑再试。\n";
                    }
                    else if (msg.IndexOf("proxy") + 1 != 0 && msg.IndexOf("already exists") + 1 != 0)
                    {
                        frpcOutlog.Text += "隧道已被占用！请不要频繁开关内网映射并等待一分钟再试。\n若一分钟后仍然占用，请尝试手动结束frpc进程或重启电脑再试。\n";
                    }
                    if (!FrpcProcess.HasExited)
                    {
                        Task.Run(() => Functions.StopProcess(FrpcProcess));
                    }
                }
            }
            if (msg.Contains(" 发现新版本"))
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jobject["frpcServer"].ToString() == "1")
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
                                        await Task.Run(() => Functions.StopProcess(FrpcProcess));
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

        private async void PayService()
        {
            string userAccount = "";
            string userPassword = "";

            //保险起见，还是加个try吧（
            try
            {
                string _text = File.ReadAllText(@"MSL\frp\frpc.toml");
                string pattern = @"user\s*=\s*""(\w+)""\s*metadatas\.token\s*=\s*""(\w+)""";
                Match match = Regex.Match(_text, pattern);

                if (match.Success)
                {
                    userAccount = match.Groups[1].Value;
                    userPassword = match.Groups[2].Value;
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "您的付费资格已过期，但自动续费功能出现问题，请手动前往爱发电续费或重新配置节点再试！", "错误");
                });
                return;
            }

            bool _ret = false;
            await Dispatcher.Invoke(async () =>
            {
                if (!await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您的付费资格已过期，请进行续费！\n点击确定开始付费节点续费操作。", "提示", true, "取消"))
                {
                    _ret = true;
                }
            });
            if (_ret)
            {
                return;
            }

            Process.Start("https://afdian.net/a/makabaka123");
            await Dispatcher.Invoke(async () =>
            {
                if (!await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
                {
                    _ret = true;
                }
            });
            if (_ret)
            {
                return;
            }

            string order = null;
            string qq = null;
            await Dispatcher.Invoke(async () =>
            {
                order = await Shows.ShowInput(Window.GetWindow(this), "输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）");
            });
            if (order == null)
            {
                return;
            }
            if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                });
                return;
            }

            await Dispatcher.Invoke(async () =>
            {
                qq = await Shows.ShowInput(Window.GetWindow(this), "输入账号(QQ号)：");
            });

            if (qq == null)
            {
                return;
            }
            if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
                });
                return;
            }

            Dialog _dialog = null;
            try
            {
                Dispatcher.Invoke(() => { _dialog = Dialog.Show(new TextDialog("发送请求中，请稍等……")); });
                JObject keyValuePairs = new JObject()
                {
                    ["order"] = order,
                    ["qq"] = qq,
                };
                var ret = HttpService.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), HttpService.Get("query/MSLFrps/orderapi"));
                Dispatcher.Invoke(() =>
                {
                    Window.GetWindow(this).Focus();
                    _dialog.Close();
                });
                JObject keyValues = JObject.Parse(ret);
                if (keyValues != null && (int)keyValues["status"] == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "您的付费密码为：" + keyValues["password"].ToString() + "\n注册时间：" + keyValues["registration"].ToString() + "\n本次续费：" + keyValues["days"].ToString() + "天\n到期时间：" + keyValues["expiration"].ToString(), "续费成功！");
                    });
                }
                else if (keyValues != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), keyValues["reason"].ToString(), "获取失败！");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "返回内容为空！", "获取失败！");
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Window.GetWindow(this).Focus();
                    _dialog.Close();
                    Shows.ShowMsgDialog(Window.GetWindow(this), "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送发电成功截图+订单号来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                });
            }
        }

        private void setfrpc_Click(object sender, RoutedEventArgs e)
        {
            SetFrpc fw = new SetFrpc();
            var mainwindow = Window.GetWindow(Window.GetWindow(this));
            fw.Owner = mainwindow;
            fw.ShowDialog();
            try
            {
                if (File.Exists(@"MSL\frp\frpc") || File.Exists(@"MSL\frp\frpc.toml"))
                {
                    Thread thread = new Thread(GetFrpcInfo);
                    thread.Start();
                }
                else
                {
                    copyFrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frplab1.Text = LanguageManager.Instance["Pages_Frpc_Status"];
                    frplab3.Text = LanguageManager.Instance["Pages_Frpc_IPNull"];
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试:" + "m0x3");
            }
        }

        private void startfrpc_Click(object sender, RoutedEventArgs e)
        {
            if (startfrpc.Content.ToString() == "启动内网映射")
            {
                Task.Run(() => StartFrpc());
            }
            else
            {
                try
                {
                    startfrpc.IsEnabled = false;
                    Growl.Info("正在关闭内网映射！");
                    Task.Run(() => Functions.StopProcess(FrpcProcess));
                }
                catch
                {
                    Growl.Error("关闭失败！请尝试手动结束frpc进程！");
                }
            }
        }

        private void copyFrpc_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(frplab3.Text.ToString());
        }

        private void GetFrpcInfo()
        {
            try
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jobject["frpcServer"] == null)
                {
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                Dispatcher.Invoke(() =>
                {
                    copyFrpc.IsEnabled = true;
                    startfrpc.IsEnabled = true;
                    frplab1.Text = "检测节点信息中……";
                });
                if (jobject["frpcServer"].ToString() == "0" || jobject["frpcServer"].ToString() == "2" || jobject["frpcServer"].ToString() == "-2" || jobject["frpcServer"].ToString() == "-1")
                {

                    string configText;
                    if (jobject["frpcServer"].ToString() == "2")
                    {
                        configText = File.ReadAllText(@"MSL\frp\frpc");
                    }
                    else
                    {
                        configText = File.ReadAllText(@"MSL\frp\frpc.toml");
                    }
                    // 读取每一行
                    string[] lines = configText.Split('\n');

                    // 节点名称
                    string nodeName;
                    if (jobject["frpcServer"].ToString() == "0")
                    {
                        nodeName = lines[0].TrimStart('#').Trim();
                    }
                    else if (jobject["frpcServer"].ToString() == "2")
                    {
                        nodeName = "ChmlFrp节点";
                    }
                    else
                    {
                        nodeName = "自定义节点";
                    }

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
                        else if ((lines[i].StartsWith("serverAddr") || lines[i].StartsWith("server_addr")) && readServerInfo)
                        {
                            serverAddr = lines[i].Split('=')[1].Trim().Replace("\"", string.Empty);
                        }
                        else if ((lines[i].StartsWith("serverPort") || lines[i].StartsWith("server_port")) && readServerInfo)
                        {
                            serverPort = int.Parse(lines[i].Split('=')[1].Trim());
                        }
                        else if ((lines[i].StartsWith("remotePort") || lines[i].StartsWith("remote_port")) && readServerInfo)
                        {
                            remotePort = lines[i].Split('=')[1].Trim();
                        }
                        else if (jobject["frpcServer"].ToString() == "2" && lines[i].StartsWith("[") && readServerInfo)//针对chmlfrp的节点名字读取
                        {
                            nodeName = "ChmlFrp节点-" + lines[i].Replace("[", "").Replace("]", "").Replace("\r", "").ToString();
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
                            frplab1.Text = nodeName + "  节点离线或禁Ping，若无法连接，请重新配置！";
                        });
                    }
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        copyFrpc.IsEnabled = false;
                        frplab1.Text = "OpenFrp节点";
                        frplab3.Text = "请启动内网映射以查看IP";
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    copyFrpc.IsEnabled = false;
                    frplab1.Text = "获取Frp信息失败，建议重新配置！";
                    frplab3.Text = "无";
                });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(@"MSL\frp\frpc") || File.Exists(@"MSL\frp\frpc.toml"))
                {
                    Thread thread = new Thread(GetFrpcInfo);
                    thread.Start();
                }
                else
                {
                    copyFrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frplab1.Text = LanguageManager.Instance["Pages_Frpc_Status"];
                    frplab3.Text = LanguageManager.Instance["Pages_Frpc_IPNull"];
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试");
            }
        }
    }
}
