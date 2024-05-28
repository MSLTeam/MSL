using HandyControl.Controls;
using HandyControl.Themes;
using MSL.controls;
using MSL.i18n;
using MSL.pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.MessageBox;

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
            new FrpcPage(),
            new OnlinePage(),
            new SettingsPage(),
            new About()
        };
        /*
        private readonly Home _homePage = new Home();
        private readonly ServerList _listPage = new ServerList();
        private readonly FrpcPage _frpcPage = new FrpcPage();
        private readonly OnlinePage _onlinePage = new OnlinePage();
        private readonly SettingsPage _setPage = new SettingsPage();
        private readonly About _aboutPage = new About();
        */
        public static event DeleControl LoadAnnounce;
        public static event DeleControl AutoOpenServer;
        public static event DeleControl AutoOpenFrpc;
        public static string serverLink = null;
        public static float PhisicalMemory;
        public static bool getServerInfo = false;
        public static bool getPlayerInfo = false;
        public static readonly bool isI18N = false; //标识当前版本是否支持i18n

        public MainWindow()
        {
            InitializeComponent();
            Home.GotoFrpcEvent += GotoOnlinePage;
            Home.CreateServerEvent += GotoCreatePage;
            ServerList.CreateServerEvent += GotoCreatePage;
            CreateServer.GotoServerList += GotoListPage;
            SettingsPage.C_NotifyIcon += CtrlNotifyIcon;
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            Topmost = false;
            Focus();

            if (!Directory.Exists("MSL"))
            {
                //Logger.LogWarning("未检测到MSL文件夹，已进行创建");
                Directory.CreateDirectory("MSL");
            }
            try
            {
                //firstLauchEvent
                if (!File.Exists(@"MSL\config.json"))
                {
                    //Logger.LogWarning("未检测到config.json文件，载入首次启动事件");
                    File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
                }
            }
            catch (Exception ex)
            {
                //Logger.LogError("生成config.json文件失败，原因："+ex.Message);
                await Shows.ShowMsgDialogAsync(this, LanguageManager.Instance["MainWindow_GrowlMsg_InitErr"] + ex.Message, LanguageManager.Instance["Dialog_Err"]);
                Close();
            }
            //Logger.LogInfo("开始载入配置文件……");
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                ////Logger.LogInfo("读取config.json成功！");
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取config.json失败！尝试重新载入……");
                await Shows.ShowMsgDialogAsync(this, LanguageManager.Instance["MainWindow_GrowlMsg_ConfigErr2"] + ex.Message, LanguageManager.Instance["Dialog_Err"]);
                File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
                jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                //Logger.LogInfo("读取config.json成功！");
            }
            try
            {
                if (jsonObject["lang"] == null)
                {
                    jsonObject.Add("lang", "zh-CN");
                    string convertString = Convert.ToString(jsonObject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    LanguageManager.Instance.ChangeLanguage(new CultureInfo("zh-CN"));
                }
                else
                {
                    LanguageManager.Instance.ChangeLanguage(new CultureInfo(jsonObject["lang"].ToString()));
                }
            }
            finally
            {
                bool onlineMode = await EulaEvent(jsonObject);
                //Logger.LogInfo("主窗体UI控件加载完毕！");
                await Task.Run(() =>
                {
                    AsyncLoadEvent(jsonObject);
                    if (onlineMode)
                    {
                        OnlineService(jsonObject);
                    }
                    //Logger.LogInfo("异步载入线程已启动！");
                });
            }
        }

        private async Task<bool> EulaEvent(JObject jsonObject)
        {
            if (jsonObject["eula"] == null || (bool)jsonObject["eula"] == false)
            {
                int dialog = Shows.ShowMsg(this, LanguageManager.Instance["MainWindow_GrowlMsg_Eula"], LanguageManager.Instance["Dialog_Tip"], true, LanguageManager.Instance["MainWindow_GrowlMsg_Agree"], LanguageManager.Instance["MainWindow_GrowlMsg_ReadEula"]);
                if (dialog == 0)
                {
                    if (jsonObject["eula"] == null)
                    {
                        jsonObject.Add("eula", true);
                    }
                    else
                    {
                        jsonObject["eula"] = true;
                    }
                    string convertString = Convert.ToString(jsonObject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    return true;
                }
                else if (dialog == 2)
                {
                    //Logger.LogWarning("用户未同意使用协议……");
                    await Shows.ShowMsgDialogAsync(this, "您未同意用户使用协议，为保障您的权益，软件将以离线模式运行！", "提示");
                    return false;
                }
                else
                {
                    Process.Start("https://www.mslmc.cn/eula.html");
                    return await EulaEvent(jsonObject);
                }
            }
            else
            {
                return true;
            }
        }

        private void AsyncLoadEvent(JObject jsonObject)
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
                    Dispatcher.Invoke(() =>
                    {
                        CtrlNotifyIcon();
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
                    Dispatcher.Invoke(() =>
                    {
                        SideMenu.Width = double.NaN;
                    });
                }
                else if ((bool)jsonObject["sidemenuExpanded"] == true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SideMenu.Width = double.NaN;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        SideMenu.Width = 50;
                    });
                }
                //Logger.LogInfo("读取侧栏配置成功！");
                if (jsonObject["skin"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("skin", 1);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    Dispatcher.Invoke(() =>
                    {
                        BrushConverter brushConverter = new BrushConverter();
                        ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
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
                    });
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
                    Dispatcher.Invoke(() =>
                    {
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    });
                }
                //Logger.LogInfo("读取暗色模式配置成功！");
                if (File.Exists("MSL\\Background_.png"))
                {
                    File.Copy("MSL\\Background_.png", "MSL\\Background.png", true);
                    File.Delete("MSL\\Background_.png");
                }
                if (File.Exists("MSL\\Background.png"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Background = new ImageBrush(SettingsPage.GetImage("MSL\\Background.png"));
                        frame.BorderThickness = new Thickness(0);
                    });
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
                else if ((bool)jsonObject["semitransparentTitle"] == true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ChangeTitleStyle(true);
                    });
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
                //Logger.LogError("读取配置时出现错误！错误代码："+ex.Message);
                Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_ConfigErr"] + ex.Message);
                File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
                //jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                //Growl.Info("配置文件已更新！");
            }

            //获取电脑内存
            try
            {
                //Logger.LogInfo("读取系统内存……");
                PhisicalMemory = GetPhisicalMemory();
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取系统内存失败！");
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_MemoryErr"] + ex.Message, LanguageManager.Instance["Dialog_Err"], MessageBoxButton.OK, MessageBoxImage.Error);
                PhisicalMemory = 0;
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
                    string servers = jsonObject["autoOpenServer"].ToString();
                    Growl.Info(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchServer"]);
                    while (servers != "")
                    {
                        int aserver = servers.IndexOf(",");
                        ServerList.serverID = servers.Substring(0, aserver);
                        AutoOpenServer();
                        servers = servers.Replace(ServerList.serverID.ToString() + ",", "");
                    }
                }
                //Logger.LogInfo("读取自动开启（服务器）配置成功！");
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取自动开启（服务器）配置失败！");
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchServerErr"] + ex.Message, LanguageManager.Instance["Dialog_Err"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //自动开启Frpc
            try
            {
                if (jsonObject["autoOpenFrpc"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoOpenFrpc", false);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if ((bool)jsonObject["autoOpenFrpc"] == true)
                {
                    Growl.Info(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchFrpc"]);
                    AutoOpenFrpc();
                }
                //Logger.LogInfo("读取自动开启（内网映射）配置成功！");
            }
            catch (Exception ex)
            {
                //Logger.LogError("读取自动开启（内网映射）配置失败！");
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_AutoLaunchFrpsErr"] + ex.Message, LanguageManager.Instance["Dialog_Err"], MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //Logger.LogInfo("所有配置载入完毕！调整UI界面……");
            Dispatcher.Invoke(() =>
            {
                SideMenu.SelectedIndex = 0;
            });
            //Logger.LogInfo("软件加载完毕！");
        }

        private async void OnlineService(JObject jsonObject)
        {
            //get serverlink
            try
            {
                serverLink = Functions.Get("", "https://msl-server.oss-cn-hangzhou.aliyuncs.com/", true);
                try
                {
                    if (((int)((JObject)JsonConvert.DeserializeObject(Functions.Get("")))["status"]) != 200)
                    {
                        serverLink = "waheal.top";
                        Growl.Info(LanguageManager.Instance["MainWindow_GrowlMsg_MslServerDown"]);
                    }
                }
                catch
                {
                    serverLink = "waheal.top";
                    Growl.Info(LanguageManager.Instance["MainWindow_GrowlMsg_MSLServerDown"]);
                }
            }
            catch
            {
                serverLink = "waheal.top";
                //Logger.LogError("在匹配在线服务器时出现错误，已连接至备用服务器");
            }

            //更新
            try
            {
                string _httpReturn = Functions.Get("query/update");
                Version newVersion = new Version(_httpReturn);
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    //Logger.LogInfo("检测到新版本！");
                    var updatelog = Functions.Get("query/update/log");
                    if (jsonObject["autoUpdateApp"] == null)
                    {
                        string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject.Add("autoUpdateApp", "False");
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    }
                    else if (jsonObject["autoUpdateApp"].ToString() == "True")
                    {
                        //Logger.LogInfo("自动更新功能已打开，更新新版本……");
                        UpdateApp(_httpReturn);
                    }
                    await Dispatcher.Invoke(async () =>
                    {
                        bool dialog = await Shows.ShowMsgDialogAsync(this, LanguageManager.Instance["MainWindow_GrowlMsg_UpdateInfo1"] + _httpReturn + LanguageManager.Instance["MainWindow_GrowlMsg_UpdateInfo2"] + updatelog, LanguageManager.Instance["MainWindow_GrowlMsg_Update"], true);
                        if (dialog == true)
                        {
                            //Logger.LogInfo("更新新版本……");
                            UpdateApp(_httpReturn);
                        }
                        else
                        {
                            //Logger.LogInfo("用户拒绝更新！");
                            Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_RefuseUpdate"]);
                        }
                    });
                }
                else if (newVersion < version)
                {
                    Growl.Info(LanguageManager.Instance["MainWindow_GrowlMsg_BetaVersion"]);
                }
                else
                {
                    Growl.Success(LanguageManager.Instance["MainWindow_GrowlMsg_LatestVersion"]);
                }
            }
            catch
            {
                //Logger.LogError("检测更新失败！");
                Growl.Error(LanguageManager.Instance["MainWindow_GrowlMsg_CheckUpdateErr"]);
            }
            await Dispatcher.InvokeAsync(() =>
            {
                LoadAnnounce();
            });

        }

        private async void UpdateApp(string latestVersion)
        {
            if (ProcessRunningCheck())
            {
                Shows.ShowMsgDialog(this, LanguageManager.Instance["MainWindow_GrowlMsg_UpdateWarning"], LanguageManager.Instance["Dialog_Warning"]);
                return;
            }
            string downloadUrl = Functions.Get("download/update?type=normal"); ;
            if (isI18N)
            {
                downloadUrl = Functions.Get("download/update?type=i18n");
            }
            await Shows.ShowDownloader(this, downloadUrl, AppDomain.CurrentDomain.BaseDirectory, "MSL" + latestVersion + ".exe", "下载新版本中……");
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
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_UpdateFailed"], LanguageManager.Instance["Dialog_Err"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static long GetPhisicalMemory()
        {
            long amemory = 0;
            //获得物理内存 
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (mo["TotalPhysicalMemory"] != null)
                {
                    amemory = long.Parse(mo["TotalPhysicalMemory"].ToString());
                }
            }
            return amemory;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Logger.LogInfo("MSL，关闭！");
            if (MainNoticyIcon.Visibility == Visibility.Visible)
            {
                //Logger.LogWarning("托盘图标已打开，取消关闭事件！");
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
                //Logger.LogInfo("窗口已隐藏！");
            }
            else if (ProcessRunningCheck())
            {
                int dialog = Shows.ShowMsg(this, LanguageManager.Instance["MainWindow_GrowlMsg_Close"], LanguageManager.Instance["Dialog_Warning"], true, LanguageManager.Instance["Dialog_Cancel"]);
                if (dialog != 1)
                {
                    e.Cancel = true;
                    //Logger.LogWarning("取消关闭事件！");
                }
            }
        }

        private static bool CheckServerRunning()
        {
            foreach (var item in ServerList.runningServers)
            {
                if (item.Value == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ProcessRunningCheck()
        {
            try
            {
                if (CheckServerRunning() || FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
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
                try
                {
                    if (FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
                    {
                        //Logger.LogWarning("内网映射或联机功能正在运行中！");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    try
                    {
                        if (OnlinePage.FRPCMD.HasExited == false)
                        {
                            //Logger.LogWarning("联机功能正在运行中！");
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
            }
        }

        private void CtrlNotifyIcon()//C_NotifyIcon
        {
            if (MainNoticyIcon.Visibility == Visibility.Hidden)
            {
                MainNoticyIcon.Visibility = Visibility.Visible;
            }
            else
            {
                MainNoticyIcon.Visibility = Visibility.Hidden;
            }
        }

        private void GotoOnlinePage()
        {
            //frame.Content = _onlinePage;
            SideMenu.SelectedIndex = 3;
            frame.Content = Pages[SideMenu.SelectedIndex];
        }

        private void GotoCreatePage()
        {
            SideMenu.SelectedIndex = 1;
            frame.Content = new CreateServer();
            //SideMenu.SelectedIndex = -1;
        }

        private void GotoListPage()
        {
            //frame.Content = _listPage;
            //SideMenu.SelectedIndex = 1;
            frame.Content = Pages[SideMenu.SelectedIndex];
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
                TitleBox.SetResourceReference(ForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(CloseButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonHoverForegroundProperty, "PrimaryTextBrush");
            }
            else
            {
                this.SetResourceReference(NonClientAreaBackgroundProperty, "PrimaryBrush");
                TitleBox.Foreground = Brushes.White;
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

        private void SideMenu_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SideMenu.SelectedIndex != -1)
            {
                frame.Content = Pages[SideMenu.SelectedIndex];
            }
        }

        private void MainNoticyIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void NotifyClose_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessRunningCheck())
            {
                int dialog = Shows.ShowMsg(this, LanguageManager.Instance["MainWindow_GrowlMsg_Close2"], LanguageManager.Instance["Dialog_Warning"], true, LanguageManager.Instance["Dialog_Cancel"]);
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
            //Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }
    }
}
