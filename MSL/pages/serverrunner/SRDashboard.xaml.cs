using HandyControl.Controls;
using MSL.langs;
using MSL.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MSL.pages.serverrunner
{
    /// <summary>
    /// SRDashboard.xaml 的交互逻辑
    /// </summary>
    public partial class SRDashboard : UserControl, INotifyPropertyChanged
    {
        private readonly ServerRunner _parent;
        private readonly MCServerService _serverService;

        #region MVVM 辅助
        public event PropertyChangedEventHandler PropertyChanged;

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

        #region MVVM 绑定属性 - 服务器信息面板
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

        public SRDashboard(ServerRunner parent, MCServerService serverService)
        {
            InitializeComponent();
            _parent = parent;
            _serverService = serverService;
            DataContext = this;

            // 初始化 MVVM 绑定属性默认值
            ServerStateText = Lang.SR_Closed;
            OnlineModeText = Lang.SR_Fetching;
            GameTypeText = Lang.SR_Fetching;
            GameDifficultyText = Lang.SR_Fetching;
            ServerIPText = Lang.SR_Fetching;
            LocalIPText = Lang.SR_Fetching;

            previewOutlog.Text = Lang.SR_PreviewHint;
        }

        #region 仪表盘事件处理

        private async void solveProblemBtn_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_CrashAnalysisInfo"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                _serverService.ProblemSolveSystem = true;
                _parent.LaunchServer();
            }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            Growl.Info(LanguageManager.Instance["SR_OpeningServerDir"]);
            Process.Start(_serverService.ServerBase);
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
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_ConfirmKick"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                if (!_serverService.SendCommand("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("["))))
                    Growl.Error(LanguageManager.Instance["SR_OperationFailed"]);
            }
        }

        private async void banPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_ConfirmBan"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
            if (dialogRet)
            {
                if (!_serverService.SendCommand("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("["))))
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
                        MagicShow.ShowMsgDialog(_parent, string.Format(LanguageManager.Instance["SR_PublicIPInfo"], localIP.ToString(), port), LanguageManager.Instance["Tip"]);
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(_parent, string.Format(LanguageManager.Instance["SR_PublicIPInfoNoPort"], localIP.ToString()), LanguageManager.Instance["Tip"]);
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

                        MagicShow.ShowMsgDialog(_parent,
                            string.Format(LanguageManager.Instance["SR_RadminDetected"], ipStr, portSuffix),
                            LanguageManager.Instance["SR_RadminTitle"]);
                        return;
                    }
                }
            }

            await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_NetworkMappingTip"], LanguageManager.Instance["Tip"], false);
        }

        private void controlServer1_Click(object sender, RoutedEventArgs e)
        {
            _parent.ToggleServerFromDashboard(controlServer1.IsChecked == true);
            controlServer1.IsChecked = !controlServer1.IsChecked;
        }

        private async void controlServer1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _parent.KillServerFromDashboard();
        }

        #endregion

        #region 系统信息监控

        /// <summary>
        /// 由 MoreFunctions 页面的 systemInfoBtn_Click 调用，或 Window_Loaded 自动启动。
        /// 订阅全局 SystemMonitor 单例，不再为每个窗体创建独立监控线程。
        /// </summary>
        public void StartSystemInfoMonitoring()
        {
            SystemMonitor.Instance.Subscribe(this, OnSystemInfoData);
        }

        /// <summary>
        /// 由 MoreFunctions 页面的 systemInfoBtn_Click 调用
        /// </summary>
        public async void StopSystemInfoMonitoring()
        {
            await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_DisableOutputPreview"], LanguageManager.Instance["Tip"]);
            previewOutlog.Text = LanguageManager.Instance["SR_PreviewClosed"];
            SystemMonitor.Instance.Unsubscribe(this);
        }

        /// <summary>
        /// 窗口关闭时静默取消订阅，不弹对话框。由 ServerRunner.DisposeRes 调用。
        /// </summary>
        public void CleanupSystemMonitoring()
        {
            SystemMonitor.Instance.Unsubscribe(this);
        }

        /// <summary>
        /// SystemMonitor 回调 — 在后台线程触发，需 Dispatcher 更新 UI。
        /// 系统级数据由 SystemMonitor 统一采集，进程级内存由本窗体自行获取。
        /// </summary>
        private void OnSystemInfoData(SystemMonitor.SystemInfoData data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!cpuInfoBar.IsLoaded) return;

                double processMem = _serverService.GetProcessMemoryGB();

                if ((int)data.CpuUsage <= 100)
                {
                    cpuInfoLab.Content = $"CPU: {data.CpuUsage:f2}%";
                    cpuInfoBar.Value = (int)data.CpuUsage;
                }

                double allMemory = data.TotalMemoryGB;
                double usedMemory = allMemory - (data.RamAvailableMB / 1024.0);

                memoryInfoLab.Content = string.Format(LanguageManager.Instance["SR_MemInfoFormat"], $"{allMemory:f2}", $"{usedMemory:f2}", $"{data.RamAvailableMB / 1024f:f2}");
                double usedPct = usedMemory / allMemory;
                memoryInfoBar.Value = usedPct * 100;
                processMemoryInfoBar.Value = (processMem / allMemory) * 100;
                usedMemoryLab.Content = string.Format(LanguageManager.Instance["SR_SysMemUsedFormat"], $"{usedPct:P}");
                processMemoryInfoLab.Content = string.Format(LanguageManager.Instance["SR_ProcMemUsedFormat"], $"{processMem:f2}", $"{(processMem / allMemory):P}");

                UpdateLogPreview();
            });
        }

        private void UpdateLogPreview()
        {
            if (previewOutlog.LineCount < 25)
            {
                if (_serverService == null)
                    return;
                if (!string.IsNullOrEmpty(_serverService._tempLog) && !previewOutlog.Text.Contains(_serverService._tempLog))
                {
                    previewOutlog.Text += "\n" + _serverService._tempLog;
                    previewOutlog.ScrollToEnd();
                }
            }
            else
            {
                previewOutlog.Clear();
            }
        }

        /// <summary>
        /// 由 MoreFunctions 页面的 playerInfoBtn_Click 调用
        /// </summary>
        public void SetPlayerInfoRecording(bool enabled)
        {
            _serverService.recordPlayInfo = enabled;
            if (enabled)
                Growl.Success(LanguageManager.Instance["SR_EnabledLower"]);
            else
                Growl.Success(LanguageManager.Instance["SR_DisabledLower"]);
        }

        #endregion

        #region 公共方法 - 供父窗口调用

        // 公共控件访问器 — 供 ServerRunner 桥接
        public System.Windows.Controls.ListBox PlayerListBox => serverPlayerList;
        public System.Windows.Documents.Run ServerStateLabel => serverStateLab;
        public System.Windows.Controls.Button SolveProblemButton => solveProblemBtn;
        public System.Windows.Controls.Primitives.ToggleButton ControlToggleButton => controlServer1;
        public short GetServerInfoLine { get => getServerInfoLine; set => getServerInfoLine = value; }
        private short getServerInfoLine = 0;

        public void SetPreviewOutlogText(string text) => previewOutlog.Text = text;
        public void AddPlayer(string playerName)
        {
            if (!serverPlayerList.Items.Contains(playerName))
                serverPlayerList.Items.Add(playerName);
        }
        public void RemovePlayer(string playerName)
        {
            foreach (string x in serverPlayerList.Items)
            {
                if (x.StartsWith(playerName + "[/"))
                {
                    serverPlayerList.Items.Remove(x);
                    break;
                }
            }
        }

        public void UpdateServerState(bool isRunning)
        {
            if (isRunning)
            {
                ServerStateText = LanguageManager.Instance["SR_Running"];
                serverStateLab.Foreground = Brushes.Red;
                solveProblemBtn.IsEnabled = false;
                controlServer1.IsChecked = true;

                GameDifficultyText = LanguageManager.Instance["SR_Fetching"];
                GameTypeText = LanguageManager.Instance["SR_Fetching"];
                ServerIPText = LanguageManager.Instance["SR_Fetching"];
                LocalIPText = LanguageManager.Instance["SR_Fetching"];
            }
            else
            {
                ServerStateText = LanguageManager.Instance["SR_Closed"];
                serverStateLab.Foreground = Brushes.Green;
                solveProblemBtn.IsEnabled = true;
                controlServer1.IsChecked = false;
            }
        }

        public void UpdatePlayerList(Action<ListBox> updateAction)
        {
            Dispatcher.InvokeAsync(() => updateAction(serverPlayerList));
        }

        public void ClearPlayerList()
        {
            Dispatcher.InvokeAsync(() => serverPlayerList.Items.Clear());
        }

        public void GetServerInfoSys()
        {
            try
            {
                Encoding encoding = Functions.GetTextFileEncodingType(_serverService.ServerBase + @"\server.properties");
                string config = File.ReadAllText(_serverService.ServerBase + @"\server.properties", encoding);
                if (config.Contains("\r"))
                {
                    config = config.Replace("\r", string.Empty);
                }
                int om1 = config.IndexOf("online-mode=") + 12;
                string om2 = config.Substring(om1);
                string onlineMode = om2.Substring(0, om2.IndexOf("\n"));
                if (onlineMode == "true")
                {
                    if (string.IsNullOrEmpty(_serverService.ServerYggAddr))
                    {
                        _parent.PrintLog(LanguageManager.Instance["SR_OnlineModeWarn"], Colors.OrangeRed);
                    }
                    else
                    {
                        _parent.PrintLog(LanguageManager.Instance["SR_OnlineModeThirdParty"], Colors.OrangeRed);
                    }
                    OnlineModeText = LanguageManager.Instance["SR_EnabledLower"];
                }
                else if (onlineMode == "false")
                {
                    if (string.IsNullOrEmpty(_serverService.ServerYggAddr))
                    {
                        _parent.PrintLog(LanguageManager.Instance["SR_OnlineModeClosedWarn"], Colors.OrangeRed);
                    }
                    else
                    {
                        _parent.PrintLog(LanguageManager.Instance["SR_OnlineModeThirdPartyClosed"], Colors.Red);
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
                string serverIP = GetServerPropertyValue(config, "server-ip");
                string serverPort = GetServerPropertyValue(config, "server-port");

                // server-ip 为空是 Minecraft 的默认配置；部分配置会写成单独的 0。
                if (string.IsNullOrWhiteSpace(serverIP) || serverIP == "0")
                {
                    serverIP = "127.0.0.1";
                }

                // 仪表盘的“服务器IP”栏展示服务器端口；本地进入地址单独展示实际连接地址。
                ServerIPText = string.IsNullOrWhiteSpace(serverPort) ? "25565" : serverPort;
                LocalIPText = serverPort == "25565"
                    ? serverIP
                    : FormatServerEndpoint(serverIP, serverPort);
            }
            catch
            {
                Growl.Info(LanguageManager.Instance["SR_ServerInfoErr"]);
            }
        }

        private static string GetServerPropertyValue(string config, string propertyName)
        {
            string prefix = propertyName + "=";
            foreach (string line in config.Split('\n'))
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedLine.Substring(prefix.Length).Trim();
                }
            }

            return string.Empty;
        }

        private static string FormatServerEndpoint(string serverIP, string serverPort)
        {
            if (string.IsNullOrWhiteSpace(serverIP)) return serverPort?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(serverPort)) return serverIP.Trim();
            return $"{serverIP.Trim()}:{serverPort.Trim()}";
        }

        public void HandlePlayerListAdd(string playerName)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (!serverPlayerList.Items.Contains(playerName))
                {
                    serverPlayerList.Items.Add(playerName);
                }
            });
        }

        public void HandlePlayerListRemove(string playerName)
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

        /// <summary>
        /// 同步 Dashboard 上 controlServer1 的勾选状态（由父窗口调用）
        /// </summary>
        public void SyncToggleButton(bool isChecked)
        {
            controlServer1.IsChecked = isChecked;
        }

        #endregion
    }
}
