using HandyControl.Controls;
using System.Threading.Tasks;
using Window = System.Windows.Window;

namespace MSL.controls
{
    internal class Shows
    {
        /// <summary>
        /// 显示MSG对话框（异步执行，不可等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        public static void ShowMsgDialog(Window _window, string text, string title)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            showDialogs.ShowMsgDialog(_window, text, title);
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
        public static async Task<bool> ShowMsgDialogAsync(Window _window, string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowMsgDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
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
        public static int ShowMsg(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
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
            ShowDialogs showDialogs = new ShowDialogs();
            string _ret = await showDialogs.ShowInpputDialog(_window, dialogText, textboxText, passwordMode);
            return _ret;
        }

        public static async Task<string[]> ShowInstallForge(Window _window, string forgePath, string downPath, string java)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            return await showDialogs.ShowInstallForgeDialog(_window, forgePath, downPath, java);
        }

        public static async Task<bool> ShowDownloader(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowDownloadDialog(_window, downloadurl, downloadPath, filename, downloadinfo, sha256);
            return _ret;
        }
    }

    internal class ShowDialogs
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
        }


        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(Window _window, string text, string title)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, false, "", "");
            msgDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
            msgDialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return msgDialog._dialogReturn;
        }

        public async Task<bool> ShowDownloadDialog(Window _window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            window = _window;
            DownloadDialog dwnDialog = new DownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256);
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

        public async Task<string[]> ShowInstallForgeDialog(Window _window, string forgePath, string downPath, string java)
        {
            window = _window;
            InstallForgeDialog _dialog = new InstallForgeDialog(forgePath, downPath, java);
            _dialog.CloseDialog += CloseMsgDialog;
            window?.Focus();
            dialog = Dialog.Show(_dialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            string[] strings = new string[2];
            strings[0] = _dialog._dialogReturn.ToString();
            strings[1] = _dialog.mcVersion;
            return strings;
        }

        private void CloseMsgDialog()
        {
            try
            {
                _tcs.SetResult(true);
            }
            finally
            {
                window?.Focus();
                dialog.Close();
            }
        }
    }
}
