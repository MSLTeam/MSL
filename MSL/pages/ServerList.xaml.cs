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
            LogHelper.Write.Info("服务器列表页面已加载。");
            GetServerConfig();
        }

        private void addServer_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户点击'添加服务器'按钮。");
            CreateServerEvent();
        }

        private void refreshList_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户请求刷新服务器列表。");
            GetServerConfig();
            Growl.Success("刷新成功！");
        }

        private async void GetServerConfig()
        {
            LogHelper.Write.Info("开始获取并加载服务器配置列表。");
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
                    else if (item.Value["core"].ToString().IndexOf("neoforge") + 1 != 0)
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/neoforged.png", status, brushes));
                    }
                    else if (item.Value["core"].ToString().IndexOf("forge") + 1 != 0)
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/150px-Anvil.png", status, brushes));
                    }
                    else if (item.Value["core"].ToString() == "")
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/150px-MinecartWithCommandBlock.png", status, brushes));
                    }
                    else
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), item.Value["name"].ToString(), "pack://application:,,,/images/150px-Allium.png", status, brushes));
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    serverList.ItemsSource = list;
                });
                LogHelper.Write.Info($"成功加载了 {list.Count} 个服务器配置。");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取服务器配置失败，可能是ServerList.json文件不存在或格式错误。详细信息: {ex.ToString()}");
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
            if (SL_ServerInfo == null)
            {
                LogHelper.Write.Warn("尝试打开服务器窗口，但未选择任何服务器。");
                return;
            }
            int serverID = SL_ServerInfo.ServerID;
            LogHelper.Write.Info($"准备打开服务器ID: {serverID} 的管理窗口。");
            if (ServerWindowList.ContainsKey(serverID))
            {
                LogHelper.Write.Info($"服务器ID: {serverID} 的窗口已存在，将激活现有窗口。");
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
                LogHelper.Write.Info($"为服务器ID: {serverID} 创建新的管理窗口。");
                Window window = new ServerRunner(serverID, ctrlTab);
                ServerWindowList.Add(serverID, window);
                window.Show();
            }
        }

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
            catch (Exception ex)
            {
                LogHelper.Write.Error($"打开服务器设置时出错: {ex.ToString()}");
                MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message);
            }
        }

        private void delServer_Click(object sender, RoutedEventArgs e)
        {
            DelServerEvent();
        }

        private async void DelServerEvent()
        {
            SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
            if (SL_ServerInfo == null)
            {
                LogHelper.Write.Warn("尝试删除服务器，但未选择任何服务器。");
                return;
            }
            int serverID = SL_ServerInfo.ServerID;
            LogHelper.Write.Info($"用户请求删除服务器ID: {serverID}。");

            if (ServerWindowList.ContainsKey(serverID))
            {
                LogHelper.Write.Warn($"试图删除一个仍在运行或窗口未关闭的服务器 (ID: {serverID})，操作被中止。");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请在关闭服务器并关掉服务器窗口后再进行删除！", "警告");
                return;
            }
            bool dialogRet = await MagicShow.ShowMsgDialogAsync("您确定要删除该服务器吗？", "提示", true, "取消", isDangerPrimaryBtn: true);
            if (!dialogRet)
            {
                LogHelper.Write.Info($"用户取消了删除服务器ID: {serverID} 的操作。");
                return;
            }
            //SL_ServerInfo _server = serverList.SelectedItem as SL_ServerInfo;
            try
            {
                bool _dialogRet = await MagicShow.ShowMsgDialogAsync("是否删除该服务器的目录？（服务器目录中的所有文件都会被移至回收站）", "提示", true, "取消", isDangerPrimaryBtn: true);
                if (_dialogRet)
                {
                    LogHelper.Write.Info($"用户确认删除服务器ID: {serverID} 的文件目录。");
                    JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                    JObject _json = (JObject)jsonObject[serverID.ToString()];
                    string serverPath = _json["base"].ToString();
                    FileSystem.DeleteDirectory(serverPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    LogHelper.Write.Info($"已将服务器ID: {serverID} 的目录 '{serverPath}' 发送到回收站。");
                    //Directory.Delete(_json["base"].ToString(), true);
                    Growl.Success("服务器目录已成功移至回收站！");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"删除服务器ID: {serverID} 的目录失败: {ex.ToString()}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "服务器目录删除失败！\n" + ex.Message, "警告");
            }
            try
            {
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                jsonObject.Remove(serverID.ToString());
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                LogHelper.Write.Info($"已成功从 ServerList.json 中移除服务器ID: {serverID} 的配置。");
                Growl.Success("删除服务器成功！");
                GetServerConfig();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"从 ServerList.json 中删除服务器ID: {serverID} 的配置失败: {ex.ToString()}");
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
                LogHelper.Write.Info($"用户请求使用CMD启动服务器ID: {serverID}。");
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverID];
                Process process = new Process();
                process.StartInfo.WorkingDirectory = _json["base"].ToString();
                process.StartInfo.FileName = "cmd.exe";
                string arguments,yggapi_cmd = "";
                //检测外置登录（如果文件不存在就算了）
                if (!string.IsNullOrEmpty(_json["ygg_api"]?.ToString() ?? ""))
                {
                    if (File.Exists(Path.Combine(_json["base"].ToString(), "authlib-injector.jar")))
                    {
                        yggapi_cmd = $"-javaagent:authlib-injector.jar={_json["ygg_api"]?.ToString()} ";
                    }
                    else
                    {
                        Growl.Warning("您配置了外置登录但是外置登录库并未下载\n如需正常使用外置登录+命令行开服，请先在MSL内正常开服一次！");
                    }  
                }
                if (_json["core"].ToString().StartsWith("@libraries/"))
                {
                    arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + yggapi_cmd + _json["args"] + " " + _json["core"] + " nogui&pause&exit";
                }
                else
                {
                    arguments = "/K " + "@ \"" + _json["java"] + "\" " + _json["memory"] + " " + yggapi_cmd + _json["args"] + " -jar \"" + _json["core"] + "\" nogui&pause&exit";
                }
                process.StartInfo.Arguments = arguments;
                process.Start();
                LogHelper.Write.Info($"已成功为服务器ID: {serverID} 创建CMD进程。工作目录: {_json["base"]}，启动参数: {arguments}");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"使用CMD启动服务器时出错: {ex.ToString()}");
                MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message);
            }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
                string serverID = SL_ServerInfo.ServerID.ToString();
                LogHelper.Write.Info($"用户请求打开服务器ID: {serverID} 的文件夹。");
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                JObject _json = (JObject)jsonObject[serverID];
                string path = _json["base"].ToString();
                Growl.Info("正在为您打开服务器文件夹……");
                Process.Start(path);
                LogHelper.Write.Info($"已成功打开服务器ID: {serverID} 的文件夹，路径: {path}");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"打开服务器文件夹时出错: {ex.ToString()}");
                MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message);
            }
        }

        private void setModorPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenServerWindowEvent(2);
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"打开服务器Mod/插件管理时出错: {ex.ToString()}");
                MessageBox.Show("出现错误，请检查您是否选择了服务器！\n" + ex.Message);
            }
        }

        private void AutoOpenServer()
        {
            LogHelper.Write.Info($"正在通过自动打开功能启动服务器ID: {ServerID}。");
            if (ServerWindowList.ContainsKey(ServerID))
            {
                LogHelper.Write.Info($"自动打开：服务器ID: {ServerID} 的窗口已存在，将激活现有窗口。");
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
                LogHelper.Write.Info($"自动打开：为服务器ID: {ServerID} 创建新的管理窗口。");
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
                ListBoxItem item = Functions.FindAncestor<ListBoxItem>(btn);
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
                ListBoxItem item = Functions.FindAncestor<ListBoxItem>(btn);
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
                ListBoxItem item = Functions.FindAncestor<ListBoxItem>(btn);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
            DelServerEvent();
        }

        //单独的下载按钮
        private async void DlModBtn_Click(object sender, RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "这是单独的模组/插件/整合包下载界面\n下载的文件均在MSL\\Downloads文件夹内", "提示");
            LogHelper.Write.Info("用户点击'下载模组/插件'按钮，打开独立下载窗口。");
            DownloadMod downloadMod = new DownloadMod("MSL\\Downloads")
            {
                Owner = Window.GetWindow(Window.GetWindow(this))
            };
            downloadMod.ShowDialog();
        }

        private async void DlServerCoreBtn_Click(object sender, RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "这是单独的服务端下载界面\n下载的服务端均在MSL\\Downloads文件夹内", "提示");
            LogHelper.Write.Info("用户点击'下载服务端'按钮，打开独立下载窗口。");
            DownloadServer downloadServer = new DownloadServer("MSL\\Downloads", DownloadServer.Mode.FreeDownload)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            downloadServer.Show();
        }
    }
}