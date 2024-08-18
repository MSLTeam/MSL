using HandyControl.Controls;
using MSL.utils;
using System;
using System.Windows;
using Window = System.Windows.Window;

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
            ControlServer.Content = "关服";
        }

        public void StartServer2()
        {
            ConptyConsole.StartupCommandLine = java + " " + launcharg;
            ConptyConsole.WorkingDirectory = serverbase;
            ConptyConsole.ResetTerm();
            ControlServer.Content = "关服";
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            Shows.ShowMsg(this, "终端操作：\n复制：使用鼠标选中需要复制的内容，接着点击右键，即可进行复制操作。" +
                "\n粘贴：在终端没有选择任何内容的情况下，直接点击鼠标右键，即可进行粘贴操作。" +
                "\n取消选择：直接点击鼠标右键。" +
                "\n\n终端特殊功能：\n在输入指令时，按一下Tab键可进行一键补全（或指令提示）操作。" +
                "\n使用上下方向键可以回溯历史指令，左右方向键可以移动当前光标。", "操作提示");
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            PopUp.IsOpen = true;
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            PopUp.IsOpen = false;
            Growl.SetGrowlParent(GrowlPanel, false);
        }
    }
}
