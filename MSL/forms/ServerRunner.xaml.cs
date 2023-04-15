using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.forms;
using MSL.pages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
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
    public partial class ServerRunner : Window
    {
        public delegate void DelReadStdOutput(string result);
        public static event DeleControl SaveConfigEvent;
        public static event DeleControl ServerStateChange;
        public static event DeleControl GotoFrpcEvent;
        public Process SERVERCMD = new Process();
        string ShieldLog;
        public event DelReadStdOutput ReadStdOutput;
        bool autoserver = false;
        bool getServerInfo = MainWindow.getServerInfo;
        int getServerInfoLine = 0;
        bool getPlayerInfo = MainWindow.getPlayerInfo;
        bool solveProblemSystem;
        string foundProblems;
        string DownjavaName;
        public static string DownServer;
        public string RserverId;
        public string Rservername;
        public string Rserverjava;
        public string Rserverserver;
        public string RserverJVM;
        public string RserverJVMcmd;
        public string Rserverbase;
        DispatcherTimer timer1 = new DispatcherTimer();
        DispatcherTimer timer2 = new DispatcherTimer();
        DispatcherTimer cmdtimer = new DispatcherTimer();
        DispatcherTimer cmdtimer2 = new DispatcherTimer();
        DispatcherTimer cmdtimer3 = new DispatcherTimer();

        /// <summary>
        /// /////////主要代码
        /// </summary>
        public ServerRunner()
        {
            timer1.Tick += new EventHandler(timer1_Tick);
            timer2.Tick += new EventHandler(timer2_Tick);
            cmdtimer.Tick += new EventHandler(cmdtimer_Tick);
            cmdtimer2.Tick += new EventHandler(cmdtimer2_Tick);
            cmdtimer3.Tick += new EventHandler(cmdtimer3_Tick);
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            ServerList.OpenServerForm += ShowWindowEvent;
            SettingsPage.DelBackground += DelBackground;
            InitializeComponent();
            Width += 16;
            Height += 24;
            MinWidth = Width;
            MinHeight = Height;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Get Server's Information
            JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverId];
            if (_json["core"].ToString().IndexOf("Bungeecord") + 1 != 0 || _json["core"].ToString().IndexOf("bungeecord") + 1 != 0)//is the server Bungeecord,it will send a message and close window
            {
                MessageBox.Show("开服器暂不支持Bungeecord服务端的运行，请右键点击“用命令行开服”选项来开服！");
                Close();
            }
            Rservername = _json["name"].ToString();
            Rserverjava = _json["java"].ToString();
            Rserverbase = _json["base"].ToString();
            Rserverserver = _json["core"].ToString();
            RserverJVM = _json["memory"].ToString();
            RserverJVMcmd = _json["args"].ToString();

            this.Title = Rservername;//set title to server name
            if (File.Exists(Rserverbase + "\\server-icon.png"))//check server-icon,if exist,set icon to server-icon
            {
                this.Icon= new BitmapImage(new Uri(Rserverbase + "\\server-icon.png"));
            }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"))//check background and set it
            {
                Background = new ImageBrush(SettingsPage.GetImage(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Background.png"));
            }

            GetFastCmd();
            if (ServerList.ControlSetPMTab == true)
            {
                ServerList.ControlSetPMTab = false;
                if (getServerInfo == false)
                {
                    systemInfoBtn.Content = "显示占用";
                }
                else
                {
                    Thread thread = new Thread(GetSystemInfo);
                    thread.Start();
                }
                if (getPlayerInfo == false)
                {
                    playerInfoBtn.Content = "记录玩家:关";
                }
                LoadSettings();
                ReFreshPluginsAndMods();
                TabCtrl.SelectedIndex = 3;
                ReFreshPluginsAndMods();
            }
            else if (ServerList.ControlSetServerTab == true)
            {
                ServerList.ControlSetServerTab = false;
                if (getServerInfo == false)
                {
                    systemInfoBtn.Content = "显示占用:关";
                }
                else
                {
                    Thread thread = new Thread(GetSystemInfo);
                    thread.Start();
                }
                if (getPlayerInfo == false)
                {
                    playerInfoBtn.Content = "记录玩家:关";
                }
                LoadSettings();
                ReFreshPluginsAndMods();
                TabCtrl.SelectedIndex = 4;
            }
            else
            {
                LaunchServer();
                if (getServerInfo == false)
                {
                    systemInfoBtn.Content = "显示占用:关";
                }
                else
                {
                    Thread thread = new Thread(GetSystemInfo);
                    thread.Start();
                }
                if (getPlayerInfo == false)
                {
                    playerInfoBtn.Content = "记录玩家:关";
                }
                LoadSettings();
                ReFreshPluginsAndMods();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited != true)
                {
                    DialogShow.ShowMsg(this, "检测到您没有关闭服务器，是否隐藏此窗口？\n如要重新显示此窗口，请在服务器列表内双击该服务器（或点击开启服务器按钮）", "警告", true, "取消");
                    e.Cancel = true;
                    if (MessageDialog._dialogReturn == true)
                    {
                        MessageDialog._dialogReturn = false;
                        Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    RserverId = null;
                    getServerInfo = false;
                    getPlayerInfo = false;
                    outlog.Document.Blocks.Clear();
                    GC.Collect();
                }
            }
            catch
            {
                RserverId = null;
                getServerInfo = false;
                getPlayerInfo = false;
                outlog.Document.Blocks.Clear();
                GC.Collect();
            }
        }
        void ShowWindowEvent()
        {
            if (RserverId==MainWindow.serverid)
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Visibility = Visibility.Visible;
                Topmost = true;
                Topmost = false;
            }
        }

        void DelBackground()
        {
            this.SetResourceReference(BackgroundProperty, "BackgroundBrush");
        }

        bool isModsPluginsRefresh = true;
        private void TabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabCtrl.SelectedIndex == 2)
            {
                GetServerConfig();
            }
            else
            {
                config = null;
            }
            if (TabCtrl.SelectedIndex == 3)
            {
                if (isModsPluginsRefresh)
                {
                    isModsPluginsRefresh = false;
                    ReFreshPluginsAndMods();
                    return;
                }
            }
            else
            {
                isModsPluginsRefresh = true;
                if (IsLoaded)
                {
                    try
                    {
                        pluginslist.Items.Clear();
                        modslist.Items.Clear();
                    }
                    catch
                    {
                        try
                        {
                            modslist.Items.Clear();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        private void solveProblemBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "此功能还在测试中，且占用极高，请勿随意使用！\n分析报告将在服务器关闭后生成！若使用后还是无法解决问题，请尝试进Q群询问：1145888872", "警告", true, "取消");
            if (MessageDialog._dialogReturn == true)
            {
                MessageDialog._dialogReturn = false;
                TabCtrl.SelectedIndex = 1;
                solveProblemSystem = true;
                LaunchServer();
            }
        }

        private void kickPlayer_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "确定要踢出这个玩家吗？", "警告", true, "取消");
            if (MessageDialog._dialogReturn == true)
            {
                MessageDialog._dialogReturn = false;
                try
                {
                    SERVERCMD.StandardInput.WriteLine("kick " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
                }
                catch
                {
                    Growl.Error("操作失败！");
                }
            }
        }

        private void banPlayer_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "确定要封禁这个玩家吗？封禁后该玩家将永远无法进入服务器！\n（原版解封指令：pardon +玩家名字，若添加插件，请使用插件的解封指令）", "警告", true, "取消");
            if (MessageDialog._dialogReturn == true)
            {
                MessageDialog._dialogReturn = false;
                try
                {
                    SERVERCMD.StandardInput.WriteLine("ban " + serverPlayerList.SelectedItem.ToString().Substring(0, serverPlayerList.SelectedItem.ToString().IndexOf("[")));
                }
                catch
                {
                    Growl.Error("操作失败！");
                }
            }
        }
        #region 服务器输出
        /// <summary>
        /// ///////////这里是服务器输出
        /// </summary>
        private void LaunchServer()
        {
            try
            {
                ChangeControlsState();
                if (Rserverserver == "")
                {
                    StartServer(RserverJVM + " " + RserverJVMcmd + " nogui");
                }
                else
                {
                    StartServer(RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverbase + @"\" + Rserverserver + "\" nogui");
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
                SERVERCMD.StartInfo.FileName = Rserverjava;
                SERVERCMD.StartInfo.Arguments = StartFileArg;
                Directory.SetCurrentDirectory(Rserverbase);
                SERVERCMD.StartInfo.CreateNoWindow = true;
                SERVERCMD.StartInfo.UseShellExecute = false;
                SERVERCMD.StartInfo.RedirectStandardInput = true;
                SERVERCMD.StartInfo.RedirectStandardOutput = true;
                SERVERCMD.StartInfo.RedirectStandardError = true;
                SERVERCMD.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                SERVERCMD.ErrorDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                SERVERCMD.Start();
                SERVERCMD.BeginOutputReadLine();
                SERVERCMD.BeginErrorReadLine();
                timer1.Interval = TimeSpan.FromSeconds(1);
                timer1.Start();
            }
            catch (Exception e)
            {
                timer1.Stop();
                ShowLog("出现错误，正在检查问题...", Brushes.Red);
                string a = Rserverjava;
                if (File.Exists(a))
                {
                    ShowLog("Java路径正常", Brushes.Green);
                }
                else
                {
                    ShowLog("Java路径有误", Brushes.Red);
                }
                string b = Rserverserver;
                if (File.Exists(b))
                {
                    ShowLog("服务端路径正常", Brushes.Green);
                }
                else
                {
                    ShowLog("服务端路径有误", Brushes.Red);
                }
                if (Directory.Exists(Rserverbase))
                {
                    ShowLog("服务器目录正常", Brushes.Green);
                }
                else
                {
                    ShowLog("服务器目录有误", Brushes.Red);
                }
                DialogShow.ShowMsg(this, "出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！\n错误代码:" + e.Message, "错误", false, "确定");
                TabCtrl.SelectedIndex = 1;
                ChangeControlsState(false);
            }
        }
        void ChangeControlsState(bool isEnable = true)
        {
            if (isEnable)
            {
                ServerList.RunningServerIDs = ServerList.RunningServerIDs + RserverId + " ";
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
                ShowLog("正在开启服务器，请稍等...", Brushes.Green);
            }
            else
            {
                ServerList.RunningServerIDs = ServerList.RunningServerIDs.Replace(RserverId + " ", "");
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
                    SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.CancelOutputRead();
                    SERVERCMD.CancelErrorRead();
                }
                catch
                { return; }
            }
        }//enable or disable some controls
        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }
        Brush tempbrush = Brushes.Green;
        void ShowLog(string msg, Brush color)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate ()
            {
                Paragraph p = new Paragraph(new Run(string.Format("{1}", DateTime.Now, msg)));
                if (color == Brushes.White)
                {
                    p.Foreground = tempbrush;
                    outlog.Document.Blocks.Add(p);
                    if (outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight)
                    {
                        outlog.ScrollToEnd();
                    }
                    return;
                }
                p.Foreground = color;
                tempbrush = color;
                outlog.Document.Blocks.Add(p);
                if (outlog.VerticalOffset + outlog.ViewportHeight >= outlog.ExtentHeight)
                {
                    outlog.ScrollToEnd();
                }
            });
        }
        private delegate void AddMessageHandler(string msg);
        private void ReadStdOutputAction(string msg)
        {
            if (solveProblemSystem)
            {
                ShowLog(msg, Brushes.Blue);
                if (getServerInfoLine <= 50)
                {
                    getServerInfoLine++;
                    if (msg.IndexOf("UnsupportedClassVersionError") != -1)
                    {
                        foundProblems = foundProblems + "*不支持的Class版本：您的Java版本可能太低！\n";
                        int a=int.Parse(msg.Substring(msg.IndexOf("(class file version ") + 20, 2));
                        switch(a)
                        {
                            case 52:
                                foundProblems += "请使用Java8或以上版本！\n";
                                break;
                            case 53:
                                foundProblems += "请使用Java9或以上版本！\n";
                                break;
                            case 54:
                                foundProblems += "请使用Java10或以上版本！\n";
                                break;
                            case 55:
                                foundProblems += "请使用Java11或以上版本！\n";
                                break;
                            case 56:
                                foundProblems += "请使用Java12或以上版本！\n";
                                break;
                            case 57:
                                foundProblems += "请使用Java13或以上版本！\n";
                                break;
                            case 58:
                                foundProblems += "请使用Java14或以上版本！\n";
                                break;
                            case 59:
                                foundProblems += "请使用Java15或以上版本！\n";
                                break;
                            case 60:
                                foundProblems += "请使用Java16或以上版本！\n";
                                break;
                            case 61:
                                foundProblems += "请使用Java17或以上版本！\n";
                                break;
                            case 62:
                                foundProblems += "请使用Java18或以上版本！\n";
                                break;
                            case 63:
                                foundProblems += "请使用Java19或以上版本！\n";
                                break;
                            case 64:
                                foundProblems += "请使用Java20或以上版本！\n";
                                break;
                        }
                    }
                    else if (msg.IndexOf("Unsupported Java detected") != -1)
                    {
                        foundProblems += "*不匹配的Java版本：\n";
                        foundProblems += "请使用"+ msg.Substring(msg.IndexOf("Only up to ") + 11, 7) +"！\n";
                    }
                    else if (msg.IndexOf("requires running the server with") != -1)
                    {
                        foundProblems = foundProblems + "*不匹配的Java版本：\n";
                        foundProblems = foundProblems + "请使用" + msg.Substring(msg.IndexOf("Java"), 7) + "！\n";
                    }
                    else if (msg.IndexOf("OutOfMemoryError") != -1)
                    {
                        foundProblems = foundProblems + "*服务器内存分配过低或过高！\n";
                    }
                    else if (msg.IndexOf("Invalid maximum heap size") != -1)
                    {
                        foundProblems = foundProblems + "*服务器最大内存分配有误！\n"+msg+"\n";
                    }
                    else if (msg.IndexOf("Unrecognized VM option") != -1)
                    {
                        foundProblems = foundProblems + "*服务器JVM参数有误！请前往设置界面进行查看！\n错误的参数为：" + msg.Substring(msg.IndexOf("'")+1,msg.Length-3- msg.IndexOf(" '")) + "\n";
                    }
                    else if (msg.IndexOf("There is insufficient memory for the Java Runtime Environment to continue") != -1)
                    {
                        foundProblems = foundProblems + "*JVM内存分配不足，请尝试增加系统的虚拟内存（不是内存条！具体方法请自行上网查找）！\n";
                    }
                    else if (msg.IndexOf("进程无法访问") != -1)
                    {
                        foundProblems = foundProblems + "*文件被占用，您的服务器可能多开，可尝试重启电脑解决！\n";
                    }
                    else if (msg.IndexOf("FAILED TO BIND TO PORT")  != -1)
                    {
                        foundProblems = foundProblems + "*端口被占用，您的服务器可能多开，可尝试重启电脑解决！\n";
                    }
                    else if (msg.IndexOf("Unable to access jarfile") != -1)
                    {
                        foundProblems = foundProblems + "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！\n";
                    }
                    else if (msg.IndexOf("加载 Java 代理时出错") != -1)
                    {
                        foundProblems = foundProblems + "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！\n";
                    }
                    else if(msg.IndexOf("ArraylndexOutOfBoundsException") != -1)
                    {
                        foundProblems = foundProblems + "*开启服务器时发生数组越界错误，请尝试更换服务端再试！\n";
                    }
                    else if (msg.IndexOf("ClassCastException") != -1)
                    {
                        foundProblems = foundProblems + "*开启服务器时发生类转换异常，请检查Java版本是否匹配，或者让开服器为您下载Java环境（设置界面更改）！\n";
                    }
                    else if (msg.IndexOf("could not open") != -1&& msg.IndexOf("jvm.cfg") != -1)
                    {
                        foundProblems = foundProblems + "*Java异常，请检查Java环境是否正常，或者让开服器为您下载Java环境（设置界面更改）！\n";
                    }
                }
                if (msg.IndexOf("Could not load") !=-1 && msg.IndexOf("plugin") != -1)
                {
                    foundProblems = foundProblems + "*无法加载插件！\n";
                    foundProblems = foundProblems + "插件名称："+msg.Substring(msg.IndexOf("Could not load '")+16,msg.IndexOf("' ")- (msg.IndexOf("Could not load '") + 16))+"\n";
                }
                else if (msg.IndexOf("Error loading plugin") != -1)
                {
                    foundProblems = foundProblems + "*无法加载插件！\n";
                    foundProblems = foundProblems + "插件名称：" + msg.Substring(msg.IndexOf(" '") + 2, msg.IndexOf("' ") - (msg.IndexOf(" '") + 2)) + "\n";
                }
                else if (msg.IndexOf("Error occurred while enabling ") != -1)
                {
                    foundProblems = foundProblems + "*在启用 "+msg.Substring(msg.IndexOf("enabling ") + 9, msg.IndexOf(" (") - (msg.IndexOf("enabling ") + 9)) + " 时发生了错误\n"; ;
                }
                else if (msg.IndexOf("Encountered an unexpected exception") != -1)
                {
                    foundProblems = foundProblems + "*服务器出现意外崩溃，可能是由于模组冲突，请检查您的模组列表（如果使用的是整合包，请使用整合包制作方提供的Server专用包开服）\n";
                }
            }
            else
            {
                if (outlog.Document.Blocks.Count >= 1000 && DeveloperSettings.developerMode != true)
                {
                    outlog.Document.Blocks.Clear();
                }
                if (ShieldLog != null && msg.IndexOf(ShieldLog) + 1 != 0)
                {
                    return;
                }
                if (closeOutlog.Content.ToString() != "日志输出:开")
                {
                    return;
                }
                if (getServerInfoLine <= 50)
                {
                    getServerInfoLine++;
                    if (getServerInfoLine == 50)
                    {
                        if (serverVersionLab.Content.ToString() == "获取中")
                        {
                            DialogShow.ShowMsg(this, "开服器未能获取到服务器信息，可能该服务端需要下载依赖文件，请耐心等待！\n即将为您跳转至输出界面，开服过程中部分问题可能需要您手动处理！\n\n(注：Mohist端接受EULA条款的方式是在控制台输入true，在接受前请务必前往该网站仔细阅读条款内容：https://account.mojang.com/documents/minecraft_eula)", "提示");
                            TabCtrl.SelectedIndex = 1;
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
                }
                if (msg.IndexOf("INFO") + 1 != 0)
                {
                    ShowLog("[" + DateTime.Now.ToString("T") + " 信息]" + msg.Substring(msg.IndexOf("INFO") + 5), Brushes.Green);
                    if (getServerInfoLine < 50)
                    {
                        if (msg.IndexOf("You need to agree to the EULA in order to run the server") + 1 != 0)
                        {
                            getServerInfoLine = -10;
                            DialogShow.ShowMsg(this, "检测到您没有接受Mojang的EULA条款！是否阅读并接受EULA条款并继续开服？", "提示", true, "取消");
                            if (MessageDialog._dialogReturn == true)
                            {
                                MessageDialog._dialogReturn = false;
                                try
                                {
                                    timer1.Stop();
                                    string path1 = Rserverbase + @"\eula.txt";
                                    FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                    StreamReader sr = new StreamReader(fs, Encoding.Default);
                                    string line;
                                    line = sr.ReadToEnd();
                                    line = line.Replace("eula=false", "eula=true");
                                    string path = Rserverbase + @"\eula.txt";
                                    StreamWriter streamWriter = new StreamWriter(path);
                                    streamWriter.WriteLine(line);
                                    streamWriter.Flush();
                                    streamWriter.Close();
                                    if (!SERVERCMD.HasExited)
                                    {
                                        SERVERCMD.Kill();
                                    }
                                    SERVERCMD.CancelOutputRead();
                                    SERVERCMD.CancelErrorRead();
                                    SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                                    SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                                    outlog.Document.Blocks.Clear();
                                    ShowLog("正在重启服务器...", Brushes.Green);
                                    LaunchServer();
                                }
                                catch (Exception a)
                                {
                                    MessageBox.Show("出现错误，请手动修改eula文件或重试:" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                Process.Start("https://account.mojang.com/documents/minecraft_eula");
                            }

                        }
                        else if (msg.IndexOf("Starting minecraft server version") + 1 != 0)
                        {
                            serverVersionLab.Content = msg.Substring(msg.LastIndexOf(" ") + 1);
                        }
                        else if (msg.IndexOf("Default game type:") + 1 != 0)
                        {
                            gameTypeLab.Content = msg.Substring(msg.LastIndexOf(" ") + 1);
                        }
                        else if (msg.IndexOf("Starting Minecraft server on") + 1 != 0)
                        {
                            serverIPLab.Content = msg.Substring(msg.LastIndexOf(" ") + 1);
                            if (serverIPLab.Content.ToString().IndexOf("*") + 1 != 0)
                            {
                                localServerIPLab.Content = serverIPLab.Content.ToString().Replace("*", "127.0.0.1");
                                if (localServerIPLab.Content.ToString().IndexOf(":25565") + 1 != 0)
                                {
                                    localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                                }
                            }
                            else
                            {
                                localServerIPLab.Content = serverIPLab.Content;
                                if (localServerIPLab.Content.ToString().IndexOf(":25565") + 1 != 0)
                                {
                                    localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                                }
                            }
                        }
                        else if (msg.IndexOf("正在启动") + 1 != 0)
                        {
                            if (msg.IndexOf("的Minecraft服务端") + 1 != 0)
                            {
                                serverVersionLab.Content = msg.Substring(msg.LastIndexOf("正在启动") + 4, (msg.IndexOf("的") / 2) - 7);
                            }
                        }
                        else if (msg.IndexOf("默认游戏模式:") + 1 != 0)
                        {
                            gameTypeLab.Content = msg.Substring(msg.LastIndexOf("游戏模式:") + 5);
                        }
                        else if (msg.IndexOf("正在") + 1 != 0)
                        {
                            if (msg.IndexOf("上启动服务器") + 1 != 0)
                            {
                                try
                                {
                                    serverIPLab.Content = msg.Substring(msg.IndexOf("正在 ") + 3, (msg.IndexOf("上") / 2) - 4);
                                    if (serverIPLab.Content.ToString().IndexOf("*") + 1 != 0)
                                    {
                                        localServerIPLab.Content = serverIPLab.Content.ToString().Replace("*", "127.0.0.1");
                                        if (localServerIPLab.Content.ToString().IndexOf(":25565") + 1 != 0)
                                        {
                                            localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                                        }
                                    }
                                    else
                                    {
                                        localServerIPLab.Content = serverIPLab.Content;
                                        if (localServerIPLab.Content.ToString().IndexOf(":25565") + 1 != 0)
                                        {
                                            localServerIPLab.Content = localServerIPLab.Content.ToString().Replace(":25565", "");
                                        }
                                    }
                                }
                                catch (Exception ex) { MessageBox.Show(ex.Message); }
                            }
                        }
                    }

                    //服务器启动成功和关闭时的提示
                    if (msg.IndexOf("Done") + 1 != 0)
                    {
                        if (msg.IndexOf("For help") + 1 != 0)
                        {
                            ShowLog("已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情参照开服器的内网映射界面。", Brushes.Green);
                            Growl.Success("已成功开启服务器！");
                            serverStateLab.Content = "运行中(已开服)";
                            Thread thread = new Thread(CheckOnlineMode);
                            thread.Start();
                        }
                    }
                    else if (msg.IndexOf("Stopping server") + 1 != 0)
                    {
                        ShowLog("正在关闭服务器！", Brushes.Green);
                    }
                    else if (msg.IndexOf("加载完成") + 1 != 0)
                    {
                        if (msg.IndexOf("如需帮助") + 1 != 0)
                        {
                            ShowLog("已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情参照开服器的内网映射界面。", Brushes.Green);
                            Growl.Success("已成功开启服务器！");
                            serverStateLab.Content = "运行中(已开服)";
                            Thread thread = new Thread(CheckOnlineMode);
                            thread.Start();
                        }
                    }

                    //玩家进服是否记录
                    if (getPlayerInfo == true)
                    {
                        if (msg.IndexOf("logged in with entity id") + 1 != 0)
                        {
                            string a = msg.Substring(0, msg.IndexOf(" logged in with entity id"));
                            while (a.IndexOf(" ") + 1 != 0)
                            {
                                a = a.Substring(a.IndexOf(" ") + 1);
                            }
                            serverPlayerList.Items.Add(a);
                        }
                        else if (msg.IndexOf("lost connection:") + 1 != 0)
                        {
                            try
                            {
                                string a = msg.Substring(0, msg.IndexOf(" lost connection:"));
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
                        else if (msg.IndexOf("与服务器失去连接:") + 1 != 0)
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
                }
                else if (msg.IndexOf("WARN") + 1 != 0)
                {
                    ShowLog("[" + DateTime.Now.ToString("T") + " 警告]" + msg.Substring(msg.IndexOf("WARN") + 5), Brushes.Orange);
                    if (msg.IndexOf("FAILED TO BIND TO PORT") + 1 != 0)
                    {
                        ShowLog("警告：由于端口占用，服务器已自动关闭！请检查您的服务器是否多开或者有其他软件占用端口！\r\n解决方法：您可尝试通过重启电脑解决！", Brushes.Red);
                    }
                    else if (msg.IndexOf("Unable to access jarfile") + 1 != 0)
                    {
                        ShowLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
                    }
                    else if (msg.IndexOf("加载 Java 代理时出错") + 1 != 0)
                    {
                        ShowLog("警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
                    }
                }
                else if (msg.IndexOf("ERROR") + 1 != 0)
                {
                    ShowLog("[" + DateTime.Now.ToString("T") + " 错误]" + msg.Substring(msg.IndexOf("ERROR") + 6), Brushes.Red);
                }
                else
                {
                    ShowLog(msg, Brushes.White);
                }
            }
        }
        void CheckOnlineMode()
        {
            for(int i = 0; i < 5; i++)
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
                        this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                        {
                            ShowLog("检测到您没有关闭正版验证，如果客户端为离线登录的话，请点击“更多功能”里“关闭正版验证”按钮以关闭正版验证。否则离线账户将无法进入服务器！", Brushes.OrangeRed);
                            Growl.Info("检测到您没有关闭正版验证，您可前往“更多功能”界面点击“关闭正版验证”按钮以关闭。否则离线账户无法进入服务器！");
                            onlineModeLab.Content = "已开启";
                        });
                        break;
                    }
                    else if (text.IndexOf("online-mode=false") + 1 != 0)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                        { onlineModeLab.Content = "已关闭"; }); break;
                    }
                }
                catch
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                    { onlineModeLab.Content = "未知"; }); break;
                }
            }
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            { if(onlineModeLab.Content.ToString()=="获取中") onlineModeLab.Content = "未知"; });
        }
        void timer1_Tick(object sender, EventArgs e)
        {
            if (SERVERCMD.HasExited == true)
            {
                timer1.Stop();
                ChangeControlsState(false);
                if (solveProblemSystem)
                {
                    solveProblemSystem = false;
                    if (foundProblems == null)
                    {
                        Growl.Info("服务器已关闭！开服器未检测到相关问题，请加Q群寻求帮助：1145888872！");
                    }
                    else
                    {
                        Growl.Info("服务器已关闭！即将为您展示分析报告！");
                        DialogShow.ShowMsg(this, foundProblems, "服务器分析报告");
                        foundProblems = null;
                    }
                }
                else if (autoserver == true)
                {
                    LaunchServer();
                }
                else
                {
                    if (getServerInfoLine <= 50 && getServerInfoLine >= 0)
                    {
                        DialogShow.ShowMsg(this, "您的服务器疑似异常关闭，请尝试使用智能分析功能进行检查，若无法解决，请加群:1145888872 并发送服务器输出日志来寻求帮助！", "提示");
                    }
                }
            }
        }


        string lastCommand;
        string nextCommand;
        private void sendcmd_Click(object sender, RoutedEventArgs e)
        {
            lastCommand = cmdtext.Text;
            try
            {
                if (fastCMD.SelectedIndex == 0)
                {
                    SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().IndexOf("/op（给管理员）") != -1)
                {
                    SERVERCMD.StandardInput.WriteLine("op " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().IndexOf("/deop（去除管理员）") != -1)
                {
                    SERVERCMD.StandardInput.WriteLine("deop " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().IndexOf("/ban（封禁玩家）") != -1)
                {
                    SERVERCMD.StandardInput.WriteLine("ban " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().IndexOf("/say（全服说话）") != -1)
                {
                    SERVERCMD.StandardInput.WriteLine("say " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().IndexOf("/pardon（解除封禁）") != -1)
                {
                    SERVERCMD.StandardInput.WriteLine("pardon " + cmdtext.Text);
                    cmdtext.Text = "";
                }
            }
            catch
            {
                fastCMD.SelectedIndex = 0;
                SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                cmdtext.Text = "";
            }
        }

        //tab补全命令列表
        List<string> tabCompleteList = new List<string>();
        private void cmdtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                lastCommand = cmdtext.Text;
                try
                {
                    if (fastCMD.SelectedIndex == 0)
                    {
                        SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().IndexOf("/op（给管理员）") != -1)
                    {
                        SERVERCMD.StandardInput.WriteLine("op " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().IndexOf("/deop（去除管理员）") != -1)
                    {
                        SERVERCMD.StandardInput.WriteLine("deop " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().IndexOf("/ban（封禁玩家）") != -1)
                    {
                        SERVERCMD.StandardInput.WriteLine("ban " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().IndexOf("/say（全服说话）") != -1)
                    {
                        SERVERCMD.StandardInput.WriteLine("say " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().IndexOf("/pardon（解除封禁）") != -1)
                    {
                        SERVERCMD.StandardInput.WriteLine("pardon " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                }
                catch
                {
                    fastCMD.SelectedIndex = 0;
                    SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                    cmdtext.Text = "";
                }
            }
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (fastCMD.SelectedIndex == 0)
                {
                    try
                    {
                        tabCompleteList.Clear();
                        string text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\TabComplete.txt");
                        while (text != "")
                        {
                            string a = text.Substring(0, text.IndexOf("\r\n"));
                            tabCompleteList.Add(a);
                            text = text.Replace(a + "\r\n", "");
                        }
                        if (cmdtext.Text.IndexOf(" ") + 1 != 0)
                        {
                            bool isReplace = true;
                            string _a = cmdtext.Text;
                            string a = "";
                            string d = "";
                            if (!cmdtext.Text.EndsWith(" "))
                            {
                                a = cmdtext.Text.Substring(0, cmdtext.Text.LastIndexOf(" ") + 1);
                            }
                            foreach (string n in tabCompleteList)
                            {
                                if (n.IndexOf(_a) == 0)
                                {
                                    string b = n.Replace(_a, "");
                                    string c = "";
                                    if (a != "")
                                    {
                                        c = n.Replace(a, "");
                                    }
                                    if (b.IndexOf(" ") + 1 != 0)
                                    {
                                        if (isReplace)
                                        {
                                            cmdtext.Text = cmdtext.Text + b.Substring(0, b.IndexOf(" "));
                                        }
                                        if (c != "")
                                        {
                                            if (d.IndexOf(c.Substring(0, c.IndexOf(" ")) + ", ") + 1 == 0)
                                            {
                                                d += c.Substring(0, c.IndexOf(" ")) + ", ";
                                            }
                                        }
                                        else
                                        {
                                            if (d.IndexOf(b.Substring(0, b.IndexOf(" ")) + ", ") + 1 == 0)
                                            {
                                                d += b.Substring(0, b.IndexOf(" ")) + ", ";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isReplace)
                                        {
                                            cmdtext.Text = cmdtext.Text + b;
                                        }
                                        if (c != "")
                                        {
                                            if (d.IndexOf(c + ", ") + 1 == 0)
                                            {
                                                d += c + ", ";
                                            }
                                        }
                                        else
                                        {
                                            if (d.IndexOf(b + ", ") + 1 == 0)
                                            {
                                                d += b + ", ";
                                            }
                                        }
                                    }
                                    isReplace = false;
                                }
                            }
                            if (d != "")
                            {
                                ShowLog(d.Substring(0, d.Length - 2), Brushes.Green);
                            }
                        }
                        else
                        {
                            int a = cmdtext.Text.Length;
                            bool endWhile = false;
                            string c = "";
                            while (a != 0)
                            {
                                string b = cmdtext.Text.Substring(0, a);
                                foreach (string n in tabCompleteList)
                                {
                                    if (n.IndexOf(b) == 0)
                                    {
                                        if (n.IndexOf(" ") + 1 != 0)
                                        {
                                            if (!endWhile)
                                            {
                                                cmdtext.Text = n.Substring(0, n.IndexOf(" "));
                                            }
                                            if (c.IndexOf(n.Substring(0, n.IndexOf(" ")) + ", ") + 1 == 0)
                                            {
                                                c += n.Substring(0, n.IndexOf(" ")) + ", ";
                                            }
                                        }
                                        else
                                        {
                                            if (!endWhile)
                                            {
                                                cmdtext.Text = n;
                                            }
                                            if (c.IndexOf(n + ", ") + 1 == 0)
                                            {
                                                c += n + ", ";
                                            }
                                        }
                                        endWhile = true;
                                        //break;
                                    }
                                }
                                if (endWhile)
                                {
                                    break;
                                }
                                else
                                {
                                    a--;
                                }
                            }
                            if (c != "")
                            {
                                ShowLog(c.Substring(0, c.Length - 2), Brushes.Green);
                            }
                        }
                        cmdtext.SelectionStart = cmdtext.Text.Length;
                    }
                    catch
                    {
                        Growl.Error("命令补全失败！");
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
            }
            if (e.Key == Key.Down)
            {
                if (cmdtext.Text != nextCommand)
                {
                    lastCommand = cmdtext.Text;
                    cmdtext.Text = nextCommand;
                }
            }
            cmdtext.Select(cmdtext.Text.Length, 0);
        }
        private void controlServer_Click(object sender, RoutedEventArgs e)
        {
            if (controlServer.Content.ToString() == "开服")
            {
                LaunchServer();
                try
                {
                    WebClient MyWebClient = new WebClient();
                    MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/commands.txt");
                    string pageHtml = Encoding.UTF8.GetString(pageData);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\TabComplete.txt", pageHtml);
                }
                catch
                {
                    Growl.Error("获取Tab补全信息失败！");
                }
            }
            else
            {
                Growl.Info("关服中……\n(双击按钮可强制关服)");
                SERVERCMD.StandardInput.WriteLine("stop");
            }
        }

        private void controlServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (controlServer.Content.ToString() == "关服")
                {
                    SERVERCMD.Kill();
                }
            }
            catch { }
        }
        private void gotoFrpc_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "服务器开启后，通常远程的小伙伴是无法进入的，你需要开通公网IP或进行内网映射才可让他人进入。开服器内置有免费的内网映射，您可点击主界面左侧的“内网映射”按钮查看详情并进行配置。", "注意");
            GotoFrpcEvent();
        }
        private void systemInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (systemInfoBtn.Content.ToString() == "显示占用:关")
            {
                Growl.Info("加载相关信息中，请稍等……");
                getServerInfo = true;
                //outlog.Margin = new Thickness(10, 10, 125, 47);
                systemInfoBtn.Content = "显示占用:开";
                Thread thread = new Thread(GetSystemInfo);
                thread.Start();
            }
            else
            {
                DialogShow.ShowMsg(this, "关闭此功能后，输出预览功能也将同时关闭！", "注意");
                //outlog.Margin = new Thickness(10, 10, 10, 47);
                getServerInfo = false;
                systemInfoBtn.Content = "显示占用:关";
                previewOutlog.Text = "预览功能已关闭，请前往服务器输出界面查看日志信息！";
            }
        }

        void GetSystemInfo()
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
                            this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate ()
                            {
                                cpuInfoLab.Content = "CPU:" + cpuUsage.ToString("f2") + "%";
                                cpuInfoBar.Value = (int)cpuUsage;
                            });
                        }
                    }
                    catch
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate ()
                        {
                            cpuInfoLab.Content = "无法获取CPU信息";
                        });
                    }
                    var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

                    float ramAvailable = ramCounter.NextValue() / 1024;
                    double allMemory = MainWindow.PhisicalMemory / 1024.0 / 1024.0 / 1024.0;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate ()
                    {
                        memoryInfoLab.Content = "总内存:" + allMemory.ToString("f2") + "G\n" + "已使用:" + (allMemory - ramAvailable).ToString("f2") + "G\n" + "可使用:" + ramAvailable.ToString("f2") + "G";
                        memoryInfoBar.Value = (allMemory - ramAvailable) / allMemory * 100;
                        availableMemoryInfoBar.Value = ramAvailable / allMemory * 100;

                        if (outlog.Document.Blocks.LastBlock != null)
                        {
                            TextRange textRange = new TextRange(outlog.Document.Blocks.LastBlock.ContentStart, outlog.Document.Blocks.LastBlock.ContentEnd);
                            previewOutlog.Text = textRange.Text;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Growl.Error("出现错误！" + ex.Message);
                    this.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate ()
                    {
                        previewOutlog.Text = "预览功能已关闭，请前往服务器输出界面查看日志信息！";
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
                //outlog.Margin = new Thickness(10, 10, 125, 47);
                playerInfoBtn.Content = "记录玩家:开";
            }
            else
            {
                Growl.Success("已关闭");
                //outlog.Margin = new Thickness(10, 10, 10, 47);
                getPlayerInfo = false;
                playerInfoBtn.Content = "记录玩家:关";
            }
        }
        #endregion

        #region 服务器功能调整
        /// <summary>
        /// /////////这里是服务器功能调整
        /// </summary>
        string config;
        void GetServerConfig()
        {
            try
            {
                config = File.ReadAllText(Rserverbase + @"\server.properties");
                int om1 = config.IndexOf("\r\nonline-mode=") + 14;
                string om2 = config.Substring(om1);
                string om3 = om2.Substring(0, om2.IndexOf("\r\n"));
                config = config.Replace("online-mode=" + om3, "online-mode=");
                onlineModeText.Text = om3;
                int gm1 = config.IndexOf("\r\ngamemode=") + 11;
                string gm2 = config.Substring(gm1);
                string gm3 = gm2.Substring(0, gm2.IndexOf("\r\n"));
                config = config.Replace("gamemode=" + gm3, "gamemode=");
                gameModeText.Text = gm3;
                int dc1 = config.IndexOf("\r\ndifficulty=") + 13;
                string dc2 = config.Substring(dc1);
                string dc3 = dc2.Substring(0, dc2.IndexOf("\r\n"));
                config = config.Replace("difficulty=" + dc3, "difficulty=");
                gameDifficultyText.Text = dc3;
                int mp1 = config.IndexOf("\r\nmax-players=") + 14;
                string mp2 = config.Substring(mp1);
                string mp3 = mp2.Substring(0, mp2.IndexOf("\r\n"));
                config = config.Replace("max-players=" + mp3, "max-players=");
                gamePlayerText.Text = mp3;
                int sp1 = config.IndexOf("\r\nserver-port=") + 14;
                string sp2 = config.Substring(sp1);
                string sp3 = sp2.Substring(0, sp2.IndexOf("\r\n"));
                config = config.Replace("server-port=" + sp3, "server-port=");
                gamePortText.Text = sp3;
                int ec1 = config.IndexOf("\r\nenable-command-block=") + 23;
                string ec2 = config.Substring(ec1);
                string ec3 = ec2.Substring(0, ec2.IndexOf("\r\n"));
                config = config.Replace("enable-command-block=" + ec3, "enable-command-block=");
                commandBlockText.Text = ec3;
                int vd1 = config.IndexOf("\r\nview-distance=") + 16;
                string vd2 = config.Substring(vd1);
                string vd3 = vd2.Substring(0, vd2.IndexOf("\r\n"));
                config = config.Replace("view-distance=" + vd3, "view-distance=");
                viewDistanceText.Text = vd3;
                int pp1 = config.IndexOf("\r\npvp=") + 6;
                string pp2 = config.Substring(pp1);
                string pp3 = pp2.Substring(0, pp2.IndexOf("\r\n"));
                config = config.Replace("pvp=" + pp3, "pvp=");
                gamePvpText.Text = pp3;
                int gw1 = config.IndexOf("\r\nlevel-name=") + 13;
                string gw2 = config.Substring(gw1);
                string gw3 = gw2.Substring(0, gw2.IndexOf("\r\n"));
                config = config.Replace("level-name=" + gw3, "level-name=");
                gameWorldText.Text = gw3;
                savaServerConfig.IsEnabled = true;
            }
            catch (Exception ex) { DialogShow.ShowMsg(this,"出现错误，您的服务器可能还未启动过，请开启一次服务器再试！\n" + ex.Message, "错误"); savaServerConfig.IsEnabled=false; }
        }
        private void saveServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    DialogShow.ShowMsg(this, "您没有关闭服务器，无法调整服务器功能！", "错误", false, "确定");
                    return;
                }
            }
            catch
            { }
            try
            {
                config = config.Replace("online-mode=", "online-mode=" + onlineModeText.Text);
                config = config.Replace("gamemode=", "gamemode=" + gameModeText.Text);
                config = config.Replace("difficulty=", "difficulty=" + gameDifficultyText.Text);
                config = config.Replace("max-players=", "max-players=" + gamePlayerText.Text);
                config = config.Replace("server-port=", "server-port=" + gamePortText.Text);
                config = config.Replace("enable-command-block=", "enable-command-block=" + commandBlockText.Text);
                config = config.Replace("view-distance=", "view-distance=" + viewDistanceText.Text);
                config = config.Replace("pvp=", "pvp=" + gamePvpText.Text);
                config = config.Replace("level-name=", "level-name=" + gameWorldText.Text);
                File.WriteAllText(Rserverbase + @"\server.properties", config);
                DialogShow.ShowMsg(this, "保存成功！", "信息");
                GetServerConfig();
            }
            catch { }
        }
        private void changeServerIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    DialogShow.ShowMsg(this, "您没有关闭服务器，无法更换图标！", "错误");
                    return;
                }
            }
            catch
            { }
            if (File.Exists(Rserverbase + "\\server-icon.png"))
            {
                DialogShow.ShowMsg(this, "此功能会覆盖原来的旧图标，是否要继续？", "警告", true, "取消");
                if (!MessageDialog._dialogReturn)
                {
                    return;
                }
            }
            MessageDialog._dialogReturn = false;
            DialogShow.ShowMsg(this, "请先准备一张64*64像素的图片（格式为png），准备完成后点击确定以继续", "如何操作？");
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件";
            openfile.Filter = "PNG图像|*.png";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                File.Copy(openfile.FileName, Rserverbase + "\\server-icon.png",true);
                DialogShow.ShowMsg(this, "图标更换完成！", "信息");
            }
        }
        private void changeWorldMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    DialogShow.ShowMsg(this, "您没有关闭服务器，无法更换地图！", "错误");
                    return;
                }
            }
            catch
            { }
            DialogShow.ShowMsg(this, "此功能会删除原来的旧地图，是否要继续使用？", "警告", true, "取消");
            if (MessageDialog._dialogReturn == true)
            {
                MessageDialog._dialogReturn = false;
                if (Directory.Exists(Rserverbase + @"\" + gameWorldText.Text))
                {
                    DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + gameWorldText.Text);
                    di.Delete(true);
                    Directory.CreateDirectory(Rserverbase + @"\" + gameWorldText.Text);
                }
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "请选择地图文件夹(或解压后的文件夹)";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + gameWorldText.Text);
                    DialogShow.ShowMsg(this, "导入世界成功！", "信息", false, "确定");
                }
            }
        }
        #endregion

        #region 插件mod管理
        /// <summary>
        /// ///////////这里是插件mod管理
        /// </summary>
        void ReFreshPluginsAndMods()
        {
            pluginslist.Items.Clear();
            modslist.Items.Clear();
            if (Directory.Exists(Rserverbase + @"\plugins"))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\plugins");
                FileInfo[] file = directoryInfo.GetFiles("*.jar");
                foreach (FileInfo f in file)
                {
                    pluginslist.Items.Add(f.Name);
                }
                if (Directory.Exists(Rserverbase + @"\mods"))
                {
                    lab001.Content = "已检测到插件和模组的文件夹，以下为插件和模组列表";
                    lab001.Margin = new Thickness(0);
                    pluginListBox.Visibility = Visibility.Visible;
                    modListBox.Visibility = Visibility.Visible;
                    pluginListBox.Width = 410;
                    modListBox.Width = 410;
                    pluginListBox.Margin = new Thickness(10, 70, 0, 0);
                    modListBox.Margin = new Thickness(430, 70, 0, 0);
                    DirectoryInfo directoryInfo1 = new DirectoryInfo(Rserverbase + @"\mods");
                    FileInfo[] file1 = directoryInfo1.GetFiles("*.jar");
                    foreach (FileInfo f1 in file1)
                    {
                        modslist.Items.Add(f1.Name);
                    }
                }
                else
                {
                    lab001.Content = "已检测到插件文件夹，以下为插件列表";
                    lab001.Margin = new Thickness(0);
                    pluginListBox.Visibility = Visibility.Visible;
                    modListBox.Visibility = Visibility.Hidden;
                    pluginListBox.Width = 820;
                    pluginListBox.Margin = new Thickness(10, 70, 0, 0);
                }
            }
            else
            {
                if (Directory.Exists(Rserverbase + @"\mods"))
                {
                    lab001.Content = "已检测到模组文件夹，以下为模组列表";
                    lab001.Margin = new Thickness(0);
                    pluginListBox.Visibility = Visibility.Hidden;
                    modListBox.Visibility = Visibility.Visible;
                    modListBox.Width = 820;
                    modListBox.Margin = new Thickness(10, 70, 0, 0);
                    modslist.Items.Clear();
                    DirectoryInfo directoryInfo = new DirectoryInfo(Rserverbase + @"\mods");
                    FileInfo[] file = directoryInfo.GetFiles("*.jar");
                    foreach (FileInfo f in file)
                    {
                        modslist.Items.Add(f.Name);
                    }
                    openpluginsDir.IsEnabled = false;
                    addPlugin.IsEnabled = false;
                }
                else
                {
                    lab001.Content = "未检测到任何插件和模组，请重启服务器或检查该服务端是否支持";
                    lab001.Margin = new Thickness(182, 209,0,0);
                    pluginListBox.Visibility = Visibility.Hidden;
                    modListBox.Visibility = Visibility.Hidden;
                    openpluginsDir.IsEnabled = false;
                    openmodsDir.IsEnabled = false;
                    addPlugin.IsEnabled = false;
                    addMod.IsEnabled = false;
                    delPlugin.IsEnabled = false;
                    delMod.IsEnabled = false;
                }
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
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    File.Copy(openfile.FileName, Rserverbase + @"\plugins\" + openfile.SafeFileName);
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
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                try
                {
                    File.Copy(openfile.FileName, Rserverbase + @"\mods\" + openfile.SafeFileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                ReFreshPluginsAndMods();
            }
        }
        private void delPlugin_Click(object sender, RoutedEventArgs e)
        {
            if (pluginslist.SelectedIndex != -1)
            {
                File.Delete(Rserverbase + @"\plugins\" + pluginslist.SelectedItem.ToString());
                ReFreshPluginsAndMods();
            }
        }

        private void delMod_Click(object sender, RoutedEventArgs e)
        {
            if (modslist.SelectedIndex != -1)
            {
                File.Delete(Rserverbase + @"\mods\" + modslist.SelectedItem.ToString());
                ReFreshPluginsAndMods();
            }
        }

        private void opencurseforge_Click(object sender, RoutedEventArgs e)
        {
            DownloadMods downloadMods = new DownloadMods();
            downloadMods.serverbase= Rserverbase;
            downloadMods.Owner = this;
            downloadMods.ShowDialog();
            ReFreshPluginsAndMods();
        }
        private void openpluginweb_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "由于没有相关API，开服器无法提供一键下载功能，即将为您打开MCBBS和Spigot网站，请您自行寻找并下载","提示");
            Process.Start("https://www.mcbbs.net/forum-servermod-1.html");
            Process.Start("https://www.spigotmc.org/resources/");
        }
        #endregion

        #region 服务器设置
        /// <summary>
        /// //////////////////////这里是服务器设置界面
        /// </summary>
        void LoadSettings()
        {
            try
            {
                if (DeveloperSettings.developerMode == true)
                {
                    nAme.IsReadOnly = false;
                    bAse.IsReadOnly = false;
                }
                nAme.Text = Rservername;
                server.Text = Rserverserver;
                memorySlider.Maximum = MainWindow.PhisicalMemory / 1024.0 / 1024.0;
                bAse.Text = Rserverbase;
                jVMcmd.Text = RserverJVMcmd;
                jAva.Text = Rserverjava;
                if (jAva.Text == "Java")
                {
                    useJvpath.IsChecked = true;
                }
                if (jAva.Text == AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java8\bin\java.exe")
                {
                    useDownJv.IsChecked = true;
                    selectJava.SelectedIndex = 0;
                }
                if (jAva.Text == AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java16\bin\java.exe")
                {
                    useDownJv.IsChecked = true;
                    selectJava.SelectedIndex = 1;
                }
                if (jAva.Text == AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java17\bin\java.exe")
                {
                    useDownJv.IsChecked = true;
                    selectJava.SelectedIndex = 2;
                }
                if (jAva.Text == AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java18\bin\java.exe")
                {
                    useDownJv.IsChecked = true;
                    selectJava.SelectedIndex = 3;
                }
                if (RserverJVM == "")
                {
                    memorySlider.IsEnabled = false;
                    useJVMself.IsChecked = false;
                    useJVMauto.IsChecked = true;
                    memoryInfo.Text = "内存：自动分配";
                }
                else
                {
                    memorySlider.IsEnabled = true;
                    useJVMauto.IsChecked = false;
                    useJVMself.IsChecked = true;
                    try
                    {
                        int IndexofA6 = RserverJVM.IndexOf("-Xms");
                        string Ru6 = RserverJVM.Substring(IndexofA6 + 4);
                        string a600 = Ru6.Substring(0, Ru6.IndexOf("M"));
                        int IndexofA7 = RserverJVM.IndexOf("-Xmx");
                        string Ru7 = RserverJVM.Substring(IndexofA7 + 4);
                        string a700 = Ru7.Substring(0, Ru7.IndexOf("M"));

                        memorySlider.ValueStart = int.Parse(a600);
                        memorySlider.ValueEnd = int.Parse(a700);
                        memoryInfo.Text = "最小:" + a600 + "M," + "最大:" + a700 + "M";

                    }
                    catch
                    {
                        int IndexofA7 = RserverJVM.IndexOf("-Xmx");
                        string Ru7 = RserverJVM.Substring(IndexofA7 + 4);
                        string a700 = Ru7.Substring(0, Ru7.IndexOf("M"));

                        memorySlider.ValueStart = 0;
                        memorySlider.ValueEnd = int.Parse(a700);
                        memoryInfo.Text = "最小:0M," + "最大:" + a700 + "M";
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error!!!");
            }
        }
        private void refreahConfig_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }
        private void doneBtn1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    DialogShow.ShowMsg(this, "您没有关闭服务器，无法更改服务器设置！", "错误", false, "确定");
                    return;
                }
            }
            catch
            { }
            try
            {
                if (!Path.IsPathRooted(jAva.Text))
                {
                    jAva.Text = AppDomain.CurrentDomain.BaseDirectory + jAva.Text;
                }
                WebClient MyWebClient = new WebClient();
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/otherdownload.json");
                string _javaList = Encoding.UTF8.GetString(pageData);

                JObject javaList0 = JObject.Parse(_javaList);
                JObject javaList = (JObject)javaList0["java"];

                doneBtn1.IsEnabled = false;
                if (useDownJv.IsChecked == true)
                {
                    try
                    {
                        switch (selectJava.SelectedIndex)
                        {
                            case 0:
                                DownloadJava("Java8", javaList["Java8"].ToString());
                                return;
                            case 1:
                                DownloadJava("Java11", javaList["Java11"].ToString());
                                return;
                            case 2:
                                DownloadJava("Java16", javaList["Java16"].ToString());
                                return;
                            case 3:
                                DownloadJava("Java17", javaList["Java17"].ToString());
                                return;
                            case 4:
                                DownloadJava("Java18", javaList["Java18"].ToString());
                                return;
                            case 5:
                                DownloadJava("Java19", javaList["Java19"].ToString());
                                return;
                            default:
                                Growl.Error("请选择一个版本以下载！");
                                return;
                        }
                    }
                    catch
                    {
                        Growl.Error("出现错误，请检查网络连接！");
                        return;
                    }
                }
                else if (useSelf.IsChecked == true)
                {
                    if (useJVMauto.IsChecked == true)
                    {
                        RserverJVM = "";
                    }
                    else
                    {
                        RserverJVM = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                    }
                }
                else   // (useJvpath.IsChecked == true)
                {
                    if (useJVMauto.IsChecked == true)
                    {
                        RserverJVM = "";
                    }
                    else
                    {
                        RserverJVM = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                    }
                }
                //Directory.CreateDirectory(bAse.Text);
                doneBtn1.IsEnabled = true;
                Rservername = nAme.Text;
                Rserverserver = server.Text;
                Rserverbase = bAse.Text;
                RserverJVMcmd = jVMcmd.Text;
                Rserverjava = jAva.Text;
                
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverId];
                _json["name"].Replace(Rservername);
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                _json["core"].Replace(Rserverserver);
                _json["memory"].Replace(RserverJVM);
                _json["args"].Replace(RserverJVMcmd);
                jsonObject[RserverId].Replace(_json);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                LoadSettings();
                SaveConfigEvent();

                DialogShow.ShowMsg(this, "保存完毕！", "信息");
            }
            catch (Exception err)
            {
                MessageBox.Show("出现错误！请重试:\n" + err.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                doneBtn1.IsEnabled = true;
            }
        }

        private void DownloadJava(string fileName, string downUrl)
        {
            jAva.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe";
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
            {
                if (useJVMauto.IsChecked == true)
                {
                    RserverJVM = "";
                }
                else
                {
                    RserverJVM = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                }
                
                doneBtn1.IsEnabled = true;
                Rservername = nAme.Text;
                Rserverserver = server.Text;
                Rserverbase = bAse.Text;
                RserverJVMcmd = jVMcmd.Text;
                Rserverjava = jAva.Text;

                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[RserverId];
                _json["name"].Replace(Rservername);
                _json["java"].Replace(Rserverjava);
                _json["base"].Replace(Rserverbase);
                _json["core"].Replace(Rserverserver);
                _json["memory"].Replace(RserverJVM);
                _json["args"].Replace(RserverJVMcmd);
                jsonObject[RserverId].Replace(_json);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                LoadSettings();
                SaveConfigEvent();

                DialogShow.ShowMsg(this, "保存完毕！", "信息");
            }
            else
            {
                //MessageBox.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogShow.ShowMsg(this, "下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false, "确定");
                //MessageDialog messageDialog = new MessageDialog();
                //messageDialog.Owner = this;
                //messageDialog.ShowDialog();
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
                {
                    DownjavaName = fileName;
                    //DownloadWindow.downloadurl = RserverLink +@"/web/Java8.exe";
                    DialogShow.ShowDownload(this, downUrl, AppDomain.CurrentDomain.BaseDirectory + "MSL", "Java.zip", "下载" + fileName + "中……");
                    downout.Content = "解压中...";
                    try
                    {
                        string javaDirName = "";
                        using (ZipFile zip = new ZipFile(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip"))
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
                        fastZip.ExtractZip(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip", AppDomain.CurrentDomain.BaseDirectory + "MSL", "");
                        downout.Content = "解压完成，移动中...";
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip");
                        timer2.Interval = TimeSpan.FromSeconds(3);
                        timer2.Start();
                        if (AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName != AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName)
                        {
                            MoveFolder(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName, AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        downout.Content = "安装失败！";
                        doneBtn1.IsEnabled = true;
                    }
                }

            }
        }
        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    //采用递归的方法实现
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe"))
            {
                try
                {
                    if (useJVMauto.IsChecked == true)
                    {
                        RserverJVM = "";
                    }
                    else
                    {
                        RserverJVM = "-Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M";
                    }
                    downout.Content = "安装成功！";
                    doneBtn1.IsEnabled = true;
                    Rservername = nAme.Text;
                    Rserverserver = server.Text;
                    Rserverbase = bAse.Text;
                    RserverJVMcmd = jVMcmd.Text;
                    Rserverjava = jAva.Text;

                    JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                    JObject _json = (JObject)jsonObject[RserverId];
                    _json["name"].Replace(Rservername);
                    _json["java"].Replace(Rserverjava);
                    _json["base"].Replace(Rserverbase);
                    _json["core"].Replace(Rserverserver);
                    _json["memory"].Replace(RserverJVM);
                    _json["args"].Replace(RserverJVMcmd);
                    jsonObject[RserverId].Replace(_json);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                    LoadSettings();
                    SaveConfigEvent();

                    DialogShow.ShowMsg(this, "保存完毕！", "信息");
                    timer2.Stop();
                }
                catch
                {
                    return;
                }
            }
        }
        private void a01_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件，通常为*.jar";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                if (Path.GetDirectoryName(openfile.FileName) != Rserverbase)
                {
                    File.Copy(openfile.FileName, Rserverbase + @"\" + openfile.SafeFileName);
                    DialogShow.ShowMsg(this, "已将服务端文件移至服务器文件夹中！您可将源文件删除！", "提示", false, "确定");
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
            DownloadServer.downloadServerJava = Rserverjava;
            DownloadServer.downloadServerBase = Rserverbase;
            DownloadServer downloadServer = new DownloadServer();
            downloadServer.Owner = this;
            downloadServer.ShowDialog();
            if (File.Exists(Rserverbase + @"\" + DownloadServer.downloadServerName))
            {
                server.Text = DownloadServer.downloadServerName;
            }
            else if (DownloadServer.downloadServerArgs != "")
            {
                jVMcmd.Text = RserverJVMcmd;
            }
        }
        private void useJVMauto_Click(object sender, RoutedEventArgs e)
        {
            if (useJVMauto.IsChecked == true)
            {
                memorySlider.IsEnabled = false;
                memoryInfo.Text = "内存：自动分配";
                useJVMself.IsChecked = false;
            }
            else
            {
                useJVMauto.IsChecked = true;
            }
        }

        private void useJVMself_Click(object sender, RoutedEventArgs e)
        {
            if (useJVMself.IsChecked == true)
            {
                memorySlider.IsEnabled = true;
                memoryInfo.Text = "最小:" + memorySlider.ValueStart.ToString("f0") + "M," + "最大:" + memorySlider.ValueEnd.ToString("f0") + "M";
                useJVMauto.IsChecked = false;
            }
            else
            {
                useJVMself.IsChecked = true;
            }
        }
        private void memorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<HandyControl.Data.DoubleRange> e)
        {
            memoryInfo.Text = "最小:" + memorySlider.ValueStart.ToString("f0") + "M," + "最大:" + memorySlider.ValueEnd.ToString("f0") + "M";
        }
        private void memoryInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (useJVMself.IsChecked == true)
                {
                    if (memoryInfo.IsFocused == true)
                    {
                        try
                        {
                            string a = memoryInfo.Text.Substring(0, memoryInfo.Text.IndexOf(","));
                            string b = memoryInfo.Text.Substring(memoryInfo.Text.IndexOf(","));
                            string resultA = System.Text.RegularExpressions.Regex.Replace(a, @"[^0-9]+", "");
                            string resultB = System.Text.RegularExpressions.Regex.Replace(b, @"[^0-9]+", "");
                            memorySlider.ValueStart = double.Parse(resultA);
                            memorySlider.ValueEnd = double.Parse(resultB);
                        }
                        catch { }
                    }
                }
            }
        }
        private void setServerconfig_Click(object sender, RoutedEventArgs e)
        {
            SetServerconfig.serverbase = Rserverbase;
            Window window = new SetServerconfig();
            window.ShowDialog();
        }
        private void getLaunchercode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(Rserverbase + @"\StartServer.bat", "@ECHO OFF\n\"" + Rserverjava + "\" " + RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverbase + @"\" + Rserverserver + "\" nogui" + "\npause");
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
        /// <summary>
        /// ////////这里是更多功能界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autostartServer_Click(object sender, RoutedEventArgs e)
        {
            if (autoStartserver.Content.ToString() == "关服自动开服:禁用")
            {
                autoserver = true;
                autoStartserver.Content = "关服自动开服:启用";
            }
            else
            {
                autoserver = false;
                autoStartserver.Content = "关服自动开服:禁用";
            }
        }
        private void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    //MessageBox.Show("检测到服务器正在运行，正在关闭服务器");
                    DialogShow.ShowMsg(this, "检测到服务器正在运行，正在关闭服务器", "信息", false, "确定");
                    //MessageDialog messageDialog = new MessageDialog();
                    //messageDialog.Owner = this;
                    //messageDialog.ShowDialog();
                    SERVERCMD.StandardInput.WriteLine("stop");
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
                    DialogShow.ShowMsg(this, "修改完毕，请重新开启服务器！", "信息", false, "确定");
                    //MessageDialog messageDialog = new MessageDialog();
                    //messageDialog.Owner = this;
                    //messageDialog.ShowDialog();
                    //MessageBox.Show("修改完毕，请重新开启服务器！");

                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请手动修改server.properties文件或重试:" + a.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    DialogShow.ShowMsg(this, "修改完毕，请重新开启服务器！", "信息", false, "确定");
                    //MessageDialog messageDialog = new MessageDialog();
                    //messageDialog.Owner = this;
                    //messageDialog.ShowDialog();
                    //MessageBox.Show("修改完毕，请重新开启服务器！");
                }
                catch (Exception a)
                {
                    MessageBox.Show("出现错误，请手动修改server.properties文件或重试:" + a.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void closeOutlog_Click(object sender, RoutedEventArgs e)
        {
            if (closeOutlog.Content.ToString() == "日志输出:开")
            {
                closeOutlog.Content = "日志输出:关";
            }
            else
            {
                closeOutlog.Content = "日志输出:开";
            }

        }

        private void closeOutlog_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (closeOutlog_Copy.Content.ToString() == "屏蔽关键字日志:关")
            {
                ShieldLog = Microsoft.VisualBasic.Interaction.InputBox("输入你想屏蔽的关键词", "输入", "", -1, -1);
                closeOutlog_Copy.Content = "屏蔽关键字日志:开";
            }
            else
            {
                ShieldLog = null;
                closeOutlog_Copy.Content = "屏蔽关键字日志:关";
            }
        }

        void GetFastCmd()
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverId];
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
        void SetFastCmd()
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverId];
            if (_json["fastcmd"] == null)
            {
                JArray fastcmdArray = new JArray(fastCmdList.Items.Cast<string>().Skip(1));
                _json.Add("fastcmd", fastcmdArray);
                jsonObject[RserverId] = _json;
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Serverlist.json", Convert.ToString(jsonObject), Encoding.UTF8);
                GetFastCmd();
            }
            else
            {
                JArray fastcmdArray = new JArray(fastCmdList.Items.Cast<string>().Skip(1));
                _json["fastcmd"].Replace(fastcmdArray);
                jsonObject[RserverId] = _json;
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Serverlist.json", Convert.ToString(jsonObject), Encoding.UTF8);
                GetFastCmd();
            }
            /*
            fastCmdList.Items.Clear();
            fastCMD.Items.Add("/（指令）");
            foreach (var item in _json["fastcmd"])
            {
                fastCMD.Items.Add(item.ToString());
                fastCmdList.Items.Add(item.ToString());
            }
            */
        }

        private void refrushFastCmd_Click(object sender, RoutedEventArgs e)
        {
            GetFastCmd();
        }
        private void resetFastCmd_Click(object sender, RoutedEventArgs e)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
            JObject _json = (JObject)jsonObject[RserverId];
            if (_json["fastcmd"] == null)
            {
                return;
            }
            else
            {
                _json.Remove("fastcmd");
                jsonObject[RserverId] = _json;
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Serverlist.json", Convert.ToString(jsonObject), Encoding.UTF8);
                DialogShow.ShowMsg(this, "要使重置生效需重启此窗口，请您手动关闭此窗口并打开", "提示");
            }
        }

        private void addFastCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = Microsoft.VisualBasic.Interaction.InputBox("请输入指令（格式为：/指令）\n请不要加入其他字符，如：（、）、[、]等", "输入", "", -1, -1);
                if (text.Substring(text.Length - 1, 1) == " ")
                {
                    text = text.Remove(text.Length - 1, 1);
                }
                //fastCMD.Items.Add(text);
                fastCmdList.Items.Add(text);
                SetFastCmd();
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
        /// <summary>
        /// ///////////这是定时任务 （3个）
        /// </summary>
        private void startTimercmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startTimercmd.Content.ToString() == "启动定时任务")
                {
                    cmdtimer.Interval = TimeSpan.FromSeconds(int.Parse(timercmdTime.Text));
                    cmdtimer.Start();
                    startTimercmd.Content = "停止定时任务";
                }
                else
                {
                    cmdtimer.Stop();
                    startTimercmd.Content = "启动定时任务";
                }
            }
            catch (Exception a)
            {
                timerCmdout.Content = "执行失败，" + a.Message;
            }
        }
        private void startTimercmd2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startTimercmd2.Content.ToString() == "启动定时任务")
                {
                    cmdtimer2.Interval = TimeSpan.FromSeconds(int.Parse(timercmdTime2.Text));
                    cmdtimer2.Start();
                    startTimercmd2.Content = "停止定时任务";
                }
                else
                {
                    cmdtimer2.Stop();
                    startTimercmd2.Content = "启动定时任务";
                }
            }
            catch (Exception a)
            {
                timerCmdout2.Content = "执行失败，" + a.Message;
            }
        }
        private void startTimercmd3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (startTimercmd3.Content.ToString() == "启动定时任务")
                {
                    cmdtimer3.Interval = TimeSpan.FromSeconds(int.Parse(timercmdTime3.Text));
                    cmdtimer3.Start();
                    startTimercmd3.Content = "停止定时任务";
                }
                else
                {
                    cmdtimer3.Stop();
                    startTimercmd3.Content = "启动定时任务";
                }
            }
            catch (Exception a)
            {
                timerCmdout3.Content = "执行失败，" + a.Message;
            }
        }
        private void cmdtimer_Tick(object sender, EventArgs e)
        {

            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    SERVERCMD.StandardInput.WriteLine(timercmdCmd.Text);
                    timerCmdout.Content = "执行成功  时间：" + DateTime.Now.ToString("F");
                }
            }
            catch
            {
                timerCmdout.Content = "执行失败，请检查服务器是否开启  时间：" + DateTime.Now.ToString("F");
            }
        }
        private void cmdtimer2_Tick(object sender, EventArgs e)
        {

            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    SERVERCMD.StandardInput.WriteLine(timercmdCmd2.Text);
                    timerCmdout2.Content = "执行成功  时间：" + DateTime.Now.ToString("F");
                }
            }
            catch
            {
                timerCmdout2.Content = "执行失败，请检查服务器是否开启  时间：" + DateTime.Now.ToString("F");
            }
        }
        private void cmdtimer3_Tick(object sender, EventArgs e)
        {

            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    SERVERCMD.StandardInput.WriteLine(timercmdCmd3.Text);
                    timerCmdout3.Content = "执行成功  时间：" + DateTime.Now.ToString("F");
                }
            }
            catch
            {
                timerCmdout3.Content = "执行失败，请检查服务器是否开启  时间：" + DateTime.Now.ToString("F");
            }
        }
        #endregion

        #region active event
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