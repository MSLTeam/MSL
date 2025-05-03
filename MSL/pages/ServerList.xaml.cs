using HandyControl.Controls;
using Microsoft.VisualBasic.FileIO;
using MSL.controls;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Window = System.Windows.Window;

namespace MSL.pages
{

    /// <summary>
    /// Cmdoutlog.xaml 的交互逻辑
    /// </summary>
    public partial class ServerList : Page
    {
        public static event DeleControl CreateServerEvent;
        public static int ServerID;
        public static List<int> RunningServers = new List<int>();
        public static Dictionary<int, Window> ServerWindowList = new Dictionary<int, Window>();
        //private static readonly List<int> serverIDs = new List<int>();

        public ServerList()
        {
            InitializeComponent();
            ServerRunner.SaveConfigEvent += GetServerConfig;
            ServerRunner.ServerStateChange += GetServerConfig;
            MainWindow.AutoOpenServer += AutoOpenServer;
            Home.AutoOpenServer += AutoOpenServer;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            CreateServerEvent();
        }

        private void refreshList_Click(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
            Growl.Success("刷新成功！");
        }

        private async void GetServerConfig()
        {
            try
            {
                List<object> list = new List<object>();
                //serverIDs.Clear();

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                foreach (var item in jsonObject)
                {
                    string status = "未运行";
                    Brush brushes = Brushes.MediumSeaGreen;
                    if (RunningServers.Contains(int.Parse(item.Key)))
                    {
                        status = "运行中";
                        brushes = Brushes.Orange;
                    }
                    if (File.Exists(item.Value["base"].ToString() + "\\server-icon.png"))
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), item.Value["base"].ToString() + "\\server-icon.png", status, brushes));
                    }
                    else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0 || item.Value["core"].ToString() == "")
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/150px-Anvil.png", status, brushes));
                    }
                    else
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/150px-Impulse_Command_Block.png", status, brushes));
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    serverList.ItemsSource = list;
                });
            }
            catch
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    bool dialogRet = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), LanguageManager.Instance["Page_ServerList_Dialog_NoConfTip"], LanguageManager.Instance["Warning"], true, LanguageManager.Instance["Cancel"]);
                    if (dialogRet)
                    {
                        CreateServerEvent();
                    }
                });
            }
        }

        private void serverList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverList.SelectedIndex == -1)
            {
                return;
            }
            OpenServerWindowEvent();
        }

        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            OpenServerWindowEvent();
        }

        private void OpenServerWindowEvent(short ctrlTab = 0)
        {
            SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
            int serverID = SL_ServerInfo.ServerID;
            if (ServerWindowList.ContainsKey(serverID))
            {
                ServerWindowList.TryGetValue(serverID, out Window window);
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }
                window.Visibility = Visibility.Visible;
                window.Topmost = true;
                window.Topmost = false;
                window.Focus();
            }
            else
            {
                Window window = new ServerRunner(serverID, ctrlTab);
                ServerWindowList.Add(serverID, window);
                window.Show();
            }
        }

        /*
        private ServerRunner CheckServerRunStatus(int tabCtrl = 0)
        {
            if (runningServers.ContainsKey(serverIDs[serverList.SelectedIndex]))
            {
                serverID = serverIDs[serverList.SelectedIndex];
                OpenServerForm();
                return null;
            }
            else
            {
                ServerRunner runner = new ServerRunner(serverIDs[serverList.SelectedIndex], tabCtrl);
                return runner;
            }
        }
        

        private void StartServerEvent()
        {
            try
            {
                var runner = CheckServerRunStatus();
                runner?.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }
        */

        private void setServer_Click(object sender, RoutedEventArgs e)
        {
            OpenServerWindowEvent(3);
        }

        private void SetServerEvent()
        {
            try
            {
                OpenServerWindowEvent(3);
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void delServer_Click(object sender, RoutedEventArgs e)
        {
            DelServerEvent();
        }

        private async void DelServerEvent()
        {
            SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
            int serverID = SL_ServerInfo.ServerID;
            if (ServerWindowList.ContainsKey(serverID))
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请在关闭服务器并关掉服务器窗口后再进行删除！", "警告");
                return;
            }
            bool dialogRet = await MagicShow.ShowMsgDialogAsync("您确定要删除该服务器吗？", "提示", true, "取消", isDangerPrimaryBtn: true);
            if (!dialogRet)
            {
                return;
            }
            //SL_ServerInfo _server = serverList.SelectedItem as SL_ServerInfo;
            try
            {
                bool _dialogRet = await MagicShow.ShowMsgDialogAsync("是否删除该服务器的目录？（服务器目录中的所有文件都会被移至回收站）", "提示", true, "取消", isDangerPrimaryBtn: true);
                if (_dialogRet)
                {
                    JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                    JObject _json = (JObject)jsonObject[serverID.ToString()];
                    FileSystem.DeleteDirectory(_json["base"].ToString(), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    //Directory.Delete(_json["base"].ToString(), true);
                    Growl.Success("服务器目录已成功移至回收站！");
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "服务器目录删除失败！\n" + ex.Message, "警告");
            }
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                jsonObject.Remove(serverID.ToString());
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                Growl.Success("删除服务器成功！");
                GetServerConfig();
            }
            catch
            {
                Growl.Error("删除服务器失败！");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "服务器删除失败！", "警告");
                GetServerConfig();
            }
        }

        private void startWithCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
                string serverID = SL_ServerInfo.ServerID.ToString();
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverID];
                Process process = new Process();
                process.StartInfo.WorkingDirectory = _json["base"].ToString();
                process.StartInfo.FileName = "cmd.exe";
                if (_json["core"].ToString().StartsWith("@libraries/"))
                {
                    process.StartInfo.Arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + _json["args"] + " " + _json["core"] + " nogui&pause&exit";
                }
                else
                {
                    process.StartInfo.Arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + _json["args"] + " -jar \"" + _json["core"] + "\" nogui&pause&exit";
                }
                process.Start();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
                string serverID = SL_ServerInfo.ServerID.ToString();
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverID];
                Growl.Info("正在为您打开服务器文件夹……");
                Process.Start(_json["base"].ToString());
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void setModorPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenServerWindowEvent(2);
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void AutoOpenServer()
        {
            if (ServerWindowList.ContainsKey(ServerID))
            {
                ServerWindowList.TryGetValue(ServerID, out Window window);
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }
                window.Visibility = Visibility.Visible;
                window.Topmost = true;
                window.Topmost = false;
                window.Focus();
            }
            else
            {
                Window window = new ServerRunner(ServerID);
                ServerWindowList.Add(ServerID, window);
                window.Show();
            }
        }

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
                ListBoxItem item = FindAncestor<ListBoxItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            OpenServerWindowEvent();
        }
        private void setServerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ListBoxItem item = FindAncestor<ListBoxItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            SetServerEvent();
        }
        private void delServerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ListBoxItem item = FindAncestor<ListBoxItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            DelServerEvent();
        }
        public static T FindAncestor<T>(System.Windows.DependencyObject current) where T : System.Windows.DependencyObject
        {
            current = VisualTreeHelper.GetParent(current);

            while (current != null && !(current is T))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }

        //单独的下载按钮
        private async void DlModBtn_Click(object sender, RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "这是单独的模组/插件/整合包下载界面\n下载的文件均在MSL\\Downloads文件夹内", "提示");
            DownloadMod downloadMod = new DownloadMod("MSL\\Downloads"){
                Owner = Window.GetWindow(Window.GetWindow(this))
            };
            downloadMod.ShowDialog();
        }

        private async void DlServerCoreBtn_Click(object sender, RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "这是单独的服务端下载界面\n下载的服务端均在MSL\\Downloads文件夹内", "提示");
            DownloadServer downloadServer = new DownloadServer("MSL\\Downloads", DownloadServer.Mode.FreeDownload)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            downloadServer.Show();
        }
    }
}
