using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void HyperLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "HyperLink1":
                    Process.Start("https://github.com/Fody/Fody");
                    break;
                case "HyperLink2":
                    Process.Start("https://github.com/Fody/Costura");
                    break;
                case "HyperLink3":
                    Process.Start("https://github.com/CurseForgeCommunity/.NET-APIClient");
                    break;
                case "HyperLink4":
                    Process.Start("https://github.com/bezzad/Downloader");
                    break;
                case "HyperLink5":
                    Process.Start("https://github.com/ghost1372/HandyControls");
                    break;
                case "HyperLink6":
                    Process.Start("https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts");
                    break;
                case "HyperLink7":
                    Process.Start("https://www.newtonsoft.com/json");
                    break;
                case "HyperLink8":
                    Process.Start("https://github.com/icsharpcode/SharpZipLib");
                    break;
                case "HyperLink9":
                    Process.Start("https://github.com/dotnet/runtime");
                    break;
            }
        }
    }
}
