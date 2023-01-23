using HandyControl.Controls;
using MSL.controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageDialog = MSL.controls.MessageDialog;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Window = System.Windows.Window;

namespace MSL.pages
{
    
    /// <summary>
    /// Cmdoutlog.xaml 的交互逻辑
    /// </summary>
    public partial class ServerList : System.Windows.Controls.Page
    {
        public static event DeleControl OpenServerForm;
        public static bool ControlSetServerTab = false;
        public static bool ControlSetPMTab = false;
        List<string> serverjava = new List<string>();
        List<string> serverserver = new List<string>();
        List<string> serverJVM = new List<string>();
        List<string> serverbase = new List<string>();
        List<string> serverJVMcmd = new List<string>();
        public static string RunningServerIDs="";

        class ServerInfo
        {
            public string ServerName { get; set; }
            public string ServerIcon { get; set; }
            public ServerInfo(string serverName, string serverIcon)
            {
                ServerName = serverName;
                ServerIcon = serverIcon;
            }
        }
        public ServerList()
        {
            MainWindow.SetControlsColor += ChangeControlsColor;
            ServerRunner.SaveConfigEvent += Func;
            MainWindow.AutoOpenServer += Func1;
            InitializeComponent();
        }
        void ChangeControlsColor()
        {
            if (MainWindow.ControlsColor == 0)
            {
                Brush brush = new SolidColorBrush(Color.FromRgb(50, 108, 243));
                addServer.Background = brush;
            }
            if (MainWindow.ControlsColor == 1)
            {
                Brush brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                addServer.Background = brush;
            }
            if (MainWindow.ControlsColor == 2)
            {
                Brush brush3 = new SolidColorBrush(Color.FromRgb(232, 19, 19));
                addServer.Background = brush3;
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
        }
        void Func()
        {
            GetServerConfig();
        }
        void Func1()
        {
            GetServerConfig();
            serverList.SelectedIndex = int.Parse(MainWindow.serverid);
            try
            {
                if (serverserver[serverList.SelectedIndex].ToString().IndexOf("Bungeecord") + 1 != 0 || serverList.SelectedItem.ToString().IndexOf("bungeecord") + 1 != 0)
                {
                    MessageBox.Show("开服器暂不支持Bungeecord服务端的运行，请点击右侧“用命令行开启服务器”按钮来开服！");
                    return;
                }
            }
            catch
            { }
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                if (RunningServerIDs.IndexOf(serverList.SelectedIndex.ToString() + " ") + 1 != 0)
                {
                    MainWindow.servername = _server.ServerName;
                    OpenServerForm();
                    return;
                }
                RunningServerIDs = RunningServerIDs + serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                ServerRunner runner = new ServerRunner();
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }
        private void serverList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (serverserver[serverList.SelectedIndex].ToString().IndexOf("Bungeecord")+1!=0|| serverList.SelectedItem.ToString().IndexOf("bungeecord") + 1 != 0)
                {
                    MessageBox.Show("开服器暂不支持Bungeecord服务端的运行，请点击右侧“用命令行开启服务器”按钮来开服！");
                    return;
                }
            }
            catch
            {}
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                if (RunningServerIDs.IndexOf(serverList.SelectedIndex.ToString() + " ") + 1 != 0)
                {
                    MainWindow.servername = _server.ServerName;
                    OpenServerForm();
                    return;
                }
                RunningServerIDs = RunningServerIDs + serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                ServerRunner runner = new ServerRunner();
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            CreateServer window = new CreateServer();
            var mainwindow = (MainWindow)Window.GetWindow(this);
            window.Owner = mainwindow;
            window.ShowDialog();
            mainwindow.Focus();
            GetServerConfig();
            Growl.Success("刷新成功！");
        }

