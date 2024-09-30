using CurseForge.APIClient;
using CurseForge.APIClient.Models;
using CurseForge.APIClient.Models.Mods;
using Modrinth;
using Modrinth.Models;
using MSL.controls;
using MSL.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MSL
{
    /// <summary>
    /// DownloadMods.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadMod : HandyControl.Controls.Window
    {
        private int LoadType = 0;  //0: mods , 1: modpacks  , 2: plugins
        private int LoadSource = 0;  //0: Curseforge , 1: Modrinth 
        private bool CloseImmediately;
        private readonly string SavingPath;
        private ApiClient CurseForgeApiClient;
        private ModrinthClient ModrinthApiClient;

        public DownloadMod(string savingPath, int loadSource = 0, int loadType = 0, bool canChangeLoadType = true, bool canChangeSource = true, bool closeImmediately = false)
        {
            InitializeComponent();
            SavingPath = savingPath;
            LoadSource = loadSource;
            LoadType = loadType;
            LoadSourceBox.SelectedIndex = loadSource;
            LoadTypeBox.SelectedIndex = loadType;
            if (LoadSource == 0)
            {
                LTB_Plugins.Visibility = Visibility.Collapsed;
            }
            if (LoadType == 2)
            {
                LSB_CurseForge.Visibility = Visibility.Collapsed;
            }
            if (!canChangeLoadType)
            {
                LoadTypeBox.IsEnabled = false;
            }
            if (!canChangeSource)
            {
                LoadSourceBox.IsEnabled = false;
            }
            CloseImmediately = closeImmediately;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEvent();
        }

        private async Task LoadEvent_CurseForge()
        {
            try
            {
                if (CurseForgeApiClient == null)
                {
                    string token = string.Empty;
                    string _token = (await HttpService.GetApiContentAsync("query/cf_token"))["data"]["cfToken"].ToString();
                    byte[] data = Convert.FromBase64String(_token);
                    string decodedString = Encoding.UTF8.GetString(data);
                    token = decodedString;
                    CurseForgeApiClient = new ApiClient(token);
                }
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                if (LoadType == 0)
                {
                    var featuredMods = await CurseForgeApiClient.GetFeaturedModsAsync(new GetFeaturedModsRequestBody
                    {
                        GameId = 432,
                        ExcludedModIds = new List<int>(),
                        GameVersionTypeId = null,
                    });

                    foreach (var featuredMod in featuredMods.Data.Popular)
                    {
                        list.Add(new DM_ModsInfo(featuredMod.Id.ToString(), featuredMod.Logo.ThumbnailUrl, featuredMod.Name, featuredMod.Links.WebsiteUrl.ToString()));
                    }
                    NowPageLabel.Content = "精选";
                }
                else if (LoadType == 1)
                {
                    var modpacks = await CurseForgeApiClient.SearchModsAsync(432, null, 5128);
                    foreach (var modPack in modpacks.Data)
                    {
                        list.Add(new DM_ModsInfo(modPack.Id.ToString(), modPack.Logo.ThumbnailUrl, modPack.Name, modPack.Links.WebsiteUrl.ToString()));
                    }
                    NowPageLabel.Content = "1";
                }
                ModList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task LoadEvent_Modrinth()
        {
            try
            {
                if (ModrinthApiClient == null)
                {
                    // Note: All properties are optional, and will be ignored if they are null or empty
                    var userAgent = new UserAgent
                    {
                        ProjectName = "MSL",
                        ProjectVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                        GitHubUsername = "MSLTeam"
                    };

                    var options = new ModrinthClientConfig
                    {
                        // Optional, if you want to access authenticated API endpoints
                        //ModrinthToken = "Your_Authentication_Token",
                        // For Modrinth API, you must specify a user-agent
                        // There is a default library user-agent, but it is recommended to specify your own
                        UserAgent = userAgent.ToString()
                    };

                    ModrinthApiClient = new ModrinthClient(options);
                }
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                SearchResponse mods = null;
                if (LoadType == 0)
                {
                    mods = await ModrinthApiClient.Project.SearchAsync("");
                }
                else if (LoadType == 1)
                {
                    var facets = new FacetCollection()
                    {
                        Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Modpack)
                    };
                    mods = await ModrinthApiClient.Project.SearchAsync("", facets: facets);
                }
                else
                {
                    var facets = new FacetCollection()
                    {
                        Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Plugin)
                    };
                    mods = await ModrinthApiClient.Project.SearchAsync("", facets: facets);
                }
                foreach (var mod in mods?.Hits)
                {
                    list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Title, mod.Url));
                }

                ModList.ItemsSource = list;
                NowPageLabel.Content = "1";
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task Search_CurseForge(string name, int index = 0)
        {
            try
            {
                if (CurseForgeApiClient == null)
                {
                    string token = string.Empty;
                    string _token = (await HttpService.GetApiContentAsync("query/cf_token"))["data"]["cfToken"].ToString();
                    byte[] data = Convert.FromBase64String(_token);
                    string decodedString = Encoding.UTF8.GetString(data);
                    token = decodedString;
                    CurseForgeApiClient = new ApiClient(token);
                }
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                GenericListResponse<Mod> mods = null;
                if (LoadType == 0)
                {
                    mods = await CurseForgeApiClient.SearchModsAsync(432, searchFilter: name, index: index);
                }
                else if (LoadType == 1)
                {
                    mods = await CurseForgeApiClient.SearchModsAsync(432, categoryId: 5128, searchFilter: name, index: index);
                }
                foreach (var mod in mods.Data)
                {
                    list.Add(new DM_ModsInfo(mod.Id.ToString(), mod.Logo.ThumbnailUrl, mod.Name, mod.Links.WebsiteUrl.ToString()));
                }
                ModList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task Search_Modrinth(string name, ulong offset = 0)
        {
            try
            {
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                SearchResponse mods = null;
                if (LoadType == 0)
                {
                    mods = await ModrinthApiClient.Project.SearchAsync(name, offset: offset);
                }
                else if (LoadType == 1)
                {
                    var facets = new FacetCollection()
                    {
                        Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Modpack)
                    };
                    mods = await ModrinthApiClient.Project.SearchAsync(name, facets: facets, offset: offset);
                }
                else
                {
                    var facets = new FacetCollection()
                    {
                        Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Plugin)
                    };
                    mods = await ModrinthApiClient.Project.SearchAsync(name, facets: facets, offset: offset);
                }
                foreach (var mod in mods?.Hits)
                {
                    list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Title, mod.Url));
                }

                ModList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async void searchMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ModListGrid.IsEnabled = false;
                //lCircle.IsRunning = true;
                //lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                if (LoadSource == 0)
                {
                    await Search_CurseForge(SearchTextBox.Text);
                }
                else if (LoadSource == 1)
                {
                    await Search_Modrinth(SearchTextBox.Text);
                }
                //lCircle.IsRunning = false;
                //lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                ModListGrid.IsEnabled = true;
                NowPageLabel.Content = 1;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "搜索失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            await LoadEvent();
        }

        private async void LastPageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ulong nowPage;
                if (NowPageLabel.Content.ToString() == "精选")
                {
                    nowPage = 0;
                }
                else
                {
                    nowPage = ulong.Parse(NowPageLabel.Content.ToString());
                }
                if (nowPage <= 1)
                {
                    return;
                }
                ModListGrid.IsEnabled = false;
                //lCircle.IsRunning = true;
                //lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                if (LoadSource == 0)
                {
                    await Search_CurseForge(SearchTextBox.Text, ((int)nowPage - 2) * 50);
                }
                else if (LoadSource == 1)
                {
                    await Search_Modrinth(SearchTextBox.Text, (nowPage - 2) * 10);
                }
                //lCircle.IsRunning = false;
                //lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                ModListGrid.IsEnabled = true;
                NowPageLabel.Content = nowPage - 1;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "加载失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ulong nowPage;
                if (NowPageLabel.Content.ToString() == "精选")
                {
                    nowPage = 0;
                }
                else
                {
                    nowPage = ulong.Parse(NowPageLabel.Content.ToString());
                }
                ModListGrid.IsEnabled = false;
                //lCircle.IsRunning = true;
                //lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                if (LoadSource == 0)
                {
                    await Search_CurseForge(SearchTextBox.Text, (int)nowPage * 50);
                }
                else if (LoadSource == 1)
                {
                    await Search_Modrinth(SearchTextBox.Text, nowPage * 10);
                }
                //lCircle.IsRunning = false;
                //lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                ModListGrid.IsEnabled = true;
                NowPageLabel.Content = nowPage + 1;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "加载失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task ModInfo_CurseForge(DM_ModsInfo info)
        {
            var modFiles = await CurseForgeApiClient.GetModFilesAsync(int.Parse(info.ID));
            var loadedCount = 0;
            var totalCount = modFiles.Data.Count;
            using var semaphore = new SemaphoreSlim(50);
            bool onlyShowServerPack = false;
            if (LoadType == 1)
            {
                if (await MagicShow.ShowMsgDialogAsync(this, "是否仅展示适用于服务器的整合包文件？\n注意：如果不使用服务器专用包开服，可能会出现无法开服/崩溃的问题！", "询问", true) == true)
                {
                    onlyShowServerPack = true;
                }
            }
            async Task LoadAndAddModInfo(CurseForge.APIClient.Models.Files.File modData)
            {
                await semaphore.WaitAsync();
                try
                {
                    DM_ModInfo modInfo;
                    DM_ModInfo _modInfo = null;
                    if (LoadType == 0)
                    {
                        modInfo = new DM_ModInfo(
                            modData.DisplayName,
                            modData.DownloadUrl,
                            modData.FileName,
                        "",
                            string.Join(",", (await Task.WhenAll(modData.Dependencies.Select(s => CurseForgeApiClient.GetModAsync(s.ModId)))).Select(p => p.Data.Name)),
                            string.Join(",", modData.GameVersions));
                    }
                    else if (LoadType == 1)
                    {
                        try
                        {
                            if (!onlyShowServerPack)
                            {
                                var _modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.Id);
                                _modInfo = new DM_ModInfo(_modFile.Data.DisplayName, _modFile.Data.DownloadUrl, _modFile.Data.FileName, "", string.Join(",", (await Task.WhenAll(_modFile.Data.Dependencies.Select(s => CurseForgeApiClient.GetModAsync(s.ModId)))).Select(p => p.Data.Name)), string.Join(",", _modFile.Data.GameVersions));
                            }
                        }
                        finally
                        {
                            var modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.ServerPackFileId.Value);
                            modInfo = new DM_ModInfo(modFile.Data.DisplayName, modFile.Data.DownloadUrl, modFile.Data.FileName, "", string.Join(",", (await Task.WhenAll(modFile.Data.Dependencies.Select(s => CurseForgeApiClient.GetModAsync(s.ModId)))).Select(p => p.Data.Name)), string.Join(",", modFile.Data.GameVersions));
                        }
                    }
                    else
                    {
                        return; // 不支持的 LoadType
                    }

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ModVerList.Items.Add(modInfo);
                        if (_modInfo != null)
                        {
                            ModVerList.Items.Add(_modInfo);
                        }
                        loadedCount++;
                        ModInfoLoadingProcess.Content = $"{loadedCount}/{totalCount}";
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }

            var loadTasks = modFiles.Data.Select(LoadAndAddModInfo);
            await Task.WhenAll(loadTasks);
        }

        private async Task ModInfo_Modrinth(DM_ModsInfo info)
        {
            var modInfo = await ModrinthApiClient.Project.GetAsync(info.ID);
            string[] versions = modInfo.Versions.Reverse().ToArray();//反转列表
            VerFilterCombo.Items.Add("全部");
            VerFilterCombo.SelectedIndex = 0;
            foreach (var gameVersion in modInfo.GameVersions.Reverse())
            {
                VerFilterCombo.Items.Add(gameVersion);
            }
            var loadedCount = 0;
            var totalCount = modInfo.Versions.Length;
            using var semaphore = new SemaphoreSlim(50);
            async Task LoadAndAddVersion(string modID)
            {
                await semaphore.WaitAsync();
                try
                {
                    var modVersion = await ModrinthApiClient.Version.GetAsync(modID);
                    var modGameVer = modVersion.GameVersions;
                    var modInfo = new DM_ModInfo(
                        modVersion.Name,
                        modVersion.Files[0].Url,
                        modVersion.Files[0].FileName,
                        string.Join(",", modVersion.Loaders),
                        string.Join(",", (await Task.WhenAll(modVersion.Dependencies.Select(s => ModrinthApiClient.Project.GetAsync(s.ProjectId)))).Select(p => p.Title)),
                        //string.Join(",", modVersion.GameVersions)
                        GetMcVersion(modGameVer)
                    );

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ModVerList.Items.Add(modInfo);
                        VerFilter_VersList.Add(modGameVer);
                        loadedCount++;
                        ModInfoLoadingProcess.Content = $"{loadedCount}/{totalCount}";
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }

            var loadTasks = versions.Select(LoadAndAddVersion);
            await Task.WhenAll(loadTasks);
        }

        public static string GetMcVersion(string[] lists)
        {
            string output = "";
            if (lists.Length == 1)
            {
                output = lists[0];
            }
            else
            {
                string startVersion = lists[0];
                string lastVersion = startVersion;

                for (int i = 1; i < lists.Length; i++)
                {
                    string currentVersion = lists[i];
                    string[] lastVersionSplit = lastVersion.Split('.');
                    string[] currentVersionSplit = currentVersion.Split('.');

                    if (currentVersionSplit.Length > 1 && lastVersionSplit.Length > 1)
                    {
                        int lastVersionNumber;
                        int currentVersionNumber;
                        if (int.TryParse(lastVersionSplit[1], out lastVersionNumber) && int.TryParse(currentVersionSplit[1], out currentVersionNumber) && currentVersionNumber - lastVersionNumber > 1)
                        {
                            output += startVersion + " - " + lastVersion + " / ";
                            startVersion = currentVersion;
                        }
                    }

                    lastVersion = currentVersion;
                }

                output += startVersion + " - " + lastVersion;
            }

            return output;
        }

        private async void ModList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModList.Items.Count == 0 || ModList.SelectedIndex == -1)
            {
                return;
            }
            try
            {
                var info = ModList.SelectedItem as DM_ModsInfo;
                backBtn.IsEnabled = false;
                ModInfoGrid.Visibility = Visibility.Visible;
                ModIconLabel.Source = new BitmapImage(new Uri(info.Icon));
                ModNameLabel.Content = info.Name;
                ModWebsiteUrl.Subject = info.WebsiteUrl;
                ModWebsiteUrl.CommandParameter = info.WebsiteUrl;
                ModInfoLoadingProcess.Content = "0/0";
                ModInfoLoadingProcess.Visibility = Visibility.Visible;
                VerFilterCombo.Items.Clear();
                VerFilterCombo.IsEnabled = false;

                if (LoadSource == 0)
                {
                    VerFilterPannel.Visibility = Visibility.Collapsed;
                    MVL_Platform.Width = 0;
                    await ModInfo_CurseForge(info);
                }
                else
                {
                    VerFilterPannel.Visibility = Visibility.Visible;
                    MVL_Platform.Width = 100;
                    await ModInfo_Modrinth(info);
                }
                ModInfoLoadingProcess.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(this, "获取失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
            finally
            {
                backBtn.IsEnabled = true;
                VerFilterCombo.IsEnabled = true;
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            ModInfoGrid.Visibility = Visibility.Collapsed;
            ModVerList.Items.Clear();
            VerFilter_VersList.Clear();
        }

        private List<string[]> VerFilter_VersList = new List<string[]>();
        private void VerFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (VerFilterCombo.Items.Count == 0) return;
            if (VerFilterCombo.SelectedItem.ToString() == "全部")
            {
                foreach (DM_ModInfo item in ModVerList.Items)
                {
                    if (item.IsVisible == false)
                    {
                        item.IsVisible = true;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (var item in VerFilter_VersList)
                {
                    DM_ModInfo dM_ModInfo = ModVerList.Items[i] as DM_ModInfo;
                    if (!item.Contains(VerFilterCombo.SelectedItem.ToString()))
                    {
                        dM_ModInfo.IsVisible = false;
                    }
                    else
                    {
                        if (dM_ModInfo.IsVisible == false)
                        {
                            dM_ModInfo.IsVisible = true;
                        }
                    }
                    i++;
                }
            }
        }

        private async Task LoadEvent()
        {
            //lCircle.IsRunning = true;
            //lCircle.Visibility = Visibility.Visible;
            lb01.Visibility = Visibility.Visible;
            ModListGrid.IsEnabled = false;
            if (LoadSource == 0)
            {
                await LoadEvent_CurseForge();
            }
            else if (LoadSource == 1)
            {
                await LoadEvent_Modrinth();
            }
            //lCircle.IsRunning = false;
            //lCircle.Visibility = Visibility.Collapsed;
            lb01.Visibility = Visibility.Collapsed;
            ModListGrid.IsEnabled = true;
        }

        private async void LoadSourceBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            LoadSource = LoadSourceBox.SelectedIndex;
            if (LoadSource == 0)
            {
                LTB_Plugins.Visibility = Visibility.Collapsed;
            }
            else
            {
                LTB_Plugins.Visibility = Visibility.Visible;
            }
            await LoadEvent();
        }

        private async void LoadTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            LoadType = LoadTypeBox.SelectedIndex;
            if (LoadType == 2)
            {
                LSB_CurseForge.Visibility = Visibility.Collapsed;
            }
            else
            {
                LSB_CurseForge.Visibility = Visibility.Visible;
            }
            await LoadEvent();
        }

        private async void ModVerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModVerList.Items.Count == 0 || ModVerList.SelectedIndex == -1)
            {
                return;
            }
            var iteminfo = ModVerList.SelectedItem as DM_ModInfo;
            Directory.CreateDirectory(SavingPath);
            string filename = iteminfo.FileName;
            //MessageBox.Show(iteminfo.DownloadUrl);
            bool dwnRet = await MagicShow.ShowDownloader(this, iteminfo.DownloadUrl, SavingPath, filename, "下载中……", "", false, 2);
            if (dwnRet)
            {
                if (CloseImmediately)
                {
                    Close();
                }
                MagicShow.ShowMsgDialog(this, "下载完成！", "提示");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ModList.ItemsSource = null;
            ModList.Items.Clear();
            ModVerList.Items.Clear();
            VerFilter_VersList.Clear();
            if (CurseForgeApiClient != null)
            {
                CurseForgeApiClient.Dispose();
                CurseForgeApiClient = null;
            }
            if (ModrinthApiClient != null)
            {
                ModrinthApiClient.Dispose();
                ModrinthApiClient = null;
            }
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }
    }
}
