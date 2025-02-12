using MSL.langs;
using System.Windows.Controls;

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

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            AbortSoftwareCard.Title = string.Format(LanguageManager.Instance["Page_About_AboutMSL"], MainWindow.MSLVersion.ToString());
        }
    }
}
