using HandyControl.Controls;
using HandyControl.Themes;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL
{
    public delegate void DeleControl();
    /// <summary>
    /// xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Home _homePage = new Home();
        readonly ServerList _listPage = new ServerList();
        readonly FrpcPage _frpcPage = new FrpcPage();
        readonly OnlinePage _onlinePage = new OnlinePage();
        readonly SettingsPage _setPage = new SettingsPage();
        readonly About _aboutPage = new About();
        public static event DeleControl AutoOpenServer;
        public static event DeleControl AutoOpenFrpc;
        public static string serverid;
        public static string serverLink;
        public static float PhisicalMemory;
        public static bool getServerInfo = false;
        public static bool getPlayerInfo = false;

        public MainWindow()
        {
            InitializeComponent();
            Home.GotoFrpcEvent += GotoOnlinePage;
            SettingsPage.C_NotifyIcon += CtrlNotifyIcon;
            ServerRunner.GotoFrpcEvent += GotoFrpcPage;
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            LoadingCircle loadingCircle = new LoadingCircle();
            BodyGrid.Children.Add(loadingCircle);
            BodyGrid.RegisterName("loadingBar", loadingCircle);
            this.Topmost = false;
            Thread thread = new Thread(AsyncLoadEvent);
            thread.Start();
        }

        private void AsyncLoadEvent()
        {
            //get serverlink
            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData = MyWebClient.DownloadData("https://waheal.oss-cn-hangzhou.aliyuncs.com/");
                //serverLink = Encoding.UTF8.GetString(pageData);
                string serverAddr = Encoding.UTF8.GetString(pageData);
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(serverAddr, 2000); // 替换成您要 ping 的 IP 地址
                if (reply.Status == IPStatus.Success)
                {
                    serverLink = "http://" + serverAddr;
                }
                else
                {
                    serverLink = "https://msl.waheal.top";
                    Growl.Info("MSL主服务器连接超时（可能被DDos），已切换至备用服务器！");
                }
            }
            catch
            {
                serverLink = "https://msl.waheal.top";
            }

            //MessageBox.Show("GetLinkSuccess");

            try
            {
                //firstLauchEvent
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL"))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"MSL");
                }
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json"))
                {
                    Process.Start("https://www.waheal.top/eula.html");
                    Dispatcher.Invoke(new Action(delegate
                    {
                        bool dialog= DialogShow.ShowMsg(this, "请阅读并同意MSL开服器使用协议：https://www.waheal.top/eula.html", "提示", true, "不同意", "同意");
                        if (!dialog)
                        {
                            Close();
                        }
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", string.Format("{{{0}}}", "\n"));
                    }));
                }
            }
            catch (Exception ex)
            {
                DialogShow.ShowMsg(this, "MSL在初始化加载过程中出现问题，请尝试用管理员身份运行MSL……\n错误代码：" + ex.Message, "错误");
            }

            //MessageBox.Show("CheckDirSuccess");
            JObject jsonObject = null;
            try
            {
                jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8));
            }
            catch(Exception ex)
            {
                DialogShow.ShowMsg(this, "MSL在加载配置文件时出现错误，将进行重试，若点击确定后软件突然闪退，请尝试使用管理员身份运行或将此问题报告给作者！\n错误代码："+ex.Message, "错误");
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", string.Format("{{{0}}}", "\n"));
                jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8));
            }

            //下面是加载配置部分
            try
            {
                if (jsonObject["frpc"] != null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Remove("frpc");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                //MessageBox.Show("CheckFrpcSuccess");
                if (jsonObject["notifyIcon"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("notifyIcon", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["notifyIcon"].ToString() == "True")
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        CtrlNotifyIcon();
                    }));
                }
                //MessageBox.Show("CheckNotifySuccess");
                if (jsonObject["sidemenu"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("sidemenu", "0");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                    Dispatcher.Invoke(new Action(delegate
                    {
                        sideMenuContextOpen.Width = 100;
                        SideMenu.Width = 100;
                        frame.Margin = new Thickness(100, 0, 0, 0);
                    }));
                }
                else if (jsonObject["sidemenu"].ToString() == "0")
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        sideMenuContextOpen.Width = 100;
                        SideMenu.Width = 100;
                        frame.Margin = new Thickness(100, 0, 0, 0);
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        sideMenuContextOpen.Width = 50;
                        SideMenu.Width = 50;
                        frame.Margin = new Thickness(50, 0, 0, 0);
                    }));
                }
                //MessageBox.Show("CheckSidemenuSuccess");
                if (jsonObject["skin"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("skin", "1");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                    Dispatcher.Invoke(new Action(delegate
                    {
                        BrushConverter brushConverter = new BrushConverter();
                        ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        switch (jsonObject["skin"].ToString())
                        {
                            case "0":
                                ThemeManager.Current.UsingSystemTheme = true;
                                break;
                            case "1":
                                BrushConverter brushConverter = new BrushConverter();
                                ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                                break;
                            case "2":
                                ThemeManager.Current.AccentColor = Brushes.Red;
                                break;
                            case "3":
                                ThemeManager.Current.AccentColor = Brushes.Green;
                                break;
                            case "4":
                                ThemeManager.Current.AccentColor = Brushes.Orange;
                                break;
                            case "5":
                                ThemeManager.Current.AccentColor = Brushes.Purple;
                                break;
                            case "6":
                                ThemeManager.Current.AccentColor = Brushes.DeepPink;
                                break;
                            default:
                                BrushConverter _brushConverter = new BrushConverter();
                                ThemeManager.Current.AccentColor = (Brush)_brushConverter.ConvertFromString("#0078D4");
                                break;
                        }
                    }));
                }
                //MessageBox.Show("SkinLoaded");
                if (jsonObject["darkTheme"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("darkTheme", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["darkTheme"].ToString() == "True")
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    }));
                }
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"))
                {
                    Background = new ImageBrush(SettingsPage.GetImage(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"));
                    SideMenuBorder.BorderThickness = new Thickness(0);
                }
                //MessageBox.Show("BackgroundLoaded");
                if (jsonObject["semitransparentTitle"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("semitransparentTitle", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["semitransparentTitle"].ToString() == "True")
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        ChangeTitleStyle(true);
                    }));
                }
                //MessageBox.Show("ThemeLoaded");
                if (jsonObject["autoGetServerInfo"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoGetServerInfo", "True");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                    getServerInfo = true;
                }
                else if (jsonObject["autoGetServerInfo"].ToString() == "True")
                {
                    getServerInfo = true;
                }
                if (jsonObject["autoGetPlayerInfo"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoGetPlayerInfo", "True");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                    getPlayerInfo = true;
                }
                else if (jsonObject["autoGetPlayerInfo"].ToString() == "True")
                {
                    getPlayerInfo = true;
                }
                //MessageBox.Show("AutoGetSuccess");
            }
            catch (Exception ex)
            {
                Growl.Error("MSL在加载配置文件时出现错误，此报错可能不影响软件运行，但还是建议您将其反馈给作者！\n错误代码：" + ex.Message);
            }

            //更新
            try
            {
                string pageHtml = Functions.Get("update");
                string strtempa = "#";
                int IndexofA = pageHtml.IndexOf(strtempa);
                string Ru = pageHtml.Substring(IndexofA + 1);
                string aaa = Ru.Substring(0, Ru.IndexOf("#"));
                if (aaa.Contains("v"))
                {
                    aaa = aaa.Replace("v", "");
                }
                Version newVersion = new Version(aaa);
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    /*
                    byte[] _updatelog = MyWebClient.DownloadData(serverLink + @"/msl/updatelog.txt");
                    string updatelog = Encoding.UTF8.GetString(_updatelog);
                    */
                    string updatelog = Functions.Post("update", 1);
                    Dispatcher.Invoke(new Action(delegate
                    {
                        try
                        {
                            if (jsonObject["autoUpdateApp"] == null)
                            {
                                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                                JObject jobject = JObject.Parse(jsonString);
                                jobject.Add("autoUpdateApp", "False");
                                string convertString = Convert.ToString(jobject);
                                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                            }
                            else if (jsonObject["autoUpdateApp"].ToString() == "True")
                            {
                                UpdateApp(pageHtml, aaa);
                            }
                        }
                        catch
                        { }
                        bool dialog = DialogShow.ShowMsg(this, "发现新版本，版本号为：" + aaa + "，是否进行更新？\n更新日志：\n" + updatelog, "更新", true, "取消");
                        if (dialog == true)
                        {
                            UpdateApp(pageHtml, aaa);
                        }
                        else
                        {
                            Growl.Error("您拒绝了更新新版本，若在此版本中遇到bug，请勿报告给作者！");
                        }
                    }));
                }
                else if (newVersion < version)
                {
                    Growl.Info("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！");
                }
                else
                {
                    Growl.Success("您使用的开服器已是最新版本！");
                }
            }
            catch
            {
                Growl.Error("检查更新失败！");
            }

            //MessageBox.Show("CheckUpdateSuccess");

            //获取电脑内存
            try
            {
                PhisicalMemory = GetPhisicalMemory();
            }
            catch(Exception ex)
            {
                MessageBox.Show("获取系统内存失败！" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                PhisicalMemory = 0;
            }

            //自动开启服务器
            try
            {
                if (jsonObject["autoOpenServer"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoOpenServer", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["autoOpenServer"].ToString() != "False")
                {
                    string servers = jsonObject["autoOpenServer"].ToString();
                    Growl.Info("正在为你自动打开相应服务器……");
                    while (servers != "")
                    {
                        int aserver = servers.IndexOf(",");
                        serverid = servers.Substring(0, aserver);
                        AutoOpenServer();
                        servers = servers.Replace(serverid + ",", "");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("自动启动服务器失败！" + ex.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            //自动开启Frpc
            try
            {
                if (jsonObject["autoOpenFrpc"] == null)
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("autoOpenFrpc", "False");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, Encoding.UTF8);
                }
                else if (jsonObject["autoOpenFrpc"].ToString() == "True")
                {
                    Growl.Info("正在为你自动打开内网映射……");
                    AutoOpenFrpc();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("自动启动内网映射失败！" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //MessageBox.Show("AutoEventSuccess");

            Dispatcher.Invoke(new Action(delegate
            {
                //frame.Content = _homePage;
                SideMenu.IsEnabled = true;
                SideMenu.SelectedIndex = 0;
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                BodyGrid.Children.Remove(loadingCircle);
                BodyGrid.UnregisterName("loadingBar");
            }));
        }

        private void UpdateApp(string pageHtml, string aaa)
        {
            string strtempa1 = "* ";
            int IndexofA1 = pageHtml.IndexOf(strtempa1);
            string Ru1 = pageHtml.Substring(IndexofA1 + 2);
            string aaa1 = Ru1.Substring(0, Ru1.IndexOf(" *"));
            DialogShow.ShowDownload(this, aaa1, AppDomain.CurrentDomain.BaseDirectory, "MSL" + aaa + ".exe", "下载新版本中……");
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL" + aaa + ".exe"))
            {
                string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                //string dwnExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MSL" + aaa + ".exe");
                string newExeDir = AppDomain.CurrentDomain.BaseDirectory;

                // 输出CMD命令以便调试
                string cmdCommand = "/C choice /C Y /N /D Y /T 1 & Del \"" + oldExePath + "\" & Ren \"" + "MSL" + aaa + ".exe" + "\" \"MSL.exe\" & start \"\" \"MSL.exe\"";
                //MessageBox.Show(cmdCommand);

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
            }
            else
            {
                MessageBox.Show("更新失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (MainNoticyIcon.Visibility == Visibility.Visible)
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
                return;
            }
            else if (CloseApp())
            {
                Application.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                e.Cancel = true;
            }
        }
        private bool CloseApp()
        {
            try
            {
                if (ServerList.RunningServerIDs != "" || FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
                {
                    bool dialog = DialogShow.ShowMsg(this, "您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让服务器进程在后台一直运行并占用资源！确定要继续关闭吗？\n注：如果想隐藏主窗口的话，请前往设置打开托盘图标", "警告", true, "取消");
                    if (dialog == true)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return true;
                }
            }
            catch
            {
                try
                {
                    if (FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
                    {
                        bool dialog = DialogShow.ShowMsg(this, "内网映射或联机功能正在运行中，关闭软件可能会让内网映射进程在后台一直运行并占用资源！确定要继续关闭吗？\n如果想隐藏主窗口的话，请前往设置打开托盘图标", "警告", true, "取消");
                        if (dialog == true)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                catch
                {
                    try
                    {
                        if (OnlinePage.FRPCMD.HasExited == false)
                        {
                            bool dialog = DialogShow.ShowMsg(this, "联机功能正在运行中，关闭软件可能会让内网映射进程在后台一直运行并占用资源！确定要继续关闭吗？\n如果想隐藏主窗口的话，请前往设置打开托盘图标", "警告", true, "取消");
                            if (dialog == true)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        return true;
                    }
                }
            }
        }

        void CtrlNotifyIcon()//C_NotifyIcon
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

        void GotoFrpcPage()
        {
            this.Focus();
            frame.Content = _frpcPage;
            SideMenu.SelectedIndex = 2;
        }
        void GotoOnlinePage()
        {
            frame.Content = _onlinePage;
            SideMenu.SelectedIndex = 3;
        }
        void ChangeSkinStyle()
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8));
                if (jsonObject["semitransparentTitle"].ToString() == "True")
                {
                    ChangeTitleStyle(true);
                }
                else
                {
                    ChangeTitleStyle(false);
                }
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"))//check background and set it
                {
                    SideMenuBorder.BorderThickness = new Thickness(0);
                    Background = new ImageBrush(SettingsPage.GetImage(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"));
                }
                else
                {
                    SetResourceReference(BackgroundProperty, "BackgroundBrush");
                    SideMenuBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                }
                //RunFormChangeTitle();
            }
            catch
            { }
        }
        void ChangeTitleStyle(bool isOpen)
        {
            if (isOpen)
            {
                TitleGrid.SetResourceReference(BackgroundProperty, "SideMenuBrush");
                TitleBox.SetResourceReference(ForegroundProperty, "TextBlockBrush");
                MaxBtn.SetResourceReference(ForegroundProperty, "TextBlockBrush");
                MinBtn.SetResourceReference(ForegroundProperty, "TextBlockBrush");
                CloseBtn.SetResourceReference(ForegroundProperty, "TextBlockBrush");
            }
            else
            {
                TitleGrid.SetResourceReference(BackgroundProperty, "PrimaryBrush");
                TitleBox.Foreground = Brushes.White;
                MaxBtn.Foreground = Brushes.White;
                MinBtn.Foreground = Brushes.White;
                CloseBtn.Foreground = Brushes.White;
            }
        }
        public static void SetDynamicResourceKey(DependencyObject obj, DependencyProperty prop, object resourceKey)
        {
            var dynamicResource = new DynamicResourceExtension(resourceKey);
            var resourceReferenceExpression = dynamicResource.ProvideValue(null);
            obj.SetValue(prop, resourceReferenceExpression);
        }

        private void sideMenuContextOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenuContextOpen.Width == 50)
            {
                sideMenuContextOpen.Width = 100;
                SideMenu.Width = 100;
                frame.Margin = new Thickness(100, 0, 0, 0);
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["sidemenu"] = "0";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
            }
            else
            {
                sideMenuContextOpen.Width = 50;
                SideMenu.Width = 50;
                frame.Margin = new Thickness(50, 0, 0, 0);
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["sidemenu"] = "1";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }


        private void SideMenu_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (SideMenu.SelectedIndex)
            {
                case 0:
                    frame.Content = _homePage;
                    break;
                case 1:
                    frame.Content = _listPage;
                    break;
                case 2:
                    frame.Content = _frpcPage;
                    break;
                case 3:
                    frame.Content = _onlinePage;
                    break;
                case 4:
                    frame.Content = _setPage;
                    break;
                case 5:
                    frame.Content = _aboutPage;
                    break;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MainGrid.Margin = new Thickness(7);

            }
            else
            {
                MainGrid.Margin = new Thickness(0);
            }
        }

        private void MainNoticyIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void NotifyClose_Click(object sender, RoutedEventArgs e)
        {
            if (CloseApp())
            {
                Application.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                return;
            }
        }
    }
}
