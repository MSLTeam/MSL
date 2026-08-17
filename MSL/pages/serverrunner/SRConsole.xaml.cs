using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using MSL.langs;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static MSL.utils.LogColorizer;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace MSL.pages.serverrunner
{
    /// <summary>
    /// SRConsole.xaml 的交互逻辑
    /// </summary>
    public partial class SRConsole : UserControl
    {
        private readonly ServerRunner _parent;
        private readonly MCServerService _serverService;

        public LogColorizer _logColorizer;
        private int _logEntryCount = 0;
        private const int MaxLogEntries = 1000;
        private bool firstTimeOpenTerminal = true;
        private readonly List<Tuple<string, Color>> _pendingLogs = new List<Tuple<string, Color>>();

        // Fast command list
        private List<FastCommandInfo> CurrentFastCmds = new List<FastCommandInfo>();

        public SRConsole(ServerRunner parent, MCServerService serverService)
        {
            InitializeComponent();
            _parent = parent;
            _serverService = serverService;
            InitializeOutlog();
            cmdtext.Text = Lang.SR_ServerClosed;
        }

        #region 公共访问器

        public void ScrollToEnd()
        {
            if (outlog.IsLoaded && !double.IsInfinity(outlog.ExtentHeight))
                outlog.ScrollToEnd();
        }
        public string OutlogText => outlog.Document.Text;
        public ToggleButton ControlToggleButton => controlServer;
        public ToggleButton DashboardToggleButton => _parent.DashboardControlToggle;
        public System.Windows.Controls.ComboBox MoreOperationCombo => MoreOperation;
        public System.Windows.Controls.ComboBox FastCmdComboBox => fastCMD;

        public void OnPageActivated()
        {
            // 刷出暂存的日志
            if (_pendingLogs.Count > 0)
            {
                var pending = _pendingLogs.ToList();
                _pendingLogs.Clear();
                foreach (var item in pending)
                    WriteLog(item.Item1, item.Item2);
            }

            if (firstTimeOpenTerminal)
            {
                firstTimeOpenTerminal = false;
                outlog.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!double.IsInfinity(outlog.ExtentHeight))
                        outlog.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public void UpdateFastCmds(System.Collections.Generic.List<FastCommandInfo> cmds)
        {
            CurrentFastCmds = cmds;
            fastCMD.ItemsSource = null;
            fastCMD.Items.Clear();
            fastCMD.ItemsSource = CurrentFastCmds;
            fastCMD.DisplayMemberPath = "DisplayText";
            fastCMD.SelectedIndex = 0;
        }

        #endregion

        #region 初始化

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

        #endregion

        #region 服务器启动

        public async void LaunchServerOnLoad()
        {
            while (!_parent.IsLoaded)
            {
                Thread.Sleep(1000);
            }
            await Dispatcher.InvokeAsync(() =>
            {
                LaunchServer();
            });
        }

        public async void LaunchServer()
        {
            LogHelper.Write.Info("开服操作 - 实例ID：" + _parent.RserverID);
            if (await MCEulaEvent() != true)
                return;
            if (_serverService.ServerMode == 0 && !string.IsNullOrEmpty(_serverService.ServerYggAddr))
            {
                // 代表启动的是一个MC服务器
                // 处理外置登录
                if (!await DownloadAuthlib())
                {
                    return; // 下载authlib失败，退出
                }
                LogHelper.Write.Info("成功启用外置登录库，地址：" + _serverService.ServerYggAddr);
            }
            await _serverService.LaunchServer();
            ChangeControlsState();
        }

        private async Task<bool> MCEulaEvent()
        {
            if (_serverService.ServerMode != 0) // 以自定义命令方式启动时，不执行接受eula事件
                return true;
            string path1 = _serverService.ServerBase + "\\eula.txt";
            if (!File.Exists(path1) || (File.Exists(path1) && !File.ReadAllText(path1).Contains("eula=true")))
            {
                var shield = new Shield
                {
                    Command = HandyControl.Interactivity.ControlCommands.OpenLink,
                    CommandParameter = "https://aka.ms/MinecraftEULA",
                    Subject = "https://aka.ms/MinecraftEULA",
                    Status = LanguageManager.Instance["OpenWebsite"]
                };
                bool dialog = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_MCEulaPrompt"], LanguageManager.Instance["Tip"], true, LanguageManager.Instance["No"], LanguageManager.Instance["Yes"], shield);
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
                if (!File.Exists(Path.Combine(_serverService.ServerBase, "authlib-injector.jar")) ||
                    !Functions.VerifyFileSHA256(Path.Combine(_serverService.ServerBase, "authlib-injector.jar"), authlib_jobj["checksums"]["sha256"].ToString()))
                {
                    //下载或更新authlib-injector.jar
                    bool download_suc = await MagicShow.ShowDownloader(_parent,
                        authlib_jobj["download_url"].ToString().Replace("authlib-injector.yushi.moe", "authlib-injector.mirrors.mslmc.cn"),
                        _serverService.ServerBase, "authlib-injector.jar", LanguageManager.Instance["SR_AuthlibUpdating"], authlib_jobj["checksums"]["sha256"].ToString());
                    if (!download_suc)
                    {
                        Growl.Error(LanguageManager.Instance["SR_DownloadFailed"]);
                        return false;
                    }
                }
            }
            else
            {
                if (File.Exists(Path.Combine(_serverService.ServerBase, "authlib-injector.jar")))
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

        #endregion

        #region 控件状态

        public void ChangeControlsState(bool isEnable = true)
        {
            if (isEnable)
            {
                if (!ServerList.RunningServers.Contains(_parent.RserverID))
                {
                    ServerList.RunningServers.Add(_parent.RserverID);
                }
                _parent.NotifyServerStateChange();
                _parent.GetServerInfoLine = 0;
                _parent.ServerPlayerList.Items.Clear();
                _parent.ServerStateText = LanguageManager.Instance["SR_Running"];
                _parent.ServerStateLab.Foreground = Brushes.Red;
                _parent.SolveProblemBtn.IsEnabled = false;
                controlServer.IsChecked = true;
                _parent.DashboardControlToggle.IsChecked = true;
                MoreOperation.IsEnabled = false; //服务器完成启动前禁止备份
                _parent.GameDifficultyText = LanguageManager.Instance["SR_Fetching"];
                _parent.GameTypeText = LanguageManager.Instance["SR_Fetching"];
                _parent.ServerIPText = LanguageManager.Instance["SR_Fetching"];
                _parent.LocalIPText = LanguageManager.Instance["SR_Fetching"];
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
                if (ServerList.RunningServers.Contains(_parent.RserverID))
                {
                    ServerList.RunningServers.Remove(_parent.RserverID);
                }
                _parent.NotifyServerStateChange();

                _parent.ServerStateText = LanguageManager.Instance["SR_Closed"];
                _parent.ServerStateLab.Foreground = Brushes.Green;
                _parent.SolveProblemBtn.IsEnabled = true;
                controlServer.IsChecked = false;
                _parent.DashboardControlToggle.IsChecked = false;
                MoreOperation.IsEnabled = true; // 服务器关闭后允许备份
                MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_ServerClosedMsg"]);
                sendcmd.IsEnabled = false;
                cmdtext.IsEnabled = false;
                fastCMD.IsEnabled = false;
                cmdtext.Text = LanguageManager.Instance["SR_ServerClosed"];
            }
        }

        #endregion

        #region 日志显示功能、日志清空功能

        public void PrintLog(string msg, Color defaultColor)
        {
            var segments = MCServerLogHelper.ParseLogSegments(msg, defaultColor);
            if (segments.Count == 0) return;

            Dispatcher.Invoke(() =>
            {
                // 控件不可见时（页面已切走）暂存，避免 AvalonEdit 内部 Infinity 绑定错误
                if (!outlog.IsLoaded || outlog.ActualHeight <= 0 || double.IsInfinity(outlog.ActualHeight))
                {
                    _pendingLogs.Add(Tuple.Create(msg, defaultColor));
                    return;
                }
                WriteLog(msg, defaultColor);
            });
        }

        private void WriteLog(string msg, Color defaultColor)
        {
            var segments = MCServerLogHelper.ParseLogSegments(msg, defaultColor);
            if (segments.Count == 0) return;

            // 自动清屏
            if (_parent.AutoClearOutlogToggle?.IsChecked == true && _logEntryCount >= MaxLogEntries)
            {
                ClearLog();
            }

            bool shouldScroll = !double.IsInfinity(outlog.ExtentHeight) && !double.IsNaN(outlog.ExtentHeight)
                && outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight - 48;
            string plainText = string.Concat(segments.Select(s => s.Text));
            int insertOffset = outlog.Document.TextLength;

            if (insertOffset > 0)
            {
                outlog.Document.Insert(insertOffset, "\n");
                insertOffset++;
            }

            var entry = new LogEntry
            {
                StartOffset = insertOffset,
                Segments = segments
            };

            outlog.Document.Insert(insertOffset, plainText);
            _logColorizer.AddEntry(entry);
            outlog.TextArea.TextView.Redraw();

            _logEntryCount++;

            if (shouldScroll)
                outlog.ScrollToEnd();
        }

        public void ClearLog()
        {
            outlog.Clear();
            _logColorizer.Clear();
            _logEntryCount = 0;
        }

        #endregion

        #region 服务器事件

        public void ServerStartedEvent()
        {
            MagicFlowMsg.ShowMessage(string.Format(LanguageManager.Instance["SR_ServerLaunchedSuccess"], _serverService.ServerName), 1);
            _parent.ServerStateText = LanguageManager.Instance["SR_Launched"];
            _parent.GetServerInfoSys();
            MoreOperation.IsEnabled = true;
        }

        public void ServerExitEvent(int exitCode)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                ChangeControlsState(false);
                if (_serverService.ProblemSolveSystem)
                {
                    _serverService.ProblemSolveSystem = false;
                    if (string.IsNullOrEmpty(_serverService.ProblemFound))
                    {
                        MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_ServerClosedForAnalysis"], LanguageManager.Instance["SR_CrashAnalysisSystem"]);
                    }
                    else
                    {
                        Growl.Info(LanguageManager.Instance["SR_ServerClosedShowingReport"]);
                        MagicShow.ShowMsgDialog(_parent, _serverService.ProblemFound + "\n" + LanguageManager.Instance["SR_ProblemFoundPS"], LanguageManager.Instance["SR_ServerAnalysisReport"]);
                        _serverService.ProblemFound = string.Empty;
                    }
                }
                else if (exitCode != 0 && _parent.GetServerInfoLine <= 100)
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_AbnormalClose"], LanguageManager.Instance["Tip"], true);
                    if (dialogRet)
                    {
                        _parent.NavigateToConsole();
                        _serverService.ProblemSolveSystem = true;
                        LaunchServer();
                    }
                }
                else if (_parent.AutoStartServerToggle.IsChecked == true)
                {
                    Console.WriteLine(LanguageManager.Instance["SR_ServerClosedRestartEvent"]);
                    await Task.Delay(200);
                    RestartServer();
                }
            });
        }

        public void RestartServer()
        {
            MagicFlowMsg.ShowAskMessage(
                message: LanguageManager.Instance["SR_RestartConfirmMsg"],
                callback: confirmed =>
                {
                    if (confirmed)
                    {
                        if (_serverService != null)
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
                container: _parent.GrowlPanel
            );
        }

        #endregion

        #region 编码切换

        public void HandleEncodingChange()
        {
            string encoding = "UTF8";
            if (_parent.OutputCmdEncodingButton.Content.ToString().Contains("UTF8"))
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
                        _serverService.InstanceConfig.EncodingOut = encoding;
                        ServerConfig.Current.Save();
                        Dispatcher.InvokeAsync(() =>
                        {
                            _parent.OutputCmdEncodingButton.Content = encoding;
                            Growl.Success(LanguageManager.Instance["SR_ChangeDone"]);
                        });
                        Task.Run(async () =>
                        {
                            _parent.GetServerInfoLine = 102;
                            await Task.Delay(100);
                            _serverService.KillServer();
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

        #endregion

        #region 命令发送

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
                        // 检查下拉框是否选择"/"，且输入的第一个词是否含有别名（忽略大小写）
                        var aliasMatch = CurrentFastCmds.FirstOrDefault(c =>
                            !string.IsNullOrEmpty(c.Alias) &&
                            c.Alias.Equals(firstWord, StringComparison.OrdinalIgnoreCase));

                        if (aliasMatch != null)
                        {
                            string args = parts.Length > 1 ? parts[1] : string.Empty;
                            // 清理"/"
                            finalCmd = $"{aliasMatch.Cmd.Trim().TrimStart('/')} {args}".Trim();
                        }
                    }
                    else if (fastCMD.SelectedIndex > 0 && fastCMD.SelectedItem is FastCommandInfo selectedCmd)
                    {
                        // 下拉框不选择"/"时，不触发别名（下拉框选择了某个快捷指令）
                        finalCmd = $"{selectedCmd.Cmd.Trim().TrimStart('/')} {inputText}".Trim();
                    }
                }

                // 发送命令
                _serverService.SendCommand(finalCmd);
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
            if (_serverService.ServerTerm != null && e.Key == Key.Tab)
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

        #endregion

        #region 自动补全

        private async Task TriggerCompletion()
        {
            if (_serverService.ServerTerm == null || !_serverService.ServerTerm.IsRunning) return;

            completionList.Items.Clear();
            completionList.Items.Add(LanguageManager.Instance["SR_FetchingCompletion"]);
            completionPopup.IsOpen = true;

            var candidates = await _serverService.ServerTerm.RequestCompletionAsync(cmdtext.Text);

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

        #endregion

        #region 控制按钮

        private void controlServer_Click(object sender, RoutedEventArgs e)
        {
            var _sender = sender as ToggleButton;
            if (_sender.IsChecked == true)
            {
                controlServer.IsChecked = false;
                _parent.DashboardControlToggle.IsChecked = false;
                if (_parent.GetServerInfoLine == 102)
                {
                    _parent.GetServerInfoLine = 101;
                    return;
                }
                LaunchServer();
            }
            else
            {
                controlServer.IsChecked = true;
                _parent.DashboardControlToggle.IsChecked = true;
                if (_serverService.ServerTerm != null)
                {
                    _serverService.ServerTerm.Stop();
                }
                else
                {
                    MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_Stopping"]);
                    _serverService.StopServer();
                }

                _parent.GetServerInfoLine = 101;
            }
        }

        private async void controlServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var _sender = sender as ToggleButton;
                if (_sender.IsChecked == true)
                {
                    _parent.GetServerInfoLine = 102;
                    if (_serverService.ServerTerm != null)
                    {
                        _serverService.ServerTerm.Kill();
                    }
                    else
                    {
                        _serverService.ServerProcess.Kill();
                    }
                }
            }
            catch { }
            await Task.Delay(500);
            _parent.GetServerInfoLine = 101;
        }

        private async void MoreOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MoreOperation.SelectedIndex)
            {
                case 1:
                    MoreOperation.IsEnabled = false;
                    await _parent.BackupWorld();
                    MoreOperation.IsEnabled = true;
                    break;
                case 2:
                    _parent.OpenAILogAnalyseDialog();
                    break;
            }
            MoreOperation.SelectedIndex = 0;
        }

        #endregion

        #region 右键菜单

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

        #region 快捷指令

        public void GetFastCmd()
        {
            CurrentFastCmds.Clear();
            CurrentFastCmds.Add(new FastCommandInfo { Cmd = "/", Remark = LanguageManager.Instance["SR_CmdRemark"] });

            var config = _serverService.InstanceConfig;
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
            _parent.FastCmdList.ItemsSource = null;
            _parent.FastCmdList.Items.Clear();
            _parent.FastCmdList.ItemsSource = CurrentFastCmds;
            _parent.FastCmdList.DisplayMemberPath = "DisplayText";
        }

        public void SetFastCmd()
        {
            // Utils类 --> Config类
            _serverService.InstanceConfig.FastCmds = CurrentFastCmds.Skip(1)
                .Select(c => new ServerConfig.FastCommandInfo
                {
                    Cmd = c.Cmd,
                    Remark = c.Remark,
                    Alias = c.Alias
                }).ToList();
            ServerConfig.Current.Save();
            GetFastCmd();
        }

        #endregion
    }
}
