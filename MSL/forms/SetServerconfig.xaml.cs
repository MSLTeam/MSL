using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSL
{
    /// <summary>
    /// SetServerconfig.xaml 的交互逻辑
    /// </summary>
    public partial class SetServerconfig : Window
    {
        bool nosafeClose = false;
        public SetServerconfig()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                item001.Text = File.ReadAllText(MainWindow.serverbase + @"\server.properties");
                nosafeClose = false;
            }
            catch(Exception aaa)
            {
                MessageBox.Show("出现错误，请启动一次服务器后再试！\n" +aaa.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                nosafeClose = true;
                Close();
            }
            try
            {
                item002.Text = File.ReadAllText(MainWindow.serverbase + @"\bukkit.yml");
                nosafeClose = false;
            }
            catch
            {
                bukkit.IsEnabled = false;
            }
            try
            {
                item003.Text = File.ReadAllText(MainWindow.serverbase + @"\spigot.yml");
                nosafeClose = false;
            }
            catch
            {
                spigot.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (nosafeClose == false)
            {
                if (server.IsEnabled == true)
                {
                    File.WriteAllText(MainWindow.serverbase + @"\server.properties", item001.Text);
                }
                if (bukkit.IsEnabled == true)
                {
                    File.WriteAllText(MainWindow.serverbase + @"\bukkit.yml", item002.Text);
                }
                if (spigot.IsEnabled == true)
                {
                    File.WriteAllText(MainWindow.serverbase + @"\spigot.yml", item003.Text);
                }
                MessageBox.Show("配置已成功保存，请重启服务器以使设置生效！","提示",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }
    }
}
