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
using File = System.IO.File;

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

        private Dialog downloadManagerDialog;
        private DownloadManagerDialog downloadManager;

        public DownloadServer(string savingPath, Mode downloadMode, string javaPath = "")
        {
            InitializeComponent();
            SavingPath = savingPath;
            DownloadMode = downloadMode;
            JavaPath = javaPath;

            if (DownloadMode == Mode.FreeDownload)
            {
                downloadManagerDialog = new Dialog();
                downloadManager = new DownloadManagerDialog()
                {
                    Margin= new Thickness(20),
                };
                downloadManager.ManagerControl.AutoRemoveCompletedItems = false;
            }
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
            if (downloadManagerDialog != null)
            {
                downloadManager.ManagerControl.ClearAllItems();
                downloadManagerDialog.Close();
                downloadManagerDialog = null;
                downloadManager = null;
            }
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

        private async Task<bool> DownloadFun(string downUrl, string path, string filename, string sha256 = "", bool _enableParellel = true, string remark = "")
        {
            if (DownloadMode == Mode.FreeDownload)
            {
                var dwnManager = DownloadManager.Instance;
                string groupid = dwnManager.CreateDownloadGroup(isTempGroup: true);
                dwnManager.AddDownloadItem(groupid, downUrl, path, filename, sha256, enableParallel: _enableParellel);
                dwnManager.StartDownloadGroup(groupid);
                downloadManager.ManagerControl.AddDownloadGroup(groupid, true);
                return true;
            }
            else
            {
                int ret = await MagicShow.ShowDownloaderWithIntReturn(this, downUrl, path, filename, remark, sha256, true, _enableParellel);
                switch (ret)
                {
                    case 1:
                        return true;
                    case 2:
                        MagicShow.ShowMsgDialog(this, "下载取消！", "错误");
                        return false;
                    case 3:
                        MagicShow.ShowMsgDialog(this, "下载失败！", "错误");
                        return false;
                    default:
                        return false;
                }
            }
        }

        private async Task DownloadServerFunc()
        {
            if (serverCoreList.SelectedIndex == -1 || coreVersionList.SelectedIndex == -1 || versionBuildList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(this, "请检查您是否已正确选择 服务端-版本-构建版本 ！", "错误");
                return;
            }
            string downServer = serverCoreList.SelectedValue.ToString();
            string downVersion = coreVersionList.SelectedValue.ToString();
            string downBuild = versionBuildList.SelectedValue.ToString();
            if (downBuild.Contains("latest"))
            {
                downBuild = "latest";
            }
            JObject downContext = await HttpService.GetApiContentAsync("download/server/" + downServer + "/" + downVersion + "?build=" + downBuild);
            string downUrl = downContext["data"]["url"].ToString();

            downUrl = MriiorCheck(downUrl);
            string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;
            string filename = downServer + "-" + downVersion + ".jar";


            bool _enableParalle = true;
            if (downServer == "vanilla" || downServer == "forge" || downServer == "neoforge")
                _enableParalle = false;

            if(!await DownloadFun(downUrl, SavingPath, filename, sha256Exp, _enableParalle, "下载服务端中……"))
            {
                return;
            }

            if (DownloadMode == Mode.FreeDownload)
            {
                MagicFlowMsg.ShowMessage("已将任务添加至下载列表中！");
                return;
            }
            if (!File.Exists(SavingPath + "\\" + filename))
            {
                MagicShow.ShowMsgDialog(this, "下载失败！（或服务端文件不存在）", "提示");
                return;
            }
            
            await FinalStep(downServer, downVersion, filename); // 下载完成后的处理步骤
        }        

        private async Task FinalStep(string downServer, string downVersion, string filename)
        {
            string installReturn;
            switch (downServer)
            {
                case "spongeforge":
                    string forgeName = downServer.Replace("spongeforge", "forge");
                    string _filename = forgeName + ".jar";
                    JObject _dlContext = await HttpService.GetApiContentAsync("download/server/" + forgeName + "/" + downVersion);
                    string _dlUrl = _dlContext["data"]["url"].ToString();
                    _dlUrl = MriiorCheck(_dlUrl);
                    string _sha256Exp = _dlContext["data"]["sha256"]?.ToString() ?? string.Empty;

                    if(!await DownloadFun(_dlUrl, SavingPath, _filename, _sha256Exp, true, "下载依赖服务端中……"))
                    {
                        return;
                    }

                    //sponge应当作为模组加载，所以要再下载一个forge才是服务端
                    try
                    {
                        //移动到mods文件夹
                        Directory.CreateDirectory(SavingPath + "\\mods\\");
                        if (File.Exists(SavingPath + "\\mods\\" + filename))
                        {
                            File.Delete(SavingPath + "\\mods\\" + filename);
                        }
                        File.Move(SavingPath + "\\" + filename, SavingPath + "\\mods\\" + filename);
                    }
                    catch (Exception e)
                    {
                        MagicShow.ShowMsgDialog(this, "Sponge核心移动失败！\n请重试！" + e.Message, "错误");
                        return;
                    }
                    installReturn = await InstallForge(_filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    FileName = installReturn;
                    break;
                case "neoforge":
                    installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    FileName = installReturn;
                    break;
                case "forge":
                    installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(this, "安装失败！", "错误");
                        return;
                    }

                    FileName = installReturn;
                    break;
                    /*
                case "banner":
                    //banner应当作为模组加载，所以要再下载一个fabric才是服务端
                    try
                    {
                        //移动到mods文件夹
                        Directory.CreateDirectory(SavingPath + "\\mods\\");
                        if (File.Exists(SavingPath + "\\mods\\" + filename))
                        {
                            File.Delete(SavingPath + "\\mods\\" + filename);
                        }
                        File.Move(SavingPath + "\\" + filename, SavingPath + "\\mods\\" + filename);
                    }
                    catch (Exception e)
                    {
                        MagicShow.ShowMsgDialog(this, "Banner端移动失败！\n请重试！" + e.Message, "错误");
                        return;
                    }

                    //下载一个fabric端
                    //获取版本号
                    if (!await DownloadFun((await HttpService.GetApiContentAsync("download/server/fabric/" + downVersion))["data"]["url"].ToString(),
                        SavingPath, $"fabric-{downVersion}.jar", remark: "下载Fabric端中···"))
                    {
                        return;
                    }

                    //下载Vanilla端
                    if (!await DownloadVanilla(SavingPath + "\\.fabric\\server", downVersion + "-server.jar", downVersion))
                    {
                        MagicShow.ShowMsgDialog(this, "您取消了跳过，请重新下载。", "错误");
                        return;
                    }
                    FileName = $"fabric-{downVersion}.jar";
                    break; */
                case "fabric":
                    //下载Vanilla端
                    if (!await DownloadVanilla(SavingPath + "\\.fabric\\server", downVersion + "-server.jar", downVersion))
                    {
                        MagicShow.ShowMsgDialog(this, "您取消了跳过，请重新下载。", "错误");
                        return;
                    }
                    FileName = filename;
                    break;
                case "paper":
                    //下载Vanilla端
                    if (!await DownloadVanilla(SavingPath + "\\cache", "mojang_" + downVersion + ".jar", downVersion))
                    {
                        MagicShow.ShowMsgDialog(this, "您取消了跳过，请重新下载。", "错误");
                        return;
                    }
                    FileName = filename;
                    break;
                default:
                    FileName = filename;
                    break;
            }
            Close();
        }

        private async Task<bool> DownloadVanilla(string path, string filename, string version)
        {
            JObject downContext = await HttpService.GetApiContentAsync("download/server/vanilla/" + version);
            string downUrl = downContext["data"]["url"].ToString();

            downUrl = MriiorCheck(downUrl);
            string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;

            if(!await DownloadFun(downUrl, path, filename, sha256Exp, true, "下载依赖中（香草端）……"))
            {
                return false;
            }
            return true;
        }

        private async Task<string> InstallForge(string filename)
        {
            //调用新版forge安装器
            string[] installForge = await MagicShow.ShowInstallForge(this, SavingPath, filename, JavaPath);
            Functions functions = new Functions();
            if (installForge[0] == "0")
            {
                if (await MagicShow.ShowMsgDialogAsync(this, "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
                {
                    return Functions.InstallForge(JavaPath, SavingPath, filename, string.Empty, false);
                }
                else
                {
                    return null;
                }
            }
            else if (installForge[0] == "1")
            {
                string _ret = Functions.InstallForge(JavaPath, SavingPath, filename, installForge[1]);
                if (_ret == null)
                {
                    return Functions.InstallForge(JavaPath, SavingPath, filename, string.Empty, false);
                }
                else
                {
                    return _ret;
                }
            }
            else if (installForge[0] == "3")
            {
                return Functions.InstallForge(JavaPath, SavingPath, filename, string.Empty, false);
            }
            else
            {
                return null;
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
            Process.Start("https://www.mslmc.cn/docs/choose-server-tips.html");
        }

        private void OpenDownloadManager_Click(object sender, RoutedEventArgs e)
        {
            downloadManagerDialog = Dialog.Show(downloadManager);
            downloadManager.fatherDialog = downloadManagerDialog;
        }
    }
}
