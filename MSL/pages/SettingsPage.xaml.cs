using HandyControl.Controls;
using HandyControl.Themes;
using Microsoft.Win32;
using MSL.controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using MessageWindow = MSL.controls.MessageWindow;

namespace MSL.pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        public static event DeleControl C_NotifyIcon;
        public static event DeleControl ChangeSkinStyle;
        List<string> _runServerList = new List<string>();
        public SettingsPage()
        {
            InitializeComponent();
        }
        private void mulitDownthread_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow.downloadthread = int.Parse(downthreadCount.Text);
        }
        private void setdefault_Click(object sender, RoutedEventArgs e)
        {
            var mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
            Shows.ShowMsg(mainwindow, "恢复默认设置会清除MSL文件夹内的所有文件，请您谨慎选择！", "警告", true, "取消");
            if (MessageWindow._dialogReturn)
            {
                MessageWindow._dialogReturn = false;
                try
                {
                    Directory.Delete(@"MSL", true);
                }
                catch
                {
                }
                Process.Start(Application.ResourceAssembly.Location);
                Process.GetCurrentProcess().Kill();
            }
        }
        private void notifyIconbtn_Click(object sender, RoutedEventArgs e)
        {
            if (notifyIconbtn.Content.ToString() == "托盘图标:打开")
            {
                notifyIconbtn.Content = "托盘图标:关闭";
                C_NotifyIcon();
                try
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = "False";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Shows.GrowlSuccess("关闭成功！");
                    return;
                }
                catch
                {
                    Shows.GrowlErr("关闭失败！");
                    return;
                }
            }
            else
            {
                notifyIconbtn.Content = "托盘图标:打开";
                C_NotifyIcon();
                try
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = "True";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Shows.GrowlSuccess("打开成功！");
                    return;
                }
                catch
                {
                    Shows.GrowlErr("打开失败！");
                    return;
                }
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jsonObject["notifyIcon"] != null && jsonObject["notifyIcon"].ToString() == "True")
                {
                    notifyIconbtn.Content = "托盘图标:打开";
                }
                if (jsonObject["autoRunApp"] != null && jsonObject["autoRunApp"].ToString() == "True")
                {
                    autoRunApp.IsChecked = true;
                }
                if (jsonObject["autoUpdateApp"] != null && jsonObject["autoUpdateApp"].ToString() == "True")
                {
                    autoUpdateApp.IsChecked = true;
                }
                if (jsonObject["autoOpenServer"] != null && jsonObject["autoOpenServer"].ToString() != "False")
                {
                    openserversOnStart.IsChecked = true;
                    openserversOnStartList.Text = jsonObject["autoOpenServer"].ToString();
                }
                if (jsonObject["autoOpenFrpc"] != null && jsonObject["autoOpenFrpc"].ToString() == "True")
                {
                    openfrpOnStart.IsChecked = true;
                }
                if (jsonObject["autoGetPlayerInfo"] != null && jsonObject["autoGetPlayerInfo"].ToString() == "True")
                {
                    autoGetPlayerInfo.IsChecked = true;
                }
                if (jsonObject["autoGetServerInfo"] != null && jsonObject["autoGetServerInfo"].ToString() == "True")
                {
                    autoGetServerInfo.IsChecked = true;
                }
                if (jsonObject["darkTheme"] != null && jsonObject["darkTheme"].ToString() == "True")
                {
                    autoSetTheme.IsChecked = false;
                    darkTheme.IsChecked = true;
                    darkTheme.IsEnabled = true;
                }
                else if (jsonObject["darkTheme"] != null && jsonObject["darkTheme"].ToString() == "False")
                {
                    autoSetTheme.IsChecked = false;
                    darkTheme.IsEnabled = true;
                }
                if (jsonObject["skin"] != null)
                {
                    if(jsonObject["skin"].ToString() == "0")
                    {
                        autoSetTheme.IsChecked = true;
                        BlueSkinBtn.IsEnabled = false;
                        RedSkinBtn.IsEnabled = false;
                        GreenSkinBtn.IsEnabled = false;
                        OrangeSkinBtn.IsEnabled = false;
                        PurpleSkinBtn.IsEnabled = false;
                        PinkSkinBtn.IsEnabled = false;
                    }
                    else
                    {
                        BlueSkinBtn.IsEnabled = true;
                        RedSkinBtn.IsEnabled = true;
                        GreenSkinBtn.IsEnabled = true;
                        OrangeSkinBtn.IsEnabled = true;
                        PurpleSkinBtn.IsEnabled = true;
                        PinkSkinBtn.IsEnabled = true;
                        switch (jsonObject["skin"].ToString())
                        {
                            case "1":
                                autoSetTheme.IsChecked = false;
                                BlueSkinBtn.IsChecked = true;
                                break;
                            case "2":
                                autoSetTheme.IsChecked = false;
                                RedSkinBtn.IsChecked = true;
                                break;
                            case "3":
                                autoSetTheme.IsChecked = false;
                                GreenSkinBtn.IsChecked = true;
                                break;
                            case "4":
                                autoSetTheme.IsChecked = false;
                                OrangeSkinBtn.IsChecked = true;
                                break;
                            case "5":
                                autoSetTheme.IsChecked = false;
                                PurpleSkinBtn.IsChecked = true;
                                break;
                            case "6":
                                autoSetTheme.IsChecked = false;
                                PinkSkinBtn.IsChecked = true;
                                break;
                        }
                    }
                }
                if (jsonObject["semitransparentTitle"] != null && jsonObject["semitransparentTitle"].ToString() == "True")
                {
                    semitransparentTitle.IsChecked = true;
                }
                serverListBox.Items.Clear();
                try
                {
                    if (File.Exists(@"MSL\ServerList.json"))
                    {
                        JObject _json = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                        foreach (var item in _json)
                        {
                            serverListBox.Items.Add(item.Value["name"]);
                            _runServerList.Add(item.Key);
                            serverListBox.SelectedIndex = 0;
                        }
                    }
                }
                catch { return; }
            }
            catch
            {
                Shows.GrowlErr("加载配置时发生错误！此错误不影响使用，您可继续使用或将其反馈给作者！");
            }
        }

        private void useidea_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/");
        }

        private void openserversOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openserversOnStart.IsChecked == true)
            {
                if (openserversOnStartList.Text == "")
                {
                    Shows.GrowlErr("请先将服务器添加至启动列表！");
                    openserversOnStart.IsChecked = false;
                    return;
                }
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = openserversOnStartList.Text;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
            }
        }

        private void openfrpOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openfrpOnStart.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
            }
        }

        private void ChangeSkin(object sender, RoutedEventArgs e)
        {
            if (BlueSkinBtn.IsChecked == true)
            {
                BrushConverter brushConverter = new BrushConverter();
                ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "1";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (RedSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Red;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "2";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (GreenSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Green;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "3";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (OrangeSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Orange;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "4";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (PurpleSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Purple;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "5";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (PinkSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.DeepPink;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "6";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
        }

        private void autoGetPlayerInfo_Click(object sender, RoutedEventArgs e)
        {
            if (autoGetPlayerInfo.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetPlayerInfo"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
                MainWindow.getPlayerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetPlayerInfo"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
                MainWindow.getPlayerInfo = false;
            }
        }

        private void autoGetServerInfo_Click(object sender, RoutedEventArgs e)
        {
            if (autoGetServerInfo.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
                MainWindow.getServerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
                MainWindow.getServerInfo = false;
            }
        }

        private void autoSetTheme_Click(object sender, RoutedEventArgs e)
        {
            if (autoSetTheme.IsChecked == true)
            {
                //ThemeManager.Current.AccentColor = Brushes.DeepPink;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "Auto";
                jobject["skin"] = "0";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);

                Shows.GrowlSuccess("开启成功！");
                ThemeManager.Current.UsingSystemTheme = true;
                BlueSkinBtn.IsChecked = false;
                darkTheme.IsChecked = false;

                BlueSkinBtn.IsEnabled = false;
                RedSkinBtn.IsEnabled = false;
                GreenSkinBtn.IsEnabled = false;
                OrangeSkinBtn.IsEnabled = false;
                PurpleSkinBtn.IsEnabled = false;
                PinkSkinBtn.IsEnabled = false;
                darkTheme.IsEnabled = false;
            }
            else
            {
                //ThemeManager.Current.AccentColor = Brushes.DeepPink;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "False";
                jobject["skin"] = "1";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);

                Shows.GrowlSuccess("关闭成功！");
                ThemeManager.Current.UsingSystemTheme = false;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                BlueSkinBtn.IsChecked = true;

                BlueSkinBtn.IsEnabled = true;
                RedSkinBtn.IsEnabled = true;
                GreenSkinBtn.IsEnabled = true;
                OrangeSkinBtn.IsEnabled = true;
                PurpleSkinBtn.IsEnabled = true;
                PinkSkinBtn.IsEnabled = true;
                darkTheme.IsEnabled = true;
            }
        }

        private void darkTheme_Click(object sender, RoutedEventArgs e)
        {
            if (darkTheme.IsChecked == true)
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
        }
        private void semitransparentTitle_Click(object sender, RoutedEventArgs e)
        {
            if (semitransparentTitle.IsChecked == true)
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["semitransparentTitle"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
                ChangeSkinStyle();
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["semitransparentTitle"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
                ChangeSkinStyle();
            }
        }

        private void paintedEgg_Click(object sender, RoutedEventArgs e)
        {
            var mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
            bool dialog = Shows.ShowMsg(mainwindow, "点击此按钮后软件出现任何问题作者概不负责，你确定要继续吗？\n（光敏性癫痫警告！若您患有光敏性癫痫，请不要点击确定！）", "警告", true, "取消");
            if (dialog)
            {
                ThemeManager.Current.UsingSystemTheme = false;
                Thread thread = new Thread(PaintedEgg);
                thread.Start();
            }
        }
        void PaintedEgg()
        {
            System.Windows.Window mainwindow = null;
            Dispatcher.Invoke(() =>
            {
                mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
            });
            while (true)
            {
                Dispatcher.Invoke(() =>
                {
                    mainwindow.Background = Brushes.LightBlue;
                    BrushConverter brushConverter = new BrushConverter();
                    ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                });
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    ThemeManager.Current.AccentColor = Brushes.Red;
                });
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    mainwindow.Background = Brushes.LightGreen;
                    ThemeManager.Current.AccentColor = Brushes.Green;
                });
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    mainwindow.Background = Brushes.LightYellow;
                    ThemeManager.Current.AccentColor = Brushes.Orange;
                });
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    ThemeManager.Current.AccentColor = Brushes.Purple;
                });
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    mainwindow.Background = Brushes.LightPink;
                    ThemeManager.Current.AccentColor = Brushes.DeepPink;
                });
                Thread.Sleep(200);
            }
        }

        private void changeBackImg_Click(object sender, RoutedEventArgs e)
        {
            var mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件";
            openfile.Filter = "所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    if (openfile.FileName != "MSL\\Background.png")
                    {
                        File.Copy(openfile.FileName, "MSL\\Background.png", true);
                        ChangeSkinStyle();
                    }
                }
                catch (Exception ex)
                {
                    Shows.ShowMsg(mainwindow, "更换背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
                }
            }
        }
        public static BitmapImage GetImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            if (File.Exists(imagePath))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                using (Stream ms = new MemoryStream(File.ReadAllBytes(imagePath)))
                {
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
            }
            return bitmap;
        }

        private void delBackImg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Delete("MSL\\Background.png");
                ChangeSkinStyle();
            }
            catch (Exception ex)
            {
                Shows.ShowMsg((MainWindow)System.Windows.Window.GetWindow(this), "清除背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
            }
        }

        private void addServerToRunlist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                openserversOnStartList.Text += _runServerList[serverListBox.SelectedIndex] + ",";
            }
            catch
            {
                Shows.GrowlErr("出现错误，您是否选择了一个服务器？");
            }
        }

        private void autoRunApp_Click(object sender, RoutedEventArgs e)
        {
            if (autoRunApp.IsChecked == true)
            {
                // 获取当前用户的注册表启动项
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // 如果启动项中不存在该应用程序，则添加该应用程序
                if (regKey.GetValue("Minecraft Server Launcher") == null)
                {
                    regKey.SetValue("Minecraft Server Launcher", System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoRunApp"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
            }
            else
            {
                // 获取当前用户的注册表启动项
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // 如果启动项中存在该应用程序，则删除该应用程序
                if (regKey.GetValue("Minecraft Server Launcher") != null)
                {
                    regKey.DeleteValue("Minecraft Server Launcher");
                }
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoRunApp"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
            }
        }
        private void autoUpdateApp_Click(object sender, RoutedEventArgs e)
        {
            if (autoUpdateApp.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoUpdateApp"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoUpdateApp"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Shows.GrowlSuccess("关闭成功！");
            }
        }

        private void checkUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            //更新
            try
            {
                var mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
                string aaa = Functions.Get("query/update");
                if (aaa.Contains("v"))
                {
                    aaa = aaa.Replace("v", "");
                }
                Version newVersion = new Version(aaa);
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    var updatelog = Functions.Get("query/update/log");
                    Dispatcher.Invoke(() =>
                    {
                        bool dialog = Shows.ShowMsg(mainwindow, "发现新版本，版本号为：" + aaa + "，是否进行更新？\n更新日志：\n" + updatelog, "更新", true, "取消");
                        if (dialog == true)
                        {
                            string aaa1 = Functions.Get("download/update");
                            Shows.ShowDownloader(mainwindow, aaa1, AppDomain.CurrentDomain.BaseDirectory, "MSL" + aaa + ".exe", "下载新版本中……");
                            if (File.Exists("MSL" + aaa + ".exe"))
                            {
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
                            Shows.GrowlErr("您拒绝了更新新版本，若在此版本中遇到bug，请勿报告给作者！");
                        }
                    });
                }
                else if (newVersion < version)
                {
                    Shows.GrowlInfo("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！");
                }
                else
                {
                    Shows.GrowlSuccess("您使用的开服器已是最新版本！");
                }
            }
            catch
            {
                Shows.GrowlErr("检查更新失败！");
            }
        }
    }
}