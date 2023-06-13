using HandyControl.Controls;
using Microsoft.VisualBasic.FileIO;
using MSL.controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageDialog = MSL.controls.MessageDialog;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Window = System.Windows.Window;

namespace MSL.pages
{

    /// <summary>
    /// Cmdoutlog.xaml 的交互逻辑
    /// </summary>
    public partial class ServerList : Page
    {
        public static event DeleControl OpenServerForm;
        public static bool ControlSetServerTab = false;
        public static bool ControlSetPMTab = false;
        public static List<string> serverid = new List<string>();
        public static string RunningServerIDs = "";

        class ServerInfo
        {
            public string ServerName { get; set; }
            public string ServerIcon { get; set; }
            public string ServerState { get; set; }
            public Brush ServerStateFore { get; set; }
            public ServerInfo(string serverName, string serverIcon, string serverState, Brush serverStateFore)
            {
                ServerName = serverName;
                ServerIcon = serverIcon;
                ServerState = serverState;
                ServerStateFore = serverStateFore;
            }
        }
        public ServerList()
        {
            ServerRunner.SaveConfigEvent += RefreshConfig;
            ServerRunner.ServerStateChange += RefreshConfig;
            MainWindow.AutoOpenServer += AutoOpenServer;
            Home.AutoOpenServer += AutoOpenServer;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
        }
        void RefreshConfig()
        {
            GetServerConfig();
        }

