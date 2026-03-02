using HandyControl.Controls;
using MSL.controls.dialogs;
using MSL.langs;
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

namespace MSL.pages
{
    /// <summary>
    /// DownloadServer.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadServer : HandyControl.Controls.Window
    {
        public enum Mode // 在自由模式下，不会自动安装forge，同时对于一些需要Vanilla或其他依赖的服务端，也不会自动下载依赖
        {
            CreateServer,
            ChangeServerSettings,
            FreeDownload,
        }
        public string FileName { get; set; }
        private string SavingPath;

        private readonly Mode DownloadMode;
        private string JavaPath; // The Java Path for install Forge-ServerCore

        public DownloadServer(string savingPath, Mode downloadMode, string javaPath = "")
        {
            InitializeComponent();
            SavingPath = savingPath;
            DownloadMode = downloadMode;
            JavaPath = javaPath;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DownloadMode != Mode.FreeDownload)
            {
                OpenDownloadManager.Visibility = Visibility.Collapsed;
            }
            await GetServer();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DownloadMode == Mode.FreeDownload)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            serverCoreList.ItemsSource = null;
            coreVersionList.ItemsSource = null;
            versionBuildList.ItemsSource = null;
            SavingPath = null;
            JavaPath = null;
            FileName = null;
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
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
            try
            {
                await DownloadServerFunc();
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, ex.Message, "错误");
            }
            versionBuildList.IsEnabled = true;
            DownloadBtn.IsEnabled = true;
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
            try
            {
                await DownloadServerFunc();
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, ex.Message, "错误");
            }
            versionBuildList.IsEnabled = true;
            DownloadBtn.IsEnabled = true;
        }

        private string MriiorCheck(string downUrl)
        {
            if (UseMirrorUrl.IsChecked == false)
            {
                downUrl = downUrl.Replace("file.mslmc.cn/mirrors/vanilla/", "piston-data.mojang.com/v1/objects/");
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

        private static bool IsSequentialDownload(string serverType) => serverType is "vanilla" or "forge" or "neoforge";
        private async Task DownloadServerFunc()
        {
            if (serverCoreList.SelectedIndex == -1 ||
                coreVersionList.SelectedIndex == -1 ||
                versionBuildList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(this, "请检查您是否已正确选择 服务端-版本-构建版本 ！", "错误");
                return;
            }

            string downServer = serverCoreList.SelectedValue.ToString();
            string downVersion = coreVersionList.SelectedValue.ToString();
            string downBuild = versionBuildList.SelectedValue.ToString();

            if (DownloadMode == Mode.FreeDownload)
            {
                // ── 自由下载模式：只加入下载列表，不等待，不安装 ──
                // 获取下载信息后直接丢给 DownloadManager，无需走 ServerCoreInstaller
                string buildQuery = downBuild.Contains("latest") ? "latest" : downBuild;
                JObject dlContext = await HttpService.GetApiContentAsync(
                    $"download/server/{downServer}/{downVersion}?build={buildQuery}");
                string dlUrl = MriiorCheck(dlContext["data"]["url"].ToString());
                string sha256Exp = dlContext["data"]["sha256"]?.ToString() ?? string.Empty;
                string filename = $"{downServer}-{downVersion}.jar";
                bool enableParallel = !IsSequentialDownload(downServer);

                var dwnManager = DownloadManager.Instance;
                string groupId = dwnManager.CreateDownloadGroup();
                dwnManager.AddDownloadItem(groupId, dlUrl, SavingPath, filename, sha256Exp,
                                           enableParallel: enableParallel);
                dwnManager.StartDownloadGroup(groupId);
                DownloadManagerDialog.Instance.ManagerControl.AddDownloadGroup(groupId, true);

                MagicFlowMsg.ShowMessage("已添加至下载列表~");
            }
            else
            {
                // ── 正常安装模式：交由 ServerCoreInstaller 完成下载+安装 ──
                var installer = new ServerCoreInstaller(this, SavingPath, JavaPath, useMirror: UseMirrorUrl.IsChecked == true);
                var result = await installer.DownloadAndInstallAsync(downServer, downVersion, downBuild);

                if (!result.Success)
                {
                    MagicShow.ShowMsgDialog(Functions.GetWindow(this), result.ErrorMessage, "INFO");
                    return;
                }

                FileName = result.FinalFileName;
                Close();
            }
        }

        private async Task GetServer()
        {
            serverCoreList.ItemsSource = null;
            serverCoreLoadTip.Text = LanguageManager.Instance["Loading_PlzWait"];
            serverCoreLoadTip.Visibility = Visibility.Visible;
            try
            {
                HttpResponse httpResponse = await HttpService.GetApiAsync("query/available_server_types");
                if (httpResponse.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    string[] serverTypes = JsonConvert.DeserializeObject<string[]>(((JObject)JsonConvert.DeserializeObject(httpResponse.HttpResponseContent.ToString()))["data"]["types"].ToString());
                    serverCoreList.ItemsSource = serverTypes;
                    serverCoreList.SelectedIndex = 0;
                    serverCoreLoadTip.Visibility = Visibility.Collapsed;
                }
                else
                {
                    server_d.Text = "请求错误！请重试！";
                    serverCoreLoadTip.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试！";
                serverCoreLoadTip.Text = "获取服务端失败！请重试\n" + a.Message;
            }
        }

        private async void serverCoreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverCoreList.SelectedIndex == -1)
            {
                return;
            }
            coreVersionList.ItemsSource = null;
            coreVersionLoadTip.Text = LanguageManager.Instance["Loading_PlzWait"];
            coreVersionLoadTip.Visibility = Visibility.Visible;
            try
            {

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
                    coreVersionLoadTip.Visibility = Visibility.Collapsed;
                }
                else
                {
                    server_d.Text = "请求错误！请重试！";
                    coreVersionLoadTip.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试！";
                coreVersionLoadTip.Text = "获取服务端失败！请重试\n" + a.Message;
            }
        }

        private async void coreVersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (coreVersionList.SelectedIndex == -1)
            {
                return;
            }
            versionBuildList.ItemsSource = null;
            versionBuildLoadTip.Text = LanguageManager.Instance["Loading_PlzWait"];
            versionBuildLoadTip.Visibility = Visibility.Visible;
            try
            {
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
                    versionBuildLoadTip.Visibility = Visibility.Collapsed;
                }
                else
                {
                    server_d.Text = "请求错误！请重试！";
                    versionBuildLoadTip.Text = "请求错误！请重试\n(" + httpResponse.HttpResponseCode.ToString() + ")" + httpResponse.HttpResponseContent.ToString();
                }
            }
            catch (Exception a)
            {
                server_d.Text = "获取服务端失败！请重试！";
                versionBuildLoadTip.Text = "获取服务端失败！请重试\n" + a.Message;
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await GetServer();
        }

        private void openChooseServerDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/mc-server/choose-server-tips/");
        }

        private void OpenDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            var token = Guid.NewGuid().ToString();
            Dialog.SetToken(this, token);
            DownloadManagerDialog.Instance.LoadDialog(token, true);
            Dialog.Show(DownloadManagerDialog.Instance, token);
        }
    }
}
