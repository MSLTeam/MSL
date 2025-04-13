using HandyControl.Controls;
using HandyControl.Tools;
using MSL.controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            MagicDialog.ShowMsgDialog(Functions.GetWindow(_window), text, title, uIElement,isDangerPrimaryBtn);
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
        public static async Task<bool> ShowMsgDialogAsync(string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "CANCEL", string primaryBtnContext = "CONFIRM", UIElement uIElement = null, UIElement _window = null,bool isDangerPrimaryBtn=false)
        {
            MagicDialog MagicDialog = new MagicDialog();
            bool _ret = await MagicDialog.ShowMsgDialog(Functions.GetWindow(_window), text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement,isDangerPrimaryBtn);
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
            MagicDialog.ShowMsgDialog(_window, text, title, uIElement,isDangerPrimaryBtn);
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
            bool _ret = await MagicDialog.ShowMsgDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement,isDangerPrimaryBtn);
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
        public static async Task<bool> ShowDownloader(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, int headerMode = 1)
        {
            MagicDialog MagicDialog = new MagicDialog();
            int _ret = await MagicDialog.ShowDownloadDialog(_window, downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, headerMode);
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
        public static async Task<int> ShowDownloaderWithIntReturn(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, int headerMode = 1)
        {
            MagicDialog MagicDialog = new MagicDialog();
            int _ret = await MagicDialog.ShowDownloadDialog(_window, downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, headerMode);
            return _ret;
        }


    }

    internal class MagicDialog
    {
        private Window window;
        private Dialog dialog;


        public void ShowTextDialog(Window _window, string text)
        {
            window = _window;
            window?.Focus();
            dialog = Dialog.Show(new TextDialog(text));
        }

        public void ShowTextDialog(string text, UIElement _window = null)
        {
            window = Functions.GetWindow(_window);
            window?.Focus();
            dialog = Dialog.Show(new TextDialog(text));
        }

        public void CloseTextDialog()
        {
            window?.Focus();
            dialog.Close();
            window = null;
            dialog = null;
        }


        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(Window _window, string text, string title, UIElement uIElement, bool isDangerPrimaryBtn)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, false, "", "", uIElement, isDangerPrimaryBtn);
            msgDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext, string primaryBtnContext, UIElement uIElement, bool isDangerPrimaryBtn)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement, isDangerPrimaryBtn);
            msgDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return msgDialog._dialogReturn;
        }

        public async Task<int> ShowDownloadDialog(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "", bool closeDirectly = false, int headerMode = 1)
        {
            window = _window;
            DownloadDialog dwnDialog = new DownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256, closeDirectly, headerMode);
            dwnDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(dwnDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return dwnDialog._dialogReturn;
        }

        public async Task<string> ShowInpputDialog(Window _window, string dialogText, string textboxText = "", bool passwordMode = false)
        {
            window = _window;
            InputDialog inputDialog = new InputDialog(dialogText, textboxText, passwordMode);
            inputDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(inputDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return inputDialog._dialogReturn;
        }

        public async Task<string[]> ShowInstallForgeDialog(Window _window, string installPath, string forgeFileName, string javaPath)
        {
            window = _window;
            InstallForgeDialog _dialog = new InstallForgeDialog(installPath, forgeFileName, javaPath);
            _dialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(_dialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            string[] strings = [_dialog.DialogReturn.ToString(), _dialog.McVersion];
            return strings;
        }

        private void CloseMsgDialog()
        {
            _tcs?.TrySetResult(true);
            window?.Focus();
            dialog?.Close();
            _tcs = null;
            window = null;
            dialog = null;
        }
    }

    public class MagicFlowMsg
    {
        private static Panel targetContainer;
        private static StackPanel growlPanel;
        private static StackPanel messageStackPanel;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="panel">容器</param>
        /// <param name="message">要显示的消息文本</param>
        /// <param name="type">消息类型，默认为0（primary），1为sucess，2为danger，3为LabelWarning，4为Info</param>
        /// <param name="seconds">显示时长，单位：秒（默认 3 秒）</param>
        public static void ShowMessage(string message, int type = 0, int seconds = 3, UIElement panel = null)
        {
            panel ??= WindowHelper.GetActiveWindow();
            if (panel is Page page)
            {
                panel = Window.GetWindow(page);
            }
            if (panel is Window window)
            {
                panel = window.Content as UIElement;
            }
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

            //targetContainer = panel;

            // 创建 Label 控件来显示消息
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

            // 根据消息类型设置样式，可以根据需要为不同类型设置不同的样式
            switch (type)
            {
                case 0:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelPrimary"];
                    break;
                case 1:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelSuccess"];
                    break;
                case 2:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelDanger"];
                    break;
                case 3:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelWarning"];
                    break;
                case 4:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelInfo"];
                    break;
                default:
                    msgLabel.Style = (Style)Application.Current.Resources["LabelPrimary"];
                    break;
            }
            if (growlPanel != null)
            {
                growlPanel.Children.Insert(0, msgLabel);
            }
            else
            {
                messageStackPanel?.Children.Insert(0, msgLabel);
            }
            //messageStackPanel.Children.Add(msgLabel);

            // 显示消息并启动动画
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

        /// <summary>
        /// 启动消息的显示动画
        /// </summary>
        private static void ShowMessageAnimation(Label msgLabel, int seconds)
        {
            // 设置为可见，并开始淡入动画
            msgLabel.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            msgLabel.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // 启动定时器，指定秒数后隐藏消息
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                HideMessageAnimation(msgLabel);
            };
            timer.Start();
        }

        /// <summary>
        /// 隐藏消息的动画
        /// </summary>
        private static void HideMessageAnimation(Label msgLabel)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) =>
            {
                msgLabel.Visibility = Visibility.Collapsed;
                if (growlPanel != null)
                {
                    growlPanel.Children.Remove(msgLabel);
                }
                else
                {
                    messageStackPanel.Children.Remove(msgLabel);
                    // 如果所有消息都已隐藏，移除 StackPanel
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
    }
}
