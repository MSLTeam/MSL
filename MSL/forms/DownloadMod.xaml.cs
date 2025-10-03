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
        public string FileName { get; set; }
        private int LoadType = 0;  //0: mods , 1: modpacks  , 2: plugins ,3: datapacks
        private int LoadSource = 1;  //0: Curseforge , 1: Modrinth 
        private readonly bool CloseImmediately;
        private readonly string SavingPath;
        private ApiClient CurseForgeApiClient;
        private ModrinthClient ModrinthApiClient;

        public DownloadMod(string savingPath, int loadSource = 1, int loadType = 0, bool canChangeLoadType = true, bool canChangeSource = true, bool closeImmediately = false)
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
                LTB_DataPacks.Visibility = Visibility.Collapsed;
            }
            if (LoadType == 2 || LoadType == 3)
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
                SelMCVerCard.IsEnabled = false;
                SelMCLoaderCard.IsEnabled = false;
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
                    var modpacks = await CurseForgeApiClient.SearchModsAsync(432, null, 4475);
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
                SelMCVerCard.IsEnabled = true;
                SelMCLoaderCard.IsEnabled = true;
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
                var facets = new FacetCollection();
                // 筛选类型
                switch (LoadType)
                {
                    case 0:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Mod));
                        break;
                    case 1:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Modpack));
                        break;
                    case 3:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Datapack));
                        break;
                    default:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Plugin));
                        break;
                }
                // 版本筛选
                if(MinecraftVersionTypeBox.SelectedIndex!=-1 && MinecraftVersionTypeBox.SelectedIndex != 0)
                {
                    facets.Add(Facet.Version(MinecraftVersionTypeBox.Text));
                }
                // 加载器筛选
                if((LoadType == 1 || LoadType == 0) && MinecraftLoaderTypeBox.SelectedIndex != 0 && MinecraftLoaderTypeBox.SelectedIndex != -1)
                {
                    facets.Add(Facet.Category(MinecraftLoaderTypeBox.Text));
                }
                // 执行搜索
                mods = await ModrinthApiClient.Project.SearchAsync("", facets: facets);
                foreach (var mod in mods?.Hits)
                {
                    list.Add(new DM_ModsInfo(mod.ProjectId, mod.IconUrl, mod.Title, mod.Url));
                }

                ModList.ItemsSource = list;
                NowPageLabel.Content = "1";
            }
            catch (OperationCanceledException)
            {
                return;
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
                    mods = await CurseForgeApiClient.SearchModsAsync(432, categoryId: 4475, searchFilter: name, index: index);
                }
                foreach (var mod in mods.Data)
                {
                    //MessageBox.Show(mod.PrimaryCategoryId.ToString());
                    list.Add(new DM_ModsInfo(mod.Id.ToString(), mod.Logo.ThumbnailUrl, mod.Name, mod.Links.WebsiteUrl.ToString()));
                }
                ModList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async Task Search_Modrinth(string name, int offset = 0)
        {
            try
            {
                ModList.ItemsSource = null;
                List<DM_ModsInfo> list = new List<DM_ModsInfo>();
                SearchResponse mods = null;
                var facets = new FacetCollection();
                // 筛选类型
                switch (LoadType)
                {
                    case 0:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Mod));
                        break;
                    case 1:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Modpack));
                        break;
                    case 3:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Datapack));
                        break;
                    default:
                        facets.Add(Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Plugin));
                        break;
                }
                // 版本筛选
                if (MinecraftVersionTypeBox.SelectedIndex != -1 && MinecraftVersionTypeBox.SelectedIndex != 0)
                {
                    facets.Add(Facet.Version(MinecraftVersionTypeBox.Text));
                }
                // 加载器筛选
                if ((LoadType == 1 || LoadType == 0) && MinecraftLoaderTypeBox.SelectedIndex != 0 && MinecraftLoaderTypeBox.SelectedIndex != -1)
                {
                    facets.Add(Facet.Category(MinecraftLoaderTypeBox.Text));
                }
                // 执行搜索
                mods = await ModrinthApiClient.Project.SearchAsync(name, facets: facets,offset:offset);
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
                int nowPage;
                if (NowPageLabel.Content.ToString() == "精选")
                {
                    nowPage = 0;
                }
                else
                {
                    nowPage = int.Parse(NowPageLabel.Content.ToString());
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
                int nowPage;
                if (NowPageLabel.Content.ToString() == "精选")
                {
                    nowPage = 0;
                }
                else
                {
                    nowPage = int.Parse(NowPageLabel.Content.ToString());
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

            // 获取用户是否仅展示适用于服务器的整合包文件
            if (LoadType == 1 && await MagicShow.ShowMsgDialogAsync(this,
                "是否仅展示适用于服务器的整合包文件？\n注意：如果不使用服务器专用包开服，可能会出现无法开服/崩溃的问题！", "询问", true) == true)
            {
                onlyShowServerPack = true;
            }

            async Task LoadAndAddModInfo(CurseForge.APIClient.Models.Files.File modData)
            {
                await semaphore.WaitAsync();
                try
                {
                    // 用于保存要显示的 modInfo
                    DM_ModInfo modInfo = null;
                    DM_ModInfo _modInfo = null;

                    if (LoadType == 0)
                    {
                        // 直接加载 Mod 信息
                        modInfo = await CreateModInfo(modData);
                    }
                    else if (LoadType == 1)
                    {
                        if (!onlyShowServerPack)
                        {
                            // 加载非服务器专用包文件
                            var _modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.Id);
                            _modInfo = await CreateModInfo(_modFile.Data);
                        }

                        // 加载服务器专用包文件
                        if (modData.ServerPackFileId.HasValue)
                        {
                            var modFile = await CurseForgeApiClient.GetModFileAsync(int.Parse(info.ID), modData.ServerPackFileId.Value);
                            modInfo = await CreateModInfo(modFile.Data);
                        }
                        else
                        {
                            // 处理没有 ServerPackFileId 的情况
                            Console.WriteLine("ServerPackFileId is null for " + modData.DisplayName);
                            return;  // 没有服务器专用包，退出处理
                        }
                    }
                    else
                    {
                        return; // 不支持的 LoadType
                    }

                    // 在 UI 线程中更新界面
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
                catch (Exception ex)
                {
                    // 处理异常，避免整个流程崩溃
                    Console.WriteLine($"Error loading mod info: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }

            async Task<DM_ModInfo> CreateModInfo(CurseForge.APIClient.Models.Files.File modData)
            {
                // 构造并返回一个 DM_ModInfo 对象
                var dependencies = await Task.WhenAll(modData.Dependencies.Select(s => CurseForgeApiClient.GetModAsync(s.ModId)));
                var dependenciesNames = string.Join(",", dependencies.Select(p => p.Data.Name));
                var gameVersions = string.Join(",", modData.GameVersions);

                return new DM_ModInfo(
                    modData.DisplayName,
                    modData.DownloadUrl,
                    modData.FileName,
                    "",
                    dependenciesNames,
                    gameVersions
                );
            }

            var loadTasks = modFiles.Data.Select(LoadAndAddModInfo);
            await Task.WhenAll(loadTasks);

        }

        private async Task ModInfo_Modrinth(DM_ModsInfo info)
        {
            var modInfo = await ModrinthApiClient.Project.GetAsync(info.ID);
            ModInfoLoadingProcess.Content = "加载中";
            VerFilterCombo.Items.Add("全部");
            VerFilterCombo.SelectedIndex = 0;
            foreach (var gameVersion in modInfo.GameVersions.Reverse())
            {
                VerFilterCombo.Items.Add(gameVersion);
            }
            //var loadedCount = 0;
            var modInfo1 = await ModrinthApiClient.Version.GetProjectVersionListAsync(info.ID);
            foreach (var version in modInfo1)
            {
                //MessageBox.Show(version.Name);
                foreach (var file in version.Files)
                {
                    //MessageBox.Show(file.FileName);
                    DM_ModInfo DMmodInfo = null;

                    if (LoadType == 1)
                    {
                        DMmodInfo = new DM_ModInfo(
                            version.Name,
                            file.Url,
                            file.FileName,
                            string.Join(",", version.Loaders),
                            "",
                            GetMcVersion(version.GameVersions)
                        );
                    }
                    else
                    {
                        DMmodInfo = new DM_ModInfo(
                            version.Name,
                            file.Url,
                            file.FileName,
                            string.Join(",", version.Loaders),
                            "",
                            GetMcVersion(version.GameVersions)
                        );
                    }
                    ModVerList.Items.Add(DMmodInfo);  // 将每个 modInfo 添加到列表
                    VerFilter_VersList.Add(version.GameVersions);
                }
            }

            /*
            using var semaphore = new SemaphoreSlim(50);

            async Task LoadAndAddVersion(string modID)
            {
                await semaphore.WaitAsync();
                try
                {
                    var modVersion = await ModrinthApiClient.Version.GetAsync(modID);
                    var modGameVer = modVersion.GameVersions;

                    // 遍历 modVersion.Files 中的每个文件，创建一个 DM_ModInfo 实例
                    var modInfos = new List<DM_ModInfo>();  // 创建一个列表保存所有生成的 DM_ModInfo 实例

                    foreach (var file in modVersion.Files)
                    {
                        DM_ModInfo modInfo = null;

                        if (LoadType == 1)
                        {
                            modInfo = new DM_ModInfo(
                                modVersion.Name,
                                file.Url,
                                file.FileName,
                                string.Join(",", modVersion.Loaders),
                                "",
                                GetMcVersion(modGameVer)
                            );
                        }
                        else
                        {
                            modInfo = new DM_ModInfo(
                                modVersion.Name,
                                file.Url,
                                file.FileName,
                                string.Join(",", modVersion.Loaders),
                                string.Join(",", (await Task.WhenAll(modVersion.Dependencies.Select(s => ModrinthApiClient.Project.GetAsync(s.ProjectId)))).Select(p => p.Title)),
                                GetMcVersion(modGameVer)
                            );
                        }

                        modInfos.Add(modInfo);  // 将每个文件对应的 modInfo 添加到列表中
                    }

                    // 使用 Dispatcher 更新 UI，将所有的 modInfo 添加到 ModVerList 中
                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var info in modInfos)
                        {
                            ModVerList.Items.Add(info);  // 将每个 modInfo 添加到列表
                        }
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
            */
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
                    MVL_Dependency.Width = 100;
                    await ModInfo_CurseForge(info);
                }
                else
                {
                    VerFilterPannel.Visibility = Visibility.Visible;
                    MVL_Platform.Width = 100;
                    MVL_Dependency.Width = 0;
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
            await LoadMCVersion();
            //lCircle.IsRunning = false;
            //lCircle.Visibility = Visibility.Collapsed;
            lb01.Visibility = Visibility.Collapsed;
            ModListGrid.IsEnabled = true;
        }

        private async Task LoadMCVersion()
        {
            try
            {
                LogHelper.Write.Info("[下载资源页]正在从原版服务端获取 MC 版本列表");
                MinecraftVersionTypeBox.Items.Clear();
                var mcVersions = await HttpService.GetApiContentAsync("query/available_versions/vanilla");
                MinecraftVersionTypeBox.Items.Add("全部");
                foreach (var mcVersion in mcVersions["data"]["versionList"])
                {
                    MinecraftVersionTypeBox.Items.Add(mcVersion.ToString());
                }
                MinecraftVersionTypeBox.SelectedIndex = 0;
            }
            catch (Exception ex) {
                MinecraftVersionTypeBox.Items.Clear();
                MinecraftVersionTypeBox.Items.Add("全部");
                MinecraftVersionTypeBox.SelectedIndex = 0;
                LogHelper.Write.Error("[下载资源页]获取 MC 版本列表失败" + ex.ToString());
            }

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
                LTB_DataPacks.Visibility = Visibility.Collapsed;
            }
            else
            {
                LTB_Plugins.Visibility = Visibility.Visible;
                LTB_DataPacks.Visibility = Visibility.Visible;
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
            if (LoadType == 2 || LoadType == 3)
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
            FileName = iteminfo.FileName;
            //MessageBox.Show(iteminfo.DownloadUrl);
            bool dwnRet = await MagicShow.ShowDownloader(this, iteminfo.DownloadUrl, SavingPath, FileName, "下载中……", "", false);
            if (dwnRet)
            {
                if (CloseImmediately)
                {
                    Close();
                    return;
                }
                MagicShow.ShowMsgDialog(this, "下载完成！", "提示");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
            ModList.ItemsSource = null;
            ModList.Items.Clear();
            ModVerList.Items.Clear();
            VerFilter_VersList.Clear();
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }
    }
}
