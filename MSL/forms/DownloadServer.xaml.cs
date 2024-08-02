using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await GetServer();
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (serverlist1.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(this, "请先选择一个版本！", "警告");
                return;
            }
            await DownloadServerFunc();
        }

        private async void serverlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverlist1.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(this, "请先选择一个版本！", "警告");
                return;
            }
            await DownloadServerFunc();
        }

        private async Task DownloadServerFunc()
        {
            downVersion = serverlist1.SelectedItem.ToString();
            downServer = serverlist.SelectedItem.ToString();

            if (serverlist1.SelectedIndex != -1)
            {
                JObject downContext = await HttpService.GetApiContentAsync("download/server/" + downServer + "/" + downVersion);
                string downUrl = downContext["data"]["url"].ToString();
                string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;
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
                    JObject _dlContext = await HttpService.GetApiContentAsync("download/server/" + downServer + "/" + downVersion);
                    string _dlUrl = _dlContext["data"]["url"].ToString();
                    string _sha256Exp = _dlContext["data"]["sha256"]?.ToString() ?? string.Empty;
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
                        Shows.ShowMsgDialog(this, "Banner端移动失败！\n请重试！" + e.Message, "错误");
                        return;
                    }

                    //下载一个fabric端
                    //获取版本号
                    string bannerVersion = filename.Replace("banner-", "").Replace(".jar", "");
                    bool dwnFabric = await Shows.ShowDownloader(GetWindow(this), (await HttpService.GetApiContentAsync("download/server/fabric/" + bannerVersion))["data"]["url"].ToString(), downloadServerBase, $"fabric-{bannerVersion}.jar", "下载Fabric端中···");
                    if (!dwnFabric || !File.Exists(downloadServerBase + "\\" + $"fabric-{bannerVersion}.jar"))
                    {
                        Shows.ShowMsgDialog(this, "Fabric端下载取消（或服务端文件不存在）！", "错误");
                        return;
                    }

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
            Functions functions = new Functions();
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
                string _ret = Functions.InstallForge(downloadServerJava, downloadServerBase, filename, installForge[1]);
                if (_ret == null)
                {
                    return Functions.InstallForge(downloadServerJava, downloadServerBase, filename, string.Empty, false);
                }
                else
                {
                    return _ret;
                }
            }
            else if (installForge[0] == "3")
            {
                return Functions.InstallForge(downloadServerJava, downloadServerBase, filename, string.Empty, false);
            }
            else
            {
                return null;
            }
        }

        private async Task GetServer()
        {
            serverlist.ItemsSource = null;
            serverlist1.ItemsSource = null;
            try
            {
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/available_server_types");
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string[] serverTypes = JsonConvert.DeserializeObject<string[]>(((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["types"].ToString());
                    serverlist.ItemsSource = serverTypes;
                    serverlist.SelectedIndex = 0;
                }
                else
                {
                    getservermsg.Text = "请求错误！请重试\n（" + httpResponse.HttpResponseCode.ToString() + "）" + httpResponse.HttpResponseContent.ToString();
                    Loading_Circle.IsRunning = false;
                    Loading_Circle.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception a)
            {
                getservermsg.Text = "获取服务端失败！请重试\n" + a.Message;
                Loading_Circle.IsRunning = false;
                Loading_Circle.Visibility = Visibility.Collapsed;
            }
        }

        private async void serverlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverlist.Items.Count != 0)
            {
                await GetServerVersionList();
            }
        }

        private async Task GetServerVersionList()
        {
            Loading_Circle.IsRunning = true;
            Loading_Circle.Visibility = Visibility.Visible;
            serverlist1.ItemsSource = null;
            try
            {
                serverlist1.ItemsSource = null;
                getservermsg.Visibility = Visibility.Visible;
                getservermsg.Text = "加载中，请稍等...";
                string serverName = serverlist.SelectedItem.ToString();
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/available_versions/" + serverName);
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string resultData = ((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["versionList"].ToString();
                    server_d.Text = (await HttpService.GetApiContentAsync("query/servers_description/" + serverName))["data"]["description"].ToString();
                    JArray serverVersions = JArray.Parse(resultData);
                    List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                    serverlist1.ItemsSource = sortedVersions;
                    getservermsg.Visibility = Visibility.Collapsed;
                }
                else
                {
                    getservermsg.Text = "请求错误！请重试\n（" + httpResponse.HttpResponseCode.ToString() + "）" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                getservermsg.Text = "获取服务端失败！请重试\n" + a.Message;
            }
            Loading_Circle.IsRunning = false;
            Loading_Circle.Visibility = Visibility.Collapsed;
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await GetServer();
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
    }
}
