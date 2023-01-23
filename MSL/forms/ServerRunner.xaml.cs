using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.pages;
using PastebinAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.Web.UI;
using static System.Net.Mime.MediaTypeNames;
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
        public Process SERVERCMD = new Process();
        string ShieldLog;
        public event DelReadStdOutput ReadStdOutput;
        bool autoserver = false;
        string DownjavaName;
        public static string DownServer;
        string RserverId = MainWindow.serverid;
        string Rservername = MainWindow.servername;
        string Rserverjava = MainWindow.serverjava;
        string Rserverserver = MainWindow.serverserver;
        string RserverJVM = MainWindow.serverJVM;
        string RserverJVMcmd = MainWindow.serverJVMcmd;
        string Rserverbase = MainWindow.serverbase;
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
            ServerList.OpenServerForm += Func3;
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ControlsColor == 1)
            {
                Brush back = new SolidColorBrush(Color.FromRgb(234, 234, 234));
                this.Background = back;
                Brush brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                sendcmd.Background = brush;
                reFresh.Background = brush;
                opencurseforge.Background = brush;
                refreahConfig.Background = brush;
                doneBtn1.Background = brush;
            }
            if (MainWindow.ControlsColor == 2)
            {
                Brush back2 = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 245, 245));
                Background = back2;
                Brush brush2 = new SolidColorBrush(Color.FromRgb(232, 19, 19));
                sendcmd.Background = brush2;
                reFresh.Background = brush2;
                opencurseforge.Background = brush2;
                refreahConfig.Background = brush2;
                doneBtn1.Background = brush2;
                outlog.Background = back2;
            }
            this.Title = Rservername;
            MainWindow.serverJVMcmd = "";
            MainWindow.serverserver = "";
            MainWindow.serverJVM = "";
            MainWindow.serverbase = "";
            MainWindow.serverjava = "";
            MainWindow.servername = "";
            GetFastCmd();
            if (ServerList.ControlSetPMTab == true)
            {
                ServerList.ControlSetPMTab = false;
                //ReFreshPluginsAndMods();
                Func1();
                ReFreshPluginsAndMods();
                TabCtrl.SelectedIndex = 2;
                ReFreshPluginsAndMods();
            }
            else
            {
                if (ServerList.ControlSetServerTab == true)
                {
                    ServerList.ControlSetServerTab = false;
                    Func1();
                    ReFreshPluginsAndMods();
                    TabCtrl.SelectedIndex = 3;
                }
                else
                {
                    Func();
                    Func1();
                    ReFreshPluginsAndMods();
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited != true)
                {
                    MessageDialogShow.Show("检测到您没有关闭服务器，是否隐藏此窗口？\n如要重新显示此窗口，请在服务器列表内双击该服务器（或点击开启服务器按钮）", "警告", true, "确定", "取消");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
                    e.Cancel = true;
                    if (MessageDialog._dialogReturn == true)
                    {
                        MessageDialog._dialogReturn = false;
                        this.Visibility = System.Windows.Visibility.Hidden;
                    }
                    //MessageBox.Show("您没有关闭服务器，请输入stop关闭服务器后再关闭此窗口！");
                }
                else
                {
                    ServerList.RunningServerIDs = ServerList.RunningServerIDs.Replace(RserverId, "");
                    this.Title = string.Empty;
                    outlog.Document.Blocks.Clear();
                    Rserverbase = null;
                    RserverId = null;
                    Rserverjava = null;
                    RserverJVM = null;
                    RserverJVMcmd = null;
                    Rservername = null;
                    Rserverserver = null;
                    ShieldLog = null;
                    DownjavaName = null;
                    GC.Collect();
                }
            }
            catch
            {
                ServerList.RunningServerIDs = ServerList.RunningServerIDs.Replace(RserverId, "");
                this.Title = string.Empty;
                outlog.Document.Blocks.Clear();
                Rserverbase = null;
                RserverId = null;
                Rserverjava = null;
                RserverJVM = null;
                RserverJVMcmd = null;
                Rservername = null;
                Rserverserver = null;
                ShieldLog = null;
                DownjavaName = null;
                GC.Collect();
            }
        }
        void Func3()
        {
            if (this.Title == MainWindow.servername)
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Visibility = System.Windows.Visibility.Visible;
                this.Topmost = true;
                this.Topmost = false;
            }
        }
        bool isModsPluginsRefresh = true;
        private void TabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabCtrl.SelectedIndex == 1)
            {
                GetServerConfig();
            }
            else
            {
                config = null;
            }
            if (TabCtrl.SelectedIndex == 2)
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

        #region
        /// <summary>
        /// ///////////这里是服务器输出
        /// </summary>
        private void Func()
        {
            try
            {
                cmdtext.IsEnabled = true;
                controlServer.Content = "关服";
                fastCMD.IsEnabled = true;
                outlog.Document.Blocks.Clear();
                Growl.Info("开服中，请稍等……");
                ShowLog("正在开启服务器，请稍等...", Brushes.Black);
                //serverState.Content = "开服中";
                if (Rserverserver == "")
                {
                    StartServer(RserverJVM + " " + RserverJVMcmd + " nogui");
                }
                else
                {
                    StartServer(RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverbase + @"\" + Rserverserver + "\" nogui");
                }
            }
            catch (Exception a)
            {
                Growl.Error("开服失败！");
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
                //StartServerControl();
                //cmdtext.IsEnabled = true;
                sendcmd.IsEnabled = true;
                SERVERCMD.StartInfo.FileName = Rserverjava;
                //SERVERCMD.StartInfo.FileName = StartFileName;
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
                cmdtext.Text = "";
                timer1.Interval = TimeSpan.FromSeconds(1);
                timer1.Start();
                //SERVERCMD.StandardInput.WriteLine(StartFileArg + "&exit");
                //serverTime = 0;
                //timer2.Tick += new EventHandler(timer2_Tick);
                //timer2.Interval = TimeSpan.FromSeconds(1);
                //timer2.Start();
            }
            catch (Exception e)
            {
                timer1.Stop();
                //StopServerControl();
                Growl.Error("开服失败！");
                ShowLog("出现错误，正在检查问题...", Brushes.Black);
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
                //MessageBox.Show("出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！\n错误代码:" + e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageDialogShow.Show("出现错误，开服器已检测完毕，请根据检测信息对服务器设置进行更改！\n错误代码:" + e.Message, "错误", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                try
                {
                    SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.CancelOutputRead();
                    SERVERCMD.CancelErrorRead();
                }
                catch
                { }
                cmdtext.IsEnabled = false;
                controlServer.Content = "开服";
                fastCMD.IsEnabled = false;

            }
        }
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
                    outlog.ScrollToEnd();
                    return;
                }
                p.Foreground = color;
                tempbrush = color;
                outlog.Document.Blocks.Add(p);
                outlog.ScrollToEnd();
            });
        }
        private delegate void AddMessageHandler(string msg);
        private void ReadStdOutputAction(string msg)
        {
            if (outlog.Document.Blocks.Count >= 500)
            {
                outlog.Document.Blocks.Clear();
            }
            if (closeOutlog_Copy.Content.ToString() == "屏蔽关键字日志:开")
            {
                if (msg.IndexOf(ShieldLog) + 1 != 0)
                {
                    return;
                }
            }
            if (msg.IndexOf("agree to the EULA") + 1 != 0)
            {
                //outlog.AppendText("[信息]" + msg);
                //MessageBoxResult msgr = MessageBox.Show("检测到您没有接受Mojang的EULA条款！是否阅读并接受EULA条款并继续开服？", , MessageBoxButton.YesNo, MessageBoxImage.Warning);
                ShowLog(msg, Brushes.Green);
                //var result = MessageBox.Show(, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                MessageDialogShow.Show("检测到您没有接受Mojang的EULA条款！是否阅读并接受EULA条款并继续开服？", "提示", true, "确定", "取消");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                if (MessageDialog._dialogReturn == true)
                {
                    MessageDialog._dialogReturn = false;
                    //var result = MessageBox.Show("检测到您没有接受Mojang的EULA条款！是否阅读并接受EULA条款并继续开服？", "提示",MessageBoxButton.YesNo,MessageBoxImage.Information);
                    //if (result == MessageBoxResult.Yes)
                    //{
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
                        //serverState.Content = "重启中";
                        SERVERCMD.CancelOutputRead();
                        SERVERCMD.CancelErrorRead();
                        SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        //ReadStdOutput = null;
                        //ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
                        outlog.Document.Blocks.Clear();
                        ShowLog("正在重启服务器...", Brushes.Black);
                        Func();
                    }
                    catch (Exception a)
                    {
                        MessageBox.Show("出现错误，请手动修改eula文件或重试:" + a, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Process.Start("https://account.mojang.com/documents/minecraft_eula");
                }

            }
            else
            {
                if (msg.IndexOf("Unable to access jarfile") + 1 != 0)
                {
                    ShowLog(msg + "\r\n警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
                }
                else if (msg.IndexOf("加载 Java 代理时出错") + 1 != 0)
                {
                    ShowLog(msg + "\r\n警告：无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符,请及时修改！", Brushes.Red);
                }
                else if (msg.IndexOf("Done") + 1 != 0)
                {
                    if (msg.IndexOf("help") + 1 != 0)
                    {
                        //outlog.AppendText("[信息]" + msg + "已成功开启服务器！在没有改动服务器IP和端口的情况下，请使用127.0.0.1:25565进入服务器；要让他人进服，需要进行内网映射或使用公网IP。");
                        ShowLog(msg + "\r\n已成功开启服务器！你可以输入stop来关闭服务器！\r\n服务器本地IP通常为:127.0.0.1，想要远程进入服务器，需要开通公网IP或使用内网映射，详情参照开服器的内网映射界面。", Brushes.OrangeRed);
                        Growl.Success("已成功开启服务器！");
                        //serverState.Content = "已开服";
                        try
                        {
                            string path1 = Rserverbase + @"\server.properties";
                            FileStream fs = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default);
                            string line;
                            line = sr.ReadToEnd();
                            if (line.IndexOf("online-mode=true") + 1 != 0)
                            {
                                ShowLog("\r\n检测到您没有关闭正版验证，如果客户端为离线登录的话，请点击“更多功能”里“关闭正版验证”按钮以关闭正版验证。否则离线账户将无法进入服务器！\r\n", Brushes.Red);
                            }
                        }
                        catch
                        { }
                    }
                }
                else if (msg.IndexOf("Stopping server") + 1 != 0)
                {
                    //outlog.AppendText("[信息]" + msg + "正在关闭服务器！");
                    ShowLog(msg + "\r\n正在关闭服务器！", Brushes.Black);
                    //serverState.Content = "关服中";
                }
                else if (msg.IndexOf("FAILED") + 1 != 0)
                {
                    if (msg.IndexOf("PORT") + 1 != 0)
                    {
                        ShowLog(msg + "\r\n警告：由于端口占用，服务器已自动关闭！请检查您的服务器是否多开或者有其他软件占用端口！\r\n解决方法：您可尝试通过重启电脑解决！", Brushes.Red);
                    }
                }
                else if (closeOutlog.Content.ToString() == "关闭日志输出")
                {
                    if (msg.IndexOf("INFO") + 1 != 0)
                    {
                        ShowLog("[" + DateTime.Now.ToString("T") + " 信息]" + msg.Substring(msg.IndexOf("INFO") + 5), Brushes.Green);

                    }
                    else if (msg.IndexOf("WARN") + 1 != 0)
                    {

                        ShowLog("[" + DateTime.Now.ToString("T") + " 警告]" + msg.Substring(msg.IndexOf("WARN") + 5), Brushes.Orange);
                    }
                    else if (msg.IndexOf("ERROR") + 1 != 0)
                    {

                        ShowLog("[" + DateTime.Now.ToString("T") + " 错误]" + msg.Substring(msg.IndexOf("ERROR") + 5), Brushes.Red);

                    }
                    else
                    {
                        ShowLog(msg, Brushes.White);
                    }
                }
            }
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (SERVERCMD.HasExited == true)
            {
                timer1.Stop();
                if (autoserver == true)
                {
                    //serverState.Content = "重启中";
                    SERVERCMD.CancelOutputRead();
                    SERVERCMD.CancelErrorRead();
                    SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    //ReadStdOutput = null;
                    //ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
                    outlog.Document.Blocks.Clear();
                    ShowLog("正在重启服务器...", Brushes.Black);
                    //outlog.AppendText("正在重启服务器...");
                    Func();
                }
                else
                {
                    //StopServerControl();
                    Growl.Info("服务器已关闭！");
                    cmdtext.Text = "服务器已关闭";
                    sendcmd.IsEnabled = false;
                    cmdtext.IsEnabled = false;
                    controlServer.Content = "开服";
                    fastCMD.IsEnabled = false;
                    SERVERCMD.CancelOutputRead();
                    SERVERCMD.CancelErrorRead();
                    SERVERCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    SERVERCMD.ErrorDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    //ReadStdOutput = null;
                    //ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
                    //outlog.AppendText("\n服务器已关闭！输入start来开启服务器.");
                }
            }
        }
        private void sendcmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/（指令）")
                {
                    SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/op（给管理员）")
                {
                    SERVERCMD.StandardInput.WriteLine("op " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/deop（去除管理员）")
                {
                    SERVERCMD.StandardInput.WriteLine("deop " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/ban（封禁玩家）")
                {
                    SERVERCMD.StandardInput.WriteLine("ban " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/say（全服说话）")
                {
                    SERVERCMD.StandardInput.WriteLine("say " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/pardon（解除封禁）")
                {
                    SERVERCMD.StandardInput.WriteLine("pardon " + cmdtext.Text);
                    cmdtext.Text = "";
                }
                else
                {
                    SERVERCMD.StandardInput.WriteLine(fastCMD.SelectedItem.ToString().Replace("/", "") + " " + cmdtext.Text);
                    cmdtext.Text = "";
                }
            }
            catch
            {
                SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                cmdtext.Text = "";
            }
        }

        private void cmdtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/（指令）")
                    {
                        SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/op（给管理员）")
                    {
                        SERVERCMD.StandardInput.WriteLine("op " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/deop（去除管理员）")
                    {
                        SERVERCMD.StandardInput.WriteLine("deop " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/ban（封禁玩家）")
                    {
                        SERVERCMD.StandardInput.WriteLine("ban " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/say（全服说话）")
                    {
                        SERVERCMD.StandardInput.WriteLine("say " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else if (fastCMD.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "") == "/pardon（解除封禁）")
                    {
                        SERVERCMD.StandardInput.WriteLine("pardon " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                    else
                    {
                        SERVERCMD.StandardInput.WriteLine(fastCMD.SelectedItem.ToString().Replace("/", "") + " " + cmdtext.Text);
                        cmdtext.Text = "";
                    }
                }
                catch
                {
                    SERVERCMD.StandardInput.WriteLine(cmdtext.Text);
                    cmdtext.Text = "";
                }
            }
        }

        private void controlServer_Click(object sender, RoutedEventArgs e)
        {
            if (controlServer.Content.ToString() == "开服")
            {
                Func();
            }
            else
            {
                SERVERCMD.StandardInput.WriteLine("stop");
            }
        }

        private void controlServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (controlServer.Content.ToString() == "关服")
            {
                SERVERCMD.Kill();
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
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请开启一次服务器后再试！\n" + ex.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error); TabCtrl.SelectedIndex = 0; }
        }
        private void completeServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    //MessageBox.Show("您没有关闭服务器，无法调整服务器功能！");
                    MessageDialogShow.Show("您没有关闭服务器，无法调整服务器功能！", "错误", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
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
                MessageDialogShow.Show("保存成功！", "信息", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                GetServerConfig();
            }
            catch { }
        }
        private void changeWorldMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    //MessageBox.Show("您没有关闭服务器，无法调整服务器功能！");
                    MessageDialogShow.Show("您没有关闭服务器，无法调整服务器功能！", "错误", false, "", "确定");
                    MessageDialog messageDialog0 = new MessageDialog();
                    messageDialog0.Owner = this;
                    messageDialog0.ShowDialog();
                    return;
                }
            }
            catch
            { }
            MessageDialogShow.Show("此功能会删除原来的旧地图，是否要继续使用？", "警告", true, "确定", "取消");
            MessageDialog messageDialog = new MessageDialog();
            messageDialog.Owner = this;
            messageDialog.ShowDialog();
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
                    MessageDialogShow.Show("导入世界成功！", "信息", false, "", "确定");
                    MessageDialog messageDialog1 = new MessageDialog();
                    messageDialog1.Owner = this;
                    messageDialog1.ShowDialog();
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
                    lab001.Content = "已检测到plugins和mods文件夹，以下为您的插件及模组";
                    modLabel.Visibility = System.Windows.Visibility.Visible;
                    modslist.Visibility = System.Windows.Visibility.Visible;
                    pluginslist.Width = 255;
                    modslist.Width = 255;
                    pluginslist.Margin = new Thickness(10, 70, 0, 19);
                    modslist.Margin = new Thickness(290, 70, 0, 19);
                    openpluginsDir.IsEnabled = true;
                    openmodsDir.IsEnabled = true;
                    addPlugin.IsEnabled = true;
                    addMod.IsEnabled = true;
                    delPlugin.IsEnabled = true;
                    delMod.IsEnabled = true;
                    DirectoryInfo directoryInfo1 = new DirectoryInfo(Rserverbase + @"\mods");
                    FileInfo[] file1 = directoryInfo1.GetFiles("*.jar");
                    foreach (FileInfo f1 in file1)
                    {
                        modslist.Items.Add(f1.Name);
                    }
                }
                else
                {
                    lab001.Content = "已检测到plugins文件夹，以下为您的插件";
                    modLabel.Visibility = System.Windows.Visibility.Hidden;
                    modslist.Visibility = System.Windows.Visibility.Hidden;
                    pluginslist.Width = 500;
                    pluginslist.Margin = new Thickness(10, 70, 0, 19);
                    openpluginsDir.IsEnabled = true;
                    openmodsDir.IsEnabled = false;
                    addPlugin.IsEnabled = true;
                    addMod.IsEnabled = false;
                    delPlugin.IsEnabled = true;
                    delMod.IsEnabled = false;
                }
            }
            else
            {
                if (Directory.Exists(Rserverbase + @"\mods"))
                {
                    lab001.Content = "已检测到mods文件夹，以下为您的模组";
                    pluginLabel.Visibility = System.Windows.Visibility.Hidden;
                    pluginslist.Visibility = System.Windows.Visibility.Hidden;
                    modslist.Width = 500;
                    modslist.Margin = new Thickness(10, 70, 0, 19);
                    openmodsDir.IsEnabled = true;
                    addMod.IsEnabled = true;
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
                    lab001.Content = "未检测到plugins文件夹及mods文件夹，请重启服务器或检查服务端是否支持插件模组";
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
        private void openspigot_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.spigotmc.org/");
        }

        private void opencurseforge_Click(object sender, RoutedEventArgs e)
        {
            //Process.Start("https://www.curseforge.com/");
            DownloadPorM downloadPorM = new DownloadPorM();
            downloadPorM.Owner = this;
            downloadPorM.ShowDialog();
            ReFreshPluginsAndMods();
        }
        #endregion

        #region 服务器设置
        /// <summary>
        /// //////////////////////这里是服务器设置界面
        /// </summary>
        void Func1()
        {
            try
            {
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
            Func1();
        }
        private void doneBtn1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SERVERCMD.HasExited == false)
                {
                    //MessageBox.Show("您没有关闭服务器，无法更改服务器设置！");
                    MessageDialogShow.Show("您没有关闭服务器，无法更改服务器设置！", "错误", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
                    return;
                }
            }
            catch
            { }
            try
            {
                if (!System.IO.Path.IsPathRooted(jAva.Text))
                {
                    jAva.Text = AppDomain.CurrentDomain.BaseDirectory + jAva.Text;
                }
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                //version
                byte[] pageData1 = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/otherdownload.txt");
                string nv1 = Encoding.UTF8.GetString(pageData1);
                //frpc
                int IndexofA1 = nv1.IndexOf("* ");
                string Ru1 = nv1.Substring(IndexofA1 + 2);
                string nv2 = nv1.Replace("* " + Ru1.Substring(0, Ru1.IndexOf(" *")) + " *", "");
                //jv83 *已删除
                //jv86
                int IndexofA3 = nv2.IndexOf("* ");
                string Ru3 = nv2.Substring(IndexofA3 + 2);
                string _dnjv86 = Ru3.Substring(0, Ru3.IndexOf(" *"));
                string nv4 = nv2.Replace("* " + _dnjv86 + " *", "");
                //jv16
                int IndexofA4 = nv4.IndexOf("* ");
                string Ru4 = nv4.Substring(IndexofA4 + 2);
                string _dnjv16 = Ru4.Substring(0, Ru4.IndexOf(" *"));
                string nv5 = nv4.Replace("* " + _dnjv16 + " *", "");
                //jv17
                int IndexofA5 = nv5.IndexOf("* ");
                string Ru5 = nv5.Substring(IndexofA5 + 2);
                string _dnjv17 = Ru5.Substring(0, Ru5.IndexOf(" *"));
                string nv6 = nv5.Replace("* " + _dnjv17 + " *", "");
                //jv18
                int IndexofA6 = nv6.IndexOf("* ");
                string Ru6 = nv6.Substring(IndexofA6 + 2);
                string _dnjv18 = Ru6.Substring(0, Ru6.IndexOf(" *"));
                doneBtn1.IsEnabled = false;
                if (useDownJv.IsChecked == true)
                {
                    if (selectJava.SelectedIndex == 0)
                    {
                        DownloadJava("Java8", _dnjv86);
                    }
                    if (selectJava.SelectedIndex == 1)
                    {
                        DownloadJava("Java16", _dnjv16);
                    }
                    if (selectJava.SelectedIndex == 2)
                    {
                        DownloadJava("Java17", _dnjv17);
                    }
                    if (selectJava.SelectedIndex == 3)
                    {
                        DownloadJava("Java18", _dnjv18);
                    }
                }
                if (useSelf.IsChecked == true)
                {
                    if (useJVMauto.IsChecked == true)
                    {
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a |-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    else
                    {
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a " + " -Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M" + "|-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    //MessageBox.Show("保存完毕！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageDialogShow.Show("保存完毕！", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
                    doneBtn1.IsEnabled = true;
                    Rservername = nAme.Text;
                    Rserverserver = server.Text;
                    Rserverbase = bAse.Text;
                    RserverJVMcmd = jVMcmd.Text;
                    Rserverjava = jAva.Text;
                    Func1();
                    SaveConfigEvent();
                }
                if (useJvpath.IsChecked == true)
                {
                    if (useJVMauto.IsChecked == true)
                    {
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j Java|-s " + server.Text + "|-a |-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    else
                    {
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j Java|-s " + server.Text + "|-a " + " -Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M" + "|-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    //MessageBox.Show("保存完毕！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageDialogShow.Show("保存完毕！", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
                    doneBtn1.IsEnabled = true;
                    Rservername = nAme.Text;
                    Rserverserver = server.Text;
                    Rserverbase = bAse.Text;
                    RserverJVMcmd = jVMcmd.Text;
                    Rserverjava = jAva.Text;
                    Func1();
                    SaveConfigEvent();
                }
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
                    Directory.CreateDirectory(bAse.Text);
                    string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                    line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a |-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                    FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.Write(line);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                else
                {
                    Directory.CreateDirectory(bAse.Text);
                    string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                    line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a " + " -Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M" + "|-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                    FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.Write(line);
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                //MessageBox.Show("保存完毕！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                MessageDialogShow.Show("保存完毕！", "信息", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                doneBtn1.IsEnabled = true;
                Rservername = nAme.Text;
                Rserverserver = server.Text;
                Rserverbase = bAse.Text;
                RserverJVMcmd = jVMcmd.Text;
                Rserverjava = jAva.Text;
                Func1();
                SaveConfigEvent();
            }
            else
            {
                //MessageBox.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
                MessageDialogShow.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
                {
                    DownjavaName = fileName;
                    //DownloadWindow.downloadurl = RserverLink +@"/web/Java8.exe";
                    DownloadWindow.downloadurl = downUrl;
                    DownloadWindow.downloadPath = AppDomain.CurrentDomain.BaseDirectory + "MSL";
                    DownloadWindow.filename = "Java.zip";
                    DownloadWindow.downloadinfo = "下载" + fileName + "中……";
                    Window window = new DownloadWindow();
                    window.Owner = this;
                    window.ShowDialog();
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
                    /*
                    Form4 fw = new Form4();
                    fw.ShowDialog();*/
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
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a |-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    else
                    {
                        Directory.CreateDirectory(bAse.Text);
                        string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                        line = line.Replace("*|-n " + Rservername + "|-j " + Rserverjava + "|-s " + Rserverserver + "|-a " + RserverJVM + "|-b " + Rserverbase + "|-c " + RserverJVMcmd + "|*\n", "*|-n " + nAme.Text + "|-j " + jAva.Text + "|-s " + server.Text + "|-a " + " -Xms" + memorySlider.ValueStart.ToString("f0") + "M" + " -Xmx" + memorySlider.ValueEnd.ToString("f0") + "M" + "|-b " + bAse.Text + "|-c " + jVMcmd.Text + "|*\n");
                        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(line);
                        sw.Flush();
                        sw.Close();
                        fs.Close();
                    }
                    //MessageBox.Show("保存完毕！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageDialogShow.Show("保存完毕！", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
                    downout.Content = "安装成功！";
                    doneBtn1.IsEnabled = true;
                    Rservername = nAme.Text;
                    Rserverserver = server.Text;
                    Rserverbase = bAse.Text;
                    RserverJVMcmd = jVMcmd.Text;
                    Rserverjava = jAva.Text;
                    Func1();
                    SaveConfigEvent();
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
                    MessageDialogShow.Show("已将服务端文件移至服务器文件夹中！您可将源文件删除！", "提示", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
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
            MainWindow.serverJVMcmd = "";
            MainWindow.serverserver = "";
            MainWindow.serverJVM = "";
            MainWindow.serverjava = "";
            MainWindow.servername = "";
            MainWindow.serverbase = Rserverbase;
            DownloadServer downloadServer = new DownloadServer();
            downloadServer.Owner = this;
            downloadServer.ShowDialog();
            if (File.Exists(Rserverbase + @"\" + MainWindow.serverserver))
            {
                server.Text = MainWindow.serverserver;
            }
            else if (MainWindow.serverJVMcmd != "")
            {
                jVMcmd.Text = RserverJVMcmd;
            }
            else
            {
                MessageBox.Show("下载失败！");
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
            MainWindow.serverbase = Rserverbase;
            Window window = new SetServerconfig();
            window.ShowDialog();
        }
        private void getLaunchercode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(Rserverbase + @"\StartServer.bat", "@ECHO OFF\n" + Rserverjava + RserverJVM + " " + RserverJVMcmd + " -jar \"" + Rserverserver + "\" nogui " + "\npause");
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
                    MessageDialogShow.Show("检测到服务器正在运行，正在关闭服务器", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
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
                    MessageDialogShow.Show("修改完毕，请重新开启服务器！", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
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
                    MessageDialogShow.Show("修改完毕，请重新开启服务器！", "信息", false, "", "确定");
                    MessageDialog messageDialog = new MessageDialog();
                    messageDialog.Owner = this;
                    messageDialog.ShowDialog();
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
            if (closeOutlog.Content.ToString() == "关闭日志输出")
            {
                closeOutlog.Content = "打开日志输出";
            }
            else
            {
                closeOutlog.Content = "关闭日志输出";
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
        private async void uplodeLog_Click(object sender, RoutedEventArgs e)
        {
            uplodeLog.Content = "请等待……";
            uplodeLog.IsEnabled = false;
            Pastebin.DevKey = "";
            User me = await Pastebin.LoginAsync("", "");
            TextRange code = new TextRange(outlog.Document.ContentStart, outlog.Document.ContentEnd);
            Paste newPaste = await me.CreatePasteAsync(code.Text, null, null, PastebinAPI.Visibility.Private, Expiration.OneDay);
            MessageBox.Show("URL:" + newPaste.Url + "\n" + "Paste key:" + newPaste.Key + "\nPress Ctrl+C to copy");
            uplodeLog.Content = "上传日志至云端";
            uplodeLog.IsEnabled = true;
        }

        void GetFastCmd()
        {
            //object transferStr="";
            fastCmdList.Items.Clear();
            for (int i = 0; i < fastCMD.Items.Count; i++)
            {
                //transferStr=fastCMD.Items[i];
                fastCmdList.Items.Add(fastCMD.Items[i].ToString().Replace("System.Windows.Controls.ComboBoxItem: ", ""));
            }
        }
        void SetFastCmd()
        {
            //object transferStr="";
            fastCMD.Items.Clear();
            fastCMD.Items.Add("/（指令）");
            for (int i = 1; i < fastCmdList.Items.Count; i++)
            {
                //transferStr=fastCMD.Items[i];
                fastCMD.Items.Add(fastCmdList.Items[i].ToString());

            }
        }

        private void refrushFastCmd_Click(object sender, RoutedEventArgs e)
        {
            GetFastCmd();
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
                fastCMD.Items.Add(text);
                GetFastCmd();
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

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }
    }
}