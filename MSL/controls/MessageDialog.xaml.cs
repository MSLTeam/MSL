using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSL.controls
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MessageDialog : Window
    {
        public static string _dialogTitle;
        public static string _dialogText;
        public static bool _dialogPrimaryBtn=false;
        public static string _dialogPrimaryText;
        public static string _dialogCloseText;
        public static bool _dialogReturn=false;
        public MessageDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            titleText.Text = _dialogTitle;
            bodyText.Text=_dialogText;
            primaryBtn.Content = _dialogPrimaryText;
            closeBtn.Content = _dialogCloseText;
            if (_dialogPrimaryBtn)
            {
                primaryBtn.Visibility = Visibility.Visible;
            }
            else
            {
                primaryBtn.Visibility = Visibility.Hidden;
            }
        }

        private void primaryBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = true;
            Close(); 
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            _dialogReturn = false;
            Close();
        }
    }
}
