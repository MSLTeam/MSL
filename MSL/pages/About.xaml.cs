using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSL.pages
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Page
    {
        public About()
        {
            InitializeComponent();
        }
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://waheal.github.io/Minecraft-Server-Launcher/");
        }
        private void openSource_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/Waheal/Minecraft-Server-Launcher/");
        }

        private void support_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://afdian.net/@makabaka123?tab=sponsor");
        }
    }
}
