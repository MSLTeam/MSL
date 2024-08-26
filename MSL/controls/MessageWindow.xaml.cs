using MSL.langs;
using System.Windows;

namespace MSL.controls
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWindow : HandyControl.Controls.Window
    {
        public bool _dialogReturn = false;
        public bool _closeBtnReturn = true;
        public MessageWindow(Window window, string dialogText, string dialogTitle, bool primaryBtnVisible, string closeText, string primaryText)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            Title = dialogTitle;
            titleText.Text = dialogTitle;
            bodyText.Text = dialogText;
            if (!primaryBtnVisible)
            {
                primaryBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                if (closeText == "否")
                {
                    closeText = LanguageManager.Instance["No"];
                }
                if (primaryText == "是")
                {
                    primaryText = LanguageManager.Instance["Yes"];
                }
                closeBtn.Content = closeText;
                primaryBtn.Content = primaryText;
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
