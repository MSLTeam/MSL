using System.Windows;

namespace MSL.controls
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWindow : HandyControl.Controls.Window
    {
        public bool _dialogReturn;
        public bool _closeBtnReturn = true;
        public MessageWindow(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible, string closeText, string primaryText, bool showCloseBtn)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            _dialogReturn = false;
            Title = dialogTitle;
            titleText.Text = dialogTitle;
            bodyText.Text = dialogText;
            closeBtn.Content = closeText;
            primaryBtn.Content = primaryText;
            if (!primaryBtnVisible)
            {
                primaryBtn.Visibility = Visibility.Hidden;
            }
            if (showCloseBtn)
            {
                ShowCloseButton = true;
            }
        }

        private void primaryBtn_Click(object sender, RoutedEventArgs e)
        {
            _closeBtnReturn = false;
            _dialogReturn = true;
            Close();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            _closeBtnReturn = false;
            _dialogReturn = false;
            Close();
        }
    }
}
