using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using MSL.controls;
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
        private delegate void DelReadStdOutput(string result);
        public static Process FRPCMD = new Process();
        private event DelReadStdOutput ReadStdOutput;
        private string _dnfrpc;
        public FrpcPage()
        {
            InitializeComponent();
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            FRPCMD.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            MainWindow.AutoOpenFrpc += AutoStartFrpc;
        }

        private async void StartFrpc()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    startfrpc.Content = "关闭内网映射";
                    Shows.GrowlInfo("正在启动内网映射！");
                    setfrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frpcOutlog.Text = "启动中，请稍候……\n";
                });
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jobject["frpcServer"] == null)
                {
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                if (jobject["frpcServer"].ToString() == "0")
                {
                    //内网映射版本检测
                    if (jobject["frpcversion"] == null)
                    {
                        jobject.Add("frpcversion", "6");
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                        if (!File.Exists("MSL\\frpc.exe"))
                        {
                            RefreshLink();
                            await Dispatcher.Invoke(async () =>
                            {
                                await Shows.ShowDownloader(_dnfrpc, "MSL", "frpc.exe", "下载内网映射中...");
                            });
                            _dnfrpc = "";
                        }
                    }
                    else if (jobject["frpcversion"].ToString() != "6")
                    {
                        RefreshLink();
                        await Dispatcher.Invoke(async () =>
                        {
                            await Shows.ShowDownloader(_dnfrpc, "MSL", "frpc.exe", "更新内网映射中...");
                        });
                        _dnfrpc = "";
                        jobject["frpcversion"] = "6";
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                    }
                    //内网映射检测
                    if (!File.Exists("MSL\\frpc.exe"))
                    {
                        RefreshLink();
                        await Dispatcher.Invoke(async () =>
                        {
                            await Shows.ShowDownloader(_dnfrpc, "MSL", "frpc.exe", "下载内网映射中...");
                        });
                        _dnfrpc = "";
                    }

                    FRPCMD.StartInfo.WorkingDirectory = "MSL";
                    FRPCMD.StartInfo.FileName = "MSL\\frpc.exe";
                    FRPCMD.StartInfo.Arguments = "-c frpc";
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
                }
                else if (jobject["frpcServer"].ToString() == "1")
                {
                    //内网映射检测
                    if (!File.Exists("MSL\\frpc_of.exe"))
                    {
                        string latest_url = Functions.Get("download/frpc/OpenFrp/amd64");
                        await Dispatcher.Invoke(async () =>
                        {
                            await Shows.ShowDownloader(latest_url, "MSL", "frpc_of.zip", "下载内网映射中...");
                            string fileName = "";
                            using (ZipFile zip = new ZipFile(@"MSL\frpc_of.zip"))
                            {
                                foreach (ZipEntry entry in zip)
                                {
                                    fileName = entry.Name.Replace("/", "");
                                    break;
                                }
                            }
                            FastZip fastZip = new FastZip();
                            fastZip.ExtractZip(@"MSL\frpc_of.zip", "MSL", "");
                            File.Delete(@"MSL\frpc_of.zip");
                            File.Move("MSL\\" + fileName, "MSL\\frpc_of.exe");
                            File.Delete("MSL\\" + fileName);

                        });
                    }
                    FRPCMD.StartInfo.WorkingDirectory = "MSL";
                    FRPCMD.StartInfo.FileName = "MSL\\frpc_of.exe";
                    FRPCMD.StartInfo.Arguments = File.ReadAllText("MSL\\frpc");
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
                }
                Dispatcher.Invoke(() =>
                {
                    Shows.GrowlSuccess("内网映射已关闭！");
                    startfrpc.IsEnabled = true;
                });
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() =>
                {
                    startfrpc.IsEnabled = true;
                    MessageBox.Show("出现错误，请检查是否有杀毒软件误杀并重试:" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    setfrpc.IsEnabled = true;
                    startfrpc.IsEnabled = true;
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
                    Shows.GrowlErr("内网映射桥接失败！");
                    if (msg.Contains("付费资格已过期"))
                    {
                        Thread thread = new Thread(PaidServe);
                        thread.Start();
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
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接成功！您可复制IP进入游戏了！\n";
                    Shows.GrowlSuccess("内网映射桥接成功！");
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + "内网映射桥接失败！\n";
                    Shows.GrowlErr("内网映射桥接失败！");
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
                if (!await Shows.ShowMsgDialogAsync("您的付费资格已过期，请进行续费！\n点击确定开始付费节点续费操作。", "提示", true, "取消"))
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
                if (!await Shows.ShowMsgDialogAsync("请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
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
                order = await Shows.ShowInput("输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）");
            });
            if (order == null)
            {
                return;
            }
            if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog("请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                });
                return;
            }
            await Dispatcher.Invoke(async () =>
            {
                qq = await Shows.ShowInput("输入账号(QQ号)：");
            });

            if (qq == null)
            {
                return;
            }
            if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog("请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
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
                    this.Focus();
                    _dialog.Close();
                });
                JObject keyValues = JObject.Parse(ret);
                if (keyValues != null && int.Parse(keyValues["status"].ToString()) == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog("您的付费密码为：" + keyValues["password"].ToString() + "\n注册时间：" + keyValues["registration"].ToString() + "\n本次续费：" + keyValues["days"].ToString() + "天\n到期时间：" + keyValues["expiration"].ToString(), "续费成功！");
                    });
                }
                else if (keyValues != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(keyValues["reason"].ToString(), "获取失败！");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog("返回内容为空！", "获取失败！");
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    this.Focus();
                    _dialog.Close();
                    Shows.ShowMsgDialog("获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送发电成功截图+订单号来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                });
            }
        }

        private void setfrpc_Click(object sender, RoutedEventArgs e)
        {
            SetFrpc fw = new SetFrpc();
            var mainwindow = Window.GetWindow(this);
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

        void RefreshLink()
        {
            /*
            string url = "https://api.waheal.top";
            if (MainWindow.serverLink != "waheal.top")
            {
                url = "https://api." + MainWindow.serverLink;
            }
            */
            //WebClient MyWebClient = new WebClient();
            //byte[] pageData = MyWebClient.DownloadData(url + "/otherdownloads");
            //string _javaList = Encoding.UTF8.GetString(pageData);

            //JObject javaList0 = JObject.Parse(_javaList);
            //_dnfrpc = javaList0["frpc"].ToString();
            _dnfrpc = Functions.Get("/download/frpc/MSLFrp/amd64");
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
                    Shows.GrowlInfo("正在关闭内网映射！");
                    Task.Run(() => StopProcess(FRPCMD));
                }
                catch
                {
                    Shows.GrowlErr("关闭失败！请尝试手动结束frpc进程！");
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
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jobject["frpcServer"] == null)
                {
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                if (jobject["frpcServer"].ToString() == "0")
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
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        startfrpc.IsEnabled = true;
                        frplab1.Text = "您正在使用OpenFrp的节点";
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
                    frplab1.Text = "未检测到内网映射配置，请点击 配置 按钮以配置";
                    frplab3.Text = "无";
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试");
            }
        }
    }
}
