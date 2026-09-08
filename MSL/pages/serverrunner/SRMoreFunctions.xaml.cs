using HandyControl.Controls;
using MSL.controls.dialogs;
using MSL.langs;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace MSL.pages.serverrunner
{
    /// <summary>
    /// SRMoreFunctions.xaml 的交互逻辑
    /// </summary>
    public partial class SRMoreFunctions : UserControl
    {
        private readonly ServerRunner _parent;
        private readonly MCServerService _serverService;
        private List<FastCommandInfo> CurrentFastCmds = new List<FastCommandInfo>();

        public SRMoreFunctions(ServerRunner parent, MCServerService serverService)
        {
            InitializeComponent();
            _parent = parent;
            _serverService = serverService;
        }

        // Public methods for parent to call
        public void LoadFastCommands() => GetFastCmd();

        // Public accessors for parent bridge properties
        public System.Windows.Controls.Primitives.ToggleButton AutoStartServerToggleButton => autoStartserver;
        public System.Windows.Controls.Button OutputCmdEncodingButton => outputCmdEncoding;
        public System.Windows.Controls.Primitives.ToggleButton AutoClearOutlogToggleButton => autoClearOutlog;
        public bool IsSystemInfoMonitoringEnabled => systemInfoBtn.IsChecked == true;

        /// <summary>
        /// 由 ServerRunner.Window_Loaded 调用，根据配置设置开关状态
        /// </summary>
        public void SetSystemInfoToggle(bool isChecked)
        {
            systemInfoBtn.IsChecked = isChecked;
        }

        public void LoadConfig(AppConfig config)
        {
            autoStartserver.IsChecked = _serverService.InstanceConfig.AutoStartServer;
            showOutlog.IsChecked = _serverService.InstanceConfig.ShowOutlog;
            formatOutHead.IsChecked = _serverService.InstanceConfig.FormatLogPrefix;
            shieldStackOut.IsChecked = _serverService.InstanceConfig.ShieldStackOut;
            inputCmdEncoding.Content = _serverService.InstanceConfig.EncodingIn;
            outputCmdEncoding.Content = _serverService.InstanceConfig.EncodingOut;
            fileforceUTF8encoding.IsChecked = _serverService.InstanceConfig.FileForceUTF8;

            if (_serverService.InstanceConfig.ShieldLogs != null && _serverService.InstanceConfig.ShieldLogs.Count > 0)
            {
                shieldLogBtn.IsChecked = true;
                LogShield_Add.IsEnabled = false;
                LogShield_Del.IsEnabled = false;
            }
            if (_serverService.InstanceConfig.HighLightLogs != null && _serverService.InstanceConfig.HighLightLogs.Count > 0)
            {
                highLightLogBtn.IsChecked = true;
                LogHighLight_Add.IsEnabled = false;
                LogHighLight_Del.IsEnabled = false;
            }

            if (_serverService.InstanceConfig.UseConpty)
            {
                ServerEncodingSettings.Visibility = Visibility.Collapsed;
                useConpty.IsChecked = true;
            }
        }

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
                    MagicShow.ShowMsgDialog(_parent, string.Format(LanguageManager.Instance["SR_IPv6Success"], ipv6), LanguageManager.Instance["SR_IPv6SuccessTitle"]);
                }
                else
                {
                    throw new Exception(response?.HttpResponseContent.ToString());
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_IPv6Failed"] + ex.Message, LanguageManager.Instance["SR_IPv6FailedTitle"]);
            }
            finally
            {
                GetIPV6.IsEnabled = true;
            }
        }

        private void autostartServer_Click(object sender, RoutedEventArgs e)
        {
            _serverService.InstanceConfig.AutoStartServer = autoStartserver.IsChecked == true;
            ServerConfig.Current.Save();
        }

        private void inputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (inputCmdEncoding.Content.ToString() == "ANSI")
            {
                _serverService.InstanceConfig.EncodingIn = "UTF8";
                inputCmdEncoding.Content = "UTF8";
            }
            else if (inputCmdEncoding.Content.ToString() == "UTF8")
            {
                _serverService.InstanceConfig.EncodingIn = "ANSI";
                inputCmdEncoding.Content = "ANSI";
            }
            ServerConfig.Current.Save();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_EncodingChanged"], 1);
        }

        private void outputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (outputCmdEncoding.Content.ToString() == "ANSI")
            {
                _serverService.InstanceConfig.EncodingOut = "UTF8";
                outputCmdEncoding.Content = "UTF8";
            }
            else if (outputCmdEncoding.Content.ToString() == "UTF8")
            {
                _serverService.InstanceConfig.EncodingOut = "ANSI";
                outputCmdEncoding.Content = "ANSI";
            }
            ServerConfig.Current.Save();
            try
            {
                if (_serverService.CheckServerRunning())
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
            _serverService.InstanceConfig.FileForceUTF8 = fileforceUTF8encoding.IsChecked == true;
            ServerConfig.Current.Save();
            MagicFlowMsg.ShowMessage(LanguageManager.Instance["SR_SettingRestartRequired"], 1);
        }

        private void KillProcessTreeTogBtn_Click(object sender, RoutedEventArgs e)
        {
            _serverService.InstanceConfig.KillProcessTree = KillProcessTreeTogBtn.IsChecked == true;
            ServerConfig.Current.Save();
        }

        private void useConpty_Click(object sender, RoutedEventArgs e)
        {
            if (_serverService.CheckServerRunning())
            {
                MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_CloseServerFirst"], LanguageManager.Instance["Tip"]);
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
                _serverService.InstanceConfig.UseConpty = false;
            }
            else
            {
                ServerEncodingSettings.Visibility = Visibility.Collapsed;
                _serverService.InstanceConfig.UseConpty = true;
            }
            ServerConfig.Current.Save();
        }

        private async void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_serverService.CheckServerRunning())
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_ServerRunningCloseConfirm"], LanguageManager.Instance["Tip"]);
                    if (!dialogRet)
                    {
                        return;
                    }
                    _serverService.ServerProcess.StandardInput.WriteLine("stop");
                }
                try
                {
                    string path1 = _serverService.ServerBase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = _serverService.ServerBase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_ModifyDone"], LanguageManager.Instance["Tip"]);
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
                    string path1 = _serverService.ServerBase + @"\server.properties";
                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string line;
                    line = sr.ReadToEnd();
                    line = line.Replace("online-mode=true", "online-mode=false");
                    string path = _serverService.ServerBase + @"\server.properties";
                    StreamWriter streamWriter = new StreamWriter(path);
                    streamWriter.WriteLine(line);
                    streamWriter.Flush();
                    streamWriter.Close();
                    MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_ModifyDone"], LanguageManager.Instance["Tip"]);
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
                _serverService.ServerLogHandler.IsShowOutLog = true;
                _serverService.InstanceConfig.ShowOutlog = true;
            }
            else
            {
                _serverService.ServerLogHandler.IsShowOutLog = false;
                _serverService.InstanceConfig.ShowOutlog = false;
            }
            ServerConfig.Current.Save();
        }

        private void formatOutHead_Click(object sender, RoutedEventArgs e)
        {
            if (formatOutHead.IsChecked == true)
            {
                _serverService.ServerLogHandler.IsFormatLogPrefix = true;
                _serverService.InstanceConfig.FormatLogPrefix = true;
            }
            else
            {
                _serverService.ServerLogHandler.IsFormatLogPrefix = false;
                _serverService.InstanceConfig.FormatLogPrefix = false;
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

                    _serverService.ServerLogHandler.ShieldLog = [.. tempList];
                    _serverService.InstanceConfig.ShieldLogs = tempList;
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
                _serverService.ServerLogHandler.ShieldLog = null;
                _serverService.InstanceConfig.ShieldLogs.Clear();
                LogShield_Add.IsEnabled = true;
                LogShield_Del.IsEnabled = true;
            }
            ServerConfig.Current.Save();
        }

        private async void LogShield_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(_parent, LanguageManager.Instance["SR_ShieldLogInput"]);
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

                    _serverService.ServerLogHandler.HighLightLog = [.. tempList];
                    _serverService.InstanceConfig.HighLightLogs = tempList;

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
                _serverService.ServerLogHandler.HighLightLog = null;
                _serverService.InstanceConfig.HighLightLogs.Clear();
                LogHighLight_Add.IsEnabled = true;
                LogHighLight_Del.IsEnabled = true;
            }
            ServerConfig.Current.Save();
        }

        private async void LogHighLight_Add_Click(object sender, RoutedEventArgs e)
        {
            string text = await MagicShow.ShowInput(_parent, LanguageManager.Instance["SR_HighlightInput"]);
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
                _serverService.ServerLogHandler.IsShieldStackOut = false;
                _serverService.InstanceConfig.ShieldStackOut = false;
            }
            else
            {
                _serverService.ServerLogHandler.IsShieldStackOut = true;
                _serverService.InstanceConfig.ShieldStackOut = true;
            }
            ServerConfig.Current.Save();
        }

        private async void autoClearOutlog_Click(object sender, RoutedEventArgs e)
        {
            if (autoClearOutlog.IsChecked == false)
            {
                bool msgreturn = await MagicShow.ShowMsgDialogAsync(_parent,
                    LanguageManager.Instance["SR_AutoClearWarning"],
                    LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                if (msgreturn)
                {
                    _serverService.InstanceConfig.AutoClearOutlog = false;
                }
                else
                {
                    autoClearOutlog.IsChecked = true;
                }
            }
            else
            {
                _serverService.InstanceConfig.AutoClearOutlog = true;
            }
            ServerConfig.Current.Save();
        }

        private void logsAnalyse_Click(object sender, RoutedEventArgs e)
        {
            OpenAILogAnalyseDialog();
        }

        public void OpenAILogAnalyseDialog()
        {
            LogAnalysisDialog logAnalysisDialog = new LogAnalysisDialog(_parent, _serverService.ServerBase, _serverService.ServerCore);
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
            if (File.Exists(_serverService.ServerBase + "\\logs\\latest.log"))
            {
                FileStream fileStream = new FileStream(_serverService.ServerBase + "\\logs\\latest.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
            strings[1] = _parent.OutlogText;

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
                    if ((await MagicShow.ShowMsgDialogAsync(_parent, string.Format(LanguageManager.Instance["SR_LogUploadBigFail"], response.HttpResponseCode + " " + response.HttpResponseContent), LanguageManager.Instance["Error"], true) == true))
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
                if (File.Exists(Path.Combine(_serverService.ServerBase, "msl-installForge.log")))
                {
                    logsContent = "[MSL端处理日志]\n" + File.ReadAllText(Path.Combine(_serverService.ServerBase, "msl-installForge.log"));
                }
                if (File.Exists(Path.Combine(_serverService.ServerBase, "msl-compileForge.log")))
                {
                    logsContent = logsContent + "\n[Java端编译日志]\n" + File.ReadAllText(Path.Combine(_serverService.ServerBase, "msl-compileForge.log"));
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

            _parent.UpdateFastCmdComboBox(CurrentFastCmds);
            fastCmdList.ItemsSource = null;
            fastCmdList.Items.Clear();
            fastCmdList.ItemsSource = CurrentFastCmds;
            fastCmdList.DisplayMemberPath = "DisplayText";
        }

        private void SetFastCmd()
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

        private void refrushFastCmd_Click(object sender, RoutedEventArgs e)
        {
            GetFastCmd();
        }

        private void resetFastCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_serverService.InstanceConfig.FastCmds == null || _serverService.InstanceConfig.FastCmds.Count == 0)
            {
                return;
            }
            else
            {
                _serverService.InstanceConfig.FastCmds = null;
                ServerConfig.Current.Save();
                MagicShow.ShowMsgDialog(_parent, LanguageManager.Instance["SR_RestartWindowHint"], LanguageManager.Instance["Tip"]);
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

                await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_AddFastCmd"], LanguageManager.Instance["SR_Input"], uIElement: uniformStack);

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

        private async void systemInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (systemInfoBtn.IsChecked == true)
            {
                // 全局开关关闭时，不允许单个窗体开启占用显示
                if (!ConfigStore.GetServerInfo)
                {
                    systemInfoBtn.IsChecked = false;
                    await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_GlobalOccupancyDisabled"], LanguageManager.Instance["Tip"]);
                    return;
                }
                _serverService.InstanceConfig.ShowOccupancy = true;
                ServerConfig.Current.Save();
                _parent.StartSystemInfoMonitoring();
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(_parent, LanguageManager.Instance["SR_DisableOutputPreview"], LanguageManager.Instance["Tip"]);
                _serverService.InstanceConfig.ShowOccupancy = false;
                ServerConfig.Current.Save();
                _parent.SetPreviewOutlogText(LanguageManager.Instance["SR_PreviewClosed"]);
                _parent.StopSystemInfoMonitoring();
            }
        }

        private void playerInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playerInfoBtn.IsChecked == true)
            {
                _serverService.recordPlayInfo = true;
                Growl.Success(LanguageManager.Instance["SR_EnabledLower"]);
            }
            else
            {
                _serverService.recordPlayInfo = false;
                Growl.Success(LanguageManager.Instance["SR_DisabledLower"]);
            }
        }
    }
}
