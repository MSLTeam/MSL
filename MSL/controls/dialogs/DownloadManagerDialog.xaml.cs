using HandyControl.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MSL.controls.dialogs
{
    /// <summary>
    /// DownloadManagerDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadManagerDialog : UserControl
    {
        #region 单例模式
        private static readonly Lazy<DownloadManagerDialog> _instance = new Lazy<DownloadManagerDialog>(() => new DownloadManagerDialog());
        public static DownloadManagerDialog Instance => _instance.Value;
        #endregion

        private string DialogToken { get; set; }

        public DownloadManagerDialog()
        {
            InitializeComponent();
        }

        private void CloseDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            Dialog.Close(DialogToken);
        }

        public void LoadDialog(string token, bool canClose)
        {
            DialogToken = token;
            if (canClose)
                CloseDialogBtn.Visibility = Visibility.Visible;
            else
                CloseDialogBtn.Visibility = Visibility.Collapsed;
        }
    }
}
