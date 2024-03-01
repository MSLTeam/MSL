using CurseForge.APIClient.Models.Mods;
using MSL.controls;
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
    public partial class DownloadMods : HandyControl.Controls.Window
    {
        string Url;
        string Dir;
        public string serverbase;
        int loadType = 0;  //0: mods , 1: modpacks 
        List<int> modIds = new List<int>();
        List<string> modVersions = new List<string>();
        List<string> modVersionurl = new List<string>();
        List<string> modUrls = new List<string>();
        List<string> imageUrls = new List<string>();
        List<string> backList = new List<string>();
        public DownloadMods(int loadtype = 0)
        {
            InitializeComponent();
            loadType = loadtype;
        }
        class MODsInfo
        {
            public string Icon { set; get; }
            public string State { set; get; }

            public MODsInfo(string icon, string state)
            {
                this.Icon = icon;
                this.State = state;
            }
        }

        CurseForge.APIClient.ApiClient cfApiClient = null;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                string token = string.Empty;
                await Task.Run(() =>
                {
                    string _token = Functions.Get("query/cf_token", out string sha256Exp);
                    try
                    {
                        byte[] data = Convert.FromBase64String(_token);
                        string decodedString = Encoding.UTF8.GetString(data);
                        token = decodedString;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("出现错误！请及时向开发者反馈！\n" + ex.Message);
                    }
                });
                backList.Clear();
                imageUrls.Clear();
                listBox.Items.Clear();
                modIds.Clear();
                modVersionurl.Clear();
                modUrls.Clear();
                cfApiClient = new CurseForge.APIClient.ApiClient(token);
                if (loadType == 0)
                {
                    var featuredMods = await cfApiClient.GetFeaturedModsAsync(new GetFeaturedModsRequestBody
                    {
                        GameId = 432,
                        ExcludedModIds = new List<int>(),
                        GameVersionTypeId = null
                    });
                    for (int i = 0; i < featuredMods.Data.Popular.Count; i++)
                    {
                        listBox.Items.Add(new MODsInfo(featuredMods.Data.Popular[i].Logo.ThumbnailUrl, featuredMods.Data.Popular[i].Name));
                        imageUrls.Add(featuredMods.Data.Popular[i].Logo.ThumbnailUrl);
                        backList.Add(featuredMods.Data.Popular[i].Name);
                        modIds.Add(featuredMods.Data.Popular[i].Id);
                        modUrls.Add(featuredMods.Data.Popular[i].Links.WebsiteUrl.ToString());
                    }
                }
                else if (loadType == 1)
                {
                    var modpacks = await cfApiClient.SearchModsAsync(432, null, 5128);
                    
                    for (int i = 0; i < modpacks.Data.Count; i++)
                    {

                        listBox.Items.Add(new MODsInfo(modpacks.Data[i].Logo.ThumbnailUrl, modpacks.Data[i].Name));
                        imageUrls.Add(modpacks.Data[i].Logo.ThumbnailUrl);
                        backList.Add(modpacks.Data[i].Name);

                        modIds.Add(modpacks.Data[i].Id);
                        modUrls.Add(modpacks.Data[i].Links.WebsiteUrl.ToString());
                    }
                }
                lCircle.IsRunning = false;
                lCircle.Visibility = Visibility.Hidden;
                lb01.Visibility = Visibility.Hidden;
                backBtn.IsEnabled = false;
                listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
            }
            catch (Exception ex)
            {
                Shows.ShowMsg(this, "获取模组/整合包失败！请重试或尝试连接代理后再试！\n" + ex.Message, "错误");
                Close();
            }
        }
        private async void searchMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lCircle.IsRunning = true;
                lCircle.Visibility = Visibility.Visible;
                lb01.Visibility = Visibility.Visible;
                var searchedMods = await cfApiClient.SearchModsAsync(432, null, null, null, null, null, textBox1.Text);
                backList.Clear();
                listBox.Items.Clear();
                modIds.Clear();
                modVersionurl.Clear();
                modUrls.Clear();
                imageUrls.Clear();
                for (int i = 0; i < searchedMods.Data.Count; i++)
                {

                    listBox.Items.Add(new MODsInfo(searchedMods.Data[i].Logo.ThumbnailUrl, searchedMods.Data[i].Name));
                    imageUrls.Add(searchedMods.Data[i].Logo.ThumbnailUrl);
                    backList.Add(searchedMods.Data[i].Name);

                    modIds.Add(searchedMods.Data[i].Id);
                    modUrls.Add(searchedMods.Data[i].Links.WebsiteUrl.ToString());
                }
                lCircle.IsRunning = false;
                lCircle.Visibility = Visibility.Hidden;
                lb01.Visibility = Visibility.Hidden;
                backBtn.IsEnabled = false;
                listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
            }
            catch
            {
                MessageBox.Show("获取MOD失败！您的系统版本可能过旧，请再次尝试或前往浏览器自行下载！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listBoxColumnName.Header.ToString() == "模组列表（双击获取该模组的版本）：")
                {
                    string imageurl = imageUrls[listBox.SelectedIndex];
                    //MessageBox.Show(imageurl);
                    backBtn.IsEnabled = true;
                    lCircle.IsRunning = true;
                    lCircle.Visibility = Visibility.Visible;
                    lb01.Visibility = Visibility.Visible;
                    try
                    {
                        var selectedModId = modIds[listBox.SelectedIndex];
                        var modFiles = await cfApiClient.GetModFilesAsync(selectedModId);

                        listBox.Items.Clear();
                        modVersions.Clear();

                        if (loadType == 0)
                        {
                            for (int i = 0; i < modFiles.Data.Count; i++)
                            {
                                listBox.Items.Add(new MODsInfo(imageurl, modFiles.Data[i].DisplayName));
                                modVersions.Add(modFiles.Data[i].DisplayName);
                                modVersionurl.Add(modFiles.Data[i].DownloadUrl);
                            }
                        }
                        else if (loadType == 1)
                        {
                            for (int i = 0; i < modFiles.Data.Count; i++)
                            {
                                try
                                {
                                    var serverPackFileId = modFiles.Data[i].ServerPackFileId.Value;
                                    //MessageBox.Show(serverPackFileId.ToString());
                                    var modFile = await cfApiClient.GetModFileAsync(selectedModId, serverPackFileId);
                                    listBox.Items.Add(new MODsInfo(imageurl, modFile.Data.DisplayName));
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
                    lCircle.IsRunning = false;
                    lCircle.Visibility = Visibility.Hidden;
                    lb01.Visibility = Visibility.Hidden;
                    listBoxColumnName.Header = "版本列表（双击下载）：";
                    if (listBox.Items.Count > 0)
                    {
                        listBox.ScrollIntoView(listBox.Items[0]);
                    }
                }
                else
                {
                    if (loadType == 0)
                    {
                        if (Directory.Exists(serverbase + @"\mods"))
                        {
                            Dir = serverbase + @"\mods";
                        }
                        else
                        {
                            FolderBrowserDialog dialog = new FolderBrowserDialog();
                            dialog.Description = "请选择模组存放文件夹";
                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                Dir = dialog.SelectedPath;
                            }
                        }
                        Url = modVersionurl[listBox.SelectedIndex];
                        string filename = modVersions[listBox.SelectedIndex].ToString();
                        if (!filename.EndsWith(".jar"))
                        {
                            filename += ".jar";
                        }

                        Shows.ShowDownloader(this, Url, Dir, filename, "下载中……");
                    }
                    else if (loadType == 1)
                    {
                        Dir = "MSL";
                        Url = modVersionurl[listBox.SelectedIndex];
                        Shows.ShowDownloader(this, Url, Dir, "ServerPack.zip", "下载中……");
                        Close();
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
                listBox.Items.Add(new MODsInfo(imageUrls[i], backList[i]));
            }
            listBoxColumnName.Header = "模组列表（双击获取该模组的版本）：";
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listBox.Items.Clear();
            modIds.Clear();
            modVersions.Clear();
            modVersionurl.Clear();
            modUrls.Clear();
            imageUrls.Clear();
            backList.Clear();
            GC.Collect();
        }

        private void Modrinth_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://modrinth.com/");
        }
    }
}
