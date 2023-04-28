using HandyControl.Controls;
using System;
using System.Runtime.CompilerServices;

namespace MSL.controls
{
    public class DialogShow
    {
        public static void ShowMsg(System.Windows.Window window, string dialogText, string dialogTitle, bool primaryBtnVisible = false, string closeText = "确定",string primaryText="确定")
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
            }
            catch
            {
                MessageDialog messageDialog = new MessageDialog(window, dialogText, dialogTitle, primaryBtnVisible, closeText, primaryText);
                messageDialog.Owner = window;
                messageDialog.ShowDialog();
            }
        }
        public static void ShowDownload(System.Windows.Window window, string downloadurl, string downloadPath, string filename,string downloadinfo)
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
            }
            catch
            {
                DownloadWindow download = new DownloadWindow(downloadurl, downloadPath, filename, downloadinfo);
                download.Owner = window;
                download.ShowDialog();
            }
            
        }
    }
}
