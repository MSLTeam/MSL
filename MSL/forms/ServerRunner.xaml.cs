using HandyControl.Controls;
using HandyControl.Data;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
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
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly Process ServerProcess = new Process();
        private string ShieldLog = null;
        private bool autoRestart = false;
        private bool mslTips = true;
        private bool getServerInfo = MainWindow.getServerInfo;
        private int getServerInfoLine = 0;
        private bool getPlayerInfo = MainWindow.getPlayerInfo;
        private readonly int FirstStartTab;
        private string DownjavaName;
        private string RserverID = string.Empty;
        private string Rservername;
        private string Rserverjava;
        private string Rserverserver;
        private string RserverJVM;
        private string RserverJVMcmd;
        private string Rserverbase;

        /// <summary>
        /// 服务器运行窗口
        /// </summary>
        /// <param name="serverID">服务器ID</param>
        /// <param name="controlTab">Tab标签</param>
        public ServerRunner(string serverID, int controlTab = 0)
        {
            InitializeComponent();
            ServerProcess.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            ServerProcess.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            ServerProcess.Exited += new EventHandler(ServerExitEvent);
            ServerList.OpenServerForm += ShowWindowEvent;
            SettingsPage.ChangeSkinStyle += ChangeSkinStyle;
            RserverID = serverID;
            FirstStartTab = controlTab;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeSkinStyle();
            TabCtrl.SelectedIndex = -1;
            LoadingCircle loadingCircle = new LoadingCircle();
            MainGrid.Children.Add(loadingCircle);
            MainGrid.RegisterName("loadingBar", loadingCircle);
            ServerList.runningServers.Add(RserverID, 0);
            await Task.Delay(100);
            await Task.Run(() => { LoadingInfoEvent(); });
            GetFastCmd();
            LoadedInfoEvent();
            await Task.Delay(100);
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
                if (ServerProcess.HasExited != true)
                {
                    e.Cancel = true;
                    int dialog = Shows.ShowMsg(this, "检测到您没有关闭服务器，是否隐藏此窗口？\n如要重新显示此窗口，请在服务器列表内双击该服务器（或点击开启服务器按钮）", "警告", true, "取消");
                    if (dialog == 1)
                    {
                        Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    ServerList.runningServers.Remove(RserverID);
                    RserverID = string.Empty;
                    getServerInfo = false;
                    getPlayerInfo = false;
                    outlog.Document.Blocks.Clear();
                    GC.Collect();
                }
            }
            catch
            {
                ServerList.runningServers.Remove(RserverID);
                RserverID = string.Empty;
                getServerInfo = false;
                getPlayerInfo = false;
                outlog.Document.Blocks.Clear();
                GC.Collect();
            }
        }

        private void LoadingInfoEvent()
        {
            try
            {
                if (File.Exists(@"MSL\config.json"))
                {
                    JObject keys = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    if (keys["mslTips"] != null && (bool)keys["mslTips"] == false)
                    {
                        mslTips = false;
                    }
                    if (keys["sidemenuExpanded"] == null)
                    {
                        string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject.Add("sidemenuExpanded", true);
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                        Dispatcher.Invoke(() =>
                        {
                            Tab_Home.Width = double.NaN;
                            Tab_Console.Width = double.NaN;
                            Tab_Plugins.Width = double.NaN;
                            Tab_Settings.Width = double.NaN;
                            Tab_MoreFunctions.Width = double.NaN;
                            Tab_Timer.Width = double.NaN;
                        });
                    }
                    else if ((bool)keys["sidemenuExpanded"] == true)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Tab_Home.Width = double.NaN;
                            Tab_Console.Width = double.NaN;
                            Tab_Plugins.Width = double.NaN;
                            Tab_Settings.Width = double.NaN;
                            Tab_MoreFunctions.Width = double.NaN;
                            Tab_Timer.Width = double.NaN;
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Tab_Home.Width = 50;
                            Tab_Console.Width = 50;
                            Tab_Plugins.Width = 50;
                            Tab_Settings.Width = 50;
                            Tab_MoreFunctions.Width = 50;
                            Tab_Timer.Width = 50;
                        });
                    }
                }
            }
            catch
            {
                MessageBox.Show("err");
            }

            //Get Server's Information
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (_json["core"].ToString().IndexOf("bungeecord", StringComparison.OrdinalIgnoreCase) != -1 || _json["core"].ToString().IndexOf("waterfall", StringComparison.OrdinalIgnoreCase) != -1)//is the server Bungeecord,it will send a message and close window
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("开服器暂不支持Bungeecord类服务端的运行，请右键服务器并点击“用命令行开服”选项来开服！");
                    Close();
                });
            }
            Rservername = _json["name"].ToString();
            Rserverjava = _json["java"].ToString();
            Rserverbase = _json["base"].ToString();
            Rserverserver = _json["core"].ToString();
            RserverJVM = _json["memory"].ToString();
            RserverJVMcmd = _json["args"].ToString();
            if (_json["autostartServer"] != null && _json["autostartServer"].ToString() == "True")
            {
                autoRestart = true;
            }
            bool _closeOutlog = false;
            if (_json["closeOutlog"] != null && _json["closeOutlog"].ToString() == "True")
            {
                _closeOutlog = true;
            }
            if (_json["shieldLog"] != null)
            {
                ShieldLog = _json["shieldLog"].ToString();
            }
            bool _shieldStackOut = true;
            if (_json["shieldStackOut"] != null && _json["shieldStackOut"].ToString() == "False")
            {
                _shieldStackOut = false;
            }
            bool _autoClearOutlog = true;
            if (_json["autoClearOutlog"] != null && _json["autoClearOutlog"].ToString() == "False")
            {
                _autoClearOutlog = false;
            }
            string _encoding_in = null;
            if (_json["encoding_in"] != null)
            {
                _encoding_in = "输入编码:" + _json["encoding_in"].ToString();
            }
            string _encoding_out = null;
            if (_json["encoding_out"] != null)
            {
                _encoding_out = "输出编码:" + _json["encoding_out"].ToString();
            }
            bool _fileforceUTF8 = false;
            if (_json["fileforceUTF8"] != null && _json["fileforceUTF8"].ToString() == "True")
            {
                _fileforceUTF8 = true;
            }

            Dispatcher.Invoke(() =>
            {
                if (autoRestart)
                {
                    autoStartserver.Content = "关服自动重启:启用";
                }
                if (_closeOutlog)
                {
                    closeOutlog.Content = "日志输出:关";
                }
                if (ShieldLog != null)
                {
                    closeOutlog_Copy.Content = "屏蔽关键字日志:开";
                }
                if (!_shieldStackOut)
                {
                    shieldStackOut.Content = "屏蔽堆栈追踪:关";
                }
                if (!_autoClearOutlog)
                {
                    autoClearOutlog.Content = "自动清屏:关";
                }
                if (_encoding_in != null)
                {
                    inputCmdEncoding.Content = _encoding_in;
                }
                if (_encoding_out != null)
                {
                    outputCmdEncoding.Content = _encoding_out;
                }
                if (_fileforceUTF8)
                {
                    fileforceUTF8encoding.Content = "强制服务器文件UTF8编码:开";
                }
                this.Title = Rservername;//set title to server name
            });

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
                Dispatcher.Invoke(() =>
                {
                    this.Icon = new BitmapImage(new Uri(Rserverbase + "\\server-icon.png"));
                });
            }
            if (Rserverjava != "Java" && Rserverjava != "java")
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
            if (isChangeConfig)
            {
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                jsonObject[RserverID].Replace(_json);
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            }
        }//窗体加载后，运行此方法，主要为改变UI、检测服务器是否完整

        private void LoadedInfoEvent()
        {
            if (getServerInfo == false)
            {
                systemInfoBtn.Content = "显示占用:关";
            }
            else
            {
                Task.Run(GetSystemInfo);
            }
            if (getPlayerInfo == false)
            {
                playerInfoBtn.Content = "记录玩家:关";
            }
            LoadSettings();
            ReFreshPluginsAndMods();
        }//运行完LoadingInfoEvent后运行此方法，主要为加载占用显示模块和其他配置加载

        private void ShowWindowEvent()
        {
            if (ServerList.serverID == RserverID)
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Visibility = Visibility.Visible;
                Topmost = true;
                Topmost = false;
                Focus();
            }
        }//此方法是在Mainwindow的服务器列表双击后，如果服务器运行，就显示此窗口

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
                    Background = new ImageBrush(SettingsPage.GetImage("MSL\\Background.png"));
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
                TitleBox.SetResourceReference(ForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(CloseButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonForegroundProperty, "PrimaryTextBrush");
                this.SetResourceReference(OtherButtonHoverForegroundProperty, "PrimaryTextBrush");
            }
            else
            {
                this.SetResourceReference(NonClientAreaBackgroundProperty, "PrimaryBrush");
                TitleBox.Foreground = Brushes.White;
                CloseButtonForeground = Brushes.White;
                OtherButtonForeground = Brushes.White;
                OtherButtonHoverForeground = Brushes.White;
            }
        }
        #endregion

        private void SideMenuContextOpen_Click(object sender, RoutedEventArgs e)
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
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["sidemenuExpanded"] = true;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
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
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["sidemenuExpanded"] = false;
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
                catch { }
            }
        }

        private bool isRefresh = true;
        private void TabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (TabCtrl.SelectedIndex)
            {
                case 2:
                    if (isRefresh)
                    {
                        isRefresh = false;
                        ReFreshPluginsAndMods();
                    }
                    break;
                case 3:
                    if (isRefresh)
                    {
                        isRefresh = false;
                        GetServerConfig();
                    }
                    break;
                default:
                    isRefresh = true;
                    break;
            }
        }


        #region 仪表盘
        private async void solveProblemBtn_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await Shows.ShowMsgDialogAsync(this, "分析报告将在服务器关闭后生成！若使用后还是无法解决问题，请尝试进Q群询问（附带日志链接，日志链接可以点击分享日志按钮生成）：\n一群：1145888872  二群：234477679", "警告", true, "取消");
            if (dialogRet)
            {
                TabCtrl.SelectedIndex = 1;
                solveProblemSystem = true;
                LaunchServer();
            }
        }
        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            Growl.Info("正在为您打开服务器目录……");
            Process.Start(Rserverbase);
        }
        private async void kickPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await Shows.ShowMsgDialogAsync(this, "确定要踢出这个玩家吗？", "警告", true, "取消");
            if (dialogRet)
            {
                try
                {
                    ServerProcess.StandardInput.WriteLine("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
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
                        Shows.ShowMsgDialog(this, "您的公网IP为：" + localIP.ToString() + "\n您的服务器远程进入地址为：" + localIP.ToString() + ":" + ipAddress.Substring(ipAddress.IndexOf(":") + 1, ipAddress.Length - ipAddress.IndexOf(":") - 1) + "\n注意：记得检查您的防火墙是否关闭，否则远程玩家无法进入服务器！", "信息");
                    }
                    else
                    {
                        Shows.ShowMsgDialog(this, "您的公网IP为：" + localIP.ToString() + "\n您的服务器远程进入地址为：" + localIP.ToString() + "\n注意：记得检查您的防火墙是否关闭，否则远程玩家无法进入服务器！", "信息");
                    }
                    return;
                }
            }
            // 返回IP地址，如果没有找到公网IP，则返回"none"
            //return string.IsNullOrEmpty(ipAddress) ? "none" : ipAddress;
            await Shows.ShowMsgDialogAsync(this, "服务器开启后，通常远程的小伙伴是无法进入的，您需要进行内网映射才可让他人进入。开服器内置有免费的内网映射，您可点击主界面左侧的“内网映射”按钮查看详情并进行配置。", "注意", false);
            //GotoFrpcEvent();
        }

        private async void systemInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (systemInfoBtn.Content.ToString() == "显示占用:关")
            {
                Growl.Info("加载相关信息中，请稍等……");
                getServerInfo = true;
                systemInfoBtn.Content = "显示占用:开";
                Thread thread = new Thread(GetSystemInfo);
                thread.Start();
            }
            else
            {
                await Shows.ShowMsgDialogAsync(this, "关闭此功能后，输出预览功能也将同时关闭！", "注意", false);
                getServerInfo = false;
                systemInfoBtn.Content = "显示占用:关";
                previewOutlog.Text = "预览功能已关闭，请前往服务器控制台界面查看日志信息！";
            }
        }

        private void GetSystemInfo()
        {
            while (getServerInfo == true)
            {
                try
                {
                    try
                    {
                        var cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                        cpuCounter.NextValue();
                        float cpuUsage = cpuCounter.NextValue();
                        if ((int)cpuUsage <= 100)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                cpuInfoLab.Content = "CPU:" + cpuUsage.ToString("f2") + "%";
                                cpuInfoBar.Value = (int)cpuUsage;
                            });
                        }
                    }
                    catch
                    {
                        Dispatcher.Invoke(() =>
                        {
                            cpuInfoLab.Content = "无法获取CPU信息";
                        });
                    }
                    var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

                    float ramAvailable = ramCounter.NextValue() / 1024;
                    double allMemory = MainWindow.PhisicalMemory / 1024.0 / 1024.0 / 1024.0;
                    Dispatcher.Invoke(() =>
                    {
                        memoryInfoLab.Content = "总内存:" + allMemory.ToString("f2") + "G\n" + "已使用:" + (allMemory - ramAvailable).ToString("f2") + "G\n" + "可使用:" + ramAvailable.ToString("f2") + "G";
                        double temp = (allMemory - ramAvailable) / allMemory;
                        double _temp = ramAvailable / allMemory;
                        memoryInfoBar.Value = temp * 100;
                        availableMemoryInfoBar.Value = _temp * 100;
                        usedMemoryLab.Content = "系统已用内存:" + string.Format("{0:P}", temp);
                        availableMemoryInfoLab.Content = "系统空闲内存:" + string.Format("{0:P}", _temp);

                        if (outlog.Document.Blocks.LastBlock != null)
                        {
                            if (previewOutlog.LineCount >= 25)
                            {
                                previewOutlog.Clear();
                            }
                            TextRange textRange = new TextRange(outlog.Document.Blocks.LastBlock.ContentStart, outlog.Document.Blocks.LastBlock.ContentEnd);
                            if (!previewOutlog.Text.Contains(textRange.Text))
                            {
                                previewOutlog.Text += textRange.Text + "\n";
                                previewOutlog.ScrollToEnd();
                            }
                        }
                    });
                }
                catch
                {
                    Growl.Error("无法获取系统占用信息！显示占用功能已自动关闭！\n通常此问题是因为系统原因造成的，不影响软件正常使用！");
                    Dispatcher.Invoke(() =>
                    {
                        previewOutlog.Text = "预览功能已关闭，请前往服务器控制台界面查看日志信息！";
                        systemInfoBtn.Content = "显示占用:关";
                    });
                    getServerInfo = false;
                }
                Thread.Sleep(3000);
            }
        }
        private void playerInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playerInfoBtn.Content.ToString() == "记录玩家:关")
            {
                Growl.Success("已开启");
                getPlayerInfo = true;
                playerInfoBtn.Content = "记录玩家:开";
            }
            else
            {
                Growl.Success("已关闭");
                getPlayerInfo = false;
                playerInfoBtn.Content = "记录玩家:关";
            }
        }

        private async void banPlayer_Click(object sender, RoutedEventArgs e)
        {
            bool dialogRet = await Shows.ShowMsgDialogAsync(this, "确定要封禁这个玩家吗？封禁后该玩家将永远无法进入服务器！\n（原版解封指令：pardon +玩家名字，若添加插件，请使用插件的解封指令）", "警告", true, "取消");
            if (dialogRet)
            {
                try
                {
                    ServerProcess.StandardInput.WriteLine("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
                }
                catch
                {
                    Growl.Error("操作失败！");
                }
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
                string path1 = Rserverbase + "\\eula.txt";
                if (!File.Exists(path1) || (File.Exists(path1) && !File.ReadAllText(path1).Contains("eula=true")))
                {

                    bool dialog = await Shows.ShowMsgDialogAsync(this, "开启Minecraft服务器需要接受Mojang的EULA，是否仔细阅读EULA条款（https://aka.ms/MinecraftEULA）并继续开服？", "提示", true, "取消");
                    if (dialog == true)
                    {
                        Process.Start("https://aka.ms/MinecraftEULA");
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
                        }
                        catch (Exception a)
                        {
                            MessageBox.Show("出现错误，请手动修改eula文件或重试:" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        //Process.Start("https://account.mojang.com/documents/minecraft_eula");
                    }
                    else
                    {
                        return;
                    }
                }
                ChangeControlsState();
                string fileforceUTF8Jvm = "";
                if (fileforceUTF8encoding.Content.ToString().Contains("开") && !RserverJVMcmd.Contains("-Dfile.encoding=UTF-8"))
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
                GC.Collect();
            }
            catch (Exception a)
            {
                MessageBox.Show("出现错误！开服失败！\n错误代码: " + a.Message, "", MessageBoxButton.OK, MessageBoxImage.Question);
                cmdtext.IsEnabled = false;
                controlServer.Content = "开服";
                fastCMD.IsEnabled = false;
            }
        }
        private void StartServer(string StartFileArg)
        {
            try
            {
                Directory.CreateDirectory(Rserverbase);
                ServerProcess.StartInfo.WorkingDirectory = Rserverbase;
                ServerProcess.StartInfo.FileName = Rserverjava;
                ServerProcess.StartInfo.Arguments = StartFileArg;
                ServerProcess.StartInfo.CreateNoWindow = true;
                ServerProcess.StartInfo.UseShellExecute = false;
                ServerProcess.StartInfo.RedirectStandardInput = true;
                ServerProcess.StartInfo.RedirectStandardOutput = true;
                ServerProcess.StartInfo.RedirectStandardError = true;
                ServerProcess.EnableRaisingEvents = true;
                if (outputCmdEncoding.Content.ToString() == "输出编码:UTF8")
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
                Shows.ShowMsgDialog(this, "出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！", "错误");
                TabCtrl.SelectedIndex = 1;
                ChangeControlsState(false);
            }
        }

        private void ChangeControlsState(bool isEnable = true)
        {
            if (isEnable)
            {
                if (ServerList.runningServers.ContainsKey(RserverID))
                {
                    ServerList.runningServers.Remove(RserverID);
                    ServerList.runningServers.Add(RserverID, 1);
                }
                ServerStateChange();

                getServerInfoLine = 0;
                serverPlayerList.Items.Clear();
                serverStateLab.Content = "运行中";
                serverStateLab.Foreground = Brushes.Red;
                solveProblemBtn.IsEnabled = false;
                cmdtext.IsEnabled = true;
                cmdtext.Text = "";
                controlServer.Content = "关服";
                controlServer_Copy.Content = "关服";
                serverVersionLab.Content = "获取中";
                gameTypeLab.Content = "获取中";
                serverIPLab.Content = "获取中";
                localServerIPLab.Content = "获取中";
                fastCMD.IsEnabled = true;
                sendcmd.IsEnabled = true;
                outlog.Document.Blocks.Clear();
                Growl.Info("开服中，请稍等……");
                PrintLog("正在开启服务器，请稍等...", Brushes.Green);
            }
            else
            {
                if (ServerList.runningServers.ContainsKey(RserverID))
                {
                    ServerList.runningServers.Remove(RserverID);
                    ServerList.runningServers.Add(RserverID, 0);
                }
                ServerStateChange();

                Growl.Info("服务器已关闭！");
                cmdtext.Text = "服务器已关闭";
                serverStateLab.Content = "已关闭";
                serverStateLab.Foreground = Brushes.Green;
                solveProblemBtn.IsEnabled = true;
                sendcmd.IsEnabled = false;
                cmdtext.IsEnabled = false;
                controlServer.Content = "开服";
                controlServer_Copy.Content = "开服";
                fastCMD.IsEnabled = false;
                try
                {
                    ServerProcess.CancelOutputRead();
                    ServerProcess.CancelErrorRead();
                }
                catch
                { return; }
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() =>
                {
                    ReadStdOutputAction(e.Data);
                });
            }
        }

        #region 日志显示功能、彩色日志实现
        private static Brush tempbrush = Brushes.Green;
        private void PrintLog(string msg, Brush color)
        {
            tempbrush = color;
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

        private Dictionary<char, SolidColorBrush> colorDict = new Dictionary<char, SolidColorBrush>
        {
            ['r'] = (SolidColorBrush)tempbrush,
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

        private bool solveProblemSystem;
        private bool outlogEncodingAsk = true;
        private delegate void AddMessageHandler(string msg);
        private void ReadStdOutputAction(string msg)//日志回显实现
        {
            if (solveProblemSystem)
            {
                ProblemSystemShow(msg);
            }
            if (outlog.Document.Blocks.Count >= 1000 && autoClearOutlog.Content.ToString().Contains("开"))
            {
                outlog.Document.Blocks.Clear();
            }
            if ((msg.Contains("\tat ") && shieldStackOut.Content.ToString().Contains("开")) || (ShieldLog != null && msg.Contains(ShieldLog)) || closeOutlog.Content.ToString() != "日志输出:开")
            {
                return;
            }
            if (mslTips == false)
            {
                if (msg.StartsWith("["))
                {
                    if (msg.Contains("INFO]"))
                    {
                        PrintLog(msg, Brushes.Green);
                    }
                    else if (msg.Contains("WARN]"))
                    {
                        PrintLog(msg, Brushes.Orange);
                    }
                    else if (msg.Contains("ERROR]"))
                    {
                        PrintLog(msg, Brushes.Red);
                    }
                }
                else
                {
                    if (msg.Contains("INFO"))
                    {
                        PrintLog(msg, Brushes.Green);
                    }
                    else if (msg.Contains("WARN"))
                    {
                        PrintLog(msg, Brushes.Orange);
                    }
                    else if (msg.Contains("ERROR"))
                    {
                        PrintLog(msg, Brushes.Red);
                    }
                }
                return;
            }
            if (getServerInfoLine < 100)
            {
                GetServerInfoSys(msg);
            }
            if (msg.StartsWith("["))
            {
                if (msg.Contains("INFO]"))
                {
                    PrintLog("[" + DateTime.Now.ToString("T") + " 信息]" + msg.Substring(msg.IndexOf("INFO]") + 5), Brushes.Green);
                    //服务器启动成功和关闭时的提示
                    LogHandleInfo(msg);
                }
                else if (msg.Contains("WARN]"))
                {
                    if (msg.Contains("Advanced terminal features are not available in this environment"))
                    {
                        return;
                    }
                    PrintLog("[" + DateTime.Now.ToString("T") + " 警告]" + msg.Substring(msg.IndexOf("WARN]") + 5), Brushes.Orange);
                    LogHandleWarn(msg);
                }
                else if (msg.Contains("ERROR]"))
                {
                    PrintLog("[" + DateTime.Now.ToString("T") + " 错误]" + msg.Substring(msg.IndexOf("ERROR]") + 6), Brushes.Red);
                }
                else
                {
                    PrintLog(msg, Brushes.Green);
                }
            }
            else
            {
                if (msg.Contains("INFO"))
                {
                    PrintLog(msg, Brushes.Green);
                    LogHandleInfo(msg);
                }
                else if (msg.Contains("WARN"))
                {
                    if (msg.Contains("Advanced terminal features are not available in this environment"))
                    {
                        return;
                    }
                    PrintLog(msg, Brushes.Orange);
                    LogHandleWarn(msg);
                }
                else if (msg.Contains("ERROR"))
                {
                    PrintLog(msg, Brushes.Red);
                }
                else
                {
                    PrintLog(msg, tempbrush);
                }
            }
            if (msg.Contains("�"))
            {
                Brush brush = tempbrush;
                PrintLog("MSL检测到您的服务器输出了乱码日志，请尝试去“更多功能”界面更改服务器的“输出编码”来解决此问题！", Brushes.Red);
                tempbrush = brush;
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
                                JObject _json = (JObject)jsonObject[RserverID];
                                _json["encoding_out"] = encoding;
                                jsonObject[RserverID] = _json;
                                File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                                Dispatcher.InvokeAsync(() =>
                                {
                                    outputCmdEncoding.Content = "输出编码:" + encoding;
                                    Growl.Success("更改完毕！");
                                });
                                Task.Run(async () =>
                                {
                                    getServerInfoLine = 102;
                                    autoRestart = true;
                                    await Task.Delay(200);
                                    try
                                    {
                                        ServerProcess.Kill();
                                    }
                                    catch { }
                                    await Task.Delay(200);
                                    autoRestart = false;
                                });
                            }
                            return true;
                        },
                        ShowDateTime = false
                    });
                }
            }
        }

        private void LogHandleInfo(string msg)
        {
            if (msg.Contains("Done") && msg.Contains("For help"))
            {
                getServerInfoLine = 101;
                PrintLog("已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情查看开服器的内网映射界面。", Brushes.Green);
                Growl.Success("已成功开启服务器！");
                serverStateLab.Content = "已开服";
                Thread thread = new Thread(CheckOnlineMode);
                thread.Start();
            }
            else if (msg.Contains("加载完成") && msg.Contains("如需帮助"))
            {
                getServerInfoLine = 101;
                PrintLog("已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情参照开服器的内网映射界面。", Brushes.Green);
                Growl.Success("已成功开启服务器！");
                serverStateLab.Content = "已开服";
                Thread thread = new Thread(CheckOnlineMode);
                thread.Start();
            }
            else if (msg.Contains("Stopping server"))
            {
                PrintLog("正在关闭服务器！", Brushes.Green);
            }

            //玩家进服是否记录
            if (getPlayerInfo == true)
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

        private void GetServerInfoSys(string msg)
        {
            if (msg.Contains("下载") || msg.Contains("Download") || msg.Contains("download"))
            {
                getServerInfoLine--;

                if (getServerInfoLine == -5)
                {
                    Growl.Info("该服务端可能在下载依赖文件，请耐心等待！");
                }
            }
            else
            {
                getServerInfoLine++;
            }
            if (getServerInfoLine == 100)
            {
                if (serverVersionLab.Content.ToString() == "获取中")
                {
                    serverVersionLab.Content = "未知";
                }
                if (gameTypeLab.Content.ToString() == "获取中")
                {
                    gameTypeLab.Content = "未知";
                }
                if (serverIPLab.Content.ToString() == "获取中")
                {
                    serverIPLab.Content = "未知";
                }
                if (localServerIPLab.Content.ToString() == "获取中")
                {
                    localServerIPLab.Content = "未知";
                }
            }
            try
            {
                if (msg.Contains("Starting minecraft server version"))
                {
                    serverVersionLab.Content = msg.Substring(msg.LastIndexOf(" ") + 1);
                }
                else if (msg.Contains("Default game type:"))
                {
                    string gamemode = msg.Substring(msg.LastIndexOf(" ") + 1);
                    if (gamemode.IndexOf("survival", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        gamemode = "生存";
                    }
                    if (gamemode.IndexOf("creative", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        gamemode = "创造";
                    }
                    if (gamemode.IndexOf("adventure", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        gamemode = "冒险";
                    }
                    if (gamemode.IndexOf("spectator", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        gamemode = "旁观";
                    }
                    gameTypeLab.Content = gamemode;
                }
                else if (msg.Contains("Starting Minecraft server on"))
                {
                    serverIPLab.Content = msg.Substring(msg.LastIndexOf(" ") + 1);
                    ChangeServerIP();
                }
                else if (msg.Contains("正在启动") && msg.Contains("的Minecraft服务端"))
                {
                    serverVersionLab.Content = msg.Substring(msg.IndexOf("正在启动") + 4, msg.IndexOf("的") - (msg.IndexOf("正在启动") + 4));
                }
                else if (msg.Contains("正在启动Minecraft服务器") && msg.Contains("版本"))
                {
                    serverVersionLab.Content = msg.Substring(msg.IndexOf("版本") + 2);
                }
                else if (msg.Contains("默认游戏模式:"))
                {
                    gameTypeLab.Content = msg.Substring(msg.LastIndexOf("游戏模式:") + 5);
                }
                else if (msg.Contains("正在") && msg.Contains("上启动服务器"))
                {
                    serverIPLab.Content = msg.Substring(msg.IndexOf("正在 ") + 3, msg.IndexOf("上") - (msg.IndexOf("正在 ") + 3));
                    ChangeServerIP();
                }
            }
            catch
            {
                Growl.Info("开服器在获取服务器信息时出现错误！此问题不影响服务器运行，您可继续正常使用或将此问题报告给作者！");
            }
        }

        private void ChangeServerIP()
        {
            if (serverIPLab.Content.ToString().Contains("*"))
            {
                localServerIPLab.Content = serverIPLab.Content.ToString().Replace("*", "127.0.0.1");
                if (localServerIPLab.Content.ToString().Contains(":25565"))
                {
                    localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                    return;
                }
                localServerIPLab.FontSize = 12;
            }
            else
            {
                serverIPLab.FontSize = 12;
                localServerIPLab.Content = serverIPLab.Content;
                if (localServerIPLab.Content.ToString().Contains(":25565"))
                {
                    localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                    return;
                }
                localServerIPLab.FontSize = 12;
            }
        }
        private void GetPlayerInfoSys(string msg)
        {
            if (msg.Contains("logged in with entity id"))
            {
                string a = msg.Substring(0, msg.IndexOf(" logged in with entity id"));
                while (a.Contains(" "))
                {
                    a = a.Substring(a.IndexOf(" ") + 1);
                }
                serverPlayerList.Items.Add(a);
            }
            else if (msg.Contains("lost connection:"))
            {
                try
                {
                    string a = msg.Substring(0, msg.IndexOf(" lost connection:"));
                    while (a.Contains(" "))
                    {
                        a = a.Substring(a.IndexOf(" ") + 1);
                    }
                    foreach (string x in serverPlayerList.Items)
                    {
                        if (x.IndexOf(a + "[/") + 1 != 0)
                        {
                            serverPlayerList.Items.Remove(x);
                            break;
                        }
                    }
                }
                catch
                {
                    Growl.Error("好像出现了点错误……");
                }
            }
            else if (msg.Contains("与服务器失去连接:"))
            {
                try
                {
                    string a = msg.Substring(0, msg.IndexOf(" 与服务器失去连接"));
                    while (a.IndexOf(" ") + 1 != 0)
                    {
                        a = a.Substring(a.IndexOf(" ") + 1);
                    }
                    foreach (string x in serverPlayerList.Items)
                    {
                        if (x.IndexOf(a + "[/") + 1 != 0)
                        {
                            serverPlayerList.Items.Remove(x);
                            break;
                        }
                    }
                }
                catch
                {
                    Growl.Error("好像出现了点错误……");
                }
            }
        }
        private void CheckOnlineMode()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(1000);
                    FileStream fs = new FileStream(Rserverbase + @"\server.properties", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                    string text = sr.ReadToEnd();
                    sr.Close();
                    if (text.IndexOf("online-mode=true") + 1 != 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            PrintLog("检测到您没有关闭正版验证，如果客户端为离线登录的话，请点击“更多功能”里“关闭正版验证”按钮以关闭正版验证。否则离线账户将无法进入服务器！", Brushes.OrangeRed);
                            Growl.Info("检测到您没有关闭正版验证，您可前往“更多功能”界面点击“关闭正版验证”按钮以关闭。否则离线账户无法进入服务器！");
                            onlineModeLab.Content = "已开启";
                        });
                        break;
                    }
                    else if (text.IndexOf("online-mode=false") + 1 != 0)
                    {
                        Dispatcher.Invoke(() =>
                        { onlineModeLab.Content = "已关闭"; }); break;
                    }
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    { onlineModeLab.Content = "未知"; }); break;
                }
            }
            Dispatcher.Invoke(() =>
            { if (onlineModeLab.Content.ToString() == "获取中") onlineModeLab.Content = "未知"; });
        }

        private string foundProblems;
        private void ProblemSystemShow(string msg)
        {
            if (getServerInfoLine <= 50)
            {
                getServerInfoLine++;
                if (msg.Contains("UnsupportedClassVersionError"))
                {
                    foundProblems += "*不支持的Class版本：您的Java版本可能太低！\n";
                    int a = int.Parse(msg.Substring(msg.IndexOf("(class file version ") + 20, 2));
                    foundProblems += $"请使用Java{a - 44}或以上版本！\n";
                }
                else if (msg.Contains("Unsupported Java detected"))
                {
                    foundProblems += "*不匹配的Java版本：\n";
                    foundProblems += "请使用" + msg.Substring(msg.IndexOf("Only up to ") + 11, 7) + "！\n";
                }
                else if (msg.Contains("requires running the server with"))
                {
                    foundProblems += "*不匹配的Java版本：\n";
                    foundProblems += "请使用" + msg.Substring(msg.IndexOf("Java"), 7) + "！\n";
                }
                else if (msg.Contains("Invalid or corrupt jarfile"))
                {
                    foundProblems += "*服务端核心不完整，请重新下载！\n";
                }
                else if (msg.Contains("OutOfMemoryError"))
                {
                    foundProblems += "*服务器内存分配过低或过高！\n";
                }
                else if (msg.Contains("Invalid maximum heap size"))
                {
                    foundProblems += "*服务器最大内存分配有误！\n" + msg + "\n";
                }
                else if (msg.Contains("Unrecognized VM option"))
                {
                    foundProblems += "*服务器JVM参数有误！请前往设置界面进行查看！\n错误的参数为：" + msg.Substring(msg.IndexOf("'") + 1, msg.Length - 3 - msg.IndexOf(" '")) + "\n";
                }
                else if (msg.Contains("There is insufficient memory for the Java Runtime Environment to continue"))
                {
                    foundProblems += "*JVM内存分配不足，请尝试增加系统的虚拟内存（不是内存条！具体方法请自行上网查找）！\n";
                }
                else if (msg.Contains("进程无法访问"))
                {
                    if (foundProblems == null || !foundProblems.Contains("*文件被占用，您的服务器可能多开，可尝试重启电脑解决！\n"))
                    {
                        foundProblems += "*文件被占用，您的服务器可能多开，可尝试重启电脑解决！\n";
                    }
                }
                else if (msg.Contains("FAILED TO BIND TO PORT"))
                {
                    foundProblems += "*端口被占用，您的服务器可能多开，可尝试重启电脑解决！\n";
                }
                else if (msg.Contains("Unable to access jarfile"))
                {
                    foundProblems += "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！\n";
                }
                else if (msg.Contains("加载 Java 代理时出错"))
                {
                    foundProblems += "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！\n";
                }
                else if (msg.Contains("ArraylndexOutOfBoundsException"))
                {
                    foundProblems += "*开启服务器时发生数组越界错误，请尝试更换服务端再试！\n";
                }
                else if (msg.Contains("ClassCastException"))
                {
                    foundProblems += "*开启服务器时发生类转换异常，请检查Java版本是否匹配，或者让开服器为您下载Java环境（设置界面更改）！\n";
                }
                else if (msg.Contains("could not open") && msg.Contains("jvm.cfg"))
                {
                    foundProblems += "*Java异常，请检查Java环境是否正常，或者让开服器为您下载Java环境（设置界面更改）！\n";
                }
                else if (msg.Contains("Failed to download vanilla jar"))
                {
                    foundProblems += "*下载原版核心文件失败，您可尝试使用代理或更换服务端为Spigot端！\n";
                }
                else if (msg.Contains("Exception in thread \"main\""))
                {
                    foundProblems += "*服务端核心Main方法报错，可能是Java版本不正确，请尝试更换Java版本！\n";
                }
            }
            if (msg.Contains("Could not load") && msg.Contains("plugin"))
            {
                foundProblems += "*无法加载插件！\n";
                foundProblems += "插件名称：" + msg.Substring(msg.IndexOf("Could not load '") + 16, msg.IndexOf("' ") - (msg.IndexOf("Could not load '") + 16)) + "\n";
            }
            else if (msg.Contains("Error loading plugin"))
            {
                foundProblems += "*无法加载插件！\n";
                foundProblems += "插件名称：" + msg.Substring(msg.IndexOf(" '") + 2, msg.IndexOf("' ") - (msg.IndexOf(" '") + 2)) + "\n";
            }
            else if (msg.Contains("Error occurred while enabling "))
            {
                foundProblems += "*在启用 " + msg.Substring(msg.IndexOf("enabling ") + 9, msg.IndexOf(" (") - (msg.IndexOf("enabling ") + 9)) + " 时发生了错误\n"; ;
            }
            else if (msg.Contains("Encountered an unexpected exception"))
            {
                foundProblems += "*服务器出现意外崩溃，可能是由于模组冲突，请检查您的模组列表（如果使用的是整合包，请使用整合包制作方提供的Server专用包开服）\n";
            }
            else if (msg.Contains("Mod") && msg.Contains("requires"))
            {
                string _msg = msg;
                if (msg.Contains("&"))
                {
                    _msg = "";
                    string[] splitMsg = msg.Split('&');
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }
                        string text = everyMsg.Substring(1);
                        _msg += text;
                    }
                }
                else if (msg.Contains("§"))
                {
                    _msg = "";
                    string[] splitMsg = msg.Split('§');
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }
                        string text = everyMsg.Substring(1);
                        _msg += text;
                    }
                }
                else if (msg.Contains("\x1B"))
                {
                    _msg = "";
                    string[] splitMsg = msg.Split('\x1B');
                    foreach (var everyMsg in splitMsg)
                    {
                        if (everyMsg == string.Empty)
                        {
                            continue;
                        }
                        string text = everyMsg.Substring(everyMsg.IndexOf("m") + 1);
                        _msg += text;
                    }
                }
                string modNamePattern = @"Mod (\w+) requires";
                string preModPattern = @"requires (\w+ \d+\.\d+\.\d+)";

                Match modNameMatch = Regex.Match(_msg, modNamePattern);
                Match preModMatch = Regex.Match(_msg, preModPattern);

                if (modNameMatch.Success && preModMatch.Success)
                {
                    string modName = modNameMatch.Groups[1].Value;
                    string preMod = preModMatch.Groups[1].Value;

                    if (msg.Contains("or above"))
                    {
                        if (foundProblems == null || !foundProblems.Contains("*" + modName + " 模组出现问题！该模组需要 " + preMod + " 或以上版本！\n"))
                        {
                            foundProblems += "*" + modName + " 模组出现问题！该模组需要 " + preMod + " 或以上版本！\n";
                        }
                    }
                    else
                    {
                        if (foundProblems == null || !foundProblems.Contains("*" + modName + " 模组出现问题！该模组需要 " + preMod + " ！\n"))
                        {
                            foundProblems += "*" + modName + " 模组出现问题！该模组需要 " + preMod + " ！\n";
                        }
                    }
                }
            }
        }

        private void ServerExitEvent(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                ChangeControlsState(false);
                if (solveProblemSystem)
                {
                    solveProblemSystem = false;
                    if (foundProblems == null)
                    {
                        Growl.Info("服务器已关闭！开服器未检测到相关问题，您可前往“更多功能”界面上传服务器日志，并发送给他人以寻求帮助！");
                    }
                    else
                    {
                        Growl.Info("服务器已关闭！即将为您展示分析报告！");
                        Shows.ShowMsgDialog(this, foundProblems, "服务器分析报告");
                        foundProblems = null;
                    }
                }
                else if (ServerProcess.ExitCode != 0 && getServerInfoLine <= 100)
                {
                    bool dialogRet = await Shows.ShowMsgDialogAsync(this, "服务器疑似异常关闭，是您人为关闭的吗？\n您可使用MSL的崩溃分析系统进行检测，也可前往“更多功能”界面上传服务器日志，并发送给他人以寻求帮助！（请不要截图此弹窗！）\n点击确定开始进行崩溃分析", "提示", true, "取消");
                    if (dialogRet)
                    {
                        TabCtrl.SelectedIndex = 1;
                        solveProblemSystem = true;
                        LaunchServer();
                    }
                }
                else if (autoRestart == true)
                {
                    await Task.Delay(200);
                    LaunchServer();
                }
            });
        }


        string lastCommand;
        string nextCommand;

        void SendCommand()
        {
            lastCommand = cmdtext.Text;
            try
            {
                if (inputCmdEncoding.Content.ToString() == "输入编码:UTF8")
                {
                    if (fastCMD.SelectedIndex == 0)
                    {
                        SendCmdUTF8(cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/op（给管理员）"))
                    {
                        SendCmdUTF8("op " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/deop（去除管理员）"))
                    {
                        SendCmdUTF8("deop " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/ban（封禁玩家）"))
                    {
                        SendCmdUTF8("ban " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/say（全服说话）"))
                    {
                        SendCmdUTF8("say " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/pardon（解除封禁）"))
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
                    else if (fastCMD.SelectedItem.ToString().Contains("/op（给管理员）"))
                    {
                        SendCmdANSL("op " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/deop（去除管理员）"))
                    {
                        SendCmdANSL("deop " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/ban（封禁玩家）"))
                    {
                        SendCmdANSL("ban " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/say（全服说话）"))
                    {
                        SendCmdANSL("say " + cmdtext.Text);
                    }
                    else if (fastCMD.SelectedItem.ToString().Contains("/pardon（解除封禁）"))
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
                if (inputCmdEncoding.Content.ToString() == "输入编码:UTF8")
                {
                    SendCmdUTF8(cmdtext.Text);
                }
                else
                {
                    SendCmdANSL(cmdtext.Text);
                }
            }
        }

        void SendCmdUTF8(string cmd)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(cmd);
            ServerProcess.StandardInput.BaseStream.Write(utf8Bytes, 0, utf8Bytes.Length);
            ServerProcess.StandardInput.WriteLine();
            cmdtext.Text = "";
        }
        void SendCmdANSL(string cmd)
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
            else if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (fastCMD.SelectedIndex == 0)
                {
                    try
                    {
                        //tab补全命令列表
                        List<string> tabCompleteList = new List<string>
                        {
                            "gamemode creative",
                            "gamemode survival",
                            "gamemode spectator",
                            "gamemode adventure",
                            "gamerule keepInventory true",
                            "gamerule keepInventory false",
                            "difficulty peaceful",
                            "difficulty easy",
                            "difficulty normal",
                            "difficulty hard",
                            "say",
                            "op",
                            "pardon",
                            "time set day",
                            "time set night",
                            "ban",
                            "weather clear",
                            "ban",
                            "kick"
                        };
                        //获取输入的文本
                        string input = cmdtext.Text;
                        //判断是否为空
                        if (!string.IsNullOrEmpty(input))
                        {
                            //初始化替换标志和候选列表
                            bool isReplace = true;
                            List<string> candidates = new List<string>();
                            //获取最后一个空格之前的文本
                            string prefix = input;
                            if (input.Contains(" "))
                            {
                                prefix = input.Substring(0, input.LastIndexOf(" ") + 1);
                            }
                            //遍历命令列表
                            foreach (string command in tabCompleteList)
                            {
                                //如果命令以输入的文本开头
                                if (command.StartsWith(input))
                                {
                                    //获取命令的剩余部分
                                    string suffix = command.Replace(input, "");
                                    //如果剩余部分包含空格
                                    if (suffix.Contains(" "))
                                    {
                                        //如果是第一次匹配
                                        if (isReplace)
                                        {
                                            //替换输入的文本为命令的第一个空格之前的部分
                                            cmdtext.Text = input + suffix.Substring(0, suffix.IndexOf(" "));
                                        }
                                        if (!candidates.Contains(command.Substring(0, command.IndexOf(" "))))
                                        {
                                            //添加候选项为命令的第一个空格之前的部分
                                            candidates.Add(command.Substring(0, command.IndexOf(" ")));
                                        }
                                    }
                                    else
                                    {
                                        //如果是第一次匹配
                                        if (isReplace)
                                        {
                                            //替换输入的文本为命令
                                            cmdtext.Text = command;
                                        }
                                        if (!candidates.Contains(command))
                                        {
                                            //添加候选项为命令
                                            candidates.Add(command);
                                        }
                                    }
                                    //设置替换标志为false
                                    isReplace = false;
                                }
                            }
                            //如果有候选项
                            if (candidates.Count > 0)
                            {
                                if (candidates.Count == 1 && prefix != input)
                                {
                                    candidates.Clear();
                                    //遍历命令列表
                                    foreach (string command in tabCompleteList)
                                    {
                                        //如果命令以输入的文本开头
                                        if (command.StartsWith(prefix))
                                        {
                                            //添加候选项为命令
                                            candidates.Add(command.Replace(prefix, ""));
                                        }
                                    }
                                    if (candidates.Count != 1)
                                    {
                                        //显示候选项，用逗号分隔
                                        PrintLog(string.Join(",", candidates), Brushes.Green);
                                        int multiComplete = 0;
                                        foreach (string command in candidates)
                                        {
                                            if (cmdtext.Text.Replace(prefix, "") == command)
                                            {
                                                multiComplete++;
                                                break;
                                            }
                                            multiComplete++;
                                        }
                                        if (candidates.Count < multiComplete + 1)
                                        {
                                            multiComplete = 0;
                                        }

                                        cmdtext.Text = prefix + candidates[multiComplete];
                                    }
                                }
                                else
                                {
                                    //显示候选项，用逗号分隔
                                    PrintLog(string.Join(",", candidates), Brushes.Green);
                                }
                            }
                        }
                        //清空命令列表
                        tabCompleteList.Clear();
                    }
                    catch
                    {
                        Growl.Error("命令补全失败！");
                    }
                    finally
                    {
                        //设置光标位置为文本末尾
                        cmdtext.SelectionStart = cmdtext.Text.Length;
                    }
                }
            }
        }

        private void cmdtext_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                if (cmdtext.Text != lastCommand)
                {
                    nextCommand = cmdtext.Text;
                    cmdtext.Text = lastCommand;
                }
                cmdtext.Select(cmdtext.Text.Length, 0);
            }
            if (e.Key == Key.Down)
            {
                if (cmdtext.Text != nextCommand)
                {
                    lastCommand = cmdtext.Text;
                    cmdtext.Text = nextCommand;
                }
                cmdtext.Select(cmdtext.Text.Length, 0);
            }
        }
        private void controlServer_Click(object sender, RoutedEventArgs e)
        {
            if (controlServer.Content.ToString() == "开服")
            {
                if (getServerInfoLine == 102)
                {
                    getServerInfoLine = 101;
                    return;
                }
                LaunchServer();
            }
            else
            {
                Growl.Info("关服中，请耐心等待……\n双击按钮可强制关服（不建议）");
                ServerProcess.StandardInput.WriteLine("stop");
            }
        }

        private void controlServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (controlServer.Content.ToString() == "关服")
                {
                    getServerInfoLine = 102;
                    ServerProcess.Kill();
                }
            }
            catch { }
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
                changeServerPropertiesLab.Content = "更改服务器配置信息";
                changeServerProperties.Visibility = Visibility.Visible;
                changeServerProperties.Height = double.NaN;
                changeServerProperties_Add.Visibility = Visibility.Visible;
                changeServerProperties_Add.Height = double.NaN;
                changeServerProperties_Add_Add.Visibility = Visibility.Visible;
                changeServerProperties_Add_Add.Height = double.NaN;
            }
            catch { changeServerPropertiesLab.Content = "找不到配置文件，无法更改相关设置（请尝试开启一次服务器）"; changeServerProperties.Visibility = Visibility.Collapsed; changeServerProperties.Height = 0; changeServerProperties_Add.Visibility = Visibility.Collapsed; changeServerProperties_Add.Height = 0; changeServerProperties_Add_Add.Visibility = Visibility.Collapsed; changeServerProperties_Add_Add.Height = 0; }
        }

        private string[] ServerBaseConfig()
        {
            string[] strings = new string[9];
            Encoding encoding = Functions.GetTextFileEncodingType(Rserverbase + @"\server.properties");
            string config = File.ReadAllText(Rserverbase + @"\server.properties", encoding);
            if (config.Contains("\r"))
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
                if (ServerProcess.HasExited == false)
                {
                    Shows.ShowMsgDialog(this, "您没有关闭服务器，无法调整服务器功能！", "错误");
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
                    Shows.ShowMsgDialog(this, "保存成功！", "信息");
                }
                catch (Exception ex)
                {
                    Shows.ShowMsgDialog(this, "保存失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
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
                if (ServerProcess.HasExited == false)
                {
                    Shows.ShowMsgDialog(this, "您没有关闭服务器，无法更换图标！", "错误");
                    return;
                }
            }
            catch
            { }
            if (File.Exists(Rserverbase + "\\server-icon.png"))
            {
                bool dialogret = await Shows.ShowMsgDialogAsync(this, "检测到服务器已设置有图标，是否删除该图标？", "警告", true, "取消");
                if (dialogret)
                {
                    try
                    {
                        File.Delete(Rserverbase + "\\server-icon.png");
                    }
                    catch (Exception ex)
                    {
                        Shows.ShowMsgDialog(this, "图标删除失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                        return;
                    }
                    bool _dialogret = await Shows.ShowMsgDialogAsync(this, "原图标已删除，是否继续操作？", "提示", true, "取消");
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
            await Shows.ShowMsgDialogAsync(this, "请先准备一张64*64像素的图片（格式为png），准备完成后点击确定以继续", "如何操作？");
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
                    Shows.ShowMsgDialog(this, "图标更换完成！", "信息");
                }
                catch (Exception ex)
                {
                    Shows.ShowMsgDialog(this, "图标更换失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                }
            }
        }

        private async void changeWorldMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServerProcess.HasExited == false)
                {
                    Shows.ShowMsgDialog(this, "您没有关闭服务器，无法更换地图！", "错误");
                    return;
                }
            }
            catch
            { }
            string levelName = gameWorldText.Text;
            if (Directory.Exists(Rserverbase + @"\" + levelName))
            {
                if (await Shows.ShowMsgDialogAsync(this, "点击确定后，MSL将删除原先主世界地图（删除后，地图将从电脑上彻底消失，如有必要请提前备份！）\n点击取消以中止操作", "警告", true, "取消"))
                {
                    ShowDialogs dialog = new ShowDialogs();
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
                    if (await Shows.ShowMsgDialogAsync(this, "MSL同时检测到了下界地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                    {
                        ShowDialogs dialog = new ShowDialogs();
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
                    if (await Shows.ShowMsgDialogAsync(this, "MSL同时检测到了末地地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                    {
                        ShowDialogs dialog = new ShowDialogs();
                        dialog.ShowTextDialog(this, "删除中，请稍候");
                        await Task.Run(() =>
                        {
                            DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_the_end");
                            di.Delete(true);
                        });
                        dialog.CloseTextDialog();
                    }
                }
                if (await Shows.ShowMsgDialogAsync(this, "相关地图已经成功删除！是否选择新存档进行导入？（如果不导入而直接开服，服务器将会重新创建一个新世界）", "提示", true, "取消"))
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "请选择地图文件夹(或解压后的文件夹)"
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            ShowDialogs _dialog = new ShowDialogs();
                            _dialog.ShowTextDialog(this, "导入中，请稍候");
                            await Task.Run(() =>
                            {
                                Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                            });
                            _dialog.CloseTextDialog();
                            Shows.ShowMsgDialog(this, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                        }
                        catch (Exception ex)
                        {
                            Shows.ShowMsgDialog(this, "导入世界失败！\n错误代码：" + ex.Message, "错误");
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
                        ShowDialogs _dialog = new ShowDialogs();
                        _dialog.ShowTextDialog(this, "导入中，请稍候");
                        await Task.Run(() =>
                        {
                            Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                        });
                        _dialog.CloseTextDialog();
                        Shows.ShowMsgDialog(this, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                    }
                    catch (Exception ex)
                    {
                        Shows.ShowMsgDialog(this, "导入世界失败！\n错误代码：" + ex.Message, "错误");
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
                List<PluginInfo> list = new List<PluginInfo>();
                DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\plugins");
                FileInfo[] file = directoryInfo.GetFiles("*.*");
                foreach (FileInfo f in file)
                {
                    if (f.Name.EndsWith(".disabled"))
                    {
                        list.Add(new PluginInfo("[已禁用]" + f.Name));
                    }
                    else if (f.Name.EndsWith(".jar"))
                    {
                        list.Add(new PluginInfo(f.Name));
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
                List<ModInfo> list = new List<ModInfo>();
                DirectoryInfo directoryInfo1 = new DirectoryInfo(Rserverbase + @"\mods");
                FileInfo[] file1 = directoryInfo1.GetFiles("*.*");
                foreach (FileInfo f1 in file1)
                {
                    if (f1.Name.EndsWith(".disabled"))
                    {
                        list.Add(new ModInfo("[已禁用]" + f1.Name));
                    }
                    else if (f1.Name.EndsWith(".jar"))
                    {
                        list.Add(new ModInfo(f1.Name));
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
            }
            if (hideList)
            {
                lab001.Visibility = Visibility.Visible;
                reFresh.Visibility = Visibility.Visible;
                pluginsAndModsTab.Visibility = Visibility.Hidden;
            }
            else if (lab001.Visibility == Visibility.Visible)
            {
                lab001.Visibility = Visibility.Hidden;
                reFresh.Visibility = Visibility.Hidden;
                pluginsAndModsTab.Visibility = Visibility.Visible;
            }
        }

        private void mpHelp_Click(object sender, RoutedEventArgs e)
        {
            Shows.ShowMsgDialog(this, "若标签栏为灰色且无法点击，说明此服务端不支持相应的（插件或模组）功能，或相关（插件或模组）文件夹未创建。请更换服务端核心并重启服务器再试。", "提示");
        }

        private async void addModsTip_Click(object sender, RoutedEventArgs e)
        {
            bool dialog = await Shows.ShowMsgDialogAsync(this, "服务器需要添加的模组和客户端要添加的模组有所不同，增加方块、实体、玩法的MOD，是服务器需要安装的（也就是服务端和客户端都需要安装），而小地图、皮肤补丁、输入补丁、优化MOD、视觉显示类的MOD，服务器是一定不需要安装的（也就是只能加在客户端里）\n点击确定查看详细区分方法", "提示", true, "取消");
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
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
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
                PluginInfo pluginInfo = pluginslist.SelectedItem as PluginInfo;
                if (pluginInfo.PluginName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\plugins\" + pluginInfo.PluginName, Rserverbase + @"\plugins\" + pluginInfo.PluginName + ".disabled", true);
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName);
                    ReFreshPluginsAndMods();
                }
                else
                {
                    File.Copy(Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 5), Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 13), true);
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 5));
                    ReFreshPluginsAndMods();
                }
            }
            catch { return; }
        }
        private void delPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
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

                PluginInfo pluginInfo = pluginslist.SelectedItem as PluginInfo;
                if (pluginInfo.PluginName.ToString().Contains("[已禁用]"))
                {
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 5));
                }
                else
                {
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName);
                }
                ReFreshPluginsAndMods();
            }
            catch { return; }
        }
        private void disMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
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
                ModInfo modInfo = modslist.SelectedItem as ModInfo;
                if (modInfo.ModName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\mods\" + modInfo.ModName, Rserverbase + @"\mods\" + modInfo.ModName + ".disabled", true);
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName);
                    ReFreshPluginsAndMods();
                }
                else
                {
                    File.Copy(Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 5), Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 13), true);
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 5));
                    ReFreshPluginsAndMods();
                }
            }
            catch { return; }
        }
        private void delMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
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
                ModInfo modInfo = modslist.SelectedItem as ModInfo;
                if (modInfo.ModName.ToString().Contains("[已禁用]"))
                {
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 5));
                }
                else
                {
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName);
                }
                ReFreshPluginsAndMods();
            }
            catch { return; }
        }

        private void disAllPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }
            }
            catch { }
            foreach (var x in pluginslist.Items)
            {
                PluginInfo pluginInfo = x as PluginInfo;
                if (pluginInfo.PluginName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\plugins\" + pluginInfo.PluginName, Rserverbase + @"\plugins\" + pluginInfo.PluginName + ".disabled", true);
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName);
                }
                else
                {
                    File.Copy(Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 5), Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 13), true);
                    File.Delete(Rserverbase + @"\plugins\" + pluginInfo.PluginName.Substring(5, pluginInfo.PluginName.Length - 5));
                }
            }
            ReFreshPluginsAndMods();
        }
        private void disAllMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ServerProcess.HasExited)
                {
                    Shows.ShowMsgDialog(this, "服务器在运行中，无法进行操作！请关闭服务器后再试！", "警告");
                    return;
                }
            }
            catch { }
            foreach (var x in modslist.Items)
            {
                ModInfo modInfo = x as ModInfo;
                if (modInfo.ModName.ToString().IndexOf("[已禁用]") == -1)
                {
                    File.Copy(Rserverbase + @"\mods\" + modInfo.ModName, Rserverbase + @"\mods\" + modInfo.ModName + ".disabled", true);
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName);
                }
                else
                {
                    File.Copy(Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 5), Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 13), true);
                    File.Delete(Rserverbase + @"\mods\" + modInfo.ModName.Substring(5, modInfo.ModName.Length - 5));
                }
            }
            ReFreshPluginsAndMods();
        }
        private void opencurseforge_Click(object sender, RoutedEventArgs e)
        {
            DownloadMods downloadMods = new DownloadMods
            {
                serverbase = Rserverbase,
                Owner = this
            };
            downloadMods.ShowDialog();
            ReFreshPluginsAndMods();
        }
        private async void openpluginweb_Click(object sender, RoutedEventArgs e)
        {
            await Shows.ShowMsgDialogAsync(this, "开服器暂时无法提供插件下载功能，即将为您打开Spigot网站，请您自行寻找插件并下载（下载后点击右下角“+”将其添加入服务器）。", "提示");
            Process.Start("https://www.spigotmc.org/resources/");
        }
        #endregion

        #region 服务器设置

        //////////////////////这里是服务器设置界面

        private void LoadSettings()
        {
            try
            {
                nAme.Text = Rservername;
                server.Text = Rserverserver;
                memorySlider.Maximum = MainWindow.PhisicalMemory / 1024.0 / 1024.0;
                bAse.Text = Rserverbase;
                jVMcmd.Text = RserverJVMcmd;
                jAva.Text = Rserverjava;

                Task.Run(() =>
                {
                    LoadJavaInfo();
                });

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
                        string minMemorySubstring = RserverJVM.Substring(minMemoryIndex + 4);
                        string minMemoryValue = minMemorySubstring.Substring(0, minMemorySubstring.IndexOf("M"));

                        int maxMemoryIndex = RserverJVM.IndexOf("-Xmx");
                        string maxMemorySubstring = RserverJVM.Substring(maxMemoryIndex + 4);
                        string maxMemoryValue = maxMemorySubstring.Substring(0, maxMemorySubstring.IndexOf("M"));

                        memorySlider.ValueStart = int.Parse(minMemoryValue);
                        memorySlider.ValueEnd = int.Parse(maxMemoryValue);
                        memoryInfo.Text = "最小:" + minMemoryValue + "M," + "最大:" + maxMemoryValue + "M";
                    }
                    catch
                    {
                        int maxMemoryIndex = RserverJVM.IndexOf("-Xmx");
                        string maxMemorySubstring = RserverJVM.Substring(maxMemoryIndex + 4);
                        string maxMemoryValue = maxMemorySubstring.Substring(0, maxMemorySubstring.IndexOf("M"));

                        memorySlider.ValueStart = 0;
                        memorySlider.ValueEnd = int.Parse(maxMemoryValue);
                        memoryInfo.Text = "最小:0M," + "最大:" + maxMemoryValue + "M";
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error!!!");
            }
        }

        private void LoadJavaInfo()
        {
            try
            {
                if (MainWindow.serverLink == null)
                {
                    Thread.Sleep(5000);
                }
                Dispatcher.Invoke(() =>
                {
                    selectJava.Items.Clear();
                });
                string response = HttpService.Get("query/java");
                JArray jArray = JArray.Parse(response);
                foreach (var j in jArray)
                {
                    Dispatcher.Invoke(() =>
                    {
                        selectJava.Items.Add(j.ToString());
                    });
                }
                Dispatcher.Invoke(() =>
                {
                    selectJava.SelectedIndex = 0;
                });
            }
            catch
            {
                Growl.Error("出现错误，获取Java版本列表失败！");
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
                    Regex pattern = new Regex(@"(Java\d+)");
                    Match m = pattern.Match(jAva.Text);
                    string javaVersion = m.Groups[1].Value;

                    // 和selectJava.Items里的每一项比对
                    bool found = false;
                    foreach (var item in selectJava.Items)
                    {
                        if (item.ToString() == javaVersion)
                        {
                            // 如果有相等的，就把selectJava切换到相应的栏
                            useDownJv.IsChecked = true;
                            selectJava.SelectedItem = item;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        useSelf.IsChecked = true;
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
                if (ServerProcess.HasExited == false)
                {
                    Shows.ShowMsgDialog(this, "服务器运行时无法更改服务器设置！", "错误");
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
                if (useDownJv.IsChecked == true)
                {
                    Growl.Info("获取Java地址……");
                    int dwnJava = 0;
                    try
                    {
                        await Dispatcher.Invoke(async () =>
                        {
                            dwnJava = await DownloadJava(selectJava.SelectedItem.ToString(), HttpService.Get("download/java/" + selectJava.SelectedItem.ToString()));
                        });
                        if (dwnJava == 1)
                        {
                            Growl.Info("解压中……");
                            bool unzipJava = await UnzipJava();
                            if (!unzipJava)
                            {
                                Shows.ShowMsgDialog(this, "安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误");
                                doneBtn1.IsEnabled = true;
                                refreahConfig.IsEnabled = true;
                                return;
                            }
                            Growl.Info("Java下载完成！");
                        }
                        else if (dwnJava == 2)
                        {
                            Growl.Info("完成！");
                        }
                        else
                        {
                            Shows.ShowMsgDialog(this, "下载取消！", "提示");
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
                        Shows.ShowMsgDialog(this, "检测Java可用性失败，您的Java似乎不可用！请检查是否选择正确！", "错误");
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
                //Directory.CreateDirectory(bAse.Text);
                doneBtn1.IsEnabled = true;
                refreahConfig.IsEnabled = true;
                Rservername = nAme.Text;
                Title = Rservername;
                Rserverjava = jAva.Text;
                if (server.Text.Contains("forge") && server.Text.Contains("installer"))
                {
                    bool dialog = await Shows.ShowMsgDialogAsync(this, "您选择的服务端是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                    if (dialog)
                    {
                        string installReturn;
                        //调用新版forge安装器
                        string[] installForge = await Shows.ShowInstallForge(this, Rserverbase + "\\" + server.Text, Rserverbase, Rserverjava);
                        if (installForge[0] == "0")
                        {
                            if (await Shows.ShowMsgDialogAsync(this, "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
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
                            installReturn = Functions.InstallForge(Rserverjava, Rserverbase, server.Text, installForge[1]);
                        }
                        else
                        {
                            return;
                        }
                        if (installReturn == null)
                        {
                            Shows.ShowMsgDialog(this, "下载失败！", "错误");
                            return;
                        }
                        server.Text = installReturn;
                    }
                }
                Rserverserver = server.Text;
                if (Rserverbase != bAse.Text)
                {
                    bool dialog = await Shows.ShowMsgDialogAsync(this, "检测到您更改了服务器目录，是否将当前的服务器目录移动至新的目录？", "警告", true, "取消");
                    if (dialog)
                    {
                        Functions.MoveFolder(Rserverbase, bAse.Text);
                    }
                }
                Rserverbase = bAse.Text;
                RserverJVMcmd = jVMcmd.Text;

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID];
                _json["name"].Replace(Rservername);
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                _json["core"].Replace(Rserverserver);
                _json["memory"].Replace(RserverJVM);
                _json["args"].Replace(RserverJVMcmd);
                jsonObject[RserverID].Replace(_json);
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                LoadSettings();
                SaveConfigEvent();

                Shows.ShowMsgDialog(this, "保存完毕！", "信息");
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
                await Shows.ShowMsgDialogAsync(this, "下载Java即代表您接受Java的服务条款：\nhttps://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false);
                DownjavaName = fileName;
                bool downDialog = await Shows.ShowDownloader(this, downUrl, "MSL", "Java.zip", "下载" + fileName + "中……");
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

        private async Task<bool> UnzipJava()
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
        private void a01_Click(object sender, RoutedEventArgs e)
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
                if (Path.GetDirectoryName(openfile.FileName) != Rserverbase)
                {
                    File.Copy(openfile.FileName, Rserverbase + @"\" + openfile.SafeFileName, true);
                    Shows.ShowMsgDialog(this, "已将服务端核心复制到了服务器目录之中，您现在可以将源文件删除了！", "提示");
                }
                server.Text = openfile.SafeFileName;
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
            DownloadServer downloadServer = new DownloadServer(Rserverbase, Rserverjava)
            {
                Owner = this
            };
            downloadServer.ShowDialog();
            if (File.Exists(Rserverbase + @"\" + downloadServer.downloadServerName))
            {
                server.Text = downloadServer.downloadServerName;
                Growl.Success("服务端下载完毕！已自动选择该服务端核心，请记得保存哦~");
            }
            else if (downloadServer.downloadServerName.StartsWith("@libraries/"))
            {
                server.Text = downloadServer.downloadServerName;
                Growl.Success("服务端下载完毕！已自动选择该服务端核心，请记得保存哦~");
            }
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
                    Shows.ShowMsgDialog(this, "检测失败，您的环境变量似乎不存在！", "错误");
                }
            }
        }

        private void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            if (selectCheckedJavaComb.Items.Count == 0)
            {
                Shows.ShowMsgDialog(this, "请先进行搜索！", "警告");
                useSelf.IsChecked = true;
            }
        }

        private async void ScanJava_Click(object sender, RoutedEventArgs e)
        {
            List<JavaScanner.JavaInfo> strings = null;
            int dialog = Shows.ShowMsg(this, "即将开始检测电脑上的Java，此过程可能需要一些时间，请耐心等待。\n目前有两种检测模式，一种是简单检测，只检测一些关键目录，用时较少，普通用户可优先使用此模式。\n第二种是深度检测，将检测所有磁盘的所有目录，耗时可能会很久，请慎重选择！", "提示", true, "开始深度检测", "开始简单检测");
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
                selectCheckedJavaComb.ItemsSource = strings.Select(info => $"Java{info.Version}: {info.Path}").ToList();
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
                string content = "@ECHO OFF\r\n\"" + Rserverjava + "\" " + RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverbase + @"\" + Rserverserver + "\" nogui" + "\r\npause";
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
        private void GetIPV6_Click(object sender, RoutedEventArgs e)
        {
            string ipv6 = "";
            HttpListener listener = null;
            try
            {
                ipv6 = HttpService.Get("", "https://6.ipw.cn", 3);
                Clipboard.Clear();
                Clipboard.SetText(ipv6);

                //监听到21102端口
                listener = new HttpListener();
                listener.Prefixes.Add($"http://*:{21102}/");
                listener.Start();

                //异步，用于监听get请求
                Task.Run(async () =>
                {
                    while (listener.IsListening)
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if (request.HttpMethod == "GET")
                        {
                            string responseString = "200";
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            System.IO.Stream output = response.OutputStream;
                            await output.WriteAsync(buffer, 0, buffer.Length);
                            output.Close();
                        }
                    }
                });

                //发送get，测试是否通
                string result = HttpService.Get("", $"https://ipv6test.52nahida.site/?addr=[{ipv6}]&port=21102", 3);

                //结果返回
                var jsonResult = JObject.Parse(result);
                int em = jsonResult.Value<int>("em");
                string emsg = jsonResult.Value<string>("msg");
                if (em == 200)
                {
                    Shows.ShowMsgDialog(this, $"您的IPV6公网地址是：{ipv6}\n已经帮您复制到剪贴板啦！\n注意：IPV6地址格式是：[IP]:端口\n若无法使用IPV6连接，请检查：\n-连接方是否有IPV6地址", "成功获取IPV6公网地址并测试连通性！");
                }
                else
                {
                    Shows.ShowMsgDialog(this, $"您的IPV6公网地址是：{ipv6}\n但是您的IPV6地址目前不能被访问！\n请检查：\n-您是否放行防火墙（包含电脑，路由器防火墙）\n-路由器是否使用桥接模式（若使用NAT，IPV6地址将不是公网）\n错误信息：{emsg}", "成功获取IPV6公网地址但测试连通性失败！");
                }

            }
            catch (Exception ex)
            {
                if (ipv6 == "")
                {
                    Shows.ShowMsgDialog(this, "您当前的网络没有IPV6支持\n建议上网搜索如何开启IPV6\n或者联系运营商获取帮助~", "获取IPV6地址失败！");
                }
                else
                {
                    Shows.ShowMsgDialog(this, $"获取到了IPv6地址:{ipv6}，但是公网连接测试失败\n请检查：\n-您是否放行防火墙（包含电脑，路由器防火墙）\n-路由器是否使用桥接模式（若使用NAT，IPV6地址将不是公网）\n错误信息：{ex.Message}", "测试连接失败！");
                }
            }
            finally
            {
                //// 测试完成后关闭监听
                if (listener != null)
                {
                    listener.Stop();
                    listener.Close();
                }
            }
        }



        private void autostartServer_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (autoStartserver.Content.ToString() == "关服自动重启:禁用")
            {
                autoRestart = true;
                autoStartserver.Content = "关服自动重启:启用";
                _json["autostartServer"] = "True";
            }
            else
            {
                autoRestart = false;
                autoStartserver.Content = "关服自动重启:禁用";
                _json["autostartServer"] = "False";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }
        private void inputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (inputCmdEncoding.Content.ToString() == "输入编码:ANSI")
            {
                inputCmdEncoding.Content = "输入编码:UTF8";
                _json["encoding_in"] = "UTF8";
            }
            else if (inputCmdEncoding.Content.ToString() == "输入编码:UTF8")
            {
                inputCmdEncoding.Content = "输入编码:ANSI";
                _json["encoding_in"] = "ANSI";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            Growl.Success("编码更改已生效！");
        }
        private void outputCmdEncoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (outputCmdEncoding.Content.ToString() == "输出编码:ANSI")
            {
                outputCmdEncoding.Content = "输出编码:UTF8";
                _json["encoding_out"] = "UTF8";
            }
            else if (outputCmdEncoding.Content.ToString() == "输出编码:UTF8")
            {
                outputCmdEncoding.Content = "输出编码:ANSI";
                _json["encoding_out"] = "ANSI";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            try
            {
                if (ServerProcess.HasExited)
                {
                    Growl.Success("编码更改已生效！");
                }
                else
                {
                    Growl.Warning("编码已更改，重启服务器后生效！");
                }
            }
            catch
            {
                Growl.Success("编码更改已生效！");
            }
        }
        private void fileforceUTF8encoding_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (fileforceUTF8encoding.Content.ToString().Contains("开"))
            {
                fileforceUTF8encoding.Content = "强制服务器文件UTF8编码:关";
                _json["fileforceUTF8"] = "False";
            }
            else if (fileforceUTF8encoding.Content.ToString().Contains("关"))
            {
                fileforceUTF8encoding.Content = "强制服务器文件UTF8编码:开";
                _json["fileforceUTF8"] = "True";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }
        private async void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServerProcess.HasExited == false)
                {
                    bool dialogRet = await Shows.ShowMsgDialogAsync(this, "检测到服务器正在运行，点击确定以关闭服务器", "信息");
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
                    Shows.ShowMsgDialog(this, "修改完毕，请重新开启服务器！", "信息");
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
                    Shows.ShowMsgDialog(this, "修改完毕，请重新开启服务器！", "信息");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，您确定您的服务器启动过一次吗？请手动修改server.properties文件或重试:" + a.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void closeOutlog_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (closeOutlog.Content.ToString() == "日志输出:开")
            {
                closeOutlog.Content = "日志输出:关";
                _json["closeOutlog"] = "True";
            }
            else
            {
                closeOutlog.Content = "日志输出:开";
                _json["closeOutlog"] = "False";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        private async void closeOutlog_Copy_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (closeOutlog_Copy.Content.ToString() == "屏蔽关键字日志:关")
            {
                string text = await Shows.ShowInput(this, "输入你想屏蔽的关键字，\n开服器将不会输出含有此关键字的日志");
                if (text != null)
                {
                    ShieldLog = text;
                    closeOutlog_Copy.Content = "屏蔽关键字日志:开";
                    _json["shieldLog"] = text;
                }
            }
            else
            {
                ShieldLog = null;
                closeOutlog_Copy.Content = "屏蔽关键字日志:关";
                _json.Remove("shieldLog");
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }
        private void shieldStackOut_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (shieldStackOut.Content.ToString() == "屏蔽堆栈追踪:开")
            {
                shieldStackOut.Content = "屏蔽堆栈追踪:关";
                _json["shieldStackOut"] = "False";
            }
            else
            {
                shieldStackOut.Content = "屏蔽堆栈追踪:开";
                _json["shieldStackOut"] = "True";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }
        private async void autoClearOutlog_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            if (autoClearOutlog.Content.ToString() == "自动清屏:开")
            {
                bool msgreturn = await Shows.ShowMsgDialogAsync(this, "关闭此功能后，服务器输出界面超过1000行日志后将不再清屏，这样可能会造成性能损失，您确定要继续吗？", "警告", true, "取消");
                if (msgreturn)
                {
                    autoClearOutlog.Content = "自动清屏:关";
                    _json["autoClearOutlog"] = "False";
                }
            }
            else
            {
                autoClearOutlog.Content = "自动清屏:开";
                _json["autoClearOutlog"] = "True";
            }
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
        }

        #region 上传日志到mclo.gs

        private void shareLog_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(outlog.Document.Blocks.FirstBlock.ContentStart, outlog.Document.Blocks.LastBlock.ContentEnd);
            /*
            //遍历获取logs
            foreach (var block in outlog.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            logs += run.Text + "\n";
                        }
                    }
                }
            }
            */
            //启动线程上传日志
            Task.Run(() =>
            {
                UploadLogs(textRange.Text);
            });
            Growl.Info("正在上传···");
        }

        private void UploadLogs(string logs)
        {
            string path = "1/log";
            string customUrl = "https://api.mclo.gs";
            int contentType = 2;
            //请求内容
            string parameterData = "content=" + logs;

            string response = HttpService.Post(path, contentType, parameterData, customUrl);
            //解析返回的东东
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);

            if (jsonResponse.success == true)
            {
                Dispatcher.Invoke(() =>
                {
                    Clipboard.Clear();
                    Clipboard.SetText(jsonResponse.url.ToString());
                });
                Growl.Success("日志地址: " + jsonResponse.url + "\n已经复制到剪贴板啦！\n如果遇到问题且不会看日志,\n请把链接粘贴给别人寻求帮助，\n记得要详细描述你的问题哦！");
            }
            else
            {
                Growl.Error("请求失败: " + jsonResponse.error);
            }
        }

        //上传Forge安装日志
        private void forgeInstallLogUpload_Click(object sender, RoutedEventArgs e)
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
                    Task.Run(() =>
                    {
                        UploadLogs(logsContent);
                    });
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
            JObject _json = (JObject)jsonObject[RserverID];
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
            JObject _json = (JObject)jsonObject[RserverID];
            JArray fastcmdArray = new JArray(fastCmdList.Items.Cast<string>().Skip(1));
            _json["fastcmd"] = fastcmdArray;
            jsonObject[RserverID] = _json;
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
            JObject _json = (JObject)jsonObject[RserverID];
            if (_json["fastcmd"] == null)
            {
                return;
            }
            else
            {
                _json.Remove("fastcmd");
                jsonObject[RserverID] = _json;
                File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                Shows.ShowMsgDialog(this, "要使重置生效需重启此窗口，请您手动关闭此窗口并打开", "提示");
            }
        }

        private async void addFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = await Shows.ShowInput(this, "请输入指令（格式为：/指令）\n若要输入的指令不是完整指令，请自行在最后添加空格");
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

        List<int> taskID = new List<int>();
        Dictionary<int, int> taskTimers = new Dictionary<int, int>();
        Dictionary<int, string> taskCmds = new Dictionary<int, string>();
        Dictionary<int, bool> stopTasks = new Dictionary<int, bool>();
        private void addTask_Click(object sender, RoutedEventArgs e)
        {
            if (taskID.Count == 0)
            {
                taskID.Add(0);
            }
            else
            {
                taskID.Add(taskID.Max() + 1);
            }
            //MessageBox.Show(taskID.Max().ToString());
            tasksList.ItemsSource = taskID.ToArray();
            stopTasks.Add(taskID.Max(), true);
            taskTimers.Add(taskID.Max(), 10);
            taskCmds.Add(taskID.Max(), "say Hello World!");
            //tasksList.Items.Add(taskID.Max());
            loadOrSaveTaskConfig.Content = "保存任务配置";
        }

        private void delTask_Click(object sender, RoutedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                if (startTimercmd.Content.ToString() == "停止定时任务")
                {
                    Shows.ShowMsgDialog(this, "请先停止任务！", "警告");
                    return;
                }
                int selectedIndex = tasksList.SelectedIndex;
                int selectedTaskID = taskID[selectedIndex];

                stopTasks.Remove(selectedTaskID);
                taskTimers.Remove(selectedTaskID);
                taskCmds.Remove(selectedTaskID);

                taskID.RemoveAt(selectedIndex);
                tasksList.ItemsSource = taskID.ToArray();

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
                if (stopTasks[taskID[tasksList.SelectedIndex]] == false)
                {
                    startTimercmd.Content = "停止定时任务";
                }
                else
                {
                    startTimercmd.Content = "启动定时任务";
                }
                timerCmdout.Content = "无";
                timercmdTime.Text = taskTimers[taskID[tasksList.SelectedIndex]].ToString();
                timercmdCmd.Text = taskCmds[taskID[tasksList.SelectedIndex]];
            }
            else
            {
                timercmdTime.Text = "";
                timercmdCmd.Text = "";
            }
        }
        private void timercmdTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                taskTimers[taskID[tasksList.SelectedIndex]] = int.Parse(timercmdTime.Text);
            }
        }

        private void timercmdCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tasksList.SelectedIndex != -1)
            {
                taskCmds[taskID[tasksList.SelectedIndex]] = timercmdCmd.Text;
            }
        }
        private void startTimercmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startTimercmd.Content.ToString() == "启动定时任务")
                {
                    stopTasks[taskID[tasksList.SelectedIndex]] = false;
                    int i = taskID[tasksList.SelectedIndex];
                    Task.Run(() => TimedTasks(i, taskTimers[i], taskCmds[i]));
                    startTimercmd.Content = "停止定时任务";
                }
                else
                {
                    stopTasks[taskID[tasksList.SelectedIndex]] = true;
                    startTimercmd.Content = "启动定时任务";
                }
            }
            catch (Exception a)
            {
                timerCmdout.Content = "执行失败，" + a.Message;
            }
        }

        void TimedTasks(int id, int timer, string cmd)
        {
            try
            {
                while (!stopTasks[id])
                {
                    try
                    {
                        if (ServerProcess.HasExited == false)
                        {
                            ServerProcess.StandardInput.WriteLine(cmd);
                            Dispatcher.Invoke(() =>
                            {
                                if (tasksList.SelectedIndex != -1 && taskID[tasksList.SelectedIndex] == id)
                                {
                                    timerCmdout.Content = "执行成功  时间：" + DateTime.Now.ToString("F");
                                }
                            });
                        }
                    }
                    catch
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (tasksList.SelectedIndex != -1 && taskID[tasksList.SelectedIndex] == id)
                            {
                                timerCmdout.Content = "执行失败，请检查服务器是否开启  时间：" + DateTime.Now.ToString("F");
                            }
                        });
                    }
                    Thread.Sleep(timer * 1000);
                }
            }
            catch
            {
                return;
            }
        }

        private void loadOrSaveTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            if (loadOrSaveTaskConfig.Content.ToString() == "加载任务配置")
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID];

                if (_json["timedtasks"] != null)
                {
                    // Access data
                    //JArray taskNameFromFile = (JArray)_json["timedtasks"]["taskName"];
                    JArray taskIDFromFile = (JArray)_json["timedtasks"]["taskID"];
                    JObject taskTimersFromFile = (JObject)_json["timedtasks"]["taskTimers"];
                    JObject taskCmdsFromFile = (JObject)_json["timedtasks"]["taskCmds"];
                    JObject stopTasksFromFile = (JObject)_json["timedtasks"]["stopTasks"];

                    // Process data and clear existing lists
                    //tasksList.Items.Clear();
                    taskID.Clear();
                    taskTimers.Clear();
                    taskCmds.Clear();
                    stopTasks.Clear();

                    // Process taskName
                    /*
                    foreach (var taskname in taskNameFromFile)
                    {
                        tasksList.Items.Add(taskname);
                    }*/

                    // Process taskID
                    foreach (var taskid in taskIDFromFile)
                    {
                        taskID.Add((int)taskid);
                    }

                    // Process taskTimers
                    foreach (var tasktimer in taskTimersFromFile)
                    {
                        taskTimers.Add(int.Parse(tasktimer.Key), (int)tasktimer.Value);
                    }

                    // Process taskCmds
                    foreach (var taskcmd in taskCmdsFromFile)
                    {
                        taskCmds.Add(int.Parse(taskcmd.Key), (string)taskcmd.Value);
                    }

                    // Process stopTasks
                    foreach (var stoptask in stopTasksFromFile)
                    {
                        stopTasks.Add(int.Parse(stoptask.Key), (bool)stoptask.Value);
                    }
                    tasksList.ItemsSource = taskID.ToArray();
                }
                Growl.Success("加载成功！");
                if (tasksList.Items.Count != 0)
                {
                    loadOrSaveTaskConfig.Content = "保存任务配置";
                }
            }
            else
            {
                JObject data = new JObject(
            new JProperty("taskName", new JArray(tasksList.Items)),
            new JProperty("taskID", new JArray(taskID)),
            new JProperty("taskTimers", new JObject(taskTimers.Select(kv => new JProperty(kv.Key.ToString(), kv.Value)))),
            new JProperty("taskCmds", new JObject(taskCmds.Select(kv => new JProperty(kv.Key.ToString(), kv.Value)))),
            new JProperty("stopTasks", new JObject(stopTasks.Select(kv => new JProperty(kv.Key.ToString(), kv.Value))))
        );
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverID];
                _json["timedtasks"] = data;
                jsonObject[RserverID] = _json;
                File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                Growl.Success("保存成功！");
            }
        }

        private void delTaskConfig_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverID];
            _json.Remove("timedtasks");
            jsonObject[RserverID] = _json;
            File.WriteAllText("MSL\\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
            Growl.Success("删除成功！");
        }
        #endregion

        #region window event
        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }

        #endregion
    }
}