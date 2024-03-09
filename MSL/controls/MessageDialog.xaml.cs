using System.Windows;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog
    {
        public event DeleControl CloseDialog;
        //private readonly Window owner;
        public MessageDialog(Window window, string dialogText, string dialogTitle)
        {
            InitializeComponent();
            this.MaxHeight = window.ActualHeight;
            this.MaxWidth = window.ActualWidth - 200;
            //owner = window;
            bodyText.Text = dialogText;
            titleText.Text = dialogTitle;
            //Task.Run(ChangeSize);
        }

        /*
        private void ChangeSize()
        {
            while(true)
            {
                Dispatcher.Invoke(() =>
                {
                    this.Height = owner.ActualHeight - 40;
                    this.Width = owner.ActualWidth;
                });
                Thread.Sleep(1000);
            }
        }
        */

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }
    }
}
