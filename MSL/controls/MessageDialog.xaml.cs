using System.Windows;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog
    {
        public event DeleControl CloseDialog;
        public MessageDialog(Window window,string dialogText,string dialogTitle)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            bodyText.Text = dialogText;
            titleText.Text=dialogTitle;
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }
    }
}
