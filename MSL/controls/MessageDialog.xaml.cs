using System.Windows;
using System.Windows.Media.Animation;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog : Window
    {
        public static bool _dialogReturn;
        public MessageDialog(Window window,string dialogText, string dialogTitle, bool primaryBtnVisible, string closeText,string primaryText)
        {
            InitializeComponent();
            this.MaxHeight=window.ActualHeight-50;
            this.MaxWidth = window.ActualWidth - 200;
            _dialogReturn = false;
            titleText.Text = dialogTitle;
            bodyText.Text = dialogText;
            closeBtn.Content = closeText;
            primaryBtn.Content=primaryText;
            if (!primaryBtnVisible)
            {
                primaryBtn.Visibility = Visibility.Hidden;
            }
        }

        private void primaryBtn_Click(object sender, RoutedEventArgs e)
        {
            var story = (Storyboard)this.Resources["HideWindow"];
            if (story != null)
            {
                story.Completed += delegate
                {
                    _dialogReturn = true;
                    Close();
                };
                story.Begin(this);
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            var story = (Storyboard)this.Resources["HideWindow"];
            if (story != null)
            {
                story.Completed += delegate 
                {
                    _dialogReturn = false;
                    Close();
                };
                story.Begin(this);
            }
        }
    }
}
