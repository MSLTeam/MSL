using HandyControl.Controls;
using System;
using System.Runtime.CompilerServices;

namespace MSL.controls
{
    public class DialogShow
    {
        public static bool ShowMsg(System.Windows.Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定",string primaryText="确定")
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
        public static bool ShowInput(System.Windows.Window window, string dialogText, out string userInput, string textboxText = "")
        {
            userInput = string.Empty;
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                InputDialog inputDialog = new InputDialog(window, dialogText, textboxText);
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

        public static bool ShowDownload(System.Windows.Window window, string downloadurl, string downloadPath, string filename,string downloadinfo)
        {
            try
            {
                window.Focus();
                var dialog = Dialog.Show(string.Empty);
                DownloadWindow download = new DownloadWindow(downloadurl, downloadPath, filename, downloadinfo);
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
    }
}
