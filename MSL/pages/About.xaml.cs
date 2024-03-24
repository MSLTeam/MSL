using MSL.i18n;
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
            AppVersionLab.Content = LanguageManager.Instance["Pages_About_AboutMSL"] + string.Format("(MSLv{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }
    }
}
