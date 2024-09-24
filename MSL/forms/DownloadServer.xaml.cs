﻿using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
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
            if (versionBuildList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(this, "请先选择一个构建版本！", "警告");
                return;
            }
            versionBuildList.IsEnabled = false;
            DownloadBtn.IsEnabled = false;
            await DownloadServerFunc();
            versionBuildList.IsEnabled = true;
            DownloadBtn.IsEnabled = true;
        }

        private string MriiorCheck(string downUrl)
        {
            if (UseMirrorUrl.IsChecked == false)
            {
                downUrl = downUrl.Replace("bmclapi2.bangbang93.com", "piston-data.mojang.com");
                if (serverCoreList.SelectedItem.ToString() == "forge")
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
                    downUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/forge-{mcVersion}-{forgeVersion}-installer.jar";
                }
            }
            return downUrl;
        }

        private async void versionBuildList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (versionBuildList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(this, "请先选择一个构建版本！", "警告");
                return;
            }
            versionBuildList.IsEnabled = false;
            DownloadBtn.IsEnabled = false;
            await DownloadServerFunc();
            versionBuildList.IsEnabled = true;
            DownloadBtn.IsEnabled = true;
        }

        private async Task DownloadServerFunc()
        {
            if (versionBuildList.SelectedIndex == -1)
            {
                return;
            }
            string downServer = serverCoreList.SelectedItem.ToString();
            string downVersion = coreVersionList.SelectedItem.ToString();
            string downBuild = versionBuildList.SelectedItem.ToString();
            if (downBuild.Contains("latest"))
            {
                downBuild = "latest";
            }
            JObject downContext = await HttpService.GetApiContentAsync("download/server/" + downServer + "/" + downVersion + "?build=" + downBuild);
            string downUrl = downContext["data"]["url"].ToString();

            downUrl= MriiorCheck(downUrl);
            string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;
            downPath = downloadServerBase;
            filename = downServer + "-" + downVersion + ".jar";

            int dwnDialog = await MagicShow.ShowDownloaderWithIntReturn(this, downUrl, downPath, filename, "下载服务端中……", sha256Exp, true);
            if (dwnDialog == 2)
            {
                MagicShow.ShowMsgDialog(this, "下载取消！", "错误");
                return;
            }
            if (!File.Exists(downPath + "\\" + filename))
            {
                MagicShow.ShowMsgDialog(this, "下载失败！（或服务端文件不存在）", "提示");
                return;
            }
            if (!isInstallSomeCore)
            {
                MagicShow.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                return;
            }

            if (downServer == "spongeforge")
            {
                string forgeName = downServer.Replace("spongeforge", "forge");
                string _filename = forgeName + ".jar";
                JObject _dlContext = await HttpService.GetApiContentAsync("download/server/" + forgeName + "/" + downVersion);
                string _dlUrl = _dlContext["data"]["url"].ToString();
                _dlUrl= MriiorCheck(_dlUrl);
                string _sha256Exp = _dlContext["data"]["sha256"]?.ToString() ?? string.Empty;
                int _dwnDialog = await MagicShow.ShowDownloaderWithIntReturn(this, _dlUrl, downPath, _filename, "下载依赖服务端中……", _sha256Exp, true);

                if (_dwnDialog == 2)
                {
                    MagicShow.ShowMsgDialog(this, "下载取消！", "提示");
                    return;
                }

                //sponge应当作为模组加载，所以要再下载一个forge才是服务端
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
                    MagicShow.ShowMsgDialog(this, "Sponge核心移动失败！\n请重试！" + e.Message, "错误");
                    return;
                }
                string installReturn = await InstallForge(_filename);
                if (installReturn == null)
                {
                    MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                    return;
                }

                downloadServerName = installReturn;
            }
            else if (downServer == "neoforge")
            {
                string installReturn = await InstallForge(filename);
                if (installReturn == null)
                {
                    MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                    return;
                }

                downloadServerName = installReturn;
            }
            else if (downServer == "forge")
            {
                string installReturn = await InstallForge(filename);
                if (installReturn == null)
                {
                    MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                    return;
                }

                downloadServerName = installReturn;
            }
            else if (downServer == "banner")
            {
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
                    MagicShow.ShowMsgDialog(this, "Banner端移动失败！\n请重试！" + e.Message, "错误");
                    return;
                }

                //下载一个fabric端
                //获取版本号
                string bannerVersion = filename.Replace("banner-", "").Replace(".jar", "");
                bool dwnFabric = await MagicShow.ShowDownloader(GetWindow(this), (await HttpService.GetApiContentAsync("download/server/fabric/" + bannerVersion))["data"]["url"].ToString(), downloadServerBase, $"fabric-{bannerVersion}.jar", "下载Fabric端中···");
                if (!dwnFabric || !File.Exists(downloadServerBase + "\\" + $"fabric-{bannerVersion}.jar"))
                {
                    MagicShow.ShowMsgDialog(this, "Fabric端下载取消（或服务端文件不存在）！", "错误");
                    return;
                }

                downloadServerName = $"fabric-{bannerVersion}.jar";
            }
            else
            {
                if (!isInstallSomeCore)
                {
                    MagicShow.ShowMsgDialog(this, "下载完成！服务端核心放置在“MSL\\ServerCores”文件夹中！", "提示");
                    return;
                }
                downloadServerName = filename;
            }
            Close();
        }

        private async Task<string> InstallForge(string filename)
        {
            //调用新版forge安装器
            string[] installForge = await MagicShow.ShowInstallForge(this, downPath + "\\" + filename, downPath, downloadServerJava);
            Functions functions = new Functions();
            if (installForge[0] == "0")
            {
                if (await MagicShow.ShowMsgDialogAsync(this, "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
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
            serverCoreList.ItemsSource = null;
            coreVersionList.ItemsSource = null;
            versionBuildList.ItemsSource = null;
            try
            {
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/available_server_types");
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string[] serverTypes = JsonConvert.DeserializeObject<string[]>(((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["types"].ToString());
                    serverCoreList.ItemsSource = serverTypes;
                    serverCoreList.SelectedIndex = 0;
                }
                else
                {
                    server_d.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                    //Loading_Circle.IsRunning = false;
                    //Loading_Circle.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试\n" + a.Message;
                //Loading_Circle.IsRunning = false;
                //Loading_Circle.Visibility = Visibility.Collapsed;
            }
        }

        private async void serverCoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverCoreList.SelectedIndex == -1)
            {
                return;
            }

            //Loading_Circle.IsRunning = true;
            //Loading_Circle.Visibility = Visibility.Visible;
            try
            {
                coreVersionList.ItemsSource = null;
                //getservermsg.Visibility = Visibility.Visible;
                //getservermsg.Text = "加载中，请稍等...";
                string serverName = serverCoreList.SelectedItem.ToString();
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/available_versions/" + serverName);
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string resultData = ((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["versionList"].ToString();
                    server_d.Text = (await HttpService.GetApiContentAsync("query/servers_description/" + serverName))["data"]["description"].ToString();
                    JArray serverVersions = JArray.Parse(resultData);
                    List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                    coreVersionList.ItemsSource = sortedVersions;
                    coreVersionList.SelectedIndex = 0;
                    //getservermsg.Visibility = Visibility.Collapsed;
                }
                else
                {
                    //getservermsg.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试\n" + a.Message;
            }
            //Loading_Circle.IsRunning = false;
            //Loading_Circle.Visibility = Visibility.Collapsed;
        }

        private async void coreVersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (coreVersionList.SelectedIndex == -1)
            {
                return;
            }
            //Loading_Circle.IsRunning = true;
            //Loading_Circle.Visibility = Visibility.Visible;
            try
            {
                versionBuildList.ItemsSource = null;
                //getservermsg.Visibility = Visibility.Visible;
                //getservermsg.Text = "加载中，请稍等...";
                string serverName = serverCoreList.SelectedItem.ToString();
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/server/" + serverName + "/" + coreVersionList.SelectedItem.ToString());
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string resultData = ((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["builds"].ToString();
                    if (resultData.Contains("latest"))
                    {
                        resultData = resultData.Replace("latest", "latest - 最新构建版本");
                    }
                    JArray serverVersions = JArray.Parse(resultData);
                    List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                    versionBuildList.ItemsSource = sortedVersions;
                    versionBuildList.SelectedIndex = 0;
                    //getservermsg.Visibility = Visibility.Collapsed;
                }
                else
                {
                    server_d.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试\n" + a.Message;
            }
            //Loading_Circle.IsRunning = false;
            //Loading_Circle.Visibility = Visibility.Collapsed;
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
