using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        public static string downloadServerBase;
        public static string downloadServerName;
        public static string downloadServerJava;

        public DownloadServer()
        {
            downloadServerName = string.Empty;
            InitializeComponent();
        }
        string downPath = "";
        string filename = "";
        string downServer = "";
        string downVersion = "";
        //服务端下载

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
                //int url = serverlist1.SelectedIndex;

                string[] downContext = Functions.GetWithSha256("download/server/" + downServer + "/" + downVersion);
                string downUrl = downContext[0];
                string sha256Exp = downContext[1];

                if (serverlist.SelectedItem.ToString().IndexOf("（") + 1 != 0)
                {
                    if (serverlist1.SelectedItem.ToString().IndexOf("（") + 1 != 0)
                    {
                        downPath = downloadServerBase;
                        filename = serverlist.SelectedItem.ToString().Substring(0, serverlist.SelectedItem.ToString().IndexOf("（")) + "-" + serverlist1.SelectedItem.ToString().Substring(0, serverlist1.SelectedItem.ToString().IndexOf("（")) + ".jar";
                    }
                    else
                    {
                        downPath = downloadServerBase;
                        filename = serverlist.SelectedItem.ToString().Substring(0, serverlist.SelectedItem.ToString().IndexOf("（")) + "-" + serverlist1.SelectedItem.ToString() + ".jar";
                    }

                }
                else
                {
                    if (serverlist1.SelectedItem.ToString().IndexOf("（") + 1 != 0)
                    {
                        downPath = downloadServerBase;
                        filename = serverlist.SelectedItem.ToString() + "-" + serverlist1.SelectedItem.ToString().Substring(0, serverlist1.SelectedItem.ToString().IndexOf("（")) + ".jar";
                    }
                    else
                    {
                        downPath = downloadServerBase;
                        filename = serverlist.SelectedItem.ToString() + "-" + serverlist1.SelectedItem.ToString() + ".jar";
                    }
                }
                bool dwnDialog = await Shows.ShowDownloader(this, downUrl, downPath, filename, "下载服务端中……", sha256Exp);
                if (!dwnDialog)
                {
                    Shows.ShowMsgDialog(this, "下载取消！", "提示");
                    return;
                }
                if (File.Exists(downPath + @"\" + filename))
                {
                    if (filename.IndexOf("forge") + 1 != 0)
                    {
                        await Shows.ShowMsgDialog(this, "检测到您下载的是Forge端，开服器将自动进行安装操作，稍后请您不要随意操作，耐心等待安装完毕！", "提示", false);
                        bool installForge = Shows.ShowInstallForge(this, downPath + "\\" + filename, downPath, downloadServerJava);
                        string installReturn;
                        if (installForge)
                        {
                            installReturn = Functions.InstallForge(downloadServerJava, downloadServerBase, filename);
                        }
                        else
                        {
                            installReturn = Functions.InstallForge(downloadServerJava, downloadServerBase, filename, false);
                        }
                        if (installReturn == null)
                        {
                            Shows.ShowMsgDialog(this, "下载失败！", "错误");
                            return;
                        }
                        downloadServerName = installReturn;
                        Close();
                    }
                    else
                    {
                        downloadServerName = filename;
                        Close();
                    }
                }
                else
                {
                    Shows.ShowMsgDialog(this, "下载失败！（文件无法下载/下载后校验完整性失败）\n请重试！", "错误");
                }
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
                string jsonData = Functions.Get("query/available_server_types");
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
                    var resultData = Functions.Get("query/available_versions/" + serverName);
                    string server_des = Functions.Get("query/servers_description/" + serverName);
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
                        var resultData = Functions.Get("query/available_versions/" + serverName);
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

        /*
        private void InstallForge(string downurl, bool fastMode = true)
        {
            try
            {
                string forgeVersion;

                if (downurl.Contains("bmcl"))
                {
                    Match match = Regex.Match(downurl, @"&version=([\w.-]+)&category");
                    if (serverlist1.SelectedItem.ToString().Contains("-"))
                    {
                        string version = serverlist1.SelectedItem.ToString().Split('-')[0];
                        forgeVersion = version + "-" + match.Groups[1].Value;
                    }
                    else
                    {
                        forgeVersion = serverlist1.SelectedItem.ToString() + "-" + match.Groups[1].Value;
                    }
                }
                else
                {
                    Match match = Regex.Match(downurl, @"forge-([\w.-]+)-installer");
                    forgeVersion = match.Groups[1].Value;
                }
                if (!Path.IsPathRooted(downloadServerJava) && File.Exists(downloadServerJava))
                {
                    downloadServerJava = AppDomain.CurrentDomain.BaseDirectory + downloadServerJava;
                }


                if (!fastMode)
                {
                    Process process = new Process();
                    process.StartInfo.WorkingDirectory = downloadServerBase;
                    process.StartInfo.FileName = "cmd";
                    if (downloadServerJava == "Java")
                    {
                        process.StartInfo.Arguments = "/c java -jar " + filename + " -installServer";
                    }
                    else
                    {
                        process.StartInfo.Arguments = @"/c """ + downloadServerJava + @""" -jar " + filename + " -installServer";
                    }
                    process.Start();

                    while (!process.HasExited)
                    {
                        Thread.Sleep(1000);
                    }
                }

                //检测安装成功与否
                try
                {
                    if (File.Exists(downloadServerBase + "\\libraries\\net\\minecraftforge\\forge\\" + forgeVersion + "\\win_args.txt"))
                    {
                        downloadServerName = "@libraries/net/minecraftforge/forge/" + forgeVersion + "/win_args.txt %*";
                        Close();
                    }
                    else if (File.Exists(downloadServerBase + "\\libraries\\net\\neoforged\\neoforge\\" + forgeVersion + "\\win_args.txt"))
                    {
                        downloadServerName = "@libraries/net/neoforged/neoforge/" + forgeVersion + "/win_args.txt %*";
                        Close();
                    }
                    else
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(downloadServerBase);
                        FileInfo[] fileInfo = directoryInfo.GetFiles();
                        bool checkResult = false;
                        foreach (FileInfo file in fileInfo)
                        {
                            if (file.Name.IndexOf("forge-" + forgeVersion) + 1 != 0)
                            {
                                downloadServerName = file.FullName.Replace(downloadServerBase + @"\", "");
                                checkResult = true;
                                break;
                            }
                        }
                        if (!checkResult)
                        {
                            Shows.ShowMsgDialog(this, "下载失败,请多次尝试或使用代理再试！", "错误");
                        }
                        Close();
                    }
                }
                catch
                {
                    Shows.ShowMsgDialog(this, "下载失败！", "错误");
                }
            }
            catch (Exception ex)
            {
                Shows.ShowMsgDialog(this, "出现错误！\n" + ex.ToString(), "错误");
            }
        }
        */

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