        private void refreshList_Click(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
            Growl.Success("刷新成功！");
        }
        private void GetServerConfig()
        {
            try
            {
                string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                serverList.Items.Clear();
                serverjava.Clear();
                serverserver.Clear();
                serverJVM.Clear();
                serverbase.Clear();
                serverJVMcmd.Clear();
                while (line.IndexOf("*") + 1 != 0)
                {
                    int IndexofA3 = line.IndexOf("-s ");
                    string Ru3 = line.Substring(IndexofA3 + 3);
                    string a300 = Ru3.Substring(0, Ru3.IndexOf("|"));
                    //serverserverlist.Items.Add(a300);
                    serverserver.Add(a300);

                    int IndexofA1 = line.IndexOf("-n ");
                    string Ru1 = line.Substring(IndexofA1 + 3);
                    string a100 = Ru1.Substring(0, Ru1.IndexOf("|"));
                    //serverjavalist.Items.Add(a200);
                    if (a300.IndexOf("forge") + 1 != 0||a300=="")
                    {
                        serverList.Items.Add(new ServerInfo(a100, "/images/150px-Anvil.png"));
                    }
                    else
                    {
                        serverList.Items.Add(new ServerInfo(a100, "/images/150px-Impulse_Command_Block.png"));
                    }

                    int IndexofA2 = line.IndexOf("-j ");
                    string Ru2 = line.Substring(IndexofA2 + 3);
                    string a200 = Ru2.Substring(0, Ru2.IndexOf("|"));
                    //serverjavalist.Items.Add(a200);
                    serverjava.Add(a200);

                    int IndexofA4 = line.IndexOf("-a ");
                    string Ru4 = line.Substring(IndexofA4 + 3);
                    string a400 = Ru4.Substring(0, Ru4.IndexOf("|"));
                    //serverJVMlist.Items.Add(a400);
                    serverJVM.Add(a400);

                    int IndexofA5 = line.IndexOf("-b ");
                    string Ru5 = line.Substring(IndexofA5 + 3);
                    string a500 = Ru5.Substring(0, Ru5.IndexOf("|"));
                    //serverbaselist.Items.Add(a500);
                    serverbase.Add(a500);

                    int IndexofA6 = line.IndexOf("-c ");
                    string Ru6 = line.Substring(IndexofA6 + 3);
                    string a600 = Ru6.Substring(0, Ru6.IndexOf("|"));
                    //serverbaselist.Items.Add(a500);
                    serverJVMcmd.Add(a600);

                    int IndexofA7 = line.IndexOf("*");
                    string Ru7 = line.Substring(IndexofA7);
                    string a700 = Ru7.Substring(0, Ru7.IndexOf("\n"));
                    //serverbaselist.Items.Add(a500);
                    line= line.Replace(a700, "");
                }
            }
            catch
            {
                MessageDialogShow.Show("开服器检测到配置文件出现了错误，是第一次使用吗？\n是否创建一个新的服务器？", "警告", true, "确定", "取消");
                MessageDialog messageDialog = new MessageDialog();
                var mainwindow = (MainWindow)Window.GetWindow(this);
                messageDialog.Owner = mainwindow;
                messageDialog.ShowDialog();
                //var result = MessageBox.Show("开服器检测到您可能没有创建服务器或配置文件出现了错误，\n是否创建配置服务器？","",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                if (MessageDialog._dialogReturn ==true)
                {
                    Window wn = new CreateServer();
                    wn.Owner = mainwindow;
                    wn.ShowDialog();
                    GetServerConfig();
                }
            }
        }

