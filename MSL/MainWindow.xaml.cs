using HandyControl.Controls;
using HandyControl.Themes;
using MSL.langs;
using MSL.pages;
using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSL
{
    public delegate void DeleControl();
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly List<Page> Pages = new List<Page>
        {
            new Home(),
            new ServerList(),
            new FrpcList(),
            new OnlinePage(),
            new SettingsPage(),
            new About(),
            new CreateServer()
        };
        public static event DeleControl AutoOpenServer;
        public static bool getServerInfo = false;
        public static bool getPlayerInfo = false;
        public static bool LoadingCompleted = false;
        public static string ServerLink { get; set; }
        public static Version MSLVersion { get; set; }
        // public static bool IsOldVersion { get; set; } 旧版本会加一个叹号提示（现在暂没使用此功能），后续可能会用上此字段
        public static string DeviceID { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            Home.CreateServerEvent += GotoCreatePage;
            ServerList.CreateServerEvent += GotoCreatePage;
            FrpcList.OpenFrpcPage += OpenFrpcPage;
            FrpcPage.GotoFrpcListPage += GotoFrpcListPage;
            CreateServer.GotoServerList += GotoListPage;
            SettingsPage.C_NotifyIcon += CtrlNotifyIcon;
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            Focus();
            Topmost = false;
            MSLVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                DeviceID = Functions.GetDeviceID();
                if (jsonObject["eula"] == null || jsonObject["eula"].ToString() != DeviceID.Substring(0, 5))
                {
                    if (await EulaEvent())
                    {
                        if (jsonObject["eula"] == null)
                        {
                            jsonObject.Add("eula", DeviceID.Substring(0, 5));
                        }
                        else
                        {
                            jsonObject["eula"] = DeviceID.Substring(0, 5);
                        }
                        string convertString = Convert.ToString(jsonObject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                }

                bool downloadTermDll = false;
                if (!(Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1))
                {
                    if (File.Exists("MSL\\Microsoft.Terminal.Control.dll"))
                    {
                        try
                        {
                            LoadLibEx();
                        }
                        catch
                        {
                            File.Delete("MSL\\Microsoft.Terminal.Control.dll");
                            downloadTermDll = true;
                        }
                    }
                    else
                    {
                        downloadTermDll = true;
                    }
                }
                //Logger.LogInfo("载入配置……");
                await LoadConfigEvent(jsonObject);
                //Logger.LogInfo("异步载入联网功能……");
                await OnlineService(jsonObject, downloadTermDll);
                //Logger.LogInfo("启动事件完成！");
                LoadingCompleted = true;
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(this, ex.Message, "EEEOR");
            }
        }

        private async Task<bool> EulaEvent()
        {
            var shield = new Shield
            {
                Command = HandyControl.Interactivity.ControlCommands.OpenLink,
                CommandParameter = "https://www.mslmc.cn/eula.html",
                Subject = "https://www.mslmc.cn/eula.html",
                Status = LanguageManager.Instance["MainWindow_GrowlMsg_ReadEula"]
            };
            bool dialog = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["MainWindow_GrowlMsg_Eula"], LanguageManager.Instance["Tip"], true, LanguageManager.Instance["No"], LanguageManager.Instance["Yes"], shield);
            if (dialog)
            {
                //Logger.LogInfo("EULA=TRUE");
                return true;
            }
            else
            {
                //Logger.LogInfo("EULA=False");
                return false;
            }
        }

        private async Task LoadConfigEvent(JObject jsonObject)
        {
            //下面是加载配置部分
            try
            {
                if (jsonObject["notifyIcon"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("notifyIcon", false);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if ((bool)jsonObject["notifyIcon"] == true)
                {
                    await Task.Run(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            CtrlNotifyIcon();
                        });
                    });
                }
                //Logger.LogInfo("读取托盘图标配置成功！");
                if (jsonObject["sidemenuExpanded"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("sidemenuExpanded", true);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    SideMenu.Width = double.NaN;
                }
                else if ((bool)jsonObject["sidemenuExpanded"] == true)
                {
                    SideMenu.Width = double.NaN;
                }
                else
                {
                    SideMenu.Width = 50;
                }
                //Logger.LogInfo("读取侧栏配置成功！");
                if (jsonObject["skin"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("skin", 1);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    BrushConverter brushConverter = new BrushConverter();
                    ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                }
                else
                {
                    switch ((int)jsonObject["skin"])
                    {
                        case 0:
                            ThemeManager.Current.UsingSystemTheme = true;
                            break;
                        case 1:
                            BrushConverter brushConverter = new BrushConverter();
                            ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                            break;
                        case 2:
                            ThemeManager.Current.AccentColor = Brushes.Red;
                            break;
                        case 3:
                            ThemeManager.Current.AccentColor = Brushes.Green;
                            break;
                        case 4:
                            ThemeManager.Current.AccentColor = Brushes.Orange;
                            break;
                        case 5:
                            ThemeManager.Current.AccentColor = Brushes.Purple;
                            break;
                        case 6:
                            ThemeManager.Current.AccentColor = Brushes.DeepPink;
                            break;
                        default:
                            BrushConverter _brushConverter = new BrushConverter();
                            ThemeManager.Current.AccentColor = (Brush)_brushConverter.ConvertFromString("#0078D4");
                            break;
                    }
                }
                //Logger.LogInfo("读取皮肤配置成功！");
                if (jsonObject["darkTheme"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("darkTheme", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["darkTheme"].ToString() == "True")
                {
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                }
                //Logger.LogInfo("读取暗色模式配置成功！");
                if (File.Exists("MSL\\Background_.png"))
                {
                    File.Copy("MSL\\Background_.png", "MSL\\Background.png", true);
                    File.Delete("MSL\\Background_.png");
                }
                if (File.Exists("MSL\\Background.png"))
                {
                    Background = new ImageBrush(SettingsPage.GetImage("MSL\\Background.png"));
                    frame.BorderThickness = new Thickness(0);
                }
                //Logger.LogInfo("加载背景图片成功！");
                if (jsonObject["semitransparentTitle"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("semitransparentTitle", false);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else
                {
                    if ((bool)jsonObject["semitransparentTitle"] == true)
                    {
                        ChangeTitleStyle(true);
                    }
                    else
                    {
                        ChangeTitleStyle(false);
                    }
                }
                //Logger.LogInfo("读取标题栏样式成功！");
                if (jsonObject["autoGetServerInfo"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoGetServerInfo", true);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    getServerInfo = true;
                }
                else if ((bool)jsonObject["autoGetServerInfo"] == true)
                {
                    getServerInfo = true;
                }
                if (jsonObject["autoGetPlayerInfo"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoGetPlayerInfo", true);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    getPlayerInfo = true;
                }
                else if ((bool)jsonObject["autoGetPlayerInfo"] == true)
                {
                    getPlayerInfo = true;
                }
                //Logger.LogInfo("读取自动化功能配置成功（自动打开显示占用、记录玩家功能）！");
            }
            catch (Exception ex)
            {
                Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_ConfigErr"] + ex.Message);
                File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
            }

            //自动开启服务器
            try
            {
                if (jsonObject["autoOpenServer"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoOpenServer", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["autoOpenServer"].ToString() != "False")
                {
                    // 判断是否开了自动更新软件，如果开了自动更新软件，先不执行此操作
                    if (jsonObject["autoUpdateApp"] == null || jsonObject["autoUpdateApp"].ToString() != "True")
                    {
                        await AutoRunServer(jsonObject);
                    }
                }
                //Logger.LogInfo("读取自动开启（服务器）配置成功！");
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取自动开启（服务器）配置失败！");
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchServerErr"] + ex.Message, LanguageManager.Instance["Error"]);
            }
            //自动开启Frpc
            try
            {
                if (jsonObject["autoOpenFrpc"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoOpenFrpc", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["autoOpenFrpc"].ToString() != "False")
                {
                    // 判断是否开了自动更新软件，如果开了自动更新软件，先不执行此操作
                    if (jsonObject["autoUpdateApp"] == null || jsonObject["autoUpdateApp"].ToString() != "True")
                    {
                        await AutoRunFrpc(jsonObject);
                    }
                }
                //Logger.LogInfo("读取自动开启（内网映射）配置成功！");
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取自动开启（内网映射）配置失败！");
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchFrpcErr"] + ex.Message, LanguageManager.Instance["Error"]);
            }

            //Logger.LogInfo("所有配置载入完毕！调整UI界面……");
            SideMenu.SelectedIndex = 0;
            //Logger.LogInfo("配置加载完毕！");
        }

        private async Task AutoRunServer(JObject json)
        {
            string servers = json["autoOpenServer"].ToString();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchServer"]);
            if (!servers.Contains(","))
            {
                servers += ",";
            }
            while (servers != "")
            {
                int aserver = servers.IndexOf(",");
                ServerList.ServerID = int.Parse(servers.Substring(0, aserver));
                AutoOpenServer();
                servers = servers.Replace(ServerList.ServerID.ToString() + ",", "");
                await Task.Delay(50);
            }
        }

        private async Task AutoRunFrpc(JObject json)
        {
            string frpcs = json["autoOpenFrpc"].ToString();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchFrpc"]);
            if (!frpcs.Contains(","))
            {
                frpcs += ",";
            }
            while (frpcs != "")
            {
                int afrpc = frpcs.IndexOf(",");
                FrpcList.FrpcID = int.Parse(frpcs.Substring(0, afrpc));
                if (!FrpcList.FrpcPageList.ContainsKey(FrpcList.FrpcID))
                {
                    FrpcList.FrpcPageList.Add(FrpcList.FrpcID, new FrpcPage(FrpcList.FrpcID, true));
                }
                frpcs = frpcs.Replace(FrpcList.FrpcID.ToString() + ",", "");
                await Task.Delay(50);
            }
        }

        private async Task OnlineService(JObject jsonObject, bool downloadTermDll)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //get serverlink
            try
            {
                _ = HttpService.GetContentAsync("https://msl-api.oss-cn-hangzhou.aliyuncs.com/");
                ServerLink = "mslmc.cn/v3/";
                //Logger.LogInfo("连接到api：" + "https://api." + _link);
                if (((int)JObject.Parse((await HttpService.GetContentAsync("https://api." + ServerLink, headers => { headers.Add("DeviceID", DeviceID); }, 1)).ToString())["code"]) == 200)
                {
                    // 检查更新
                    await CheckUpdate(jsonObject);

                    // 下载必要DLL
                    if (downloadTermDll)
                    {
                        var result = await MagicShow.ShowDownloader(this, "https://file.mslmc.cn/Microsoft.Terminal.Control.dll", "MSL", "Microsoft.Terminal.Control.dll", "下载必要文件……");
                        if (result)
                        {
                            try
                            {
                                LoadLibEx();
                            }
                            catch (Exception ex)
                            {
                                File.Delete("MSL\\Microsoft.Terminal.Control.dll");
                                MagicShow.ShowMsg(this, $"必要DLL“Microsoft.Terminal.Control.dll”加载失败！可能是文件不完整，已将其删除，请重启软件以确保其被重新下载并加载。（{ex.Message}）\n如果不重启软件，高级终端（ConPty）功能将失效！\n若您多次重启软件后，此问题依旧未被解决，请联系作者进行反馈！", "错误");
                            }
                        }
                    }

                    // 判断是否开了自动更新软件，如果开了自动更新软件，说明之前的自动开服和自动开Frpc功能并未执行，这里开始执行
                    if (jsonObject["autoUpdateApp"] != null)
                    {
                        if (jsonObject["autoUpdateApp"].ToString() != "True") return;
                        try
                        {
                            // 检测是否开启对应功能
                            if (jsonObject["autoOpenServer"] != null && jsonObject["autoOpenServer"].ToString() != "False")
                            {
                                await AutoRunServer(jsonObject);
                            }
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchServerErr"] + ex.Message, LanguageManager.Instance["Error"]);
                        }
                        try
                        {
                            // 检测是否开启对应功能
                            if (jsonObject["autoOpenFrpc"] != null && jsonObject["autoOpenFrpc"].ToString() != "False")
                            {
                                await AutoRunFrpc(jsonObject);
                            }
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchFrpcErr"] + ex.Message, LanguageManager.Instance["Error"]);
                        }
                    }
                    return;
                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["MainWindow_GrowlMsg_MSLServerDown"], 2);
                }
            }
            catch (Exception ex)
            {
                Growl.Error(ex.Message);
            }
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
        private void LoadLibEx()
        {
            string md5Vaule = "";
            using (FileStream file = new FileStream("MSL\\Microsoft.Terminal.Control.dll", FileMode.Open))
            {
                MD5 md5 = new MD5CryptoServiceProvider();

                byte[] retVal = md5.ComputeHash(file);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                //Console.WriteLine(sb.ToString());
                md5Vaule = sb.ToString();
            }
            if (md5Vaule == "d01fd30d79d02f008d18565a9df8077d")
            {
                LoadLibraryEx("MSL\\Microsoft.Terminal.Control.dll", IntPtr.Zero, 0x00000008);
            }
            else
            {
                throw new Exception("错误的MD5值");
            }
        }

        private async Task CheckUpdate(JObject jsonObject)
        {
            //更新
            try
            {
                //Logger.LogInfo("检查更新……");
                JObject _httpReturn = await HttpService.GetApiContentAsync("query/update");
                string _version = _httpReturn["data"]["latestVersion"].ToString();
                Version newVersion = new Version(_httpReturn["data"]["latestVersion"].ToString());
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    //Logger.LogInfo("检测到新版本！");
                    var updatelog = _httpReturn["data"]["log"].ToString();
                    if (jsonObject["autoUpdateApp"] == null)
                    {
                        jsonObject.Add("autoUpdateApp", "False");
                        string convertString = Convert.ToString(jsonObject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    }
                    else if (jsonObject["autoUpdateApp"].ToString() == "True")
                    {
                        //Logger.LogInfo("自动更新功能已打开，更新新版本……");
                        await UpdateApp(_version);
                    }
                    else
                    {
                        if (await MagicShow.ShowMsgDialogAsync(this, string.Format(LanguageManager.Instance["MainWindow_GrowlMsg_UpdateInfo"] + "\n" + updatelog, _version), LanguageManager.Instance["MainWindow_GrowlMsg_Update"], true))
                        {
                            //Logger.LogInfo("更新新版本……");
                            await UpdateApp(_version);
                        }
                        else
                        {
                            //Logger.LogInfo("用户拒绝更新！");
                            Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_RefuseUpdate"]);
                            /*
                            IsOldVersion = true;
                            OldVersionTip();
                            */
                        }
                    }
                }
                else
                {
                    if (newVersion < version)
                    {
                        MagicFlowMsg.ShowMessage(LanguageManager.Instance["MainWindow_GrowlMsg_BetaVersion"], 4, panel: this);
                    }
                    else
                    {
                        MagicFlowMsg.ShowMessage(LanguageManager.Instance["MainWindow_GrowlMsg_LatestVersion"], 1, panel: this);
                    }

                }
            }
            catch (Exception ex)
            {
                //Logger.LogError("检测更新失败！");
                Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_CheckUpdateErr"] + $"\n{ex.Message}");
            }
        }

        private async Task UpdateApp(string latestVersion)
        {
            try
            {
                if (ProcessRunningCheck())
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["MainWindow_GrowlMsg_UpdateWarning"], LanguageManager.Instance["Warning"]);
                    return;
                }
                string downloadUrl = (await HttpService.GetApiContentAsync("download/update"))["data"].ToString(); ;
                await MagicShow.ShowDownloader(this, downloadUrl, AppDomain.CurrentDomain.BaseDirectory, "MSL" + latestVersion + ".exe", "下载新版本中……");
                if (File.Exists("MSL" + latestVersion + ".exe"))
                {
                    string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                    string newExeDir = AppDomain.CurrentDomain.BaseDirectory;

                    string cmdCommand = "/C choice /C Y /N /D Y /T 1 & Del \"" + oldExePath + "\" & Ren \"" + "MSL" + latestVersion + ".exe" + "\" \"MSL.exe\" & start \"\" \"MSL.exe\"";

                    // 关闭当前运行中的应用程序
                    Application.Current.Shutdown();

                    // 删除旧版本并启动新版本
                    Process delProcess = new Process();
                    delProcess.StartInfo.FileName = "cmd.exe";
                    delProcess.StartInfo.Arguments = cmdCommand;
                    Directory.SetCurrentDirectory(newExeDir);
                    delProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    delProcess.Start();

                    // 退出当前进程
                    Process.GetCurrentProcess().Kill();
                    //Environment.Exit(0);
                }
                else
                {
                    /*
                    IsOldVersion = true;
                    OldVersionTip();
                    */
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["MainWindow_GrowlMsg_UpdateFailed"], LanguageManager.Instance["Error"]);
                }
            }
            catch (Exception ex)
            {
                /*
                IsOldVersion = true;
                OldVersionTip();
                */
                MagicShow.ShowMsgDialog(this, "出现错误，更新失败！\n" + ex.Message, LanguageManager.Instance["Error"]);
            }
        }

        /*
        private void OldVersionTip()
        {
            var poptip = new Poptip
            {
                Content = "由于用户拒绝更新或检测更新失败，此版本可能并非最新版本",
                PlacementType = PlacementType.Right,
                HorizontalOffset = -345
            };
            var button = new Button
            {
                Name = "LowVersionTip",
                Margin = new Thickness(10, 0, 0, 0),
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Foreground = Brushes.Red
            };
            BorderElement.SetCornerRadius(button, new CornerRadius(12));
            IconElement.SetGeometry(button, Application.Current.FindResource("WarningGeometry") as Geometry);
            IconElement.SetHeight(button, 16d);

            AdornerElement.SetInstance(button, poptip);

            NonClientAreaContent = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Children = { button }
            };
        }
        */

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Logger.LogInfo("MSL，关闭！");
            if (MainNotifyIcon.Visibility == Visibility.Visible)
            {
                //Logger.LogWarning("托盘图标已打开，取消关闭事件！");
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
                //Logger.LogInfo("窗口已隐藏！");
            }
            else if (ProcessRunningCheck())
            {
                int dialog = MagicShow.ShowMsg(this, LanguageManager.Instance["MainWindow_Close_Warning"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                if (dialog != 1)
                {
                    e.Cancel = true;
                    //Logger.LogWarning("取消关闭事件！");
                }
            }
        }

        private static bool CheckServerRunning()
        {
            if (ServerList.RunningServers.Count != 0)
            {
                return true;
            }
            return false;
        }

        private static bool CheckFrpcRunning()
        {
            if (FrpcList.RunningFrpc.Count != 0)
            {
                return true;
            }
            return false;
        }

        public static bool ProcessRunningCheck()
        {
            try
            {
                if (CheckServerRunning() || CheckFrpcRunning() || OnlinePage.FrpcProcess.HasExited == false)
                {
                    //Logger.LogWarning("服务器、内网映射或联机功能正在运行中！");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CtrlNotifyIcon()//C_NotifyIcon
        {
            if (MainNotifyIcon.Visibility == Visibility.Collapsed)
            {
                //MessageBox.Show("111");
                MainNotifyIcon.Visibility = Visibility.Visible;
            }
            else
            {
                MainNotifyIcon.Visibility = Visibility.Collapsed;
            }
        }

        private void GotoOnlinePage()
        {
            SideMenu.SelectedIndex = 3;
            frame.Content = Pages[SideMenu.SelectedIndex];
        }

        private void GotoCreatePage()
        {
            SideMenu.SelectedIndex = 1;
            frame.Content = Pages[6];
        }

        private void GotoListPage()
        {
            SideMenu.SelectedIndex = 1;
            frame.Content = Pages[SideMenu.SelectedIndex];
        }

        private void GotoFrpcListPage()
        {
            SideMenu.SelectedIndex = 2;
            frame.Content = Pages[SideMenu.SelectedIndex];
        }

        private void OpenFrpcPage()
        {
            SideMenu.SelectedIndex = 2;
            if (!FrpcList.FrpcPageList.ContainsKey(FrpcList.FrpcID))
            {
                FrpcList.FrpcPageList.Add(FrpcList.FrpcID, new FrpcPage(FrpcList.FrpcID));
            }
            FrpcList.FrpcPageList.TryGetValue(FrpcList.FrpcID, out Page page);
            frame.Content = page;
        }

        private void ChangeSkinStyle()
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if ((bool)jsonObject["semitransparentTitle"] == true)
                {
                    ChangeTitleStyle(true);
                }
                else
                {
                    ChangeTitleStyle(false);
                }
                if (File.Exists("MSL\\Background.png"))//check background and set it
                {
                    frame.BorderThickness = new Thickness(0);
                    Background = new ImageBrush(SettingsPage.GetImage("MSL\\Background.png"));
                }
                else
                {
                    SetResourceReference(BackgroundProperty, "BackgroundBrush");
                    frame.BorderThickness = new Thickness(1, 0, 0, 0);
                }
                //RunFormChangeTitle();
            }
            catch
            { }
        }

        private void ChangeTitleStyle(bool isOpen)
        {
            if (isOpen)
            {
                this.SetResourceReference(NonClientAreaBackgroundProperty, "SideMenuBrush");
                this.SetResourceReference(NonClientAreaForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(CloseButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonHoverForegroundProperty, "PrimaryTextBrush");
            }
            else
            {
                this.SetResourceReference(NonClientAreaBackgroundProperty, "PrimaryBrush");
                NonClientAreaForeground = Brushes.White;
                CloseButtonForeground = Brushes.White;
                OtherButtonForeground = Brushes.White;
                OtherButtonHoverForeground = Brushes.White;
            }
        }

        private void SideMenuContextOpen_Click(object sender, RoutedEventArgs e)
        {
            if (SideMenu.Width == 50)
            {
                SideMenu.Width = double.NaN;
                //frame.Margin = new Thickness(100, 0, 0, 0);
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["sidemenuExpanded"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
            }
            else
            {
                SideMenu.Width = 50;
                //frame.Margin = new Thickness(50, 0, 0, 0);
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["sidemenuExpanded"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideMenu.SelectedIndex != -1)
            {
                frame.Content = Pages[SideMenu.SelectedIndex];
            }
        }

        private void MainNotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void NotifyClose_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessRunningCheck())
            {
                int dialog = MagicShow.ShowMsg(this, LanguageManager.Instance["MainWindow_Close_Warning2"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                if (dialog == 1)
                {
                    Application.Current.Shutdown();
                    //Process.GetCurrentProcess().Kill();
                    //Environment.Exit(0);
                }
            }
            else
            {
                Application.Current.Shutdown();
                //Process.GetCurrentProcess().Kill();
                //Environment.Exit(0);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
            //Environment.Exit(0);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(this.GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(this.GrowlPanel, false);
        }
    }
}
