using HandyControl.Controls;
using Microsoft.Win32;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.UI.Popups;
using MessageBox = System.Windows.MessageBox;
using MessageDialog = MSL.controls.MessageDialog;

namespace MSL.pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        public static event DeleControl C_NotifyIcon;
        public static event DeleControl SetNormalColor;
        public static event DeleControl SetBlackWhiteColor;
        public static event DeleControl SetRedColor;

        public SettingsPage()
        {
            InitializeComponent();
        }
        private void mulitDownthread_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow.downloadthread = int.Parse(downthreadCount.Text);
        }
        private void setdefault_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogShow.Show("恢复默认设置会清除MSL文件夹内的所有文件，请您谨慎选择！", "警告", true, "确定", "取消");
            MessageDialog messageDialog = new MessageDialog();
            MainWindow mainwindow = (MainWindow)System.Windows.Window.GetWindow(this);
            messageDialog.Owner = mainwindow;
            messageDialog.ShowDialog();
            if (MessageDialog._dialogReturn)
            {
                MessageDialog._dialogReturn = false;
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"MSL", true);
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"MSL");
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", MainWindow.mslConfig);
                }
                catch
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", MainWindow.mslConfig);
                }
                Process.Start(Application.ResourceAssembly.Location);
                Process.GetCurrentProcess().Kill();
            }
        }
        private void notifyIconbtn_Click(object sender, RoutedEventArgs e)
        {
            if (notifyIconbtn.Content.ToString() == "关闭托盘图标")
            {
                notifyIconbtn.Content = "打开托盘图标";
                MainWindow.notifyIcon = false;
                try
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = "False";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Growl.Success("打开成功！");
                    return;
                }
                catch
                {
                    Growl.Error("打开失败！");
                    return;
                }
            }
            else
            {
                notifyIconbtn.Content = "关闭托盘图标";
                MainWindow.notifyIcon = true;
                C_NotifyIcon();
                try
                {
                    string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                    JObject jobject = JObject.Parse(jsonString);
                    jobject["notifyIcon"] = "True";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                    Growl.Success("关闭成功！");
                    return;
                }
                catch
                {
                    Growl.Error("关闭失败！");
                    return;
                }
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.notifyIcon == true)
            {
                notifyIconbtn.Content = "关闭托盘图标";
            }
            else
            {
                notifyIconbtn.Content = "打开托盘图标";
            }
            StreamReader reader = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json");
            JsonTextReader jsonTextReader = new JsonTextReader(reader);
            JObject jsonObject = (JObject)JToken.ReadFrom(jsonTextReader);
            reader.Close();
            if (jsonObject["autoOpenServer"].ToString() != "False")
            {
                openserversOnStart.IsChecked = true;
                openserversOnStartList.Text = jsonObject["autoOpenServer"].ToString();
            }
            if (jsonObject["autoOpenFrpc"].ToString() == "True")
            {
                openfrpOnStart.IsChecked = true;
            }
            if (MainWindow.ControlsColor == 0)
            {
                NormalSkinBtn.IsChecked = true;
            }
            if (MainWindow.ControlsColor == 1)
            {
                BlackWhiteSkinBtn.IsChecked = true;
            }
            if (MainWindow.ControlsColor == 2)
            {
                RedSkinBtn.IsChecked = true;
            }
        }

        private void useidea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/help.txt");
                string notice = Encoding.UTF8.GetString(pageData);
                Process.Start(notice);
            }
            catch 
            {
                MessageBox.Show("获取教程失败！", "err", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void openserversOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openserversOnStart.IsChecked==true)
            {
                if (openserversOnStartList.Text == "")
                {
                    Growl.Error("请先在下面列表中输入服务器ID！");
                    openserversOnStart.IsChecked = false;
                    return;
                }
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = openserversOnStartList.Text;
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenServer"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
            }
        }

        private void openfrpOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (openfrpOnStart.IsChecked == true)
            {
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = "True";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("开启成功！");
            }
            else
            {
                string jsonString = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", System.Text.Encoding.UTF8);
                JObject jobject = JObject.Parse(jsonString);
                jobject["autoOpenFrpc"] = "False";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\config.json", convertString, System.Text.Encoding.UTF8);
                Growl.Success("关闭成功！");
            }
        }
        private void NormalSkinBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ControlsColor != 0)
            {
                SettingsPage.SetNormalColor();
                JObject jobject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "0";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("皮肤切换成功！");
            }
        }

        private void BlackWhiteSkinBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ControlsColor != 1)
            {
                SettingsPage.SetBlackWhiteColor();
                JObject jobject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "1";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("皮肤切换成功！");
            }
        }

        private void RedSkinBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ControlsColor != 2)
            {
                SettingsPage.SetRedColor();
                JObject jobject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", Encoding.UTF8));
                jobject["skin"] = "2";
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "MSL\\config.json", convertString, Encoding.UTF8);
                Growl.Success("皮肤切换成功！");
            }
        }
    }
}