        private void setServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                if (RunningServerIDs.IndexOf(serverList.SelectedIndex.ToString() + " ") + 1 != 0)
                {
                    MainWindow.servername = _server.ServerName;
                    OpenServerForm();
                    return;
                }
                RunningServerIDs = RunningServerIDs + serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                //SetServerConfig();
                ControlSetServerTab=true;
                ServerRunner runner = new ServerRunner();
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void delServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                line = line.Replace("*|-n " + MainWindow.servername + "|-j " + MainWindow.serverjava + "|-s " + MainWindow.serverserver + "|-a " + MainWindow.serverJVM + "|-b " + MainWindow.serverbase + "|-c " + MainWindow.serverJVMcmd + "|*\n", "");
                FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(line);
                sw.Flush();
                sw.Close();
                fs.Close();
                Growl.Success("删除服务器成功！");
            }
            catch
            {
                Growl.Error("删除服务器失败！");
                MessageDialogShow.Show("服务器删除失败！", "警告", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                var mainwindow = (MainWindow)Window.GetWindow(this);
                messageDialog.Owner = mainwindow;
                messageDialog.ShowDialog();
                GetServerConfig();
            }
            try
            {
                MessageDialogShow.Show("是否删除服务器相关文件夹及文件？（该功能会删除所选服务器目录内的其他文件，请谨慎选择！）", "提示", true, "确定", "取消");
                MessageDialog messageDialog2 = new MessageDialog();
                MainWindow mainwindow2 = (MainWindow)System.Windows.Window.GetWindow(this);
                messageDialog2.Owner = mainwindow2;
                messageDialog2.ShowDialog();
                if (MessageDialog._dialogReturn)
                {
                    MessageDialog._dialogReturn = false;
                    DirectoryInfo directoryInfo = new DirectoryInfo(MainWindow.serverbase);
                    directoryInfo.Delete(true);
                }
                GetServerConfig();
                Growl.Success("删除服务器文件成功！");
            }
            catch(Exception ex)
            {
                MessageDialogShow.Show("服务器已删除！但部分文件可能需要手动进行删除\n"+ex, "警告", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                var mainwindow = (MainWindow)Window.GetWindow(this);
                messageDialog.Owner = mainwindow;
                messageDialog.ShowDialog(); 
                GetServerConfig();
            }
        }

        private void startWithCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/K " + "@ \"" + MainWindow.serverjava + "\" " + MainWindow.serverJVM + " " + MainWindow.serverJVMcmd + " -jar \"" + MainWindow.serverserver + "\" nogui&pause&exit";
                Directory.SetCurrentDirectory(MainWindow.serverbase);
                process.Start();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void startServerBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serverserver[serverList.SelectedIndex].ToString().IndexOf("Bungeecord") + 1 != 0 || serverList.SelectedItem.ToString().IndexOf("bungeecord") + 1 != 0)
                {
                    MessageBox.Show("开服器暂不支持Bungeecord服务端的运行，请点击右侧“用命令行开启服务器”按钮来开服！");
                    return;
                }
            }
            catch
            { }
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                if (RunningServerIDs.IndexOf(serverList.SelectedIndex.ToString() + " ") + 1 != 0)
                {
                    MainWindow.servername = _server.ServerName;
                    OpenServerForm();
                    return;
                }
                RunningServerIDs = RunningServerIDs + serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                ServerRunner runner = new ServerRunner();
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Growl.Info("正在为您打开服务器文件夹……");
                Process.Start(serverbase[serverList.SelectedIndex]);
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void setModorPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServerInfo _server = serverList.SelectedItem as ServerInfo;
                if (RunningServerIDs.IndexOf(serverList.SelectedIndex.ToString() + " ") + 1 != 0)
                {
                    MainWindow.servername = _server.ServerName;
                    OpenServerForm();
                    return;
                }
                RunningServerIDs = RunningServerIDs + serverList.SelectedIndex.ToString() + " ";
                MainWindow.serverid = serverList.SelectedIndex.ToString() + " ";
                MainWindow.servername = _server.ServerName;
                MainWindow.serverjava = serverjava[serverList.SelectedIndex];
                MainWindow.serverserver = serverserver[serverList.SelectedIndex];
                MainWindow.serverJVM = serverJVM[serverList.SelectedIndex];
                MainWindow.serverJVMcmd = serverJVMcmd[serverList.SelectedIndex];
                MainWindow.serverbase = serverbase[serverList.SelectedIndex];
                //SetServerConfig();
                ControlSetPMTab = true;
                ServerRunner runner = new ServerRunner();
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void serverList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.serverList.SelectedIndex != -1)
            {
                startServerBtn.IsEnabled = true;
                delServer.IsEnabled = true;
                startWithCmd.IsEnabled = true;
                setServer.IsEnabled = true;
                setModorPlugin.IsEnabled = true;
                openServerDir.IsEnabled = true;
                return;
            }
            startServerBtn.IsEnabled = false;
            delServer.IsEnabled = false;
            startWithCmd.IsEnabled = false;
            setServer.IsEnabled = false;
            setModorPlugin.IsEnabled = false;
            openServerDir.IsEnabled = false;
        }
    }
}
