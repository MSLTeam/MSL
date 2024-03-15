using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog
    {
        public event DeleControl CloseDialog;
        public bool _dialogReturn;

        public MessageDialog(Window window, string dialogText, string dialogTitle, bool showPrimaryBtn, string closeBtnContext, string primaryBtnContext)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            bodyText.Text = dialogText;
            titleText.Text = dialogTitle;
            if (!showPrimaryBtn)
            {
                PrimaryBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                CloseBtn.Content = closeBtnContext;
                PrimaryBtn.Content = primaryBtnContext;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrimaryBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = true;
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
