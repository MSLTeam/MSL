using HandyControl.Controls;
using HandyControl.Data;

namespace MSL.controls
{
    public class DialogShow
    {
        public static bool ShowMsg(System.Windows.Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定", string primaryText = "确定")
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                MessageDialog messageDialog = new MessageDialog(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText);
                messageDialog.Owner = window;
                messageDialog.ShowDialog();
                window.Focus();
                dialog.Close();
                if (MessageDialog._dialogReturn)
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
        public static bool ShowInput(System.Windows.Window window, string dialogText, out string userInput, string textboxText = "",bool passwordMode=false)
        {
            userInput = string.Empty;
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                InputDialog inputDialog = new InputDialog(window, dialogText, textboxText,passwordMode);
                inputDialog.Owner = window;
                inputDialog.ShowDialog();
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

        public static bool ShowInstallForge(System.Windows.Window window,string forgePath)
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                InstallForgeDialog installforge = new InstallForgeDialog(forgePath);
                installforge.Owner = window;
                installforge.ShowDialog();
                window.Focus();
                dialog.Close();
                if (InstallForgeDialog.isStopDwn)
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

        public static bool ShowDownload(System.Windows.Window window, string downloadurl, string downloadPath, string filename, string downloadinfo, string sha256 = "")
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                DownloadWindow download = new DownloadWindow(downloadurl, downloadPath, filename, downloadinfo, sha256);
                download.Owner = window;
                download.ShowDialog();
                window.Focus();
                dialog.Close();
                if (DownloadWindow.isStopDwn)
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
}
