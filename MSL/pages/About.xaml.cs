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
            AppVersionLab.Content += string.Format("(MSLv{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}
