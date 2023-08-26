using System.Windows;
using System.Windows.Media.Animation;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : HandyControl.Controls.Window
    {
        public static bool _dialogReturn;
        public static string _textReturn;
        public InputDialog(Window window, string dialogText, string textboxText)
        {
            InitializeComponent();
            //this.MaxHeight = window.ActualHeight - 50;
            //this.MaxWidth = window.ActualWidth - 200;
            _dialogReturn = false;
            bodyText.Text = dialogText;
            textBox.Text = textboxText;
        }

        private void primaryBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = true;
            _textReturn = textBox.Text;
            Close();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = false;
            Close();
        }
    }
}
