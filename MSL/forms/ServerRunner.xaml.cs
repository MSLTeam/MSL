using Cronos;
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
using MSL.langs;
using MSL.pages;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static MSL.utils.LogColorizer;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace MSL
{
    /// <summary>
    /// ServerRunner.xaml 的交互逻辑
    /// </summary>
    public partial class ServerRunner : HandyControl.Controls.Window, INotifyPropertyChanged
    {
        #region 事件定义
        public static event App.DeleControl SaveConfigEvent;
        public static event App.DeleControl ServerStateChange;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region MVVM 辅助
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region 字段&属性&类定义
        private short GetServerInfoLine = 0;
        private readonly short FirstStartTab;
        private MCServerService ServerService { get; set; }
        private ServerProperties ServerProperties { get; set; }
        private int RserverID { get; }
        public LogColorizer _logColorizer;
        private int _logEntryCount = 0;
        private const int MaxLogEntries = 1000;

        // 全局指令列表
        private List<FastCommandInfo> CurrentFastCmds = new List<FastCommandInfo>();

        // MVVM 绑定属性 - 服务器信息面板
        private string _serverStateText;
        public string ServerStateText
        {
            get => _serverStateText;
            set => SetProperty(ref _serverStateText, value);
        }

        private string _onlineModeText;
        public string OnlineModeText
        {
            get => _onlineModeText;
            set => SetProperty(ref _onlineModeText, value);
        }

        private string _gameTypeText;
        public string GameTypeText
        {
            get => _gameTypeText;
            set => SetProperty(ref _gameTypeText, value);
        }

        private string _gameDifficultyText;
        public string GameDifficultyText
        {
            get => _gameDifficultyText;
            set => SetProperty(ref _gameDifficultyText, value);
        }

        private string _serverIPText;
        public string ServerIPText
        {
            get => _serverIPText;
            set => SetProperty(ref _serverIPText, value);
        }

        private string _localIPText;
        public string LocalIPText
        {
            get => _localIPText;
            set => SetProperty(ref _localIPText, value);
        }
        #endregion

        #region 初始化

        /// <summary>
        /// 服务器运行窗口
        /// </summary>
        /// <param name="serverID">服务器ID</param>
        /// <param name="controlTab">Tab标签</param>
        public ServerRunner(int serverID, short controlTab = 0)
        {
            InitializeComponent();
            DataContext = this;
            InitializeOutlog();
            previewOutlog.Text = Lang.SR_PreviewHint;
            cmdtext.Text = Lang.SR_ServerClosed;
            memoryInfo.Text = Lang.SR_AutoDetectMem;

            // 初始化 MVVM 绑定属性默认值
            ServerStateText = Lang.SR_Closed;
            OnlineModeText = Lang.SR_Fetching;
            GameTypeText = Lang.SR_Fetching;
            GameDifficultyText = Lang.SR_Fetching;
            ServerIPText = Lang.SR_Fetching;
            LocalIPText = Lang.SR_Fetching;

            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
            RserverID = serverID;
            FirstStartTab = controlTab;

            ServerService = new MCServerService(serverID,
                onPrintLog: PrintLog,
                onServerExit: ServerExitEvent,
                onServerStarted: ServerStartedEvent,
                onPlayerListAdd: HandlePlayerListAdd,
                onPlayerListRemove: HandlePlayerListRemove,
                onChangeEncodingOut: HandleEncodingChange);
        }

        private void InitializeOutlog()
        {
            _logColorizer = new LogColorizer();
            var cft = AppConfig.Current.LogFont;
            if (!string.IsNullOrEmpty(cft.Family))
            {
                FontFamily fontFamily = new FontFamily(AppConfig.Current.LogFont.Family);
                outlog.FontFamily = fontFamily;
            }
            if (cft != null && cft.Size > 0)
                outlog.FontSize = AppConfig.Current.LogFont.Size;
            outlog.TextArea.TextView.LineTransformers.Add(_logColorizer);
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
            await LoadedInfoEvent();
            await Task.Delay(50);
            MainGrid.Children.Remove(loadingCircle);
            MainGrid.UnregisterName("loadingBar");
            TabCtrl.SelectedIndex = FirstStartTab;
            if (FirstStartTab == 0)
            {
                LaunchServerOnLoad();
            }
        }

        private async Task<bool> LoadingInfoEvent()
        {
            AppConfig config = AppConfig.Current;
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
            autoStartserver.IsChecked = ServerService.InstanceConfig.AutoStartServer;
            showOutlog.IsChecked = ServerService.InstanceConfig.ShowOutlog;
            formatOutHead.IsChecked = ServerService.InstanceConfig.FormatLogPrefix;

            if (ServerService.InstanceConfig.ShieldLogs != null && ServerService.InstanceConfig.ShieldLogs.Count > 0)
            {
                shieldLogBtn.IsChecked = true;
                LogShield_Add.IsEnabled = false;
                LogShield_Del.IsEnabled = false;
            }
            if (ServerService.InstanceConfig.HighLightLogs != null && ServerService.InstanceConfig.HighLightLogs.Count > 0)
            {
                highLightLogBtn.IsChecked = true;
                LogHighLight_Add.IsEnabled = false;
                LogHighLight_Del.IsEnabled = false;
            }

            shieldStackOut.IsChecked = ServerService.InstanceConfig.ShieldStackOut;
            inputCmdEncoding.Content = ServerService.InstanceConfig.EncodingIn;
            inputCmdEncoding.Content = ServerService.InstanceConfig.EncodingIn;
            outputCmdEncoding.Content = ServerService.InstanceConfig.EncodingOut;
            fileforceUTF8encoding.IsChecked = ServerService.InstanceConfig.FileForceUTF8;
            YggdrasilAddr.Text = ServerService.InstanceConfig.YggApi;
            var backupMode = ServerService.InstanceConfig.BackupConfigs.BackupMode;
            try
            {
                if (backupMode >= 0 && backupMode <= 2)
                {
                    ComboBackupPath.SelectedIndex = backupMode;
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
            TextBackupMaxLimitCount.Text = ServerService.InstanceConfig.BackupConfigs.BackupMaxLimit.ToString();
            TextBackupPath.Text = ServerService.InstanceConfig.BackupConfigs.BackupCustomPath;
            TextBackupDelay.Text = ServerService.InstanceConfig.BackupConfigs.BackupSaveDelay.ToString();
            this.Title = ServerService.ServerName;  // set title to server name

            if (File.Exists(ServerService.ServerBase + "\\server-icon.png"))//check server-icon,if exist,set icon to server-icon
            {
                try
                {
                    Icon = new BitmapImage(new Uri(ServerService.ServerBase + "\\server-icon.png"));
                }
                catch { Console.WriteLine(LanguageManager.Instance["SR_IconLoadFailed"]); }
            }

            if (ServerService.InstanceConfig.UseConpty)
            {
                ServerEncodingSettings.Visibility = Visibility.Collapsed;
                useConpty.IsChecked = true;
            }

            return true;
        }//窗体加载后，运行此方法，主要为改变UI等内容

        private async Task LoadedInfoEvent()
        {
            systemInfoBtn.IsChecked = ConfigStore.GetServerInfo;
            var getPlayerInfo = ConfigStore.GetPlayerInfo;
            ServerService.recordPlayInfo = getPlayerInfo;
            playerInfoBtn.IsChecked = getPlayerInfo;
            ServerProperties = new ServerProperties(this, ServerService, ServerService.ServerBase);
            SettingsGrid.Content = ServerProperties;
            await LoadSettings();
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


                if (AppConfig.Current.MicaEffect == true)
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
                    if (AppConfig.Current.SemitransparentTitle == true)
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
                    Content = LanguageManager.Instance["SR_NotLatestVersion"],
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

        #endregion

        #region 控件事件
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
                try
                {
                    Config.Write("sidemenuExpanded", false);
                }
                catch { }
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
            switch (TabCtrl.SelectedIndex)
            {
                case 1:
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

        //检验输入合法性
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //匹配非数字
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        #region 关闭事件
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (ServerService.CheckServerRunning())
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
                        dialog = MagicShow.ShowMsg(this, LanguageManager.Instance["SR_HideWindowConfirm"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                    }

                    if (dialog == 1)
                    {
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
            ServerService.Dispose();
            ServerService = null;
            ClearLog();
            if (ServerList.ServerWindowList.ContainsKey(RserverID))
            {
                ServerList.ServerWindowList.Remove(RserverID);
            }
            SettingsPage.ChangeSkinStyle -= ChangeSkinStyle;

            getSystemInfo = false;

            ServerProperties.Dispose();

            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }
        #endregion

        #region 仪表盘

        //////////////////
        /////这里是仪表盘
        //////////////////

        private async void solveProblemBtn_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_CrashAnalysisInfo"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                ServerService.ProblemSolveSystem = true;
                LaunchServer();
            }
        }
        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            Growl.Info(LanguageManager.Instance["SR_OpeningServerDir"]);
            Process.Start(ServerService.ServerBase);
        }

        private void copyPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (serverPlayerList.SelectedValue == null)
            {
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_SelectPlayerFirst"], 2);
                return;
            }
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_CopySuccess"]);
            Clipboard.SetText(serverPlayerList.SelectedValue.ToString());
        }

        private async void kickPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_ConfirmKick"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                if (!ServerService.SendCommand("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("["))))
                    Growl.Error(LanguageManager.Instance["SR_OperationFailed"]);
            }
        }

        private async void banPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_ConfirmBan"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                if (!ServerService.SendCommand("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("["))))
                    Growl.Error(LanguageManager.Instance["SR_OperationFailed"]);
            }
        }

        private async void gotoFrpc_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress;
            // 获取本地计算机的IP地址列表
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            // 正则表达式匹配内网地址的模式
            string privateIpPattern = @"^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)";
            // radmin IP段
            string radminIpPattern = @"^26\.";

            // 遍历IP地址列表
            foreach (IPAddress localIP in localIPs)
            {
                // 检查IPv4地址是否为公网IP
                if (localIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(localIP) &&
                    !Regex.IsMatch(localIP.ToString(), privateIpPattern) && !Regex.IsMatch(localIP.ToString(), radminIpPattern))
                {
                    ipAddress = LocalIPText;

                    if (ipAddress.Contains(":"))
                    {
                        string port = ipAddress.Substring(ipAddress.IndexOf(":") + 1);
                        MagicShow.ShowMsgDialog(this, string.Format(LanguageManager.Instance["SR_PublicIPInfo"], localIP.ToString(), port), LanguageManager.Instance["Tip"]);
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(this, string.Format(LanguageManager.Instance["SR_PublicIPInfoNoPort"], localIP.ToString()), LanguageManager.Instance["Tip"]);
                    }
                    return;
                }
            }

            // 检查radmin地址
            foreach (IPAddress localIP in localIPs)
            {
                if (localIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipStr = localIP.ToString();
                    if (Regex.IsMatch(ipStr, radminIpPattern))
                    {
                        ipAddress = LocalIPText;
                        string portSuffix = ipAddress.Contains(":") ? ":" + ipAddress.Substring(ipAddress.IndexOf(":") + 1) : "";

                        MagicShow.ShowMsgDialog(this,
                            string.Format(LanguageManager.Instance["SR_RadminDetected"], ipStr, portSuffix),
                            LanguageManager.Instance["SR_RadminTitle"]);
                        return;
                    }
                }
            }

            await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_NetworkMappingTip"], LanguageManager.Instance["Tip"], false);
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
                await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_DisableOutputPreview"], LanguageManager.Instance["Tip"]);
                previewOutlog.Text = LanguageManager.Instance["SR_PreviewClosed"];
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
                    Growl.Error(LanguageManager.Instance["SR_SysOccupationErr"]);
                    previewOutlog.Text = LanguageManager.Instance["SR_PreviewClosed"];
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
                if (ServerService.ServerProcess != null && !ServerService.ServerProcess.HasExited)
                {
                    targetProc = ServerService.ServerProcess;
                }
                // 其次检查 ConPTYWindow 内部进程
                else if (ServerService.ServerTerm != null && ServerService.ServerTerm.IsRunning)
                {
                    var p = ServerService.ServerTerm._process.Process;
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
            memoryInfoLab.Content = string.Format(LanguageManager.Instance["SR_MemInfoFormat"], $"{allMemory:f2}", $"{allMemory - ramAvailable:f2}", $"{ramAvailable:f2}");
            double usedMemoryPercentage = (allMemory - ramAvailable) / allMemory;
            memoryInfoBar.Value = usedMemoryPercentage * 100;
            processMemoryInfoBar.Value = (processUsedMemory / allMemory) * 100;
            usedMemoryLab.Content = string.Format(LanguageManager.Instance["SR_SysMemUsedFormat"], $"{usedMemoryPercentage:P}");
            processMemoryInfoLab.Content = string.Format(LanguageManager.Instance["SR_ProcMemUsedFormat"], $"{processUsedMemory:f2}", $"{(processUsedMemory / allMemory):P}");
        }

        private void UpdateLogPreview()
        {
            if (previewOutlog.LineCount < 25)
            {
                if (ServerService == null)
                    return;
                if (!string.IsNullOrEmpty(ServerService._tempLog) && !previewOutlog.Text.Contains(ServerService._tempLog))
                {
                    previewOutlog.Text += "\n" + ServerService._tempLog;
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
                ServerService.recordPlayInfo = true;
                Growl.Success(LanguageManager.Instance["SR_EnabledLower"]);
            }
            else
            {
                ServerService.recordPlayInfo = false;
                Growl.Success(LanguageManager.Instance["SR_DisabledLower"]);
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
            LogHelper.Write.Info("开服操作 - 实例ID：" + RserverID);
            if (await MCEulaEvent() != true)
                return;
            if (ServerService.ServerMode == 0 && !string.IsNullOrEmpty(ServerService.ServerYggAddr))
            {
                // 代表启动的是一个MC服务器
                // 处理外置登录
                if (!await DownloadAuthlib())
                {
                    return; // 下载authlib失败，退出
                }
                LogHelper.Write.Info("成功启用外置登录库，地址：" + ServerService.ServerYggAddr);
            }
            await ServerService.LaunchServer();
            ChangeControlsState();
        }

        private async Task<bool> MCEulaEvent()
        {
            if (ServerService.ServerMode != 0) // 以自定义命令方式启动时，不执行接受eula事件
                return true;
            string path1 = ServerService.ServerBase + "\\eula.txt";
            if (!File.Exists(path1) || (File.Exists(path1) && !File.ReadAllText(path1).Contains("eula=true")))
            {
                var shield = new Shield
                {
                    Command = HandyControl.Interactivity.ControlCommands.OpenLink,
                    CommandParameter = "https://aka.ms/MinecraftEULA",
                    Subject = "https://aka.ms/MinecraftEULA",
                    Status = LanguageManager.Instance["OpenWebsite"]
                };
                bool dialog = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_MCEulaPrompt"], LanguageManager.Instance["Tip"], true, LanguageManager.Instance["No"], LanguageManager.Instance["Yes"], shield);
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
                        MessageBox.Show(LanguageManager.Instance["SR_EulaError"] + a, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task<bool> DownloadAuthlib()
        {
            HttpResponse res = await HttpService.GetAsync("https://authlib-injector.mirrors.mslmc.cn/artifact/latest.json");
            if (res.HttpResponseCode == HttpStatusCode.OK)
            {
                JObject authlib_jobj = JObject.Parse((string)res.HttpResponseContent);
                if (!File.Exists(Path.Combine(ServerService.ServerBase, "authlib-injector.jar")) ||
                    !Functions.VerifyFileSHA256(Path.Combine(ServerService.ServerBase, "authlib-injector.jar"), authlib_jobj["checksums"]["sha256"].ToString()))
                {
                    //下载或更新authlib-injector.jar
                    bool download_suc = await MagicShow.ShowDownloader(this,
                        authlib_jobj["download_url"].ToString().Replace("authlib-injector.yushi.moe", "authlib-injector.mirrors.mslmc.cn"),
                        ServerService.ServerBase, "authlib-injector.jar", LanguageManager.Instance["SR_AuthlibUpdating"], authlib_jobj["checksums"]["sha256"].ToString());
                    if (!download_suc)
                    {
                        Growl.Error(LanguageManager.Instance["SR_DownloadFailed"]);
                        return false;
                    }
                }
            }
            else
            {
                if (File.Exists(Path.Combine(ServerService.ServerBase, "authlib-injector.jar")))
                {
                    LogHelper.Write.Warn("无法获取最新的authlib-injector.jar信息，使用本地文件。" + res.HttpResponseContent);
                }
                else
                {
                    Growl.Error(LanguageManager.Instance["SR_AuthlibNotFound"]);
                    return false;
                }
            }
            return true;
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
                ServerStateText = LanguageManager.Instance["SR_Running"];
                serverStateLab.Foreground = Brushes.Red;
                solveProblemBtn.IsEnabled = false;
                controlServer.IsChecked = true;
                controlServer1.IsChecked = true;
                MoreOperation.IsEnabled = false; //服务器完成启动前禁止备份
                GameDifficultyText = LanguageManager.Instance["SR_Fetching"];
                GameTypeText = LanguageManager.Instance["SR_Fetching"];
                ServerIPText = LanguageManager.Instance["SR_Fetching"];
                LocalIPText = LanguageManager.Instance["SR_Fetching"];
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_Launching"]);
                ClearLog();
                PrintLog(LanguageManager.Instance["SR_Launching"], ConfigStore.LogColor.INFO);
                cmdtext.IsEnabled = true;
                cmdtext.Clear();
                fastCMD.IsEnabled = true;
                sendcmd.IsEnabled = true;
            }
            else
            {
                if (ServerList.RunningServers.Contains(RserverID))
                {
                    ServerList.RunningServers.Remove(RserverID);
                }
                ServerStateChange();

                ServerStateText = LanguageManager.Instance["SR_Closed"];
                serverStateLab.Foreground = Brushes.Green;
                solveProblemBtn.IsEnabled = true;
                controlServer.IsChecked = false;
                controlServer1.IsChecked = false;
                MoreOperation.IsEnabled = true; // 服务器关闭后允许备份
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_ServerClosedMsg"]);
                sendcmd.IsEnabled = false;
                cmdtext.IsEnabled = false;
                fastCMD.IsEnabled = false;
                cmdtext.Text = LanguageManager.Instance["SR_ServerClosed"];
            }
        }

        #region 日志显示功能、日志清空功能

        private void PrintLog(string msg, Color defaultColor)
        {
            // 解析出带颜色的片段列表
            var segments = MCServerLogHelper.ParseLogSegments(msg, defaultColor);
            if (segments.Count == 0) return;

            Dispatcher.Invoke(() =>
            {
                // 自动清屏
                if (autoClearOutlog.IsChecked == true && _logEntryCount >= MaxLogEntries)
                {
                    ClearLog();
                }

                bool shouldScroll = outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight - 48;
                string plainText = string.Concat(segments.Select(s => s.Text));  // 拼接纯文本
                int insertOffset = outlog.Document.TextLength;  // 记录插入位置

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

                outlog.Document.Insert(insertOffset, plainText);  // 插入纯文本
                _logColorizer.AddEntry(entry);  // 注册着色信息
                outlog.TextArea.TextView.Redraw();  // 触发重绘

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
        #endregion

        private void ServerStartedEvent()
        {
            MagicFlowMsg.ShowMessage(string.Format(LanguageManager.Instance["SR_ServerLaunchedSuccess"], ServerService.ServerName), 1);
            ServerStateText = LanguageManager.Instance["SR_Launched"];
            GetServerInfoSys();
            MoreOperation.IsEnabled = true;
        }

        private void GetServerInfoSys()
        {
            try
            {
                Encoding encoding = Functions.GetTextFileEncodingType(ServerService.ServerBase + @"\server.properties");
                string config = File.ReadAllText(ServerService.ServerBase + @"\server.properties", encoding);
                if (config.Contains("\r"))
                {
                    config = config.Replace("\r", string.Empty);
                }
                int om1 = config.IndexOf("online-mode=") + 12;
                string om2 = config.Substring(om1);
                string onlineMode = om2.Substring(0, om2.IndexOf("\n"));
                if (onlineMode == "true")
                {
                    if (string.IsNullOrEmpty(ServerService.ServerYggAddr))
                    {
                        PrintLog(LanguageManager.Instance["SR_OnlineModeWarn"], Colors.OrangeRed);
                    }
                    else
                    {
                        PrintLog(LanguageManager.Instance["SR_OnlineModeThirdParty"], Colors.OrangeRed);
                    }
                    OnlineModeText = LanguageManager.Instance["SR_EnabledLower"];
                }
                else if (onlineMode == "false")
                {
                    if (string.IsNullOrEmpty(ServerService.ServerYggAddr))
                    {
                        PrintLog(LanguageManager.Instance["SR_OnlineModeClosedWarn"], Colors.OrangeRed);
                    }
                    else
                    {
                        PrintLog(LanguageManager.Instance["SR_OnlineModeThirdPartyClosed"], Colors.Red);
                    }
                    OnlineModeText = LanguageManager.Instance["SR_DisabledLower"];
                }
                string[] strings1 = config.Split('\n');
                foreach (string s in strings1)
                {
                    if (s.StartsWith("gamemode="))
                    {
                        GameTypeText = s.Substring(9);
                        break;
                    }
                }
                int dc1 = config.IndexOf("difficulty=") + 11;
                string dc2 = config.Substring(dc1);
                GameDifficultyText = dc2.Substring(0, dc2.IndexOf("\n"));
                int ip1 = config.IndexOf("server-ip=") + 10;
                string ip2 = config.Substring(ip1);
                string serverIP = ip2.Substring(0, ip2.IndexOf("\n"));

                int sp1 = config.IndexOf("server-port=") + 12;
                string sp2 = config.Substring(sp1);
                string serverPort = sp2.Substring(0, sp2.IndexOf("\n"));
                ServerIPText = serverIP + ":" + serverPort;
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
                LocalIPText = serverIP + serverPort;
            }
            catch
            {
                Growl.Info(LanguageManager.Instance["SR_ServerInfoErr"]);
            }
        }

        private void HandlePlayerListAdd(string playerName)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (!serverPlayerList.Items.Contains(playerName))
                {
                    serverPlayerList.Items.Add(playerName);
                }
            });
        }

        private void HandlePlayerListRemove(string playerName)
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
                Growl.Error(LanguageManager.Instance["SR_SomeError"]);
            }
        }

        private void ServerExitEvent(int exitCode)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                ChangeControlsState(false);
                if (ServerService.ProblemSolveSystem)
                {
                    ServerService.ProblemSolveSystem = false;
                    if (string.IsNullOrEmpty(ServerService.ProblemFound))
                    {
                        MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ServerClosedForAnalysis"], LanguageManager.Instance["SR_CrashAnalysisSystem"]);
                    }
                    else
                    {
                        Growl.Info(LanguageManager.Instance["SR_ServerClosedShowingReport"]);
                        MagicShow.ShowMsgDialog(this, ServerService.ProblemFound + "\n" + LanguageManager.Instance["SR_ProblemFoundPS"], LanguageManager.Instance["SR_ServerAnalysisReport"]);
                        ServerService.ProblemFound = string.Empty;
                    }
                }
                else if (exitCode != 0 && GetServerInfoLine <= 100)
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_AbnormalClose"], LanguageManager.Instance["Tip"], true);
                    if (dialogRet)
                    {
                        TabCtrl.SelectedIndex = 1;
                        ServerService.ProblemSolveSystem = true;
                        LaunchServer();
                    }
                }
                else if (autoStartserver.IsChecked == true)
                {
                    Console.WriteLine(LanguageManager.Instance["SR_ServerClosedRestartEvent"]);
                    await Task.Delay(200);
                    RestartServer();
                }
            });
        }

        private void RestartServer()
        {
            MagicFlowMsg.ShowAskMessage(
                message: LanguageManager.Instance["SR_RestartConfirmMsg"],
                callback: confirmed =>
                {
                    if (confirmed)
                    {
                        if (ServerService != null)
                        {
                            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_ServerRestarting"], type: 1);
                            LaunchServer();
                        }
                    }
                    else
                    {
                        // 用户主动取消
                        MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_RestartCancelled"], type: 3);
                    }
                },
                waitSeconds: 5,
                titleText: LanguageManager.Instance["SR_RestartAutoTitle"],
                confirmText: LanguageManager.Instance["SR_RestartNow"],
                cancelText: LanguageManager.Instance["SR_RestartCancel"],
                container: GrowlPanel
            );
        }

        private void HandleEncodingChange()
        {
            string encoding = "UTF8";
            if (outputCmdEncoding.Content.ToString().Contains("UTF8"))
            {
                encoding = "ANSI";
            }
            Growl.Ask(new GrowlInfo
            {
                Message = string.Format(LanguageManager.Instance["SR_EncodingChangeMsg"], encoding),
                ActionBeforeClose = isConfirmed =>
                {
                    if (isConfirmed)
                    {
                        ServerService.InstanceConfig.EncodingOut = encoding;
                        ServerConfig.Current.Save();
                        Dispatcher.InvokeAsync(() =>
                        {
                            outputCmdEncoding.Content = encoding;
                            Growl.Success(LanguageManager.Instance["SR_ChangeDone"]);
                        });
                        Task.Run(async () =>
                        {
                            GetServerInfoLine = 102;
                            await Task.Delay(100);
                            ServerService.KillServer();
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

        private void SendCommand()
        {
            try
            {
                string inputText = cmdtext.Text.Trim();
                if (string.IsNullOrEmpty(inputText)) return;

                string finalCmd = inputText;

                // 解析输入的内容：分离第一个词（可能是别名）和剩余参数
                string[] parts = inputText.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);

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
                            string args = parts.Length > 1 ? parts[1] : string.Empty;
                            // 清理“/”
                            finalCmd = $"{aliasMatch.Cmd.Trim().TrimStart('/')} {args}".Trim();
                        }
                    }
                    else if (fastCMD.SelectedIndex > 0 && fastCMD.SelectedItem is FastCommandInfo selectedCmd)
                    {
                        // 下拉框不选择“/”时，不触发别名（下拉框选择了某个快捷指令）
                        finalCmd = $"{selectedCmd.Cmd.Trim().TrimStart('/')} {inputText}".Trim();
                    }
                }

                // 发送命令
                ServerService.SendCommand(finalCmd);
                cmdtext.Clear();
            }
            catch (Exception ex)
            {
                fastCMD.SelectedIndex = 0;
                PrintLog(string.Format(LanguageManager.Instance["SR_SendCmdError"], ex.Message), Colors.Red);
            }
        }

        private void sendcmd_Click(object sender, RoutedEventArgs e)
        {
            SendCommand();
        }

        private async void cmdtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (ServerService.ServerTerm != null && e.Key == Key.Tab)
            {
                if (completionPopup.IsOpen)
                {
                    return;
                }
                e.Handled = true; // 阻止焦点跳转
                await TriggerCompletion();
                return;
            }

            if (e.Key == Key.Enter) { SendCommand(); }
        }

        private void cmdtext_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (completionPopup.IsOpen)
            {
                if (e.Key == Key.Up)
                {
                    completionList.Focus();
                    if (completionList.SelectedIndex > 0)
                        completionList.SelectedIndex--;
                    return;
                }
                if (e.Key == Key.Down)
                {
                    completionList.Focus();
                    if (completionList.SelectedIndex < completionList.Items.Count)
                        completionList.SelectedIndex++;
                    return;
                }
                if (e.Key == Key.Escape || e.Key == Key.Back)
                {
                    CloseCompletion();
                    e.Handled = true;
                    return;
                }
            }
        }

        private async Task TriggerCompletion()
        {
            if (ServerService.ServerTerm == null || !ServerService.ServerTerm.IsRunning) return;

            completionList.Items.Clear();
            completionList.Items.Add(LanguageManager.Instance["SR_FetchingCompletion"]);
            completionPopup.IsOpen = true;

            var candidates = await ServerService.ServerTerm.RequestCompletionAsync(cmdtext.Text);

            completionList.Items.Clear();

            if (candidates.Count == 0)
            {
                CloseCompletion();
                return;
            }

            foreach (var c in candidates)
            {
                if (!c.StartsWith("(") && !c.EndsWith(")"))
                    completionList.Items.Add(c);
            }

            completionList.SelectedIndex = 0;
        }

        private void CloseCompletion()
        {
            completionPopup.IsOpen = false;
            completionList.Items.Clear();
            cmdtext.Focus();
        }

        // 列表键盘操作
        private void completionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                ApplyCompletion();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape || e.Key == Key.Back)
            {
                CloseCompletion();
                e.Handled = true;
            }
        }

        private void completionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => ApplyCompletion();

        private void ApplyCompletion()
        {
            if (completionList.SelectedItem is string selected)
            {
                // 替换输入框内容：保留已输入的前缀空格+新词
                var parts = cmdtext.Text.Split(' ');
                parts[parts.Length - 1] = selected;
                cmdtext.Text = string.Join(" ", parts) + " ";
                cmdtext.CaretIndex = cmdtext.Text.Length;
            }
            CloseCompletion();
            cmdtext.Focus();
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
                if (ServerService.ServerTerm != null)
                {
                    ServerService.ServerTerm.Stop();
                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_Stopping"]);
                    ServerService.StopServer();
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
                    if (ServerService.ServerTerm != null)
                    {
                        ServerService.ServerTerm.Kill();
                    }
                    else
                    {
                        ServerService.ServerProcess.Kill();
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

        // 路径快捷属性
        private string PluginsDir => Path.Combine(ServerService.ServerBase, "plugins");
        private string ModsDir => Path.Combine(ServerService.ServerBase, "mods");

        // 刷新
        private void ReFreshPluginsAndMods()
        {
            RefreshTab(
                directory: PluginsDir,
                tabItem: pluginsTabItem,
                managedCard: ManagePluginsCard,
                createNoContent: () =>
                {
                    var tips = new NoPlugins();
                    tips.RefreshCommand = new RelayCommand(_ => ReFreshPluginsAndMods());
                    return tips;
                },
                bindList: () =>
                {
                    pluginslist.ItemsSource = FileListManager.LoadItems<SR_PluginInfo>(
                        PluginsDir,
                        (name, _) => new SR_PluginInfo(name));
                });

            RefreshTab(
                directory: ModsDir,
                tabItem: modsTabItem,
                managedCard: ManageModsCard,
                createNoContent: () =>
                {
                    var tips = new NoMods();
                    tips.RefreshCommand = new RelayCommand(_ => ReFreshPluginsAndMods());
                    return tips;
                },
                bindList: () =>
                {
                    modslist.ItemsSource = FileListManager.LoadItems<SR_ModInfo>(
                        ModsDir,
                        (name, _) => new SR_ModInfo(name));
                });
        }

        /// <summary>
        /// 通用 Tab 刷新：目录存在则显示管理卡片并绑定列表，否则显示占位提示。
        /// </summary>
        private static void RefreshTab(
            string directory,
            System.Windows.Controls.TabItem tabItem,
            UIElement managedCard,
            System.Func<UIElement> createNoContent,
            System.Action bindList)
        {
            if (Directory.Exists(directory))
            {
                tabItem.Content = managedCard;
                bindList();
            }
            else
            {
                tabItem.Content = createNoContent();
            }
        }

        /// <summary>
        /// 如果服务器正在运行或列表无选中项，则弹提示并返回 false。
        /// </summary>
        private bool GuardCanOperate(System.Collections.IList selectedItems, string itemTypeName)
        {
            if (ServerService.CheckServerRunning())
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ServerRunningCantOp"], LanguageManager.Instance["Warning"]);
                return false;
            }
            if (selectedItems.Count == 0)
            {
                MagicFlowMsg.ShowMessage(string.Format(LanguageManager.Instance["SR_SelectAtLeastOne"], itemTypeName), 3);
                return false;
            }
            return true;
        }

        // 插件事件
        private void disPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardCanOperate(pluginslist.SelectedItems, LanguageManager.Instance["SR_Plugin"])) return;
            try { FileListManager.ToggleDisabled(PluginsDir, pluginslist.SelectedItems.Cast<SR_PluginInfo>()); }
            catch { return; }
            ReFreshPluginsAndMods();
        }

        private void delPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardCanOperate(pluginslist.SelectedItems, LanguageManager.Instance["SR_Plugin"])) return;
            try { FileListManager.DeleteItems(PluginsDir, pluginslist.SelectedItems.Cast<SR_PluginInfo>()); }
            catch { return; }
            ReFreshPluginsAndMods();
        }

        private void disAllPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardServerRunning()) return;
            try { FileListManager.ToggleDisabled(PluginsDir, pluginslist.Items.Cast<SR_PluginInfo>()); }
            catch { }
            ReFreshPluginsAndMods();
        }

        private void addPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (TryPickJarFiles(out var files, out var names))
            {
                FileListManager.CopyFilesTo(PluginsDir, files, names);
                ReFreshPluginsAndMods();
            }
        }

        // 模组事件
        private void disMod_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardCanOperate(modslist.SelectedItems, LanguageManager.Instance["SR_Mod"])) return;
            try { FileListManager.ToggleDisabled(ModsDir, modslist.SelectedItems.Cast<SR_ModInfo>()); }
            catch { return; }
            ReFreshPluginsAndMods();
        }

        private void delMod_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardCanOperate(modslist.SelectedItems, LanguageManager.Instance["SR_Mod"])) return;
            try { FileListManager.DeleteItems(ModsDir, modslist.SelectedItems.Cast<SR_ModInfo>()); }
            catch { return; }
            ReFreshPluginsAndMods();
        }

        private void disAllMod_Click(object sender, RoutedEventArgs e)
        {
            if (!GuardServerRunning()) return;
            try { FileListManager.ToggleDisabled(ModsDir, modslist.Items.Cast<SR_ModInfo>()); }
            catch { }
            ReFreshPluginsAndMods();
        }

        private void addMod_Click(object sender, RoutedEventArgs e)
        {
            if (TryPickJarFiles(out var files, out var names))
            {
                FileListManager.CopyFilesTo(ModsDir, files, names);
                ReFreshPluginsAndMods();
            }
        }

        // 其余独立事件
        private void reFresh_Click(object sender, RoutedEventArgs e)
            => ReFreshPluginsAndMods();

        private void openpluginsDir_Click(object sender, RoutedEventArgs e)
            => OpenExplorer(PluginsDir);

        private void openmodsDir_Click(object sender, RoutedEventArgs e)
            => OpenExplorer(ModsDir);

        private async void addModsTip_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = await MagicShow.ShowMsgDialogAsync(this,
                LanguageManager.Instance["SR_ModDescTip"],
                LanguageManager.Instance["Tip"], true, LanguageManager.Instance["Cancel"]);

            if (confirmed)
                Process.Start("https://zhidao.baidu.com/question/927720370906860259.html");
        }

        private void DownloadPluginBtn_Click(object sender, RoutedEventArgs e)
            => OpenDownloadDialog(PluginsDir, resourceType: 1, pageIndex: 2);

        private void DownloadModBtn_Click(object sender, RoutedEventArgs e)
            => OpenDownloadDialog(ModsDir, resourceType: 0, pageIndex: 0);

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var item = Functions.FindAncestor<ListViewItem>(button);
            if (item != null) item.IsSelected = true;

            if (button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }

        // 私有辅助
        /// <summary>仅检查服务器是否运行，不检查选中项（用于"全部"操作）。</summary>
        private bool GuardServerRunning()
        {
            try
            {
                if (ServerService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ServerRunningCantOp"], LanguageManager.Instance["Warning"]);
                    return false;
                }
            }
            catch { }
            return true;
        }

        /// <summary>打开 JAR 文件选择对话框，成功选择时输出文件路径和安全文件名。</summary>
        private static bool TryPickJarFiles(out string[] files, out string[] safeNames)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Multiselect = true,
                Title = LanguageManager.Instance["SR_SelectFile"],
                Filter = LanguageManager.Instance["SR_JarFileFilter"]
            };

            if (dialog.ShowDialog() == true)
            {
                files = dialog.FileNames;
                safeNames = dialog.SafeFileNames;
                return true;
            }

            files = safeNames = System.Array.Empty<string>();
            return false;
        }

        /// <summary>用资源管理器打开指定目录。</summary>
        private static void OpenExplorer(string directory)
            => Process.Start(new ProcessStartInfo("explorer.exe", directory));

        /// <summary>打开下载模组/插件的对话框。</summary>
        private void OpenDownloadDialog(string targetDir, int resourceType, int pageIndex)
        {
            var tempContent = this.Content;
            DownloadMod downloadModPage = null;
            downloadModPage = new DownloadMod((string filename) =>
            {
                ReFreshPluginsAndMods();
                this.Content = tempContent;
                downloadModPage.Dispose();
                downloadModPage = null;
            }, targetDir, resourceType, pageIndex, false);

            this.Content = downloadModPage;
        }

        // 检测客户端模组
        private async void detectClientMods_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(ServerService.ServerBase + @"\mods"))
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_NoModsFolder"], LanguageManager.Instance["Error"]);
                return;
            }

            addModBtn.IsEnabled = false;

            // 异步执行检测
            var resultList = await Task.Run(() =>
            {
                List<SR_ModInfo> list = new List<SR_ModInfo>();
                DirectoryInfo directoryInfo = new DirectoryInfo(ServerService.ServerBase + @"\mods");
                FileInfo[] files = directoryInfo.GetFiles("*.*");

                // 临时存放客户端模组和普通模组
                List<SR_ModInfo> clientMods = new List<SR_ModInfo>();
                List<SR_ModInfo> normalMods = new List<SR_ModInfo>();
                List<SR_ModInfo> disabledMods = new List<SR_ModInfo>();

                foreach (FileInfo f in files)
                {
                    if (f.Name.EndsWith(".disabled"))
                    {
                        disabledMods.Add(new SR_ModInfo(f.Name.Replace(".disabled", "")) { IsDisabled = true });
                    }
                    else if (f.Name.EndsWith(".jar"))
                    {
                        // 检测是否为客户端模组
                        if (IsClientSideMod(f.FullName))
                        {
                            clientMods.Add(new SR_ModInfo(f.Name) { IsClient = true });
                        }
                        else
                        {
                            normalMods.Add(new SR_ModInfo(f.Name) { IsClient = false });
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
                MagicShow.ShowMsgDialog(this, string.Format(LanguageManager.Instance["SR_ClientModDetectResult"], clientCount), LanguageManager.Instance["SR_DetectResult"]);
            }
            else
            {
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_NoClientMods"], 3);
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
            catch { }
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

        private async Task LoadSettings()
        {
            try
            {
                //检测是否自定义模式
                if (ServerService.ServerMode == 1)
                {
                    LabelArgsText.Content = LanguageManager.Instance["SR_CustomArgs"];
                    GridServerCore.Visibility = Visibility.Collapsed;
                    GridJavaSet.Visibility = Visibility.Collapsed;
                    GridJavaRem.Visibility = Visibility.Collapsed;
                    DivJavaSet.Visibility = Visibility.Collapsed;
                    DivJvmSet.Visibility = Visibility.Collapsed;
                    DivRemSet.Visibility = Visibility.Collapsed;
                    DivYggdrasilSet.Visibility = Visibility.Collapsed;
                    GridYggdrasilSet.Visibility = Visibility.Collapsed;
                    TextArgsTips.Text = LanguageManager.Instance["SR_CustomArgsHint"];
                }
                else
                {
                    LabelArgsText.Content = LanguageManager.Instance["SR_JvmArgs"];
                    GridServerCore.Visibility = Visibility.Visible;
                    GridJavaSet.Visibility = Visibility.Visible;
                    GridJavaRem.Visibility = Visibility.Visible;
                    DivJavaSet.Visibility = Visibility.Visible;
                    DivJvmSet.Visibility = Visibility.Visible;
                    DivRemSet.Visibility = Visibility.Visible;
                    TextArgsTips.Text = LanguageManager.Instance["SR_JvmArgsHint"];
                }
                nAme.Text = ServerService.ServerName;
                server.Text = ServerService.ServerCore;
                memorySlider.Maximum = Functions.GetPhysicalMemoryMB();
                bAse.Text = ServerService.ServerBase;
                jVMcmd.Text = ServerService.ServerArgs;
                jAva.Text = ServerService.ServerJava;

                _ = Task.Run(LoadJavaInfo);

                var RserverJVM = ServerService.ServerMem ?? string.Empty;  // 之前的JVM就是重构后的MEM，千万不要再搞混了QWQ
                if (RserverJVM == "")
                {
                    memorySlider.IsEnabled = false;
                    autoSetMemory.IsChecked = true;
                    memoryInfo.Text = LanguageManager.Instance["SR_MemAutoAlloc"];
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
                        memoryInfo.Text = string.Format(LanguageManager.Instance["SR_MemMinMax"], minMemory, maxMemory);
                    }
                    catch (Exception ex)
                    {
                        memorySlider.ValueStart = 0;
                        memorySlider.ValueEnd = 0;
                        memoryInfo.Text = LanguageManager.Instance["SR_MemParseFailed"];
                        Console.WriteLine(string.Format(LanguageManager.Instance["SR_ErrorPrefix"], ex.Message));
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

        private async void LoadJavaInfo()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    selectCheckedJavaComb.ItemsSource = null;
                    selectCheckedJavaComb.Items.Clear();
                    selectCheckedJavaComb.ItemsSource = AppConfig.Current.JavaList;
                    selectCheckedJavaComb.SelectedIndex = 0;
                });
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
                string response = (await HttpService.GetApiContentAsync("jdk?os=windows&arch=x64"))["data"].ToString();
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
                if (jAva.Text == "Java")
                {
                    useJvpath.IsChecked = true;
                }
                else
                {
                    // 使用正则表达式来提取Java版本
                    Regex pattern = new Regex(@"MSL\\Java\\(\d+)");
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

        private async void refreahConfig_Click(object sender, RoutedEventArgs e)
        {
            await LoadSettings();
        }

        private async void doneBtn1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServerService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_CantChangeWhileRunning"], LanguageManager.Instance["Error"]);
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
                    ServerService.ServerMem = "";
                }
                else
                {
                    ServerService.ServerMem = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                }
                if (ServerService.ServerMode == 0)
                {
                    if (useDownJv.IsChecked == true)
                    {
                        Growl.Info(LanguageManager.Instance["SR_GettingJavaPath"]);
                        try
                        {
                            var selectedJava = selectJava.SelectedValue?.ToString();
                            var (Status, JavaPath, Msg) = await Functions.DownloadJava(this, selectedJava,
                                (await HttpService.GetApiContentAsync("download/jdk/" + selectedJava + "?os=windows&arch=x64"))["data"]["url"].ToString());
                            doneBtn1.IsEnabled = true;
                            refreahConfig.IsEnabled = true;
                            if (Status == 1 || Status == 2)
                            {
                                Growl.Info(LanguageManager.Instance["SR_JavaDownloadDone"]);
                                jAva.Text = JavaPath;
                            }
                            else if (Status == 3)
                            {
                                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_DownloadCancelled"], LanguageManager.Instance["Tip"]);
                                return;
                            }
                            else
                            {
                                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_DownloadFailedMsg"] + "\n" + Msg, LanguageManager.Instance["Error"]);
                                return;
                            }
                        }
                        catch
                        {
                            doneBtn1.IsEnabled = true;
                            refreahConfig.IsEnabled = true;
                            Growl.Error(LanguageManager.Instance["SR_NetworkError"]);
                            return;
                        }
                    }
                    else if (useSelf.IsChecked == true)
                    {
                        if (!Path.IsPathRooted(jAva.Text))
                        {
                            jAva.Text = AppDomain.CurrentDomain.BaseDirectory.ToString() + jAva.Text;
                        }
                        Growl.Info(LanguageManager.Instance["SR_CheckingJava"]);
                        (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync(jAva.Text);
                        if (javaAvailability)
                        {
                            Growl.Success(LanguageManager.Instance["SR_JavaAvailable"] + javainfo);
                        }
                        else
                        {
                            MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_JavaCheckFailed"], LanguageManager.Instance["Error"]);
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
                ServerService.ServerName = nAme.Text;
                Title = ServerService.ServerName;
                ServerService.ServerJava = jAva.Text;
                string fullFileName;
                var Rserverjava = ServerService.ServerJava;
                var Rserverbase = ServerService.ServerBase;
                if (File.Exists(ServerService.ServerBase + "\\" + server.Text))
                {
                    fullFileName = ServerService.ServerBase + "\\" + server.Text;
                }
                else
                {
                    fullFileName = server.Text;
                }
                if (Functions.CheckForgeInstaller(fullFileName))
                {
                    bool dialog = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_ForgeInstaller"], LanguageManager.Instance["Tip"], true, LanguageManager.Instance["Cancel"]);
                    if (dialog)
                    {
                        string installReturn;
                        //调用新版forge安装器
                        string[] installForge = await MagicShow.ShowInstallForge(this, ServerService.ServerBase, server.Text, Rserverjava);
                        if (installForge[0] == "0")
                        {
                            if (await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_AutoInstallFailed"], LanguageManager.Instance["Error"], true))
                            {
                                installReturn = Functions.InstallForge(Rserverjava, ServerService.ServerBase, server.Text, string.Empty, false);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (installForge[0] == "1")
                        {
                            string _ret = Functions.InstallForge(Rserverjava, ServerService.ServerBase, server.Text, installForge[1]);
                            if (_ret == null)
                            {
                                installReturn = Functions.InstallForge(Rserverjava, ServerService.ServerBase, server.Text, installForge[1], false);
                            }
                            else
                            {
                                installReturn = _ret;
                            }
                        }
                        else if (installForge[0] == "3")
                        {
                            installReturn = Functions.InstallForge(Rserverjava, ServerService.ServerBase, server.Text, string.Empty, false);
                        }
                        else
                        {
                            return;
                        }
                        if (installReturn == null)
                        {
                            MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_DownloadFailedSimple"], LanguageManager.Instance["Error"]);
                            return;
                        }
                        server.Text = installReturn;
                    }
                }
                ServerService.ServerCore = server.Text;
                if (ServerService.ServerBase != bAse.Text)
                {
                    bool dialog = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_DirChanged"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                    if (dialog)
                    {
                        await Functions.MoveFolder(ServerService.ServerBase, bAse.Text);
                    }
                }
                ServerService.ServerBase = bAse.Text;
                ServerService.ServerArgs = jVMcmd.Text;

                //粗略检测外置登录地址的合法性
                if (YggdrasilAddr.Text.Length > 0 && !YggdrasilAddr.Text.Contains("http://") && !YggdrasilAddr.Text.Contains("https://"))
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_YggInvalid"], LanguageManager.Instance["Error"]);
                    doneBtn1.IsEnabled = true;
                    refreahConfig.IsEnabled = true;
                    return;
                }
                else
                {
                    ServerService.ServerYggAddr = YggdrasilAddr.Text;
                }

                // 检查备份相关设置参数的合法性
                try
                {
                    if (int.Parse(TextBackupMaxLimitCount.Text) < 0)
                    {
                        throw new Exception(LanguageManager.Instance["SR_MaxBackupGE0"]);
                    }
                    if (int.Parse(TextBackupDelay.Text) < 5)
                    {
                        throw new Exception(LanguageManager.Instance["SR_BackupDelayGE5"]);
                    }
                    if (ComboBackupPath.SelectedIndex == 2)
                    {
                        if (String.IsNullOrEmpty(TextBackupPath.Text) || TextBackupPath.Text.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        {
                            throw new Exception(LanguageManager.Instance["SR_CustomPathInvalid"]);
                        }
                        Path.GetFullPath(TextBackupPath.Text); // 这个东西能检测路径合法不 不合法会抛出异常~
                    }
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_BackupParamsError"] + "\n" + ex.Message, LanguageManager.Instance["Error"]);
                    doneBtn1.IsEnabled = true;
                    refreahConfig.IsEnabled = true;
                    return;
                }

                ServerService.InstanceConfig.Name = ServerService.ServerName;
                ServerService.InstanceConfig.Java = ServerService.ServerJava;
                ServerService.InstanceConfig.Base = ServerService.ServerBase;
                ServerService.InstanceConfig.Core = ServerService.ServerCore;
                ServerService.InstanceConfig.Memory = ServerService.ServerMem;
                ServerService.InstanceConfig.Args = ServerService.ServerArgs;
                ServerService.InstanceConfig.YggApi = ServerService.ServerYggAddr;
                ServerService.InstanceConfig.BackupConfigs = new ServerConfig.BackupConfig
                {
                    BackupMode = ComboBackupPath.SelectedIndex,
                    BackupMaxLimit = int.Parse(TextBackupMaxLimitCount.Text),
                    BackupCustomPath = TextBackupPath.Text,
                    BackupSaveDelay = int.Parse(TextBackupDelay.Text)
                };

                ServerConfig.Current.Save();
                await LoadSettings();
                SaveConfigEvent();

                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ChangeDone"], LanguageManager.Instance["Tip"]);
            }
            catch (Exception err)
            {
                MessageBox.Show(LanguageManager.Instance["SR_GeneralError"] + "\n" + err.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                doneBtn1.IsEnabled = true;
                refreahConfig.IsEnabled = true;
            }
        }

        private void a0_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = LanguageManager.Instance["SR_SelectFolder"];
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
                Title = LanguageManager.Instance["SR_SelectJarFile"],
                Filter = LanguageManager.Instance["SR_JarFileFilter"]
            };
            var res = openfile.ShowDialog();
            if (res == true)
            {
                server.Text = openfile.FileName;
                if (File.Exists(ServerService.ServerBase + "\\" + openfile.SafeFileName))
                {
                    server.Text = openfile.SafeFileName;
                }
                else
                {
                    if (Path.GetDirectoryName(openfile.FileName) != ServerService.ServerBase)
                    {
                        if (await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_CoreNotInDir"], LanguageManager.Instance["Tip"], true))
                        {
                            File.Copy(openfile.FileName, ServerService.ServerBase + @"\" + openfile.SafeFileName, true);
                            MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_CoreCopied"], LanguageManager.Instance["Tip"]);
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
            openfile.Title = LanguageManager.Instance["SR_SelectJavaExe"];
            openfile.Filter = LanguageManager.Instance["SR_ExeFileFilter"];
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
            var tempContent=this.Content;
            DownloadServer downloadServerPage = null;
            downloadServerPage = new DownloadServer((string filename) =>
            {
                if (File.Exists(ServerService.ServerBase + @"\" + filename))
                {
                    server.Text = filename;
                    Growl.Success(LanguageManager.Instance["SR_ServerDownloadDone"]);
                }
                else if (filename.StartsWith("@libraries/"))
                {
                    server.Text = filename;
                    Growl.Success(LanguageManager.Instance["SR_ServerDownloadDone"]);
                }
                this.Content = tempContent;
                downloadServerPage.Dispose();
                downloadServerPage = null;
            }, ServerService.ServerBase, DownloadServer.Mode.ChangeServerSettings, ServerService.ServerJava);
            
            this.Content = downloadServerPage;
        }

        private void autoSetMemory_Click(object sender, RoutedEventArgs e)
        {
            if (autoSetMemory.IsChecked == true)
            {
                memorySlider.IsEnabled = false;
                memoryInfo.Text = LanguageManager.Instance["SR_MemAutoAlloc"];
            }
            else
            {
                memorySlider.IsEnabled = true;
                memoryInfo.Text = string.Format(LanguageManager.Instance["SR_MemMinMax"], memorySlider.ValueStart.ToString("f0"), memorySlider.ValueEnd.ToString("f0"));
            }
        }
        private void memorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<DoubleRange> e)
        {
            memoryInfo.Text = string.Format(LanguageManager.Instance["SR_MemMinMax"], memorySlider.ValueStart.ToString("f0"), memorySlider.ValueEnd.ToString("f0"));
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
                Growl.Info(LanguageManager.Instance["SR_CheckingEnvVar"]);
                (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync("java");
                if (javaAvailability)
                {
                    Growl.Success(LanguageManager.Instance["SR_EnvVarOK"]);
                    useJvpath.Content = LanguageManager.Instance["SR_UseEnvVar"] + javainfo;
                }
                else
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_EnvVarFailed"], LanguageManager.Instance["Error"]);
                }
            }
        }

        private void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            if (selectCheckedJavaComb.Items.Count == 0)
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_SearchFirst"], LanguageManager.Instance["Warning"]);
                useSelf.IsChecked = true;
            }
        }

        private async void ScanJava_Click(object sender, RoutedEventArgs e)
        {
            List<JavaScanner.JavaInfo> strings = null;
            int dialog = MagicShow.ShowMsg(this, LanguageManager.Instance["SR_JavaDetectIntro"], LanguageManager.Instance["Tip"], true, LanguageManager.Instance["SR_DeepDetect"], LanguageManager.Instance["SR_SimpleDetect"]);
            if (dialog == 2)
            {
                return;
            }
            Dialog waitDialog = Dialog.Show(new TextDialog(LanguageManager.Instance["SR_Scanning"]));
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
                AppConfig.Current.JavaList.Clear();
                var javaList = strings.Select(info => $"Java{info.Version}: {info.Path}").ToList();
                selectCheckedJavaComb.ItemsSource = null;
                selectCheckedJavaComb.Items.Clear();
                selectCheckedJavaComb.ItemsSource = javaList;
                AppConfig.Current.JavaList = javaList;
                AppConfig.Current.Save();
            }
            if (selectCheckedJavaComb.Items.Count > 0)
            {
                Growl.Success(LanguageManager.Instance["SR_CheckComplete"]);
                selectCheckedJavaComb.SelectedIndex = 0;
            }
            else
            {
                Growl.Error(LanguageManager.Instance["SR_NoJavaFound"]);
            }
        }

        private async void getLaunchercode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string content;
                var Rserverserver = ServerService.ServerCore;
                var Rserverjava = ServerService.ServerJava;
                var RserverJVM = ServerService.ServerMem;
                var RserverJVMcmd = ServerService.ServerArgs;
                if (ServerService.ServerMode == 0)
                {
                    string ygg_api_jvm = "";
                    // 处理外置登录
                    if (!string.IsNullOrEmpty(ServerService.ServerYggAddr))
                    {
                        ygg_api_jvm = $"-javaagent:authlib-injector.jar={ServerService.ServerYggAddr} ";
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


                string filePath = Path.Combine(ServerService.ServerBase, "StartServer.bat");
                File.WriteAllText(filePath, content, Encoding.Default);
                MessageBox.Show(LanguageManager.Instance["SR_ScriptFile"] + ServerService.ServerBase + @"\StartServer.bat", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start("explorer.exe", ServerService.ServerBase);
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
            Growl.Info(LanguageManager.Instance["SR_FetchingWait"]);
            try
            {
                HttpResponse response = await HttpService.GetAsync("https://6.ipw.cn");
                if (response?.HttpResponseCode == HttpStatusCode.OK)
                {
                    string ipv6 = response?.HttpResponseContent.ToString();
                    Clipboard.Clear();
                    Clipboard.SetText(ipv6);
                    MagicShow.ShowMsgDialog(this, string.Format(LanguageManager.Instance["SR_IPv6Success"], ipv6), LanguageManager.Instance["SR_IPv6SuccessTitle"]);
                }
                else
                {
                    throw new Exception(response?.HttpResponseContent.ToString());
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_IPv6Failed"] + ex.Message, LanguageManager.Instance["SR_IPv6FailedTitle"]);
            }
            finally
            {
                GetIPV6.IsEnabled = true;
            }
        }

        private void autostartServer_Click(object sender, RoutedEventArgs e)
        {
            ServerService.InstanceConfig.AutoStartServer = autoStartserver.IsChecked == true;
            ServerConfig.Current.Save();
        }

        private void inputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (inputCmdEncoding.Content.ToString() == "ANSI")
            {
                ServerService.InstanceConfig.EncodingIn = "UTF8";
                inputCmdEncoding.Content = "UTF8";
            }
            else if (inputCmdEncoding.Content.ToString() == "UTF8")
            {
                ServerService.InstanceConfig.EncodingIn = "ANSI";
                inputCmdEncoding.Content = "ANSI";
            }
            ServerConfig.Current.Save();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_EncodingChanged"], 1);
        }

        private void outputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (outputCmdEncoding.Content.ToString() == "ANSI")
            {
                ServerService.InstanceConfig.EncodingOut = "UTF8";
                outputCmdEncoding.Content = "UTF8";
            }
            else if (outputCmdEncoding.Content.ToString() == "UTF8")
            {
                ServerService.InstanceConfig.EncodingOut = "ANSI";
                outputCmdEncoding.Content = "ANSI";
            }
            ServerConfig.Current.Save();
            try
            {
                if (ServerService.CheckServerRunning())
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_EncodingRestartRequired"], 3);

                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_EncodingChanged"], 1);
                }
            }
            catch
            {
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_EncodingChanged"], 1);
            }
        }
        private void fileforceUTF8encoding_Click(object sender, RoutedEventArgs e)
        {
            ServerService.InstanceConfig.FileForceUTF8 = fileforceUTF8encoding.IsChecked == true;
            ServerConfig.Current.Save();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_SettingRestartRequired"], 1);
        }

        private void KillProcessTreeTogBtn_Click(object sender, RoutedEventArgs e)
        {
            ServerService.InstanceConfig.KillProcessTree = KillProcessTreeTogBtn.IsChecked == true;
            ServerConfig.Current.Save();
        }

        private void useConpty_Click(object sender, RoutedEventArgs e)
        {
            if (ServerService.CheckServerRunning())
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_CloseServerFirst"], LanguageManager.Instance["Tip"]);
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
            if (useConpty.IsChecked == false)
            {
                ServerEncodingSettings.Visibility = Visibility.Visible;
                ServerService.InstanceConfig.UseConpty = false;
            }
            else
            {
                ServerEncodingSettings.Visibility = Visibility.Collapsed;
                ServerService.InstanceConfig.UseConpty = true;
            }
            ServerConfig.Current.Save();
        }

        private async void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServerService.CheckServerRunning())
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_ServerRunningCloseConfirm"], LanguageManager.Instance["Tip"]);
                    if (!dialogRet)
                    {
                        return;
                    }
                    ServerService.ServerProcess.StandardInput.WriteLine("stop");
                }
                try
                {
                    string path1 = ServerService.ServerBase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = ServerService.ServerBase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ModifyDone"], LanguageManager.Instance["Tip"]);
                }
                catch (Exception a)
                {
                    MessageBox.Show(LanguageManager.Instance["SR_OnlineModeError"] + a.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch
            {
                try
                {
                    string path1 = ServerService.ServerBase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = ServerService.ServerBase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_ModifyDone"], LanguageManager.Instance["Tip"]);
                }
                catch (Exception a)
                {
                    MessageBox.Show(LanguageManager.Instance["SR_OnlineModeError"] + a.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void showOutlog_Click(object sender, RoutedEventArgs e)
        {
            if (showOutlog.IsChecked == true)
            {
                ServerService.ServerLogHandler.IsShowOutLog = true;
                ServerService.InstanceConfig.ShowOutlog = true;
            }
            else
            {
                ServerService.ServerLogHandler.IsShowOutLog = false;
                ServerService.InstanceConfig.ShowOutlog = false;
            }
            ServerConfig.Current.Save();
        }

        private void formatOutHead_Click(object sender, RoutedEventArgs e)
        {
            if (formatOutHead.IsChecked == true)
            {
                ServerService.ServerLogHandler.IsFormatLogPrefix = true;
                ServerService.InstanceConfig.FormatLogPrefix = true;
            }
            else
            {
                ServerService.ServerLogHandler.IsFormatLogPrefix = false;
                ServerService.InstanceConfig.FormatLogPrefix = false;
            }
            ServerConfig.Current.Save();
        }

        private void shieldLogBtn_Click(object sender, RoutedEventArgs e)
        {
            if (shieldLogBtn.IsChecked == true)
            {
                if (ShieldLogList.Items.Count > 0)
                {
                    List<string> tempList = new List<string>();

                    foreach (var item in ShieldLogList.Items)
                    {
                        tempList.Add(item.ToString());
                    }

                    ServerService.ServerLogHandler.ShieldLog = [.. tempList];
                    ServerService.InstanceConfig.ShieldLogs = tempList;
                    LogShield_Add.IsEnabled = false;
                    LogShield_Del.IsEnabled = false;
                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_AddFirst"], 2);
                    shieldLogBtn.IsChecked = false;
                }
            }
            else
            {
                ServerService.ServerLogHandler.ShieldLog = null;
                ServerService.InstanceConfig.ShieldLogs.Clear();
                LogShield_Add.IsEnabled = true;
                LogShield_Del.IsEnabled = true;
            }
            ServerConfig.Current.Save();
        }

        private async void LogShield_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(this, LanguageManager.Instance["SR_ShieldLogInput"]);
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
            if (highLightLogBtn.IsChecked == true)
            {
                if (HighLightLogList.Items.Count > 0)
                {
                    List<string> tempList = new List<string>();

                    foreach (var item in HighLightLogList.Items)
                    {
                        tempList.Add(item.ToString());
                    }

                    ServerService.ServerLogHandler.HighLightLog = [.. tempList];
                    ServerService.InstanceConfig.HighLightLogs = tempList;

                    LogHighLight_Add.IsEnabled = false;
                    LogHighLight_Del.IsEnabled = false;
                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_AddFirst"], 2);
                    highLightLogBtn.IsChecked = false;
                }
            }
            else
            {
                ServerService.ServerLogHandler.HighLightLog = null;
                ServerService.InstanceConfig.HighLightLogs.Clear();
                LogHighLight_Add.IsEnabled = true;
                LogHighLight_Del.IsEnabled = true;
            }
            ServerConfig.Current.Save();
        }

        private async void LogHighLight_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(this, LanguageManager.Instance["SR_HighlightInput"]);
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
            if (shieldStackOut.IsChecked == false)
            {
                ServerService.ServerLogHandler.IsShieldStackOut = false;
                ServerService.InstanceConfig.ShieldStackOut = false;
            }
            else
            {
                ServerService.ServerLogHandler.IsShieldStackOut = true;
                ServerService.InstanceConfig.ShieldStackOut = true;
            }
            ServerConfig.Current.Save();
        }

        private async void autoClearOutlog_Click(object sender, RoutedEventArgs e)
        {
            if (autoClearOutlog.IsChecked == false)
            {
                bool msgreturn = await MagicShow.ShowMsgDialogAsync(this,
                    LanguageManager.Instance["SR_AutoClearWarning"],
                    LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                if (msgreturn)
                {
                    ServerService.InstanceConfig.AutoClearOutlog = false;
                }
                else
                {
                    autoClearOutlog.IsChecked = true;
                }
            }
            else
            {
                ServerService.InstanceConfig.AutoClearOutlog = true;
            }
            ServerConfig.Current.Save();
        }

        private void logsAnalyse_Click(object sender, RoutedEventArgs e)
        {
            OpenAILogAnalyseDialog();
        }

        private void OpenAILogAnalyseDialog()
        {
            LogAnalysisDialog logAnalysisDialog = new LogAnalysisDialog(this, ServerService.ServerBase, ServerService.ServerCore);
            Dialog dialog = Dialog.Show(logAnalysisDialog);
            dialog.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            dialog.VerticalContentAlignment = VerticalAlignment.Stretch;
            logAnalysisDialog.SelfDialog = dialog;
        }

        #region 上传日志到mclo.gs

        private async void shareLog_Click(object sender, RoutedEventArgs e)
        {
            shareLog.IsEnabled = false;
            Growl.Info(LanguageManager.Instance["SR_PleaseWait"]);
            string logs = string.Empty;
            string uploadMode = "A";
            if (File.Exists(ServerService.ServerBase + "\\logs\\latest.log"))
            {
                FileStream fileStream = new FileStream(ServerService.ServerBase + "\\logs\\latest.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                Growl.Info(LanguageManager.Instance["SR_LogEmpty"]);
                shareLog.IsEnabled = true;
                return;
            }
            Growl.Info(string.Format(LanguageManager.Instance["SR_Uploading"], uploadMode));
            //启动线程上传日志
            await UploadLogs(logs, true);
            shareLog.IsEnabled = true;
        }

        private string[] GetLogOtherPlan()
        {
            string[] strings = new string[2];

            strings[0] = "C";
            strings[1] = outlog.Text;

            return strings;
        }

        private async Task UpdateLogOtherPlan()
        {
            Growl.Info(LanguageManager.Instance["SR_PleaseWait"]);
            string logs = string.Empty;
            string uploadMode = "A";

            string[] strings = GetLogOtherPlan();
            uploadMode = strings[0];
            logs = strings[1];

            if (string.IsNullOrEmpty(logs))
            {
                Growl.Info(LanguageManager.Instance["SR_LogEmpty"]);
                shareLog.IsEnabled = true;
                return;
            }
            Growl.Info(string.Format(LanguageManager.Instance["SR_Uploading"], uploadMode));
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
                        Growl.Success(string.Format(LanguageManager.Instance["SR_UploadSuccess"], jsonResponse.url));
                    }
                    else
                    {
                        Growl.Error(LanguageManager.Instance["SR_UploadFailed"] + jsonResponse.error);
                    }
                }
                catch
                {
                    Growl.Error(LanguageManager.Instance["SR_ParseFailed"]);
                }
            }
            else
            {
                if (canUseOtherPlan)
                {
                    if ((await MagicShow.ShowMsgDialogAsync(this, string.Format(LanguageManager.Instance["SR_LogUploadBigFail"], response.HttpResponseCode + " " + response.HttpResponseContent), LanguageManager.Instance["Error"], true) == true))
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
                    Growl.Error(string.Format(LanguageManager.Instance["SR_LogUploadManualFail"], response.HttpResponseCode + " " + response.HttpResponseContent));
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
                if (File.Exists(Path.Combine(ServerService.ServerBase, "msl-installForge.log")))
                {
                    logsContent = "[MSL端处理日志]\n" + File.ReadAllText(Path.Combine(ServerService.ServerBase, "msl-installForge.log"));
                }
                if (File.Exists(Path.Combine(ServerService.ServerBase, "msl-compileForge.log")))
                {
                    logsContent = logsContent + "\n[Java端编译日志]\n" + File.ReadAllText(Path.Combine(ServerService.ServerBase, "msl-compileForge.log"));
                }
                if (logsContent == "")
                {
                    Growl.Error(LanguageManager.Instance["SR_ForgeLogNotFound"]);
                }
                else
                {
                    //启动线程上传日志
                    await UploadLogs(logsContent);
                    Growl.Info(LanguageManager.Instance["SR_UploadingDots"]);
                }
            }
            catch (Exception ex)
            {
                Growl.Error(LanguageManager.Instance["SR_ForgeLogUploadFail"] + ex.Message);
            }
        }

        #endregion

        private void GetFastCmd()
        {
            CurrentFastCmds.Clear();
            CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/", Remark = LanguageManager.Instance["SR_CmdRemark"] });

            var config = ServerService.InstanceConfig;
            if (config.FastCmds != null && config.FastCmds.Count > 0)
            {
                foreach (var item in config.FastCmds)
                {
                    // Config类 --> Utils类
                    CurrentFastCmds.Add(new FastCommandInfo
                    {
                        Cmd = item.Cmd,
                        Remark = item.Remark,
                        Alias = item.Alias
                    });
                }
            }
            else
            {
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/op", Remark = LanguageManager.Instance["SR_SetAdmin"] });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/deop", Remark = LanguageManager.Instance["SR_RemoveAdmin"] });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/ban", Remark = LanguageManager.Instance["SR_BanPlayerCmd"] });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/pardon", Remark = LanguageManager.Instance["SR_UnbanPlayer"] });
                CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/say", Remark = LanguageManager.Instance["SR_SayAll"] });
            }

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
            // Utils类 --> Config类
            ServerService.InstanceConfig.FastCmds = CurrentFastCmds.Skip(1)
                .Select(c => new ServerConfig.FastCommandInfo
                {
                    Cmd = c.Cmd,
                    Remark = c.Remark,
                    Alias = c.Alias
                }).ToList();
            ServerConfig.Current.Save();
            GetFastCmd();
        }

        private void refrushFastCmd_Click(object sender, RoutedEventArgs e)
        {
            GetFastCmd();
        }

        private void resetFastCmd_Click(object sender, RoutedEventArgs e)
        {
            if (ServerService.InstanceConfig.FastCmds == null || ServerService.InstanceConfig.FastCmds.Count == 0)
            {
                return;
            }
            else
            {
                ServerService.InstanceConfig.FastCmds = null;
                ServerConfig.Current.Save();
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_RestartWindowHint"], LanguageManager.Instance["Tip"]);
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

                uniformStack.Children.Add(new TextBlock { Text = LanguageManager.Instance["SR_CmdInputHint"] });
                uniformStack.Children.Add(cmdBox);
                uniformStack.Children.Add(new TextBlock { Text = LanguageManager.Instance["SR_RemarkInputHint"] });
                uniformStack.Children.Add(remarkBox);
                uniformStack.Children.Add(new TextBlock { Text = LanguageManager.Instance["SR_AliasInputHint"] });
                uniformStack.Children.Add(aliasBox);

                await MagicShow.ShowMsgDialogAsync(this, LanguageManager.Instance["SR_AddFastCmd"], LanguageManager.Instance["SR_Input"], uIElement: uniformStack);

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
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_CmdEmpty"], 2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.Instance["SR_AddFailed"] + ex.Message);
            }
        }

        private void delFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fastCmdList.SelectedIndex <= 0)
                {
                    MessageBox.Show(LanguageManager.Instance["SR_CantDeleteRoot"]);
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

        // 数据结构
        private SortedDictionary<int, bool> taskFlag = new SortedDictionary<int, bool>();  // 存储任务ID，以及状态（是否正在运行）
        private Dictionary<int, string> taskCrons = new Dictionary<int, string>();  // Cron 表达式字符串
        private Dictionary<int, string> taskCmds = new Dictionary<int, string>();  // 要执行的服务器指令
        private Dictionary<int, CancellationTokenSource> taskCtsMap = new Dictionary<int, CancellationTokenSource>(); // 每个任务的取消令牌
        // 默认值
        private const string DefaultCron = "0 */10 * * * *";   // 每10分钟
        private const string DefaultCmd = "say Hello World!";

        // 解析 Cron
        private bool TryParseCron(string expression, out CronExpression cron)
        {
            cron = null;
            try
            {
                cron = CronExpression.Parse(expression, CronFormat.IncludeSeconds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 添加任务
        private void addTask_Click(object sender, RoutedEventArgs e)
        {
            int newId = taskFlag.Count == 0 ? 0 : taskFlag.Keys.Max() + 1;
            taskFlag.Add(newId, false);
            taskCrons.Add(newId, DefaultCron);
            taskCmds.Add(newId, DefaultCmd);

            RefreshTaskList();
            loadOrSaveTaskConfig.Content = LanguageManager.Instance["SR_SaveTaskConfig"];
        }

        // 删除任务
        private void delTask_Click(object sender, RoutedEventArgs e)
        {
            if (tasksList.SelectedIndex == -1) return;

            int selectedId = GetSelectedTaskId();
            if (taskFlag[selectedId])
            {
                MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_StopTaskFirst"], LanguageManager.Instance["Warning"]);
                return;
            }

            taskFlag.Remove(selectedId);
            taskCrons.Remove(selectedId);
            taskCmds.Remove(selectedId);
            // 顺带清理 CTS（正常不应存在，但还是清理下为好~）
            if (taskCtsMap.TryGetValue(selectedId, out var cts))
            {
                cts.Dispose();
                taskCtsMap.Remove(selectedId);
            }

            RefreshTaskList();

            if (tasksList.Items.Count == 0)
                loadOrSaveTaskConfig.Content = LanguageManager.Instance["SR_LoadTaskConfig"];
        }

        // 删除所有任务
        private void delAllTask_Click(object sender, RoutedEventArgs e)
        {
            foreach (var taskf in taskFlag)
            {
                if (taskf.Value)
                {
                    MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_StopAllTasksFirst"], LanguageManager.Instance["Warning"]);
                    return;
                }
            }

            taskFlag.Clear();
            taskCrons.Clear();
            taskCmds.Clear();
            // 清理所有 CTS
            foreach (var cts in taskCtsMap.Values)
            {
                cts.Dispose();
            }
            taskCtsMap.Clear();

            RefreshTaskList();
            loadOrSaveTaskConfig.Content = LanguageManager.Instance["SR_LoadTaskConfig"];
        }

        // 选择任务变更
        private void tasksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tasksList.SelectedIndex == -1)
            {
                TimerTaskSettings.IsEnabled = false;
                timercmdCron.Text = "";
                timercmdCmd.Text = "";
                return;
            }
            TimerTaskSettings.IsEnabled = true;
            int id = GetSelectedTaskId();
            startTimercmd.IsChecked = taskFlag[id];
            timerCmdout.Text = LanguageManager.Instance["SR_NoneText"];
            timercmdCron.Text = taskCrons[id];
            timercmdCmd.Text = taskCmds[id];
        }

        // Cron表达式输入变更
        private void timercmdCron_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || tasksList.SelectedIndex == -1) return;
            string expr = timercmdCron.Text.Trim();
            if (TryParseCron(expr, out var cron))
            {
                var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local);
                string nextRun = next.HasValue
                    ? next.Value.LocalDateTime.ToString("F")
                    : "--";
                cronValidationText.Text = LanguageManager.Instance["SR_CronValid"] + nextRun;
                cronValidationText.Foreground = new SolidColorBrush(Colors.Green);
                taskCrons[GetSelectedTaskId()] = expr;
            }
            else
            {
                cronValidationText.Text = LanguageManager.Instance["SR_CronInvalid"];
                cronValidationText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        // 指令输入变更
        private void timercmdCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
                taskCmds[GetSelectedTaskId()] = timercmdCmd.Text;
        }

        // 快捷模板按钮
        private void CronTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                timercmdCron.Text = btn.Tag.ToString();
        }

        // 启动/停止
        private void startTimercmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tasksList.SelectedIndex == -1)
                {
                    timerCmdout.Text = LanguageManager.Instance["SR_SelectTaskFirst"];
                    startTimercmd.IsChecked = false;
                    return;
                }

                int id = GetSelectedTaskId();

                if (startTimercmd.IsChecked == true)
                {
                    if (!TryParseCron(taskCrons[id], out _))
                    {
                        MagicShow.ShowMsgDialog(this, LanguageManager.Instance["SR_CronInvalidDialog"], LanguageManager.Instance["Error"]);
                        startTimercmd.IsChecked = false;
                        return;
                    }
                    // 如果已有旧的 CTS，先取消并释放（防止重复启动）
                    if (taskCtsMap.TryGetValue(id, out var oldCts))
                    {
                        oldCts.Cancel();
                        oldCts.Dispose();
                    }
                    var cts = new CancellationTokenSource();
                    taskCtsMap[id] = cts;
                    taskFlag[id] = true;
                    Task.Run(() => TimedTasks(id, taskCrons[id], taskCmds[id], cts.Token));
                }
                else
                {
                    // 取消令牌，立即中断 Task.Delay 等待
                    if (taskCtsMap.TryGetValue(id, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                        taskCtsMap.Remove(id);
                    }
                    taskFlag[id] = false;
                }
            }
            catch (Exception ex)
            {
                timerCmdout.Text = LanguageManager.Instance["SR_ExecFailedPrefix"] + ex.Message;
                startTimercmd.IsChecked = false;
            }
        }

        // 核心任务循环（Cron）
        private async Task TimedTasks(int id, string cronExpr, string cmd, CancellationToken token)
        {
            var cron = CronExpression.Parse(cronExpr, CronFormat.IncludeSeconds);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    DateTimeOffset? next = cron.GetNextOccurrence(now, TimeZoneInfo.Local);

                    if (next == null) break;

                    // 等待到下次触发时间，token 取消时立即中断
                    TimeSpan delay = next.Value - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, token);

                    // 二次确认（Delay 结束后再检查一次）
                    if (token.IsCancellationRequested) break;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        OnTimerTaskRun(id, cmd);
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // 正常取消，忽略异常
            }
            finally
            {
                // 确保无论何种退出方式，taskFlag 都同步为 false
                taskFlag[id] = false;
                Dispatcher.Invoke(() =>
                {
                    // 若当前选中的正是此任务，同步按钮状态
                    if (tasksList.SelectedIndex != -1 && GetSelectedTaskId() == id)
                        startTimercmd.IsChecked = false;
                });
            }
        }

        private void OnTimerTaskRun(int id, string cmd)
        {
            try
            {
                if (ServerService.CheckServerRunning())
                {
                    switch (cmd)
                    {
                        case ".backup":
                            if (MoreOperation.IsEnabled)
                            {
                                _ = BackupWorld();
                                PrintLog(LanguageManager.Instance["SR_ScheduledBackupStarting"], Colors.Blue);
                            }
                            break;
                        default:
                            ServerService.SendCommand(cmd);
                            PrintLog(string.Format(LanguageManager.Instance["SR_ScheduledTaskExecCmd"], cmd), Colors.Blue);
                            break;
                    }

                    if (tasksList.SelectedIndex != -1 && GetSelectedTaskId() == id)
                        timerCmdout.Text = LanguageManager.Instance["SR_ExecSuccessTime"] + DateTime.Now.ToString("F");
                }
                else
                {
                    if (tasksList.SelectedIndex != -1 && GetSelectedTaskId() == id)
                        timerCmdout.Text = LanguageManager.Instance["SR_ServerNotOpenTime"] + DateTime.Now.ToString("F");
                }
            }
            catch (Exception ex)
            {
                if (tasksList.SelectedIndex != -1 && GetSelectedTaskId() == id)
                    timerCmdout.Text = string.Format(LanguageManager.Instance["SR_ExecFailedTime"], ex.Message) + DateTime.Now.ToString("F");
            }
        }

        // 加载&保存配置
        private void LoadOrSaveTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            if (loadOrSaveTaskConfig.Content.ToString() == LanguageManager.Instance["SR_LoadTaskConfig"])
            {
                if (ServerService.InstanceConfig.TimerTasks != null)
                {
                    taskFlag.Clear();
                    taskCrons.Clear();
                    taskCmds.Clear();
                    // 清理所有 CTS
                    foreach (var cts in taskCtsMap.Values)
                        cts.Dispose();
                    taskCtsMap.Clear();

                    foreach (var item in ServerService.InstanceConfig.TimerTasks)
                    {
                        int taskId = int.Parse(item.Key);
                        var details = item.Value;

                        taskFlag.Add(taskId, false);

                        if (details.Cron != null)
                        {
                            taskCrons[taskId] = (string)details.Cron;
                        }
                        else
                        {
                            // 兼容旧格式（Interval + Unit），转换为 Cron
                            int interval = (int)details.Interval;
                            int unit = (int)details.Unit;
                            int seconds = unit == 1 ? interval : Math.Max(1, interval / 1000);
                            taskCrons[taskId] = $"*/{seconds} * * * * *";
                        }
                        taskCmds[taskId] = details.Command;
                    }

                    RefreshTaskList();
                }

                Growl.Success(LanguageManager.Instance["SR_LoadSuccess"]);
                if (tasksList.Items.Count != 0)
                    loadOrSaveTaskConfig.Content = LanguageManager.Instance["SR_SaveTaskConfig"];
            }
            else
            {
                var newTasks = new Dictionary<string, ServerConfig.TimerTask>();
                foreach (var id in taskFlag.Keys)
                {
                    newTasks[id.ToString()] = new ServerConfig.TimerTask
                    {
                        Cron = taskCrons[id],
                        Command = taskCmds[id]
                    };
                }
                ServerService.InstanceConfig.TimerTasks = newTasks;

                ServerConfig.Current.Save();
                Growl.Success(LanguageManager.Instance["SR_SaveSuccess"]);
            }
        }

        private void delTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            ServerService.InstanceConfig.TimerTasks.Clear();
            ServerConfig.Current.Save();
            Growl.Success(LanguageManager.Instance["SR_ClearSuccess"]);
        }

        // 私有工具方法
        private int GetSelectedTaskId()
            => int.Parse(tasksList.SelectedItem.ToString());

        private void RefreshTaskList()
            => tasksList.ItemsSource = taskFlag.Keys.ToArray();

        #endregion

        #region window event
        private void Window_Activated(object sender, EventArgs e)
        {
            /*
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                ConptyPopUp.IsOpen = true;
                Growl.SetGrowlParent(ConptyGrowlPanel, true);
                return;
            }
            */
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            /*
            if (ConPTYWindow != null && ConPTYWindow.Visibility == Visibility.Visible)
            {
                ConptyPopUp.IsOpen = false;
                Growl.SetGrowlParent(ConptyGrowlPanel, false);
                return;
            }
            */
            Growl.SetGrowlParent(GrowlPanel, false);
        }

        /*
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
        */

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
            if (ServerService.CheckServerRunning())
            {
                ServerService.SendCommand("save-off");
                await Task.Delay(1000);
                ServerService.SendCommand("save-all");
                ServerService.SendCommand("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"" + LanguageManager.Instance["SR_TellrawBackupInProgress"] + "\",\"color\":\"aqua\"}]");
                Growl.Info(LanguageManager.Instance["SR_BackupStarting"]);
                PrintLog(LanguageManager.Instance["SR_BackupStartingLog"], Colors.Blue);
            }
            try
            {
                Dispatcher.Invoke(() =>
                {
                    MoreOperation.IsEnabled = false;
                });

                // 读取配置，从 InstanceConfig 读取
                var backupConfig = ServerService.InstanceConfig.BackupConfigs;

                // 若服务器是开启状态 执行等待
                if (backupConfig.BackupSaveDelay >= 5 && ServerService.CheckServerRunning())
                {
                    await Task.Delay(backupConfig.BackupSaveDelay * 1000);
                }
                else
                {
                    await Task.Delay(10000);
                }

                string worldPath = ServerProperties.GetConfigValue("level-name");
                if (string.IsNullOrEmpty(worldPath))
                {
                    worldPath = "world";
                }

                string fullWorldPath = Path.Combine(ServerService.ServerBase, worldPath);
                string fullNetherPath = Path.Combine(ServerService.ServerBase, worldPath + "_nether");
                string fullEndPath = Path.Combine(ServerService.ServerBase, worldPath + "_the_end");

                var foldersToCompress = new List<string>();
                if (Directory.Exists(fullWorldPath)) foldersToCompress.Add(fullWorldPath);
                if (Directory.Exists(fullNetherPath)) foldersToCompress.Add(fullNetherPath);
                if (Directory.Exists(fullEndPath)) foldersToCompress.Add(fullEndPath);

                if (foldersToCompress.Count == 0)
                {
                    Growl.Error(LanguageManager.Instance["SR_NoWorldFolder"]);
                    PrintLog(LanguageManager.Instance["SR_NoWorldFolder"], Colors.Red);
                    LogHelper.Write.Error("未找到任何世界存档文件夹（包括主世界、下界、末地），备份失败！");
                    if (ServerService.CheckServerRunning())
                    {
                        ServerService.SendCommand("save-on");
                        ServerService.SendCommand("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"" + LanguageManager.Instance["SR_TellrawBackupNoWorld"] + "\",\"color\":\"red\"}]");
                    }
                    return;
                }

                // 备份目录 - 改为从 BackupConfigs 读取
                string backupDir = Path.Combine(ServerService.ServerBase, "msl-backups"); // 默认路径
                switch (backupConfig.BackupMode)
                {
                    case 1:
                        backupDir = Path.Combine(@"MSL", "server-backups", $"{ServerService.ServerName}_{RserverID}");
                        break;
                    case 2:
                        if (!string.IsNullOrEmpty(backupConfig.BackupCustomPath))
                        {
                            backupDir = backupConfig.BackupCustomPath;
                        }
                        else
                        {
                            PrintLog(LanguageManager.Instance["SR_BackupUsingDefaultPath"], Colors.OrangeRed);
                        }
                        break;
                }

                string backupPath = Path.Combine(backupDir, $"msl-backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // 限制最大备份数量 - 改为从 BackupConfigs 读取
                int maxBackups = backupConfig.BackupMaxLimit >= 0 ? backupConfig.BackupMaxLimit : 20;

                try
                {
                    var backupFiles = Directory.GetFiles(backupDir, "msl-backup_*.zip")
                                               .Select(path => new FileInfo(path))
                                               .OrderBy(fi => fi.Name)
                                               .ToList();

                    if (maxBackups >= 1 && backupFiles.Count >= maxBackups)
                    {
                        int filesToDeleteCount = backupFiles.Count - maxBackups + 1;
                        var filesToDelete = backupFiles.Take(filesToDeleteCount).ToList();

                        foreach (var fileToDelete in filesToDelete)
                        {
                            try
                            {
                                fileToDelete.Delete();
                                PrintLog(string.Format(LanguageManager.Instance["SR_BackupDeletedOld"], fileToDelete.Name), Colors.Blue);
                            }
                            catch (Exception ex)
                            {
                                PrintLog(string.Format(LanguageManager.Instance["SR_BackupDeleteOldFailed"], fileToDelete.Name, ex.Message), Colors.OrangeRed);
                                LogHelper.Write.Warn($"删除旧备份 {fileToDelete.Name} 失败：{ex}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PrintLog(string.Format(LanguageManager.Instance["SR_BackupCleanupFailed"], ex.Message), Colors.OrangeRed);
                    LogHelper.Write.Error("检查并清理旧备份时发生错误：" + ex);
                }

                PrintLog(LanguageManager.Instance["SR_BackupCompressing"], Colors.Blue);
                LogHelper.Write.Info("正在压缩存档文件，请稍等···");
                using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(backupPath)))
                {
                    zipStream.SetLevel(5);
                    foreach (var folderPath in foldersToCompress)
                    {
                        await CompressFolder(ServerService.ServerBase, folderPath, zipStream);
                    }
                }

                if (ServerService.CheckServerRunning())
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
                        tellrawMessage += $"{{\"text\":\"{LanguageManager.Instance["SR_TellrawBackupDoneDetail"]}\",\"color\":\"aqua\"}},";
                        tellrawMessage += $"{{\"text\":\"{LanguageManager.Instance["SR_TellrawFileName"]}\",\"color\":\"gray\"}},";
                        tellrawMessage += $"{{\"text\":\"{fileName}\",\"color\":\"white\"}},";
                        tellrawMessage += $"{{\"text\":\"{LanguageManager.Instance["SR_TellrawFileSize"]}\",\"color\":\"gray\"}},";
                        tellrawMessage += $"{{\"text\":\"{formattedSize}\",\"color\":\"white\"}}";
                        tellrawMessage += "]";
                        ServerService.SendCommand("save-on");
                        ServerService.SendCommand(tellrawMessage);
                    }
                    catch (Exception ex)
                    {
                        PrintLog(string.Format(LanguageManager.Instance["SR_BackupFileInfoFailed"], ex.Message), Colors.OrangeRed);
                        LogHelper.Write.Warn("无法获取备份文件信息：" + ex);
                        ServerService.SendCommand("save-on");
                        ServerService.SendCommand("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"" + LanguageManager.Instance["SR_TellrawBackupDone"] + "\",\"color\":\"aqua\"}]");
                    }
                }

                Growl.Success(string.Format(LanguageManager.Instance["SR_BackupSuccessMsg"], backupPath));
                PrintLog(string.Format(LanguageManager.Instance["SR_BackupSuccessLog"], backupPath), Colors.Blue);
                LogHelper.Write.Info($"存档备份成功！已保存至：{backupPath}");
            }
            catch (Exception ex)
            {
                Growl.Error(LanguageManager.Instance["SR_BackupFailedMsg"] + ex.Message);
                PrintLog(string.Format(LanguageManager.Instance["SR_BackupFailedLog"], ex.Message), Colors.Red);
                LogHelper.Write.Error("备份失败！" + ex);
                if (ServerService.CheckServerRunning())
                {
                    ServerService.SendCommand("save-on");
                    ServerService.SendCommand("tellraw @a [{\"text\":\"[\",\"color\":\"yellow\"},{\"text\":\"MSL\",\"color\":\"green\"},{\"text\":\"]\",\"color\":\"yellow\"},{\"text\":\"" + LanguageManager.Instance["SR_TellrawBackupError"] + "\",\"color\":\"red\"}]");
                }
                return;
            }
            finally
            {
                if (ServerService.CheckServerRunning())
                {
                    ServerService.SendCommand("save-on");
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
                    throw new IOException(string.Format(LanguageManager.Instance["SR_ExclusiveLockError"], entryName, ex.Message), ex);
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
            dialog.Description = LanguageManager.Instance["SR_SelectBackupFolder"];
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
                    backupDir = Path.Combine(ServerService.ServerBase, "msl-backups");
                    break;
                case 1:
                    backupDir = Path.Combine(@"MSL", "server-backups", $"{ServerService.ServerName}_{RserverID}");
                    break;
                case 2:
                    if (!String.IsNullOrEmpty(TextBackupPath.Text))
                    {
                        backupDir = TextBackupPath.Text;
                    }
                    else
                    {
                        Growl.Error(LanguageManager.Instance["SR_CustomBackupPathEmpty"]);
                        return;
                    }
                    break;
                default:
                    backupDir = Path.Combine(ServerService.ServerBase, "msl-backups");
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
                Growl.Error(LanguageManager.Instance["SR_OpenBackupFailed"] + ex.Message);
            }
        }

        #endregion
    }
}