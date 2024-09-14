using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        internal class FrpcInfo
        {
            public string ID { get; set; }
            public string Name { get; set; }
        }

        public FrpcList()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetFrpcConfig();
        }

        private void GetFrpcConfig()
        {
            //绑定对象
            ObservableCollection<FrpcInfo> frplist = new ObservableCollection<FrpcInfo>();
            Dispatcher.Invoke(() =>
            {
                FrpcListBox.ItemsSource = frplist;
            });

            if (!File.Exists(Path.Combine("MSL", "frp", "config.json")))
            {
                return;
            }
            JObject keyValuePairs = JObject.Parse(File.ReadAllText(Path.Combine("MSL", "frp", "config.json")));
            foreach (var keyValue in keyValuePairs)
            {
                string key = keyValue.Key;
                if (key != "MSLFrpAccount" && key != "MSLFrpPasswd")
                {
                    if (keyValuePairs[key]["name"] != null)
                    {
                        frplist.Add(new FrpcInfo { ID = key, Name = $"[{key}]{keyValuePairs[key]["name"]}" });
                    }
                    else
                    {
                        frplist.Add(new FrpcInfo { ID = key, Name = $"[{key}]未命名的隧道" });
                    }
                }
            }
        }

        private void FrpcListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is FrpcInfo selectedTunnel)
            {
                FrpcID = int.Parse(selectedTunnel.ID);
                OpenFrpcPage();
            }
        }

        private void FrpcListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var listBox = sender as System.Windows.Controls.ListBox;
                if (listBox.SelectedItem is FrpcInfo selectedTunnel)
                {
                    FrpcID = int.Parse(selectedTunnel.ID);
                    OpenFrpcPage();
                }
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
            var listBox = FrpcListBox as System.Windows.Controls.ListBox;
            if (listBox.SelectedItem is FrpcInfo selectedTunnel)
            {
                if (RunningFrpc.Contains(int.Parse(selectedTunnel.ID)))
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "该映射正在运行中，请先关闭！", "提示");
                    return;
                }
                JObject keyValuePairs = JObject.Parse(File.ReadAllText(Path.Combine("MSL", "frp", "config.json")));
                keyValuePairs.Remove(selectedTunnel.ID);
                File.WriteAllText(Path.Combine("MSL", "frp", "config.json"), Convert.ToString(keyValuePairs));
                Directory.Delete(Path.Combine("MSL", "frp", selectedTunnel.ID), true);
                GetFrpcConfig();
            }

        }
    }
}
