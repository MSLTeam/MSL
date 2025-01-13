using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using MSL.controls;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
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
                    copyFrpc.IsEnabled = false;
                    startfrpc.IsEnabled = false;
                    frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status"];
                    frplab3.Text = LanguageManager.Instance["Page_FrpcPage_IPNull"];
                }
            }
            catch
            {
                MessageBox.Show("出现错误，请重试");
            }
        }

        private void AutoStartFrpc()
        {
            Dispatcher.InvokeAsync(async () =>
            {
                await StartFrpc();
            });
        }

        private async Task StartFrpc() //以下代码由神兽保佑
        {
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                //ui提示
                Growl.Info("正在启动内网映射！");
                startfrpc.IsEnabled = false;
                frpcOutlog.Text = "启动中，请稍候……\n";
                //读取配置
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                //默认的玩意
                string frpcServer = jobject[frpID.ToString()]["frpcServer"].ToString();
                string frpcversion = Config.Read("frpcversion")?.ToString() ?? "";
                string frpcExeName; //frpc客户端主程序
                string downloadUrl = ""; //frpc客户端在api的调用位置
                string arguments; //启动命令
                string downloadFileName;
                string osver = "10";
                if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
                {
                    osver = "6"; //OSVersion.Version win11获取的是6.2 win7是6.1
                }
                switch (frpcServer)
                {
                    case "0":
                        frpcExeName = "frpc.exe"; //frpc客户端主程序
                        arguments = "-c frpc.toml"; //启动命令
                        downloadFileName = "frpc.exe";
                        if (File.Exists($"MSL\\frp\\{frpcExeName}") && frpcversion != "0581") //mslfrp的特别更新qwq
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/MSLFrp/amd64?os=" + osver))["data"]["url"].ToString();//丢os版本号
                            await MagicShow.ShowDownloader(Window.GetWindow(this), downloadUrl, "MSL\\frp", downloadFileName, LanguageManager.Instance["Update_Frpc_Info"]);
                            Config.Write("frpcversion", "0581");
                            downloadUrl = "";
                        }
                        else if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/MSLFrp/amd64?os=" + osver))["data"]["url"].ToString();//丢os版本号
                        }
                        break;
                    case "1"://openfrp
                        frpcExeName = "frpc_of.exe";
                        arguments = File.ReadAllText($"MSL\\frp\\{frpID}\\frpc");
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "OpenFrp";
                        }
                        downloadFileName = "frpc_of.zip";
                        break;
                    case "2"://chmlfrp
                        frpcExeName = "frpc_chml.exe";
                        arguments = "-c frpc"; //启动命令
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "ChmlFrp";
                        }
                        downloadFileName = "frpc_chml.zip";
                        break;
                    case "3"://sakura
                        frpcExeName = "frpc_sakura.exe";
                        arguments = File.ReadAllText($"MSL\\frp\\{frpID}\\frpc"); //启动命令
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = "SakuraFrp";
                        }
                        downloadFileName = "frpc_sakura.exe";
                        break;
                    case "-1"://自定义frp，使用官版
                        frpcExeName = "frpc_official.exe";
                        arguments = "-c frpc.toml"; //启动命令
                        downloadFileName = "frpc_official.exe";
                        if (!File.Exists($"MSL\\frp\\{frpcExeName}"))
                        {
                            downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/Official/amd64?os=" + osver))["data"]["url"].ToString();
                        }
                        break;
                    case "-2"://自定义frp，使用自己的
                        frpcExeName = "frpc_custom.exe";
                        arguments = "-c frpc.toml"; //启动命令
                        downloadFileName = "";
                        break;
                    default:
                        frpcExeName = "frpc.exe"; //frpc客户端主程序
                        downloadUrl = (await HttpService.GetApiContentAsync("download/frpc/Official/amd64?os=" + osver))["data"]["url"].ToString();
                        arguments = "-c frpc.toml"; //启动命令
                        downloadFileName = "frpc.exe";
                        break;
                }

                if (frpcServer != "-2")//检查frpc是否存在，同时-2是用户自己设置frpc客户端，不用管
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
                else if (frpcServer == "-2" && !File.Exists($"MSL\\frp\\{frpcExeName}"))
                {
                    //找不到自定义的frp，直接失败
                    throw new FileNotFoundException("Frpc Not Found");
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
                //到这里就关掉了
                Growl.Success("内网映射已关闭！");
                startfrpc.IsEnabled = true;
            }
            catch (Exception e)//错误处理
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
            if(msg.Contains("No connection could be made because the target machine actively refused it."))
            {
                frpcOutlog.Text += "无法连接到本地服务器，请检查服务器是否开启，或内网映射本地端口和服务器本地端口是否相匹配！\n";
            }
            if (msg.Contains(" 发现新版本"))
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                if (jobject[frpID.ToString()]["frpcServer"].ToString() == "1")
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
            bool _ret = false;
            await Dispatcher.Invoke(async () =>
            {
                if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您的付费资格已过期，请进行续费！\n点击确定开始付费节点续费操作。", "提示", true, "取消"))
                {
                    _ret = true;
                }
            });
            if (_ret)
            {
                return;
            }

            Process.Start("https://afdian.com/a/makabaka123");
            await Dispatcher.Invoke(async () =>
            {
                if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "请在弹出的浏览器网站中进行购买，购买完毕后点击确定进行下一步操作……", "购买须知", true, "取消购买", "确定"))
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
                order = await MagicShow.ShowInput(Window.GetWindow(this), "输入爱发电订单号：\n（头像→订单→找到发电项目→复制项目下方订单号）");
            });
            if (order == null)
            {
                return;
            }
            if (Regex.IsMatch(order, "[^0-9]") || order.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请输入合法订单号：仅含数字且长度不小于5位！", "获取失败！");
                });
                return;
            }

            await Dispatcher.Invoke(async () =>
            {
                qq = await MagicShow.ShowInput(Window.GetWindow(this), "输入账号(QQ号)：");
            });

            if (qq == null)
            {
                return;
            }
            if (Regex.IsMatch(qq, "[^0-9]") || qq.Length < 5)
            {
                Dispatcher.Invoke(() =>
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请输入合法账号：仅含数字且长度不小于5位！", "获取失败！");
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
                var ret = HttpService.Post("getpassword", 0, JsonConvert.SerializeObject(keyValuePairs), (await HttpService.GetApiContentAsync("query/frp/MSLFrps?query=orderapi"))["data"]["url"].ToString());
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
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "您的密码为：" + keyValues["password"].ToString() + "\n注册时间：" + keyValues["registration"].ToString() + "\n本次续费：" + keyValues["days"].ToString() + "天\n到期时间：" + keyValues["expiration"].ToString(), "续费成功！");
                    });
                }
                else if (keyValues != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), keyValues["reason"].ToString(), "获取失败！");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "返回内容为空！", "获取失败！");
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Window.GetWindow(this).Focus();
                    _dialog.Close();
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取失败，请添加QQ：483232994（昵称：MSL-FRP），并发送发电成功截图+订单号来手动获取密码\r\n（注：回复消息不一定及时，请耐心等待！如果没有添加成功，或者添加后长时间无人回复，请进入MSL交流群然后从群里私聊）", "获取失败！");
                });
            }
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
                    await Functions.StopProcess(FrpcProcess); // 尝试使用CTRL+C
                }
                catch
                {
                    FrpcProcess.Kill(); // CTRL+C失败后直接Kill
                }
            }
        }

        private void copyFrpc_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(frplab3.Text.ToString());
        }

        private async Task GetFrpcInfo()
        {
            try
            {
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                if (jobject[frpID.ToString()]["frpcServer"] == null)
                {
                    jobject[frpID.ToString()]["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                }
                copyFrpc.IsEnabled = true;
                startfrpc.IsEnabled = true;
                frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_Checking"];
                if (jobject[frpID.ToString()]["frpcServer"].ToString() == "0" || jobject[frpID.ToString()]["frpcServer"].ToString() == "2" || jobject[frpID.ToString()]["frpcServer"].ToString() == "-2" || jobject[frpID.ToString()]["frpcServer"].ToString() == "-1")
                {

                    string configText;
                    if (jobject[frpID.ToString()]["frpcServer"].ToString() == "2")
                    {
                        configText = File.ReadAllText(@$"MSL\frp\{frpID}\frpc");
                    }
                    else
                    {
                        configText = File.ReadAllText(@$"MSL\frp\{frpID}\frpc.toml");
                    }
                    // 读取每一行
                    string[] lines = configText.Split('\n');

                    // 节点名称
                    string nodeName;
                    if (jobject[frpID.ToString()]["frpcServer"].ToString() == "0")
                    {
                        nodeName = lines[0].TrimStart('#').Trim();
                    }
                    else if (jobject[frpID.ToString()]["frpcServer"].ToString() == "2")
                    {
                        nodeName = LanguageManager.Instance["Page_FrpcPage_Status_ChmlFrp"];
                    }

                    else
                    {
                        nodeName = LanguageManager.Instance["Page_FrpcPage_Status_CustomFrp"];
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
                        else if (jobject[frpID.ToString()]["frpcServer"].ToString() == "2" && lines[i].StartsWith("[") && readServerInfo)//针对chmlfrp的节点名字读取
                        {
                            nodeName = LanguageManager.Instance["Page_FrpcPage_Status_ChmlFrp"] + "-" + lines[i].Replace("[", "").Replace("]", "").Replace("\r", "").ToString();
                        }
                        else if (jobject[frpID.ToString()]["frpcServer"].ToString() == "3")
                        {
                            nodeName = "SakuraFrp节点";
                        }
                    }

                    if (!readServerInfo)
                    {
                        frplab3.Text = $"{LanguageManager.Instance["Page_FrpcPage_Status_JavaVersion"]}{serverAddr}:{remotePort}" +
                            $"\n{LanguageManager.Instance["Page_FrpcPage_Status_BedrockVersion"]}" +
                            $"{LanguageManager.Instance["Page_FrpcPage_Status_IP"]}{serverAddr} {LanguageManager.Instance["Page_FrpcPage_Status_Port"]}{remotePort}";
                    }
                    else
                    {
                        if (frpcType == "udp")
                        {
                            frplab3.Text = $"{LanguageManager.Instance["Page_FrpcPage_Status_IP"]}{serverAddr} {LanguageManager.Instance["Page_FrpcPage_Status_Port"]}{remotePort}";
                        }
                        else
                        {
                            frplab3.Text = serverAddr + ":" + remotePort;
                        }
                    }
                    await Task.Run(() =>
                    {
                        Ping pingSender = new Ping();
                        PingReply reply = pingSender.Send(serverAddr, 2000);
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
                else if (jobject[frpID.ToString()]["frpcServer"].ToString() == "3")
                {
                    copyFrpc.IsEnabled = false;
                    frplab1.Text = "SakuraFrp节点";
                    frplab3.Text = LanguageManager.Instance["Page_FrpcPage_Status_OpenFrp_ViewIP"];
                }
                else
                {
                    copyFrpc.IsEnabled = false;
                    frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_OpenFrp"];
                    frplab3.Text = LanguageManager.Instance["Page_FrpcPage_Status_OpenFrp_ViewIP"];
                }
            }
            catch
            {
                copyFrpc.IsEnabled = false;
                frplab1.Text = LanguageManager.Instance["Page_FrpcPage_Status_Failed"];
                frplab3.Text = LanguageManager.Instance["None"];
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            GotoFrpcListPage();
        }
    }
}
