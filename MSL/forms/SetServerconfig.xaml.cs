using MSL.controls;
using System;
using System.IO;
using System.Windows;

namespace MSL
{
    /// <summary>
    /// SetServerconfig.xaml 的交互逻辑
    /// </summary>
    public partial class SetServerconfig : HandyControl.Controls.Window
    {
        bool nosafeClose = false;
        public static string serverbase;
        public SetServerconfig()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                item001.Text = File.ReadAllText(serverbase + @"\server.properties");
                nosafeClose = false;
            }
            catch (Exception aaa)
            {
                MessageBox.Show("出现错误，请启动一次服务器后再试！\n" + aaa.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                nosafeClose = true;
                Close();
            }
            try
            {
                item002.Text = File.ReadAllText(serverbase + @"\bukkit.yml");
                nosafeClose = false;
            }
            catch
            {
                bukkit.IsEnabled = false;
            }
            try
            {
                item003.Text = File.ReadAllText(serverbase + @"\spigot.yml");
                nosafeClose = false;
            }
            catch
            {
                spigot.IsEnabled = false;
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (nosafeClose == false)
            {
                if (server.IsEnabled == true)
                {
                    File.WriteAllText(serverbase + @"\server.properties", item001.Text);
                }
                if (bukkit.IsEnabled == true)
                {
                    File.WriteAllText(serverbase + @"\bukkit.yml", item002.Text);
                }
                if (spigot.IsEnabled == true)
                {
                    File.WriteAllText(serverbase + @"\spigot.yml", item003.Text);
                }
                await Shows.ShowMsgDialogAsync("配置已成功保存，请重启服务器以使设置生效！", "提示");
            }
        }
    }
}
