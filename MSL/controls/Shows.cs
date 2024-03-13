using HandyControl.Controls;
using HandyControl.Data;

namespace MSL.controls
{
    internal class Shows
    {
        public static bool ShowMsg(System.Windows.Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
        {
            try
            {
                var dialog = Dialog.Show(null);
                MessageWindow messageWindow = new MessageWindow(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText, false)
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

        public static int ShowMsgWithClose(System.Windows.Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
        {
            try
            {
                var dialog = Dialog.Show(null);
                MessageWindow messageWindow = new MessageWindow(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText, true)
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

        public static bool ShowInput(System.Windows.Window window, string dialogText, out string userInput, string textboxText = "", bool passwordMode = false)
        {
            userInput = string.Empty;
            try
            {
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

        public static bool ShowInstallForge(System.Windows.Window window, string forgePath, string downPath, string java)
        {
            try
            {
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

        public static bool ShowDownloader(System.Windows.Window window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            try
            {
                var dialog = Dialog.Show(null);
                DownloadWindow download = new DownloadWindow(downloadurl, downloadPath, filename, downloadinfo, sha256)
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
    }

    internal class ShowDialog
    {
        private Window window;
        private Dialog dialog;
        public void ShowTextDialog(Window _window, string text)
        {
            window = _window;
            dialog = Dialog.Show(new TextDialog(text));
        }
        public void CloseTextDialog()
        {
            window.Focus();
            dialog.Close();
        }

        public void ShowMsgDialog(Window _window, string text, string title)
        {
            window = _window;
            MessageDialog msgDialog = new MessageDialog(_window, text, title);
            msgDialog.CloseDialog += CloseMsgDialog;
            dialog = Dialog.Show(msgDialog);

        }
        public void CloseMsgDialog()
        {
            window.Focus();
            dialog.Close();
        }
    }
}
