using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
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
                LogHelper.Write.Info("主页(Home)开始加载...");
                // 加载快速启动按钮服务器信息
                GetServerConfig();

                if (!isInit)
                {
                    LogHelper.Write.Info("首次加载，等待服务器连接...");
                    // 等待服务器连接
                    bool connected = await WaitForServerConnection(10, cancellationToken);
                    if (connected && !cancellationToken.IsCancellationRequested)
                    {
                        LogHelper.Write.Info("服务器连接成功，开始获取公告。");
                        await GetNotice(true);
                        isInit = true;
                    }
                    else if (!cancellationToken.IsCancellationRequested)
                    {
                        LogHelper.Write.Warn("等待服务器连接超时。");
                        noticeLab.Text = "等待服务器连接超时。";
                    }
                }
                else if (ConfigStore.ApiLink != null && !cancellationToken.IsCancellationRequested)
                {
                    await GetNotice();
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Write.Warn("主页加载操作被取消。");
                return;
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"主页加载时发生未处理的异常: {ex.ToString()}");
                MagicFlowMsg.ShowMessage(ex.Message, 2);
            }
        }

        private async Task<bool> WaitForServerConnection(int timeoutSeconds, CancellationToken cancellationToken)
        {
            for (int i = 0; i < timeoutSeconds && !cancellationToken.IsCancellationRequested; i++)
            {
                if (MainWindow.LoadingCompleted)
                {
                    return true;
                }
                await Task.Delay(1000, cancellationToken);
            }
            return false;
        }

        private async Task GetNotice(bool firstLoad = false)
        {
            string noticeLabText = firstLoad ? string.Empty : noticeLab.Text;

            // 获取公告版本
            string currentNoticeVersion = string.Empty;
            try
            {
                LogHelper.Write.Info("开始从API获取当前公告版本号...");
                currentNoticeVersion = await GetCurrentNoticeVersion();
            }
            catch (HttpRequestException ex)
            {
                LogHelper.Write.Error($"获取公告失败，HTTP请求异常: {ex.ToString()}");
                noticeLab.Text = $"获取公告失败！\n可能是您的网络连接异常，或软件与软件服务器出现问题。若您检查自己的网络并无问题，请及时将此问题反馈！\n错误信息：[HTTP Exception]({ex.InnerException.Message}){ex.Message}";
                return;
            }
            catch (FileNotFoundException ex)
            {
                LogHelper.Write.Error($"获取公告失败，文件未找到(可能缺少.NET Framework): {ex.ToString()}");
                noticeLab.Text = $"获取公告失败！\n请检查您是否安装了.NET Framework 4.7.2运行库！\n错误信息：{ex.Message}";
                return;
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取公告失败，发生未知错误: {ex.ToString()}");
                noticeLab.Text = $"获取公告失败！可能有以下原因：\n1.网络连接异常\n2.未安装.Net Framework 4.7.2运行库\n3.软件Bug，请联系作者进行解决\n错误信息：{ex.Message}";
                return;
            }
            string savedNoticeVersion = GetSavedNoticeVersion();

            // 如果公告版本不同或首次加载且公告为空，则获取新公告
            if (currentNoticeVersion != savedNoticeVersion || string.IsNullOrEmpty(noticeLabText))
            {
                LogHelper.Write.Info($"公告版本不同或首次加载，将从API获取新公告。在线版本: {currentNoticeVersion}, 本地版本: {savedNoticeVersion}");
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
                    LogHelper.Write.Warn("从API获取的公告内容为空。");
                    noticeLabText = "获取公告失败！请检查网络连接是否正常或联系作者进行解决！";
                }

                // 处理recommendations
                var recommendations = tipsResponse["data"]["tips"];
                if (recommendations != null)
                {
                    await Dispatcher.InvokeAsync(() => LoadRecommendations((JArray)recommendations));
                }

                // 保存新公告版本
                SaveNoticeVersion(currentNoticeVersion);
            }
            else
            {
                LogHelper.Write.Info("公告版本一致，无需获取新公告。");
            }

            noticeLab.Text = noticeLabText;
        }

        private async Task<string> GetCurrentNoticeVersion()
        {
            var response = await HttpService.GetApiContentAsync("query/notice?query=id");
            return response["data"]["noticeID"]?.ToString() ?? "0";
        }

        private string GetSavedNoticeVersion()
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
            catch (Exception ex)
            {
                LogHelper.Write.Error($"读取本地配置文件 'MSL/config.json' 中的公告版本失败: {ex.ToString()}");
                return "0";
            }
        }

        private void SaveNoticeVersion(string version)
        {
            try
            {
                LogHelper.Write.Info($"准备保存新公告版本 '{version}' 到配置文件...");
                string configPath = @"MSL\config.json";
                JObject jsonObject = File.Exists(configPath)
                    ? JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8))
                    : new JObject();

                jsonObject["notice"] = version;
                File.WriteAllText(configPath, jsonObject.ToString(), Encoding.UTF8);
                LogHelper.Write.Info("成功保存新公告版本。");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"保存新公告版本 '{version}' 到配置文件失败: {ex.ToString()}");
                MagicFlowMsg.ShowMessage($"保存公告版本时出错: {ex.Message}", 2);
            }
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
                    LogHelper.Write.Info("主窗口加载完成，开始显示公告弹窗。");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), noticeText, "公告");
                });
            });
        }

        private void LoadRecommendations(JArray recommendations)
        {
            LogHelper.Write.Info("开始加载'主页推荐'内容...");
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
            LogHelper.Write.Info($"'主页推荐'内容加载完成，共 {i} 条。");
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
            LogHelper.Write.Info("开始加载快速启动栏的服务器配置...");
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
                    LogHelper.Write.Info("成功获取服务器列表信息，并将服务器名称添加进下拉列表框中。");
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Warn($"加载服务器列表文件 'MSL/ServerList.json' 失败: {ex.Message} 已将startServerDropdown选择项设置为 -1");
                    startServerDropdown.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Warn($"从 'MSL/config.json' 读取已选择的服务器配置失败: {ex.Message} 已将startServerDropdown选择项设置为 -1");
                startServerDropdown.SelectedIndex = -1;
            }
            finally
            {
                if (startServerDropdown.SelectedIndex == -1)
                {
                    selectedItemTextBlock.Text = "创建一个新的服务器";
                }
                else
                {
                    selectedItemTextBlock.Text = startServerDropdown.SelectedItem?.ToString();
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
                LogHelper.Write.Info("快速启动: 未选择服务器，触发'创建新服务器'事件。");
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
                LogHelper.Write.Info($"快速启动: 已选择服务器 (ID: {ServerList.ServerID})，触发'自动打开服务器'事件。");
                AutoOpenServer();
            }
        }

        private void StartServerDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (startServerDropdown.SelectedItem == null || !isInit) return;
            LogHelper.Write.Info($"用户更改了快速启动栏服务器选择: {startServerDropdown.SelectedItem?.ToString()} (索引: {startServerDropdown.SelectedIndex})");
            selectedItemTextBlock.Text = startServerDropdown.SelectedItem?.ToString();
            try
            {
                // 为了健壮性，这里也应该处理文件不存在的情况
                string configPath = @"MSL\config.json";
                JObject _jsonObject = File.Exists(configPath)
                    ? JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8))
                    : new JObject();

                if (_jsonObject["selectedServer"] == null)
                {
                    _jsonObject.Add("selectedServer", startServerDropdown.SelectedIndex.ToString());
                }
                else
                {
                    _jsonObject["selectedServer"].Replace(startServerDropdown.SelectedIndex.ToString());
                }
                File.WriteAllText(configPath, Convert.ToString(_jsonObject), Encoding.UTF8);
                LogHelper.Write.Info("已成功将选择的服务器索引保存到配置文件。");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"保存选择的服务器索引到 'MSL/config.json' 失败: {ex.Message}");
            }

            startServer.IsDropDownOpen = false;
        }
    }
}