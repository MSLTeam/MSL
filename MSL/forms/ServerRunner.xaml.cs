using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using HandyControl.Tools.Command;
using HandyControl.Tools.Extension;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.controls.ctrls_serverrunner;
using MSL.controls.dialogs;
using MSL.forms;
using MSL.langs;
using MSL.pages;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace MSL
{
    /// <summary>
    /// ServerRunner.xaml 的交互逻辑
    /// </summary>
    public partial class ServerRunner : HandyControl.Controls.Window
    {
        public static event App.DeleControl SaveConfigEvent;
        public static event App.DeleControl ServerStateChange;
        private short GetServerInfoLine = 0;
        private readonly short FirstStartTab;
        private MCSLogHandler MCSLogHandler { get; set; }
        private Process ServerProcess { get; set; }
        private ConptyWindow ConPTYWindow { get; set; }
        private DispatcherTimer ConPTYResizeTimer { get; set; } = new DispatcherTimer();
        private ServerProperties ServerProperties { get; set; }
        private int RserverID { get; }
        private string Rservername { get; set; }
        private string Rserverjava { get; set; }
        private string Rserverserver { get; set; }
        private string RserverJVM { get; set; }
        private string RserverJVMcmd { get; set; }
        private string RserverYggAddr { get; set; }
        private string Rserverbase { get; set; }
        private short Rservermode { get; set; }
        public class LogSegment
        {
            public string Text { get; set; }
            public Color Color { get; set; }
            public bool IsBold { get; set; }
            public bool IsUnderline { get; set; }
        }

        public class LogEntry
        {
            public int StartOffset { get; set; }  // 在 Document 中的起始位置
            public List<LogSegment> Segments { get; set; } = new();
        }
        private LogColorizer _logColorizer;
        private int _logEntryCount = 0;
        private const int MaxLogEntries = 1000;

        public class FastCommandInfo
        {
            public string Remark { get; set; }  // 备注 (如: 给管理员)
            public string Cmd { get; set; }     // 指令 (如: op)
            public string Alias { get; set; }   // 别名 (如: o)

            // 用于在 ComboBox 和 ListBox 中展示的格式
            public string DisplayText
            {
                get
                {
                    if (Cmd == "/") return "/（指令）"; // 根命令兜底
                    string text = $"/{Cmd.TrimStart('/')}"; // 统一显示前缀
                    if (!string.IsNullOrEmpty(Remark)) text += $"（{Remark}）";
                    if (!string.IsNullOrEmpty(Alias)) text += $" [{Alias}]";
                    return text;
                }
            }
        }

        // 全局指令列表
        private List<FastCommandInfo> CurrentFastCmds = new List<FastCommandInfo>();

        /// <summary>
        /// 服务器运行窗口
        /// </summary>
        /// <param name="serverID">服务器ID</param>
        /// <param name="controlTab">Tab标签</param>
        public ServerRunner(int serverID, short controlTab = 0)
        {
            InitializeComponent();
            InitializeLogHandler();
            InitializeOutlog();

            ConPTYResizeTimer.Interval = TimeSpan.FromMilliseconds(200);
            ConPTYResizeTimer.Tick += new EventHandler(ConPTYResizeTimer_Tick);
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
            RserverID = serverID;
            FirstStartTab = controlTab;
        }

        private void InitializeOutlog()
        {
            _logColorizer = new LogColorizer();
            outlog.TextArea.TextView.LineTransformers.Add(_logColorizer);
        }

        private void InitializeLogHandler()
        {
            MCSLogHandler = new MCSLogHandler(
                logAction: PrintLog,
                infoHandler: LogHandleInfo,
                warnHandler: LogHandleWarn,
                encodingIssueHandler: HandleEncodingIssue
            );
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // OldVersionTip();
            ChangeSkinStyle();
            TabCtrl.SelectedIndex = -1;
            LoadingCircle loadingCircle = new LoadingCircle();
            MainGrid.Children.Add(loadingCircle);
            MainGrid.RegisterName("loadingBar", loadingCircle);
            await Task.Delay(50);
            if (!await LoadingInfoEvent())
                return;
            GetFastCmd();
            LoadedInfoEvent();
            await Task.Delay(50);
            MainGrid.Children.Remove(loadingCircle);
            MainGrid.UnregisterName("loadingBar");
            TabCtrl.SelectedIndex = FirstStartTab;
            if (FirstStartTab == 0)
            {
                LaunchServerOnLoad();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    e.Cancel = true;
                    int dialog = 0;
                    AppConfig config = AppConfig.Current;
                    Console.WriteLine(config.CloseWindowDialog);
                    if (config.CloseWindowDialog == false)
                    {
                        dialog = 1; // 不显示提示，直接关闭窗口
                    }
                    else
                    {
                        dialog = MagicShow.ShowMsg(this, "检测到您没有关闭服务器，是否隐藏此窗口？\n如要重新显示此窗口，请在服务器列表内双击该服务器（或点击开启服务器按钮）", "警告", true, "取消");
                    }

                    if (dialog == 1)
                    {
                        if (ConPTYWindow != null)
                        {
                            ConptyPopUp.IsOpen = false;
                            Growl.SetGrowlParent(ConptyGrowlPanel, false);
                            Growl.SetGrowlParent(GrowlPanel, true);
                            ConPTYWindow.Visibility = Visibility.Collapsed;
                        }
                        Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DisposeRes();
                }
            }
            catch
            {
                DisposeRes();
            }
        }

        private void DisposeRes()
        {
            MCSLogHandler.CleanupResources();
            ClearLog();
            if (ServerList.ServerWindowList.ContainsKey(RserverID))
            {
                ServerList.ServerWindowList.Remove(RserverID);
            }
            SettingsPage.ChangeSkinStyle -= ChangeSkinStyle;
            if (ConPTYWindow != null)
            {
                try
                {
                    ConPTYWindow.Closing -= ConptyWindowClosing;
                    ConPTYWindow.ControlServer.Click -= ConptyWindowControlServer;
                    ConPTYWindow.ControlServer.MouseDoubleClick -= KillConptyServer;
                    ConPTYWindow.Close();
                }
                finally
                {
                    ConPTYWindow = null;
                }
            }
            MCSLogHandler.Dispose();
            MCSLogHandler = null;
            ServerProperties.Dispose();
            getSystemInfo = false;
            Rservername = null;
            Rserverjava = null;
            Rserverserver = null;
            RserverJVM = null;
            RserverJVMcmd = null;
            Rserverbase = null;
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }

        private async Task<bool> LoadingInfoEvent()
        {
            AppConfig config = AppConfig.Current;
            if (config.MslTips == false)
            {
                MCSLogHandler.IsMSLFormatedLog = false;
            }
            if (config.SideMenuExpanded == true)
            {
                Tab_Home.Width = double.NaN;
                Tab_Console.Width = double.NaN;
                Tab_Plugins.Width = double.NaN;
                Tab_Settings.Width = double.NaN;
                Tab_MoreFunctions.Width = double.NaN;
                Tab_Timer.Width = double.NaN;
            }
            else
            {
                Tab_Home.Width = 50;
                Tab_Console.Width = 50;
                Tab_Plugins.Width = 50;
                Tab_Settings.Width = 50;
                Tab_MoreFunctions.Width = 50;
                Tab_Timer.Width = 50;
            }

            //Get Server-Information
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            try
            {
                if (_json["name"] == null || _json["java"] == null || _json["base"] == null || _json["core"] == null || _json["memory"] == null || _json["args"] == null)
                {
                    await MagicShow.ShowMsgDialogAsync("加载服务器信息时出现错误！", "错误");
                    Close();
                    return false;
                }
            }
            catch
            {
                await MagicShow.ShowMsgDialogAsync("加载服务器信息时出现错误！", "错误");
                Close();
                return false;
            }
            Rservername = _json["name"].ToString();
            Rserverjava = _json["java"].ToString();
            Rserverbase = _json["base"].ToString();
            Rserverserver = _json["core"].ToString();
            RserverJVM = _json["memory"].ToString();
            RserverJVMcmd = _json["args"].ToString();
            if (_json.ContainsKey("mode")) // 1为自定义模式
            {
                Rservermode = short.Parse(_json["mode"].ToString());
            }
            else
            {
                Rservermode = 0;
            }

            if (_json.ContainsKey("ygg_api")) // 1为自定义模式
            {
                RserverYggAddr = _json["ygg_api"].ToString();
            }
            else
            {
                RserverYggAddr = "";
            }

            if (_json["autostartServer"] != null && _json["autostartServer"].ToString() == "True")
            {
                autoStartserver.IsChecked = true;
            }
            if (_json["showOutlog"] != null && _json["showOutlog"].ToString() == "False")
            {
                MCSLogHandler.IsShowOutLog = false;
                showOutlog.IsChecked = false;
            }
            if (_json["formatOutPrefix"] != null && (bool)_json["formatOutPrefix"] == false)
            {
                MCSLogHandler.IsFormatLogPrefix = false;
                formatOutHead.IsChecked = false;
            }
            if (_json["shieldLogKeys"] != null)
            {
                var items = _json["shieldLogKeys"];
                List<string> tempList = new List<string>();
                foreach (var item in items)
                {
                    tempList.Add(item.ToString());
                    ShieldLogList.Items.Add(item.ToString());
                }
                MCSLogHandler.ShieldLog = [.. tempList];
                shieldLogBtn.IsChecked = true;
                LogShield_Add.IsEnabled = false;
                LogShield_Del.IsEnabled = false;
            }
            if (_json["highLightLogKeys"] != null)
            {
                var items = _json["highLightLogKeys"];
                List<string> tempList = new List<string>();
                foreach (var item in items)
                {
                    tempList.Add(item.ToString());
                    HighLightLogList.Items.Add(item.ToString());
                }
                MCSLogHandler.HighLightLog = [.. tempList];
                highLightLogBtn.IsChecked = true;
                LogHighLight_Add.IsEnabled = false;
                LogHighLight_Del.IsEnabled = false;
            }
            if (_json["shieldStackOut"] != null && _json["shieldStackOut"].ToString() == "False")
            {
                MCSLogHandler.IsShowOutLog = false;
                shieldStackOut.IsChecked = false;
            }
            if (_json["autoClearOutlog"] != null && _json["autoClearOutlog"].ToString() == "False")
            {
                autoClearOutlog.IsChecked = false;
            }
            if (_json["encoding_in"] != null)
            {
                inputCmdEncoding.Content = _json["encoding_in"].ToString();
            }
            if (_json["encoding_out"] != null)
            {
                outputCmdEncoding.Content = _json["encoding_out"].ToString();
            }
            if (_json["fileforceUTF8"] != null && _json["fileforceUTF8"].ToString() == "True")
            {
                fileforceUTF8encoding.IsChecked = true;
            }
            if (_json["ygg_api"] != null)
            {
                YggdrasilAddr.Text = _json["ygg_api"].ToString();
            }
            if (_json["backup_mode"] != null)
            {
                try
                {
                    if (int.Parse(_json["backup_mode"].ToString()) >= 0 && int.Parse(_json["backup_mode"].ToString()) <= 2)
                    {
                        ComboBackupPath.SelectedIndex = int.Parse(_json["backup_mode"].ToString());
                    }
                    else
                    {
                        ComboBackupPath.SelectedIndex = 0;
                    }

                }
                catch (Exception)
                {
                    ComboBackupPath.SelectedIndex = 0;
                }
            }
            else
            {
                ComboBackupPath.SelectedIndex = 0;
            }
            if (_json["backup_max_limit"] != null)
            {
                TextBackupMaxLimitCount.Text = _json["backup_max_limit"].ToString();
            }
            if (_json["backup_custom_path"] != null)
            {
                TextBackupPath.Text = _json["backup_custom_path"].ToString();
            }
            if (_json["backup_save_delay"] != null)
            {
                TextBackupDelay.Text = _json["backup_save_delay"].ToString();
            }
            this.Title = Rservername;//set title to server name

            bool isChangeConfig = false;
            if (!Directory.Exists(Rserverbase))
            {
                string[] pathParts = Rserverbase.Split('\\');
                if (pathParts.Length >= 2 && pathParts[pathParts.Length - 2] == "MSL")
                {
                    // 路径的倒数第二个是 MSL
                    isChangeConfig = true;
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; // 获取当前应用程序的基目录
                    Rserverbase = Path.Combine(baseDirectory, "MSL", string.Join("\\", pathParts.Skip(pathParts.Length - 1))); // 拼接 MSL 目录下的路径
                }
                else
                {
                    // 路径的倒数第二个不是 MSL
                    Growl.Error("您的服务器目录似乎有误，是从别的位置转移到此处吗？请手动前往服务器设置界面进行更改！");
                }
            }
            else if (File.Exists(Rserverbase + "\\server-icon.png"))//check server-icon,if exist,set icon to server-icon
            {
                try
                {
                    Icon = new BitmapImage(new Uri(Rserverbase + "\\server-icon.png"));
                }
                catch { Icon = null; }
            }
            if (Rserverjava != "Java" && Rserverjava != "java" && Rservermode == 0)
            {
                if (!Path.IsPathRooted(Rserverjava))
                {
                    Rserverjava = AppDomain.CurrentDomain.BaseDirectory + Rserverjava;
                }
                if (!File.Exists(Rserverjava))
                {
                    string[] pathParts = Rserverjava.Split('\\');
                    if (pathParts.Length >= 4 && pathParts[pathParts.Length - 4] == "MSL")
                    {
                        // 路径的倒数第四个是 MSL
                        isChangeConfig = true;
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; // 获取当前应用程序的基目录
                        Rserverjava = Path.Combine(baseDirectory, "MSL", string.Join("\\", pathParts.Skip(pathParts.Length - 3))); // 拼接 MSL 目录下的路径
                    }
                    else
                    {
                        // 路径的倒数第四个不是 MSL
                        Growl.Error("您的Java目录似乎有误，是从别的位置转移到此处的吗？请手动前往服务器设置界面进行更改！");
                    }
                }
            }
            if (_json["useConpty"] != null && _json["useConpty"].ToString() == "True")
            {
                Tradition_LogFunGrid.Visibility = Visibility.Collapsed;
                Tradition_LogFunDivider.Visibility = Visibility.Collapsed;
                Tradition_CMDCard.Visibility = Visibility.Collapsed;
                useConpty.IsChecked = true;
            }

            if (isChangeConfig)
            {
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                jsonObject[RserverID.ToString()].Replace(_json);
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            }
            return true;
        }//窗体加载后，运行此方法，主要为改变UI、检测服务器是否完整

        private void LoadedInfoEvent()
        {
            systemInfoBtn.IsChecked = ConfigStore.GetServerInfo;
            recordPlayInfo = ConfigStore.GetPlayerInfo;
            playerInfoBtn.IsChecked = recordPlayInfo;
            ServerProperties = new ServerProperties(this, Rserverbase);
            SettingsGrid.Content = ServerProperties;
            LoadSettings();
            if (systemInfoBtn.IsChecked == true)
            {
                getSystemInfo = true;
                Thread thread = new Thread(GetSystemInfo);
                thread.Start();
            }
        }//运行完LoadingInfoEvent后运行此方法，主要为加载占用显示模块和其他配置加载

        //此部分是更改窗体皮肤的方法
        #region ChangeSkin
        private void ChangeSkinStyle()
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));

                if (jsonObject["MicaEffect"].Value<bool>() == true)
                {
                    ChangeTitleStyle(true);
                    this.SystemBackdropType = BackdropType.Auto;
                    this.SystemBackdropType = BackdropType.Mica;
                    this.Background = Brushes.Transparent;
                }
                else
                {
                    this.SystemBackdropType = BackdropType.Auto;
                    this.SetResourceReference(BackgroundProperty, "BackgroundBrush");
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
                        Task.Run(async () =>
                        {
                            int i = 0;
                            while (MainWindow.BackImageBrush == null)
                            {
                                if (i == 5)
                                    break;
                                i++;
                                await Task.Delay(1000);
                            }
                            Dispatcher.Invoke(() =>
                            {
                                Background = MainWindow.BackImageBrush;
                            });
                        });
                    }
                    else
                    {
                        SetResourceReference(BackgroundProperty, "BackgroundBrush");
                    }
                }
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

        /*
        private void OldVersionTip()
        {
            if (MainWindow.IsOldVersion)
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
        }
        */
        #endregion

        private async void SideMenuContextOpen_Click(object sender, RoutedEventArgs e)
        {
            if (Tab_Home.Width == 50)
            {
                Tab_Home.Width = double.NaN;
                Tab_Console.Width = double.NaN;
                Tab_Plugins.Width = double.NaN;
                Tab_Settings.Width = double.NaN;
                Tab_MoreFunctions.Width = double.NaN;
                Tab_Timer.Width = double.NaN;
                //frame.Margin = new Thickness(100, 0, 0, 0);
                try
                {
                    Config.Write("sidemenuExpanded", true);
                }
                catch { }
            }
            else
            {
                Tab_Home.Width = 50;
                Tab_Console.Width = 50;
                Tab_Plugins.Width = 50;
                Tab_Settings.Width = 50;
                Tab_MoreFunctions.Width = 50;
                Tab_Timer.Width = 50;
                //frame.Margin = new Thickness(50, 0, 0, 0);
                try
                {
                    Config.Write("sidemenuExpanded", false);
                }
                catch { }
            }
            if (TabCtrl.SelectedIndex == 1 && ConPTYWindow != null)
            {
                await Task.Delay(50);
                ShowConptyWindow();
            }
        }

        private bool firstTimeOpenTerminal = true;
        private async void TabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }
            if (!ReferenceEquals(e.OriginalSource, this.TabCtrl))
            {
                return;
            }
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible && TabCtrl.SelectedIndex != 1)
            {
                ConPTYWindow.Visibility = Visibility.Collapsed;
                ConptyPopUp.IsOpen = false;
                Growl.SetGrowlParent(ConptyGrowlPanel, false);
                // 下面三行别动，就是这么写的，要不然会出问题
                Growl.SetGrowlParent(GrowlPanel, true);
                Growl.SetGrowlParent(GrowlPanel, false);
                Growl.SetGrowlParent(GrowlPanel, true);
            }
            switch (TabCtrl.SelectedIndex)
            {
                case 1:
                    if (ConPTYWindow != null)
                    {
                        await Task.Delay(50);
                        ShowConptyWindow();
                        ConptyPopUp.IsOpen = true;
                        Growl.SetGrowlParent(GrowlPanel, false);
                        Growl.SetGrowlParent(ConptyGrowlPanel, true);
                    }
                    else
                    {
                        if (firstTimeOpenTerminal)
                        {
                            firstTimeOpenTerminal = false;
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(500);
                                Dispatcher.Invoke(() =>
                                {
                                    outlog.ScrollToEnd();
                                });
                            });
                        }
                    }
                    break;
                case 2:
                    ReFreshPluginsAndMods();
                    break;
                case 3:
                    ServerProperties.RefreshServerConfig();
                    break;
                default:
                    break;
            }
        }

        public bool CheckServerRunning()
        {
            if (ConPTYWindow != null)
            {
                if (ConPTYWindow.ConptyConsole.ConPTYTerm.TermProcIsRunning)
                {
                    Logger.Info("检测服务器运行事件：服务器正在运行 (Conpty)");
                    return true;
                }
                else
                {
                    Logger.Info("检测服务器运行事件：服务器未运行 (Conpty)");
                    return false;
                }
            }
            try
            {
                if (ServerProcess != null && !ServerProcess.HasExited)
                {
                    Logger.Info("检测服务器运行事件：服务器正在运行");
                    return true;
                }
            }
            catch
            {
                Logger.Info("检测服务器运行事件：服务器未运行");
                return false;
            }
            Logger.Info("检测服务器运行事件：服务器未运行 (已关闭)");
            return false;
        }

        #region 仪表盘

        //////////////////
        /////这里是仪表盘
        //////////////////

        private async void solveProblemBtn_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "分析报告将在服务器关闭后生成！若使用后还是无法解决问题，请尝试进Q群询问（附带日志或日志链接，日志链接可以点击分享日志按钮生成）：\n一群：1145888872  二群：234477679", "警告", true, "取消");
            if (dialogRet)
            {
                MCSLogHandler.ServerService.ProblemSolveSystem = true;
                LaunchServer();
            }
        }
        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            Growl.Info("正在为您打开服务器目录……");
            Process.Start(Rserverbase);
        }

        private void copyPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (serverPlayerList.SelectedValue == null)
            {
                MagicFlowMsg.ShowMessage("请先选择一个玩家！", 2);
                return;
            }
            MagicFlowMsg.ShowMessage("复制成功！");
            Clipboard.SetText(serverPlayerList.SelectedValue.ToString());
        }

        private async void kickPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "确定要踢出这个玩家吗？", "警告", true, "取消");
            if (dialogRet)
            {
                try
                {
                    if (ConPTYWindow != null)
                    {
                        ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm(("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")) + "\r\n").AsSpan());
                        return;
                    }
                    ServerProcess.StandardInput.WriteLine("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
                }
                catch
                {
                    Growl.Error("操作失败！");
                }
            }
        }

        private async void banPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "确定要封禁这个玩家吗？封禁后该玩家将永远无法进入服务器！\n（原版解封指令：pardon +玩家名字，若添加插件，请使用插件的解封指令）", "警告", true, "取消");
            if (dialogRet)
            {
                try
                {
                    if (ConPTYWindow != null)
                    {
                        ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm(("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")) + "\r\n").AsSpan());
                        return;
                    }
                    ServerProcess.StandardInput.WriteLine("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
                }
                catch
                {
                    Growl.Error("操作失败！");
                }
            }
        }

        private async void gotoFrpc_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress;
            // 获取本地计算机的IP地址列表
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            // 正则表达式匹配内网地址的模式
            string privateIpPattern = @"^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)";
            // 遍历IP地址列表
            foreach (IPAddress localIP in localIPs)
            {
                // 检查IPv4地址是否为公网IP
                if (localIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(localIP) &&
                    !Regex.IsMatch(localIP.ToString(), privateIpPattern))
                {
                    ipAddress = localServerIPLab.Content.ToString();

                    if (ipAddress.Contains(":"))
                    {
                        MagicShow.ShowMsgDialog(this, "您的公网IP为：" + localIP.ToString() + "\n您的服务器远程进入地址为：" + localIP.ToString() + ":" + ipAddress.Substring(ipAddress.IndexOf(":") + 1, ipAddress.Length - ipAddress.IndexOf(":") - 1) + "\n注意：记得检查您的防火墙是否关闭，否则远程玩家无法进入服务器！", "信息");
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(this, "您的公网IP为：" + localIP.ToString() + "\n您的服务器远程进入地址为：" + localIP.ToString() + "\n注意：记得检查您的防火墙是否关闭，否则远程玩家无法进入服务器！", "信息");
                    }
                    return;
                }
            }
            await MagicShow.ShowMsgDialogAsync(this, "服务器开启后，通常远程的小伙伴是无法进入的，您需要进行内网映射才可让他人进入。开服器内置有免费的内网映射，您可点击主界面左侧的“内网映射”按钮查看详情并进行配置。", "注意", false);
        }

        private async void systemInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (systemInfoBtn.IsChecked == true)
            {
                getSystemInfo = true;
                Thread thread = new Thread(GetSystemInfo);
                thread.Start();
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(this, "关闭此功能后，输出预览功能也将同时关闭！", "注意");
                previewOutlog.Text = "预览功能已关闭，请前往服务器控制台界面查看日志信息！";
                getSystemInfo = false;
            }
        }

        private bool getSystemInfo = false;
        private void GetSystemInfo()
        {
            PerformanceCounter cpuCounter;
            PerformanceCounter ramCounter;
            float phisicalMemory;
            double processUsedMemory;
            try
            {
                if (PerformanceCounterCategory.Exists("Processor Information") && PerformanceCounterCategory.CounterExists("% Processor Utility", "Processor Information"))
                {
                    cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                }
                else
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                phisicalMemory = Functions.GetPhysicalMemoryGB();
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Growl.Error("无法获取系统占用信息！显示占用功能已自动关闭！\n通常此问题是因为系统原因造成的，不影响软件正常使用！");
                    previewOutlog.Text = "预览功能已关闭，请前往服务器控制台界面查看日志信息！";
                });
                getSystemInfo = false;
                return;
            }
            while (getSystemInfo)
            {
                try
                {
                    float cpuUsage = cpuCounter.NextValue();
                    float ramAvailable = ramCounter.NextValue() / 1024;
                    double allMemory = phisicalMemory;
                    processUsedMemory = GetProcessMem();
                    Dispatcher.Invoke(() =>
                    {
                        if ((int)cpuUsage <= 100)
                        {
                            cpuInfoLab.Content = $"CPU: {cpuUsage:f2}%";
                            cpuInfoBar.Value = (int)cpuUsage;
                        }
                        UpdateMemoryInfo(ramAvailable, allMemory, processUsedMemory);
                        UpdateLogPreview();
                    });
                }
                catch
                {
                    getSystemInfo = false;
                    break;
                }
                finally
                {
                    Thread.Sleep(3000);
                }
            }
            return;
        }

        private double GetProcessMem()
        {
            try
            {
                Process targetProc = null;

                // 优先检查 ServerProcess
                if (ServerProcess != null && !ServerProcess.HasExited)
                {
                    targetProc = ServerProcess;
                }
                // 其次检查 ConPTYWindow 内部进程
                else if (ConPTYWindow?.ConptyConsole?.ConPTYTerm?.Process?.Process != null)
                {
                    var p = ConPTYWindow.ConptyConsole.ConPTYTerm.Process.Process;
                    if (!p.HasExited) targetProc = p;
                }

                if (targetProc != null)
                {
                    targetProc.Refresh(); // 刷新进程快照

                    return targetProc.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);
                }
            }
            catch { /* 忽略进程关闭瞬间的访问异常 */ }
            return 0;
        }

        private void UpdateMemoryInfo(float ramAvailable, double allMemory, double processUsedMemory)
        {
            memoryInfoLab.Content = $"总内存: {allMemory:f2}G\n已使用: {allMemory - ramAvailable:f2}G\n可使用: {ramAvailable:f2}G";
            double usedMemoryPercentage = (allMemory - ramAvailable) / allMemory;
            memoryInfoBar.Value = usedMemoryPercentage * 100;
            processMemoryInfoBar.Value = (processUsedMemory / allMemory) * 100;
            usedMemoryLab.Content = $"系统已用内存: {usedMemoryPercentage:P}";
            processMemoryInfoLab.Content = $"进程已用内存: {processUsedMemory:f2}G 占比: {(processUsedMemory / allMemory):P}";
        }

        private string tempLog;
        private void UpdateLogPreview()
        {
            if (previewOutlog.LineCount < 25)
            {
                if (!string.IsNullOrEmpty(tempLog) && !previewOutlog.Text.Contains(tempLog))
                {
                    previewOutlog.Text += "\n" + tempLog;
                    previewOutlog.ScrollToEnd();
                }
            }
            else
            {
                previewOutlog.Clear();
            }
        }

        private void playerInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playerInfoBtn.IsChecked == true)
            {
                recordPlayInfo = true;
                Growl.Success("已开启");
            }
            else
            {
                recordPlayInfo = false;
                Growl.Success("已关闭");
            }
        }
        #endregion

        #region 服务器输出

        ///////////这里是服务器输出

        private async void LaunchServerOnLoad()
        {
            while (!IsLoaded)
            {
                Thread.Sleep(1000);
            }
            await Dispatcher.InvokeAsync(() =>
            {
                LaunchServer();
            });
        }

        private async void LaunchServer()
        {
            LogHelper.Write.Info("尝试启动服务器：" + Rservername);
            try
            {
                if (await MCEulaEvent() != true)
                    return;
                string fileforceUTF8Jvm = "";
                if (Rservermode == 0)
                {
                    string ygg_api_jvm = "", full_cmd;
                    // 处理外置登录
                    if (!string.IsNullOrEmpty(RserverYggAddr))
                    {
                        ygg_api_jvm = $"-javaagent:authlib-injector.jar={RserverYggAddr} ";
                        if (!await DownloadAuthlib())
                        {
                            return; // 下载authlib失败，退出
                        }
                        LogHelper.Write.Info("成功启用外置登录库，地址：" + RserverYggAddr);
                    }

                    if (fileforceUTF8encoding.IsChecked == true && !RserverJVMcmd.Contains("-Dfile.encoding=UTF-8"))
                    {
                        fileforceUTF8Jvm = "-Dfile.encoding=UTF-8 ";
                    }

                    if (Rserverserver.StartsWith("@libraries/"))
                    {
                        full_cmd = RserverJVM + " " + fileforceUTF8Jvm + ygg_api_jvm + RserverJVMcmd + " " + Rserverserver + " nogui";
                    }
                    else
                    {
                        full_cmd = RserverJVM + " " + fileforceUTF8Jvm + ygg_api_jvm + RserverJVMcmd + " -jar \"" + Rserverserver + "\" nogui";
                    }
                    StartServer(full_cmd);
                    LogHelper.Write.Info("启动参数：" + full_cmd);
                    //GC.Collect();
                }
                else
                {
                    StartServer(RserverJVMcmd);
                }

            }
            catch (Exception a)
            {
                MessageBox.Show("出现错误！开服失败！\n错误代码: " + a.Message, "", MessageBoxButton.OK, MessageBoxImage.Question);
                cmdtext.IsEnabled = false;
                fastCMD.IsEnabled = false;
            }
        }

        private void SendCmdToServer(string cmd)
        {
            if (CheckServerRunning())
            {
                if (ConPTYWindow != null)
                {
                    ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm((cmd + "\r\n").AsSpan());
                }
                else
                {
                    if (inputCmdEncoding.Content.ToString() == "UTF8")
                    {
                        SendCmdUTF8(cmd);
                    }
                    else
                    {
                        SendCmdANSL(cmd);
                    }
                }
            }
        }

        private async Task<bool> DownloadAuthlib()
        {
            HttpResponse res = await HttpService.GetAsync("https://authlib-injector.mirrors.mslmc.cn/artifact/latest.json");
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                JObject authlib_jobj = JObject.Parse((string)res.HttpResponseContent);
                if (!File.Exists(Path.Combine(Rserverbase, "authlib-injector.jar")) || !Functions.VerifyFileSHA256(Path.Combine(Rserverbase, "authlib-injector.jar"), authlib_jobj["checksums"]["sha256"].ToString()))
                {
                    //下载或更新authlib-injector.jar
                    bool download_suc = await MagicShow.ShowDownloader(Window.GetWindow(this), authlib_jobj["download_url"].ToString().Replace("authlib-injector.yushi.moe", "authlib-injector.mirrors.mslmc.cn"), Rserverbase, "authlib-injector.jar", "正在更新外置登录库文件...", authlib_jobj["checksums"]["sha256"].ToString());
                    if (!download_suc)
                    {
                        Growl.Error("下载外置登录库文件失败，请检查网络连接或稍后重试！");
                        return false;
                    }
                }
            }
            else
            {
                if (File.Exists(Path.Combine(Rserverbase, "authlib-injector.jar")))
                {
                    LogHelper.Write.Warn("无法获取最新的authlib-injector.jar信息，使用本地文件。" + res.HttpResponseContent);
                }
                else
                {
                    Growl.Error("无法获取最新的authlib-injector.jar信息，且本地没有authlib-injector.jar文件，请检查网络连接或稍后重试！");
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> MCEulaEvent()
        {
            if (Rservermode != 0) // 以自定义命令方式启动时，不执行接受eula事件
                return true;
            string path1 = Rserverbase + "\\eula.txt";
            if (!File.Exists(path1) || (File.Exists(path1) && !File.ReadAllText(path1).Contains("eula=true")))
            {
                var shield = new Shield
                {
                    Command = HandyControl.Interactivity.ControlCommands.OpenLink,
                    CommandParameter = "https://aka.ms/MinecraftEULA",
                    Subject = "https://aka.ms/MinecraftEULA",
                    Status = LanguageManager.Instance["OpenWebsite"]
                };
                bool dialog = await MagicShow.ShowMsgDialogAsync(this, "开启Minecraft服务器需要接受Mojang的EULA，是否仔细阅读EULA条款（https://aka.ms/MinecraftEULA）并继续开服？", "提示", true, "否", "是", shield);
                if (dialog == true)
                {
                    try
                    {
                        File.WriteAllText(path1, string.Empty);
                        FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        StreamReader sr = new StreamReader(fs, Encoding.Default);

                        StreamWriter streamWriter = new StreamWriter(path1);
                        // 写入注释和日期
                        streamWriter.WriteLine("#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://aka.ms/MinecraftEULA).");
                        streamWriter.WriteLine($"#{DateTime.Now.ToString("ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture)}");

                        // 写入eula=true
                        streamWriter.WriteLine("eula=true");
                        streamWriter.Flush();
                        streamWriter.Close();
                        return true;
                    }
                    catch (Exception a)
                    {
                        MessageBox.Show("出现错误，请手动修改eula文件或重试:" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
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

        // 更新子窗口大小
        private async Task UpdateChildWindowSize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                await Task.Delay(250);
                ConPTYWindow.Width = this.ActualWidth - Tab_Home.ActualWidth - 17;
                ConPTYWindow.Height = this.ActualHeight - this.NonClientAreaHeight - 15;
                return;
            }
            ConPTYWindow.Width = this.ActualWidth - Tab_Home.ActualWidth - 10;
            ConPTYWindow.Height = this.ActualHeight - this.NonClientAreaHeight - 15;
        }

        // 更新子窗口位置
        private void UpdateChildWindowPosition()
        {
            //ConPTYWindow.ConptyConsole.ConPTYTerm.CanOutLog = false;
            ConptyCanOutLog = false;
            if (ConPTYResizeTimer.IsEnabled)
            {
                ConPTYResizeTimer.Stop();
            }
            ConPTYResizeTimer.Start();
            if (this.WindowState == WindowState.Maximized)
            {
                // 获取屏幕工作区的宽度和高度
                double screenWidth = SystemParameters.WorkArea.Width;
                double screenHeight = SystemParameters.WorkArea.Height;

                // 计算子窗口居中后的 Left 和 Top 值
                ConPTYWindow.Left = (screenWidth - ConPTYWindow.Width) / 2 + (Tab_Home.ActualWidth / 2);
                ConPTYWindow.Top = (screenHeight - ConPTYWindow.Height) / 2 + (this.NonClientAreaHeight / 2);
            }
            else if (this.WindowState == WindowState.Normal)
            {
                ConPTYWindow.Left = this.Left + (this.Width - ConPTYWindow.Width) / 2 + (Tab_Home.ActualWidth / 2);
                ConPTYWindow.Top = this.Top + (this.Height - ConPTYWindow.Height) / 2 + (this.NonClientAreaHeight / 2);
            }
            else
            {
                return;
            }
            ConptyPopUp.HorizontalOffset += 1;
            ConptyPopUp.HorizontalOffset -= 1;
        }

        private void ConPTYResizeTimer_Tick(object sender, EventArgs e)
        {
            ConptyCanOutLog = true;
            ConPTYResizeTimer.Stop();
        }

        private void ConptyWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Close();
        }

        private void ConptyWindowControlServer(object sender, RoutedEventArgs e)
        {
            if (ConPTYWindow.ControlServer.Content.ToString() == "关服")
            {
                if (ConPTYWindow.ConptyConsole.ConPTYTerm.TermProcIsRunning)
                {
                    ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm("stop\r\n".AsSpan());
                    MagicFlowMsg.ShowMessage("关服中，请稍等……\n双击关服按钮可强制关服（不推荐）", _growlPanel: GetActiveGrowlPanel());
                }
                else
                {
                    ConPTYWindow.ControlServer.Content = "开服";
                    ChangeControlsState(false);
                }
                GetServerInfoLine = 101;
            }
            else
            {
                if (GetServerInfoLine == 102)
                {
                    GetServerInfoLine = 101;
                    return;
                }
                LaunchServer();
                ConPTYWindow.ControlServer.Content = "关服";
            }
        }

        private UIElement GetActiveGrowlPanel()
        {
            if (ConPTYWindow != null)
            {
                if (ConPTYWindow.Visibility == Visibility.Visible)
                {
                    return ConptyGrowlPanel;
                }
                return GrowlPanel;
            }
            else
            {
                return GrowlPanel;
            }
        }

        private async void KillConptyServer(object sender, RoutedEventArgs e)
        {
            if (ConPTYWindow.ControlServer.Content.ToString() == "关服")
            {
                try
                {
                    GetServerInfoLine = 102;
                    ConPTYWindow.ConptyConsole.ConPTYTerm.Process.Process.Kill();
                    await Task.Delay(500);
                    GetServerInfoLine = 101;
                }
                catch { return; }
            }
        }

        private void ShowConptyWindow()
        {
            if (ConPTYWindow != null)
            {
                _ = UpdateChildWindowSize();
                UpdateChildWindowPosition();
                ConPTYWindow.Show();
                ConPTYWindow.Visibility = Visibility.Visible;
                ConPTYWindow.ConptyConsole.Focus();
            }
        }

        private void StartServer(string StartFileArg)
        {
            if (useConpty.IsChecked == true)
            {
                if (ConPTYWindow == null)
                {
                    try
                    {
                        TabCtrl.SelectedIndex = 1;
                        ConPTYWindow = new ConptyWindow();
                        ConPTYWindow.Closing += ConptyWindowClosing;
                        ConPTYWindow.ControlServer.Click += ConptyWindowControlServer;
                        ConPTYWindow.ControlServer.MouseDoubleClick += KillConptyServer;
                        ConPTYWindow.Activated += Window_Activated;
                        ConPTYWindow.Deactivated += Window_Deactivated;
                        ConPTYWindow.serverbase = Rserverbase;
                        if (Rservermode == 0)
                        {
                            ConPTYWindow.java = Rserverjava;
                            ConPTYWindow.launcharg = StartFileArg;
                        }
                        else
                        {
                            ConPTYWindow.java = "cmd.exe";
                            ConPTYWindow.launcharg = "/c " + StartFileArg;
                        }
                        ConPTYWindow.Owner = this;
                        ConPTYWindow.Width = this.ActualWidth - Tab_Home.ActualWidth - 10;
                        ConPTYWindow.Height = this.ActualHeight - this.NonClientAreaHeight - 15;
                        ConPTYWindow.Show();
                        UpdateChildWindowPosition();
                        ConPTYWindow.Focus();
                        ConPTYWindow.ConptyConsole.Focus();
                        ConPTYWindow.StartServer();
                        ConPTYWindow.ConptyConsole.ConPTYTerm.TermExited += OnTermExited;
                        ConPTYWindow.ConptyConsole.ConPTYTerm.OnOutputReceived += ProcessOutputEvent;
                        ChangeControlsState();
                    }
                    catch
                    {
                        Growl.Warning("高级终端（ConPty）启动失败，已自动使用传统终端来开服！");
                        try
                        {
                            try
                            {
                                ConPTYWindow.Closing -= ConptyWindowClosing;
                                ConPTYWindow.ControlServer.Click -= ConptyWindowControlServer;
                                ConPTYWindow.ControlServer.MouseDoubleClick -= KillConptyServer;
                                ConPTYWindow.Close();
                            }
                            finally
                            {
                                ConPTYWindow = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        Tradition_StartServer(StartFileArg);
                    }
                }
                else
                {
                    ConPTYWindow.serverbase = Rserverbase;
                    if (Rservermode == 0)
                    {
                        ConPTYWindow.java = Rserverjava;
                        ConPTYWindow.launcharg = StartFileArg;
                    }
                    else
                    {
                        ConPTYWindow.java = "cmd.exe";
                        ConPTYWindow.launcharg = "/c " + StartFileArg;
                    }
                    if (TabCtrl.SelectedIndex != 1)
                    {
                        TabCtrl.SelectedIndex = 1;
                    }
                    else
                    {
                        ShowConptyWindow();
                    }
                    ConPTYWindow.StartServer2();
                    ConPTYWindow.ConptyConsole.ConPTYTerm.TermExited += OnTermExited;
                    ConPTYWindow.ConptyConsole.ConPTYTerm.OnOutputReceived += ProcessOutputEvent;
                    ChangeControlsState();
                }
            }
            else
            {
                Tradition_StartServer(StartFileArg);
            }
        }

        private void Tradition_StartServer(string StartFileArg)
        {
            try
            {
                Directory.CreateDirectory(Rserverbase);
                
                ServerProcess = new Process();
                ServerProcess.StartInfo.WorkingDirectory = Rserverbase;
                if (Rservermode == 0)
                {
                    ServerProcess.StartInfo.FileName = Rserverjava;
                    ServerProcess.StartInfo.Arguments = StartFileArg;
                }
                else
                {
                    ServerProcess.StartInfo.FileName = "cmd.exe";
                    ServerProcess.StartInfo.Arguments = "/c " + StartFileArg;
                }
                ServerProcess.StartInfo.CreateNoWindow = true;
                ServerProcess.StartInfo.UseShellExecute = false;
                ServerProcess.StartInfo.RedirectStandardInput = true;
                ServerProcess.StartInfo.RedirectStandardOutput = true;
                ServerProcess.StartInfo.RedirectStandardError = true;
                ServerProcess.EnableRaisingEvents = true;
                ServerProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                ServerProcess.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                ServerProcess.Exited += new EventHandler(ServerExitEvent);
                if (outputCmdEncoding.Content.ToString() == "UTF8")
                {
                    ServerProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    ServerProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                }
                else
                {
                    ServerProcess.StartInfo.StandardOutputEncoding = Encoding.Default;
                    ServerProcess.StartInfo.StandardErrorEncoding = Encoding.Default;
                }
                ServerProcess.Start();
                ServerProcess.BeginOutputReadLine();
                ServerProcess.BeginErrorReadLine();
                ChangeControlsState();
            }
            catch (Exception e)
            {
                PrintLog("出现错误，正在检查问题...", Colors.Red);
                if (File.Exists(Rserverjava))
                {
                    PrintLog("Java路径正常", Colors.Green);
                }
                else
                {
                    PrintLog("Java路径有误", Colors.Red);
                }
                if (Directory.Exists(Rserverbase))
                {
                    PrintLog("服务器目录正常", Colors.Green);
                }
                else
                {
                    PrintLog("服务器目录有误", Colors.Red);
                }
                if (File.Exists(Rserverbase + "\\" + Rserverserver))
                {
                    PrintLog("服务端路径正常", Colors.Green);
                }
                else
                {
                    PrintLog("服务端路径有误", Colors.Red);
                }

                PrintLog("错误代码：" + e.Message, Colors.Red);
                MagicShow.ShowMsgDialog(this, "出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！", "错误");
                TabCtrl.SelectedIndex = 1;
            }
        }

        private void ChangeControlsState(bool isEnable = true)
        {
            if (isEnable)
            {
                if (!ServerList.RunningServers.Contains(RserverID))
                {
                    ServerList.RunningServers.Add(RserverID);
                }
                ServerStateChange();
                GetServerInfoLine = 0;
                serverPlayerList.Items.Clear();
                serverStateLab.Content = "运行中";
                serverStateLab.Foreground = Brushes.Red;
                solveProblemBtn.IsEnabled = false;
                controlServer.IsChecked = true;
                controlServer1.IsChecked = true;
                MoreOperation.IsEnabled = false; //服务器完成启动前禁止备份
                gameDifficultyLab.Content = "获取中";
                gameTypeLab.Content = "获取中";
                serverIPLab.Content = "获取中";
                localServerIPLab.Content = "获取中";
                MagicFlowMsg.ShowMessage("开服中，请稍等……", _growlPanel: GetActiveGrowlPanel());
                ClearLog();
                PrintLog("正在开启服务器，请稍等...", ConfigStore.LogColor.INFO);
                if (ConPTYWindow == null)
                {
                    MCSLogHandler._logProcessTimer.IsEnabled = true;
                    MCSLogHandler._logProcessTimer.Start();
                    cmdtext.IsEnabled = true;
                    cmdtext.Text = "";
                    fastCMD.IsEnabled = true;
                    sendcmd.IsEnabled = true;
                }
                else
                {
                    ConPTYWindow.ServerStatus.Text = "运行中";
                }

            }
            else
            {
                if (ServerList.RunningServers.Contains(RserverID))
                {
                    ServerList.RunningServers.Remove(RserverID);
                }
                ServerStateChange();

                serverStateLab.Content = "已关闭";
                serverStateLab.Foreground = Brushes.Green;
                solveProblemBtn.IsEnabled = true;
                controlServer.IsChecked = false;
                controlServer1.IsChecked = false;
                MoreOperation.IsEnabled = true; // 服务器关闭后允许备份
                MagicFlowMsg.ShowMessage("服务器已关闭！", _growlPanel: GetActiveGrowlPanel());
                if (ConPTYWindow == null)
                {
                    sendcmd.IsEnabled = false;
                    cmdtext.IsEnabled = false;
                    fastCMD.IsEnabled = false;
                    cmdtext.Text = "服务器已关闭";
                    MCSLogHandler.CleanupResources();
                    
                }
                else
                {
                    ConPTYWindow.ServerStatus.Text = "已关服";
                }
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                // 将日志添加到缓冲区，不要直接处理（否则UI线程压力很大，可能会使软件崩溃）
                MCSLogHandler._logBuffer.Enqueue(e.Data);
                tempLog = e.Data;
                Dispatcher.InvokeAsync(() =>
                {
                    if (!MCSLogHandler._logProcessTimer.IsEnabled)
                    {
                        MCSLogHandler._logProcessTimer.Start();
                    }
                });
            }
        }

        #region 日志显示功能、彩色日志

        private void PrintLog(string msg, Color defaultColor)
        {
            // 1. 解析出带颜色的片段列表
            var segments = ParseLogSegments(msg, defaultColor);
            if (segments.Count == 0) return;

            Dispatcher.Invoke(() =>
            {
                // 自动清屏
                if (autoClearOutlog.IsChecked == true && _logEntryCount >= MaxLogEntries)
                {
                    ClearLog();
                }

                bool shouldScroll = outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight - 5;

                // 拼接纯文本
                string plainText = string.Concat(segments.Select(s => s.Text));

                // 记录插入位置
                int insertOffset = outlog.Document.TextLength;

                // 如果文档已有内容，先加换行
                if (insertOffset > 0)
                {
                    outlog.Document.Insert(insertOffset, "\n");
                    insertOffset++;
                }

                // 记录本条日志起始 offset
                var entry = new LogEntry
                {
                    StartOffset = insertOffset,
                    Segments = segments
                };

                // 插入纯文本
                outlog.Document.Insert(insertOffset, plainText);

                // 注册着色信息
                _logColorizer.AddEntry(entry);

                // 触发重绘
                outlog.TextArea.TextView.Redraw();

                _logEntryCount++;

                if (shouldScroll)
                    outlog.ScrollToEnd();
            });
        }

        private void ClearLog()
        {
            outlog.Clear();
            _logColorizer.Clear();
            _logEntryCount = 0;
        }

        private List<LogSegment> ParseLogSegments(string msg, Color defaultColor)
        {
            var segments = new List<LogSegment>();

            try
            {
                char delimiter = msg.Contains('&') ? '&' : msg.Contains('§') ? '§' : '\0';

                if (delimiter != '\0')
                {
                    // Minecraft & / § 颜色代码
                    int lastIndex = 0;
                    int firstDelimiterIndex = msg.IndexOf(delimiter);

                    if (firstDelimiterIndex > 0)
                        segments.Add(new LogSegment { Text = msg.Substring(0, firstDelimiterIndex), Color = defaultColor });
                    else if (firstDelimiterIndex == -1)
                    {
                        segments.Add(new LogSegment { Text = msg, Color = defaultColor });
                        return segments;
                    }

                    while ((lastIndex = msg.IndexOf(delimiter, lastIndex)) != -1)
                    {
                        int nextIndex = msg.IndexOf(delimiter, lastIndex + 1);
                        if (nextIndex == -1) nextIndex = msg.Length;

                        string segment = msg.Substring(lastIndex + 1, nextIndex - lastIndex - 1);
                        if (segment.Length > 1)
                        {
                            char code = segment[0];
                            string text = segment.Substring(1);
                            segments.Add(new LogSegment
                            {
                                Text = text,
                                Color = GetColorFromMinecraftCode(code)
                            });
                        }
                        lastIndex = nextIndex;
                        if (lastIndex >= msg.Length) break;
                    }
                }
                else if (msg.Contains("\x1B"))
                {
                    // ANSI 转义码
                    string[] parts = msg.Split(new[] { '\x1B' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        int mIndex = part.IndexOf('m');
                        if (mIndex == -1 || mIndex + 1 >= part.Length) continue;

                        string codesPart = part.Substring(0, mIndex).TrimStart('[');
                        string text = part.Substring(mIndex + 1);
                        if (string.IsNullOrEmpty(text)) continue;

                        bool isBold = false, isUnderline = false;
                        Color foreground = Colors.Green;

                        foreach (var code in codesPart.Split(';'))
                        {
                            switch (code)
                            {
                                case "0": isBold = false; isUnderline = false; foreground = Colors.Green; break;
                                case "1": isBold = true; break;
                                case "4": isUnderline = true; break;
                                default:
                                    if (_ansiColorMap.TryGetValue(code, out var c)) foreground = c;
                                    break;
                            }
                        }
                        segments.Add(new LogSegment { Text = text, Color = foreground, IsBold = isBold, IsUnderline = isUnderline });
                    }
                }
                else
                {
                    segments.Add(new LogSegment { Text = msg, Color = defaultColor });
                }
            }
            catch
            {
                segments.Clear();
                segments.Add(new LogSegment { Text = msg, Color = defaultColor });
            }

            return segments;
        }

        private static readonly Dictionary<char, Color> _mcColorMap = new()
        {
            ['0'] = Colors.Black,
            ['1'] = Colors.DarkBlue,
            ['2'] = Colors.DarkGreen,
            ['3'] = Colors.DarkCyan,
            ['4'] = Colors.DarkRed,
            ['5'] = Colors.DarkMagenta,
            ['6'] = Colors.Orange,
            ['7'] = Colors.Gray,
            ['8'] = Colors.DarkGray,
            ['9'] = Colors.Blue,
            ['a'] = Colors.Green,
            ['b'] = Colors.Cyan,
            ['c'] = Colors.Red,
            ['d'] = Colors.Magenta,
            ['e'] = Colors.Gold,
            ['f'] = Colors.White,
        };

        private static readonly Dictionary<string, Color> _ansiColorMap = new()
        {
            ["30"] = Colors.Black,
            ["31"] = Colors.Red,
            ["32"] = Colors.Green,
            ["33"] = Colors.Gold,
            ["34"] = Colors.Blue,
            ["35"] = Colors.Magenta,
            ["36"] = Colors.Cyan,
            ["37"] = Colors.White,
            ["90"] = Colors.Gray,
            ["91"] = Colors.LightPink,
            ["92"] = Colors.LightGreen,
            ["93"] = Colors.LightYellow,
            ["94"] = Colors.LightBlue,
            ["95"] = Colors.LightPink,
            ["96"] = Colors.LightCyan,
            ["97"] = Colors.White,
        };

        private Color GetColorFromMinecraftCode(char code)
            => _mcColorMap.TryGetValue(code, out var c) ? c : Colors.Green;
        #endregion

        private bool ConptyCanOutLog = true;
        private void ProcessOutputEvent(string msg)
        {
            if (!ConptyCanOutLog)
                return;

            tempLog = msg;
            ProcessOutput(msg);
        }

        private void ProcessOutput(string msg)
        {
            if (msg.Contains("\n"))
            {
                if (msg.Contains("\r"))
                {
                    msg = msg.Replace("\r", string.Empty);
                }
                string[] strings = msg.Split('\n');
                foreach (string s in strings)
                {
                    LogHandleInfo(s);

                    if (MCSLogHandler.ServerService.ProblemSolveSystem)
                        MCSLogHandler.ServerService.ProblemSystemHandle(s);
                }
            }
            else
            {
                LogHandleInfo(msg);
                if (MCSLogHandler.ServerService.ProblemSolveSystem)
                    MCSLogHandler.ServerService.ProblemSystemHandle(msg);
            }
        }

        private bool outlogEncodingAsk = true;
        private void HandleEncodingIssue()
        {
            Color brush = MCSLogHandler.LogInfo[0].Color;
            PrintLog("MSL检测到您的服务器输出了乱码日志，请尝试去“更多功能”界面更改服务器的“输出编码”来解决此问题！", Colors.Red);
            MCSLogHandler.LogInfo[0].Color = brush;
            if (outlogEncodingAsk)
            {
                outlogEncodingAsk = false;
                string encoding = "UTF8";
                if (outputCmdEncoding.Content.ToString().Contains("UTF8"))
                {
                    encoding = "ANSI";
                }
                Growl.Ask(new GrowlInfo
                {
                    Message = "MSL检测到您的服务器输出了乱码日志，是否将服务器输出编码更改为“" + encoding + 
                    "”？\n点击确定后将自动更改编码并重启服务器（注意：软件会强制关闭服务器进程，若害怕服务器数据丢失，可先手动关服，然后再点击确定按钮）。",
                    ActionBeforeClose = isConfirmed =>
                    {
                        if (isConfirmed)
                        {
                            JObject jsonObject = JObject.Parse(File.ReadAllText("MSL\\ServerList.json", Encoding.UTF8));
                            JObject _json = (JObject)jsonObject[RserverID.ToString()];
                            _json["encoding_out"] = encoding;
                            jsonObject[RserverID.ToString()] = _json;
                            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                            Dispatcher.InvokeAsync(() =>
                            {
                                outputCmdEncoding.Content = encoding;
                                Growl.Success("更改完毕！");
                            });
                            Task.Run(async () =>
                            {
                                GetServerInfoLine = 102;
                                await Task.Delay(100);
                                try
                                {
                                    if (ServerProcess != null && ServerProcess.HasExited == false)
                                        ServerProcess.Kill();
                                }
                                catch { }
                                await Task.Delay(200);
                                Dispatcher.Invoke(() =>
                                {
                                    RestartServer();
                                });
                            });
                        }
                        return true;
                    },
                    ShowDateTime = false
                });
            }
        }

        private bool recordPlayInfo = false;
        private void LogHandleInfo(string msg)
        {
            if ((msg.Contains("Done") && msg.Contains("For help")) || (msg.Contains("加载完成") && msg.Contains("如需帮助") || (msg.Contains("Server started."))))
            {
                GetServerInfoLine = 101;
                Dispatcher.InvokeAsync(() =>
                {
                    PrintLog("已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情查看开服器的内网映射界面。\r\n若控制台输出乱码日志，请去更多功能界面修改“输出编码”。", ConfigStore.LogColor.INFO);
                    MagicFlowMsg.ShowMessage(string.Format("服务器 {0} 已成功开启！", Rservername), 1, _growlPanel: GetActiveGrowlPanel());
                    serverStateLab.Content = "已开服";
                    if (ConPTYWindow != null)
                    {
                        ConPTYWindow.ServerStatus.Text = "已开服";
                    }
                    GetServerInfoSys();
                    MoreOperation.IsEnabled = true;
                });
            }
            else if (msg.Contains("Stopping server"))
            {
                Dispatcher.InvokeAsync(() =>
                {
                    PrintLog("正在关闭服务器！", ConfigStore.LogColor.INFO);
                });

            }

            //玩家进服是否记录
            if (recordPlayInfo == true)
            {
                GetPlayerInfoSys(msg);
            }
        }

        private void LogHandleWarn(string msg)
        {
            if (msg.Contains("FAILED TO BIND TO PORT"))
            {
                PrintLog("警告：由于端口占用，服务器已自动关闭！请检查您的服务器是否多开或者有其他软件占用端口！\r\n解决方法：您可尝试通过重启电脑解决！", Colors.Red);
            }
            else if (msg.Contains("Unable to access jarfile"))
            {
                PrintLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Colors.Red);
            }
            else if (msg.Contains("加载 Java 代理时出错"))
            {
                PrintLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Colors.Red);
            }
        }

        private void GetServerInfoSys()
        {
            try
            {
                Encoding encoding = Functions.GetTextFileEncodingType(Rserverbase + @"\server.properties");
                string config = File.ReadAllText(Rserverbase + @"\server.properties", encoding);
                if (config.Contains("\r"))
                {
                    config = config.Replace("\r", string.Empty);
                }
                int om1 = config.IndexOf("online-mode=") + 12;
                string om2 = config.Substring(om1);
                string onlineMode = om2.Substring(0, om2.IndexOf("\n"));
                if (onlineMode == "true")
                {
                    if (string.IsNullOrEmpty(RserverYggAddr))
                    {
                        PrintLog("检测到您没有关闭正版验证，如果客户端为离线登录的话，请点击“更多功能”里“关闭正版验证”按钮以关闭正版验证。否则离线账户将无法进入服务器！", Colors.OrangeRed);
                    }
                    else
                    {
                        PrintLog("检测到您正在使用第三方外置登录验证，请确保客户端均采用第三方外置登录进入游戏，否则将无法进入服务器哦！", Colors.OrangeRed);
                    }
                    onlineModeLab.Content = "已开启";
                }
                else if (onlineMode == "false")
                {
                    if (string.IsNullOrEmpty(RserverYggAddr))
                    {
                        PrintLog("检测到您关闭了正版验证，若没有采取相关措施来保护服务器（如添加登录插件等），服务器会有被入侵的风险，请务必注意！", Colors.OrangeRed);
                    }
                    else
                    {
                        PrintLog("检测到您配置了外置登录且关闭了正版验证，这样做是无效的！！！请您打开正版验证！！！", Colors.Red);
                    }
                    onlineModeLab.Content = "已关闭";
                }
                string[] strings1 = config.Split('\n');
                foreach (string s in strings1)
                {
                    if (s.StartsWith("gamemode="))
                    {
                        gameTypeLab.Content = s.Substring(9);
                        break;
                    }
                }
                int dc1 = config.IndexOf("difficulty=") + 11;
                string dc2 = config.Substring(dc1);
                gameDifficultyLab.Content = dc2.Substring(0, dc2.IndexOf("\n"));
                int ip1 = config.IndexOf("server-ip=") + 10;
                string ip2 = config.Substring(ip1);
                string serverIP = ip2.Substring(0, ip2.IndexOf("\n"));

                int sp1 = config.IndexOf("server-port=") + 12;
                string sp2 = config.Substring(sp1);
                string serverPort = sp2.Substring(0, sp2.IndexOf("\n"));
                serverIPLab.Content = serverIP + ":" + serverPort;
                if (string.IsNullOrEmpty(serverIP))
                {
                    serverIP = "127.0.0.1";
                }
                if (serverPort == "25565")
                {
                    serverPort = string.Empty;
                }
                else
                {
                    serverPort = ":" + serverPort;
                }
                localServerIPLab.Content = serverIP + serverPort;
            }
            catch
            {
                Growl.Info("开服器在获取服务器信息时出现错误！此问题不影响服务器运行，您可继续正常使用或将此问题报告给作者！");
            }
        }

        private void GetPlayerInfoSys(string msg)
        {
            Regex disconnectRegex = new Regex(@"\s*]: (\S+)\s*lost connection:");
            Regex serverDisconnectRegex = new Regex(@"\s*]: (\S+)\s*与服务器失去连接");

            if (msg.Contains("logged in with entity id"))
            {
                string playerName = ExtractPlayerName(msg);
                if (playerName != null)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (!serverPlayerList.Items.Contains(playerName))
                        {
                            serverPlayerList.Items.Add(playerName);
                        }
                    });
                }
                else
                {
                    return;
                }
            }
            else if (disconnectRegex.IsMatch(msg))
            {
                string playerName = disconnectRegex.Match(msg).Groups[1].Value;
                RemovePlayerFromList(playerName);
            }
            else if (serverDisconnectRegex.IsMatch(msg))
            {
                string playerName = serverDisconnectRegex.Match(msg).Groups[1].Value;
                RemovePlayerFromList(playerName);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// 从日志消息中提取出用户标识字符串。
        /// 例如：
        ///   输入: "[21:58:19 INFO]: Weheal[/127.0.0.1:25565] logged in with entity id 100 at (...)" 
        ///   输出: "Weheal[/127.0.0.1:25565]"
        ///   
        ///   输入: "[22:59:55] [Server thread/INF0]: Weheal[/[0000:0000:0000:0000:0000:0000:0000:0000]:25565] logged in with entity id 100 at(...)" 
        ///   输出: "Weheal[/[0000:0000:0000:0000:0000:0000:0000:0000]:25565]"
        /// </summary>
        public string ExtractPlayerName(string msg)
        {
            // 定位登录标志所在位置
            int endIndex = msg.IndexOf(" logged in with entity id");
            if (endIndex == -1)
            {
                // 找不到登录标志，返回 null 或者其他错误处理方式
                return null;
            }

            // 定位 "]: " 分隔符，它出现在前面的时间戳和其它信息之后，紧接着用户标识
            string delimiter = "]: ";
            int startIndex = msg.LastIndexOf(delimiter, endIndex);
            if (startIndex == -1)
            {
                // 如果没有找到分隔符，也返回 null
                return null;
            }

            // 实际的用户标识开始于分隔符之后
            startIndex += delimiter.Length;

            // 截取 startIndex 到 endIndex 之间的子字符串
            return msg.Substring(startIndex, endIndex - startIndex);
        }


        private void RemovePlayerFromList(string playerName)
        {
            try
            {
                Dispatcher.InvokeAsync(() =>
                {
                    foreach (string x in serverPlayerList.Items)
                    {
                        if (x.StartsWith(playerName + "[/"))
                        {
                            serverPlayerList.Items.Remove(x);
                            break;
                        }
                    }
                });
            }
            catch
            {
                Growl.Error("好像出现了点错误……");
            }
        }

        private void ServerExitEvent(object sender, EventArgs e)//Tradition_ServerExitEvent
        {
            try
            {
                ServerProcess.CancelOutputRead();
                ServerProcess.CancelErrorRead();
            }
            catch
            { return; }
            ServerProcess.OutputDataReceived -= OutputDataReceived;
            ServerProcess.ErrorDataReceived -= OutputDataReceived;
            ServerProcess.Exited -= ServerExitEvent;
            int exitCode = ServerProcess.ExitCode;
            ServerProcess.Dispose();
            ServerProcess = null;
            Dispatcher.InvokeAsync(async () =>
            {
                ChangeControlsState(false);
                if (MCSLogHandler.ServerService.ProblemSolveSystem)
                {
                    MCSLogHandler.ServerService.ProblemSolveSystem = false;
                    if (string.IsNullOrEmpty(MCSLogHandler.ServerService.ProblemFound))
                    {
                        MagicShow.ShowMsgDialog(this, "服务器已关闭！开服器未检测到相关问题，您可将服务器日志发送给他人以寻求帮助！\n日志发送方式：\n1.直接截图控制台内容\n2.服务器目录\\logs\\latest.log\n3.前往“更多功能”界面上传至Internet", "崩溃分析系统");
                    }
                    else
                    {
                        Growl.Info("服务器已关闭！即将为您展示分析报告！");
                        MagicShow.ShowMsgDialog(this, MCSLogHandler.ServerService.ProblemFound + "\nPS:软件检测不一定准确，若您无法解决，可将服务器日志发送给他人以寻求帮助，但请不要截图此弹窗！！！\n日志发送方式：\n1.直接截图控制台内容\n2.服务器目录\\logs\\latest.log\n3.前往“更多功能”界面上传至Internet", "服务器分析报告");
                        MCSLogHandler.ServerService.Dispose();
                    }
                }
                else if (exitCode != 0 && GetServerInfoLine <= 100)
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "服务器疑似异常关闭，是您人为关闭的吗？\n您可使用MSL的崩溃分析系统进行检测，也可使用AI日志分析功能或将服务器日志发送给他人以寻求帮助！\n注意:请不要截图此弹窗！！！\nAI日志分析入口：服务器控制台的更多操作栏，或“更多功能”页面里。\n服务器日志在何处：\n1.服务器控制台内容；2.服务器目录\\logs\\latest.log；3.“更多功能”界面将日志上传至Internet。\n\n点击确定开始进行崩溃分析", "提示", true);
                    if (dialogRet)
                    {
                        TabCtrl.SelectedIndex = 1;
                        MCSLogHandler.ServerService.ProblemSolveSystem = true;
                        LaunchServer();
                    }
                }
                else if (autoStartserver.IsChecked == true)
                {
                    Console.WriteLine("服务器已关闭，触发重启事件...");
                    await Task.Delay(200);
                    RestartServer();
                }
            });
        }

        private void RestartServer()
        {
            MagicFlowMsg.ShowAskMessage(
                message: "服务器已关闭。倒计时结束后将自动重启，\n您也可以提前点击按钮操作。",
                callback: confirmed =>
                {
                    if (confirmed)
                    {
                        if (MCSLogHandler != null && ServerProcess == null)
                        {
                            MagicFlowMsg.ShowMessage("服务器正在重启...", type: 1);
                            LaunchServer();
                        }
                    }
                    else
                    {
                        // 用户主动取消
                        MagicFlowMsg.ShowMessage("已取消本次服务器重启", type: 3);
                    }
                },
                waitSeconds: 5,
                titleText:"服务器已关闭，将自动重启。",
                confirmText: "立即重启",
                cancelText: "取消重启",
                container: GrowlPanel
            );
        }

        #region ConptyExitEvent

        private void OnTermExited(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToUITerminal("服务器已关闭！".AsSpan());
                ChangeControlsState(false);
                ConPTYWindow.ConptyConsole.ConPTYTerm.TermExited -= OnTermExited;
                ConPTYWindow.ConptyConsole.ConPTYTerm.OnOutputReceived -= ProcessOutputEvent;
                ConPTYWindow.ControlServer.Content = "开服";
                if (MCSLogHandler.ServerService.ProblemSolveSystem)
                {
                    MCSLogHandler.ServerService.ProblemSolveSystem = false;
                    string[] strings = (ConPTYWindow.ConptyConsole.ConPTYTerm.GetConsoleText()).Split('\n');
                    foreach (var log in strings)
                    {
                        MCSLogHandler.ServerService.ProblemSystemHandle(log);
                    }

                    bool isCVisible = false;
                    if (ConPTYWindow.Visibility == Visibility.Visible)
                    {
                        isCVisible = true;
                        ConPTYWindow.Visibility = Visibility.Collapsed;
                    }
                    if (string.IsNullOrEmpty(MCSLogHandler.ServerService.ProblemFound))
                    {
                        await MagicShow.ShowMsgDialogAsync(this, "服务器已关闭！开服器未检测到相关问题，您可将服务器日志发送给他人以寻求帮助！若并未输出任何日志，请尝试关闭伪终端再试（更多功能界面）！\n日志发送方式：\n1.直接截图控制台内容\n2.服务器目录\\logs\\latest.log\n3.前往“更多功能”界面上传至Internet", "崩溃分析系统");
                    }
                    else
                    {
                        Growl.Info("服务器已关闭！即将为您展示分析报告！");
                        await MagicShow.ShowMsgDialogAsync(this, MCSLogHandler.ServerService.ProblemFound + "\nPS:软件检测不一定准确，若您无法解决，可将服务器日志发送给他人以寻求帮助，但请不要截图此弹窗！！！\n日志发送方式：\n1.直接截图控制台内容\n2.服务器目录\\logs\\latest.log\n3.前往“更多功能”界面上传至Internet", "服务器分析报告");
                        MCSLogHandler.ServerService.Dispose();
                    }
                    if (isCVisible)
                        ShowConptyWindow();
                }
                else if (GetServerInfoLine <= 100)
                {
                    bool isCVisible = false;
                    if (ConPTYWindow.Visibility == Visibility.Visible)
                    {
                        isCVisible = true;
                        ConPTYWindow.Visibility = Visibility.Collapsed;
                    }
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "服务器疑似异常关闭，是您人为关闭的吗？\n您可使用MSL的崩溃分析系统进行检测，也可将服务器日志发送给他人以寻求帮助，但请不要截图此弹窗！！！\n日志发送方式：\n1.直接截图控制台内容\n2.服务器目录\\logs\\latest.log\n3.前往“更多功能”界面上传至Internet\n\n点击确定开始进行崩溃分析", "提示", true);
                    if (dialogRet)
                    {
                        MCSLogHandler.ServerService.ProblemSolveSystem = true;
                        LaunchServer();
                    }
                    else
                    {
                        if (isCVisible)
                            ShowConptyWindow();
                    }
                }
                else if (autoStartserver.IsChecked == true)
                {
                    await Task.Delay(200);
                    LaunchServer();
                }
            });
        }
        #endregion

        private void SendCommand()
        {
            try
            {
                string inputText = cmdtext.Text.Trim();
                if (string.IsNullOrEmpty(inputText)) return;

                string finalCmd = inputText;

                // 解析输入的内容：分离第一个词(可能是别名) 和 剩余参数
                string[] parts = inputText.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    string firstWord = parts[0];

                    if (fastCMD.SelectedIndex == 0)
                    {
                        // 检查下拉框是否选择“/”，且输入的第一个词是否含有别名（忽略大小写）
                        var aliasMatch = CurrentFastCmds.FirstOrDefault(c =>
                            !string.IsNullOrEmpty(c.Alias) &&
                            c.Alias.Equals(firstWord, StringComparison.OrdinalIgnoreCase));

                        if (aliasMatch != null)
                        {
                            string args = parts.Length > 1 ? parts[1] : "";
                            // 清理“/”
                            finalCmd = $"{aliasMatch.Cmd.Trim().TrimStart('/')} {args}".Trim();
                        }
                    }
                    else if (fastCMD.SelectedIndex > 0 && fastCMD.SelectedItem is FastCommandInfo selectedCmd)
                    {
                        // 没触发别名，且下拉框选择了某个快捷指令
                        finalCmd = $"{selectedCmd.Cmd.Trim().TrimStart('/')} {inputText}".Trim();
                    }
                }

                // 发送命令
                if (inputCmdEncoding.Content.ToString() == "UTF8")
                {
                    SendCmdUTF8(finalCmd);
                }
                else
                {
                    SendCmdANSL(finalCmd);
                }
            }
            catch
            {
                // 异常回退
                fastCMD.SelectedIndex = 0;
                if (inputCmdEncoding.Content.ToString() == "UTF8") SendCmdUTF8(cmdtext.Text);
                else SendCmdANSL(cmdtext.Text);
            }
        }

        private void SendCmdUTF8(string cmd)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(cmd);
            ServerProcess.StandardInput.BaseStream.Write(utf8Bytes, 0, utf8Bytes.Length);
            ServerProcess.StandardInput.WriteLine();
            cmdtext.Text = "";
        }
        private void SendCmdANSL(string cmd)
        {
            ServerProcess.StandardInput.WriteLine(cmd);
            cmdtext.Text = "";
        }
        private void sendcmd_Click(object sender, RoutedEventArgs e)
        {
            SendCommand();
        }

        private void cmdtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendCommand();
            }
        }

        private void controlServer_Click(object sender, RoutedEventArgs e)
        {
            var _sender = sender as ToggleButton;
            if (_sender.IsChecked == true)
            {
                controlServer.IsChecked = false;
                controlServer1.IsChecked = false;
                if (GetServerInfoLine == 102)
                {
                    GetServerInfoLine = 101;
                    return;
                }
                LaunchServer();
            }
            else
            {
                controlServer.IsChecked = true;
                controlServer1.IsChecked = true;
                if (ConPTYWindow != null)
                {
                    ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm("stop\r\n".AsSpan());
                    if (TabCtrl.SelectedIndex != 1)
                    {
                        TabCtrl.SelectedIndex = 1;
                    }
                    else
                    {
                        ShowConptyWindow();
                    }
                }
                else
                {
                    MagicFlowMsg.ShowMessage("关服中，请耐心等待……\n双击按钮可强制关服（不建议）", _growlPanel: GetActiveGrowlPanel());
                    ServerProcess.StandardInput.WriteLine("stop");
                }

                GetServerInfoLine = 101;
            }
        }

        private async void controlServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var _sender = sender as ToggleButton;
                if (_sender.IsChecked == true)
                {
                    GetServerInfoLine = 102;
                    if (ConPTYWindow != null)
                    {
                        try
                        {
                            ConPTYWindow.ConptyConsole.ConPTYTerm.Process.Process.Kill();
                        }
                        catch { }
                    }
                    else
                    {
                        ServerProcess.Kill();
                    }
                }
            }
            catch { }
            await Task.Delay(500);
            GetServerInfoLine = 101;
        }

        private async void MoreOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MoreOperation.SelectedIndex)
            {
                case 1:
                    MoreOperation.IsEnabled = false;
                    await BackupWorld();
                    MoreOperation.IsEnabled = true;
                    break;
                case 2:
                    OpenAILogAnalyseDialog();
                    break;
            }
            MoreOperation.SelectedIndex = 0;
        }
        #endregion

        #region 插件mod管理

        ///////////这里是插件mod管理

        void ReFreshPluginsAndMods()
        {
            // 检测plugins文件夹是否存在
            if (Directory.Exists(Rserverbase + @"\plugins"))
            {
                pluginsTabItem.Content = ManagePluginsCard;
                List<SR_PluginInfo> list = new List<SR_PluginInfo>();
                DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\plugins");
                FileInfo[] file = directoryInfo.GetFiles("*.*");
                foreach (FileInfo f in file)
                {
                    if (f.Name.EndsWith(".disabled"))
                    {
                        list.Add(new SR_PluginInfo(f.Name.Replace(".disabled", "")) { IsDisabled = true });
                    }
                    else if (f.Name.EndsWith(".jar"))
                    {
                        list.Add(new SR_PluginInfo(f.Name) { IsDisabled = false });
                    }
                }
                pluginslist.ItemsSource = list;
            }
            else
            {
                NoPlugins tips = new NoPlugins();
                tips.RefreshCommand = new RelayCommand(o => ReFreshPluginsAndMods());
                pluginsTabItem.Content = tips;
            }

            // 检测mods文件夹是否存在
            if (Directory.Exists(Rserverbase + @"\mods"))
            {
                modsTabItem.Content = ManageModsCard;
                List<SR_ModInfo> list = new List<SR_ModInfo>();
                DirectoryInfo directoryInfo1 = new DirectoryInfo(Rserverbase + @"\mods");
                FileInfo[] file1 = directoryInfo1.GetFiles("*.*");

                foreach (FileInfo f1 in file1)
                {
                    if (f1.Name.EndsWith(".disabled"))
                    {
                        list.Add(new SR_ModInfo(f1.Name.Replace(".disabled", ""), false) { IsDisabled = true });
                    }
                    else if (f1.Name.EndsWith(".jar"))
                    {
                        bool isClient = IsClientSideMod(f1.FullName);
                        list.Add(new SR_ModInfo(f1.Name, isClient) { IsDisabled = false });
                    }
                }
                modslist.ItemsSource = list;
            }
            else
            {
                NoMods tips = new NoMods();
                tips.RefreshCommand = new RelayCommand(o => ReFreshPluginsAndMods());
                modsTabItem.Content = tips;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                ListViewItem item = Functions.FindAncestor<ListViewItem>(button);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            if (button != null && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }

        private async void addModsTip_Click(object sender, RoutedEventArgs e)
        {
            bool dialog = await MagicShow.ShowMsgDialogAsync(this, "服务器需要添加的模组和客户端要添加的模组有所不同，增加方块、实体、玩法的MOD，是服务器需要安装的（也就是服务端和客户端都需要安装），而小地图、皮肤补丁、输入补丁、优化MOD、视觉显示类的MOD，服务器是一定不需要安装的（也就是只能加在客户端里）\n点击确定查看详细区分方法", "提示", true, "取消");
            if (dialog)
            {
                Process.Start("https://zhidao.baidu.com/question/927720370906860259.html");
            }
        }

        private void openpluginsDir_Click(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = Rserverbase + @"\plugins";
            p.Start();
        }

        private void openmodsDir_Click(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = Rserverbase + @"\mods";
            p.Start();
        }

        private void reFresh_Click(object sender, RoutedEventArgs e)
        {
            ReFreshPluginsAndMods();
        }

        private void addPlugin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Multiselect = true;
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    int i = 0;
                    foreach (var file in openfile.FileNames)
                    {
                        File.Copy(file, Rserverbase + @"\plugins\" + openfile.SafeFileNames[i]);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                ReFreshPluginsAndMods();
            }
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Multiselect = true;
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    int i = 0;
                    foreach (var file in openfile.FileNames)
                    {
                        File.Copy(file, Rserverbase + @"\mods\" + openfile.SafeFileNames[i]);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                ReFreshPluginsAndMods();
            }
        }

        private void disPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }

                if (pluginslist.SelectedItems.Count == 0)
                {
                    MagicFlowMsg.ShowMessage("请先选择至少一个插件！", 3);
                    return;
                }

                foreach (SR_PluginInfo info in pluginslist.SelectedItems)
                {
                    string path = Rserverbase + @"\plugins\";
                    if (!info.IsDisabled) 
                    {
                        // 原名 -> 原名.disabled
                        File.Move(path + info.PluginName, path + info.PluginName + ".disabled");
                    }
                    else // 当前是禁用状态 -> 去启用
                    {
                        // 原名.disabled -> 原名
                        File.Move(path + info.PluginName + ".disabled", path + info.PluginName);
                    }
                }
                ReFreshPluginsAndMods();
            }
            catch { return; }
        }

        private void delPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }

                if (pluginslist.SelectedItems.Count == 0)
                {
                    MagicFlowMsg.ShowMessage("请先选择至少一个插件！", 3);
                    return;
                }

                foreach (SR_PluginInfo info in pluginslist.SelectedItems)
                {
                    string path = Rserverbase + @"\plugins\";
                    // 如果是禁用的，文件名后面有 .disabled，否则就是原名
                    string fileName = info.IsDisabled ? info.PluginName + ".disabled" : info.PluginName;
                    File.Delete(path + fileName);
                }
                ReFreshPluginsAndMods();
            }
            catch { return; }
        }

        private void disMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }

                if (modslist.SelectedItems.Count == 0)
                {
                    MagicFlowMsg.ShowMessage("请先选择至少一个模组！", 3);
                    return;
                }

                foreach (SR_ModInfo info in modslist.SelectedItems)
                {
                    string path = Rserverbase + @"\mods\";
                    if (!info.IsDisabled)
                    {
                        // 当前是启用 -> 禁用 (文件名加 .disabled)
                        if (File.Exists(path + info.ModName))
                            File.Move(path + info.ModName, path + info.ModName + ".disabled");
                    }
                    else
                    {
                        // 当前是禁用 -> 启用 (文件名去 .disabled)
                        if (File.Exists(path + info.ModName + ".disabled"))
                            File.Move(path + info.ModName + ".disabled", path + info.ModName);
                    }
                }

                ReFreshPluginsAndMods();
            }
            catch { return; }
        }
        private void delMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }

                if (modslist.SelectedItems.Count == 0)
                {
                    MagicFlowMsg.ShowMessage("请先选择至少一个模组！", 3);
                    return;
                }

                foreach (SR_ModInfo info in modslist.SelectedItems)
                {
                    string path = Rserverbase + @"\mods\";
                    string fileName = info.IsDisabled ? info.ModName + ".disabled" : info.ModName;

                    if (File.Exists(path + fileName))
                    {
                        File.Delete(path + fileName);
                    }
                }

                ReFreshPluginsAndMods();
            }
            catch { return; }
        }

        private void disAllPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }
            }
            catch { }
            foreach (SR_PluginInfo info in pluginslist.Items)
            {
                string path = Rserverbase + @"\plugins\";
                if (!info.IsDisabled)
                {
                    if (File.Exists(path + info.PluginName))
                        File.Move(path + info.PluginName, path + info.PluginName + ".disabled");
                }
                else
                {
                    if (File.Exists(path + info.PluginName + ".disabled"))
                        File.Move(path + info.PluginName + ".disabled", path + info.PluginName);
                }
            }
            ReFreshPluginsAndMods();
        }

        private void disAllMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }
            }
            catch { }
            foreach (SR_ModInfo info in modslist.Items)
            {
                string path = Rserverbase + @"\mods\";
                if (!info.IsDisabled)
                {
                    if (File.Exists(path + info.ModName))
                        File.Move(path + info.ModName, path + info.ModName + ".disabled");
                }
                else
                {
                    if (File.Exists(path + info.ModName + ".disabled"))
                        File.Move(path + info.ModName + ".disabled", path + info.ModName);
                }
            }
            ReFreshPluginsAndMods();
        }

        private void DownloadModBtn_Click(object sender, RoutedEventArgs e)
        {
            DownloadMod downloadMod = new DownloadMod(Rserverbase + "\\mods", 0, 0, false)
            {
                Owner = this
            };
            downloadMod.ShowDialog();
            ReFreshPluginsAndMods();
        }

        private void DownloadPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            DownloadMod downloadMod = new DownloadMod(Rserverbase + "\\plugins", 1, 2, false)
            {
                Owner = this
            };
            downloadMod.ShowDialog();
            ReFreshPluginsAndMods();
        }

        // 检测客户端模组
        private async void detectClientMods_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Rserverbase + @"\mods"))
            {
                MagicShow.ShowMsgDialog(this, "未找到mods文件夹！", "错误");
                return;
            }

            addModBtn.IsEnabled = false;

            // 异步执行检测
            var resultList = await Task.Run(() =>
            {
                List<SR_ModInfo> list = new List<SR_ModInfo>();
                DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\mods");
                FileInfo[] files = directoryInfo.GetFiles("*.*");

                // 临时存放客户端模组和普通模组
                List<SR_ModInfo> clientMods = new List<SR_ModInfo>();
                List<SR_ModInfo> normalMods = new List<SR_ModInfo>();
                List<SR_ModInfo> disabledMods = new List<SR_ModInfo>();

                foreach (FileInfo f in files)
                {
                    if (f.Name.EndsWith(".disabled"))
                    {
                        disabledMods.Add(new SR_ModInfo(f.Name.Replace(".disabled", ""), false) { IsDisabled = true });
                    }
                    else if (f.Name.EndsWith(".jar"))
                    {
                        // 检测是否为客户端模组
                        if (IsClientSideMod(f.FullName))
                        {
                            clientMods.Add(new SR_ModInfo(f.Name, true));
                        }
                        else
                        {
                            normalMods.Add(new SR_ModInfo(f.Name, false));
                        }
                    }
                }

                // 合并列表：客户端模组排在最前面
                list.AddRange(clientMods);
                list.AddRange(normalMods);
                list.AddRange(disabledMods);

                return list;
            });

            // 更新 UI
            modslist.ItemsSource = resultList;
            addModBtn.IsEnabled = true;

            // 自动选择
            modslist.SelectedItems.Clear(); 
            foreach (var mod in resultList)
            {
                if (mod.IsClient && !mod.IsDisabled)
                {
                    modslist.SelectedItems.Add(mod);
                }
            }

            // 统计提示
            int clientCount = resultList.Count(x => x.IsClient);
            if (clientCount > 0)
            {
                MagicShow.ShowMsgDialog(this, $"检测完成！发现 {clientCount} 个仅客户端模组（已标记为橙色）。\n建议不要将这些模组上传到服务器。", "检测结果");
            }
            else
            {
                MagicFlowMsg.ShowMessage("未检测到明确声明为仅客户端的模组。", 3);
            }
        }

        /// <summary>
        /// 检测 jar 文件是否为仅客户端模组
        /// </summary>
        private bool IsClientSideMod(string filePath)
        {
            ZipFile zip = null;
            try
            {
                zip = new ZipFile(filePath);

                // --- fabric.mod.json ---
                ZipEntry fabricEntry = zip.GetEntry("fabric.mod.json");
                if (fabricEntry != null)
                {
                    using (Stream stream = zip.GetInputStream(fabricEntry))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonContent = reader.ReadToEnd();
                        try
                        {
                            var json = JObject.Parse(jsonContent);
                            var env = json["environment"]?.ToString();

                            if ("client".Equals(env, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        catch { /* 不管qwq */ }
                    }
                }

                // --- Forge/NeoForge (mods.toml)  ---
                ZipEntry tomlEntry = zip.GetEntry("META-INF/mods.toml");
                if (tomlEntry == null)
                {
                    tomlEntry = zip.GetEntry("META-INF/neoforge.mods.toml");
                }

                if (tomlEntry != null)
                {
                    using (Stream stream = zip.GetInputStream(tomlEntry))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();

                        // 分块解析
                        var blocks = Regex.Matches(content, @"(?ms)^\[\[.*?\]\](.*?)(?=^\[\[|\z)");

                        string minecraftSide = null;
                        string firstFoundSide = null;

                        foreach (Match block in blocks)
                        {
                            string blockBody = block.Groups[1].Value;

                            // 在块内匹配 modId 和 side
                            var modIdMatch = Regex.Match(blockBody, @"^\s*modId\s*=\s*[""'](.*?)[""']", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                            var sideMatch = Regex.Match(blockBody, @"^\s*side\s*=\s*[""'](.*?)[""']", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                            if (sideMatch.Success)
                            {
                                string currentSide = sideMatch.Groups[1].Value;

                                // 记录遇到的第一个 side (fallback)
                                if (firstFoundSide == null)
                                {
                                    firstFoundSide = currentSide;
                                }

                                // 匹配 modId = minecraft，则将其作为最高优先级并跳出
                                if (modIdMatch.Success && "minecraft".Equals(modIdMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
                                {
                                    minecraftSide = currentSide;
                                    break;
                                }
                            }
                        }

                        // 优先使用 minecraft 的 side，如果没有，则使用找到的第一个 side
                        string finalSide = minecraftSide ?? firstFoundSide;

                        if (finalSide != null && "CLIENT".Equals(finalSide, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 奇怪的错误
                return false;
            }
            finally
            {
                // 释放文件锁
                if (zip != null)
                {
                    zip.IsStreamOwner = true;
                    zip.Close();
                }
            }

            return false;
        }
        #endregion

        #region 服务器设置

        //////////////////////这里是服务器设置界面

        private void LoadSettings()
        {
            try
            {
                //检测是否自定义模式
                if (Rservermode == 1)
                {
                    LabelArgsText.Content = "自定义启动参数:";
                    GridServerCore.Visibility = Visibility.Collapsed;
                    GridJavaSet.Visibility = Visibility.Collapsed;
                    GridJavaRem.Visibility = Visibility.Collapsed;
                    DivJavaSet.Visibility = Visibility.Collapsed;
                    DivJvmSet.Visibility = Visibility.Collapsed;
                    DivRemSet.Visibility = Visibility.Collapsed;
                    DivYggdrasilSet.Visibility = Visibility.Collapsed;
                    GridYggdrasilSet.Visibility = Visibility.Collapsed;
                    TextArgsTips.Text = "提示：您正在使用自定义参数模式哦~";
                }
                else
                {
                    LabelArgsText.Content = "服务器JVM参数:";
                    GridServerCore.Visibility = Visibility.Visible;
                    GridJavaSet.Visibility = Visibility.Visible;
                    GridJavaRem.Visibility = Visibility.Visible;
                    DivJavaSet.Visibility = Visibility.Visible;
                    DivJvmSet.Visibility = Visibility.Visible;
                    DivRemSet.Visibility = Visibility.Visible;
                    TextArgsTips.Text = "提示：一般格式为 -参数，如 -Dlog4j2.formatMsgNoLookups=true，非必要无需填写";
                }
                nAme.Text = Rservername;
                server.Text = Rserverserver;
                memorySlider.Maximum = Functions.GetPhysicalMemoryMB();
                bAse.Text = Rserverbase;
                jVMcmd.Text = RserverJVMcmd;
                jAva.Text = Rserverjava;

                Task.Run(LoadJavaInfo);

                if (RserverJVM == "")
                {
                    memorySlider.IsEnabled = false;
                    autoSetMemory.IsChecked = true;
                    memoryInfo.Text = "内存：自动分配";
                }
                else
                {
                    memorySlider.IsEnabled = true;
                    autoSetMemory.IsChecked = false;
                    try
                    {
                        int minMemoryIndex = RserverJVM.IndexOf("-Xms");
                        int maxMemoryIndex = RserverJVM.IndexOf("-Xmx");

                        int minMemory = 0;
                        int maxMemory = 0;

                        if (minMemoryIndex != -1) // 确保 -Xms 存在
                        {
                            string minMemorySubstring = RserverJVM.Substring(minMemoryIndex + 4);
                            string minMemoryValue = ExtractMemoryValue(minMemorySubstring);
                            int.TryParse(minMemoryValue, out minMemory);
                        }

                        if (maxMemoryIndex != -1) // 确保 -Xmx 存在
                        {
                            string maxMemorySubstring = RserverJVM.Substring(maxMemoryIndex + 4);
                            string maxMemoryValue = ExtractMemoryValue(maxMemorySubstring);
                            int.TryParse(maxMemoryValue, out maxMemory);
                        }

                        memorySlider.ValueStart = minMemory;
                        memorySlider.ValueEnd = maxMemory;
                        memoryInfo.Text = $"最小:{minMemory}M, 最大:{maxMemory}M";
                    }
                    catch (Exception ex)
                    {
                        memorySlider.ValueStart = 0;
                        memorySlider.ValueEnd = 0;
                        memoryInfo.Text = "解析内存参数失败";
                        Console.WriteLine("错误: " + ex.Message);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error!");
            }
        }

        private string ExtractMemoryValue(string memoryString)
        {
            int endIndex = memoryString.IndexOf("M");
            bool isGB = false;

            if (endIndex == -1)
            {
                endIndex = memoryString.IndexOf("G");
                isGB = true;
            }

            if (endIndex != -1)
            {
                string valueStr = memoryString.Substring(0, endIndex);
                if (int.TryParse(valueStr, out int value))
                {
                    return isGB ? (value * 1024).ToString() : value.ToString();
                }
            }

            return "0"; // 如果解析失败，返回0
        }

        private async Task LoadJavaInfo()
        {
            try
            {
                try
                {
                    JObject keyValuePairs = new JObject((JObject)JsonConvert.DeserializeObject(File.ReadAllText("MSL\\config.json")));
                    Dispatcher.Invoke(() =>
                    {
                        if (keyValuePairs["javaList"] != null)
                        {
                            selectCheckedJavaComb.ItemsSource = null;
                            selectCheckedJavaComb.ItemsSource = keyValuePairs["javaList"];
                            selectCheckedJavaComb.SelectedIndex = 0;
                        }
                        if (jAva.Text == "Java")
                        {
                            useJvpath.IsChecked = true;
                        }
                    });
                }
                catch
                {
                    Console.WriteLine("Load Local-Java-List Failed(From Config)");
                }

                for (int i = 0; i <= 10; i++)
                {
                    if (ConfigStore.ApiLink == null)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        break;
                    }
                }
                string response = (await HttpService.GetApiContentAsync("query/jdk?os=windows&arch=x64"))["data"].ToString();
                JArray jArray = JArray.Parse(response);
                List<string> list = new List<string>();
                foreach (var j in jArray)
                {
                    list.Add(j.ToString());
                }
                Dispatcher.Invoke(() =>
                {
                    selectJava.ItemsSource = list;
                    selectJava.SelectedIndex = 0;
                });
            }
            catch
            {
                Console.WriteLine("Failed to get Java-Version List");
            }
            Dispatcher.Invoke(() =>
            {
                if (jAva.Text != "Java")
                {
                    // 使用正则表达式来提取Java版本
                    Regex pattern = new Regex(@"(?:MSL\\)?(Java\d+)");
                    Match m = pattern.Match(jAva.Text);
                    string javaVersion = m.Groups[1].Value;

                    foreach (var item in selectJava.Items)
                    {
                        if (item.ToString() == javaVersion)
                        {
                            // 如果有相等的，就把selectJava切换到相应的栏
                            useDownJv.IsChecked = true;
                            selectJava.SelectedItem = item;
                            break;
                        }
                    }
                }
            });
        }

        private void refreahConfig_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private async void doneBtn1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "服务器运行时无法更改服务器设置！", "错误");
                    return;
                }
            }
            catch { }
            try
            {
                doneBtn1.IsEnabled = false;
                refreahConfig.IsEnabled = false;
                if (autoSetMemory.IsChecked == true)
                {
                    RserverJVM = "";
                }
                else
                {
                    RserverJVM = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                }
                if (Rservermode == 0)
                {
                    if (useDownJv.IsChecked == true)
                    {
                        Growl.Info("获取Java地址……");
                        int dwnJava = 0;
                        try
                        {
                            dwnJava = await DownloadJava(selectJava.SelectedValue.ToString(), (await HttpService.GetApiContentAsync("download/jdk/" + selectJava.SelectedValue.ToString() + "?os=windows&arch=x64"))["data"]["url"].ToString());
                            if (dwnJava == 1)
                            {
                                MagicDialog dialog = new MagicDialog();
                                dialog.ShowTextDialog(this, "解压中……");
                                bool unzipJava = await UnzipJava(selectJava.SelectedValue.ToString());
                                dialog.CloseTextDialog();
                                if (!unzipJava)
                                {
                                    MagicShow.ShowMsgDialog(this, "安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误");
                                    doneBtn1.IsEnabled = true;
                                    refreahConfig.IsEnabled = true;
                                    return;
                                }
                                Growl.Info("Java下载完成！");
                            }
                            else if (dwnJava == 2)
                            {
                                Growl.Success("完成！");
                            }
                            else
                            {
                                MagicShow.ShowMsgDialog(this, "下载取消！", "提示");
                                doneBtn1.IsEnabled = true;
                                refreahConfig.IsEnabled = true;
                                return;
                            }
                        }
                        catch
                        {
                            Growl.Error("出现错误，请检查网络连接！");
                            doneBtn1.IsEnabled = true;
                            refreahConfig.IsEnabled = true;
                            return;
                        }
                    }
                    else if (useSelf.IsChecked == true)
                    {
                        if (!Path.IsPathRooted(jAva.Text))
                        {
                            jAva.Text = AppDomain.CurrentDomain.BaseDirectory.ToString() + jAva.Text;
                        }
                        Growl.Info("正在检查所选Java可用性，请稍等……");
                        (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync(jAva.Text);
                        if (javaAvailability)
                        {
                            Growl.Success("检测完毕，Java可用！\n" + "版本：" + javainfo);
                        }
                        else
                        {
                            MagicShow.ShowMsgDialog(this, "检测Java可用性失败，您的Java似乎不可用！请检查是否选择正确！", "错误");
                            doneBtn1.IsEnabled = true;
                            refreahConfig.IsEnabled = true;
                            return;
                        }
                    }
                    else if (usecheckedjv.IsChecked == true)
                    {
                        string a = selectCheckedJavaComb.Items[selectCheckedJavaComb.SelectedIndex].ToString();
                        jAva.Text = a.Substring(a.IndexOf(":") + 2);
                    }
                    else// (useJvpath.IsChecked == true)
                    {
                        jAva.Text = "Java";
                    }
                }

                //Directory.CreateDirectory(bAse.Text);
                doneBtn1.IsEnabled = true;
                refreahConfig.IsEnabled = true;
                Rservername = nAme.Text;
                Title = Rservername;
                Rserverjava = jAva.Text;
                string fullFileName;
                if (File.Exists(Rserverbase + "\\" + server.Text))
                {
                    fullFileName = Rserverbase + "\\" + server.Text;
                }
                else
                {
                    fullFileName = server.Text;
                }
                if (Functions.CheckForgeInstaller(fullFileName))
                {
                    bool dialog = await MagicShow.ShowMsgDialogAsync(this, "您选择的服务端是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                    if (dialog)
                    {
                        string installReturn;
                        //调用新版forge安装器
                        string[] installForge = await MagicShow.ShowInstallForge(this, Rserverbase, server.Text, Rserverjava);
                        if (installForge[0] == "0")
                        {
                            if (await MagicShow.ShowMsgDialogAsync(this, "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
                            {
                                installReturn = Functions.InstallForge(Rserverjava, Rserverbase, server.Text, string.Empty, false);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (installForge[0] == "1")
                        {
                            string _ret = Functions.InstallForge(Rserverjava, Rserverbase, server.Text, installForge[1]);
                            if (_ret == null)
                            {
                                installReturn = Functions.InstallForge(Rserverjava, Rserverbase, server.Text, installForge[1], false);
                            }
                            else
                            {
                                installReturn = _ret;
                            }
                        }
                        else if (installForge[0] == "3")
                        {
                            installReturn = Functions.InstallForge(Rserverjava, Rserverbase, server.Text, string.Empty, false);
                        }
                        else
                        {
                            return;
                        }
                        if (installReturn == null)
                        {
                            MagicShow.ShowMsgDialog(this, "下载失败！", "错误");
                            return;
                        }
                        server.Text = installReturn;
                    }
                }
                Rserverserver = server.Text;
                if (Rserverbase != bAse.Text)
                {
                    bool dialog = await MagicShow.ShowMsgDialogAsync(this, "检测到您更改了服务器目录，是否将当前的服务器目录移动至新的目录？", "警告", true, "取消");
                    if (dialog)
                    {
                        Functions.MoveFolder(Rserverbase, bAse.Text);
                    }
                }
                Rserverbase = bAse.Text;
                RserverJVMcmd = jVMcmd.Text;

                //粗略检测外置登录地址的合法性
                if (YggdrasilAddr.Text.Length > 0 && !YggdrasilAddr.Text.Contains("http://") && !YggdrasilAddr.Text.Contains("https://"))
                {
                    MagicShow.ShowMsgDialog(this, "外置登录地址不合法！请检查地址是否正确！", "错误");
                    doneBtn1.IsEnabled = true;
                    refreahConfig.IsEnabled = true;
                    return;
                }
                else
                {
                    RserverYggAddr = YggdrasilAddr.Text;
                }

                // 检查备份相关设置参数的合法性
                try
                {
                    if (int.Parse(TextBackupMaxLimitCount.Text) < 0)
                    {
                        throw new Exception("最大备份数量必须大于等于0！");
                    }
                    if (int.Parse(TextBackupDelay.Text) < 5)
                    {
                        throw new Exception("备份保存延时必须大于等于5秒！");
                    }
                    if (ComboBackupPath.SelectedIndex == 2)
                    {
                        if (String.IsNullOrEmpty(TextBackupPath.Text) || TextBackupPath.Text.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        {
                            throw new Exception("自定义备份路径不合法！");
                        }
                        Path.GetFullPath(TextBackupPath.Text); // 这个东西能检测路径合法不 不合法会抛出异常~
                    }
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(this, "备份设置参数有误，请检查！\n" + ex.Message, "错误");
                    doneBtn1.IsEnabled = true;
                    refreahConfig.IsEnabled = true;
                    return;
                }

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID.ToString()];
                _json["name"].Replace(Rservername);
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                _json["core"].Replace(Rserverserver);
                _json["memory"].Replace(RserverJVM);
                _json["args"].Replace(RserverJVMcmd);
                _json["ygg_api"] = RserverYggAddr;
                _json["backup_mode"] = ComboBackupPath.SelectedIndex;
                _json["backup_max_limit"] = int.Parse(TextBackupMaxLimitCount.Text);
                _json["backup_custom_path"] = TextBackupPath.Text;
                _json["backup_save_delay"] = int.Parse(TextBackupDelay.Text);
                jsonObject[RserverID.ToString()].Replace(_json);
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                LoadSettings();
                SaveConfigEvent();

                MagicShow.ShowMsgDialog(this, "保存完毕！", "信息");
            }
            catch (Exception err)
            {
                MessageBox.Show("出现错误！请重试:\n" + err.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                doneBtn1.IsEnabled = true;
                refreahConfig.IsEnabled = true;
            }
        }


        private async Task<int> DownloadJava(string fileName, string downUrl)
        {
            jAva.Text = AppDomain.CurrentDomain.BaseDirectory + "MSL\\" + fileName + "\\bin\\java.exe";
            if (File.Exists(@"MSL\" + fileName + @"\bin\java.exe"))
            {
                return 2;
            }
            else
            {
                bool downDialog = await MagicShow.ShowDownloader(this, downUrl, "MSL", "Java.zip", "下载" + fileName + "中……");
                if (downDialog)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private async Task<bool> UnzipJava(string DownjavaName)
        {
            try
            {
                string javaDirName = "";
                using (ZipFile zip = new ZipFile(@"MSL\Java.zip"))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        if (entry.IsDirectory == true)
                        {
                            int c0 = entry.Name.Length - entry.Name.Replace("/", "").Length;
                            if (c0 == 1)
                            {
                                javaDirName = entry.Name.Replace("/", "");
                                break;
                            }
                        }
                    }
                }
                FastZip fastZip = new FastZip();
                await Task.Run(() => fastZip.ExtractZip(@"MSL\Java.zip", "MSL", ""));
                File.Delete(@"MSL\Java.zip");
                if (@"MSL\" + javaDirName != @"MSL\" + DownjavaName)
                {
                    Functions.MoveFolder(@"MSL\" + javaDirName, @"MSL\" + DownjavaName);
                }
                while (!File.Exists(@"MSL\" + DownjavaName + @"\bin\java.exe"))
                {
                    await Task.Delay(1000);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void a0_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bAse.Text = dialog.SelectedPath;
            }
        }

        private async void a01_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Title = "请选择文件，通常为*.jar",
                Filter = "JAR文件|*.jar|所有文件类型|*.*"
            };
            var res = openfile.ShowDialog();
            if (res == true)
            {
                server.Text = openfile.FileName;
                if (File.Exists(Rserverbase + "\\" + openfile.SafeFileName))
                {
                    server.Text = openfile.SafeFileName;
                }
                else
                {
                    if (Path.GetDirectoryName(openfile.FileName) != Rserverbase)
                    {
                        if (await MagicShow.ShowMsgDialogAsync(this, "所选的服务端核心文件并不在服务器目录中，是否将其复制进服务器目录？\n若不复制，请留意勿将核心文件删除！", "提示", true))
                        {
                            File.Copy(openfile.FileName, Rserverbase + @"\" + openfile.SafeFileName, true);
                            MagicShow.ShowMsgDialog(this, "已将服务端核心复制到了服务器目录之中，您现在可以将源文件删除了！", "提示");
                            server.Text = openfile.SafeFileName;
                        }
                    }
                }
            }
        }

        private void a03_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件，通常为java.exe";
            openfile.Filter = "EXE文件|*.exe|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                jAva.Text = openfile.FileName;
            }
        }

        private void downloadServer_Click(object sender, RoutedEventArgs e)
        {
            if (jVMcmd.Text.Contains("@libraries/net/minecraftforge/forge/"))
            {
                jVMcmd.Clear();
            }
            DownloadServer downloadServer = new DownloadServer(Rserverbase, DownloadServer.Mode.ChangeServerSettings, Rserverjava)
            {
                Owner = this
            };
            downloadServer.ShowDialog();
            if (downloadServer.FileName != null)
            {
                if (File.Exists(Rserverbase + @"\" + downloadServer.FileName))
                {
                    server.Text = downloadServer.FileName;
                    Growl.Success("服务端下载完毕！已自动选择该服务端核心，请记得保存哦~");
                }
                else if (downloadServer.FileName.StartsWith("@libraries/"))
                {
                    server.Text = downloadServer.FileName;
                    Growl.Success("服务端下载完毕！已自动选择该服务端核心，请记得保存哦~");
                }
            }
            downloadServer.Dispose();
        }

        private void autoSetMemory_Click(object sender, RoutedEventArgs e)
        {
            if (autoSetMemory.IsChecked == true)
            {
                memorySlider.IsEnabled = false;
                memoryInfo.Text = "内存：自动分配";
            }
            else
            {
                memorySlider.IsEnabled = true;
                memoryInfo.Text = "最小:" + memorySlider.ValueStart.ToString("f0") + "M," + "最大:" + memorySlider.ValueEnd.ToString("f0") + "M";
            }
        }
        private void memorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DoubleRange> e)
        {
            memoryInfo.Text = "最小:" + memorySlider.ValueStart.ToString("f0") + "M," + "最大:" + memorySlider.ValueEnd.ToString("f0") + "M";
        }
        private void memoryInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (autoSetMemory.IsChecked == false)
                {
                    if (memoryInfo.IsFocused == true)
                    {
                        try
                        {
                            string a = memoryInfo.Text.Substring(0, memoryInfo.Text.IndexOf(","));
                            string b = memoryInfo.Text.Substring(memoryInfo.Text.IndexOf(","));
                            string resultA = Regex.Replace(a, @"[^0-9]+", "");
                            string resultB = Regex.Replace(b, @"[^0-9]+", "");
                            memorySlider.ValueStart = double.Parse(resultA);
                            memorySlider.ValueEnd = double.Parse(resultB);
                        }
                        catch { }
                    }
                }
            }
        }
        private async void useJvpath_Click(object sender, RoutedEventArgs e)
        {
            if (useJvpath.IsChecked == true)
            {
                Growl.Info("正在检查环境变量可用性，请稍等……");
                (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync("java");
                if (javaAvailability)
                {
                    Growl.Success("检查完毕，您的环境变量正常！");
                    useJvpath.Content = "使用环境变量：" + javainfo;
                }
                else
                {
                    MagicShow.ShowMsgDialog(this, "检测失败，您的环境变量似乎不存在！", "错误");
                }
            }
        }

        private void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            if (selectCheckedJavaComb.Items.Count == 0)
            {
                MagicShow.ShowMsgDialog(this, "请先进行搜索！", "警告");
                useSelf.IsChecked = true;
            }
        }

        private async void ScanJava_Click(object sender, RoutedEventArgs e)
        {
            List<JavaScanner.JavaInfo> strings = null;
            int dialog = MagicShow.ShowMsg(this, "即将开始检测电脑上的Java，此过程可能需要一些时间，请耐心等待。\n目前有两种检测模式，一种是简单检测，只检测一些关键目录，用时较少，普通用户可优先使用此模式。\n第二种是深度检测，将检测所有磁盘的所有目录，耗时可能会很久，请慎重选择！", "提示", true, "开始深度检测", "开始简单检测");
            if (dialog == 2)
            {
                return;
            }
            Dialog waitDialog = Dialog.Show(new TextDialog("检测中，请稍等……"));
            JavaScanner javaScanner = new();
            if (dialog == 1)
            {
                await Task.Run(async () => { Thread.Sleep(200); strings = await javaScanner.ScanJava(); });
            }
            else
            {
                await Task.Run(() => { Thread.Sleep(200); strings = javaScanner.SearchJava(); });
            }
            this.Focus();
            waitDialog.Close();

            if (strings != null)
            {
                var javaList = strings.Select(info => $"Java{info.Version}: {info.Path}").ToList();
                selectCheckedJavaComb.ItemsSource = javaList;
                try
                {
                    JObject keyValuePairs = new JObject((JObject)JsonConvert.DeserializeObject(File.ReadAllText("MSL\\config.json")));
                    JArray jArray = new JArray(javaList);
                    if (keyValuePairs["javaList"] == null)
                    {
                        keyValuePairs.Add("javaList", jArray);
                    }
                    else
                    {
                        keyValuePairs["javaList"] = jArray;
                    }
                    File.WriteAllText("MSL\\config.json", Convert.ToString(keyValuePairs), Encoding.UTF8);
                }
                catch
                {
                    Console.WriteLine("Write Local-Java-List Failed(From Configuration)");
                }
            }
            if (selectCheckedJavaComb.Items.Count > 0)
            {
                Growl.Success("检查完毕！");
                selectCheckedJavaComb.SelectedIndex = 0;
            }
            else
            {
                Growl.Error("暂未找到Java");
            }
        }

        private async void getLaunchercode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string content;
                if (Rservermode == 0)
                {
                    string ygg_api_jvm = "";
                    // 处理外置登录
                    if (!string.IsNullOrEmpty(RserverYggAddr))
                    {
                        ygg_api_jvm = $"-javaagent:authlib-injector.jar={RserverYggAddr} ";
                        if (!await DownloadAuthlib())
                        {
                            return; // 下载authlib失败，退出
                        }
                    }
                    if (Rserverserver.StartsWith("@libraries/"))
                    {
                        content = "@ECHO OFF\r\n\"" + Rserverjava + "\" " + ygg_api_jvm + RserverJVM + " " + RserverJVMcmd + " " + Rserverserver + " nogui" + "\r\npause";
                    }
                    else
                    {
                        content = "@ECHO OFF\r\n\"" + Rserverjava + "\" " + ygg_api_jvm + RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverserver + "\" nogui" + "\r\npause";
                    }
                }
                else
                {
                    content = "@ECHO OFF\r\n\"" + RserverJVMcmd + "\r\npause";
                }


                string filePath = Path.Combine(Rserverbase, "StartServer.bat");
                File.WriteAllText(filePath, content, Encoding.Default);
                MessageBox.Show("脚本文件：" + Rserverbase + @"\StartServer.bat", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start("explorer.exe", Rserverbase);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 快捷设置ygg api
        private void YggLittleskin_Click(object sender, RoutedEventArgs e)
        {
            YggdrasilAddr.Text = "https://littleskin.cn/api/yggdrasil";
            //Growl.Success("已设置Yggdrasil服务为Littleskin\n请点击保存并重启服务器以使设置生效！", "提示");
        }

        private void YggDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/advanced/yggdrasil/");
        }

        private void YggMSL_Click(object sender, RoutedEventArgs e)
        {
            YggdrasilAddr.Text = "https://skin.mslmc.net/api/yggdrasil";
        }
        #endregion

        #region 更多功能

        ////////这里是更多功能界面

        //获取ipv6地址
        private async void GetIPV6_Click(object sender, RoutedEventArgs e)
        {
            GetIPV6.IsEnabled = false;
            Growl.Info("获取中，请稍后……");
            try
            {
                HttpResponse response = await HttpService.GetAsync("https://6.ipw.cn");
                if (response?.HttpResponseCode == HttpStatusCode.OK)
                {
                    string ipv6 = response?.HttpResponseContent.ToString();
                    Clipboard.Clear();
                    Clipboard.SetText(ipv6);
                    MagicShow.ShowMsgDialog(this, $"您的IPV6公网地址是：{ipv6}\n已经帮您复制到剪贴板啦！\n注意：IPV6地址格式是：[IP]:端口\n若无法使用IPV6连接，请检查：\n-连接方是否有IPV6地址\n-防火墙是否拦截", "成功获取IPV6公网地址！");
                }
                else
                {
                    throw new Exception(response?.HttpResponseContent.ToString());
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "出现错误，您当前的网络可能没有IPV6支持\n您可上网搜索IPV6开启教程或联系运营商以获取帮助\n错误信息：" + ex.Message, "获取IPV6地址失败！");
            }
            finally
            {
                GetIPV6.IsEnabled = true;
            }
        }

        private void autostartServer_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (autoStartserver.IsChecked == true)
            {
                _json["autostartServer"] = "True";
            }
            else
            {
                _json["autostartServer"] = "False";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private void inputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (inputCmdEncoding.Content.ToString() == "ANSI")
            {
                inputCmdEncoding.Content = "UTF8";
                _json["encoding_in"] = "UTF8";
            }
            else if (inputCmdEncoding.Content.ToString() == "UTF8")
            {
                inputCmdEncoding.Content = "ANSI";
                _json["encoding_in"] = "ANSI";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            //Growl.Success("编码更改已生效！");
            MagicFlowMsg.ShowMessage("编码更改已生效！", 1);
        }

        private void outputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (outputCmdEncoding.Content.ToString() == "ANSI")
            {
                outputCmdEncoding.Content = "UTF8";
                _json["encoding_out"] = "UTF8";
            }
            else if (outputCmdEncoding.Content.ToString() == "UTF8")
            {
                outputCmdEncoding.Content = "ANSI";
                _json["encoding_out"] = "ANSI";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            try
            {
                if (ServerProcess != null && !ServerProcess.HasExited)
                {
                    MagicFlowMsg.ShowMessage("编码已更改，重启服务器后生效！", 3);
                    
                }
                else
                {
                    MagicFlowMsg.ShowMessage("编码更改已生效！", 1);
                }
            }
            catch
            {
                MagicFlowMsg.ShowMessage("编码更改已生效！", 1);
            }
        }
        private void fileforceUTF8encoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (fileforceUTF8encoding.IsChecked == false)
            {
                _json["fileforceUTF8"] = "False";
            }
            else
            {
                _json["fileforceUTF8"] = "True";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private void useConpty_Click(object sender, RoutedEventArgs e)
        {
            if (CheckServerRunning())
            {
                MagicShow.ShowMsgDialog(this, "请关闭服务器后再进行更改！", "提示");
                if (useConpty.IsChecked == false)
                {
                    useConpty.IsChecked = true;
                }
                else
                {
                    useConpty.IsChecked = false;
                }
                return;
            }
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (useConpty.IsChecked == false)
            {
                Tradition_LogFunGrid.Visibility = Visibility.Visible;
                Tradition_LogFunDivider.Visibility = Visibility.Visible;
                Tradition_CMDCard.Visibility = Visibility.Visible;
                _json["useConpty"] = "False";
                try
                {
                    if (ConPTYWindow != null)
                    {
                        try
                        {
                            ConPTYWindow.Closing -= ConptyWindowClosing;
                            ConPTYWindow.ControlServer.Click -= ConptyWindowControlServer;
                            ConPTYWindow.ControlServer.MouseDoubleClick -= KillConptyServer;
                            ConPTYWindow.Close();
                        }
                        finally
                        {
                            ConPTYWindow = null;
                        }
                    }
                }
                catch { }
            }
            else
            {
                Tradition_LogFunGrid.Visibility = Visibility.Collapsed;
                Tradition_LogFunDivider.Visibility = Visibility.Collapsed;
                Tradition_CMDCard.Visibility = Visibility.Collapsed;
                _json["useConpty"] = "True";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private async void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "检测到服务器正在运行，点击确定以关闭服务器", "信息");
                    if (!dialogRet)
                    {
                        return;
                    }
                    ServerProcess.StandardInput.WriteLine("stop");
                }
                try
                {
                    string path1 = Rserverbase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = Rserverbase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(this, "修改完毕，请重新开启服务器！", "信息");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，您确定您的服务器启动过一次吗？请手动修改server.properties文件或重试:" + a.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch
            {
                try
                {
                    string path1 = Rserverbase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = Rserverbase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(this, "修改完毕，请重新开启服务器！", "信息");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，您确定您的服务器启动过一次吗？请手动修改server.properties文件或重试:" + a.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void showOutlog_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (showOutlog.IsChecked == true)
            {
                MCSLogHandler.IsShowOutLog = true;
                _json["showOutlog"] = "True";
            }
            else
            {
                MCSLogHandler.IsShowOutLog = false;
                _json["showOutlog"] = "False";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private void formatOutHead_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (formatOutHead.IsChecked == true)
            {
                MCSLogHandler.IsFormatLogPrefix = true;
                _json["formatOutPrefix"] = true;
            }
            else
            {
                MCSLogHandler.IsFormatLogPrefix = false;
                _json["formatOutPrefix"] = false;
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private void shieldLogBtn_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (shieldLogBtn.IsChecked == true)
            {
                if (ShieldLogList.Items.Count > 0)
                {
                    List<string> tempList = new List<string>();

                    JArray jArray = new JArray();
                    foreach (var item in ShieldLogList.Items)
                    {
                        tempList.Add(item.ToString());
                        jArray.Add(item.ToString());
                    }

                    MCSLogHandler.ShieldLog = [.. tempList];
                    _json["shieldLogKeys"] = jArray;

                    LogShield_Add.IsEnabled = false;
                    LogShield_Del.IsEnabled = false;
                }
                else
                {
                    MagicFlowMsg.ShowMessage("请先进行添加！", 2);
                    shieldLogBtn.IsChecked = false;
                }
            }
            else
            {
                MCSLogHandler.ShieldLog = null;
                _json.Remove("shieldLogKeys");
                LogShield_Add.IsEnabled = true;
                LogShield_Del.IsEnabled = true;
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private async void LogShield_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(this, "输入你想屏蔽的关键字，\n开服器将不会输出含有此关键字的日志");
            if ((!string.IsNullOrEmpty(text)) && (!ShieldLogList.Items.Contains(text)))
            {
                ShieldLogList.Items.Add(text);
            }
        }

        private void LogShield_Del_Click(object sender, RoutedEventArgs e)
        {
            if (ShieldLogList.SelectedIndex != -1)
            {
                ShieldLogList.Items.Remove(ShieldLogList.SelectedItem);
            }
        }

        private void highLightLogBtn_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (highLightLogBtn.IsChecked == true)
            {
                if (HighLightLogList.Items.Count > 0)
                {
                    List<string> tempList = new List<string>();

                    JArray jArray = new JArray();
                    foreach (var item in HighLightLogList.Items)
                    {
                        tempList.Add(item.ToString());
                        jArray.Add(item.ToString());
                    }

                    MCSLogHandler.HighLightLog = [.. tempList];
                    _json["highLightLogKeys"] = jArray;

                    LogHighLight_Add.IsEnabled = false;
                    LogHighLight_Del.IsEnabled = false;
                }
                else
                {
                    MagicFlowMsg.ShowMessage("请先进行添加！", 2);
                    highLightLogBtn.IsChecked = false;
                }
            }
            else
            {
                MCSLogHandler.HighLightLog = null;
                _json.Remove("highLightLogKeys");
                LogHighLight_Add.IsEnabled = true;
                LogHighLight_Del.IsEnabled = true;
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private async void LogHighLight_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(this, "输入你想高亮日志的关键字");
            if ((!string.IsNullOrEmpty(text)) && (!HighLightLogList.Items.Contains(text)))
            {
                HighLightLogList.Items.Add(text);
            }
        }

        private void LogHighLight_Del_Click(object sender, RoutedEventArgs e)
        {
            if (HighLightLogList.SelectedIndex != -1)
            {
                HighLightLogList.Items.Remove(HighLightLogList.SelectedItem);
            }
        }

        private void shieldStackOut_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (shieldStackOut.IsChecked == false)
            {
                MCSLogHandler.IsShieldStackOut = false;
                _json["shieldStackOut"] = "False";
            }
            else
            {
                MCSLogHandler.IsShieldStackOut = true;
                _json["shieldStackOut"] = "True";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private async void autoClearOutlog_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (autoClearOutlog.IsChecked == false)
            {
                bool msgreturn = await MagicShow.ShowMsgDialogAsync(this,
                    "关闭此功能后，服务器输出界面超过一定数量的日志后将不再清屏，这样可能会造成性能损失，您确定要继续吗？",
                    "警告", true, "取消");
                if (msgreturn)
                {
                    _json["autoClearOutlog"] = "False";
                }
                else
                {
                    autoClearOutlog.IsChecked = true;
                }
            }
            else
            {
                _json["autoClearOutlog"] = "True";
            }
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private void logsAnalyse_Click(object sender, RoutedEventArgs e)
        {
            OpenAILogAnalyseDialog();
        }

        private void OpenAILogAnalyseDialog()
        {
            LogAnalysisDialog logAnalysisDialog = new LogAnalysisDialog(this, Rserverbase, Rserverserver);
            Dialog dialog = Dialog.Show(logAnalysisDialog);
            dialog.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            dialog.VerticalContentAlignment = VerticalAlignment.Stretch;
            logAnalysisDialog.SelfDialog = dialog;
        }

        #region 上传日志到mclo.gs

        private async void shareLog_Click(object sender, RoutedEventArgs e)
        {
            shareLog.IsEnabled = false;
            Growl.Info("请稍等……");
            string logs = string.Empty;
            string uploadMode = "A";
            if (File.Exists(Rserverbase + "\\logs\\latest.log"))
            {
                FileStream fileStream = new FileStream(Rserverbase + "\\logs\\latest.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader streamReader = new StreamReader(fileStream);
                try
                {
                    logs = streamReader.ReadToEnd();
                }
                catch
                {
                    string[] strings = GetLogOtherPlan();
                    uploadMode = strings[0];
                    logs = strings[1];
                }
                finally
                {
                    fileStream.Dispose();
                    streamReader.Dispose();
                }
            }
            else
            {
                string[] strings = GetLogOtherPlan();
                uploadMode = strings[0];
                logs = strings[1];
            }

            if (string.IsNullOrEmpty(logs))
            {
                Growl.Info("日志为空，请重试！");
                shareLog.IsEnabled = true;
                return;
            }
            Growl.Info("正在上传，模式 " + uploadMode + "，请稍等……");
            //启动线程上传日志
            await UploadLogs(logs, true);
            shareLog.IsEnabled = true;
        }

        private string[] GetLogOtherPlan()
        {
            string[] strings = new string[2];
            if (useConpty.IsChecked == true)
            {
                if (ConPTYWindow != null)
                {
                    strings[0] = "B";
                    strings[1] = ConPTYWindow.ConptyConsole.ConPTYTerm.GetConsoleText();

                }
            }
            else
            {
                strings[0] = "C";
                strings[1] = outlog.Text;
            }
            return strings;
        }

        private async Task UpdateLogOtherPlan()
        {
            Growl.Info("请稍等……");
            string logs = string.Empty;
            string uploadMode = "A";

            string[] strings = GetLogOtherPlan();
            uploadMode = strings[0];
            logs = strings[1];

            if (string.IsNullOrEmpty(logs))
            {
                Growl.Info("日志为空，请重试！");
                shareLog.IsEnabled = true;
                return;
            }
            Growl.Info("正在上传，模式 " + uploadMode + "，请稍等……");
            //启动线程上传日志
            await UploadLogs(logs);
        }

        private async Task UploadLogs(string logs, bool canUseOtherPlan = false)
        {
            string customUrl = "https://api.mclo.gs/1/log";
            //请求内容
            string parameterData = "content=" + logs;

            var response = await HttpService.PostAsync(customUrl, HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (response.HttpResponseCode == HttpStatusCode.OK)
            {
                try
                {
                    //解析返回的东东
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response.HttpResponseContent.ToString());

                    if (jsonResponse.success == true)
                    {
                        Clipboard.Clear();
                        Clipboard.SetText(jsonResponse.url.ToString());
                        Growl.Success("日志地址: " + jsonResponse.url + "\n已经复制到剪贴板啦！\n如果遇到问题且不会看日志,\n请把链接粘贴给别人寻求帮助，\n记得要详细描述你的问题哦！");
                    }
                    else
                    {
                        Growl.Error("请求失败: " + jsonResponse.error);
                    }
                }
                catch
                {
                    Growl.Error("解析失败");
                }
            }
            else
            {
                if (canUseOtherPlan)
                {
                    if ((await MagicShow.ShowMsgDialogAsync(this, "请求失败：可能由于日志过大，请尝试手动上传日志或使用其他模式！\n" + response.HttpResponseCode + " " + response.HttpResponseContent + "\n点击确定将使用其他模式进行上传", "错误", true) == true))
                    {
                        await UpdateLogOtherPlan();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Growl.Error("请求失败: 可能日志过大，请尝试手动上传日志！\n" + response.HttpResponseCode + " " + response.HttpResponseContent);
                    return;
                }
            }
        }

        //上传Forge安装日志
        private async void forgeInstallLogUpload_Click(object sender, RoutedEventArgs e)
        {
            string logsContent = "";
            try
            {
                if (File.Exists(Path.Combine(Rserverbase, "msl-installForge.log")))
                {
                    logsContent = "[MSL端处理日志]\n" + File.ReadAllText(Path.Combine(Rserverbase, "msl-installForge.log"));
                }
                if (File.Exists(Path.Combine(Rserverbase, "msl-compileForge.log")))
                {
                    logsContent = logsContent + "\n[Java端编译日志]\n" + File.ReadAllText(Path.Combine(Rserverbase, "msl-compileForge.log"));
                }
                if (logsContent == "")
                {
                    Growl.Error("未找到Forge安装日志！");
                }
                else
                {
                    //启动线程上传日志
                    await UploadLogs(logsContent);
                    Growl.Info("正在上传···");
                }
            }
            catch (Exception ex)
            {
                Growl.Error("Forge安装日志上传失败！" + ex.Message);
            }
        }

        #endregion

        private void GetFastCmd()
        {
            CurrentFastCmds.Clear();
            // 添加默认根命令
            CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/", Remark = "指令" });

            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];

            if (_json["fastcmd"] != null && _json["fastcmd"] is JArray fastcmdArray)
            {
                foreach (var item in fastcmdArray)
                {
                    // 兼容先前的纯字符串数组
                    if (item.Type == JTokenType.String)
                    {
                        CurrentFastCmds.Add(new FastCommandInfo { Cmd = item.ToString().TrimStart('/') });
                    }
                    // 新版本的对象数组
                    else if (item.Type == JTokenType.Object)
                    {
                        CurrentFastCmds.Add(item.ToObject<FastCommandInfo>());
                    }
                }
            }
            else
            {
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/op", Remark = "设置管理员" });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/deop", Remark = "去除管理员" });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/ban", Remark = "封禁玩家" });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/pardon", Remark = "解封玩家" });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/say", Remark = "全服说话" });
            }

            // 绑定到 UI
            fastCMD.ItemsSource = null;
            fastCMD.Items.Clear();
            fastCMD.ItemsSource = CurrentFastCmds;
            fastCMD.DisplayMemberPath = "DisplayText";
            fastCMD.SelectedIndex = 0;

            fastCmdList.ItemsSource = null;
            fastCmdList.Items.Clear();
            fastCmdList.ItemsSource = CurrentFastCmds;
            fastCmdList.DisplayMemberPath = "DisplayText";
        }

        private void SetFastCmd()
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];

            // 提取除了根命令以外的所有指令，转为匿名对象存入 JSON
            var saveList = CurrentFastCmds.Skip(1).Select(c => new
            {
                Remark = c.Remark,
                Cmd = c.Cmd,
                Alias = c.Alias
            });

            _json["fastcmd"] = JArray.FromObject(saveList);
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText(@"MSL\ServerList.json", jsonObject.ToString(), Encoding.UTF8);

            GetFastCmd(); // 保存后刷新UI
        }

        private void refrushFastCmd_Click(object sender, RoutedEventArgs e)
        {
            GetFastCmd();
        }
        private void resetFastCmd_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (_json["fastcmd"] == null)
            {
                return;
            }
            else
            {
                _json.Remove("fastcmd");
                jsonObject[RserverID.ToString()] = _json;
                File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                MagicShow.ShowMsgDialog(this, "要使重置生效需重启此窗口，请您手动关闭此窗口并打开", "提示");
            }
        }

        private async void addFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uniformStack = new UniformSpacingPanel { Orientation = Orientation.Vertical, Spacing = 3 };
                var cmdBox = new System.Windows.Controls.TextBox { Name = "CmdTextBox" };
                var remarkBox = new System.Windows.Controls.TextBox { Name = "RemarkTextBox" };
                var aliasBox = new System.Windows.Controls.TextBox { Name = "AliasTextBox" };

                uniformStack.Children.Add(new TextBlock { Text = "指令（如：op，无需 '/' 前缀）：" });
                uniformStack.Children.Add(cmdBox);
                uniformStack.Children.Add(new TextBlock { Text = "备注（如：给管理员，非必填）：" });
                uniformStack.Children.Add(remarkBox);
                uniformStack.Children.Add(new TextBlock { Text = "别名（如：o，可在控制台快速调用，非必填）：" });
                uniformStack.Children.Add(aliasBox);

                await MagicShow.ShowMsgDialogAsync("添加快捷指令", "输入", uIElement: uniformStack);

                // 读取三个 TextBox 的值
                string newCmd = cmdBox.Text.Trim();
                string newRemark = remarkBox.Text.Trim();
                string newAlias = aliasBox.Text.Trim();

                if (!string.IsNullOrEmpty(newCmd))
                {
                    CurrentFastCmds.Add(new FastCommandInfo
                    {
                        Remark = newRemark,
                        Cmd = newCmd,
                        Alias = newAlias
                    });
                    SetFastCmd();
                }
                else
                {
                    MagicFlowMsg.ShowMessage("指令不能为空！", 2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加失败: {ex.Message}");
            }
        }

        private void delFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fastCmdList.SelectedIndex <= 0)
                {
                    MessageBox.Show("无法删除根命令或未选中任何项！");
                    return;
                }
                CurrentFastCmds.RemoveAt(fastCmdList.SelectedIndex);
                SetFastCmd();
            }
            catch { return; }
        }
        #endregion

        #region 定时任务

        ///////////这是定时任务

        SortedDictionary<int, bool> taskFlag = new SortedDictionary<int, bool>(); // 后面的bool表示该任务是否运行
        Dictionary<int, KeyValuePair<int, int>> taskTimers = new Dictionary<int, KeyValuePair<int, int>>(); // KeyValuePair里，前面的int为timer的周期，后面的为周期单位（1为秒，2为毫秒）
        Dictionary<int, string> taskCmds = new Dictionary<int, string>();
        private void addTask_Click(object sender, RoutedEventArgs e)
        {
            if (taskFlag.Count == 0)
            {
                taskFlag.Add(0, false);
            }
            else
            {
                taskFlag.Add(taskFlag.Keys.Max() + 1, false);
            }
            //MessageBox.Show(taskID.Max().ToString());
            tasksList.ItemsSource = taskFlag.Keys.ToArray();
            KeyValuePair<int, int> defaultTimerTick = new KeyValuePair<int, int>(10, 1);
            taskTimers.Add(taskFlag.Keys.Max(), defaultTimerTick);
            taskCmds.Add(taskFlag.Keys.Max(), "say Hello World!");
            //tasksList.Items.Add(taskID.Max());
            loadOrSaveTaskConfig.Content = "保存任务配置";
        }

        private void delTask_Click(object sender, RoutedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                if (startTimercmd.Content.ToString() == "停止定时任务")
                {
                    MagicShow.ShowMsgDialog(this, "请先停止任务！", "警告");
                    return;
                }
                int selectedID = int.Parse(tasksList.SelectedItem.ToString());
                //int selectedTaskID = taskID[selectedIndex];

                taskTimers.Remove(selectedID);
                taskCmds.Remove(selectedID);

                taskFlag.Remove(selectedID);
                tasksList.ItemsSource = taskFlag.Keys.ToArray();

                if (tasksList.Items.Count == 0)
                {
                    loadOrSaveTaskConfig.Content = "加载任务配置";
                }
            }
        }

        private void tasksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                int selectedID = int.Parse(tasksList.SelectedItem.ToString());
                if (taskFlag[selectedID] == true)
                {
                    startTimercmd.IsChecked = true;
                }
                else
                {
                    startTimercmd.IsChecked = false;
                }
                timerCmdout.Text = "无";
                timercmdTime.Text = taskTimers[selectedID].Key.ToString();
                TaskUnit.SelectedIndex = taskTimers[selectedID].Value - 1;
                timercmdCmd.Text = taskCmds[selectedID];
            }
            else
            {
                timercmdTime.Text = "";
                timercmdCmd.Text = "";
            }
        }

        //检验输入合法性
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //匹配非数字
            e.Handled = regex.IsMatch(e.Text);
        }

        private void timercmdTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && tasksList.SelectedIndex != -1 && TaskUnit.SelectedIndex != -1 && !string.IsNullOrEmpty(timercmdTime.Text))
            {
                SwitchTaskTimerUnit();
            }
        }

        private void TaskUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && tasksList.SelectedIndex != -1 && TaskUnit.SelectedIndex != -1 && !string.IsNullOrEmpty(timercmdTime.Text))
            {
                SwitchTaskTimerUnit();
            }
        }

        private void SwitchTaskTimerUnit()
        {
            try
            {
                int selectedUnit = 0;
                switch (TaskUnit.SelectedIndex)
                {
                    case 0:
                        selectedUnit = 1;
                        break;
                    case 1:
                        selectedUnit = 2;
                        break;
                }
                taskTimers[int.Parse(tasksList.SelectedItem.ToString())] = new KeyValuePair<int, int>(int.Parse(timercmdTime.Text), selectedUnit);
            }
            catch
            {
                MagicShow.ShowMsgDialog(this, "出现错误，请检查您所填写的内容是否正确！", "错误");
            }
        }

        private void timercmdCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                taskCmds[int.Parse(tasksList.SelectedItem.ToString())] = timercmdCmd.Text;
            }
        }

        private void startTimercmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tasksList.SelectedIndex == -1)
                {
                    timerCmdout.Text = "执行失败，请先选择一个任务！";
                    startTimercmd.IsChecked = false;
                    return;
                }
                int selectedID = int.Parse(tasksList.SelectedItem.ToString());
                if (startTimercmd.IsChecked == true)
                {
                    taskFlag[selectedID] = true;
                    int time = taskTimers[selectedID].Key;
                    switch (taskTimers[selectedID].Value)
                    {
                        case 1:
                            time = time * 1000;
                            break;
                    }
                    Task.Run(() => TimedTasks(selectedID, time, taskCmds[selectedID]));
                }
                else
                {
                    taskFlag[selectedID] = false;
                }
            }
            catch (Exception a)
            {
                timerCmdout.Text = "执行失败，" + a.Message;
                startTimercmd.IsChecked = false;
            }
        }

        private void TimedTasks(int id, int timer, string cmd)
        {
            try
            {
                while (taskFlag[id])
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (CheckServerRunning())
                            {
                                switch (cmd) // 处理一些内置的特殊任务
                                {
                                    case ".backup":
                                        if (MoreOperation.IsEnabled == true)
                                        {
                                            _ = BackupWorld();
                                            PrintLog("[MSL备份]定时备份任务开始执行~", Colors.Blue);
                                        }
                                        break;
                                    default:
                                        SendCmdToServer(cmd);
                                        PrintLog("[MSL定时任务]执行指令：" + cmd, Colors.Blue);
                                        break;
                                }
                                if (tasksList.SelectedIndex != -1 && int.Parse(tasksList.SelectedItem.ToString()) == id)
                                {
                                    timerCmdout.Text = "执行成功  时间：" + DateTime.Now.ToString("F");
                                }
                            }
                            else
                            {
                                timerCmdout.Text = "服务器未开启  时间：" + DateTime.Now.ToString("F");
                            }
                        });
                    }
                    catch
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (tasksList.SelectedIndex != -1 && int.Parse(tasksList.SelectedItem.ToString()) == id)
                            {
                                timerCmdout.Text = "执行失败  时间：" + DateTime.Now.ToString("F");
                            }
                        });
                    }
                    Thread.Sleep(timer);
                }
            }
            catch
            {
                return;
            }
        }

        private void LoadOrSaveTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            string filePath = @"MSL\ServerList.json";

            // 加载任务配置
            if (loadOrSaveTaskConfig.Content.ToString() == "加载任务配置")
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(filePath, Encoding.UTF8));
                JObject serverJson = (JObject)jsonObject[RserverID.ToString()];

                if (serverJson["timedtasks"] != null)
                {
                    // 清空现有任务列表
                    taskFlag.Clear();
                    taskTimers.Clear();
                    taskCmds.Clear();

                    // 解析 JSON 数据
                    JObject taskTimersFromFile = (JObject)serverJson["timedtasks"];
                    foreach (var taskJson in taskTimersFromFile)
                    {
                        int taskId = int.Parse(taskJson.Key);
                        JObject taskDetails = (JObject)taskJson.Value;

                        // 直接更新现有集合
                        taskFlag.Add(taskId, false);
                        taskTimers[taskId] = new KeyValuePair<int, int>(
                            (int)taskDetails["Interval"],
                            (int)taskDetails["Unit"]
                        );
                        taskCmds[taskId] = (string)taskDetails["Command"];
                    }

                    // 更新任务列表显示
                    tasksList.ItemsSource = taskFlag.Keys.ToArray();
                }

                Growl.Success("加载成功！");
                if (tasksList.Items.Count != 0)
                {
                    loadOrSaveTaskConfig.Content = "保存任务配置";
                }
            }
            // 保存任务配置
            else
            {
                JObject taskTimersJson = new JObject(
                    taskFlag.Select(id => new JProperty(
                        id.Key.ToString(),
                        new JObject(
                            new JProperty("Interval", taskTimers[id.Key].Key),
                            new JProperty("Unit", taskTimers[id.Key].Value),
                            new JProperty("Command", taskCmds[id.Key])
                        )
                    ))
                );

                JObject jsonObject = JObject.Parse(File.ReadAllText(filePath, Encoding.UTF8));
                JObject serverJson = (JObject)jsonObject[RserverID.ToString()];
                serverJson["timedtasks"] = taskTimersJson;
                jsonObject[RserverID.ToString()] = serverJson;

                File.WriteAllText(filePath, Convert.ToString(jsonObject), Encoding.UTF8);
                Growl.Success("保存成功！");
            }
        }


        private void delTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            _json.Remove("timedtasks");
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            Growl.Success("清除成功！");
        }
        #endregion

        #region window event
        private void Window_Activated(object sender, EventArgs e)
        {
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                ConptyPopUp.IsOpen = true;
                Growl.SetGrowlParent(ConptyGrowlPanel, true);
                return;
            }
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                ConptyPopUp.IsOpen = false;
                Growl.SetGrowlParent(ConptyGrowlPanel, false);
                return;
            }
            Growl.SetGrowlParent(GrowlPanel, false);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                if (ConPTYWindow != null && TabCtrl.SelectedIndex == 1)
                {
                    ShowConptyWindow();
                    ConptyPopUp.IsOpen = true;
                    Growl.SetGrowlParent(GrowlPanel, false);
                    Growl.SetGrowlParent(ConptyGrowlPanel, true);
                }
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                UpdateChildWindowPosition();
            }
        }

        private async void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                await UpdateChildWindowSize();
                UpdateChildWindowPosition();
            }
        }

        // 复制
        private void LogMenu_Copy(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(outlog.SelectedText))
                Clipboard.SetText(outlog.SelectedText);
            else if (!string.IsNullOrEmpty(outlog.Document.Text))
                Clipboard.SetText(outlog.Document.Text);
        }

        // 全选
        private void LogMenu_SelectAll(object sender, RoutedEventArgs e)
        {
            outlog.SelectAll();
        }

        // 清屏
        private void LogMenu_Clear(object sender, RoutedEventArgs e)
        {
            outlog.Document.Text = string.Empty;
            _logColorizer.Clear();
            _logEntryCount = 0;
        }
        #endregion

        #region 备份相关
        private async Task BackupWorld()
        {
            // 发送指令
            if (CheckServerRunning())
            {
                SendCmdToServer("save-off");
                await Task.Delay(1000);
                SendCmdToServer("save-all");
                SendCmdToServer("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"正在进行服务器存档备份，请勿关闭服务器哦，否则可能造成回档！备份期间不会影响正常游戏~\",\"color\":\"aqua\"}]");
                Growl.Info("开始执行备份···");
                PrintLog("[MSL备份]开始执行备份···", Colors.Blue);
            }
            try
            {
                Dispatcher.Invoke(() =>
                {
                    MoreOperation.IsEnabled = false;
                });
                // 读取配置
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID.ToString()];

                //若服务器是开启状态 执行等待
                if (_json["backup_save_delay"] != null && int.Parse(_json["backup_save_delay"].ToString()) >= 5 && CheckServerRunning())
                {
                    //PrintLog($"[MSL备份]将在 {int.Parse(_json["backup_save_delay"].ToString())} 秒后开始执行压缩备份···", Brushes.Blue);
                    await Task.Delay(int.Parse(_json["backup_save_delay"].ToString()) * 1000);
                }
                else
                {
                    //PrintLog($"[MSL备份]将在 10 秒后开始执行压缩备份···", Brushes.Blue);
                    await Task.Delay(10000);
                }
                string worldPath = ServerProperties.GetConfigValue("level-name"); // 获取世界存档路径
                if (string.IsNullOrEmpty(worldPath))
                {
                    worldPath = "world";
                }

                // 兼容插件端的分离的存档文件夹
                string fullWorldPath = Path.Combine(Rserverbase, worldPath);
                string fullNetherPath = Path.Combine(Rserverbase, worldPath + "_nether");
                string fullEndPath = Path.Combine(Rserverbase, worldPath + "_the_end");

                // 备份列表
                var foldersToCompress = new List<string>();

                if (Directory.Exists(fullWorldPath))
                {
                    foldersToCompress.Add(fullWorldPath);
                }
                if (Directory.Exists(fullNetherPath))
                {
                    foldersToCompress.Add(fullNetherPath);
                }
                if (Directory.Exists(fullEndPath))
                {
                    foldersToCompress.Add(fullEndPath);
                }

                // 检查是否至少有一个世界文件夹存在
                if (foldersToCompress.Count == 0)
                {
                    Growl.Error("未找到任何世界存档文件夹（包括主世界、下界、末地），备份失败！");
                    PrintLog("[MSL备份]未找到任何世界存档文件夹（包括主世界、下界、末地），备份失败！", Colors.Red);
                    LogHelper.Write.Error("未找到任何世界存档文件夹（包括主世界、下界、末地），备份失败！");
                    if (CheckServerRunning())
                    {
                        SendCmdToServer("save-on");
                        SendCmdToServer("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"备份失败！未找到任何世界存档文件夹！\",\"color\":\"red\"}]");
                    }
                    return;
                }

                // 相关目录文件
                string backupDir = Path.Combine(Rserverbase, "msl-backups"); //默认备份在服务端里
                if (_json["backup_mode"] != null)
                {
                    if (int.Parse(_json["backup_mode"].ToString()) == 1)
                    {
                        backupDir = Path.Combine(@"MSL", "server-backups", $"{Rservername}_{RserverID}");
                    }
                    if (int.Parse(_json["backup_mode"].ToString()) == 2)
                    {
                        if (_json["backup_custom_path"] != null && !String.IsNullOrEmpty(_json["backup_custom_path"].ToString()))
                        {
                            backupDir = _json["backup_custom_path"].ToString();

                        }
                        else
                        {
                            PrintLog("[MSL备份]自定义备份路径为空，已使用默认路径！", Colors.OrangeRed);
                        }
                    }
                }
                string backupPath = Path.Combine(backupDir, $"msl-backup_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.zip");

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // 限制最大备份数量
                int maxBackups = 20;
                if (_json["backup_max_limit"] != null)
                {
                    if (int.Parse(_json["backup_max_limit"].ToString()) >= 0)
                    {
                        maxBackups = int.Parse(_json["backup_max_limit"].ToString());
                    }
                    else
                    {
                        PrintLog("[MSL备份]最大备份数量配置错误，已使用默认值20！", Colors.OrangeRed);
                    }
                }
                try
                {
                    var backupFiles = Directory.GetFiles(backupDir, "msl-backup_*.zip")
                                               .Select(path => new FileInfo(path))
                                               .OrderBy(fi => fi.Name) // 按文件名排序，文件名早的=时间旧的
                                               .ToList();

                    if (maxBackups >= 1 && backupFiles.Count >= maxBackups)
                    {
                        int filesToDeleteCount = backupFiles.Count - maxBackups + 1;
                        var filesToDelete = backupFiles.Take(filesToDeleteCount).ToList();

                        // 遍历删除最旧的文件
                        foreach (var fileToDelete in filesToDelete)
                        {
                            try
                            {
                                fileToDelete.Delete();
                                PrintLog($"[MSL备份]已删除旧备份：{fileToDelete.Name}", Colors.Blue);
                            }
                            catch (Exception ex)
                            {
                                // 如果删除失败，仅发出警告，不中断整个备份过程
                                PrintLog($"[MSL备份]删除旧备份 {fileToDelete.Name} 失败：{ex.Message}", Colors.OrangeRed);
                                LogHelper.Write.Warn($"删除旧备份 {fileToDelete.Name} 失败：{ex.ToString()}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PrintLog("[MSL备份]目录检查或旧备份清理失败：" + ex.Message, Colors.OrangeRed);
                    LogHelper.Write.Error("检查并清理旧备份时发生错误：" + ex.ToString());
                }

                // 压缩，启动！
                PrintLog("[MSL备份]正在压缩存档文件，请稍等···", Colors.Blue);
                LogHelper.Write.Info("正在压缩存档文件，请稍等···");
                using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(backupPath)))
                {
                    zipStream.SetLevel(5); // 设置压缩等级

                    // 遍历所有需要备份的文件夹，并将它们逐一添加到压缩包
                    foreach (var folderPath in foldersToCompress)
                    {
                        // 压缩
                        await CompressFolder(Rserverbase, folderPath, zipStream);
                    }
                }

                // 重新开启服务器自动保存
                if (CheckServerRunning())
                {
                    try
                    {
                        FileInfo backupFileInfo = new FileInfo(backupPath);
                        string fileName = backupFileInfo.Name;
                        long fileSizeInBytes = backupFileInfo.Length;
                        string formattedSize;
                        if (fileSizeInBytes > 1024 * 1024 * 1024) { formattedSize = $"{fileSizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB"; }
                        else if (fileSizeInBytes > 1024 * 1024) { formattedSize = $"{fileSizeInBytes / (1024.0 * 1024.0):F2} MB"; }
                        else if (fileSizeInBytes > 1024) { formattedSize = $"{fileSizeInBytes / 1024.0:F2} KB"; }
                        else { formattedSize = $"{fileSizeInBytes} Bytes"; }
                        string tellrawMessage = $"tellraw @a [";
                        tellrawMessage += "{\"text\":\"[\",\"color\":\"yellow\"},";
                        tellrawMessage += "{\"text\":\"MSL\",\"color\":\"green\"},";
                        tellrawMessage += "{\"text\":\"]\",\"color\":\"yellow\"},";
                        tellrawMessage += "{\"text\":\" 服务器存档备份完成！\\n\",\"color\":\"aqua\"},";
                        tellrawMessage += $"{{\"text\":\"文件名: \",\"color\":\"gray\"}},";
                        tellrawMessage += $"{{\"text\":\"{fileName}\",\"color\":\"white\"}},";
                        tellrawMessage += $"{{\"text\":\"\\n大小: \",\"color\":\"gray\"}},";
                        tellrawMessage += $"{{\"text\":\"{formattedSize}\",\"color\":\"white\"}}";
                        tellrawMessage += "]";
                        SendCmdToServer("save-on");
                        SendCmdToServer(tellrawMessage);
                    }
                    catch (Exception ex)
                    {
                        PrintLog("[MSL备份]无法获取备份文件信息：" + ex.Message, Colors.OrangeRed);
                        LogHelper.Write.Warn("无法获取备份文件信息：" + ex.ToString());
                        SendCmdToServer("save-on");
                        SendCmdToServer("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"服务器存档备份完成！\",\"color\":\"aqua\"}]");
                    }
                }

                Growl.Success($"存档备份成功！已保存至：{backupPath}");
                PrintLog($"[MSL备份]存档备份成功！已保存至：{backupPath}", Colors.Blue);
                LogHelper.Write.Info($"存档备份成功！已保存至：{backupPath}");
            }
            catch (Exception ex)
            {
                Growl.Error("备份失败！" + ex.Message);
                PrintLog("[MSL备份]备份失败！" + ex.Message, Colors.Red);
                LogHelper.Write.Error("备份失败！" + ex.ToString());
                if (CheckServerRunning())
                {
                    SendCmdToServer("save-on");
                    SendCmdToServer("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"备份过程中发生错误，备份失败！\",\"color\":\"red\"}]");
                }
                return;
            }
            finally
            {
                // 最后这里再执行一次 不知道有啥意义 留着吧 qwq
                if (CheckServerRunning())
                {
                    SendCmdToServer("save-on");
                }
                Dispatcher.Invoke(() =>
                {
                    MoreOperation.IsEnabled = true;
                });
            }
        }

        private async Task CompressFolder(string rootPath, string currentPath, ZipOutputStream zipStream)
        {
            string[] files = Directory.GetFiles(currentPath);
            foreach (string file in files)
            {
                // 排除 session.lock
                if (Path.GetFileName(file).Equals("session.lock", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryName = file.Substring(rootPath.Length + 1);
                ZipEntry entry = new ZipEntry(entryName);
                entry.DateTime = DateTime.Now;
                zipStream.PutNextEntry(entry);

                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        await fs.CopyToAsync(zipStream);
                    }
                }
                catch (IOException ex)
                {
                    throw new IOException($"无法以共享只读模式打开文件 '{entryName}'。服务器施加了排他锁。错误: {ex.Message}", ex);
                }
            }

            string[] folders = Directory.GetDirectories(currentPath);
            foreach (string folder in folders)
            {
                await CompressFolder(rootPath, folder, zipStream);
            }
        }

        private void ComboBackupPath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBackupPath.SelectedIndex == 2)
            {
                GridSelBackupPath.Visibility = Visibility.Visible;
            }
            else
            {
                GridSelBackupPath.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSelBackupPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "请选择一个文件夹用于存放备份文件";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                TextBackupPath.Text = dialog.SelectedPath;
            }
        }
        private void BtnOpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            string backupDir;
            switch (ComboBackupPath.SelectedIndex)
            {
                case 0:
                    backupDir = Path.Combine(Rserverbase, "msl-backups");
                    break;
                case 1:
                    backupDir = Path.Combine(@"MSL", "server-backups", $"{Rservername}_{RserverID}");
                    break;
                case 2:
                    if (!String.IsNullOrEmpty(TextBackupPath.Text))
                    {
                        backupDir = TextBackupPath.Text;
                    }
                    else
                    {
                        Growl.Error("自定义备份路径为空！");
                        return;
                    }
                    break;
                default:
                    backupDir = Path.Combine(Rserverbase, "msl-backups");
                    break;
            }
            try
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = backupDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Growl.Error("打开备份文件夹失败！" + ex.Message);
            }
        }

        #endregion
    }
}