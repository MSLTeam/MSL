using HandyControl.Controls;
using HandyControl.Data;
using System.Threading.Tasks;
using Window = System.Windows.Window;

namespace MSL.controls
{
    internal class Shows
    {
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

        public static int ShowMsgWithClose(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
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

        public static bool ShowInput(Window window, string dialogText, out string userInput, string textboxText = "", bool passwordMode = false)
        {
            userInput = string.Empty;
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                InputWindow inputWindow = new InputWindow(window, dialogText, textboxText, passwordMode)
                {
                    Owner = window
                };
                inputWindow.ShowDialog();
                window.Focus();
                dialog.Close();
                if (inputWindow._dialogReturn)
                {
                    userInput = inputWindow._textReturn;
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

        public static bool ShowInstallForge(Window window, string forgePath, string downPath, string java)
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(null);
                InstallForgeWindow installforge = new InstallForgeWindow(forgePath, downPath, java)
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

        public static async Task<bool> ShowDownloader(Window window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowDownloadDialog(window, downloadurl, downloadPath, filename, downloadinfo, sha256);
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

        public static void ShowMsgDialog(Window _window, string text, string title)
        {
            ShowDialogs showDialogs = new ShowDialogs();
            showDialogs.ShowMsgDialog(_window, text, title);
        }

        public static async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            ShowDialogs showDialogs = new ShowDialogs();
            bool _ret = await showDialogs.ShowMsgDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
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
            window.Focus();
            dialog = Dialog.Show(new TextDialog(text));
        }

        public void CloseTextDialog()
        {
            window.Focus();
            dialog.Close();
        }

        private TaskCompletionSource<bool> _tcs;
        public void ShowMsgDialog(Window _window, string text, string title)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(_window, text, title, false);
            msgDialog.CloseDialog += CloseMsgDialog;
            window.Focus();
            dialog = Dialog.Show(msgDialog);
            _tcs = new TaskCompletionSource<bool>();
        }

        public async Task<bool> ShowMsgDialog(Window _window, string text, string title, bool showPrimaryBtn, string closeBtnContext = "取消", string primaryBtnContext = "确定")
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(_window, text, title, showPrimaryBtn, closeBtnContext, primaryBtnContext);
            msgDialog.CloseDialog += CloseMsgDialog;
            window.Focus();
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
            window.Focus();
            dialog = Dialog.Show(dwnDialog);
            _tcs = new TaskCompletionSource<bool>();
            await _tcs.Task;
            return dwnDialog._dialogReturn;
        }

        private void CloseMsgDialog()
        {
            window.Focus();
            dialog.Close();
            _tcs.SetResult(true);
        }
    }
}
