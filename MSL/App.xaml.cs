using MSL.pages;
using System;
using System.Drawing;
using System.Windows;

namespace MSL
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) => { MessageBox.Show("发生异常，请检查您的电脑是否安装.Net Framework 4.7.2或以上版本，若安装后依旧出错，请将此窗口截图发送给作者进行反馈\n" + e.Exception,"错误",MessageBoxButton.OK,MessageBoxImage.Error); };
        }
    }
}
