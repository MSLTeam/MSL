using System.Windows;

namespace MSL.forms
{
    /// <summary>
    /// ConptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConptyWindow : Window
    {
        public string serverbase;
        public string java;
        public string launcharg;

        public ConptyWindow()
        {
            InitializeComponent();
        }

        public void StartServer()
        {
            ConptyConsole.StartupCommandLine = java + " " + launcharg;
            ConptyConsole.WorkingDirectory = serverbase;
            ConptyConsole.StartTerm();
            ControlServer.Content = "StopServer";
        }

        public void StartServer2()
        {
            ConptyConsole.StartupCommandLine = java + " " + launcharg;
            ConptyConsole.WorkingDirectory = serverbase;
            ConptyConsole.ResetTerm();
            ControlServer.Content = "StopServer";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConptyConsole.ConPTYTerm.CanOutLog = false;
        }
    }
}
