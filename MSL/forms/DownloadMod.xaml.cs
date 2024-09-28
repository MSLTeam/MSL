using CurseForge.APIClient.Models.Mods;
using CurseForge.APIClient;
using Modrinth;
using MSL.controls;
using MSL.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Modrinth.Models;
using CurseForge.APIClient.Models;
using System.Linq;
using System.Threading;

namespace MSL
{
    /// <summary>
    /// DownloadMods.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadMod : HandyControl.Controls.Window
    {
        private int LoadType = 0;  //0: mods , 1: modpacks 
        private int LoadSource = 0;  //0: Curseforge , 1: Modrinth 
        private bool CloseImmediately;
        private readonly string SavingPath;
        private ApiClient CurseForgeApiClient;
        private ModrinthClient ModrinthApiClient;

        public DownloadMod(string savingPath, int loadtype = 0, bool canChangeLoadType = true, bool canChangeSource = true, bool closeImmediately = false)
        {
            InitializeComponent();
            SavingPath = savingPath;
            LoadType = loadtype;
            LoadTypeBox.SelectedIndex = loadtype;
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
                        GameVersionTypeId = null
                    });

                    foreach (var featuredMod in featuredMods.Data.Popular)
                    {
                        list.Add(new DM_ModsInfo(featuredMod.Id.ToString(), featuredMod.Logo.ThumbnailUrl, featuredMod.Name, featuredMod.Links.WebsiteUrl.ToString()));
                    }
                }
                else if (LoadType == 1)
                {
                    var modpacks = await CurseForgeApiClient.SearchModsAsync(432, null, 5128);
                    foreach (var modPack in modpacks.Data)
                    {
                        list.Add(new DM_ModsInfo(modPack.Id.ToString(), modPack.Logo.ThumbnailUrl, modPack.Name, modPack.Links.WebsiteUrl.ToString()));
                    }
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
                foreach (var mod in mods?.Hits)
                {
                    list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Slug, mod.Url));
                }

                ModList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task Search_CurseForge(string name)
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
                    mods = await CurseForgeApiClient.SearchModsAsync(432, searchFilter: name);
                }
                else if (LoadType == 1)
                {
                    mods = await CurseForgeApiClient.SearchModsAsync(432, categoryId: 5128, searchFilter: name);
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

        private async Task Search_Modrinth(string name)
        {
            try
            {
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                SearchResponse mods = null;
                if (LoadType == 0)
                {
                    mods = await ModrinthApiClient.Project.SearchAsync(name);

                    foreach (var mod in mods.Hits)
                    {
                        list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Slug, mod.Url));
                    }
                }
                else if (LoadType == 1)
                {
                    var facets = new FacetCollection()
                    {
                        Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Modpack)
                    };
                    mods = await ModrinthApiClient.Project.SearchAsync(name, facets: facets);
                }
                foreach (var mod in mods?.Hits)
                {
                    list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Slug, mod.Url));
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
                searchMod.IsEnabled = false;
                lCircle.IsRunning = true;
                lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                if (LoadSource == 0)
                {
                    await Search_CurseForge(SearchTextBox.Text);
                }
                else if (LoadSource == 1)
                {
                    await Search_Modrinth(SearchTextBox.Text);
                }
                lCircle.IsRunning = false;
                lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                searchMod.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "搜索失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task ModInfo_CurseForge(DM_ModsInfo info)
        {
            VerFilterPannel.Visibility = Visibility.Collapsed;
            ModInfoLoadingProcess.Content = "0/0";
            var modFiles = await CurseForgeApiClient.GetModFilesAsync(int.Parse(info.ID));
            using var semaphore = new SemaphoreSlim(initialCount: 10, maxCount: 10);
            var loadedCount = 0;
            var totalCount = modFiles.Data.Count;
            bool onlyShowServerPack = false;
            if (LoadType == 1)
            {
                if(await MagicShow.ShowMsgDialogAsync(this, "是否仅展示适用于服务器的整合包文件？\n注意：如果不使用服务器专用包开服，可能会出现无法开服/崩溃的问题！", "询问", true) == true)
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
                    if (LoadType == 0)
                    {
                        modInfo = new DM_ModInfo(info.Icon, modData.DisplayName, modData.DownloadUrl, modData.FileName, string.Join(",", modData.GameVersions));
                    }
                    else if (LoadType == 1)
                    {
                        try
                        {
                            if (!onlyShowServerPack)
                            {
                                var _modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.Id);
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    ModVerList.Items.Add(new DM_ModInfo(info.Icon, _modFile.Data.DisplayName, _modFile.Data.DownloadUrl, _modFile.Data.FileName, string.Join(",", _modFile.Data.GameVersions)));
                                });
                            }
                            var modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.ServerPackFileId.Value);
                            modInfo = new DM_ModInfo(info.Icon, modFile.Data.DisplayName, modFile.Data.DownloadUrl, modFile.Data.FileName, string.Join(",", modFile.Data.GameVersions));
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else
                    {
                        return; // 不支持的 LoadType
                    }

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ModVerList.Items.Add(modInfo);
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
            ModInfoLoadingProcess.Content = "0/0";
            VerFilterPannel.Visibility = Visibility.Visible;
            VerFilterCombo.Items.Clear();
            var modInfo = await ModrinthApiClient.Project.GetAsync(info.ID);
            VerFilterCombo.Items.Add("全部");
            VerFilterCombo.SelectedIndex = 0;
            foreach (var gameVersion in modInfo.GameVersions)
            {
                VerFilterCombo.Items.Add(gameVersion);
            }

            using var semaphore = new SemaphoreSlim(initialCount: 10, maxCount: 10);
            var loadedCount = 0;
            var totalCount = modInfo.Versions.Length;

            async Task LoadAndAddVersion(string modID)
            {
                await semaphore.WaitAsync();
                try
                {
                    var modVersion = await ModrinthApiClient.Version.GetAsync(modID);
                    var modInfo = new DM_ModInfo(
                        info.Icon,
                        modVersion.Name,
                        modVersion.Files[0].Url,
                        modVersion.Files[0].FileName,
                        string.Join(",", modVersion.GameVersions)
                    );

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ModVerList.Items.Add(modInfo);
                        loadedCount++;
                        ModInfoLoadingProcess.Content = $"{loadedCount}/{totalCount}";
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }

            var loadTasks = modInfo.Versions.Select(LoadAndAddVersion);
            await Task.WhenAll(loadTasks);
        }

        private async void ModList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var info = ModList.SelectedItem as DM_ModsInfo;
                backBtn.IsEnabled = false;
                ModInfoGrid.Visibility = Visibility.Visible;
                ModNameLabel.Content = info.Name;
                ModWebsiteUrl.Subject = info.WebsiteUrl;
                ModWebsiteUrl.CommandParameter = info.WebsiteUrl;

                if (LoadSource == 0)
                {
                    await ModInfo_CurseForge(info);
                }
                else
                {
                    await ModInfo_Modrinth(info);
                }
                ModInfoLoadingProcess.Content = "已完成";
                backBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(this, "获取失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            ModInfoGrid.Visibility = Visibility.Collapsed;
            ModVerList.Items.Clear();
            ModVersList.Clear();
        }

        private List<DM_ModInfo> ModVersList = new List<DM_ModInfo>();
        private void VerFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (VerFilterCombo.Items.Count == 0) return;
            if (VerFilterCombo.SelectedItem.ToString() == "全部")
            {
                ModVerList.Items.Clear();
                foreach (var item in ModVersList)
                {
                    ModVerList.Items.Add(item);
                }
            }
            else
            {
                if (ModVersList.Count != 0)
                {
                    ModVerList.Items.Clear();
                    foreach (var item in ModVersList)
                    {
                        ModVerList.Items.Add(item);
                    }
                }
                ModVersList.Clear();
                List<DM_ModInfo> list = new List<DM_ModInfo>();
                foreach (var item in ModVerList.Items)
                {
                    var _item = item as DM_ModInfo;
                    ModVersList.Add(_item);
                    if (_item.MCVersion.Contains(VerFilterCombo.SelectedItem.ToString()))
                    {
                        list.Add(_item);
                    }
                }
                ModVerList.Items.Clear();
                foreach (var item in list)
                {
                    ModVerList.Items.Add(item);
                }
            }
        }

        private async Task LoadEvent()
        {
            lCircle.IsRunning = true;
            lCircle.Visibility = Visibility.Visible;
            lb01.Visibility = Visibility.Visible;
            searchMod.IsEnabled = false;
            if (LoadSource == 0)
            {
                await LoadEvent_CurseForge();
            }
            else if (LoadSource == 1)
            {
                await LoadEvent_Modrinth();
            }
            lCircle.IsRunning = false;
            lCircle.Visibility = Visibility.Collapsed;
            lb01.Visibility = Visibility.Collapsed;
            searchMod.IsEnabled = true;
        }

        private async void LoadSourceBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            LoadSource = LoadSourceBox.SelectedIndex;
            await LoadEvent();
        }

        private async void LoadTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            LoadType = LoadTypeBox.SelectedIndex;
            await LoadEvent();
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadEvent();
        }

        private async void ModVerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModVerList.Items.Count == 0)
            {
                return;
            }
            var iteminfo = ModVerList.SelectedItem as DM_ModInfo;
            Directory.CreateDirectory(SavingPath);
            string filename = iteminfo.FileName;

            bool dwnRet = await MagicShow.ShowDownloader(this, iteminfo.DownloadUrl, SavingPath, filename, "下载中……");
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
        }
    }
}
