using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            AppVersionLab.Content += string.Format("(MSLv{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void support_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://afdian.net/@makabaka123?tab=sponsor");
        }
    }
}
