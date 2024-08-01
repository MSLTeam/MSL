using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;

namespace MSL.pages
{
    /// <summary>
    /// FrpcList.xaml 的交互逻辑
    /// </summary>
    public partial class FrpcList : Page
    {
        public static event DeleControl OpenFrpcPage;
        public static int FrpcID;
        public static Dictionary<int, Page> FrpcPageList = new Dictionary<int, Page>();
        public static List<int> RunningFrpc = new List<int>();
        public FrpcList()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Path.Combine("MSL", "frp", "config.json")))
            {
                return;
            }
            GetFrpcConfig();
        }

        private void GetFrpcConfig()
        {
            FrpcListBox.Items.Clear();
            JObject keyValuePairs = JObject.Parse(File.ReadAllText(Path.Combine("MSL", "frp", "config.json")));
            foreach (var keyValue in keyValuePairs)
            {
                string key = keyValue.Key;
                FrpcListBox.Items.Add(key);
            }
        }

        private void FrpcListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FrpcID = int.Parse(FrpcListBox.SelectedItem.ToString());
            OpenFrpcPage();
        }

        private void FrpcListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FrpcID = int.Parse(FrpcListBox.SelectedItem.ToString());
                OpenFrpcPage();
            }
        }

        private void AddFrpc_Click(object sender, RoutedEventArgs e)
        {
            SetFrpc fw = new SetFrpc();
            fw.Owner = Window.GetWindow(this);
            fw.ShowDialog();
            GetFrpcConfig();
        }

        private void DelFrpc_Click(object sender, RoutedEventArgs e)
        {
            if (FrpcListBox.SelectedIndex == -1)
            {
                return;
            }
            if (RunningFrpc.Contains(int.Parse(FrpcListBox.SelectedItem.ToString())))
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "该映射正在运行中，请先关闭！", "提示");
                return;
            }
            JObject keyValuePairs = JObject.Parse(File.ReadAllText(Path.Combine("MSL", "frp", "config.json")));
            keyValuePairs.Remove(FrpcListBox.SelectedItem.ToString());
            File.WriteAllText(Path.Combine("MSL", "frp", "config.json"), Convert.ToString(keyValuePairs));
            Directory.Delete(Path.Combine("MSL", "frp", FrpcListBox.SelectedItem.ToString()), true);
            GetFrpcConfig();
        }
    }
}
