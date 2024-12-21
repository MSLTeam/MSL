using HandyControl.Controls;
using MSL.controls;
using System.Threading.Tasks;
using System.Windows;
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
        public static void ShowMsgDialog(Window _window, string text, string title, UIElement uIElement = null)
        {
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowMsgDialog(_window, text, title, uIElement);
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
        public static async Task<bool> ShowMsgDialogAsync(Window _window, string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "否", string primaryBtnContext = "是", UIElement uIElement = null)
        {
            MagicDialog MagicDialog = new MagicDialog();
            bool _ret = await MagicDialog.ShowMsgDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement);
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
        public static int ShowMsg(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "否", string primaryText = "是")
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

        public void CloseTextDialog()
        {
            window?.Focus();
            dialog.Close();
            window = null;
            dialog = null;
        }


        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(Window _window, string text, string title, UIElement uIElement)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, false, "", "", uIElement);
            msgDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext, string primaryBtnContext, UIElement uIElement)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext, uIElement);
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
}
