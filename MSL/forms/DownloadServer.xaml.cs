using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using File = System.IO.File;

namespace MSL.pages
{
    /// <summary>
    /// DownloadServer.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadServer : HandyControl.Controls.Window
    {
        public string downloadServerName = string.Empty;
        private readonly string downloadServerBase;
        private readonly string downloadServerJava;
        private readonly bool isInstallSomeCore;
        private string downPath = string.Empty;
        private string filename = string.Empty;
        private string downServer = string.Empty;
        private string downVersion = string.Empty;

        public DownloadServer(string _downloadServerBase, string _downloadServerJava, bool _isInstallSomeCore = true)
        {
            InitializeComponent();
            downloadServerBase = _downloadServerBase;
            downloadServerJava = _downloadServerJava;
            isInstallSomeCore = _isInstallSomeCore;
        }

        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (serverlist1.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(this, "请先选择一个版本！", "警告");
                return;
            }
            DownloadServerFunc();
        }

        private void serverlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverlist1.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(this, "请先选择一个版本！", "警告");
                return;
            }
            DownloadServerFunc();
        }
        private async void DownloadServerFunc()
        {

            downVersion = serverlist1.SelectedItem.ToString();
            downServer = serverlist.SelectedItem.ToString();

            if (serverlist1.SelectedIndex != -1)
            {
                string[] downContext = await HttpService.GetAsync("download/server/" + downServer + "/" + downVersion, "", 0, true);
                string downUrl = downContext[1];
                string sha256Exp = downContext[2];
                downPath = downloadServerBase;
                filename = downServer + "-" + downVersion + ".jar";
                if (downServer == "forge" || downServer == "spongeforge" || downServer == "neoforge")
                {
                    int dwnDialog = await Shows.ShowDownloaderWithIntReturn(this, downUrl, downPath, filename, "下载服务端中……", sha256Exp, true);
                    if (dwnDialog == 2)
                    {
                        Shows.ShowMsgDialog(this, "下载取消！（或服务端文件不存在）", "错误");
                        return;
                    }
                }
                else
                {
                    bool dwnDialog = await Shows.ShowDownloader(this, downUrl, downPath, filename, "下载服务端中……", sha256Exp);
                    if (!dwnDialog || !File.Exists(downPath + "\\" + filename))
                    {
                        Shows.ShowMsgDialog(this, "下载取消！（或服务端文件不存在）", "错误");
                        return;
                    }
                }

                if (downServer == "spongeforge")
                {
                    string forgeName = downServer.Replace("spongeforge", "forge");
                    string _filename = forgeName + ".jar";
                    string[] _dlContext = await HttpService.GetAsync("download/server/" + forgeName.Replace("-", "/"), "", 0, true);
                    string _dlUrl = _dlContext[1];
                    string _sha256Exp = _dlContext[2];
                    int _dwnDialog = await Shows.ShowDownloaderWithIntReturn(this, _dlUrl, downPath, _filename, "下载服务端中……", _sha256Exp, true);

                    if (_dwnDialog == 2)
                    {
                        Shows.ShowMsgDialog(this, "下载取消！", "提示");
                        return;
                    }

                    // Check if file exists and download succeeded
                    if (!File.Exists(downPath + "\\" + _filename))
                    {
                        // Extract version info and create backup URL
                        var query = new Uri(_dlUrl).Query;
                        var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
                        string mcVersion = queryDictionary["mcversion"];
                        string forgeVersion = queryDictionary["version"];
                        string[] components = mcVersion.Split('.');
                        string _mcMajorVersion = mcVersion;
                        if (components.Length >= 3 && int.TryParse(components[2], out int _))
                        {
                            _mcMajorVersion = $"{components[0]}.{components[1]}"; // remove the last component
                        }
                        if (new Version(_mcMajorVersion) < new Version("1.10"))
                        {
                            forgeVersion += "-" + mcVersion;
                        }
                        string backupUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/{forgeName}-{mcVersion}-{forgeVersion}-installer.jar";

                        // Attempt to download from backup URL
                        bool backupDownloadSuccess = await Shows.ShowDownloader(GetWindow(this), backupUrl, downPath, _filename, "备用链接下载中……", _sha256Exp);
                        if (!backupDownloadSuccess || !File.Exists(downPath + "\\" + _filename))
                        {
                            Shows.ShowMsgDialog(this, "下载取消！（或服务端文件不存在）", "错误");
                            return;
                        }
                    }
                    if (!isInstallSomeCore)
                    {
                        Shows.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                        return;
                    }
                    string installReturn = await InstallForge(_filename);
                    if (installReturn == null)
                    {
                        Shows.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    downloadServerName = installReturn;
                }
                else if (downServer == "neoforge")
                {
                    if (!File.Exists(downPath + "\\" + filename))
                    {
                        Shows.ShowMsgDialog(this, "下载失败！（或服务端文件不存在）", "提示");
                        return;
                    }
                    if (!isInstallSomeCore)
                    {
                        Shows.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                        return;
                    }
                    string installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        Shows.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    downloadServerName = installReturn;
                }
                else if (downServer == "forge")
                {
                    // Check if file exists and download succeeded
                    if (!File.Exists(downPath + "\\" + filename))
                    {
                        // Extract version info and create backup URL
                        var query = new Uri(downUrl).Query;
                        var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
                        string mcVersion = queryDictionary["mcversion"];
                        string forgeVersion = queryDictionary["version"];
                        string[] components = mcVersion.Split('.');
                        string _mcMajorVersion = mcVersion;
                        if (components.Length >= 3 && int.TryParse(components[2], out int _))
                        {
                            _mcMajorVersion = $"{components[0]}.{components[1]}"; // remove the last component
                        }
                        if (new Version(_mcMajorVersion) < new Version("1.10"))
                        {
                            forgeVersion += "-" + mcVersion;
                        }
                        string backupUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/{downServer}-{mcVersion}-{forgeVersion}-installer.jar";

                        // Attempt to download from backup URL
                        bool backupDownloadSuccess = await Shows.ShowDownloader(this, backupUrl, downPath, filename, "备用链接下载中……", sha256Exp);
                        if (!backupDownloadSuccess || !File.Exists(downPath + "\\" + filename))
                        {
                            Shows.ShowMsgDialog(this, "下载取消！（或服务端文件不存在）", "错误");
                            return;
                        }
                    }
                    if (!isInstallSomeCore)
                    {
                        Shows.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                        return;
                    }
                    string installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        Shows.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    downloadServerName = installReturn;
                }
                else if (downServer == "banner")
                {
                    if (!isInstallSomeCore)
                    {
                        Shows.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                        return;
                    }
                    //banner应当作为模组加载，所以要再下载一个fabric才是服务端
                    try
                    {
                        //移动到mods文件夹
                        Directory.CreateDirectory(downloadServerBase + "\\mods\\");
                        if (File.Exists(downloadServerBase + "\\mods\\" + filename))
                        {
                            File.Delete(downloadServerBase + "\\mods\\" + filename);
                        }
                        File.Move(downloadServerBase + "\\" + filename, downloadServerBase + "\\mods\\" + filename);
                    }
                    catch (Exception e)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Shows.ShowMsgDialog(this, "Banner端移动失败！\n请重试！" + e.Message, "错误");
                        });
                        return;
                    }

                    //下载一个fabric端
                    //获取版本号
                    string bannerVersion = filename.Replace("banner-", "").Replace(".jar", "");
                    await Dispatcher.Invoke(async () =>
                    {
                        bool dwnFabric = await Shows.ShowDownloader(GetWindow(this), HttpService.Get("download/server/fabric/" + bannerVersion), downloadServerBase, $"fabric-{bannerVersion}.jar", "下载Fabric端中···");
                        if (!dwnFabric || !File.Exists(downloadServerBase + "\\" + $"fabric-{bannerVersion}.jar"))
                        {
                            Shows.ShowMsgDialog(this, "Fabric端下载取消（或服务端文件不存在）！", "错误");
                            return;
                        }
                    });

                    downloadServerName = $"fabric-{bannerVersion}.jar";
                }
                else
                {
                    if (!isInstallSomeCore)
                    {
                        Shows.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                        return;
                    }
                    downloadServerName = filename;
                }
                Close();
            }
        }

        private async Task<string> InstallForge(string filename)
        {
            //调用新版forge安装器
            string[] installForge = await Shows.ShowInstallForge(this, downPath + "\\" + filename, downPath, downloadServerJava);
            if (installForge[0] == "0")
            {
                if (await Shows.ShowMsgDialogAsync(this, "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
                {
                    return Functions.InstallForge(downloadServerJava, downloadServerBase, filename, string.Empty, false);
                }
                else
                {
                    return null;
                }
            }
            else if (installForge[0] == "1")
            {
                return Functions.InstallForge(downloadServerJava, downloadServerBase, filename, installForge[1]);
            }
            else
            {
                return null;
            }
        }

        private void GetServer()
        {
            Dispatcher.Invoke(() =>
            {
                serverlist.ItemsSource = null;
                serverlist1.ItemsSource = null;
            });
            try
            {
                string jsonData = HttpService.Get("query/available_server_types");
                string[] serverTypes = JsonConvert.DeserializeObject<string[]>(jsonData);
                Dispatcher.Invoke(() =>
                {
                    serverlist.ItemsSource = serverTypes;

                    serverlist.SelectedIndex = 0;
                    getservermsg.Visibility = Visibility.Hidden;
                    lCircle.Visibility = Visibility.Hidden;
                });
            }
            catch (Exception a)
            {
                Dispatcher.Invoke(() =>
                {
                    getservermsg.Text = "获取服务端失败！请重试" + a.Message;
                    lCircle.Visibility = Visibility.Hidden;
                });
            }
        }

        private void serverlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverlist.Items.Count != 0)
            {
                Thread thread = new Thread(GetServerVersionList);
                thread.Start();
            }
        }
        void GetServerVersionList()
        {
            try
            {
                string serverName = "paper";
                Dispatcher.Invoke(() =>
                {
                    serverlist1.ItemsSource = null;
                    //serverurl.Clear();
                    getservermsg.Visibility = Visibility.Visible;
                    lCircle.Visibility = Visibility.Visible;
                    serverName = serverlist.SelectedItem.ToString();
                    //serverName = serverlist.SelectedItem.ToString();
                });
                try
                {
                    var resultData = HttpService.Get("query/available_versions/" + serverName);
                    string server_des = HttpService.Get("query/servers_description/" + serverName);
                    JArray serverVersions = JArray.Parse(resultData);
                    List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                    Dispatcher.Invoke(() =>
                    {
                        serverlist1.ItemsSource = sortedVersions;
                        getservermsg.Visibility = Visibility.Hidden;
                        lCircle.Visibility = Visibility.Hidden;
                        server_d.Text = "你选择的是：" + server_des;
                    });

                }
                catch
                {
                    try
                    {
                        var resultData = HttpService.Get("query/available_versions/" + serverName);
                        JArray serverVersions = JArray.Parse(resultData);
                        List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                        Dispatcher.Invoke(() =>
                        {
                            serverlist1.ItemsSource = sortedVersions;
                            getservermsg.Visibility = Visibility.Hidden;
                            lCircle.Visibility = Visibility.Hidden;
                        });

                    }
                    catch (Exception a)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            getservermsg.Text = "获取服务端失败！请重试" + a.Message;
                            lCircle.Visibility = Visibility.Hidden;
                        });
                    }
                }
            }
            catch (Exception a)
            {
                Dispatcher.Invoke(() =>
                {
                    getservermsg.Text = "获取服务端失败！请重试" + a.Message;
                    lCircle.Visibility = Visibility.Hidden;
                });
            }
        }

        private void openChooseServerDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/other/choose-server-tips.html");
        }

        private void openSpigot_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.spigotmc.org/");
        }

        private void openPaper_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://papermc.io/");
        }

        private void openMojang_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.minecraft.net/zh-hans/download/server");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(GetServer);
            thread.Start();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(GetServer);
            thread.Start();
        }
    }
}
