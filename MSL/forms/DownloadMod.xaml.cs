using CurseForge.APIClient.Models.Mods;
using MSL.controls;
using MSL.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace MSL
{
    /// <summary>
    /// DownloadMods.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadMod : HandyControl.Controls.Window
    {
        private int LoadType = 0;  //0: mods , 1: modpacks 
        private int LoadSource = 0;  //0: Curseforge , 1: Modrinth 
        private readonly string SavingPath;
        private readonly List<int> modIds = new List<int>();
        private readonly List<string> modVersions = new List<string>();
        private readonly List<string> modVersionurl = new List<string>();
        private readonly List<string> modUrls = new List<string>();
        private readonly List<string> imageUrls = new List<string>();
        private readonly List<string> backList = new List<string>();
        private CurseForge.APIClient.ApiClient cfApiClient;

        public DownloadMod(string savingPath, int loadtype = 0,bool canChangeLoadType=true,bool canChangeSource=true)
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
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEvent_CurseForge();
        }

        private async Task LoadEvent_CurseForge()
        {
            try
            {
                lCircle.IsRunning = true;
                lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                if (cfApiClient == null)
                {
                    string token = string.Empty;
                    string _token = (await HttpService.GetApiContentAsync("query/cf_token"))["data"]["cfToken"].ToString();
                    byte[] data = Convert.FromBase64String(_token);
                    string decodedString = Encoding.UTF8.GetString(data);
                    token = decodedString;
                    cfApiClient = new CurseForge.APIClient.ApiClient(token);
                }
                backList.Clear();
                modIds.Clear();
                modVersionurl.Clear();
                modUrls.Clear();
                imageUrls.Clear();
                listBox.ItemsSource = null;
                List<DM_ModInfo> list = new List<DM_ModInfo>();
                if (LoadType == 0)
                {
                    var featuredMods = await cfApiClient.GetFeaturedModsAsync(new GetFeaturedModsRequestBody
                    {
                        GameId = 432,
                        ExcludedModIds = new List<int>(),
                        GameVersionTypeId = null
                    });

                    foreach (var featuredMod in featuredMods.Data.Popular)
                    {
                        list.Add(new DM_ModInfo(featuredMod.Logo.ThumbnailUrl, featuredMod.Name));
                        imageUrls.Add(featuredMod.Logo.ThumbnailUrl);
                        backList.Add(featuredMod.Name);
                        modIds.Add(featuredMod.Id);
                        modUrls.Add(featuredMod.Links.WebsiteUrl.ToString());
                    }
                }
                else if (LoadType == 1)
                {
                    var modpacks = await cfApiClient.SearchModsAsync(432, null, 5128);
                    foreach (var modPack in modpacks.Data)
                    {
                        list.Add(new DM_ModInfo(modPack.Logo.ThumbnailUrl, modPack.Name));
                        imageUrls.Add(modPack.Logo.ThumbnailUrl);
                        backList.Add(modPack.Name);
                        modIds.Add(modPack.Id);
                        modUrls.Add(modPack.Links.WebsiteUrl.ToString());
                    }
                }
                listBox.ItemsSource = list;
                lCircle.IsRunning = false;
                lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                searchMod.IsEnabled = true;
                listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
            }
            catch (Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async void searchMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                searchMod.IsEnabled = false;
                backBtn.IsEnabled = false;
                lCircle.IsRunning = true;
                lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                var searchedMods = await cfApiClient.SearchModsAsync(432, null, null, null, null, null, textBox1.Text);
                backList.Clear();
                modIds.Clear();
                modVersionurl.Clear();
                modUrls.Clear();
                imageUrls.Clear();
                List<DM_ModInfo> list = new List<DM_ModInfo>();
                foreach (var mod in searchedMods.Data)
                {
                    list.Add(new DM_ModInfo(mod.Logo.ThumbnailUrl, mod.Name));
                    imageUrls.Add(mod.Logo.ThumbnailUrl);
                    backList.Add(mod.Name);
                    modIds.Add(mod.Id);
                    modUrls.Add(mod.Links.WebsiteUrl.ToString());
                }
                listBox.ItemsSource = list;
                lCircle.IsRunning = false;
                lCircle.Visibility = Visibility.Collapsed;
                lb01.Visibility = Visibility.Collapsed;
                searchMod.IsEnabled = true;
                listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
            }
            catch(Exception ex)
            {
                await MagicShow.ShowMsgDialogAsync(this, "搜索失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
            }
        }

        private async void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listBoxColumnName.Header.ToString() == "模组列表（双击获取该模组的版本）：")
                {
                    string imageurl = imageUrls[listBox.SelectedIndex];
                    searchMod.IsEnabled = false;
                    backBtn.IsEnabled = false;
                    backBtn.Content = "加载中……";
                    try
                    {
                        var selectedModId = modIds[listBox.SelectedIndex];
                        var modFiles = await cfApiClient.GetModFilesAsync(selectedModId);

                        listBox.ItemsSource = null;
                        modVersions.Clear();

                        listBoxColumnName.Header = "版本列表（双击下载）：";
                        if (LoadType == 0)
                        {
                            List<DM_ModInfo> list = new List<DM_ModInfo>();

                            foreach (var modData in modFiles.Data)
                            {
                                listBox.Items.Add(new DM_ModInfo(imageurl, modData.DisplayName));
                                modVersions.Add(modData.DisplayName);
                                modVersionurl.Add(modData.DownloadUrl);
                            }
                        }
                        else if (LoadType == 1)
                        {
                            foreach (var modData in modFiles.Data)
                            {
                                try
                                {
                                    var serverPackFileId = modData.ServerPackFileId.Value;
                                    var modFile = await cfApiClient.GetModFileAsync(selectedModId, serverPackFileId);
                                    listBox.Items.Add(new DM_ModInfo(imageurl, modFile.Data.DisplayName));
                                    modVersions.Add(modFile.Data.DisplayName);
                                    modVersionurl.Add(modFile.Data.DownloadUrl);
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(ex.ToString()); }

                    if (listBox.Items.Count > 0)
                    {
                        listBox.ScrollIntoView(listBox.Items[0]);
                    }
                    searchMod.IsEnabled = true;
                    backBtn.IsEnabled = true;
                    backBtn.Content = "返回";
                }
                else
                {
                    if (listBox.Items.Count == 0)
                    {
                        return;
                    }
                    if (LoadType == 0)
                    {
                        string savingDir;
                        if (Directory.Exists(SavingPath))
                        {
                            savingDir = SavingPath;
                        }
                        else
                        {
                            FolderBrowserDialog dialog = new FolderBrowserDialog
                            {
                                Description = "请选择模组存放文件夹"
                            };
                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                savingDir = dialog.SelectedPath;
                            }
                            else
                            {
                                return;
                            }
                        }
                        string filename = modVersions[listBox.SelectedIndex].ToString();
                        if (!filename.EndsWith(".jar"))
                        {
                            filename += ".jar";
                        }

                        bool dwnRet = await MagicShow.ShowDownloader(this, modVersionurl[listBox.SelectedIndex], savingDir, filename, "下载中……");
                        if (dwnRet)
                        {
                            MagicShow.ShowMsgDialog(this, "下载完成！", "提升");
                        }
                    }
                    else if (LoadType == 1)
                    {
                        bool dwnRet = await MagicShow.ShowDownloader(this, modVersionurl[listBox.SelectedIndex], SavingPath, "ServerPack.zip", "下载中……");
                        if (dwnRet)
                        {
                            Close();
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("获取MOD失败！您的系统版本可能过旧，请再次尝试或前往浏览器自行下载！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Clear();
            for (int i = 0; i < backList.Count; i++)
            {
                listBox.Items.Add(new DM_ModInfo(imageUrls[i], backList[i]));
            }
            listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            modIds.Clear();
            modVersions.Clear();
            modVersionurl.Clear();
            modUrls.Clear();
            imageUrls.Clear();
            backList.Clear();
            listBox.ItemsSource = null;
        }

        private void Modrinth_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://modrinth.com/");
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
                await LoadEvent_CurseForge();
            }
            else if (LoadSource == 1)
            {

            }
        }

        private async void LoadTypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            LoadType = LoadTypeBox.SelectedIndex;
            await LoadEvent_CurseForge();
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (LoadSource == 0)
            {
                await LoadEvent_CurseForge();
            }
            else if (LoadSource == 1)
            {

            }
        }
    }
}
