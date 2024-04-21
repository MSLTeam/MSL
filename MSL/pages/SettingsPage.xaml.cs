using HandyControl.Controls;
using HandyControl.Themes;
using Microsoft.Win32;
using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        public static event DeleControl C_NotifyIcon;
        public static event DeleControl ChangeSkinStyle;
        private string _autoStartList = "";
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jsonObject["notifyIcon"] != null && (bool)jsonObject["notifyIcon"] == true)
                {
                    notifyIconbtn.Content = "托盘图标:打开";
                }
                if (jsonObject["autoRunApp"] != null && (bool)jsonObject["autoRunApp"] == true)
                {
                    autoRunApp.IsChecked = true;
                }
                if (jsonObject["autoUpdateApp"] != null && (bool)jsonObject["autoUpdateApp"] == true)
                {
                    autoUpdateApp.IsChecked = true;
                }
                if (jsonObject["autoOpenServer"] != null && jsonObject["autoOpenServer"].ToString() != "False")
                {
                    openserversOnStart.IsChecked = true;
                    _autoStartList = jsonObject["autoOpenServer"].ToString();
                }
                if (jsonObject["autoOpenFrpc"] != null && (bool)jsonObject["autoOpenFrpc"] == true)
                {
                    openfrpOnStart.IsChecked = true;
                }
                if (jsonObject["autoGetPlayerInfo"] != null && (bool)jsonObject["autoGetPlayerInfo"] == true)
                {
                    autoGetPlayerInfo.IsChecked = true;
                }
                if (jsonObject["autoGetServerInfo"] != null && (bool)jsonObject["autoGetServerInfo"] == true)
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
                if (MainWindow.SoftTag == "i18n")
                {
                    if (jsonObject["lang"] != null)
                    {
                        int langCombo = 0;
                        switch (jsonObject["lang"].ToString())
                        {
                            case "zh-CN":
                                langCombo = 0;
                                break;
                            case "en-US":
                                langCombo = 1;
                                break;
                        }
                        ChangeLanguage.SelectedIndex = langCombo;
                    }

                }
                else
                {
                    ChangeLanguage.Visibility = Visibility.Collapsed;
                    ChangeLanguageBtn.Visibility = Visibility.Visible;
                }

                if (jsonObject["skin"] != null)
                {
                    if ((int)jsonObject["skin"] == 0)
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
                        switch ((int)jsonObject["skin"])
                        {
                            case 1:
                                autoSetTheme.IsChecked = false;
                                BlueSkinBtn.IsChecked = true;
                                break;
                            case 2:
                                autoSetTheme.IsChecked = false;
                                RedSkinBtn.IsChecked = true;
                                break;
                            case 3:
                                autoSetTheme.IsChecked = false;
                                GreenSkinBtn.IsChecked = true;
                                break;
                            case 4:
                                autoSetTheme.IsChecked = false;
                                OrangeSkinBtn.IsChecked = true;
                                break;
                            case 5:
                                autoSetTheme.IsChecked = false;
                                PurpleSkinBtn.IsChecked = true;
                                break;
                            case 6:
                                autoSetTheme.IsChecked = false;
                                PinkSkinBtn.IsChecked = true;
                                break;
                        }
                    }
                }
                if (jsonObject["semitransparentTitle"] != null && (bool)jsonObject["semitransparentTitle"] == true)
                {
                    semitransparentTitle.IsChecked = true;
                }
                //serverListBox.Items.Clear();
                ServersList.Items.Clear();
                AutoStartServers.Items.Clear();
                try
                {
                    if (File.Exists(@"MSL\ServerList.json"))
                    {
                        JObject _json = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                        foreach (var item in _json)
                        {
                            var items = _autoStartList.Split(',');
                            if (items.Contains(item.Key))
                            {
                                AutoStartServers.Items.Add(string.Format("[{0}]{1}", item.Key, item.Value["name"]));
                                continue;
                            }
                            ServersList.Items.Add(string.Format("[{0}]{1}", item.Key, item.Value["name"]));
                            //serverListBox.Items.Add(item.Value["name"]);
                            //_runServerList.Add(item.Key);
                            //serverListBox.SelectedIndex = 0;
                        }
                    }
                }
                catch { return; }
            }
            catch
            {
                Growl.Error("加载配置时发生错误！此错误不影响使用，您可继续使用或将其反馈给作者！");
            }
        }

        private void mulitDownthread_Click(object sender, RoutedEventArgs e)
        {
            DownloadDialog.downloadthread = int.Parse(downthreadCount.Text);
        }

        private async void setdefault_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "恢复默认设置会清除MSL文件夹内的所有文件，请您谨慎选择！", "警告", true);
            if (dialogRet)
            {
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
                    jobject["notifyIcon"] = false;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Growl.Success("关闭成功！");
                    return;
                }
                catch
                {
                    Growl.Error("关闭失败！");
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
                    jobject["notifyIcon"] = true;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Growl.Success("打开成功！");
                    return;
                }
                catch
                {
                    Growl.Error("打开失败！");
                    return;
                }
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
                if (_autoStartList == "")
                {
                    Growl.Error("请先将服务器添加至启动列表！");
                    openserversOnStart.IsChecked = false;
                    return;
                }
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = _autoStartList;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
            }
        }

        private void openfrpOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openfrpOnStart.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
            }
        }

        private void ChangeSkin(object sender, RoutedEventArgs e)
        {
            if (BlueSkinBtn.IsChecked == true)
            {
                BrushConverter brushConverter = new BrushConverter();
                ThemeManager.Current.AccentColor = (Brush)brushConverter.ConvertFromString("#0078D4");
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 1;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (RedSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Red;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 2;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (GreenSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Green;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 3;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (OrangeSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Orange;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 4;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (PurpleSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.Purple;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 5;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
            }
            else if (PinkSkinBtn.IsChecked == true)
            {
                ThemeManager.Current.AccentColor = Brushes.DeepPink;
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = 6;
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
                jobject["autoGetPlayerInfo"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
                MainWindow.getPlayerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetPlayerInfo"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
                MainWindow.getPlayerInfo = false;
            }
        }

        private void autoGetServerInfo_Click(object sender, RoutedEventArgs e)
        {
            if (autoGetServerInfo.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
                MainWindow.getServerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
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
                jobject["skin"] = 0;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);

                Growl.Success("开启成功！");
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
                jobject["skin"] = 1;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);

                Growl.Success("关闭成功！");
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
                Growl.Success("开启成功！");
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("关闭成功！");
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
        }
        private void semitransparentTitle_Click(object sender, RoutedEventArgs e)
        {
            if (semitransparentTitle.IsChecked == true)
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["semitransparentTitle"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("开启成功！");
                ChangeSkinStyle();
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["semitransparentTitle"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("关闭成功！");
                ChangeSkinStyle();
            }
        }

        private async void paintedEgg_Click(object sender, RoutedEventArgs e)
        {
            bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "点击此按钮后软件出现任何问题作者概不负责，你确定要继续吗？\n（光敏性癫痫警告！若您患有光敏性癫痫，请不要点击确定！）", "警告", true, "取消");
            if (dialog)
            {
                // 定义一个颜色数组
                Color[] colors = new Color[] { Colors.DeepSkyBlue, Colors.Pink, Colors.Orange, Colors.SpringGreen, Colors.Turquoise, Colors.DeepSkyBlue, Colors.Pink, Colors.HotPink, Colors.DeepSkyBlue, };

                // 创建一个颜色关键帧动画
                ColorAnimationUsingKeyFrames colorAnimation = new ColorAnimationUsingKeyFrames
                {
                    Duration = TimeSpan.FromSeconds(15.0), // 总动画时间
                    RepeatBehavior = RepeatBehavior.Forever // 无限循环
                };

                // 为每个颜色添加关键帧
                for (int i = 0; i < colors.Length; i++)
                {
                    // 每个颜色持续时间为总时间除以颜色数量
                    LinearColorKeyFrame keyFrame = new LinearColorKeyFrame(colors[i], KeyTime.FromTimeSpan(TimeSpan.FromSeconds(i * (15.0 / colors.Length))));
                    colorAnimation.KeyFrames.Add(keyFrame);
                }

                // 应用动画到背景色和AccentColor
                SolidColorBrush brush = new SolidColorBrush(colors[0]);

                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                //window.Background = brush;
                Application.Current.Resources["BackgroundBrush"] = brush;
                //ThemeManager.Current.AccentColor = brush;
                if (File.Exists("MSL\\Background.png"))
                {
                    File.Copy("MSL\\Background.png", "MSL\\Background_.png", true);
                    File.Delete("MSL\\Background.png");
                    ChangeSkinStyle();
                }
            }
        }

        private void changeBackImg_Click(object sender, RoutedEventArgs e)
        {
            var mainwindow = Window.GetWindow(Window.GetWindow(this));
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
                    Shows.ShowMsgDialog(Window.GetWindow(this), "更换背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
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
                Shows.ShowMsgDialog(Window.GetWindow(this), "清除背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
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
                jobject["autoRunApp"] = true;
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
                jobject["autoRunApp"] = false;
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
                jobject["autoUpdateApp"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoUpdateApp"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
            }
        }

        private void checkUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            //更新
            try
            {
                var mainwindow = Window.GetWindow(Window.GetWindow(this));
                string _httpReturn = Functions.Get("query/update");
                Version newVersion = new Version(_httpReturn);
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    var updatelog = Functions.Get("query/update/log");
                    Dispatcher.Invoke(async () =>
                    {
                        bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "发现新版本，版本号为：" + _httpReturn + "，是否进行更新？\n更新日志：\n" + updatelog, "更新", true, "取消");
                        if (dialog == true)
                        {
                            if (MainWindow.ProcessRunningCheck())
                            {
                                Shows.ShowMsgDialog(Window.GetWindow(this), "您的服务器/内网映射/点对点联机正在运行中，若此时更新，会造成后台残留，请将前者关闭后再进行更新！", "警告");
                                return;
                            }
                            string downloadUrl = Functions.Get("download/update?type=" + MainWindow.SoftTag);
                            await Shows.ShowDownloader(Window.GetWindow(this), downloadUrl, AppDomain.CurrentDomain.BaseDirectory, "MSL" + _httpReturn + ".exe", "下载新版本中……");
                            if (File.Exists("MSL" + _httpReturn + ".exe"))
                            {
                                string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                                string dwnExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MSL" + _httpReturn + ".exe");
                                string newExeDir = AppDomain.CurrentDomain.BaseDirectory;

                                // 输出CMD命令以便调试
                                string cmdCommand = "/C choice /C Y /N /D Y /T 1 & Del \"" + oldExePath + "\" & Ren \"" + "MSL" + _httpReturn + ".exe" + "\" \"MSL.exe\" & start \"\" \"MSL.exe\"";
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
                    });
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
        }

        private void AutoStartServers_ItemsChanged()
        {
            _autoStartList = "";
            foreach (var item in AutoStartServers.Items)
            {
                _autoStartList += item.ToString().Substring(1, 1) + ",";
            }
        }

        private void TransferOut_Click(object sender, RoutedEventArgs e)
        {
            List<object> list = new List<object>();
            foreach (var item in AutoStartServers.SelectedItems)
            {
                ServersList.Items.Add(item);
                list.Add(item);
            }
            foreach (var item in list)
            {
                AutoStartServers.Items.Remove(item);
            }
            list.Clear();
            AutoStartServers_ItemsChanged();
        }

        private void TransferIn_Click(object sender, RoutedEventArgs e)
        {
            List<object> list = new List<object>();
            foreach (var item in ServersList.SelectedItems)
            {
                AutoStartServers.Items.Add(item);
                list.Add(item);
            }
            foreach (var item in list)
            {
                ServersList.Items.Remove(item);
            }
            list.Clear();
            AutoStartServers_ItemsChanged();
        }

        private void ChangeLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string lang = "";
            switch (ChangeLanguage.SelectedIndex)
            {
                case 0:
                    lang = "zh-CN";
                    break;
                case 1:
                    lang = "en-US";
                    break;
            }
            LanguageManager.Instance.ChangeLanguage(new CultureInfo(lang));
            JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
            jobject["lang"] = lang;
            string convertString = Convert.ToString(jobject);
            File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
        }

        private async void ChangeLanguageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string _httpReturn = Functions.Get("query/update");
                bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "当前版本不支持多语言！\n是否升级到MSL多语言版本？\n警告：部分系统可能不支持允许多语言版本，若升级后您无法运行MSL，请自行下载正常版本MSL！", "升级到多语言版本？", true, "取消");
                if (dialog == true)
                {
                    if (MainWindow.ProcessRunningCheck())
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "您的服务器/内网映射/点对点联机正在运行中，若此时更新，会造成后台残留，请将前者关闭后再进行更新！", "警告");
                        return;
                    }
                    string downloadUrl = Functions.Get("download/update?type=i18n");
                    await Shows.ShowDownloader(Window.GetWindow(this), downloadUrl, AppDomain.CurrentDomain.BaseDirectory, "MSL" + _httpReturn + ".exe", "下载多语言版本中……");
                    if (File.Exists("MSL" + _httpReturn + ".exe"))
                    {
                        string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                        string dwnExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MSL" + _httpReturn + ".exe");
                        string newExeDir = AppDomain.CurrentDomain.BaseDirectory;

                        // 输出CMD命令以便调试
                        string cmdCommand = "/C choice /C Y /N /D Y /T 1 & Del \"" + oldExePath + "\" & Ren \"" + "MSL" + _httpReturn + ".exe" + "\" \"MSL.exe\" & start \"\" \"MSL.exe\"";
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
                        MessageBox.Show("升级失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "升级失败！\n错误：" + ex.Message, LanguageManager.Instance["Dialog_Err"]);
            }
            
            
        }
    }
}