using HandyControl.Controls;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;
using HandyControl.Themes;
using HandyControl.Tools.Extension;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Resources;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Web;
using System.Text.RegularExpressions;

namespace MSL
{
    public delegate void DeleControl();
    /// <summary>
    /// xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //public static string update = "v3.4.8.4";
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
            SettingsPage.ChangeTitleStyle += ChangeTitleStyle;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingCircle loadingCircle = new LoadingCircle();
            BodyGrid.Children.Add(loadingCircle);
            BodyGrid.RegisterName("loadingBar", loadingCircle);

            Thread thread = new Thread(AsyncLoadEvent);
            thread.Start();
        }

        //[DllImport("kernel32.dll")]
        //public static extern uint WinExec(string lpCmdLine, uint uCmdShow);
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
                    serverLink = "http://"+serverAddr;
                    //serverLink = "https://msl.waheal.top";
                }
                else
                {
                    serverLink = "https://msl.waheal.top";
                    Growl.Info("MSL主服务器连接超时，已切换至备用服务器！");
                }
            }
            catch
            {
                serverLink = "https://msl.waheal.top";
                Growl.Info("出现错误，已切换至备用服务器！");
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
                        DialogShow.ShowMsg(this, "请阅读并同意MSL开服器使用协议：https://www.waheal.top/eula.html", "提示", true, "不同意", "同意");
                        if (!MessageDialog._dialogReturn)
                        {

                            Close();
                        }
                        MessageDialog._dialogReturn = false;
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", string.Format("{{{0}}}", "\n"));
                    }));
                }
            }
            catch(Exception ex)
            {
                DialogShow.ShowMsg(this, "MSL在初始化加载过程中出现问题，请尝试用管理员身份运行MSL……\n错误代码：" + ex.Message, "错误");
            }

            //MessageBox.Show("CheckDirSuccess");

            JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", Encoding.UTF8));

            //检测是否配置了内网映射(新版已弃用，现改为删除frpc的key)
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
            }
            catch
            {}

            //MessageBox.Show("CheckFrpcSuccess");

            //托盘图标检测
            try
            {
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
            }
            catch
            {}

            //MessageBox.Show("CheckNotifySuccess");

            //侧边栏检测
            try
            {
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
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    sideMenuContextOpen.Width = 100;
                    SideMenu.Width = 100;
                    frame.Margin = new Thickness(100, 0, 0, 0);
                }));
            }

            //MessageBox.Show("CheckSidemenuSuccess");

            //background
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"))
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Background = new ImageBrush(SettingsPage.GetImage(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"));
                    SideMenuBorder.BorderThickness = new Thickness(0);
                }));
            }
            //skin
            try
            {
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
                    if (jsonObject["skin"].ToString() == "0")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.UsingSystemTheme = true;
                        }));
                    }
                    if (jsonObject["skin"].ToString() == "1")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            BrushConverter brushConverter = new BrushConverter();
                            ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                        }));
                        //Growl.Success("皮肤切换成功！");
                    }
                    else if (jsonObject["skin"].ToString() == "2")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.AccentColor = Brushes.Red;
                        }));
                        Growl.Success("皮肤切换成功！");
                    }
                    else if (jsonObject["skin"].ToString() == "3")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.AccentColor = Brushes.Green;
                        }));
                        Growl.Success("皮肤切换成功！");
                    }
                    else if (jsonObject["skin"].ToString() == "4")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.AccentColor = Brushes.Orange;
                        }));
                        Growl.Success("皮肤切换成功！");
                    }
                    else if (jsonObject["skin"].ToString() == "5")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.AccentColor = Brushes.Purple;
                        }));
                        Growl.Success("皮肤切换成功！");
                    }
                    else if (jsonObject["skin"].ToString() == "6")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            ThemeManager.Current.AccentColor = Brushes.DeepPink;
                        }));
                        Growl.Success("皮肤切换成功！");
                    }
                }

            }
            catch
            { }

            //MessageBox.Show("SkinLoaded");

            try
            {
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
            }
            catch
            { }
            try
            {
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
            }
            catch
            { }

            //MessageBox.Show("ThemeLoaded");


            //自动获取占用和自动记录玩家功能
            try
            {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Err" + ex.Message);
            }
            try
            {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Err" + ex.Message);
            }

            //MessageBox.Show("AutoGetSuccess");

            //更新
            try
            {
                /*
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData = MyWebClient.DownloadData(serverLink + @"/msl/update.txt");
                string pageHtml = Encoding.UTF8.GetString(pageData);
                */

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
                    string updatelog = Functions.Post("update",1);
                    Dispatcher.Invoke(new Action(delegate
                    {
                        bool dialog = DialogShow.ShowMsg(this, "发现新版本，版本号为：" + aaa + "，是否进行更新？\n更新日志：\n" + updatelog, "更新", true, "取消");
                        if (dialog == true)
                        {
                            string strtempa1 = "* ";
                            int IndexofA1 = pageHtml.IndexOf(strtempa1);
                            string Ru1 = pageHtml.Substring(IndexofA1 + 2);
                            string aaa1 = Ru1.Substring(0, Ru1.IndexOf(" *"));
                            DialogShow.ShowDownload(this, aaa1, AppDomain.CurrentDomain.BaseDirectory, "MSL" + aaa + ".exe", "下载新版本中……");
                            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL" + aaa + ".exe"))
                            {
                                /*
                                string vBatFile = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + @"\DEL.bat";
                                using (StreamWriter vStreamWriter = new StreamWriter(vBatFile, false, Encoding.Default))
                                {
                                    vStreamWriter.Write(string.Format(":del\r\n del \"" + System.Windows.Forms.Application.ExecutablePath + "\"\r\n " + "if exist \"" + System.Windows.Forms.Application.ExecutablePath + "\" goto del\r\n " + "start /d \"" + AppDomain.CurrentDomain.BaseDirectory + "\" MSL" + aaa + ".exe" + "\r\n" + " del %0\r\n", AppDomain.CurrentDomain.BaseDirectory));
                                }
                                WinExec(vBatFile, 0);
                                Process.GetCurrentProcess().Kill();
                                */
                                string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                                string dwnExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MSL" + aaa + ".exe");
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
                        else
                        {
                            Growl.Error("您拒绝了更新新版本，若在此版本中遇到bug，请勿报告给作者！");
                        }
                    }));
                }
                else if(newVersion<version)
                {
                    Growl.Info("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！");
                }
                else
                {
                    Growl.Success("您使用的开服器已是最新版本！");
                }
            }
            catch(Exception ex)
            {
                Growl.Error("检查更新失败！"+ex.Message);
            }

            //MessageBox.Show("CheckUpdateSuccess");

            /****************************************************************
             * 
             * 临时增加的serverlist.ini转serverlist.json模块
             * 后续版本要移除
             * 
             * **************************************************************/
            /*
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\ServerList.ini"))
            {
                string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\ServerList.ini");
                while (line.IndexOf("*") + 1 != 0)
                {

                    int IndexofA3 = line.IndexOf("-s ");
                    string Ru3 = line.Substring(IndexofA3 + 3);
                    string servercore = Ru3.Substring(0, Ru3.IndexOf("|"));

                    int IndexofA1 = line.IndexOf("-n ");
                    string Ru1 = line.Substring(IndexofA1 + 3);
                    string servername = Ru1.Substring(0, Ru1.IndexOf("|"));
                    //serverjavalist.Items.Add(a200);

                    int IndexofA2 = line.IndexOf("-j ");
                    string Ru2 = line.Substring(IndexofA2 + 3);
                    string serverjava = Ru2.Substring(0, Ru2.IndexOf("|"));

                    int IndexofA4 = line.IndexOf("-a ");
                    string Ru4 = line.Substring(IndexofA4 + 3);
                    string servermemory = Ru4.Substring(0, Ru4.IndexOf("|"));
                    //serverJVMlist.Items.Add(a400);

                    int IndexofA5 = line.IndexOf("-b ");
                    string Ru5 = line.Substring(IndexofA5 + 3);
                    string serverbase = Ru5.Substring(0, Ru5.IndexOf("|"));

                    int IndexofA6 = line.IndexOf("-c ");
                    string Ru6 = line.Substring(IndexofA6 + 3);
                    string serverargs = Ru6.Substring(0, Ru6.IndexOf("|"));
                    //serverbaselist.Items.Add(a500);

                    int IndexofA7 = line.IndexOf("*");
                    string Ru7 = line.Substring(IndexofA7);
                    string a700 = Ru7.Substring(0, Ru7.IndexOf("\n"));
                    //serverbaselist.Items.Add(a500);
                    line = line.Replace(a700, "");


                    try
                    {
                        Directory.CreateDirectory(serverbase);

                        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json"))
                        {
                            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", string.Format("{{{0}}}", "\n"));
                        }
                        JObject _json = new JObject
                        {
                            { "name", servername },
                            { "java", serverjava },
                            { "base", serverbase },
                            { "core", servercore },
                            { "memory", servermemory },
                            { "args", serverargs }
                        };
                        JObject _jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                        List<string> keys = _jsonObject.Properties().Select(p => p.Name).ToList();
                        keys.Sort();
                        int i = (keys.Count > 0) ? int.Parse(keys.Last()) + 1 : 0;
                        _jsonObject.Add(i.ToString(), _json);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(_jsonObject), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "MSL\\ServerList.ini");
                Growl.Success("已成功将旧版服务器存储文件转换为新版文件！旧版存储文件已删除");
            }
            */


            //获取电脑内存
            PhisicalMemory = GetPhisicalMemory();
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
                MessageBox.Show("Err" + ex.Message);
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
                MessageBox.Show("Err" + ex.Message);
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
                            if (dialog== true)
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
            if (MainNoticyIcon.Visibility==Visibility.Hidden)
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
        void ChangeTitleStyle()
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
