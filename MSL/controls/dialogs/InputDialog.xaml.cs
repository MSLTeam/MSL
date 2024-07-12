using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MSL.controls
{
    /// <summary>
    /// MessageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog
    {
        public event DeleControl CloseDialog;
        public string _dialogReturn = null;
        public InputDialog(string dialogText, string textboxText, bool passwordMode = false)
        {
            InitializeComponent();
            Margin = new Thickness(50);
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
                _dialogReturn = passBox.Password;
            }
            else
            {
                _dialogReturn = textBox.Text;
            }
            //_dialogReturn = true;
            //Close();
            CloseDialog();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            //_dialogReturn = false;
            Close();
        }

        private void textBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                //_dialogReturn = true;
                _dialogReturn = textBox.Text;
                Close();
            }
        }

        private void passBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                //_dialogReturn = true;
                _dialogReturn = passBox.Password;
                Close();
            }
        }

        private void Close()
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation scaleDownX = new DoubleAnimation(1, 1.1, TimeSpan.FromSeconds(0.15));
            DoubleAnimation scaleDownY = new DoubleAnimation(1, 1.1, TimeSpan.FromSeconds(0.15));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));

            storyboard.Children.Add(scaleDownX);
            storyboard.Children.Add(scaleDownY);
            storyboard.Children.Add(fadeOut);

            if (Template.FindName("contentPresenter", this) is ContentPresenter contentPresenter)
            {
                Storyboard.SetTarget(scaleDownX, contentPresenter);
                Storyboard.SetTarget(scaleDownY, contentPresenter);
                Storyboard.SetTarget(fadeOut, contentPresenter);

                Storyboard.SetTargetProperty(scaleDownX, new PropertyPath("RenderTransform.ScaleX"));
                Storyboard.SetTargetProperty(scaleDownY, new PropertyPath("RenderTransform.ScaleY"));
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

                storyboard.Completed += (s, a) =>
                {
                    Visibility = Visibility.Collapsed;
                    CloseDialog();
                };

                storyboard.Begin();
            }
        }
    }
}
