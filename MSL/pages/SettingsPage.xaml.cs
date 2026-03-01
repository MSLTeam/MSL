using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using Microsoft.Win32;
using MSL.controls.dialogs;
using MSL.langs;
using MSL.utils;
using MSL.utils.Config;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        public static event App.DeleControl C_NotifyIcon;
        public static event App.DeleControl ChangeSkinStyle;

        private string _autoStartList = "";

        // 当前配置单例
        private static AppConfig Cfg => AppConfig.Current;

        public SettingsPage()
        {
            InitializeComponent();
        }

        // 页面加载
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DCID.Content = Functions.GetDeviceID();

                notifyIconbtn.IsChecked = Cfg.NotifyIcon;
                MSLTips.IsChecked = Cfg.MSLTips;
                autoRunApp.IsChecked = Cfg.AutoRunApp;
                autoUpdateApp.IsChecked = Cfg.AutoUpdateApp;
                CloseWindowDialog.IsChecked = Cfg.CloseWindowDialog;

                // 自动开启服务器
                if (Cfg.AutoOpenServer != "False")
                {
                    openserversOnStart.IsChecked = true;
                    _autoStartList = Cfg.AutoOpenServer;
                }

                // 自动开启 Frpc
                if (Cfg.AutoOpenFrpc != "False")
                {
                    openfrpOnStart.IsChecked = true;
                    AutoOpenFrpcList.Text = Cfg.AutoOpenFrpc;
                    AutoOpenFrpcList.IsEnabled = false;
                }

                autoGetPlayerInfo.IsChecked = Cfg.AutoGetPlayerInfo;
                autoGetServerInfo.IsChecked = Cfg.AutoGetServerInfo;

                // 主题设置
                switch (Cfg.DarkTheme)
                {
                    case "True":
                        autoSetTheme.IsChecked = false;
                        ChangeSkinColor.IsEnabled = true;
                        darkTheme.IsChecked = true;
                        darkTheme.IsEnabled = true;
                        break;
                    case "False":
                        autoSetTheme.IsChecked = false;
                        ChangeSkinColor.IsEnabled = true;
                        darkTheme.IsEnabled = true;
                        break;
                        // "Auto": 保持默认（autoSetTheme = true）
                }

                // 语言
                ChangeLanguage.SelectedIndex = Cfg.Lang == "en-US" ? 1 : 0;

                // 半透明标题
                semitransparentTitle.IsChecked = Cfg.SemitransparentTitle;

                // 云母效果
                if (Cfg.MicaEffect)
                {
                    UseMicaEffect.IsChecked = true;
                    semitransparentTitle.IsEnabled = false;
                    autoSetTheme.IsEnabled = false;
                    ChangeSkinColor.IsEnabled = false;
                    darkTheme.IsEnabled = false;
                    changeBackImg.Visibility = Visibility.Collapsed;
                    delBackImg.Visibility = Visibility.Collapsed;
                    WesternEgg.Visibility = Visibility.Collapsed;
                }

                // 日志字体
                InitFontComboBox();
                // 日志字号
                LogFontSizeBox.Text = Cfg.LogFont.Size.ToString();

                // 服务器列表
                LoadServerList();
            }
            catch
            {
                Growl.Error("加载配置时发生错误！此错误不影响使用，您可继续使用或将其反馈给作者！");
            }
        }

        // 加载服务器列表
        private void LoadServerList()
        {
            ServersList.Items.Clear();
            AutoStartServers.Items.Clear();
            try
            {
                if (!File.Exists(@"MSL\ServerList.json")) return;
                var autoStartIds = _autoStartList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                foreach (var item in ServerConfig.Current.All)
                {
                    string entry = $"[{item.Key}]{item.Value.Name}";
                    if (autoStartIds.Contains(item.Key))
                        AutoStartServers.Items.Add(entry);
                    else
                        ServersList.Items.Add(entry);
                }
            }
            catch { }
        }

        private void InitFontComboBox()
        {
            LogFontCombo.ItemsSource = null;
            LogFontCombo.Items.Clear();
            // 获取系统所有字体，按名称排序
            var fonts = Fonts.SystemFontFamilies
                             .OrderBy(f => f.Source)
                             .ToList();
            LogFontCombo.ItemsSource = fonts;

            var ft = Cfg.LogFont.Family;
            if (ft == null)
                return;

            LogFontCombo.SelectedItem = fonts.FirstOrDefault(f => f.Source == Cfg.LogFont.Family);
        }

        // 基础功能
        private void mulitDownthread_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(downthreadCount.Text, out int count))
            {
                ConfigStore.DownloadChunkCount = count;
                Cfg.DownloadChunkCount = count;
                Cfg.Save();
            }
        }

        private async void AddDownloadTask_Click(object sender, RoutedEventArgs e)
        {
            string url = DownloadUrl.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                MagicShow.ShowMsgDialog("请输入地址后再进行下载！", "提示");
                return;
            }
            string filename = await HttpService.GetRemoteFileNameAsync(url);
            if (!await MagicShow.ShowMsgDialogAsync(
                $"URL: {url}\n文件名称: {filename}\n文件将保存至 MSL\\Downloads 文件夹内！\n\n点击确定以下载", "信息", true))
                return;

            var dwnManager = DownloadManager.Instance;
            string groupid = dwnManager.CreateDownloadGroup(isTempGroup: true);
            dwnManager.AddDownloadItem(groupid, url, Path.Combine("MSL", "Downloads"), filename);
            dwnManager.StartDownloadGroup(groupid);
            DownloadManagerDialog.Instance.ManagerControl.AddDownloadGroup(groupid, true);

            MagicFlowMsg.ShowMessage("已将其添加至任务列表中！");
        }

        private void OpenDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            var token = Guid.NewGuid().ToString();
            Dialog.SetToken(Window.GetWindow(this), token);
            DownloadManagerDialog.Instance.LoadDialog(token, true);
            Dialog.Show(DownloadManagerDialog.Instance, token);
        }

        private async void setdefault_Click(object sender, RoutedEventArgs e)
        {
            if (!await MagicShow.ShowMsgDialogAsync(
                "恢复默认设置会清除MSL文件夹内的所有文件，请您谨慎选择！", "警告", true, isDangerPrimaryBtn: true))
                return;
            try { Directory.Delete(@"MSL", true); } catch { }
            Process.Start(Application.ResourceAssembly.Location);
            Process.GetCurrentProcess().Kill();
        }

        // 托盘图标
        private void notifyIconbtn_Click(object sender, RoutedEventArgs e)
        {
            C_NotifyIcon();
            Cfg.NotifyIcon = notifyIconbtn.IsChecked == true;
            Cfg.Save();
            MagicFlowMsg.ShowMessage(Cfg.NotifyIcon ? "开启成功！" : "关闭成功！", Cfg.NotifyIcon ? 1 : 2);
        }

        // 自动开启服务器
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
                Cfg.AutoOpenServer = _autoStartList;
            }
            else
            {
                Cfg.AutoOpenServer = "False";
            }
            Cfg.Save();
            MagicFlowMsg.ShowMessage(openserversOnStart.IsChecked == true ? "开启成功！" : "关闭成功！", 1);
        }

        // 自动开启 Frpc
        private void openfrpOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openfrpOnStart.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(AutoOpenFrpcList.Text))
                {
                    Growl.Error("请先将需要自启动的Frpc之ID填入框中！");
                    openfrpOnStart.IsChecked = false;
                    return;
                }
                Cfg.AutoOpenFrpc = AutoOpenFrpcList.Text;
                AutoOpenFrpcList.IsEnabled = false;
            }
            else
            {
                Cfg.AutoOpenFrpc = "False";
                AutoOpenFrpcList.IsEnabled = true;
            }
            Cfg.Save();
            MagicFlowMsg.ShowMessage(openfrpOnStart.IsChecked == true ? "开启成功！" : "关闭成功！", 1);
        }

        // 玩家信息&服务器信息
        private void autoGetPlayerInfo_Click(object sender, RoutedEventArgs e)
        {
            Cfg.AutoGetPlayerInfo = autoGetPlayerInfo.IsChecked == true;
            Cfg.Save();
            ConfigStore.GetPlayerInfo = Cfg.AutoGetPlayerInfo;
            MagicFlowMsg.ShowMessage(Cfg.AutoGetPlayerInfo ? "开启成功！" : "关闭成功！", 1);
        }

        private void autoGetServerInfo_Click(object sender, RoutedEventArgs e)
        {
            Cfg.AutoGetServerInfo = autoGetServerInfo.IsChecked == true;
            Cfg.Save();
            ConfigStore.GetServerInfo = Cfg.AutoGetServerInfo;
            MagicFlowMsg.ShowMessage(Cfg.AutoGetServerInfo ? "开启成功！" : "关闭成功！", 1);
        }

        // 主题
        private void autoSetTheme_Click(object sender, RoutedEventArgs e)
        {
            if (autoSetTheme.IsChecked == true)
            {
                Cfg.DarkTheme = "Auto";
                Cfg.SkinColor = null;
                Cfg.Save();
                ThemeManager.Current.UsingSystemTheme = true;
                darkTheme.IsChecked = false;
                darkTheme.IsEnabled = false;
                ChangeSkinColor.IsEnabled = false;
                MagicFlowMsg.ShowMessage("开启成功！", 1);
            }
            else
            {
                Cfg.DarkTheme = "False";
                Cfg.SkinColor = "#0078D4";
                Cfg.Save();
                ThemeManager.Current.UsingSystemTheme = false;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                darkTheme.IsEnabled = true;
                ChangeSkinColor.IsEnabled = true;
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
            }
        }

        private void ChangeSkinColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            var tempColor = (SolidColorBrush)ThemeManager.Current.AccentColor;
            picker.SelectedBrush = tempColor;

            var window = new PopupWindow { PopupElement = picker };
            picker.SelectedColorChanged += delegate { ThemeManager.Current.AccentColor = picker.SelectedBrush; };
            picker.Confirmed += delegate
            {
                window.Close();
                Cfg.SkinColor = picker.SelectedBrush.ToString();
                Cfg.Save();
                MagicFlowMsg.ShowMessage("保存颜色成功！", 1);
            };
            picker.Canceled += delegate { window.Close(); ThemeManager.Current.AccentColor = tempColor; };
            window.Show(ChangeSkinColor, false);
        }

        private void darkTheme_Click(object sender, RoutedEventArgs e)
        {
            if (darkTheme.IsChecked == true)
            {
                Cfg.DarkTheme = "True";
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                LogHelper.Write.Info("[Settings] 已切换至暗色模式！");
            }
            else
            {
                Cfg.DarkTheme = "False";
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                LogHelper.Write.Info("[Settings] 暗色模式已关闭！");
            }
            Cfg.Save();
            MagicFlowMsg.ShowMessage(darkTheme.IsChecked == true ? "开启成功！" : "关闭成功！", 1);
        }

        private void semitransparentTitle_Click(object sender, RoutedEventArgs e)
        {
            Cfg.SemitransparentTitle = semitransparentTitle.IsChecked == true;
            Cfg.Save();
            ChangeSkinStyle();
            LogHelper.Write.Info($"[Settings] 半透明标题栏功能已{(Cfg.SemitransparentTitle ? "打开" : "关闭")}！");
            MagicFlowMsg.ShowMessage(Cfg.SemitransparentTitle ? "开启成功！" : "关闭成功！", 1);
        }

        // 云母效果
        private async void UseMicaEffect_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = UseMicaEffect.IsChecked == true;
            Cfg.MicaEffect = enabled;
            if (enabled)
            {
                Cfg.DarkTheme = "Auto";
                semitransparentTitle.IsEnabled = false;
                autoSetTheme.IsEnabled = false;
                ChangeSkinColor.IsEnabled = false;
                changeBackImg.Visibility = Visibility.Collapsed;
                delBackImg.Visibility = Visibility.Collapsed;
                WesternEgg.Visibility = Visibility.Collapsed;
                MagicFlowMsg.ShowMessage("已开启Mica效果！", 1);
            }
            else
            {
                semitransparentTitle.IsEnabled = true;
                autoSetTheme.IsEnabled = true;
                if (autoSetTheme.IsChecked == false)
                    ChangeSkinColor.IsEnabled = true;
                changeBackImg.Visibility = Visibility.Visible;
                delBackImg.Visibility = Visibility.Visible;
                WesternEgg.Visibility = Visibility.Visible;
                MagicFlowMsg.ShowMessage("已关闭Mica效果！", 1);
            }
            Cfg.Save();
            autoSetTheme.IsChecked = true;
            darkTheme.IsChecked = false;
            darkTheme.IsEnabled = false;
            await Task.Delay(500);
            ChangeSkinStyle();
        }

        // 背景图
        private void changeBackImg_Click(object sender, RoutedEventArgs e)
        {
            var openfile = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Title = "请选择文件",
                Filter = "所有文件类型|*.*"
            };
            if (openfile.ShowDialog() != true) return;
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
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "更换背景图片失败！\n错误代码：" + ex.Message, "错误");
            }
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
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "清除背景图片失败！\n错误代码：" + ex.Message, "错误");
            }
        }

        // 日志颜色
        private void ChangeLogForeColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            picker.SelectedBrush = new SolidColorBrush(GetCurrentLogBrush());

            var window = new PopupWindow { PopupElement = picker };
            bool confirmed = false;
            picker.Confirmed += delegate { confirmed = true; window.Close(); };
            picker.Canceled += delegate { window.Close(); };
            window.ShowDialog(ChangeLogForeColor, false);

            if (!confirmed) return;

            SetCurrentLogBrush(picker.SelectedBrush.Color);
            SaveLogColors();
            MagicFlowMsg.ShowMessage("保存日志颜色成功！重新打开服务器运行窗口以使其生效！", 1);
        }

        private void RestoreLogForeColor_Click(object sender, RoutedEventArgs e)
        {
            ConfigStore.LogColor.INFO = Colors.Green;
            ConfigStore.LogColor.WARN = Colors.Orange;
            ConfigStore.LogColor.ERROR = Colors.Red;
            ConfigStore.LogColor.HIGHLIGHT = Colors.DeepSkyBlue;

            Cfg.LogColor = new AppConfig.LogColorConfig();  // 重置为默认值
            Cfg.Save();
            MagicFlowMsg.ShowMessage("已恢复默认日志颜色！重新打开服务器运行窗口以使其生效！", 1);
        }

        private Color GetCurrentLogBrush()
        {
            return LogForeTypeCombo.SelectedIndex switch
            {
                0 => ConfigStore.LogColor.INFO,
                1 => ConfigStore.LogColor.WARN,
                2 => ConfigStore.LogColor.ERROR,
                3 => ConfigStore.LogColor.HIGHLIGHT,
                _ => ConfigStore.LogColor.INFO
            };
        }

        private void SetCurrentLogBrush(Color brush)
        {
            switch (LogForeTypeCombo.SelectedIndex)
            {
                case 0: ConfigStore.LogColor.INFO = brush; break;
                case 1: ConfigStore.LogColor.WARN = brush; break;
                case 2: ConfigStore.LogColor.ERROR = brush; break;
                case 3: ConfigStore.LogColor.HIGHLIGHT = brush; break;
            }
        }

        private void SaveLogColors()
        {
            Cfg.LogColor = new AppConfig.LogColorConfig
            {
                INFO = ConfigStore.LogColor.INFO.ToString(),
                WARN = ConfigStore.LogColor.WARN.ToString(),
                ERROR = ConfigStore.LogColor.ERROR.ToString(),
                HIGHLIGHT = ConfigStore.LogColor.HIGHLIGHT.ToString(),
            };
            Cfg.Save();
        }

        // 日志字体
        private void ApplyLogFontConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LogFontCombo.Text))
            {
                Cfg.LogFont.Family = ((FontFamily)LogFontCombo.SelectedItem).Source;
            }
            if (int.TryParse(LogFontSizeBox.Text, out int size) && size > 0)
            {
                Cfg.LogFont.Size = size;
            }
            else
            {
                MagicShow.ShowMsgDialog("请输入有效的字体大小！", "错误");
                return;
            }
            Cfg.Save();

            MagicFlowMsg.ShowMessage("保存日志字体成功！重新打开服务器运行窗口以使其生效！", 1);
        }

        // 开机自启
        private void autoRunApp_Click(object sender, RoutedEventArgs e)
        {
            var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (autoRunApp.IsChecked == true)
            {
                regKey?.SetValue("Minecraft Server Launcher",
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                Cfg.AutoRunApp = true;
            }
            else
            {
                if (regKey?.GetValue("Minecraft Server Launcher") != null)
                    regKey.DeleteValue("Minecraft Server Launcher");
                Cfg.AutoRunApp = false;
            }
            Cfg.Save();
        }

        private void autoUpdateApp_Click(object sender, RoutedEventArgs e)
        {
            Cfg.AutoUpdateApp = autoUpdateApp.IsChecked == true;
            Cfg.Save();
            MagicFlowMsg.ShowMessage(Cfg.AutoUpdateApp ? "开启成功！" : "关闭成功！", 1);
        }

        // 手动检查更新
        private async void checkUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var httpReturn = await HttpService.GetApiContentAsync("query/update");
                string latestVersionStr = httpReturn["data"]["latestVersion"].ToString();
                var newVersion = new Version(latestVersionStr);
                var version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    string updateLog = httpReturn["data"]["log"].ToString();
                    bool confirmed = await MagicShow.ShowMsgDialogAsync(
                        Window.GetWindow(this),
                        $"发现新版本：{latestVersionStr}，是否更新？\n更新日志：\n{updateLog}",
                        "更新", true, "取消");

                    if (!confirmed)
                    {
                        Growl.Error("您拒绝了更新新版本，若在此版本中遇到bug，请勿报告给作者！");
                        return;
                    }
                    if (MainWindow.ProcessRunningCheck())
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this),
                            "您的服务器/内网映射/联机正在运行中，请将其关闭后再更新！", "警告");
                        return;
                    }

                    string downloadUrl = (await HttpService.GetApiContentAsync("download/update"))["data"].ToString();
                    await MagicShow.ShowDownloader(Window.GetWindow(this), downloadUrl,
                        AppDomain.CurrentDomain.BaseDirectory, $"MSL{latestVersionStr}.exe", "下载新版本中……");

                    string newExe = $"MSL{latestVersionStr}.exe";
                    if (!File.Exists(newExe))
                    {
                        MessageBox.Show("更新失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                    string cmd = $"/C choice /C Y /N /D Y /T 1 & Del \"{oldExePath}\" & Ren \"{newExe}\" \"MSL.exe\" & start \"\" \"MSL.exe\"";
                    Application.Current.Shutdown();
                    var proc = new Process();
                    proc.StartInfo.FileName = "cmd.exe";
                    proc.StartInfo.Arguments = cmd;
                    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.Start();
                    Process.GetCurrentProcess().Kill();
                }
                else if (newVersion < version)
                {
                    MagicFlowMsg.ShowMessage("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！", 4);
                }
                else
                {
                    MagicFlowMsg.ShowMessage("您使用的开服器已是最新版本！", 1);
                }
            }
            catch
            {
                Growl.Error("检查更新失败！");
            }
        }

        // 自启动列表操作
        private void AutoStartServers_ItemsChanged()
        {
            _autoStartList = string.Join(",",
                AutoStartServers.Items.Cast<string>().Select(s => s.Substring(1, 1)));
            if (_autoStartList != "") _autoStartList += ",";
        }

        private void TransferOut_Click(object sender, RoutedEventArgs e)
        {
            if (openserversOnStart.IsChecked == true)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先关闭开关后再进行调整！", "提示");
                return;
            }
            MoveItems(AutoStartServers, ServersList);
            AutoStartServers_ItemsChanged();
        }

        private void TransferIn_Click(object sender, RoutedEventArgs e)
        {
            if (openserversOnStart.IsChecked == true)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先关闭开关后再进行调整！", "提示");
                return;
            }
            MoveItems(ServersList, AutoStartServers);
            AutoStartServers_ItemsChanged();
        }

        private static void MoveItems(System.Windows.Controls.ListBox from, System.Windows.Controls.ListBox to)
        {
            var selected = from.SelectedItems.Cast<object>().ToList();
            foreach (var item in selected)
            {
                to.Items.Add(item);
                from.Items.Remove(item);
            }
        }

        // 语言
        private void ChangeLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string lang = ChangeLanguage.SelectedIndex == 1 ? "en-US" : "zh-CN";
            LanguageManager.Instance.ChangeLanguage(new CultureInfo(lang));
            Cfg.Lang = lang;
            Cfg.Save();
        }

        // 关闭提示&MSL Tips
        private void CloseWindowDialog_Click(object sender, EventArgs e)
        {
            Cfg.CloseWindowDialog = CloseWindowDialog.IsChecked == true;
            Cfg.Save();
        }

        private async void MSLTips_Click(object sender, RoutedEventArgs e)
        {
            if (MSLTips.IsChecked == false)
            {
                bool confirmed = await MagicShow.ShowMsgDialogAsync(
                    Window.GetWindow(this),
                    "关闭此功能后，读取服务器信息、玩家等功能将会失效，请谨慎选择！", "警告", true);

                if (!confirmed)
                {
                    MSLTips.IsChecked = true;
                    return;
                }
                Cfg.MSLTips = false;
                Cfg.Save();
                MagicFlowMsg.ShowMessage("关闭成功！重启服务器运行界面以生效！", 1);
            }
            else
            {
                Cfg.MSLTips = true;
                Cfg.Save();
                MagicFlowMsg.ShowMessage("开启成功！重启服务器运行界面以生效！", 1);
            }
        }

        // 彩蛋
        private int _isWesternEgg = 0;

        private async Task GuessNumGame()
        {
            var random = new Random();
            int num = random.Next(1, 501);
            string input = await MagicShow.ShowInput(Window.GetWindow(this), "我生成了一个1-500的整数，你能猜对它吗？\n请输入数字（1-500）");
            if (string.IsNullOrEmpty(input)) { MagicFlowMsg.ShowMessage("爱猜不猜，哼！"); return; }
            if (!int.TryParse(input, out int guess)) { MagicFlowMsg.ShowMessage("为什么要胡乱输入！不玩了！", 2); return; }

            while (guess != num)
            {
                string tip = guess > num ? "你猜的数字大了！" : "你猜的数字小了！";
                MagicFlowMsg.ShowMessage(tip, 0);
                string next = await MagicShow.ShowInput(Window.GetWindow(this), tip + "再猜一次吧！\n请输入数字（1-500）");
                if (string.IsNullOrEmpty(next)) { MagicFlowMsg.ShowMessage("爱猜不猜，哼！"); return; }
                if (!int.TryParse(next, out guess)) { MagicFlowMsg.ShowMessage("为什么要胡乱输入！不玩了！", 2); return; }
            }

            MagicFlowMsg.ShowMessage("你真厉害！居然猜对了！", 1);
            bool knows = await MagicShow.ShowMsgDialogAsync($"猜对了：{num}！\n你真厉害！", "恭喜你！", true, "不，我不知道", "我知道了");
            Window.GetWindow(this).Title = knows ? ":)" : ":(";
        }

        private async void WesternEgg_Click(object sender, RoutedEventArgs e)
        {
            if (_isWesternEgg != 0)
            {
                string[] msgs = { "", "你都已经点过了，别再点了！", "你还真是执着呢！", "你真是个执着的家伙！", "好吧，那来玩一个小游戏吧！" };
                int[] levels = { 0, 0, 3, 2, 1 };
                MagicFlowMsg.ShowMessage(msgs[_isWesternEgg], levels[_isWesternEgg]);
                if (_isWesternEgg == 4) await GuessNumGame();
                if (_isWesternEgg < 4) _isWesternEgg++;
                return;
            }

            bool go = await MagicShow.ShowMsgDialogAsync(
                "点击此按钮后软件出现任何问题作者概不负责，你确定要继续吗？\n（光敏性癫痫警告！若您患有光敏性癫痫，请不要点击确定！）",
                "警告", true, isDangerPrimaryBtn: true, closeBtnContext: "我不确定QWQ");
            _isWesternEgg = 1;
            if (!go) return;

            var random = new Random();
            var colorAnimation = new ColorAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(20),
                RepeatBehavior = RepeatBehavior.Forever
            };

            for (int i = 0; i < 7; i++)
            {
                colorAnimation.KeyFrames.Add(new EasingColorKeyFrame(
                    Color.FromRgb((byte)random.Next(20, 240), (byte)random.Next(20, 240), (byte)random.Next(20, 240)),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(i * (20.0 / 7))),
                    new PowerEase { Power = 2, EasingMode = EasingMode.EaseInOut }));
            }
            if (colorAnimation.KeyFrames.Count > 0)
            {
                Color first = ((ColorKeyFrame)colorAnimation.KeyFrames[0]).Value;
                colorAnimation.KeyFrames.Add(new EasingColorKeyFrame(
                    first, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(20)),
                    new PowerEase { Power = 2, EasingMode = EasingMode.EaseInOut }));
            }

            var brush = new SolidColorBrush();
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            Application.Current.Resources["BackgroundBrush"] = brush;

            if (File.Exists("MSL\\Background.png"))
            {
                File.Copy("MSL\\Background.png", "MSL\\Background_.png", true);
                File.Delete("MSL\\Background.png");
                ChangeSkinStyle();
            }
        }

        // 其他
        private void WikiButton_Click(object sender, RoutedEventArgs e) =>
            Process.Start("https://www.mslmc.cn/docs/mc-server/start/");

        private void CopyDCID_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(Functions.GetDeviceID());
                MagicFlowMsg.ShowMessage("设备ID复制成功！", 1);
            }
            catch
            {
                MagicFlowMsg.ShowMessage("复制失败！", 2);
            }
        }
    }
}