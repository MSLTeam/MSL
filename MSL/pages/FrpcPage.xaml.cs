using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using IniParser;
using IniParser.Model;
using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
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
        //private delegate void DelReadStdOutput(string result);
        public static Process FRPCMD = new Process();
        //private event DelReadStdOutput ReadStdOutput;
        //private string _dnfrpc;

        public FrpcPage()
        {
            InitializeComponent();
            FRPCMD.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            //ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
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
                if (jobject["frpcServer"] == null)
                {
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                //默认的玩意
                string frpcServer = jobject["frpcServer"].ToString();
                string frpcversion = jobject["frpcversion"]?.ToString();
                string frpcExeName = "frpc.exe"; //frpc客户端主程序
                string downloadUrl = "/download/frpc/MSLFrp/amd64"; //frpc客户端在api的调用位置
                string arguments = "-c frpc"; //启动命令
                if (frpcServer == "1")//openfrp
                {
                    frpcExeName = "frpc_of.exe";
                    downloadUrl = "download/frpc/OpenFrp/amd64";
                    arguments = File.ReadAllText("MSL\\frpc");
                }
                else if (frpcServer == "2")//chmlfrp
                {
                    frpcExeName = "frpc_chml.exe";
                    downloadUrl = "download/frpc/ChmlFrp/amd64";
                }else if (frpcServer == "-1")//自定义frp，使用官版
                {
                    frpcExeName = "frpc_official.exe";
                    downloadUrl = "download/frpc/Official/amd64";
                }
                else if (frpcServer == "-2")//自定义frp，使用自己的
                {
                    frpcExeName = "frpc_custom.exe";
                }
                if (frpcversion == null || frpcversion != "6")//mslfrp的特别更新qwq
                {
                    
                    string _dnfrpc = Functions.Get(downloadUrl);
                    await Dispatcher.Invoke(async () =>
                    {
                        await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", $"{frpcExeName}", "更新内网映射中...");
                    });
                    Config.Write("frpcversion", "6");
                }
                if (!File.Exists($"MSL\\{frpcExeName}") && frpcServer != "-2")//检查frpc是否存在，不存在就下崽崽
                {
                    string _dnfrpc = Functions.Get(downloadUrl);
                    await Dispatcher.Invoke(async () =>
                    {
                        if (frpcServer == "0" || frpcServer == "-1")//下载exe or zip
                        {
                            await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", $"{frpcExeName}", "下载内网映射中...");
                        }
                        else
                        {
                            await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", $"{frpcExeName}.zip", "下载内网映射中...");
                        }

                    });
                    //只有mslfrp+gh不需要
                    if (frpcServer != "0" && frpcServer != "-1")
                    {
                        //很寻常的解压
                        string fileName = "";
                        using (ZipFile zip = new ZipFile($@"MSL\{frpcExeName}.zip"))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                fileName = entry.Name.Replace("/", "");
                                break;
                            }
                        }
                        FastZip fastZip = new FastZip();
                        fastZip.ExtractZip($@"MSL\{frpcExeName}.zip", "MSL", "");
                        File.Delete($@"MSL\{frpcExeName}.zip");
                        if (frpcServer == "1") //这是of的解压处理
                        {
                            File.Move("MSL\\" + fileName, $"MSL\\{frpcExeName}");
                            File.Delete("MSL\\" + fileName);
                        }
                        else //这是chml的解压处理
                        {
                            File.Move("MSL\\" + fileName + $"\\frpc.exe", $"MSL\\{frpcExeName}");
                            Directory.Delete("MSL\\" + fileName, true);
                        }
                        //三个服务 三个下载解压方式 我真是太开心了！(p≧w≦q)
                    }

                }else if(!File.Exists($"MSL\\{frpcExeName}") && frpcServer == "-2") {
                    //找不到自定义的frp，直接失败
                    throw new FileNotFoundException("Frpc Not Found");
                }
                //该启动了！
                FRPCMD.StartInfo.WorkingDirectory = "MSL";
                FRPCMD.StartInfo.FileName = $"MSL\\{frpcExeName}";
                FRPCMD.StartInfo.Arguments = arguments;
                FRPCMD.StartInfo.CreateNoWindow = true;
                FRPCMD.StartInfo.UseShellExecute = false;
                FRPCMD.StartInfo.RedirectStandardInput = true;
                FRPCMD.StartInfo.RedirectStandardOutput = true;
                FRPCMD.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                FRPCMD.Start();
                FRPCMD.BeginOutputReadLine();
                Dispatcher.Invoke(() =>
                {
                    startfrpc.IsEnabled = true;
                });
                FRPCMD.WaitForExit();
                FRPCMD.CancelOutputRead();
                //到这里就关掉了
                Dispatcher.Invoke(() =>
                {
                    Growl.Success("内网映射已关闭！");
                    startfrpc.IsEnabled = true;
                });
            }
            catch (Exception e)//错误处理
            {
                if (e.Message.Contains("Frpc Not Found") )
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
            //这里控制日志输出
            JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            if (jobject["frpcServer"].ToString() == "2")//给chmlfrp做token打码
            {
                try //写个try 预防旧版不支持
                {
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(@"MSL\frpc");
                    if (data["common"]["user"] != "")
                    {
                        frpcOutlog.Text = frpcOutlog.Text + msg.Replace(data["common"]["user"], "***usertoken***") + "\n";
                    }
                    else
                    {
                        frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
                    }
                }
                catch (Exception)
                {
                    frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
                }

            }
            else
            {
                frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
            }

            if (msg.IndexOf("login") + 1 != 0)
            {
                if (msg.IndexOf("failed") + 1 != 0)
                {
                    frpcOutlog.Text += "内网映射桥接失败！\n";
                    Growl.Error("内网映射桥接失败！");
                    if (msg.Contains("付费资格已过期"))
                    {
                        Thread thread = new Thread(PaidServe);
                        thread.Start();
                    }
                    else if (msg.IndexOf("i/o timeout") + 1 != 0)
                    {
                        frpcOutlog.Text += "连接超时，该节点可能下线，请重新配置！\n";
                    }
                    if (!FRPCMD.HasExited)
                    {
                        Task.Run(() => StopProcess(FRPCMD));
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
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接成功！您可复制IP进入游戏了！\n";
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
                    if (!FRPCMD.HasExited)
                    {
                        Task.Run(() => StopProcess(FRPCMD));
                    }
                }
            }
            if (msg.Contains(" 发现新版本"))
            {
                //JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
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
                                    await Task.Run(() => StopProcess(FRPCMD));
                                    await Task.Delay(1000);
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

        private async void PaidServe()
        {
            string userAccount = "";
            string userPassword = "";

            string _text = File.ReadAllText(@"MSL\frpc");
            string pattern = @"user\s*=\s*(\w+)\s*meta_token\s*=\s*(\w+)";
            Match match = Regex.Match(_text, pattern);

            if (match.Success)
            {
                userAccount = match.Groups[1].Value;
                userPassword = match.Groups[2].Value;
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
                var ret = Functions.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), "http://111.180.189.249:7004");
                Dispatcher.Invoke(() =>
                {
                    Window.GetWindow(this).Focus();
                    _dialog.Close();
                });
                JObject keyValues = JObject.Parse(ret);
                if (keyValues != null && int.Parse(keyValues["status"].ToString()) == 0)
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
                if (File.Exists(@"MSL\frpc"))
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
                    Task.Run(() => StopProcess(FRPCMD));
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
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jobject["frpcServer"] == null)
                {
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                if (jobject["frpcServer"].ToString() == "0" || jobject["frpcServer"].ToString() == "2" || jobject["frpcServer"].ToString() == "-2" || jobject["frpcServer"].ToString() == "-1")
                {
                    Dispatcher.Invoke(() =>
                    {
                        copyFrpc.IsEnabled = true;
                        startfrpc.IsEnabled = true;
                        frplab1.Text = "检测节点信息中……";
                    });
                    string configText = File.ReadAllText(@"MSL\frpc");
                    // 读取每一行
                    string[] lines = configText.Split('\n');

                    // 节点名称
                    string nodeName;
                    if (jobject["frpcServer"].ToString() == "0")
                    {
                        nodeName = lines[0].TrimStart('#').Trim();
                    }
                    else if (jobject["frpcServer"].ToString() == "-1" || jobject["frpcServer"].ToString() == "-2")
                    {
                        nodeName = "自定义节点";
                    }
                    else
                    {
                        nodeName = "ChmlFrp节点";
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
                        else if (lines[i].StartsWith("[") && readServerInfo && jobject["frpcServer"].ToString() == "2")//针对chmlfrp的节点名字读取
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
                        startfrpc.IsEnabled = true;
                        frplab1.Text = "OpenFrp的节点";
                        frplab3.Text = "请启动内网映射以查看IP";
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(@"MSL\frpc"))
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