        void StateCheck()
        {
            for (int id = 0; id < serverList.Items.Count; id++)
            {
                //MessageBox.Show(id.ToString());

                ServerInfo _server = serverList.Items[id] as ServerInfo;
                //MessageBox.Show(RunningServerIDs);
                //MessageBox.Show(serverid[id]);
                if (RunningServerIDs.IndexOf(serverid[id] + " ") + 1 != 0)
                {
                    _server.ServerState = "运行中";
                    _server.ServerStateFore = Brushes.Red;
                }
                else
                {
                    _server.ServerState = "未运行";
                    _server.ServerStateFore = Brushes.MediumSeaGreen;
                }
            }
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            forms.CreateServer window = new forms.CreateServer();
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
                //string line = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                serverList.Items.Clear();
                serverid.Clear();

                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                foreach(var item in jsonObject)
                {
                    serverid.Add(item.Key);
                    if (File.Exists(item.Value["base"].ToString() + "\\server-icon.png"))
                    {
                        serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), item.Value["base"].ToString() + "\\server-icon.png", "未运行", Brushes.Green));
                        StateCheck();
                    }
                    else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0 || item.Value["core"].ToString() == "")
                    {
                        serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Anvil.png", "未运行", Brushes.Green));
                        StateCheck();
                    }
                    else
                    {
                        serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Impulse_Command_Block.png", "未运行", Brushes.MediumSeaGreen));
                        StateCheck();
                    }
                }
            }
            catch
            {
                var mainwindow = (MainWindow)Window.GetWindow(this);
                DialogShow.ShowMsg(mainwindow, "开服器检测到配置文件出现了错误，是第一次使用吗？\n是否创建一个新的服务器？", "警告", true, "取消");
                if (MessageDialog._dialogReturn == true)
                {
                    Window wn = new forms.CreateServer();
                    wn.Owner = mainwindow;
                    wn.ShowDialog();
                    GetServerConfig();
                }
            }
        }
        
        private void serverList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            StartServerEvent();
        }
        

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            StartServerEvent();
        }

        void StartServerEvent()
        {
            if (serverList.SelectedIndex == -1)
            {
                return;
            }
            try
            {
                if (RunningServerIDs.IndexOf(serverid[serverList.SelectedIndex].ToString() + " ") + 1 != 0)
                {
                    MainWindow.serverid = serverid[serverList.SelectedIndex];
                    OpenServerForm();
                    return;
                }
                /*
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverid[serverList.SelectedIndex]];
                if (_json["core"].ToString().IndexOf("Bungeecord") + 1 != 0 || _json["core"].ToString().IndexOf("bungeecord") + 1 != 0)
                {
                    MessageBox.Show("开服器暂不支持Bungeecord服务端的运行，请右键点击“用命令行开服”选项来开服！");
                    return;
                }
                */
                ServerRunner runner = new ServerRunner
                {
                    RserverId = serverid[serverList.SelectedIndex],
                };
                /*
                runner.Rserverjava = _json["java"].ToString();
                runner.Rserverbase = _json["base"].ToString();
                runner.Rserverserver = _json["core"].ToString();
                runner.RserverJVM = _json["memory"].ToString();
                runner.RserverJVMcmd = _json["args"].ToString();
                */
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void setServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RunningServerIDs.IndexOf(serverid[serverList.SelectedIndex].ToString() + " ") + 1 != 0)
                {
                    MainWindow.serverid = serverid[serverList.SelectedIndex];
                    OpenServerForm();
                    return;
                }
                ServerRunner runner = new ServerRunner
                {
                    RserverId = serverid[serverList.SelectedIndex],
                };
                ControlSetServerTab = true;
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void delServer_Click(object sender, RoutedEventArgs e)
        {
            DelServerEvent();
        }
        void DelServerEvent()
        {
            var mainwindow = (MainWindow)Window.GetWindow(this);
            DialogShow.ShowMsg(mainwindow, "您确定要删除该服务器吗？", "提示", true, "取消");
            if (!MessageDialog._dialogReturn)
            {
                return;
            }
            MessageDialog._dialogReturn = false;
            ServerInfo _server = serverList.SelectedItem as ServerInfo;
            if (_server.ServerState == "运行中")
            {
                Growl.Error("服务器仍在运行中，请先关闭服务器！");
                return;
            }
            try
            {
                
                //serverList.Items.Remove(serverList.SelectedItem);
                DialogShow.ShowMsg(mainwindow, "是否删除该服务器的目录？（服务器目录中的所有文件都会被移至回收站）", "提示", true, "取消");
                if (MessageDialog._dialogReturn)
                {
                    //_server.ServerIcon = "/images/150px-Impulse_Command_Block.png";
                    MessageDialog._dialogReturn = false;
                    JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                    JObject _json = (JObject)jsonObject[serverid[serverList.SelectedIndex]];
                    FileSystem.DeleteDirectory(_json["base"].ToString(), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    //Directory.Delete(_json["base"].ToString(), true);
                    Growl.Success("服务器目录已成功移至回收站！");
                }
            }
            catch (Exception ex)
            {
                DialogShow.ShowMsg(mainwindow, "服务器目录删除失败！\n" + ex.Message, "警告", false, "确定");
            }
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                jsonObject.Remove(serverid[serverList.SelectedIndex]);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                Growl.Success("删除服务器成功！");
                GetServerConfig();
            }
            catch
            {
                Growl.Error("删除服务器失败！");
                DialogShow.ShowMsg(mainwindow, "服务器删除失败！", "警告", false, "确定");
                GetServerConfig();
            }
        }

        private void startWithCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverid[serverList.SelectedIndex]];
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + _json["args"] + " -jar \"" + _json["core"] + "\" nogui&pause&exit";
                Directory.SetCurrentDirectory(_json["base"].ToString());
                process.Start();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverid[serverList.SelectedIndex]];
                Growl.Info("正在为您打开服务器文件夹……");
                Process.Start(_json["base"].ToString());
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void setModorPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RunningServerIDs.IndexOf(serverid[serverList.SelectedIndex].ToString() + " ") + 1 != 0)
                {
                    MainWindow.serverid = serverid[serverList.SelectedIndex];
                    OpenServerForm();
                    return;
                }
                ServerRunner runner = new ServerRunner
                {
                    RserverId = serverid[serverList.SelectedIndex],
                };
                ControlSetPMTab = true;
                runner.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        void AutoOpenServer()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    serverList.Items.Clear();
                    serverid.Clear();

                    JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                    foreach (var item in jsonObject)
                    {
                        serverid.Add(item.Key);
                        if (File.Exists(item.Value["base"].ToString() + "\\server-icon.png"))
                        {
                            serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), item.Value["base"].ToString() + "\\server-icon.png", "未运行", Brushes.Green));
                            StateCheck();
                        }
                        else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0 || item.Value["core"].ToString() == "")
                        {
                            serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), "/images/150px-Anvil.png", "未运行", Brushes.Green));
                            StateCheck();
                        }
                        else
                        {
                            serverList.Items.Add(new ServerInfo(item.Value["name"].ToString(), "/images/150px-Impulse_Command_Block.png", "未运行", Brushes.MediumSeaGreen));
                            StateCheck();
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("err");
                }
                int i = 0;
                foreach (string x in serverid)
                {
                    if (x == MainWindow.serverid)
                    {
                        serverList.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
                try
                {
                    if (RunningServerIDs.IndexOf(serverid[serverList.SelectedIndex].ToString() + " ") + 1 != 0)
                    {
                        MainWindow.serverid = serverid[serverList.SelectedIndex];
                        OpenServerForm();
                        return;
                    }
                    ServerRunner runner = new ServerRunner
                    {
                        RserverId = serverid[serverList.SelectedIndex],
                    };
                    runner.Show();
                }
                catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
            }));
        }//the same of GetServerConfig and StartServerEvent

        private void serverList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverList.SelectedIndex != -1)
            {
                startServerBtn.IsEnabled = true;
                startWithCmd.IsEnabled = true;
                setServer.IsEnabled = true;
                setModorPlugin.IsEnabled = true;
                openServerDir.IsEnabled = true;
                delServer.IsEnabled = true;
            }
            else
            {
                startServerBtn.IsEnabled = false;
                startWithCmd.IsEnabled = false;
                setServer.IsEnabled = false;
                setModorPlugin.IsEnabled = false;
                openServerDir.IsEnabled = false;
                delServer.IsEnabled = false;
            }
        }

        private void startServerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ListViewItem item = FindAncestor<ListViewItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            StartServerEvent();
            // your code to handle the button click goes here...
        }
        private void delServerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ListViewItem item = FindAncestor<ListViewItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            DelServerEvent();
            // your code to handle the button click goes here...
        }
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            current = VisualTreeHelper.GetParent(current);

            while (current != null && !(current is T))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }
    }
}
