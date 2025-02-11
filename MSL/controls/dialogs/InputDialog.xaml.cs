using HandyControl.Controls;
using MSL.utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private bool AcceptEmpty;
        public InputDialog(string dialogText, string textboxText, bool passwordMode = false, bool acceptEmpty = false)
        {
            InitializeComponent();

            Margin = new Thickness(50);
            bodyText.Text = dialogText;
            TextBox.Text = textboxText;
            if (passwordMode)
            {
                PassBox.Visibility = Visibility.Visible;
                TextBox.Visibility = Visibility.Hidden;
            }
            else
            {
                PassBox.Visibility = Visibility.Hidden;
                TextBox.Visibility = Visibility.Visible;
            }
            AcceptEmpty = acceptEmpty;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (PassBox.Visibility == Visibility.Visible)
            {
                PassBox.Focus();
            }
            else
            {
                TextBox.Focus();
            }
        }

        private void PrimaryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PassBox.Visibility == Visibility.Visible)
            {
                _dialogReturn = PassBox.Password;
            }
            else
            {
                _dialogReturn = TextBox.Text;
            }
            if ((!AcceptEmpty) && string.IsNullOrEmpty(_dialogReturn))
            {
                MagicFlowMsg.ShowMessage("请输入内容！", 2, panel: MainGrid);
                return;
            }
            Close();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PrimaryBtn_Click(null, null);
                e.Handled = true;
            }
        }

        private void PassBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PrimaryBtn_Click(null, null);
                e.Handled = true;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = null;
            Close();
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
