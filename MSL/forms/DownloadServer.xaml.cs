using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        List<string> serverdownurl = new List<string>();
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
                DialogShow.ShowMsg(this, "请先选择一个版本！", "警告");
                return;
            }
            DownloadServerFunc();
        }
        private void serverlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DownloadServerFunc();
        }
        void DownloadServerFunc()
        {

                downVersion = serverlist1.SelectedItem.ToString();
                downServer = serverlist.SelectedItem.ToString();

            if (serverlist1.SelectedIndex != -1)
            {
                int url = serverlist1.SelectedIndex;

                string downUrl = Functions.Get("download/server/" + downServer + "/" + downVersion,MainWindow.serverLinkNew);

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
                bool dwnDialog = DialogShow.ShowDownload(this, downUrl, downPath, filename, "下载服务端中……");
                if (!dwnDialog)
                {
                    DialogShow.ShowMsg(this, "下载取消！", "提示");
                    return;
                }
                if (File.Exists(downPath + @"\" + filename))
                {
                    if (filename.IndexOf("forge") + 1 != 0)
                    {
                        DialogShow.ShowMsg(this, "检测到您下载的是Forge端，开服器将自动进行安装操作，稍后请您不要随意移动鼠标且不要随意触碰键盘，耐心等待安装完毕！", "提示");
                        InstallForge();
                    }
                    else
                    {
                        downloadServerName = filename;
                        Close();
                    }
                }
                else
                {
                    DialogShow.ShowMsg(this, "下载失败！", "错误");
                }
            }
        }
        void GetServer()
        {
            try
            {
                if (Functions.Get("") != "200")
                {
                    MainWindow.serverLink = "waheal.top";
                }
            }
            catch
            {
                MainWindow.serverLink = "waheal.top";
            }
            Dispatcher.Invoke(() =>
            {
                serverlist.ItemsSource = null;
                serverlist1.ItemsSource = null;
                //serverurl.Clear();
                serverdownurl = null;
            });
            try
            {
                string jsonData = Functions.Get("query/available_server_types",MainWindow.serverLinkNew);
                string[] serverTypes = JsonConvert.DeserializeObject<string[]>(jsonData);
                Dispatcher.Invoke(() =>
                {
                    /*
                    foreach (var serverType in serverTypes)
                    {
                        serverlist.Items.Add(serverType);
                    }*/
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
                    serverdownurl = null;
                    getservermsg.Visibility = Visibility.Visible;
                    lCircle.Visibility = Visibility.Visible;
                    serverName = serverlist.SelectedItem.ToString();
                    //serverName = serverlist.SelectedItem.ToString();
                });
                try
                {
                    var resultData = Functions.Get("query/available_versions/" + serverName, MainWindow.serverLinkNew);
                    string server_des = Functions.Get("query/servers_description/" + serverName, MainWindow.serverLinkNew);
                    JArray serverVersions = JArray.Parse(resultData);
                    List<string> sortedVersions = serverVersions.ToObject<List<string>>().OrderByDescending(v => Functions.VersionCompare(v)).ToList();
                    Dispatcher.Invoke(() =>
                    {
                        serverlist1.ItemsSource = sortedVersions;
                        getservermsg.Visibility = Visibility.Hidden;
                        lCircle.Visibility = Visibility.Hidden;
                        server_d.Text = "你选择的是：" +server_des;
                    });

                }
                catch
                {
                    try
                    {
                        var resultData = Functions.Get("query/available_versions/" + serverName, MainWindow.serverLinkNew);
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

        void InstallForge()
        {
            try
            {
                string forgeVersion;
                string serverDownUrl = serverdownurl[serverlist1.SelectedIndex].ToString();

                if (serverDownUrl.Contains("bmcl"))
                {
                    Match match = Regex.Match(serverDownUrl, @"&version=([\w.-]+)&category");
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
                    Match match = Regex.Match(serverDownUrl, @"forge-([\w.-]+)-installer");
                    forgeVersion = match.Groups[1].Value;
                }
                if (!Path.IsPathRooted(downloadServerJava) && File.Exists(downloadServerJava))
                {
                    downloadServerJava = AppDomain.CurrentDomain.BaseDirectory + downloadServerJava;
                }
                Directory.SetCurrentDirectory(downloadServerBase);
                Process process = new Process();
                process.StartInfo.FileName = downloadServerJava;
                process.StartInfo.Arguments = "-jar " + filename + " -installServer";
                //process.StartInfo.Arguments = "-jar " + filename + " -mirror https://bmclapi2.bangbang93.com/maven/ -installServer";
                process.Start();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                try
                {
                    while (!process.HasExited)
                    {
                        Thread.Sleep(1000);
                    }
                    if (File.Exists(downloadServerBase + "\\libraries\\net\\minecraftforge\\forge\\" + forgeVersion + "\\win_args.txt"))
                    {
                        downloadServerName = "@libraries/net/minecraftforge/forge/" + forgeVersion + "/win_args.txt %*";
                        //CreateServer.isCreateForge = true;
                        Close();
                    }
                    else
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(downloadServerBase);
                        FileInfo[] fileInfo = directoryInfo.GetFiles();
                        foreach (FileInfo file in fileInfo)
                        {
                            if (file.Name.IndexOf("forge-" + forgeVersion) + 1 != 0)
                            {
                                downloadServerName = file.FullName.Replace(downloadServerBase + @"\", "");
                                break;
                            }
                            else
                            {
                                DialogShow.ShowMsg(this, "下载失败,请多次尝试或使用代理再试！", "错误");
                            }
                        }
                        Close();
                    }
                }
                catch
                {
                    DialogShow.ShowMsg(this, "下载失败！", "错误");
                }
            }
            catch(Exception ex)
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                DialogShow.ShowMsg(this, "出现错误！\n"+ex.Message, "错误");
            }
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
