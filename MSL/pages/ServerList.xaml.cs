using HandyControl.Controls;
using Microsoft.VisualBasic.FileIO;
using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        public static event DeleControl OpenServerForm;
        public static event DeleControl CreateServerEvent;
        public static string serverID;
        public static List<string> serverIDs = new List<string>();
        public static Dictionary<string, int> runningServers = new Dictionary<string, int>();

        public ServerList()
        {
            InitializeComponent();
            ServerRunner.SaveConfigEvent += RefreshConfig;
            ServerRunner.ServerStateChange += RefreshConfig;
            MainWindow.AutoOpenServer += AutoOpenServer;
            Home.AutoOpenServer += AutoOpenServer;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
        }

        private void RefreshConfig()
        {
            GetServerConfig();
        }

        private void StateCheck(List<object> list)
        {
            for (int id = 0; id < list.Count; id++)
            {
                ServerInfo _server = list[id] as ServerInfo;
                if (runningServers.TryGetValue(serverIDs[id], out int status))
                {
                    if (status == 1)
                    {
                        _server.ServerState = "运行中";
                        _server.ServerStateFore = Brushes.Red;
                        continue;
                    }
                }
                _server.ServerState = "未运行";
                _server.ServerStateFore = Brushes.MediumSeaGreen;
            }
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            //serverList.Items.Clear();
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
                //serverList.Items.Clear();
                List<object> list = new List<object>();
                serverIDs.Clear();

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                foreach (var item in jsonObject)
                {
                    serverIDs.Add(item.Key);
                    if (File.Exists(item.Value["base"].ToString() + "\\server-icon.png"))
                    {
                        //await Dispatcher.InvokeAsync(() =>
                        //{
                        list.Add(new ServerInfo(item.Value["name"].ToString(), item.Value["base"].ToString() + "\\server-icon.png", "未运行", Brushes.Green));
                        StateCheck(list);
                        //});
                    }
                    else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0 || item.Value["core"].ToString() == "")
                    {
                        //await Dispatcher.InvokeAsync(() =>
                        //{
                        list.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Anvil.png", "未运行", Brushes.Green));
                        StateCheck(list);
                        //});
                    }
                    else
                    {
                        //await Dispatcher.InvokeAsync(() =>
                        //{
                        list.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Impulse_Command_Block.png", "未运行", Brushes.MediumSeaGreen));
                        StateCheck(list);
                        //});
                    }
                }
                serverList.ItemsSource = list;
            }
            catch
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    bool dialogRet = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), LanguageManager.Instance["Pages_ServerList_Dialog_FirstUse"], LanguageManager.Instance["Dialog_Warning"], true, LanguageManager.Instance["Dialog_Cancel"]);
                    if (dialogRet)
                    {
                        CreateServerEvent();
                    }
                });
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

        private void setServer_Click(object sender, RoutedEventArgs e)
        {
            SetServerEvent();
        }

        private void SetServerEvent()
        {
            try
            {
                var runner = CheckServerRunStatus(3);
                runner?.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void delServer_Click(object sender, RoutedEventArgs e)
        {
            DelServerEvent();
        }

        private async void DelServerEvent()
        {
            if (runningServers.ContainsKey(serverIDs[serverList.SelectedIndex]))
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请先关闭服务器窗口再进行删除！", "警告");
                return;
            }
            bool dialogRet = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您确定要删除该服务器吗？", "提示", true, "取消");
            if (!dialogRet)
            {
                return;
            }
            //ServerInfo _server = serverList.SelectedItem as ServerInfo;
            try
            {
                bool _dialogRet = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "是否删除该服务器的目录？（服务器目录中的所有文件都会被移至回收站）", "提示", true, "取消");
                if (_dialogRet)
                {
                    JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                    JObject _json = (JObject)jsonObject[serverIDs[serverList.SelectedIndex].ToString()];
                    FileSystem.DeleteDirectory(_json["base"].ToString(), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    //Directory.Delete(_json["base"].ToString(), true);
                    Growl.Success("服务器目录已成功移至回收站！");
                }
            }
            catch (Exception ex)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "服务器目录删除失败！\n" + ex.Message, "警告");
            }
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                jsonObject.Remove(serverIDs[serverList.SelectedIndex].ToString());
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                Growl.Success("删除服务器成功！");
                GetServerConfig();
            }
            catch
            {
                Growl.Error("删除服务器失败！");
                Shows.ShowMsgDialog(Window.GetWindow(this), "服务器删除失败！", "警告");
                GetServerConfig();
            }
        }

        private void startWithCmd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverIDs[serverList.SelectedIndex].ToString()];
                Process process = new Process();
                process.StartInfo.WorkingDirectory = _json["base"].ToString();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + _json["args"] + " -jar \"" + _json["core"] + "\" nogui&pause&exit";
                process.Start();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverIDs[serverList.SelectedIndex].ToString()];
                Growl.Info("正在为您打开服务器文件夹……");
                Process.Start(_json["base"].ToString());
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void setModorPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var runner = CheckServerRunStatus(2);
                //ControlSetPMTab = true;
                runner?.Show();
            }
            catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
        }

        private void AutoOpenServer()
        {
            try
            {
                //serverList.Items.Clear();
                List<object> list = new List<object>();
                serverIDs.Clear();

                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                foreach (var item in jsonObject)
                {
                    serverIDs.Add(item.Key);
                    if (File.Exists(item.Value["base"].ToString() + "\\server-icon.png"))
                    {
                        list.Add(new ServerInfo(item.Value["name"].ToString(), item.Value["base"].ToString() + "\\server-icon.png", "未运行", Brushes.Green));
                        StateCheck(list);
                    }
                    else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0 || item.Value["core"].ToString() == "")
                    {
                        list.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Anvil.png", "未运行", Brushes.Green));
                        StateCheck(list);
                    }
                    else
                    {
                        list.Add(new ServerInfo(item.Value["name"].ToString(), "pack://application:,,,/images/150px-Impulse_Command_Block.png", "未运行", Brushes.MediumSeaGreen));
                        StateCheck(list);
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    serverList.ItemsSource = list;
                });
            }
            catch
            {
                MessageBox.Show("err");
            }
            Dispatcher.Invoke(() =>
            {
                int i = 0;
                foreach (string x in serverIDs)
                {
                    if (x == serverID)
                    {
                        serverList.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
                try
                {
                    var runner = CheckServerRunStatus();
                    runner?.Show();
                }
                catch (Exception ex) { MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message); }
            });
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
        }
        private void setServerBtn_Click(object sender, RoutedEventArgs e)
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
            SetServerEvent();
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
        private async void DlServerCoreBtn_Click(object sender, RoutedEventArgs e)
        {
            await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "这是单独的服务端下载界面\n下载的服务端均在MSL文件夹下的ServerCores文件夹", "提示");
            DownloadServer downloadServer = new DownloadServer("MSL\\ServerCores\\", "", false)
            {
                Owner = Window.GetWindow(Window.GetWindow(this))
            };
            downloadServer.ShowDialog();
        }
    }
}
