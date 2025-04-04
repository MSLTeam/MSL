using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

        public Home()
        {
            InitializeComponent();
        }

        private bool isInit = true;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetServerConfig();
            if (isInit)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (MainWindow.ServerLink != null)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }
                await GetNotice(true);
                isInit = false;
            }
            else
            {
                if (MainWindow.ServerLink != null)
                {
                    await GetNotice();
                }
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
                        //Console.WriteLine("Notice Loaded");
                        if (noticeLabText != "")
                        {
                            _ = Task.Run(async () =>
                            {
                                while (!MainWindow.LoadingCompleted)
                                {
                                    await Task.Delay(1000);
                                }
                                Dispatcher.Invoke(() =>
                                {
                                    MagicShow.ShowMsgDialog(Window.GetWindow(this), noticeLabText, "公告");
                                });
                            });
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
            noticeLab.Text = noticeLabText;
        }

        private void LoadRecommendations(JArray recommendations)
        {
            recommendBorder.Visibility = Visibility.Visible;

            for (int x = 0; x < 100; x++)
            {
                StackPanel panel = RecommendGrid.FindName("RecPannel" + x.ToString()) as StackPanel;
                if (panel != null)
                {
                    RecommendGrid.Children.Remove(panel);
                    RecommendGrid.UnregisterName("RecPannel" + x.ToString());
                }
                else
                {
                    break;
                }
            }

            int i = 0;
            foreach (var recommendation in recommendations)
            {
                StackPanel recommendationPanel = new StackPanel();
                recommendationPanel.Orientation = Orientation.Horizontal;

                StackPanel recommendationTextPanel = new StackPanel();
                recommendationTextPanel.Orientation = Orientation.Vertical;

                string content = recommendation.ToString();
                string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    // 处理图片标签
                    if (trimmedLine.StartsWith("<img") && trimmedLine.EndsWith("/>"))
                    {
                        Image image = new Image();
                        image.Width = 48;
                        image.Height = 48;
                        
                        
                        var match = Regex.Match(trimmedLine, "url=\"(.*?)\"");
                        if (match.Success)
                        {
                            string imgUrl = match.Groups[1].Value;
                            if (!string.IsNullOrEmpty(imgUrl))
                            {
                                // 加载图标
                                image.Source = new BitmapImage(new Uri(imgUrl));
                            }
                            else
                            {
                                // 如果URL为空，使用默认图标
                                image.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
                            }
                        }
                        else
                        {
                            // 没有图片标签，使用默认图标
                            image.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
                        }
                        recommendationPanel.Children.Add(image);
                        continue;
                    }

                    TextBlock textBlock = new TextBlock();
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.Margin = new Thickness(5, 2, 0, 2);
                    textBlock.VerticalAlignment = VerticalAlignment.Center;
                    textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

                    ProcessLineContent(line, textBlock);
                    recommendationTextPanel.Children.Add(textBlock);
                }

                recommendationPanel.Children.Add(recommendationTextPanel);
                RecommendGrid.Children.Add(recommendationPanel);
                RecommendGrid.RegisterName("RecPannel" + i.ToString(), recommendationPanel);
            }
        }

        private void ProcessLineContent(string line, TextBlock textBlock)
        {
            var hyperlinkRegex = new Regex(@"<hyperlink url=""(.*?)"">(.*?)<\/hyperlink>");
            int lastIndex = 0;

            foreach (Match match in hyperlinkRegex.Matches(line))
            {
                // 添加前面的普通文本
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = line.Substring(lastIndex, match.Index - lastIndex)
                    });
                }

                // 添加超链接
                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(new Run { Text = match.Groups[2].Value });
                hyperlink.Command = HandyControl.Interactivity.ControlCommands.OpenLink;
                hyperlink.CommandParameter = match.Groups[1].Value;
                textBlock.Inlines.Add(hyperlink);
                lastIndex = match.Index + match.Length;
            }

            // 添加剩余文本
            if (lastIndex < line.Length)
            {
                textBlock.Inlines.Add(new Run
                {
                    Text = line.Substring(lastIndex)
                });
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