using HandyControl.Controls;
using HandyControl.Tools;
using MSL.controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Window = System.Windows.Window;

namespace MSL.utils
{
    internal class MagicShow
    {
        /// <summary>
        /// 显示MSG对话框（异步执行，不可等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        public static void ShowMsgDialog(string text, string title, UIElement uIElement = null, UIElement _window = null, bool isDangerPrimaryBtn = false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowMsgDialog(Functions.GetWindow(_window), text, title, uIElement, isDangerPrimaryBtn);
        }

        /// <summary>
        /// 显示MSG对话框（异步执行，可以等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        /// <param name="showPrimaryBtn">是否显示确认按钮</param>
        /// <param name="closeBtnContext">关闭按钮文字内容</param>
        /// <param name="primaryBtnContext">确认按钮文字内容</param>
        /// <returns>返回值：true；false</returns>
        public static async Task<bool> ShowMsgDialogAsync(string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "CANCEL", string primaryBtnContext = "CONFIRM", UIElement uIElement = null, UIElement _window = null, bool isDangerPrimaryBtn = false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            bool _ret = await MagicDialog.ShowMsgDialog(Functions.GetWindow(_window), text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement, isDangerPrimaryBtn);
            return _ret;
        }

        /// <summary>
        /// 显示MSG对话框（异步执行，不可等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        public static void ShowMsgDialog(Window _window, string text, string title, UIElement uIElement = null, bool isDangerPrimaryBtn = false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowMsgDialog(_window, text, title, uIElement, isDangerPrimaryBtn);
        }

        /// <summary>
        /// 显示MSG对话框（异步执行，可以等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        /// <param name="showPrimaryBtn">是否显示确认按钮</param>
        /// <param name="closeBtnContext">关闭按钮文字内容</param>
        /// <param name="primaryBtnContext">确认按钮文字内容</param>
        /// <returns>返回值：true；false</returns>
        public static async Task<bool> ShowMsgDialogAsync(Window _window, string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "CANCEL", string primaryBtnContext = "CONFIRM", UIElement uIElement = null, bool isDangerPrimaryBtn = false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            bool _ret = await MagicDialog.ShowMsgDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement, isDangerPrimaryBtn);
            return _ret;
        }

        /// <summary>
        /// 显示MSG对话窗体（阻塞当前线程）
        /// </summary>
        /// <param name="window">父窗体</param>
        /// <param name="dialogText">对话窗体内容</param>
        /// <param name="dialogTitle">标题</param>
        /// <param name="primaryBtnVisible">确认按钮可见性</param>
        /// <param name="closeText">关闭按钮文字</param>
        /// <param name="primaryText">确认按钮文字</param>
        /// <returns>返回值：0代表点击取消按钮；1代表点击确认按钮；2代表窗体被关闭</returns>
        public static int ShowMsg(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "CANCEL", string primaryText = "CONFIRM")
        {
            try
            {
                MessageWindow messageWindow = new MessageWindow(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText)
                {
                    Owner = window
                };
                messageWindow.ShowDialog();
                if (messageWindow._closeBtnReturn)
                {
                    return 2;
                }
                else
                {
                    if (messageWindow._dialogReturn)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch
            {
                return 2;
            }
        }

        public static async Task<string> ShowInput(Window _window, string dialogText, string textboxText = "", bool passwordMode = false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            string _ret = await MagicDialog.ShowInpputDialog(_window, dialogText, textboxText, passwordMode);
            return _ret;
        }

        public static async Task<string[]> ShowInstallForge(Window _window, string installPath, string forgeFileName, string javaPath)
        {
            MagicDialog MagicDialog = new MagicDialog();
            return await MagicDialog.ShowInstallForgeDialog(_window, installPath, forgeFileName, javaPath);
        }

        /// <summary>
        /// 下载器
        /// </summary>
        /// <param name="_window">显示在哪个窗体中</param>
        /// <param name="downloadurl">下载地址</param>
        /// <param name="downloadPath">文件存放目录</param>
        /// <param name="filename">文件名</param>
        /// <param name="downloadinfo">下载信息（label中显示的内容）</param>
        /// <param name="sha256">验证完整性（可选）</param>
        /// <param name="closeDirectly">下载失败后是否直接关闭下载对话框</param>
        /// <param name="headerMode">UA标识：0等于自动检测（MSL Downloader或无Header），1等于无Header，2等于MSL Downloader，3等于伪装浏览器Header</param>
        /// <returns>true下载成功；false下载取消/失败</returns>
        public static async Task<bool> ShowDownloader(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, bool enableParalle = true, int headerMode = 1)
        {
            MagicDialog MagicDialog = new MagicDialog();
            int _ret = await MagicDialog.ShowDownloadDialog(_window, downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, enableParalle, headerMode);
            if (_ret == 1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 下载器
        /// </summary>
        /// <param name="_window">显示在哪个窗体中</param>
        /// <param name="downloadurl">下载地址</param>
        /// <param name="downloadPath">文件存放目录</param>
        /// <param name="filename">文件名</param>
        /// <param name="downloadinfo">下载信息（label中显示的内容）</param>
        /// <param name="sha256">验证完整性（可选）</param>
        /// <param name="closeDirectly">下载失败后是否直接关闭下载对话框</param>
        /// <param name="headerMode">UA标识：0等于自动检测（MSL Downloader或无Header），1等于无Header，2等于MSL Downloader，3等于伪装浏览器Header</param>
        /// <returns>0未开始下载（或下载中），1下载完成，2下载取消，3下载失败</returns>
        public static async Task<int> ShowDownloaderWithIntReturn(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, bool enableParalle = true, int headerMode = 1)
        {
            MagicDialog MagicDialog = new MagicDialog();
            int _ret = await MagicDialog.ShowDownloadDialog(_window, downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, enableParalle, headerMode);
            return _ret;
        }


    }

    internal class MagicDialog
    {
        private Window window;
        //private Dialog dialog;
        private string token = Guid.NewGuid().ToString();

        public void ShowTextDialog(Window _window, string text)
        {
            window = _window;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(new TextDialog(text), token);
        }

        public void ShowTextDialog(string text, UIElement _window = null)
        {
            window = Functions.GetWindow(_window);
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(new TextDialog(text), token);
        }

        public void CloseTextDialog()
        {
            // window?.Focus();
            Dialog.Close(token);
            window = null;
            token = null;
            // dialog = null;
        }


        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(Window _window, string text, string title, UIElement uIElement, bool isDangerPrimaryBtn)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, false, "", "", uIElement, isDangerPrimaryBtn);
            msgDialog.CloseDialog += CloseMsgDialog;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(msgDialog, token);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext, string primaryBtnContext, UIElement uIElement, bool isDangerPrimaryBtn)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement, isDangerPrimaryBtn);
            msgDialog.CloseDialog += CloseMsgDialog;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(msgDialog, token);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return msgDialog._dialogReturn;
        }

        public async Task<int> ShowDownloadDialog(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, bool enableParalle = true, int headerMode = 1)
        {
            window = _window;
            DownloadDialog dwnDialog = new DownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, enableParalle, headerMode);
            dwnDialog.CloseDialog += CloseMsgDialog;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(dwnDialog, token);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return dwnDialog._dialogReturn;
        }

        public async Task<string> ShowInpputDialog(Window _window, string dialogText, string textboxText = "", bool passwordMode = false)
        {
            window = _window;
            InputDialog inputDialog = new InputDialog(dialogText, textboxText, passwordMode);
            inputDialog.CloseDialog += CloseMsgDialog;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(inputDialog, token);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return inputDialog._dialogReturn;
        }

        public async Task<string[]> ShowInstallForgeDialog(Window _window, string installPath, string forgeFileName, string javaPath)
        {
            window = _window;
            InstallForgeDialog _dialog = new InstallForgeDialog(installPath, forgeFileName, javaPath);
            _dialog.CloseDialog += CloseMsgDialog;
            // window?.Focus();
            Dialog.SetToken(window, token);
            Dialog.Show(_dialog, token);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            string[] strings = [_dialog.DialogReturn.ToString(), _dialog.McVersion];
            _dialog.DownloadDisplay.Dispose();
            _dialog = null;
            return strings;
        }

        private void CloseMsgDialog()
        {
            _tcs?.TrySetResult(true);
            // window?.Focus();
            Dialog.Close(token);
            _tcs = null;
            window = null;
            token = null;
            // dialog = null;
        }
    }

    public class MagicFlowMsg
    {
        private static Panel targetContainer;
        private static StackPanel growlPanel;
        private static StackPanel messageStackPanel;

        #region ShowMessage（原有逻辑，支持自定义容器）

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">要显示的消息文本</param>
        /// <param name="type">消息类型：0=Primary，1=Success，2=Danger，3=Warning，4=Info</param>
        /// <param name="seconds">显示时长（秒，默认 3 秒）</param>
        /// <param name="panel">容器（为 null 时自动获取当前活动窗口）</param>
        /// <param name="_growlPanel">自定义 GrowlPanel（StackPanel），优先级高于 panel</param>
        public static void ShowMessage(string message, int type = 0, int seconds = 3,
            UIElement panel = null, UIElement _growlPanel = null)
        {
            if (_growlPanel == null)
            {
                panel ??= WindowHelper.GetActiveWindow();
                if (panel is Page page)
                    panel = Window.GetWindow(page);
                if (panel is Window window)
                    panel = window.Content as UIElement;

                if (panel is Panel targetPanel)
                {
                    if (targetPanel.FindName("GrowlPanel") != null)
                    {
                        growlPanel = targetPanel.FindName("GrowlPanel") as StackPanel;
                    }
                    else
                    {
                        growlPanel = null;
                        if (messageStackPanel == null)
                        {
                            CreatMsgContainer(targetPanel);
                        }
                        else
                        {
                            if (!targetPanel.Children.Contains(messageStackPanel))
                            {
                                messageStackPanel.Visibility = Visibility.Collapsed;
                                targetContainer.Children.Remove(messageStackPanel);
                                messageStackPanel = null;
                                CreatMsgContainer(targetPanel);
                            }
                        }
                    }
                }
            }
            else
            {
                growlPanel = _growlPanel as StackPanel;
            }

            var msgLabel = new Label
            {
                Margin = new Thickness(10, 10, 10, 0),
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = message
                },
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var styleKey = type switch
            {
                1 => "LabelSuccess",
                2 => "LabelDanger",
                3 => "LabelWarning",
                4 => "LabelInfo",
                _ => "LabelPrimary"
            };
            msgLabel.Style = (Style)Application.Current.Resources[styleKey];

            if (growlPanel != null)
                growlPanel.Children.Insert(0, msgLabel);
            else
                messageStackPanel?.Children.Insert(0, msgLabel);

            ShowMessageAnimation(msgLabel, seconds);
        }

        private static void CreatMsgContainer(Panel panel)
        {
            messageStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            panel.Children.Add(messageStackPanel);
            targetContainer = panel;
        }

        private static void ShowMessageAnimation(Label msgLabel, int seconds)
        {
            msgLabel.Visibility = Visibility.Visible;
            msgLabel.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            timer.Tick += (s, e) => { timer.Stop(); HideMessageAnimation(msgLabel); };
            timer.Start();
        }

        private static void HideMessageAnimation(Label msgLabel)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) =>
            {
                msgLabel.Visibility = Visibility.Collapsed;
                if (growlPanel != null)
                    growlPanel.Children.Remove(msgLabel);
                else if (messageStackPanel != null)
                {
                    messageStackPanel.Children.Remove(msgLabel);
                    if (!messageStackPanel.Children.OfType<Label>().Any())
                    {
                        messageStackPanel.Visibility = Visibility.Collapsed;
                        targetContainer.Children.Remove(messageStackPanel);
                        messageStackPanel = null;
                    }
                }
            };
            msgLabel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        #endregion

        #region ShowAskMessage（自定义询问框，支持倒计时自动确认）

        /// <summary>
        /// 显示带倒计时的询问框。
        /// 倒计时结束后自动以"确认"执行回调；用户可提前点击确认/取消。
        /// </summary>
        /// <param name="message">询问消息文本</param>
        /// <param name="callback">
        ///     回调：true = 用户确认 或 倒计时自动触发；false = 用户主动取消
        /// </param>
        /// <param name="waitSeconds">倒计时秒数（默认 3 秒）</param>
        /// <param name="confirmText">确认按钮文字（默认"立即重启"）</param>
        /// <param name="cancelText">取消按钮文字（默认"取消"）</param>
        /// <param name="container">
        ///     指定父容器（Panel）。为 null 时自动寻找当前活动窗口的 Content。
        /// </param>
        public static void ShowAskMessage(
            string message,
            Action<bool> callback,
            int waitSeconds = 3,
            string titleText = "你好",
            string confirmText = "确定",
            string cancelText = "取消",
            Panel container = null)
        {
            // 找到放置弹框的容器
            Panel host = container ?? ResolveHostPanel();
            if (host == null)
            {
                // 找不到容器时直接当作确认处理，不阻塞
                callback?.Invoke(true);
                return;
            }

            // 移除同 Tag 的旧询问框
            const string askTag = "__MagicAskCard__";
            var old = host.Children.OfType<Border>()
                          .FirstOrDefault(b => b.Tag?.ToString() == askTag);
            old?.Dispatcher.Invoke(() => host.Children.Remove(old));

            bool hasResponded = false; // 防止回调被执行两次

            // 外层卡片
            var card = new MagicCard1
            {
                Tag = askTag,
                Padding = new Thickness(15),
                Margin = new Thickness(15),
                MaxWidth = 360,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            // 内部布局
            var root = new StackPanel { Orientation = Orientation.Vertical };

            // 图标 + 标题行
            var titleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            titleRow.Children.Add(new TextBlock
            {
                Text = "⚠",
                FontSize = 16,
                Foreground = (Brush)Application.Current.Resources["WarningBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = titleText,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["WarningBrush"],
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(titleRow);

            // 消息文本
            root.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var progressBar = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = (Brush)Application.Current.Resources["WarningBrush"],
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            // 用 Grid 叠放
            var progressGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            progressGrid.Children.Add(progressBar);
            root.Children.Add(progressGrid);

            // 倒计时数字标签
            var countLabel = new TextBlock
            {
                Text = $"倒计时： {waitSeconds}s",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(countLabel);

            // 按钮行
            var btnRow = new UniformSpacingPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnCancel = new Button
            {
                Content = cancelText
            };

            var btnConfirm = new Button
            {
                Content = confirmText
            };

            btnRow.Children.Add(btnCancel);
            btnRow.Children.Add(btnConfirm);
            root.Children.Add(btnRow);

            card.Content = root;

            // 插入容器
            host.Children.Insert(0, card);

            // 淡入动画
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            card.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            DispatcherTimer countTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            if (waitSeconds <= 0)
            {
                // 没有倒计时需求，直接显示完整进度条
                progressBar.Width = double.NaN; // 自动填满
                countLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // ── 7. 进度条宽度动画（需要在布局完成后获取实际宽度）─────
                card.Loaded += (_, _) =>
                {
                    double fullWidth = progressGrid.ActualWidth;
                    if (fullWidth <= 0) fullWidth = card.ActualWidth - 36;

                    var barAnim = new DoubleAnimation(0, fullWidth, new Duration(TimeSpan.FromSeconds(waitSeconds)));
                    progressBar.BeginAnimation(FrameworkElement.WidthProperty, barAnim);
                };

                // 倒计时 Label 更新
                int remaining = waitSeconds;
                countTimer.Tick += (s, e) =>
                {
                    remaining--;
                    if (remaining > 0)
                    {
                        countLabel.Text = $"倒计时： {remaining}s";
                    }
                    else
                    {
                        countTimer.Stop();
                        // 倒计时结束，自动确认
                        Respond(true);
                    }
                };
                countTimer.Start();
            }


            // 按钮事件
            btnConfirm.Click += (s, e) => Respond(true);
            btnCancel.Click += (s, e) => Respond(false);

            // 关闭并回调
            void Respond(bool confirmed)
            {
                if (hasResponded) return;
                hasResponded = true;
                countTimer.Stop();
                DismissCard(host, card, () => callback?.Invoke(confirmed));
            }
        }

        /// <summary>淡出并从容器移除卡片，完成后执行 onRemoved</summary>
        private static void DismissCard(Panel host, MagicCard1 card, Action onRemoved = null)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) =>
            {
                host.Children.Remove(card);
                onRemoved?.Invoke();
            };
            card.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        /// <summary>从当前活动窗口解析出一个 Panel 作为宿主</summary>
        private static Panel ResolveHostPanel()
        {
            var win = WindowHelper.GetActiveWindow() as Window;
            if (win == null) return null;
            if (win.Content is Panel p) return p;
            // 如果 Content 不是 Panel，尝试找第一个 Grid/Canvas/etc.
            if (win.Content is DependencyObject d)
            {
                return FindVisualChild<Panel>(d);
            }
            return null;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        #endregion
    }
}
