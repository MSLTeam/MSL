using System.Windows;

namespace MSL.controls
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InputWindow : HandyControl.Controls.Window
    {
        public static bool _dialogReturn;
        public static string _textReturn;
        public InputWindow(Window window, string dialogText, string textboxText, bool passwordMode = false)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            _dialogReturn = false;
            bodyText.Text = dialogText;
            textBox.Text = textboxText;
            if (passwordMode)
            {
                passBox.Visibility = Visibility.Visible;
                textBox.Visibility = Visibility.Hidden;
                passBox.Focus();
            }
            else
            {
                passBox.Visibility = Visibility.Hidden;
                textBox.Visibility = Visibility.Visible;
                textBox.Focus();
            }
        }

        private void primaryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (passBox.Visibility == Visibility.Visible)
            {
                _textReturn = passBox.Password;
            }
            else
            {
                _textReturn = textBox.Text;
            }
            _dialogReturn = true;
            Close();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = false;
            Close();
        }

        private void textBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _dialogReturn = true;
                _textReturn = textBox.Text;
                Close();
            }
        }

        private void passBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _dialogReturn = true;
                _textReturn = passBox.Password;
                Close();
            }
        }
    }
}
