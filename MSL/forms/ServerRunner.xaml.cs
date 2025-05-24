using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.forms;
using MSL.langs;
using MSL.pages;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
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
        public static event DeleControl SaveConfigEvent;
        public static event DeleControl ServerStateChange;
        private short GetServerInfoLine = 0;
        private readonly short FirstStartTab;
        private MCSLogHandler MCSLogHandler { get; set; }
        private Process ServerProcess { get; } = new Process();
        private ConptyWindow ConPTYWindow { get; set; }
        private DispatcherTimer ConPTYResizeTimer { get; set; }= new DispatcherTimer();
        private int RserverID { get; }
        private string Rservername { get; set; }
        private string Rserverjava { get; set; }
        private string Rserverserver { get; set; }
        private string RserverJVM { get; set; }
        private string RserverJVMcmd { get; set; }
        private string Rserverbase { get; set; }
        private short Rservermode { get; set; }

        /// <summary>
        /// 服务器运行窗口
        /// </summary>
        /// <param name="serverID">服务器ID</param>
        /// <param name="controlTab">Tab标签</param>
        public ServerRunner(int serverID, short controlTab = 0)
        {
            InitializeComponent();
            InitializeLogHandler();
            InitializeColorDict();

            ServerProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            ServerProcess.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            ServerProcess.Exited += new EventHandler(ServerExitEvent);

            ConPTYResizeTimer.Interval = TimeSpan.FromMilliseconds(200);
            ConPTYResizeTimer.Tick += new EventHandler(ConPTYResizeTimer_Tick);
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
            RserverID = serverID;
            FirstStartTab = controlTab;
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
                    int dialog = MagicShow.ShowMsg(this, "检测到您没有关闭服务器，是否隐藏此窗口？\n如要重新显示此窗口，请在服务器列表内双击该服务器（或点击开启服务器按钮）", "警告", true, "取消");
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
            outlog.Document.Blocks.Clear();
            if (ServerList.ServerWindowList.ContainsKey(RserverID))
            {
                ServerList.ServerWindowList.Remove(RserverID);
            }
            ServerProcess.OutputDataReceived -= OutputDataReceived;
            ServerProcess.ErrorDataReceived -= OutputDataReceived;
            ServerProcess.Exited -= ServerExitEvent;
            SettingsPage.ChangeSkinStyle -= ChangeSkinStyle;
            ServerProcess.Dispose();
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
            if (File.Exists(@"MSL\config.json"))
            {
                JObject keys = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (keys["mslTips"] != null && (bool)keys["mslTips"] == false)
                {
                    MCSLogHandler.IsMSLFormatedLog = false;
                }
                if (keys["sidemenuExpanded"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("sidemenuExpanded", true);
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    Tab_Home.Width = double.NaN;
                    Tab_Console.Width = double.NaN;
                    Tab_Plugins.Width = double.NaN;
                    Tab_Settings.Width = double.NaN;
                    Tab_MoreFunctions.Width = double.NaN;
                    Tab_Timer.Width = double.NaN;
                }
                else if ((bool)keys["sidemenuExpanded"] == true)
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
                    ImageBrush imageBrush = new ImageBrush(SettingsPage.GetImage("MSL\\Background.png"));
                    imageBrush.Stretch = Stretch.UniformToFill;
                    Background = imageBrush;
                }
                else
                {
                    SetResourceReference(BackgroundProperty, "BackgroundBrush");
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
                    GetServerConfig();
                    break;
                default:
                    break;
            }
        }

        private bool CheckServerRunning()
        {
            if (ConPTYWindow != null)
            {
                if (ConPTYWindow.ConptyConsole.ConPTYTerm.TermProcIsRunning)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            try
            {
                if (!ServerProcess.HasExited)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
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
            try
            {
                var cpuCounter = new PerformanceCounter();
                if (PerformanceCounterCategory.Exists("Processor Information") && PerformanceCounterCategory.CounterExists("% Processor Utility", "Processor Information"))
                {
                    cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                }
                else
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                var phisicalMemory = GetPhisicalMemory();
                while (getSystemInfo)
                {
                    float cpuUsage = cpuCounter.NextValue();
                    float ramAvailable = ramCounter.NextValue() / 1024;
                    double allMemory = phisicalMemory / 1024.0 / 1024.0 / 1024.0;

                    Dispatcher.Invoke(() =>
                    {
                        if ((int)cpuUsage <= 100)
                        {
                            cpuInfoLab.Content = $"CPU: {cpuUsage:f2}%";
                            cpuInfoBar.Value = (int)cpuUsage;
                        }
                        UpdateMemoryInfo(ramAvailable, allMemory);
                        UpdateLogPreview();
                    });
                    Thread.Sleep(3000);
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Growl.Error("无法获取系统占用信息！显示占用功能已自动关闭！\n通常此问题是因为系统原因造成的，不影响软件正常使用！");
                    previewOutlog.Text = "预览功能已关闭，请前往服务器控制台界面查看日志信息！";
                });
                getSystemInfo = false;
            }
        }

        private static long GetPhisicalMemory()
        {
            long amemory = 0;
            //获得物理内存 
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc.Cast<ManagementObject>())
            {
                if (mo["TotalPhysicalMemory"] != null)
                {
                    amemory = long.Parse(mo["TotalPhysicalMemory"].ToString());
                }
            }
            return amemory;
        }

        private void UpdateMemoryInfo(float ramAvailable, double allMemory)
        {
            memoryInfoLab.Content = $"总内存: {allMemory:f2}G\n已使用: {allMemory - ramAvailable:f2}G\n可使用: {ramAvailable:f2}G";
            double usedMemoryPercentage = (allMemory - ramAvailable) / allMemory;
            memoryInfoBar.Value = usedMemoryPercentage * 100;
            availableMemoryInfoBar.Value = (ramAvailable / allMemory) * 100;
            usedMemoryLab.Content = $"系统已用内存: {usedMemoryPercentage:P}";
            availableMemoryInfoLab.Content = $"系统空闲内存: {(ramAvailable / allMemory):P}";
        }

        private string tempLog;
        private void UpdateLogPreview()
        {
            if(previewOutlog.LineCount < 25)
            {
                if (!string.IsNullOrEmpty(tempLog) && !previewOutlog.Text.Contains(tempLog))
                {
                    previewOutlog.Text += tempLog + "\n";
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
            try
            {
                if (await MCEulaEvent() != true)
                    return;
                string fileforceUTF8Jvm = "";
                if (Rservermode == 0)
                {
                    if (fileforceUTF8encoding.IsChecked == true && !RserverJVMcmd.Contains("-Dfile.encoding=UTF-8"))
                    {
                        fileforceUTF8Jvm = "-Dfile.encoding=UTF-8 ";
                    }

                    if (Rserverserver.StartsWith("@libraries/"))
                    {
                        StartServer(RserverJVM + " " + fileforceUTF8Jvm + RserverJVMcmd + " " + Rserverserver + " nogui");
                    }
                    else
                    {
                        StartServer(RserverJVM + " " + fileforceUTF8Jvm + RserverJVMcmd + " -jar \"" + Rserverserver + "\" nogui");
                    }
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
                    ConPTYWindow.java = Rserverjava;
                    ConPTYWindow.launcharg = StartFileArg;
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
                PrintLog("出现错误，正在检查问题...", Brushes.Red);
                if (File.Exists(Rserverjava))
                {
                    PrintLog("Java路径正常", Brushes.Green);
                }
                else
                {
                    PrintLog("Java路径有误", Brushes.Red);
                }
                if (Directory.Exists(Rserverbase))
                {
                    PrintLog("服务器目录正常", Brushes.Green);
                }
                else
                {
                    PrintLog("服务器目录有误", Brushes.Red);
                }
                if (File.Exists(Rserverbase + "\\" + Rserverserver))
                {
                    PrintLog("服务端路径正常", Brushes.Green);
                }
                else
                {
                    PrintLog("服务端路径有误", Brushes.Red);
                }

                PrintLog("错误代码：" + e.Message, Brushes.Red);
                MagicShow.ShowMsgDialog(this, "出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！", "错误");
                TabCtrl.SelectedIndex = 1;
                //ChangeControlsState(false);
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
                //controlServer.Content = "关服";
                //controlServer_Copy.Content = "关服";
                controlServer.IsChecked = true;
                controlServer1.IsChecked = true;
                gameDifficultyLab.Content = "获取中";
                gameTypeLab.Content = "获取中";
                serverIPLab.Content = "获取中";
                localServerIPLab.Content = "获取中";
                MagicFlowMsg.ShowMessage("开服中，请稍等……", _growlPanel: GetActiveGrowlPanel());
                outlog.Document.Blocks.Clear();
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
                //controlServer.Content = "开服";
                //controlServer_Copy.Content = "开服";
                controlServer.IsChecked = false;
                controlServer1.IsChecked = false;
                MagicFlowMsg.ShowMessage("服务器已关闭！", _growlPanel: GetActiveGrowlPanel());
                if (ConPTYWindow == null)
                {
                    sendcmd.IsEnabled = false;
                    cmdtext.IsEnabled = false;
                    fastCMD.IsEnabled = false;
                    cmdtext.Text = "服务器已关闭";
                    MCSLogHandler.CleanupResources();
                    try
                    {
                        ServerProcess.CancelOutputRead();
                        ServerProcess.CancelErrorRead();
                    }
                    catch
                    { return; }
                }
                else
                {
                    ConPTYWindow.ServerStatus.Text = "已关服";
                }
            }
        }

        // private bool solveProblemSystem;
        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                //string msg = e.Data;
                //tempLog = msg;

                // 将日志添加到缓冲区，不要直接处理（否则UI线程压力很大，可能会使软件崩溃）
                MCSLogHandler._logBuffer.Enqueue(e.Data);

                Dispatcher.InvokeAsync(() =>
                {
                    // 检查是否需要清理日志
                    if (outlog.Document.Blocks.Count >= 1000 && autoClearOutlog.IsChecked == true)
                    {
                        outlog.Document.Blocks.Clear();
                    }
                    // 如果定时器没有运行，确保启动它
                    if (!MCSLogHandler._logProcessTimer.IsEnabled)
                    {
                        MCSLogHandler._logProcessTimer.Start();
                    }
                });
            }
        }

        

        #region 日志显示功能、彩色日志

        private void PrintLog(string msg, Brush color)
        {
            MCSLogHandler.LogInfo[0].Color = (SolidColorBrush)color;
            Paragraph p = new Paragraph();
            try
            {
                if (msg.Contains("&"))
                {
                    string[] splitMsg = msg.Split('&');
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }
                        string colorCode = everyMsg.Substring(0, 1);
                        string text = everyMsg.Substring(1);
                        Run run = new Run(text)
                        {
                            Foreground = GetBrushFromMinecraftColorCode(colorCode[0])
                        };
                        p.Inlines.Add(run);
                    }
                }
                else if (msg.Contains("§"))
                {
                    string[] splitMsg = msg.Split('§');
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }
                        string colorCode = everyMsg.Substring(0, 1);
                        string text = everyMsg.Substring(1);
                        Run run = new Run(text)
                        {
                            Foreground = GetBrushFromMinecraftColorCode(colorCode[0])
                        };
                        p.Inlines.Add(run);
                    }
                }
                else if (msg.Contains("\x1B"))
                {
                    string[] splitMsg = msg.Split(new[] { '\x1B' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }

                        // 提取ANSI码和文本内容
                        int mIndex = everyMsg.IndexOf('m');
                        if (mIndex == -1)
                        {
                            continue;
                        }

                        string[] codes = everyMsg.Substring(0, mIndex).Split(';');
                        string text = everyMsg.Substring(mIndex + 1);

                        // 默认的文字装饰
                        bool isBold = false;
                        bool isUnderline = false;

                        Brush foreground = Brushes.Green; // 默认颜色

                        foreach (var code in codes)
                        {
                            switch (code)
                            {
                                case "0": // 重置
                                    isBold = false;
                                    isUnderline = false;
                                    foreground = Brushes.Green;
                                    break;
                                case "1": // 加粗
                                    isBold = true;
                                    break;
                                case "4": // 下划线
                                    isUnderline = true;
                                    break;
                                default:
                                    if (colorDictAnsi.ContainsKey(code))
                                    {
                                        foreground = colorDictAnsi[code];
                                    }
                                    break;
                            }
                        }

                        Run run = new Run(text)
                        {
                            Foreground = foreground,
                            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                            TextDecorations = isUnderline ? TextDecorations.Underline : null
                        };

                        p.Inlines.Add(run);
                    }
                }
                else
                {
                    Run run = new Run(msg)
                    {
                        Foreground = color
                    };
                    p.Inlines.Add(run);
                }
            }
            catch
            {
                Run run = new Run(msg)
                {
                    Foreground = color
                };
                p.Inlines.Add(run);
            }
            finally
            {
                tempLog = msg;
                Dispatcher.Invoke(() =>
                {
                    outlog.Document.Blocks.Add(p);
                    if (outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight)
                    {
                        outlog.ScrollToEnd();
                    }
                });
            }
        }

        private Dictionary<char, SolidColorBrush> colorDict;
        private void InitializeColorDict()
        {
            colorDict = new Dictionary<char, SolidColorBrush>
            {
                ['r'] = MCSLogHandler.LogInfo[0].Color,
                ['0'] = Brushes.Black,
                ['1'] = Brushes.DarkBlue,
                ['2'] = Brushes.DarkGreen,
                ['3'] = Brushes.DarkCyan,
                ['4'] = Brushes.DarkRed,
                ['5'] = Brushes.DarkMagenta,
                ['6'] = Brushes.Orange,//gold
                ['7'] = Brushes.Gray,
                ['8'] = Brushes.DarkGray,
                ['9'] = Brushes.Blue,
                ['a'] = Brushes.Green,
                ['b'] = Brushes.Cyan,
                ['c'] = Brushes.Red,
                ['d'] = Brushes.Magenta,
                ['e'] = Brushes.Gold,//yellow
                ['f'] = Brushes.White,
            };
        }

        private Brush GetBrushFromMinecraftColorCode(char colorCode)
        {
            if (colorDict.ContainsKey(colorCode))
            {
                return colorDict[colorCode];
            }
            return Brushes.Green;
        }

        Dictionary<string, Brush> colorDictAnsi = new Dictionary<string, Brush>
        {
            ["30"] = Brushes.Black,
            ["31"] = Brushes.Red,
            ["32"] = Brushes.Green,
            ["33"] = Brushes.Gold,//yellow
            ["34"] = Brushes.Blue,
            ["35"] = Brushes.Magenta,
            ["36"] = Brushes.Cyan,
            ["37"] = Brushes.White,
            ["90"] = Brushes.Gray,
            ["91"] = Brushes.LightPink,
            ["92"] = Brushes.LightGreen,
            ["93"] = Brushes.LightYellow,
            ["94"] = Brushes.LightBlue,
            ["95"] = Brushes.LightPink,
            ["96"] = Brushes.LightCyan,
            ["97"] = Brushes.White,
        };

        private Brush GetBrushFromAnsiColorCode(string colorCode)
        {
            if (colorDictAnsi.ContainsKey(colorCode))
            {
                return colorDictAnsi[colorCode];
            }
            return Brushes.Green;
        }
        #endregion

        private bool ConptyCanOutLog = true;
        //private string tempLogs;
        private void ProcessOutputEvent(string msg)
        {
            if (!ConptyCanOutLog)
                return;

            tempLog = msg;
            ProcessOutput(msg);

            /*
            if (tempLogs != null)
            {
                Dispatcher.Invoke(() =>
                {
                    ProcessOutput(tempLogs);
                });
            }
            tempLogs = msg;
            */
        }

        private void ProcessOutput(string msg)
        {
            //Paragraph p = new Paragraph();
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
                    /*
                    Run run = new Run(s)
                    {
                        Foreground = Brushes.Black
                    };
                    p.Inlines.Add(run);
                    */
                }
            }
            else
            {
                LogHandleInfo(msg);
                if (MCSLogHandler.ServerService.ProblemSolveSystem)
                    MCSLogHandler.ServerService.ProblemSystemHandle(msg);
                /*
                Run run = new Run(msg)
                {
                    Foreground = Brushes.Black
                };
                p.Inlines.Add(run);
                */
            }
            //outlog.Document.Blocks.Clear();
            //outlog.Document.Blocks.Add(p);
        }

        private bool outlogEncodingAsk = true;
        private void HandleEncodingIssue()
        {
            Brush brush = MCSLogHandler.LogInfo[0].Color;
            PrintLog("MSL检测到您的服务器输出了乱码日志，请尝试去“更多功能”界面更改服务器的“输出编码”来解决此问题！", Brushes.Red);
            MCSLogHandler.LogInfo[0].Color = (SolidColorBrush)brush;
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
                    Message = "MSL检测到您的服务器输出了乱码日志，是否将服务器输出编码更改为“" + encoding + "”？\n点击确定后将自动更改编码并重启服务器",
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
                                Dispatcher.Invoke(() =>
                                {
                                    autoStartserver.IsChecked = true;
                                });
                                await Task.Delay(200);
                                try
                                {
                                    ServerProcess.Kill();
                                }
                                catch { }
                                await Task.Delay(200);
                                Dispatcher.Invoke(() =>
                                {
                                    autoStartserver.IsChecked = false;
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
                PrintLog("警告：由于端口占用，服务器已自动关闭！请检查您的服务器是否多开或者有其他软件占用端口！\r\n解决方法：您可尝试通过重启电脑解决！", Brushes.Red);
            }
            else if (msg.Contains("Unable to access jarfile"))
            {
                PrintLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
            }
            else if (msg.Contains("加载 Java 代理时出错"))
            {
                PrintLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
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
                    PrintLog("检测到您没有关闭正版验证，如果客户端为离线登录的话，请点击“更多功能”里“关闭正版验证”按钮以关闭正版验证。否则离线账户将无法进入服务器！", Brushes.OrangeRed);
                    onlineModeLab.Content = "已开启";
                }
                else if (onlineMode == "false")
                {
                    PrintLog("检测到您关闭了正版验证，若没有采取相关措施来保护服务器（如添加登录插件等），服务器会有被入侵的风险，请务必注意！", Brushes.OrangeRed);
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

        // private string foundProblems = string.Empty;

        

        private void ServerExitEvent(object sender, EventArgs e)//Tradition_ServerExitEvent
        {
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
                else if (ServerProcess.ExitCode != 0 && GetServerInfoLine <= 100)
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(this, "服务器疑似异常关闭，是您人为关闭的吗？\n您可使用MSL的崩溃分析系统进行检测，也可将服务器日志发送给他人以寻求帮助！\nPS:请不要截图此弹窗！！！\n日志发送方式：\n1.直接截图控制台内容；2.服务器目录\\logs\\latest.log；3.前往“更多功能”界面上传至Internet\n\n点击确定开始进行崩溃分析", "提示", true);
                    if (dialogRet)
                    {
                        TabCtrl.SelectedIndex = 1;
                        MCSLogHandler.ServerService.ProblemSolveSystem = true;
                        LaunchServer();
                    }
                }
                else if (autoStartserver.IsChecked == true)
                {
                    await Task.Delay(200);
                    LaunchServer();
                }
            });
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
                if (inputCmdEncoding.Content.ToString() == "UTF8")
                {
                    if (fastCMD.SelectedIndex == 0)
                    {
                        SendCmdUTF8(cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/op"))
                    {
                        SendCmdUTF8("op " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/deop"))
                    {
                        SendCmdUTF8("deop " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/ban"))
                    {
                        SendCmdUTF8("ban " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/say"))
                    {
                        SendCmdUTF8("say " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/pardon"))
                    {
                        SendCmdUTF8("pardon " + cmdtext.Text);
                    }
                    else
                    {
                        if (fastCMD.Items[fastCMD.SelectedIndex].ToString().StartsWith("/"))
                        {
                            SendCmdUTF8(fastCMD.Items[fastCMD.SelectedIndex].ToString().Substring(1) + cmdtext.Text);
                        }
                        else
                        {
                            SendCmdUTF8(fastCMD.Items[fastCMD.SelectedIndex].ToString() + cmdtext.Text);
                        }
                    }
                }
                else
                {
                    if (fastCMD.SelectedIndex == 0)
                    {
                        SendCmdANSL(cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/op"))
                    {
                        SendCmdANSL("op " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/deop"))
                    {
                        SendCmdANSL("deop " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/ban"))
                    {
                        SendCmdANSL("ban " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/say"))
                    {
                        SendCmdANSL("say " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/pardon"))
                    {
                        SendCmdANSL("pardon " + cmdtext.Text);
                    }
                    else
                    {
                        if (fastCMD.Items[fastCMD.SelectedIndex].ToString().StartsWith("/"))
                        {
                            SendCmdANSL(fastCMD.Items[fastCMD.SelectedIndex].ToString().Substring(1) + cmdtext.Text);
                        }
                        else
                        {
                            SendCmdANSL(fastCMD.Items[fastCMD.SelectedIndex].ToString() + cmdtext.Text);
                        }
                    }
                }
            }
            catch
            {
                fastCMD.SelectedIndex = 0;
                if (inputCmdEncoding.Content.ToString() == "UTF8")
                {
                    SendCmdUTF8(cmdtext.Text);
                }
                else
                {
                    SendCmdANSL(cmdtext.Text);
                }
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

        #endregion

        #region 服务器功能调整

        /////////这里是服务器功能调整

        private void refreahServerConfig_Click(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
            Growl.Success("刷新成功！");
        }

        private void GetServerConfig()
        {
            try
            {
                string[] strings = ServerBaseConfig();
                onlineModeText.Text = strings[0];
                gameModeText.Text = strings[1];
                gameDifficultyText.Text = strings[2];
                gamePlayerText.Text = strings[3];
                gamePortText.Text = strings[4];
                commandBlockText.Text = strings[5];
                viewDistanceText.Text = strings[6];
                gamePvpText.Text = strings[7];
                gameWorldText.Text = strings[8];
                changeServerPropertiesLab.Text = "更改服务器配置信息";
                changeServerProperties.Visibility = Visibility.Visible;
                changeServerProperties_Add.Visibility = Visibility.Visible;
                changeServerProperties_Add_Add.Visibility = Visibility.Visible;
            }
            catch { changeServerPropertiesLab.Text = "找不到配置文件，无法更改相关设置（请尝试开启一次服务器）"; changeServerProperties.Visibility = Visibility.Collapsed; changeServerProperties_Add.Visibility = Visibility.Collapsed; changeServerProperties_Add_Add.Visibility = Visibility.Collapsed; }
        }

        private string[] ServerBaseConfig()
        {
            string[] strings = new string[9];
            Encoding encoding = Functions.GetTextFileEncodingType(Rserverbase + @"\server.properties");
            string config = File.ReadAllText(Rserverbase + @"\server.properties", encoding);
            if (config.Contains("\r")) // 去除win系统专用换行符
            {
                config = config.Replace("\r", string.Empty);
            }
            int om1 = config.IndexOf("online-mode=") + 12;
            string om2 = config.Substring(om1);
            strings[0] = om2.Substring(0, om2.IndexOf("\n"));
            string[] strings1 = config.Split('\n');
            foreach (string s in strings1)
            {
                if (s.StartsWith("gamemode="))
                {
                    strings[1] = s.Substring(9);
                    break;
                }
            }
            int dc1 = config.IndexOf("difficulty=") + 11;
            string dc2 = config.Substring(dc1);
            strings[2] = dc2.Substring(0, dc2.IndexOf("\n"));
            int mp1 = config.IndexOf("max-players=") + 12;
            string mp2 = config.Substring(mp1);
            strings[3] = mp2.Substring(0, mp2.IndexOf("\n"));
            int sp1 = config.IndexOf("server-port=") + 12;
            string sp2 = config.Substring(sp1);
            strings[4] = sp2.Substring(0, sp2.IndexOf("\n"));
            int ec1 = config.IndexOf("enable-command-block=") + 21;
            string ec2 = config.Substring(ec1);
            strings[5] = ec2.Substring(0, ec2.IndexOf("\n"));
            int vd1 = config.IndexOf("view-distance=") + 14;
            string vd2 = config.Substring(vd1);
            strings[6] = vd2.Substring(0, vd2.IndexOf("\n"));
            int pp1 = config.IndexOf("pvp=") + 4;
            string pp2 = config.Substring(pp1);
            strings[7] = pp2.Substring(0, pp2.IndexOf("\n"));
            int gw1 = config.IndexOf("level-name=") + 11;
            string gw2 = config.Substring(gw1);
            strings[8] = gw2.Substring(0, gw2.IndexOf("\n"));
            return strings;
        }

        private void saveServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "您没有关闭服务器，无法调整服务器功能！", "错误");
                    return;
                }
            }
            catch
            { }
            try
            {
                string[] strings = ServerBaseConfig();
                Encoding encoding = Functions.GetTextFileEncodingType(Rserverbase + @"\server.properties");
                string config = File.ReadAllText(Rserverbase + @"\server.properties", encoding);
                config = config.Replace("online-mode=" + strings[0], "online-mode=" + onlineModeText.Text);
                config = config.Replace("gamemode=" + strings[1], "gamemode=" + gameModeText.Text);
                config = config.Replace("difficulty=" + strings[2], "difficulty=" + gameDifficultyText.Text);
                config = config.Replace("max-players=" + strings[3], "max-players=" + gamePlayerText.Text);
                config = config.Replace("server-port=" + strings[4], "server-port=" + gamePortText.Text);
                config = config.Replace("enable-command-block=" + strings[5], "enable-command-block=" + commandBlockText.Text);
                config = config.Replace("view-distance=" + strings[6], "view-distance=" + viewDistanceText.Text);
                config = config.Replace("pvp=" + strings[7], "pvp=" + gamePvpText.Text);
                config = config.Replace("level-name=" + strings[8], "level-name=" + gameWorldText.Text);
                try
                {
                    if (encoding == Encoding.UTF8)
                    {
                        File.WriteAllText(Rserverbase + @"\server.properties", config, new UTF8Encoding(false));
                    }
                    else if (encoding == Encoding.Default)
                    {
                        File.WriteAllText(Rserverbase + @"\server.properties", config, Encoding.Default);
                    }
                    MagicShow.ShowMsgDialog(this, "保存成功！", "信息");
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(this, "保存失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                }
                finally
                {
                    config = string.Empty;
                    GetServerConfig();
                }
            }
            catch { }
        }

        private async void changeServerIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "您没有关闭服务器，无法更换图标！", "错误");
                    return;
                }
            }
            catch
            { }
            if (File.Exists(Rserverbase + "\\server-icon.png"))
            {
                bool dialogret = await MagicShow.ShowMsgDialogAsync(this, "检测到服务器已设置有图标，是否删除该图标？", "警告", true, "取消");
                if (dialogret)
                {
                    try
                    {
                        File.Delete(Rserverbase + "\\server-icon.png");
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(this, "图标删除失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                        return;
                    }
                    bool _dialogret = await MagicShow.ShowMsgDialogAsync(this, "原图标已删除，是否继续操作？", "提示", true, "取消");
                    if (!_dialogret)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            await MagicShow.ShowMsgDialogAsync(this, "请先准备一张64*64像素的图片（格式为png），准备完成后点击确定以继续", "如何操作？");
            OpenFileDialog openfile = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Title = "请选择文件",
                Filter = "PNG图像|*.png"
            };
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    File.Copy(openfile.FileName, Rserverbase + "\\server-icon.png", true);
                    MagicShow.ShowMsgDialog(this, "图标更换完成！", "信息");
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(this, "图标更换失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                }
            }
        }

        private async void changeWorldMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(this, "您没有关闭服务器，无法更换地图！", "错误");
                    return;
                }
            }
            catch
            { }
            string levelName = "world";
            if (!string.IsNullOrEmpty(gameWorldText.Text))
            {
                levelName = gameWorldText.Text;
            }
            if (Directory.Exists(Rserverbase + @"\" + levelName))
            {
                if (await MagicShow.ShowMsgDialogAsync(this, "点击确定后，MSL将删除原先主世界地图（删除后，地图将从电脑上彻底消失，如有必要请提前备份！）\n点击取消以中止操作", "警告", true, "取消"))
                {
                    MagicDialog dialog = new MagicDialog();
                    dialog.ShowTextDialog(this, "删除中，请稍候");
                    await Task.Run(() =>
                    {
                        DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName);
                        di.Delete(true);
                    });
                    dialog.CloseTextDialog();
                }
                else
                {
                    return;
                }
                if (Directory.Exists(Rserverbase + @"\" + levelName + "_nether"))
                {
                    if (await MagicShow.ShowMsgDialogAsync(this, "MSL同时检测到了下界地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                    {
                        MagicDialog dialog = new MagicDialog();
                        dialog.ShowTextDialog(this, "删除中，请稍候");
                        await Task.Run(() =>
                        {
                            DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_nether");
                            di.Delete(true);
                        });
                        dialog.CloseTextDialog();
                    }
                }
                if (Directory.Exists(Rserverbase + @"\" + levelName + "_the_end"))
                {
                    if (await MagicShow.ShowMsgDialogAsync(this, "MSL同时检测到了末地地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                    {
                        MagicDialog dialog = new MagicDialog();
                        dialog.ShowTextDialog(this, "删除中，请稍候");
                        await Task.Run(() =>
                        {
                            DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_the_end");
                            di.Delete(true);
                        });
                        dialog.CloseTextDialog();
                    }
                }
                if (await MagicShow.ShowMsgDialogAsync(this, "相关地图已经成功删除！是否选择新存档进行导入？（如果不导入而直接开服，服务器将会重新创建一个新世界）", "提示", true, "取消"))
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "请选择地图文件夹(或解压后的文件夹)"
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            MagicDialog _dialog = new MagicDialog();
                            _dialog.ShowTextDialog(this, "导入中，请稍候");
                            await Task.Run(() =>
                            {
                                Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                            });
                            _dialog.CloseTextDialog();
                            MagicShow.ShowMsgDialog(this, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(this, "导入世界失败！\n错误代码：" + ex.Message, "错误");
                        }
                    }
                }
            }
            else
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "请选择地图文件夹(或解压后的文件夹)"
                };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        MagicDialog _dialog = new MagicDialog();
                        _dialog.ShowTextDialog(this, "导入中，请稍候");
                        await Task.Run(() =>
                        {
                            Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                        });
                        _dialog.CloseTextDialog();
                        MagicShow.ShowMsgDialog(this, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(this, "导入世界失败！\n错误代码：" + ex.Message, "错误");
                    }
                }
            }
        }

        private void setServerconfig_Click(object sender, RoutedEventArgs e)
        {
            Window window = new SetServerconfig(Rserverbase)
            {
                Owner = this
            };
            window.ShowDialog();
            GetServerConfig();
        }
        #endregion

        #region 插件mod管理

        ///////////这里是插件mod管理

        void ReFreshPluginsAndMods()
        {
            bool hideList = true;
            if (Directory.Exists(Rserverbase + @"\plugins"))
            {
                List<SR_PluginInfo> list = new List<SR_PluginInfo>();
                DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\plugins");
                FileInfo[] file = directoryInfo.GetFiles("*.*");
                foreach (FileInfo f in file)
                {
                    if (f.Name.EndsWith(".disabled"))
                    {
                        list.Add(new SR_PluginInfo("[已禁用]" + f.Name));
                    }
                    else if (f.Name.EndsWith(".jar"))
                    {
                        list.Add(new SR_PluginInfo(f.Name));
                    }
                }
                pluginslist.ItemsSource = list;
                pluginsTabItem.IsEnabled = true;
                hideList = false;
            }
            else
            {
                pluginsTabItem.IsEnabled = false;
            }
            if (Directory.Exists(Rserverbase + @"\mods"))
            {
                List<SR_ModInfo> list = new List<SR_ModInfo>();
                DirectoryInfo directoryInfo1 = new DirectoryInfo(Rserverbase + @"\mods");
                FileInfo[] file1 = directoryInfo1.GetFiles("*.*");
                foreach (FileInfo f1 in file1)
                {
                    if (f1.Name.EndsWith(".disabled"))
                    {
                        list.Add(new SR_ModInfo("[已禁用]" + f1.Name));
                    }
                    else if (f1.Name.EndsWith(".jar"))
                    {
                        list.Add(new SR_ModInfo(f1.Name));
                    }
                }
                modslist.ItemsSource = list;
                modsTabItem.IsEnabled = true;
                hideList = false;
                if (pluginsTabItem.IsEnabled == false)
                {
                    pluginsAndModsTab.SelectedIndex = 1;
                }
            }
            else
            {
                modsTabItem.IsEnabled = false;
                pluginsAndModsTab.SelectedIndex = 0;
            }
            if (hideList)
            {
                NoPluginModTip.Visibility = Visibility.Visible;
                pluginsAndModsTab.Visibility = Visibility.Collapsed;
            }
            else if (NoPluginModTip.Visibility == Visibility.Visible)
            {
                NoPluginModTip.Visibility = Visibility.Collapsed;
                pluginsAndModsTab.Visibility = Visibility.Visible;
            }
        }

        private void mpHelp_Click(object sender, RoutedEventArgs e)
        {
            MagicShow.ShowMsgDialog(this, "若标签栏为灰色且无法点击，说明此服务端不支持相应的（插件或模组）功能，或相关（插件或模组）文件夹未创建。请更换服务端核心并重启服务器再试。", "提示");
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
            }
            catch { }
            try
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    ListViewItem item = ServerList.FindAncestor<ListViewItem>(btn);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
                SR_PluginInfo SR_PluginInfo = pluginslist.SelectedItem as SR_PluginInfo;
                if (SR_PluginInfo.PluginName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName, Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName + ".disabled", true);
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName);
                    ReFreshPluginsAndMods();
                }
                else
                {
                    File.Copy(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 5), Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 13), true);
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 5));
                    ReFreshPluginsAndMods();
                }
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
            }
            catch { }
            try
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    ListViewItem item = ServerList.FindAncestor<ListViewItem>(btn);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }

                SR_PluginInfo SR_PluginInfo = pluginslist.SelectedItem as SR_PluginInfo;
                if (SR_PluginInfo.PluginName.ToString().Contains("[已禁用]"))
                {
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 5));
                }
                else
                {
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName);
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
            }
            catch { }
            try
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    ListViewItem item = ServerList.FindAncestor<ListViewItem>(btn);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
                SR_ModInfo SR_ModInfo = modslist.SelectedItem as SR_ModInfo;
                if (SR_ModInfo.ModName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\mods\" + SR_ModInfo.ModName, Rserverbase + @"\mods\" + SR_ModInfo.ModName + ".disabled", true);
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName);
                    ReFreshPluginsAndMods();
                }
                else
                {
                    File.Copy(Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 5), Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 13), true);
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 5));
                    ReFreshPluginsAndMods();
                }
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
            }
            catch { }
            try
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    ListViewItem item = ServerList.FindAncestor<ListViewItem>(btn);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
                SR_ModInfo SR_ModInfo = modslist.SelectedItem as SR_ModInfo;
                if (SR_ModInfo.ModName.ToString().Contains("[已禁用]"))
                {
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 5));
                }
                else
                {
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName);
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
            foreach (var x in pluginslist.Items)
            {
                SR_PluginInfo SR_PluginInfo = x as SR_PluginInfo;
                if (SR_PluginInfo.PluginName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName, Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName + ".disabled", true);
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName);
                }
                else
                {
                    File.Copy(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 5), Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 13), true);
                    File.Delete(Rserverbase + @"\plugins\" + SR_PluginInfo.PluginName.Substring(5, SR_PluginInfo.PluginName.Length - 5));
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
            foreach (var x in modslist.Items)
            {
                SR_ModInfo SR_ModInfo = x as SR_ModInfo;
                if (SR_ModInfo.ModName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\mods\" + SR_ModInfo.ModName, Rserverbase + @"\mods\" + SR_ModInfo.ModName + ".disabled", true);
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName);
                }
                else
                {
                    File.Copy(Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 5), Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 13), true);
                    File.Delete(Rserverbase + @"\mods\" + SR_ModInfo.ModName.Substring(5, SR_ModInfo.ModName.Length - 5));
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
                memorySlider.Maximum = GetPhisicalMemory() / 1024.0 / 1024.0;
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
                string response = (await HttpService.GetApiContentAsync("query/java"))["data"]["versionList"].ToString();
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
                            dwnJava = await DownloadJava(selectJava.SelectedValue.ToString(), (await HttpService.GetApiContentAsync("download/java/" + selectJava.SelectedValue.ToString()))["data"]["url"].ToString());
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

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID.ToString()];
                _json["name"].Replace(Rservername);
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                _json["core"].Replace(Rserverserver);
                _json["memory"].Replace(RserverJVM);
                _json["args"].Replace(RserverJVMcmd);
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
                await MagicShow.ShowMsgDialogAsync(this, "下载Java即代表您接受Java的服务条款：\nhttps://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false);
                // DownjavaName = fileName;
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

                /*
                foreach (JavaScanner.JavaInfo info in strings)
                {
                    selectCheckedJavaComb.Items.Add(info.Version + ":" + info.Path);
                }
                */
                //selectCheckedJavaComb.ItemsSource = strings;
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

        private void getLaunchercode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string content;
                if (Rservermode == 0)
                {
                    if (Rserverserver.StartsWith("@libraries/"))
                    {
                        content = "@ECHO OFF\r\n\"" + Rserverjava + "\" " + RserverJVM + " " + RserverJVMcmd + " " + Rserverserver + " nogui" + "\r\npause";
                    }
                    else
                    {
                        content = "@ECHO OFF\r\n\"" + Rserverjava + "\" " + RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverserver + "\" nogui" + "\r\npause";
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
                if (ServerProcess.HasExited)
                {
                    MagicFlowMsg.ShowMessage("编码更改已生效！", 1);
                }
                else
                {
                    MagicFlowMsg.ShowMessage("编码已更改，重启服务器后生效！", 3);
                    //Growl.Warning("编码已更改，重启服务器后生效！");
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
                bool msgreturn = await MagicShow.ShowMsgDialogAsync(this, "关闭此功能后，服务器输出界面超过一定数量的日志后将不再清屏，这样可能会造成性能损失，您确定要继续吗？", "警告", true, "取消");
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
                TextRange textRange = new TextRange(outlog.Document.Blocks.FirstBlock.ContentStart, outlog.Document.Blocks.LastBlock.ContentEnd);
                strings[1] = textRange.Text;
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
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            if (_json["fastcmd"] == null)
            {
                fastCmdList.Items.Clear();
                foreach (ComboBoxItem item in fastCMD.Items)
                {
                    if (item.Tag != null && item.Tag.ToString() == "Header")
                    {
                        continue;
                    }
                    fastCmdList.Items.Add(item.Content.ToString());
                }
            }
            else
            {
                JArray fastcmdArray = (JArray)_json["fastcmd"];
                fastCmdList.Items.Clear();
                fastCMD.Items.Clear();
                fastCmdList.Items.Add("/（指令）");
                fastCMD.Items.Add("/（指令）");
                fastCMD.SelectedIndex = 0;
                foreach (var item in fastcmdArray)
                {
                    fastCMD.Items.Add(item.ToString());
                    fastCmdList.Items.Add(item.ToString());
                }
            }
        }
        private void SetFastCmd()
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID.ToString()];
            JArray fastcmdArray = new JArray(fastCmdList.Items.Cast<string>().Skip(1));
            _json["fastcmd"] = fastcmdArray;
            jsonObject[RserverID.ToString()] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            GetFastCmd();
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
                string text = await MagicShow.ShowInput(this, "请输入指令（格式为：/指令）\n若要输入的指令不是完整指令，请自行在最后添加空格");
                if (text != null)
                {
                    fastCmdList.Items.Add(text);
                    SetFastCmd();
                }
            }
            catch
            {
                MessageBox.Show("Err");
            }
        }

        private void delFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fastCmdList.SelectedIndex == 0)
                {
                    MessageBox.Show("无法删除根命令！");
                    return;
                }
                fastCmdList.Items.Remove(fastCmdList.Items[fastCmdList.SelectedIndex]);
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
                    startTimercmd.Content = "停止定时任务";
                }
                else
                {
                    startTimercmd.Content = "启动定时任务";
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
                int selectedID = int.Parse(tasksList.SelectedItem.ToString());
                if (startTimercmd.Content.ToString() == "启动定时任务")
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
                    startTimercmd.Content = "停止定时任务";
                }
                else
                {
                    taskFlag[selectedID] = false;
                    startTimercmd.Content = "启动定时任务";
                }
            }
            catch (Exception a)
            {
                timerCmdout.Text = "执行失败，" + a.Message;
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
                                if (ConPTYWindow != null)
                                {
                                    ConPTYWindow.ConptyConsole.ConPTYTerm.WriteToTerm((cmd + "\r\n").AsSpan());
                                }
                                else
                                {
                                    ServerProcess.StandardInput.WriteLine(cmd);
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
        #endregion
    }
}