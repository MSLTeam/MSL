using HandyControl.Controls;
using Microsoft.VisualBasic.FileIO;
using MSL.controls;
using MSL.langs;
using MSL.utils;
using MSL.utils.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
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
        public static event App.DeleControl CreateServerEvent;
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
            Growl.Success(Lang.Page_ServerList_RefreshSuccess);
        }

        private async void GetServerConfig()
        {
            LogHelper.Write.Info("开始获取并加载服务器配置列表。");
            Dispatcher.Invoke(() =>
            {
                serverList.ItemsSource = null;
                serverList.Items.Clear();
            });
            if (ServerConfig.Current.Count == 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MagicFlowMsg.ShowMessage(Lang.Page_ServerList_NoServer);
                });
                return;
            }
            try
            {
                List<object> list = new List<object>();

                foreach (var item in ServerConfig.Current.All)
                {
                    var serverBase = item.Value.Base ?? string.Empty;
                    var serverName = item.Value.Name ?? string.Empty;
                    var serverCore = item.Value.Core ?? string.Empty;

                    string status = Lang.Page_ServerList_StatusNotRunning;
                    Brush brushes = Brushes.MediumSeaGreen;
                    if (RunningServers.Contains(int.Parse(item.Key)))
                    {
                        status = Lang.Page_ServerList_StatusRunning;
                        brushes = Brushes.Orange;
                    }
                    if (File.Exists(serverBase + "\\server-icon.png"))
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), serverName, serverBase + "\\server-icon.png", status, brushes));
                    }
                    else if (serverCore.IndexOf("neoforge") + 1 != 0)
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), serverName, "pack://application:,,,/images/neoforged.png", status, brushes));
                    }
                    else if (serverCore.IndexOf("forge") + 1 != 0)
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), serverName, "pack://application:,,,/images/150px-Anvil.png", status, brushes));
                    }
                    else if (string.IsNullOrEmpty(serverCore))
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), serverName, "pack://application:,,,/images/150px-MinecartWithCommandBlock.png", status, brushes));
                    }
                    else
                    {
                        list.Add(new SL_ServerInfo(int.Parse(item.Key), serverName, "pack://application:,,,/images/150px-Allium.png", status, brushes));
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
                MessageBox.Show(Lang.Page_ServerList_ErrorNoSelection + "\n" + ex.Message);
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
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Page_ServerList_Dialog_CloseFirst, Lang.Warning);
                return;
            }
            bool dialogRet = await MagicShow.ShowMsgDialogAsync(Functions.GetWindow(this), Lang.Page_ServerList_Dialog_ConfirmDelete, Lang.Tip, true, Lang.Cancel, isDangerPrimaryBtn: true);
            if (!dialogRet)
            {
                LogHelper.Write.Info($"用户取消了删除服务器ID: {serverID} 的操作。");
                return;
            }

            try
            {
                bool _dialogRet = await MagicShow.ShowMsgDialogAsync(Functions.GetWindow(this), Lang.Page_ServerList_Dialog_DeleteDir, Lang.Tip, true, Lang.Cancel, isDangerPrimaryBtn: true);
                if (_dialogRet)
                {
                    LogHelper.Write.Info($"用户确认删除服务器ID: {serverID} 的文件目录。");
                    ServerConfig.Current.TryGet(serverID.ToString(), out var instance);
                    string serverPath = instance.Base;
                    FileSystem.DeleteDirectory(serverPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    LogHelper.Write.Info($"已将服务器ID: {serverID} 的目录 '{serverPath}' 发送到回收站。");
                    //Directory.Delete(_json["base"].ToString(), true);
                    Growl.Success(Lang.Page_ServerList_DirDeleted);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"删除服务器ID: {serverID} 的目录失败: {ex.ToString()}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Page_ServerList_DirDeleteFailed + "\n" + ex.Message, Lang.Warning);
            }
            try
            {
                ServerConfig.Current.Remove(serverID.ToString());
                ServerConfig.Current.Save();
                LogHelper.Write.Info($"已成功从 ServerList.json 中移除服务器ID: {serverID} 的配置。");
                Growl.Success(Lang.Page_ServerList_Deleted);
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"从 ServerList.json 中删除服务器ID: {serverID} 的配置失败: {ex.ToString()}");
                Growl.Error(Lang.Page_ServerList_DeleteFailed);
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Page_ServerList_DeleteFailed, Lang.Warning);
            }
            finally
            {
                ServerConfig.Current.Save();
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
                ServerConfig.Current.TryGet(serverID, out var instance);
                Process process = new Process();
                process.StartInfo.WorkingDirectory = instance.Base;
                process.StartInfo.FileName = "cmd.exe";
                string arguments,yggapi_cmd = "";
                //检测外置登录（如果文件不存在就算了）
                if (!string.IsNullOrEmpty(instance.YggApi?.ToString() ?? ""))
                {
                    if (File.Exists(Path.Combine(instance.Base, "authlib-injector.jar")))
                    {
                        yggapi_cmd = $"-javaagent:authlib-injector.jar={instance.YggApi} ";
                    }
                    else
                    {
                        Growl.Warning(Lang.Page_ServerList_YggWarning);
                    }  
                }
                if (instance.Core.StartsWith("@libraries/"))
                {
                    arguments = "/K " + "@ \"" + instance.Java + "\" " + instance.Memory + " " + yggapi_cmd + instance.Args + " " + instance.Core + " nogui&pause&exit";
                }
                else
                {
                    arguments = "/K " + "@ \"" + instance.Java + "\" " + instance.Memory + " " + yggapi_cmd + instance.Args + " -jar \"" + instance.Core + "\" nogui&pause&exit";
                }
                process.StartInfo.Arguments = arguments;
                process.Start();
                LogHelper.Write.Info($"已成功为服务器ID: {serverID} 创建CMD进程。工作目录: {instance.Base}，启动参数: {arguments}");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"使用CMD启动服务器时出错: {ex.ToString()}");
                MessageBox.Show(Lang.Page_ServerList_ErrorNoSelection + "\n" + ex.Message);
            }
        }

        private void openServerDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SL_ServerInfo SL_ServerInfo = serverList.SelectedItem as SL_ServerInfo;
                string serverID = SL_ServerInfo.ServerID.ToString();
                LogHelper.Write.Info($"用户请求打开服务器ID: {serverID} 的文件夹。");
                ServerConfig.Current.TryGet(serverID, out var instance);
                string path = instance.Base;
                Growl.Info(Lang.Page_ServerList_OpeningFolder);
                Process.Start(path);
                LogHelper.Write.Info($"已成功打开服务器ID: {serverID} 的文件夹，路径: {path}");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"打开服务器文件夹时出错: {ex.ToString()}");
                MessageBox.Show(Lang.Page_ServerList_ErrorNoSelection + "\n" + ex.Message);
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
                MessageBox.Show(Lang.Page_ServerList_ErrorNoSelection + "\n" + ex.Message);
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
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Lang.Page_ServerList_DownloadModTip, Lang.Tip);
            LogHelper.Write.Info("用户点击'下载模组/插件'按钮，打开独立下载窗口。");
            var tempContent = this.Content;
            DownloadMod downloadModPage = null;
            downloadModPage = new DownloadMod((string filename) =>
            {
                this.Content = tempContent;
                downloadModPage.Dispose();
                downloadModPage = null;
            }, "MSL\\Downloads");

            this.Content = downloadModPage;
        }

        private async void DlServerCoreBtn_Click(object sender, RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Lang.Page_ServerList_DownloadCoreTip, Lang.Tip);
            LogHelper.Write.Info("用户点击'下载服务端'按钮，打开独立下载窗口。");
            var tempContent = this.Content;
            DownloadServer downloadServerPage = null;
            downloadServerPage = new DownloadServer((string filename) =>
            {
                this.Content = tempContent;
                downloadServerPage.Dispose();
                downloadServerPage = null;
            }, "MSL\\Downloads", DownloadServer.Mode.FreeDownload);

            this.Content = downloadServerPage;
            /*
            
            DownloadServer downloadServer = new DownloadServer("MSL\\Downloads", DownloadServer.Mode.FreeDownload)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            downloadServer.Show();
            */
        }
    }
}