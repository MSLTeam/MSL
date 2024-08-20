﻿using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace MSL.pages
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Page
    {
        public static event DeleControl CreateServerEvent;
        public static event DeleControl AutoOpenServer;
        public static event DeleControl GotoP2PEvent;

        public Home()
        {
            InitializeComponent();
        }

        private bool isInit = false;
        private async void Page_Initialized(object sender, EventArgs e)
        {
            GetServerConfig();
            for (int i = 0; i < 10; i++)
            {
                if (MainWindow.serverLink != null)
                {
                    break;
                }
                await Task.Delay(1000);
            }
            if (MainWindow.serverLink == null)
            {
                noticeLab.Text = "加载失败，请检查网络连接！";
            }
            else
            {
                await GetNotice(true);
            }
            isInit = true;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInit)
            {
                return;
            }
            GetServerConfig();
            if (MainWindow.serverLink != null)
            {
                await GetNotice();
            }
        }

        private async Task GetNotice(bool firstLoad = false)
        {
            //公告
            string noticeLabText = noticeLab.Text;
            if (firstLoad)
            {
                noticeLabText = "";
            }
            string noticeversion1;
            try
            {
                string noticeversion = (await HttpService.GetApiContentAsync("query/notice?query=id"))["data"]["noticeID"].ToString();
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (jsonObject["notice"] == null)
                {
                    string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject.Add("notice", "0");
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    noticeversion1 = "0";
                }
                else
                {
                    noticeversion1 = jsonObject["notice"].ToString();
                }
                if (noticeversion1 != noticeversion)
                {
                    var notice = (await HttpService.GetApiContentAsync("query/notice"))["data"]["notice"].ToString();
                    var recommendations = (await HttpService.GetApiContentAsync("query/notice?query=tips"))["data"]["tips"];

                    if (!string.IsNullOrEmpty(notice))
                    {
                        noticeLabText = notice;
                        if (noticeLabText != "")
                        {
                            Shows.ShowMsgDialog(Window.GetWindow(this), noticeLabText, "公告");
                        }
                    }
                    else
                    {
                        noticeLabText = "获取公告失败！请检查网络连接是否正常或联系作者进行解决！";
                    }

                    if (recommendations != null)
                    {
                        LoadRecommendations((JArray)recommendations);
                    }

                    try
                    {
                        string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject["notice"] = noticeversion.ToString();
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    }
                    catch (Exception a)
                    {
                        MessageBox.Show(a.Message);
                    }
                }
                else if (noticeLabText == "")
                {
                    Visibility noticevisible = Visibility.Visible;
                    noticevisible = noticeLab.Visibility;
                    if (noticevisible == Visibility.Visible)
                    {
                        var notice = (await HttpService.GetApiContentAsync("query/notice"))["data"]["notice"].ToString();
                        if (!string.IsNullOrEmpty(notice))
                        {
                            noticeLabText = notice;
                        }
                        else
                        {
                            noticeLabText = "获取公告失败！请检查网络连接是否正常或联系作者进行解决！";
                        }
                        var recommendations = (await HttpService.GetApiContentAsync("query/notice?query=tips"))["data"]["tips"];
                        if (recommendations != null)
                        {
                            LoadRecommendations((JArray)recommendations);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (noticeLabText == "")
                {
                    noticeLabText = "获取公告失败！可能有以下原因：\n1.网络连接异常\n2.未安装.Net Framework 4.7.2运行库\n3.软件Bug，请联系作者进行解决\n错误信息：" + ex.Message;
                }
            }

            if (noticeLabText == "")
            {
                noticeLab.Visibility = Visibility.Collapsed;
                noticeLab.Text = "";
                string _serverLink = MainWindow.serverLink;
                if (_serverLink.Contains("/"))
                {
                    _serverLink = _serverLink.Substring(0, _serverLink.IndexOf("/"));
                }
                noticeImage.Source = new BitmapImage(new Uri("https://file." + _serverLink + "/notice.png"));
            }
            else
            {
                noticeLab.Visibility = Visibility.Visible;
                noticeImage.Source = null;
                noticeLab.Text = noticeLabText;
            }
        }

        private void LoadRecommendations(JArray recommendations)
        {
            recommendBorder.Visibility = Visibility.Visible;
            for (int x = 1; x < 100; x++)
            {
                TextBlock textBlock = RecommendGrid.FindName("RecText" + x.ToString()) as TextBlock;
                if (textBlock != null)
                {
                    RecommendGrid.Children.Remove(textBlock);
                    RecommendGrid.UnregisterName("RecText" + x.ToString());
                    Image img = RecommendGrid.FindName("RecImg" + x.ToString()) as Image;
                    if (img != null)
                    {
                        RecommendGrid.Children.Remove(img);
                        RecommendGrid.UnregisterName("RecImg" + x.ToString());
                    }
                }
                else
                {
                    break;
                }
            }

            int i = 0;
            foreach (var recommendation in recommendations)
            {
                i++;
                Image image = new Image();
                if (i == 1)
                {
                    image.Margin = new Thickness(5, 35 * i, 5, 5);
                }
                else
                {
                    image.Margin = new Thickness(5, 35 + (53 * (i - 1)), 5, 5);
                }
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.VerticalAlignment = VerticalAlignment.Top;
                image.Width = 48;
                image.Height = 48;
                if (recommendation.ToString().StartsWith("*"))
                {
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
                }
                else
                {
                    string _serverLink = MainWindow.serverLink;
                    if (_serverLink.Contains("/"))
                    {
                        _serverLink = _serverLink.Substring(0, _serverLink.IndexOf("/"));
                    }
                    image.Source = new BitmapImage(new Uri("https://file." + _serverLink + "/recommendImg/" + i.ToString() + ".png"));
                }
                RecommendGrid.Children.Add(image);
                RecommendGrid.RegisterName("RecImg" + i.ToString(), image);

                TextBlock textBlock = new TextBlock();
                textBlock.Text = recommendation.ToString();
                textBlock.SetResourceReference(ForegroundProperty, "PrimaryTextBrush");
                if (i == 1)
                {
                    textBlock.Margin = new Thickness(58, 35 * i, 5, 5);
                }
                else
                {
                    textBlock.Margin = new Thickness(58, 35 + (53 * (i - 1)), 5, 5);
                }
                RecommendGrid.Children.Add(textBlock);
                RecommendGrid.RegisterName("RecText" + i.ToString(), textBlock);
            }
        }

        private void GetServerConfig()
        {
            try
            {
                int i = 0;
                JObject _jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                if (_jsonObject["selectedServer"] != null)
                {
                    int _i = int.Parse(_jsonObject["selectedServer"].ToString());
                    if (_i != -1)
                    {
                        i = _i;
                    }
                }
                startServerDropdown.Items.Clear();
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                foreach (var item in jsonObject)
                {
                    startServerDropdown.Items.Add(item.Value["name"]);
                }
                startServerDropdown.SelectedIndex = i;
            }
            catch
            {
                startServerDropdown.SelectedIndex = -1;
            }
            finally
            {
                if (startServerDropdown.SelectedIndex == -1)
                {
                    selectedItemTextBlock.Text = "创建一个新的服务器";
                }
            }
        }
        private void startServer_Click(object sender, RoutedEventArgs e)
        {
            if (startServer.IsDropDownOpen)
            {
                return;
            }
            if (startServerDropdown.SelectedIndex == -1)
            {
                CreateServerEvent();
            }
            else
            {
                int i = 0;
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                foreach (var item in jsonObject)
                {
                    if (i == startServerDropdown.SelectedIndex)
                    {
                        ServerList.ServerID = int.Parse(item.Key);
                        break;
                    }
                    i++;
                }
                AutoOpenServer();
            }
        }

        private void gotoFrpBtn_Click(object sender, RoutedEventArgs e)
        {
            GotoP2PEvent();
        }

        private void startServerDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedItemTextBlock.Text = startServerDropdown.SelectedItem?.ToString();
            JObject _jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            if (_jsonObject["selectedServer"] == null)
            {
                _jsonObject.Add("selectedServer", startServerDropdown.SelectedIndex.ToString());
            }
            else
            {
                _jsonObject["selectedServer"].Replace(startServerDropdown.SelectedIndex.ToString());
            }
            File.WriteAllText(@"MSL\config.json", Convert.ToString(_jsonObject), Encoding.UTF8);
            startServer.IsDropDownOpen = false;
        }
    }
}