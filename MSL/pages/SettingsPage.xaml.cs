using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using Microsoft.Win32;
using MSL.controls.dialogs;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        public static event DeleControl C_NotifyIcon;
        public static event DeleControl ChangeSkinStyle;
        private string _autoStartList = "";

        private Dialog downloadManagerDialog;
        private DownloadManagerDialog downloadManager;

        public SettingsPage()
        {
            InitializeComponent();

            downloadManagerDialog = new Dialog();
            downloadManager = DownloadManagerDialog.Instance;
            downloadManager.Margin = new Thickness(20);
            downloadManager.ManagerControl.AutoRemoveCompletedItems = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //生成设备id
                DCID.Content = Functions.GetDeviceID();
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jsonObject["notifyIcon"] != null && (bool)jsonObject["notifyIcon"] == true)
                {
                    notifyIconbtn.IsChecked = true;
                }
                if (jsonObject["mslTips"] != null && (bool)jsonObject["mslTips"] == false)
                {
                    MSLTips.IsChecked = false;
                }
                if (jsonObject["autoRunApp"] != null && (bool)jsonObject["autoRunApp"] == true)
                {
                    autoRunApp.IsChecked = true;
                }
                if (jsonObject["autoUpdateApp"] != null && (bool)jsonObject["autoUpdateApp"] == true)
                {
                    autoUpdateApp.IsChecked = true;
                }
                if (jsonObject["closeWindowDialog"] != null && (bool)jsonObject["closeWindowDialog"] == false)
                {
                    CloseWindowDialog.IsChecked = false;
                }
                if (jsonObject["autoOpenServer"] != null && jsonObject["autoOpenServer"].ToString() != "False")
                {
                    openserversOnStart.IsChecked = true;
                    _autoStartList = jsonObject["autoOpenServer"].ToString();
                }
                if (jsonObject["autoOpenFrpc"] != null && jsonObject["autoOpenFrpc"].ToString() != "False")
                {
                    openfrpOnStart.IsChecked = true;
                    AutoOpenFrpcList.Text = jsonObject["autoOpenFrpc"].ToString();
                    AutoOpenFrpcList.IsEnabled = false;
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
                    ChangeSkinColor.IsEnabled = true;
                    darkTheme.IsChecked = true;
                    darkTheme.IsEnabled = true;
                }
                else if (jsonObject["darkTheme"] != null && jsonObject["darkTheme"].ToString() == "False")
                {
                    autoSetTheme.IsChecked = false;
                    ChangeSkinColor.IsEnabled = true;
                    darkTheme.IsEnabled = true;
                }
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

                if (jsonObject["semitransparentTitle"] != null && (bool)jsonObject["semitransparentTitle"] == true)
                {
                    semitransparentTitle.IsChecked = true;
                }

                if (jsonObject["MicaEffect"] != null && (bool)jsonObject["MicaEffect"] == true)
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
            ConfigStore.DownloadChunkCount = int.Parse(downthreadCount.Text);
        }

        private async void AddDownloadTask_Click(object sender, RoutedEventArgs e)
        {
            string url = DownloadUrl.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                MagicShow.ShowMsgDialog("请输入地址后再进行下载！", "提示");
                return;
            }
            string filename = await HttpService.GetRemoteFileNameAsync(url); // 获取远程文件名
            if (!await MagicShow.ShowMsgDialogAsync("URL: " + url + "\n文件名称: " + filename + "\n文件将保存至 MSL\\Downloads 文件夹内！\n\n点击确定以下载", "信息", true))
            {
                return;
            }
            var dwnManager = DownloadManager.Instance;
            string groupid = dwnManager.CreateDownloadGroup(isTempGroup: true);
            dwnManager.AddDownloadItem(groupid, url, Path.Combine("MSL", "Downloads"), filename);
            dwnManager.StartDownloadGroup(groupid);
            downloadManager.ManagerControl.AddDownloadGroup(groupid, true);
            MagicFlowMsg.ShowMessage("已将其添加至任务列表中！");
            /*
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "保存目录:" + Path.GetDirectoryName(saveFileDialog.FileName) + "\n文件名:" + Path.GetFileName(saveFileDialog.FileName), "信息");
                await MagicShow.ShowDownloader(Window.GetWindow(this), DownloadUrl.Text, Path.GetDirectoryName(saveFileDialog.FileName), Path.GetFileName(saveFileDialog.FileName), "下载中");
            }
            */
        }

        private void OpenDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            downloadManagerDialog = Dialog.Show(downloadManager);
            downloadManager.fatherDialog = downloadManagerDialog;
        }

        private async void setdefault_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync("恢复默认设置会清除MSL文件夹内的所有文件，请您谨慎选择！", "警告", true, isDangerPrimaryBtn: true);
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
            if (notifyIconbtn.IsChecked == false)
            {
                C_NotifyIcon();
                try
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = false;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    //Growl.Success("关闭成功！");
                    MagicFlowMsg.ShowMessage("关闭成功！", 1);
                    return;
                }
                catch
                {
                    //Growl.Error("关闭失败！");
                    MagicFlowMsg.ShowMessage("关闭失败！", 2);
                    return;
                }
            }
            else
            {
                C_NotifyIcon();
                try
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = true;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    //Growl.Success("开启成功！");
                    MagicFlowMsg.ShowMessage("开启成功！", 1);
                    return;
                }
                catch
                {
                    //Growl.Error("开启失败！");
                    MagicFlowMsg.ShowMessage("开启失败！", 2);
                    return;
                }
            }
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
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = _autoStartList;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
            }
        }

        private void openfrpOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openfrpOnStart.IsChecked == true)
            {
                if (AutoOpenFrpcList.Text == "")
                {
                    Growl.Error("请先将需要自启动的Frpc之ID填入框中！");
                    openfrpOnStart.IsChecked = false;
                    return;
                }
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = AutoOpenFrpcList.Text;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                AutoOpenFrpcList.IsEnabled = false;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                AutoOpenFrpcList.IsEnabled = true;
            }
        }

        private void autoGetPlayerInfo_Click(object sender, RoutedEventArgs e)
        {
            if (autoGetPlayerInfo.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetPlayerInfo"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                ConfigStore.GetPlayerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetPlayerInfo"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                ConfigStore.GetPlayerInfo = false;
            }
        }

        private void autoGetServerInfo_Click(object sender, RoutedEventArgs e)
        {
            if (autoGetServerInfo.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                ConfigStore.GetServerInfo = true;
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoGetServerInfo"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                ConfigStore.GetServerInfo = false;
            }
        }

        private void autoSetTheme_Click(object sender, RoutedEventArgs e)
        {
            if (autoSetTheme.IsChecked == true)
            {
                Config.Write("darkTheme", "Auto");
                Config.Remove("SkinColor");
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                ThemeManager.Current.UsingSystemTheme = true;
                darkTheme.IsChecked = false;
                darkTheme.IsEnabled = false;
                ChangeSkinColor.IsEnabled = false;
            }
            else
            {
                Config.Write("darkTheme", "False");
                Config.Write("SkinColor", "#0078D4");

                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                ThemeManager.Current.UsingSystemTheme = false;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                darkTheme.IsEnabled = true;

                ChangeSkinColor.IsEnabled = true;
            }
        }

        private void ChangeSkinColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            var tempColor = (SolidColorBrush)ThemeManager.Current.AccentColor;
            picker.SelectedBrush = (SolidColorBrush)ThemeManager.Current.AccentColor;
            var window = new PopupWindow
            {
                PopupElement = picker
            };
            picker.SelectedColorChanged += delegate
            {
                ThemeManager.Current.AccentColor = picker.SelectedBrush;
            };
            picker.Confirmed += delegate
            {
                window.Close();
                Config.Write("SkinColor", picker.SelectedBrush.ToString());
                MagicFlowMsg.ShowMessage("保存颜色成功！", 1);
            };
            picker.Canceled += delegate { window.Close(); ThemeManager.Current.AccentColor = tempColor; };
            window.Show(ChangeLogForeColor, false);
        }

        private void darkTheme_Click(object sender, RoutedEventArgs e)
        {
            if (darkTheme.IsChecked == true)
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                LogHelper.Write.Info("[Settings] 已切换至暗色模式！");
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["darkTheme"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                LogHelper.Write.Info("[Settings] 暗色模式已关闭！");
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
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
                LogHelper.Write.Info("[Settings] 半透明标题栏功能已打开！");
                ChangeSkinStyle();
            }
            else
            {
                JObject jobject = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                jobject["semitransparentTitle"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText("MSL\\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
                LogHelper.Write.Info("[Settings] 半透明标题栏功能已关闭！");
                ChangeSkinStyle();
            }
        }

        private int isWesternEgg = 0;

        private async Task GuessNumGame()
        {
            Random random = new Random();
            int num = random.Next(1, 501);
            string inputContent = await MagicShow.ShowInput(Window.GetWindow(this), "我生成了一个不小于1、不大于500的整数，你能猜对它吗？\n请输入数字（1-500）");
            if (string.IsNullOrEmpty(inputContent))
            {
                MagicFlowMsg.ShowMessage("爱猜不猜，哼！");
                return;
            }
            if (!int.TryParse(inputContent, out int inputnum))
            {
                MagicFlowMsg.ShowMessage("为什么要胡乱输入！不玩了！", 2);
                return;
            }
            while (inputnum != num)
            {
                string tips = string.Empty;
                if (inputnum > num)
                {
                    tips = "你猜的数字大了！";
                    MagicFlowMsg.ShowMessage(tips, 0);
                }
                else if (inputnum < num)
                {
                    tips = "你猜的数字小了！";
                    MagicFlowMsg.ShowMessage(tips, 0);
                }
                string _inputContent = await MagicShow.ShowInput(Window.GetWindow(this), tips + "再猜一次吧！\n请输入数字（1-500）");
                if (string.IsNullOrEmpty(_inputContent))
                {
                    MagicFlowMsg.ShowMessage("爱猜不猜，哼！");
                    return;
                }
                if (!int.TryParse(_inputContent, out inputnum))
                {
                    MagicFlowMsg.ShowMessage("为什么要胡乱输入！不玩了！", 2);
                    return;
                }
                continue;
            }
            MagicFlowMsg.ShowMessage("你真厉害！居然猜对了！", 1);
            if (await MagicShow.ShowMsgDialogAsync("猜对了：" + num + "！\n你真厉害！", "恭喜你！", true, "不，我不知道", "我知道了"))
            {
                Window.GetWindow(this).Title = ":)";
            }
            else
            {
                Window.GetWindow(this).Title = ":(";
            }
            return;
        }

        private async void WesternEgg_Click(object sender, RoutedEventArgs e)
        {
            if (isWesternEgg != 0)
            {
                switch (isWesternEgg)
                {
                    case 1:
                        MagicFlowMsg.ShowMessage("你都已经点过了，别再点了！", 0);
                        break;
                    case 2:
                        MagicFlowMsg.ShowMessage("你还真是执着呢！", 3);
                        break;
                    case 3:
                        MagicFlowMsg.ShowMessage("你真是个执着的家伙！", 2);
                        break;
                    case 4:
                        MagicFlowMsg.ShowMessage("好吧，那来玩一个小游戏吧！", 1);
                        await GuessNumGame();
                        break;
                }
                if (isWesternEgg < 4)
                    isWesternEgg++;
                return;
            }
            bool dialog = await MagicShow.ShowMsgDialogAsync("点击此按钮后软件出现任何问题作者概不负责，你确定要继续吗？\n（光敏性癫痫警告！若您患有光敏性癫痫，请不要点击确定！）", "警告", true, isDangerPrimaryBtn: true, closeBtnContext: "我不确定QWQ");
            isWesternEgg = 1;
            if (dialog)
            {
                Random random = new Random();
                ColorAnimationUsingKeyFrames colorAnimation = new ColorAnimationUsingKeyFrames
                {
                    Duration = TimeSpan.FromSeconds(20.0),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                int frameCount = 7;
                for (int i = 0; i < frameCount; i++)
                {
                    // 生成随机颜色，但避免过于刺眼的颜色
                    Color randomColor = Color.FromRgb(
                        (byte)random.Next(20, 240),  // 避免极端值
                        (byte)random.Next(20, 240),
                        (byte)random.Next(20, 240)
                    );

                    // 使用缓动关键帧
                    EasingColorKeyFrame keyFrame = new EasingColorKeyFrame(
                        randomColor,
                        KeyTime.FromTimeSpan(TimeSpan.FromSeconds(i * (20.0 / frameCount))),
                        new PowerEase { Power = 2, EasingMode = EasingMode.EaseInOut }
                    );

                    colorAnimation.KeyFrames.Add(keyFrame);
                }

                // 平滑循环动画
                if (colorAnimation.KeyFrames.Count > 0)
                {
                    Color firstColor = ((ColorKeyFrame)colorAnimation.KeyFrames[0]).Value;
                    EasingColorKeyFrame lastKeyFrame = new EasingColorKeyFrame(
                        firstColor,
                        KeyTime.FromTimeSpan(TimeSpan.FromSeconds(20.0)),
                        new PowerEase { Power = 2, EasingMode = EasingMode.EaseInOut }
                    );
                    colorAnimation.KeyFrames.Add(lastKeyFrame);
                }

                SolidColorBrush brush = new SolidColorBrush();
                WeakReference<SolidColorBrush> weakBrush = new WeakReference<SolidColorBrush>(brush);

                // 开始动画并应用
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                Application.Current.Resources["BackgroundBrush"] = brush;

                // 处理背景图片
                if (File.Exists("MSL\\Background.png"))
                {
                    File.Copy("MSL\\Background.png", "MSL\\Background_.png", true);
                    File.Delete("MSL\\Background.png");
                    ChangeSkinStyle();
                }
            }
        }

        private async void UseMicaEffect_Click(object sender, RoutedEventArgs e)
        {
            if (UseMicaEffect.IsChecked == true)
            {
                Config.Write("MicaEffect", true);
                Config.Write("darkTheme", "Auto");
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
                Config.Write("MicaEffect", false);
                semitransparentTitle.IsEnabled = true;
                autoSetTheme.IsEnabled = true;
                ChangeSkinColor.IsEnabled = true;
                changeBackImg.Visibility = Visibility.Visible;
                delBackImg.Visibility = Visibility.Visible;
                WesternEgg.Visibility = Visibility.Visible;
                MagicFlowMsg.ShowMessage("已关闭Mica效果！", 1);
            }
            autoSetTheme.IsChecked = true;
            darkTheme.IsChecked = false;
            darkTheme.IsEnabled = false;
            await Task.Delay(500);
            ChangeSkinStyle();
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
                        LogHelper.Write.Info("正在更换背景图片……");
                        File.Copy(openfile.FileName, "MSL\\Background.png", true);
                        ChangeSkinStyle();
                    }
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "更换背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
                }
            }
        }

        private void delBackImg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogHelper.Write.Info("清除背景图片，恢复默认背景颜色……");
                File.Delete("MSL\\Background.png");
                ChangeSkinStyle();
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "清除背景图片失败！请重试！\n错误代码：" + ex.Message, "错误");
            }
        }

        private void ChangeLogForeColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            switch (LogForeTypeCombo.SelectedIndex)
            {
                case 0:
                    picker.SelectedBrush = ConfigStore.LogColor.INFO;
                    break;
                case 1:
                    picker.SelectedBrush = ConfigStore.LogColor.WARN;
                    break;
                case 2:
                    picker.SelectedBrush = ConfigStore.LogColor.ERROR;
                    break;
                case 3:
                    picker.SelectedBrush = ConfigStore.LogColor.HIGHLIGHT;
                    break;
            }
            var window = new PopupWindow
            {
                PopupElement = picker
            };
            bool isConfirmed = false;
            picker.Confirmed += delegate { isConfirmed = true; window.Close(); };
            picker.Canceled += delegate { window.Close(); };
            window.ShowDialog(ChangeLogForeColor, false);
            if (!isConfirmed)
            {
                return;
            }
            switch (LogForeTypeCombo.SelectedIndex)
            {
                case 0:
                    ConfigStore.LogColor.INFO = picker.SelectedBrush;
                    break;
                case 1:
                    ConfigStore.LogColor.WARN = picker.SelectedBrush;
                    break;
                case 2:
                    ConfigStore.LogColor.ERROR = picker.SelectedBrush;
                    break;
                case 3:
                    ConfigStore.LogColor.HIGHLIGHT = picker.SelectedBrush;
                    break;
            }
            var config = new
            {
                ConfigStore.LogColor.INFO,
                ConfigStore.LogColor.WARN,
                ConfigStore.LogColor.ERROR,
                ConfigStore.LogColor.HIGHLIGHT
            };
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            Config.Write("LogColor", JObject.Parse(json));
            MagicFlowMsg.ShowMessage("保存日志颜色成功！重新打开服务器运行窗口以使其生效！", 1);
        }

        private void RestoreLogForeColor_Click(object sender, RoutedEventArgs e)
        {
            Config.Remove("LogColor");
            ConfigStore.LogColor.INFO = Brushes.Green;
            ConfigStore.LogColor.WARN = Brushes.Orange;
            ConfigStore.LogColor.ERROR = Brushes.Red;
            ConfigStore.LogColor.HIGHLIGHT = Brushes.Blue;
            MagicFlowMsg.ShowMessage("已恢复默认日志颜色！重新打开服务器运行窗口以使其生效！", 1);
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
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoRunApp"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
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
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoRunApp"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
            }
        }
        private void autoUpdateApp_Click(object sender, RoutedEventArgs e)
        {
            if (autoUpdateApp.IsChecked == true)
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoUpdateApp"] = true;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("开启成功！");
                MagicFlowMsg.ShowMessage("开启成功！", 1);
            }
            else
            {
                string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoUpdateApp"] = false;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                //Growl.Success("关闭成功！");
                MagicFlowMsg.ShowMessage("关闭成功！", 1);
            }
        }

        private async void checkUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            //更新
            try
            {
                var mainwindow = Window.GetWindow(Window.GetWindow(this));
                JObject _httpReturn = (await HttpService.GetApiContentAsync("query/update"));
                string _version = _httpReturn["data"]["latestVersion"].ToString();
                Version newVersion = new Version(_httpReturn["data"]["latestVersion"].ToString());
                Version version = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if (newVersion > version)
                {
                    var updatelog = _httpReturn["data"]["log"].ToString();
                    bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "发现新版本，版本号为：" + _version + "，是否进行更新？\n更新日志：\n" + updatelog, "更新", true, "取消");
                    if (dialog == true)
                    {
                        if (MainWindow.ProcessRunningCheck())
                        {
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), "您的服务器/内网映射/点对点联机正在运行中，若此时更新，会造成后台残留，请将前者关闭后再进行更新！", "警告");
                            return;
                        }
                        string downloadUrl = (await HttpService.GetApiContentAsync("download/update"))["data"].ToString(); ;
                        await MagicShow.ShowDownloader(Window.GetWindow(this), downloadUrl, AppDomain.CurrentDomain.BaseDirectory, "MSL" + _version + ".exe", "下载新版本中……");
                        if (File.Exists("MSL" + _version + ".exe"))
                        {
                            string oldExePath = Process.GetCurrentProcess().MainModule.ModuleName;
                            string dwnExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MSL" + _version + ".exe");
                            string newExeDir = AppDomain.CurrentDomain.BaseDirectory;

                            // 输出CMD命令以便调试
                            string cmdCommand = "/C choice /C Y /N /D Y /T 1 & Del \"" + oldExePath + "\" & Ren \"" + "MSL" + _version + ".exe" + "\" \"MSL.exe\" & start \"\" \"MSL.exe\"";
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
                }
                else if (newVersion < version)
                {
                    //Growl.Info("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！");
                    MagicFlowMsg.ShowMessage("当前版本高于正式版本，若使用中遇到BUG，请及时反馈！", 4);
                }
                else
                {
                    //Growl.Success("您使用的开服器已是最新版本！");
                    MagicFlowMsg.ShowMessage("您使用的开服器已是最新版本！", 1);
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
            if (openserversOnStart.IsChecked == true)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先关闭开关后再进行调整！", "提示");
                return;
            }
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
            if (openserversOnStart.IsChecked == true)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先关闭开关后再进行调整！", "提示");
                return;
            }
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

        private void CloseWindowDialog_Click(object sender, EventArgs e)
        {
            if (CloseWindowDialog.IsChecked == true)
            {
                Config.Write("closeWindowDialog", true);
            }
            else
            {
                Config.Write("closeWindowDialog", false);
            }
        }

        private async void MSLTips_Click(object sender, RoutedEventArgs e)
        {
            if (MSLTips.IsChecked == false)
            {
                if (await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "关闭此功能后，读取服务器信息、玩家等功能将会失效，请谨慎选择！", "警告", true) == true)
                {
                    try
                    {
                        string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject["mslTips"] = false;
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                        //Growl.Success("关闭成功！重启服务器运行界面以生效！");
                        MagicFlowMsg.ShowMessage("关闭成功！重启服务器运行界面以生效！", 1);
                        return;
                    }
                    catch
                    {
                        //Growl.Error("关闭失败！");
                        MagicFlowMsg.ShowMessage("关闭失败！", 2);
                        return;
                    }
                }
                else
                {
                    MSLTips.IsChecked = true;
                    return;
                }
            }
            else
            {
                try
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["mslTips"] = true;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    //Growl.Success("开启成功！重启服务器运行界面以生效！");
                    MagicFlowMsg.ShowMessage("开启成功！重启服务器运行界面以生效！", 1);
                    return;
                }
                catch
                {
                    //Growl.Error("开启失败！");
                    MagicFlowMsg.ShowMessage("开启失败！", 2);
                    return;
                }
            }
        }

        private void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/mc-server/start/");
        }

        private void CopyDCID_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(Functions.GetDeviceID());
                //Growl.Info("设备ID复制成功！");
                MagicFlowMsg.ShowMessage("设备ID复制成功！", 1);
            }
            catch
            {
                MagicFlowMsg.ShowMessage("复制失败！", 2);
            }
        }
    }
}