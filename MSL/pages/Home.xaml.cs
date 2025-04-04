using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

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

        private bool isInit = false;
        private CancellationTokenSource _loadingCancellationTokenSource;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _loadingCancellationTokenSource.Token;

            try
            {
                // 加载快速启动按钮服务器信息
                GetServerConfig();

                if (!isInit)
                {
                    // 等待服务器连接
                    bool connected = await WaitForServerConnection(10, cancellationToken);
                    if (connected && !cancellationToken.IsCancellationRequested)
                    {
                        await GetNotice(true);
                        isInit = true;
                    }
                }
                else if (MainWindow.ServerLink != null && !cancellationToken.IsCancellationRequested)
                {
                    await GetNotice();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MagicFlowMsg.ShowMessage(ex.Message, 2);
            }
        }

        private async Task<bool> WaitForServerConnection(int timeoutSeconds, CancellationToken cancellationToken)
        {
            for (int i = 0; i < timeoutSeconds && !cancellationToken.IsCancellationRequested; i++)
            {
                if (MainWindow.ServerLink != null)
                {
                    return true;
                }
                await Task.Delay(1000, cancellationToken);
            }
            return MainWindow.ServerLink != null;
        }

        private async Task GetNotice(bool firstLoad = false)
        {
            try
            {
                string noticeLabText = firstLoad ? string.Empty : noticeLab.Text;

                // 获取公告版本
                string currentNoticeVersion = await GetCurrentNoticeVersion();
                string savedNoticeVersion = await GetSavedNoticeVersion();

                // 如果公告版本不同或首次加载且公告为空，则获取新公告
                if (currentNoticeVersion != savedNoticeVersion || (firstLoad && string.IsNullOrEmpty(noticeLabText)))
                {
                    var noticeTask = HttpService.GetApiContentAsync("query/notice");
                    var tipsTask = HttpService.GetApiContentAsync("query/notice?query=tips");

                    // 并行获取公告和recommendations
                    await Task.WhenAll(noticeTask, tipsTask);

                    var noticeResponse = await noticeTask;
                    var tipsResponse = await tipsTask;

                    // 处理公告内容
                    string notice = noticeResponse["data"]["notice"]?.ToString();
                    if (!string.IsNullOrEmpty(notice))
                    {
                        noticeLabText = notice;

                        // 在加载完成后显示通知对话框
                        if (!string.IsNullOrEmpty(noticeLabText) && currentNoticeVersion != savedNoticeVersion)
                        {
                            await ShowNoticeDialogWhenLoaded(noticeLabText);
                        }
                    }
                    else
                    {
                        noticeLabText = "获取公告失败！请检查网络连接是否正常或联系作者进行解决！";
                    }

                    // 处理recommendations
                    var recommendations = tipsResponse["data"]["tips"];
                    if (recommendations != null)
                    {
                        await Dispatcher.InvokeAsync(() => LoadRecommendations((JArray)recommendations));
                    }

                    // 保存新公告版本
                    await SaveNoticeVersion(currentNoticeVersion);
                }

                noticeLab.Text = noticeLabText;
            }
            catch (Exception ex)
            {
                // 处理异常
                string errorMessage = string.IsNullOrEmpty(noticeLab.Text)
                    ? $"获取公告失败！可能有以下原因：\n1.网络连接异常\n2.未安装.Net Framework 4.7.2运行库\n3.软件Bug，请联系作者进行解决\n错误信息：{ex.Message}"
                    : noticeLab.Text;
                noticeLab.Text = errorMessage;
            }
        }

        private async Task<string> GetCurrentNoticeVersion()
        {
            try
            {
                var response = await HttpService.GetApiContentAsync("query/notice?query=id");
                return response["data"]["noticeID"]?.ToString() ?? "0";
            }
            catch
            {
                return "0";
            }
        }

        private async Task<string> GetSavedNoticeVersion()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string configPath = @"MSL\config.json";
                    if (!File.Exists(configPath))
                    {
                        return "0";
                    }

                    JObject jsonObject = JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8));
                    if (jsonObject["notice"] == null)
                    {
                        jsonObject.Add("notice", "0");
                        File.WriteAllText(configPath, jsonObject.ToString(), Encoding.UTF8);
                        return "0";
                    }

                    return jsonObject["notice"].ToString();
                }
                catch
                {
                    return "0";
                }
            });
        }

        private async Task SaveNoticeVersion(string version)
        {
            await Task.Run(() =>
            {
                try
                {
                    string configPath = @"MSL\config.json";
                    JObject jsonObject = File.Exists(configPath)
                        ? JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8))
                        : new JObject();

                    jsonObject["notice"] = version;
                    File.WriteAllText(configPath, jsonObject.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MagicFlowMsg.ShowMessage($"保存公告版本时出错: {ex.Message}", 2);
                }
            });
        }

        private async Task ShowNoticeDialogWhenLoaded(string noticeText)
        {
            await Task.Run(async () =>
            {
                while (!MainWindow.LoadingCompleted)
                {
                    await Task.Delay(500);
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), noticeText, "公告");
                });
            });
        }

        private void LoadRecommendations(JArray recommendations)
        {
            recommendBorder.Visibility = Visibility.Visible;

            // 清除
            ClearRecommendations();

            // 添加
            int i = 0;
            foreach (var recommendation in recommendations)
            {
                var recommendationPanel = CreateRecommendationPanel(recommendation.ToString());
                RecommendGrid.Children.Add(recommendationPanel);
                RecommendGrid.RegisterName($"RecPannel{i}", recommendationPanel);
                i++;
            }
        }

        private void ClearRecommendations()
        {
            for (int i = 0; i < 100; i++)
            {
                StackPanel panel = RecommendGrid.FindName($"RecPannel{i}") as StackPanel;
                if (panel != null)
                {
                    RecommendGrid.Children.Remove(panel);
                    RecommendGrid.UnregisterName($"RecPannel{i}");
                }
                else
                {
                    break;
                }
            }
        }

        private StackPanel CreateRecommendationPanel(string content)
        {
            StackPanel recommendationPanel = new StackPanel { Orientation = Orientation.Horizontal };
            StackPanel recommendationTextPanel = new StackPanel { Orientation = Orientation.Vertical };

            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 处理图片
                if (trimmedLine.StartsWith("<img") && trimmedLine.EndsWith("/>"))
                {
                    Image image = CreateImageFromImgTag(trimmedLine);
                    recommendationPanel.Children.Add(image);
                    continue;
                }

                // 处理文本
                TextBlock textBlock = CreateTextBlock(line);
                recommendationTextPanel.Children.Add(textBlock);
            }

            recommendationPanel.Children.Add(recommendationTextPanel);
            return recommendationPanel;
        }

        private Image CreateImageFromImgTag(string imgTag)
        {
            Image image = new Image
            {
                Width = 48,
                Height = 48
            };

            var match = Regex.Match(imgTag, "url=\"(.*?)\"");
            if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                image.Source = new BitmapImage(new Uri(match.Groups[1].Value));
            }
            else
            {
                // 使用默认图标
                image.Source = new BitmapImage(new Uri("pack://application:,,,/icon.ico"));
            }

            return image;
        }

        private TextBlock CreateTextBlock(string line)
        {
            TextBlock textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 2, 0, 1),
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

            ProcessLineContent(line, textBlock);
            return textBlock;
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
                object configJson = Config.Read("selectedServer");
                int selectedIndex = -1;

                if (configJson != null && int.TryParse(configJson.ToString(), out int index))
                {
                    selectedIndex = index != -1 ? index : -1;
                }

                // 加载服务器列表
                startServerDropdown.Items.Clear();
                try
                {
                    JObject serverListJson = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                    foreach (var item in serverListJson)
                    {
                        startServerDropdown.Items.Add(item.Value["name"]);
                    }

                    startServerDropdown.SelectedIndex = selectedIndex;
                }
                catch
                {
                    startServerDropdown.SelectedIndex = -1;
                }
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

        private void StartServer_Click(object sender, RoutedEventArgs e)
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

        private void StartServerDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
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