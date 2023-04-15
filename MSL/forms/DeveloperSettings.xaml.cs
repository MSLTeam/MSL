using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSL.forms
{
    /// <summary>
    /// DeveloperSettings.xaml 的交互逻辑
    /// </summary>
    public partial class DeveloperSettings : Window
    {
        public static bool developerMode;
        public DeveloperSettings()
        {
            InitializeComponent();
        }

        private void changeServerLink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.serverLink=serverLinkBox.Text;
        }

        private void openDeveloperMode_Click(object sender, RoutedEventArgs e)
        {
            if (openDeveloperMode.IsChecked == true)
            {
                developerMode = true;
            }
            else
            {
                developerMode = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (developerMode == true)
            {
                openDeveloperMode.IsChecked = true;
            }
            else
            {
                openDeveloperMode.IsChecked = false;
            }
        }
    }
}
