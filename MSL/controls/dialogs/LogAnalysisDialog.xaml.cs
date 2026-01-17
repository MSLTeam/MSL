using HandyControl.Controls;
using MdXaml;
using MSL.pages.frpProviders.MSLFrp;
using MSL.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Window = System.Windows.Window;

namespace MSL.controls.dialogs
{
    /// <summary>
    /// LogAnalysisDialog.xaml 的交互逻辑
    /// </summary>
    public partial class LogAnalysisDialog : UserControl
    {
        private Window FatherWindow { get; set; } = null;
        private string Rserverbase { get; set; }
        private string Rservercore { get; set; }
        public Dialog SelfDialog { get; set; } = null;
        public LogAnalysisDialog(Window window, string rserverbase, string rservercore)
        {
            InitializeComponent();
            FatherWindow = window;
            Rserverbase = rserverbase;
            Rservercore = rservercore;
        }

        private bool isInit = false;

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInit)
                return;
            isInit = true;
            LogHelper.Write.Info("MSLFrpProfile 页面加载，开始初始化...");

            string token = string.Empty;
            if (string.IsNullOrEmpty(MSLFrpApi.UserToken))
            {
                LogHelper.Write.Info("内存中未找到Token，尝试从本地配置文件中读取Token。");
                token = Config.Read("MSLUserAccessToken")?.ToString();
                if (string.IsNullOrEmpty(token))
                {
                    LogHelper.Write.Warn("未找到本地或内存中的Token，显示登录页面。");
                    ShowLoginControl();
                    return;
                }
                LogHelper.Write.Info("找到Token，尝试使用Token自动登录。");
                if (await PerformLogin(token) == false)
                {
                    LogHelper.Write.Warn("Token自动登录失败，显示登录页面。");
                    ShowLoginControl();
                    return;
                }
            }
            LogHelper.Write.Info("内存中已记录用户Token，尝试继续操作……");
            token = MSLFrpApi.UserToken;
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
            await GetUserTokens();
            magicDialog.CloseTextDialog();
            LogHelper.Write.Info("用户资料及商品信息加载完成。");
        }

        private void ShowLoginControl()
        {
            // 显示登录页面
            MSLFrpLogin loginControl = new MSLFrpLogin();
            loginControl.LoginSuccess += async delegate
            {
                LogHelper.Write.Info("接收到登录成功委托，开始执行登录后操作。");
                if (await PerformLogin(MSLFrpApi.UserToken) == true)
                {
                    LoginControl.Visibility = Visibility.Collapsed;
                    MainGrid.Visibility = Visibility.Visible;
                    MagicDialog magicDialog = new MagicDialog();
                    magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
                    LogHelper.Write.Info("登录成功，开始加载用户资料和商品信息。");
                    await GetUserTokens();
                    magicDialog.CloseTextDialog();
                    LogHelper.Write.Info("用户资料及商品信息加载完成。");
                }
            };
            LoginControl.Content = loginControl;
            LoginControl.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
        }

        private async Task<bool> PerformLogin(string token)
        {
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            LogHelper.Write.Info("正在请求MSLFrp用户登录接口...");
            (int Code, string Msg, _) = await MSLFrpApi.UserLogin(token);

            magicDialog.CloseTextDialog();

            if (Code != 200)
            {
                LogHelper.Write.Error($"MSLFrp用户登录失败, Code: {Code}, Msg: {Msg}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！\n" + Msg, "错误");
                return false;
            }

            LogHelper.Write.Info("MSLFrp用户登录成功。");
            return true;
        }

        private async Task GetUserTokens()
        {
            LogHelper.Write.Info("正在请求MSLFrp用户AI日志分析接口...");
            (int Code, var Data, string Msg) = await MSLFrpApi.ApiGet("/tools/ai/usage");
            if (Code != 200)
            {
                LogHelper.Write.Error($"获取MSLFrp用户AIInfo失败, Code: {Code}, Msg: {Msg}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取信息失败！请重新登录！\n" + Msg, "错误");
                ShowLoginControl();
                return;
            }
            LogHelper.Write.Info("获取MSLFrp用户AIInfo成功。");
            if (Data == null)
            {
                return;
            }
            int uid = Data["uid"]?.ToObject<int>() ?? 0;
            int todayUsage = Data["today_usage"]?.ToObject<int>() ?? 0;
            int extraTokens = Data["extra_tokens"]?.ToObject<int>() ?? 0;
            long lastUseTime = Data["last_use_time"]?.ToObject<long>() ?? 0;
            int maxPerDay = Data["max_per_day"]?.ToObject<int>() ?? 0;
            string infoMsg = $"用户UID: {uid}\n今日使用量: {todayUsage}/{maxPerDay}\n额外额度: {extraTokens}\n最后使用时间: {Functions.ConvertUnixTimeSeconds(lastUseTime)}";
            LogHelper.Write.Info(infoMsg);
            UserAIInfo.Content = infoMsg;

            GetServerLogInfo();
        }

        private void GetServerLogInfo()
        {
            if (File.Exists(Rserverbase + "\\logs\\latest.log"))
            {
                FileStream fileStream = new FileStream(Rserverbase + "\\logs\\latest.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader streamReader = new StreamReader(fileStream);
                try
                {
                    LogInput.Text = streamReader.ReadToEnd();
                }
                finally
                {
                    fileStream.Dispose();
                    streamReader.Dispose();
                }
            }

            // 插件信息
            if (Directory.Exists(Rserverbase + "\\plugins"))
            {
                DirectoryInfo plugins = new DirectoryInfo(Rserverbase + "\\plugins");
                // 直接把所有插件文件名以,逗号间隔的方式列入PluginsInput中
                PluginsInput.Text = string.Join(", ", plugins.GetFiles().Select(f => f.Name));
            }

            // 模组信息
            if (Directory.Exists(Rserverbase + "\\mods"))
            {
                DirectoryInfo mods = new DirectoryInfo(Rserverbase + "\\mods");
                // 直接把所有模组文件名以,逗号间隔的方式列入ModsInput中
                ModsInput.Text = string.Join(", ", mods.GetFiles().Select(f => f.Name));
            }

            // ServerInfo
            ServerInfoInput.Text = $"服务器核心: {Rservercore}\n" +
                $"服务器目录: {Rserverbase}\n" +
                $"插件数量: {PluginsInput.Text.Split([", "], System.StringSplitOptions.RemoveEmptyEntries).Length}\n" +
                $"模组数量: {ModsInput.Text.Split([", "], System.StringSplitOptions.RemoveEmptyEntries).Length}\n";
        }

        private async void StartAnalyse_Click(object sender, RoutedEventArgs e)
        {
            StartAnalyse.IsEnabled = false;
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "正在分析中，请稍候...");
            LogHelper.Write.Info("正在请求AI日志分析接口...");
            string content = string.Empty;
            try
            {
                (int Code, var Data, string Msg) = await MSLFrpApi.ApiPost("/tools/ai/analysis", HttpService.PostContentType.Json, new Dictionary<string, object>
                {
                    { "core", Rservercore },
                    { "model", "qwen-flash" },
                    { "logs", LogInput.Text },
                    { "mods", ModsInput.Text },
                    { "plugins", PluginsInput.Text },
                    { "usemd", true }
                });
                if (Code != 200)
                {
                    LogHelper.Write.Error($"请求AI日志分析失败, Code: {Code}, Msg: {Msg}");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "AI日志分析失败！\n" + Msg, "错误");
                    return;
                }
                LogHelper.Write.Info("请求AI日志分析成功。");
                if (Data == null)
                {
                    return;
                }
                content = Data["content"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"请求AI日志分析出现异常: {ex}");
            }
            finally
            {
                StartAnalyse.IsEnabled = true;
                magicDialog.CloseTextDialog();
            }
            Markdown engine = new Markdown();

            FlowDocument document = engine.Transform(content);
            AnalysisOutput.Document = document;

            MainGrid.SelectedIndex = 1;
        }

        private void Store_Click(object sender, RoutedEventArgs e)
        {
            HandyControl.Controls.Window window = new HandyControl.Controls.Window
            {
                NonClientAreaBackground = (Brush)FindResource("BackgroundBrush"),
                Background = (Brush)FindResource("BackgroundBrush"),
                Title = "MSL用户中心 - 信息",
                MinHeight = 450,
                MinWidth = 750,
                Height = 450,
                Width = 750,
                ResizeMode = ResizeMode.CanResize,
                ShowMinButton = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            MSLFrpProfile frpProfile = new MSLFrpProfile();
            frpProfile.Margin = new Thickness(10);
            window.Owner = Window.GetWindow(this);
            window.Content = frpProfile;
            window.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
            SelfDialog.Close();
            GC.Collect();
        }

        private void Dispose()
        {
            LogInput.Clear();
            AnalysisOutput.Document.Blocks.Clear();
            PluginsInput.Clear();
            ModsInput.Clear();
            ServerInfoInput.Clear();
            UserAIInfo.Content = string.Empty;
            Rserverbase = string.Empty;
            Rservercore = string.Empty;
        }
    }
}
