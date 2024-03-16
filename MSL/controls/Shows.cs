using HandyControl.Controls;
using HandyControl.Data;
using System.Threading.Tasks;
using Window = System.Windows.Window;

namespace MSL.controls
{
    internal class Shows
    {
        /*
        public static bool ShowMsg(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                MessageWindow messageWindow = new MessageWindow(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText)
                {
                    Owner = window
                };
                messageWindow.ShowDialog();
                window.Focus();
                dialog.Close();
                if (messageWindow._dialogReturn)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch
            {
                return false;
            }
        }
        */

        public static int ShowMsg(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                MessageWindow messageWindow = new MessageWindow(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText)
                {
                    Owner = window
                };
                messageWindow.ShowDialog();
                window.Focus();
                dialog.Close();
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

        public static async Task<string> ShowInput(string dialogText, string textboxText = "", bool passwordMode = false)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            string _ret = await showDialogs.ShowInpputDialog(dialogText, textboxText, passwordMode);
            return _ret;
        }

        /*
        public static bool ShowInput(Window window, string dialogText, out string userInput, string textboxText = "", bool passwordMode = false)
        {
            userInput = string.Empty;
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                InputDialog InputDialog = new InputDialog(window, dialogText, textboxText, passwordMode)
                {
                    Owner = window
                };
                InputDialog.ShowDialog();
                window.Focus();
                dialog.Close();
                if (InputDialog._dialogReturn)
                {
                    userInput = InputDialog._textReturn;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {

                return false;
            }
        }
        */

        public static async Task<bool> ShowInstallForge(string forgePath, string downPath, string java)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowInstallForgeDialog(forgePath, downPath, java);
            return _ret;
        }

        /*
        public static bool ShowInstallForge(Window window, string forgePath, string downPath, string java)
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                InstallForgeDialog installforge = new InstallForgeDialog(forgePath, downPath, java)
                {
                    Owner = window
                };
                installforge.ShowDialog();
                window.Focus();
                dialog.Close();
                if (installforge.suc)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        */

        public static async Task<bool> ShowDownloader(string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowDownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256);
            return _ret;
        }

        /*
        public static bool ShowDownloader(Window window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                DownloadDialog download = new DownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256)
                {
                    Owner = window
                };
                download.ShowDialog();
                window.Focus();
                dialog.Close();
                if (download.isStopDwn)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        */

        public static void GrowlSuccess(string msg, bool showtime = false)
        {
            Growl.Success(new GrowlInfo
            {
                Message = msg,
                ShowDateTime = showtime
            });
        }
        public static void GrowlInfo(string msg, bool showtime = false)
        {
            Growl.Info(new GrowlInfo
            {
                Message = msg,
                ShowDateTime = showtime
            });
        }
        public static void GrowlErr(string msg, bool showtime = false)
        {
            Growl.Error(new GrowlInfo
            {
                Message = msg,
                ShowDateTime = showtime
            });
        }

        /// <summary>
        /// 显示MSG对话框（异步执行，不可等待）
        /// </summary>
        /// <param name="_window">对话框父窗体</param>
        /// <param name="text">对话框内容</param>
        /// <param name="title">对话框标题</param>
        public static void ShowMsgDialog(string text, string title)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            showDialogs.ShowMsgDialog(text, title);
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
        /// <returns></returns>
        public static async Task<bool> ShowMsgDialogAsync(string text, string title, bool showPrimaryBtn = false, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowMsgDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
            return _ret;
        }
    }

    internal class ShowDialogs
    {
        //private Window window;
        private Dialog dialog;


        public void ShowTextDialog(string text)
        {
            //window = _window;
            //window.Focus();
            dialog = Dialog.Show(new TextDialog(text));
        }

        public void CloseTextDialog()
        {
            //window.Focus();
            dialog.Close();
        }


        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(string text, string title)
        {
            //window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, false, "", "");
            msgDialog.CloseDialog += CloseMsgDialog;
            //window.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(string text, string title, bool showPrimaryBtn, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            //window = _window;
            MessageDialog msgDialog = new MessageDialog(text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
            msgDialog.CloseDialog += CloseMsgDialog;
            //window.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return msgDialog._dialogReturn;
        }

        public async Task<bool> ShowDownloadDialog(string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            //window = _window;
            DownloadDialog dwnDialog = new DownloadDialog(downloadurl, downloadPath, filename, downloadinfo, sha256);
            dwnDialog.CloseDialog += CloseMsgDialog;
            //window.Focus();
            dialog = Dialog.Show(dwnDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return dwnDialog._dialogReturn;
        }

        public async Task<string> ShowInpputDialog(string dialogText, string textboxText = "", bool passwordMode = false)
        {
            //window = _window;
            InputDialog inputDialog = new InputDialog(dialogText, textboxText, passwordMode);
            inputDialog.CloseDialog += CloseMsgDialog;
            //window.Focus();
            dialog = Dialog.Show(inputDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return inputDialog._dialogReturn;
        }

        public async Task<bool> ShowInstallForgeDialog(string forgePath, string downPath, string java)
        {
            //window = _window;
            InstallForgeDialog _dialog = new InstallForgeDialog(forgePath, downPath, java);
            _dialog.CloseDialog += CloseMsgDialog;
            //window.Focus();
            dialog = Dialog.Show(_dialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return _dialog._dialogReturn;
        }

        private void CloseMsgDialog()
        {
            //window.Focus();
            dialog.Close();
            _tcs.SetResult(true);
        }
    }
}
