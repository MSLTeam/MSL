using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.UI.Xaml.Controls.Maps;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// SetFrpc_OpenFrp.xaml 的交互逻辑
    /// </summary>
    public partial class SetFrpc_OpenFrp : Page
    {
        List<string> list1 = new List<string>();
        List<string> list2 = new List<string>();
        List<string> list3 = new List<string>();
        List<string> list4 = new List<string>();

        public string id = "";
        public string auth = "";
        public string token;
        public string proxy;
        public string Password;
        public JArray jArray;
        public Dictionary<string, string> nodelist;

        public SetFrpc_OpenFrp()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GetFrpsInfo());
        }
        void GetFrpsInfo(string path = "frplist", string url = "")
        {
            Dispatcher.Invoke(() =>
            {
                LoadingCircle loadingCircle = new LoadingCircle();
                loadingCircle.VerticalAlignment = VerticalAlignment.Top;
                loadingCircle.HorizontalAlignment = HorizontalAlignment.Left;
                loadingCircle.Margin = new Thickness(120, 150, 0, 0);
                MainGrid.Children.Add(loadingCircle);
                MainGrid.RegisterName("loadingBar", loadingCircle);
            });

            try
            {
                if (url == "")
                {
                    WebClient MyWebClient1 = new WebClient();
                    MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData1 = MyWebClient1.DownloadData(MainWindow.serverLink + "/msl/frpnotice");
                    Dispatcher.Invoke(() =>
                    {
                        gonggao.Content = Encoding.UTF8.GetString(pageData1);
                    });
                }
                else
                {
                    WebClient MyWebClient1 = new WebClient();
                    MyWebClient1.Credentials = CredentialCache.DefaultCredentials;
                    byte[] pageData1 = MyWebClient1.DownloadData(url + "/frpnotice");
                    Dispatcher.Invoke(() =>
                    {
                        gonggao.Content = Encoding.UTF8.GetString(pageData1);
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    gonggao.Content = "无公告";
                });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serversList.SelectedIndex == -1)
            {
                DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "需要选择一个节点", "信息");
                return;
            }
            
            if (frpProvider.SelectedIndex >= 1)
            {
                if (portBox.Text == "")
                {
                    DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "请确保内网端口不为空", "错误");
                    return;
                }
                APIControl control = new APIControl();
                if (frpProvider.SelectedIndex == 1)
                {
                    string type = "tcp";
                    if (frpcType.SelectedIndex == 0) type = "tcp";
                    else if (frpcType.SelectedIndex == 1) type = "udp";
                    bool zip;
                    if ((bool)enableCompression.IsChecked) zip = true;
                    else zip = false;
                    string selected_node = serversList.SelectedItem.ToString();
                    int selected_node_id;
                    if (selected_node != null) selected_node_id = Convert.ToInt16(nodelist[selected_node]);
                    else
                    {
                        MessageBox.Show("请确保选择了节点", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string port = portBox.Text;
                    (LoginMessage, string) proxy_name = control.CreateProxy(id, auth, type, port, zip, selected_node_id, jArray, (MainWindow)Window.GetWindow(this));
                    try
                    {
                        if (proxy_name.Item1 != null)
                        {
                            if (proxy_name.Item1.flag)
                            {
                                string proxy = control.GetUserNodeId(id, auth, proxy_name.Item2, (MainWindow)Window.GetWindow(this));
                                File.WriteAllText(@"MSL\frpc", $"-u {token} -p {proxy}");
                                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                                jobject["frpcServer"] = "0";
                                string convertString = Convert.ToString(jobject);
                                File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                            }
                            else
                            {
                                DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "创建隧道失败\n" + proxy_name.Item1.msg, "失败");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "创建隧道失败\n" + ex.Message, "失败");
                    }
                }
                //现有隧道
                else if (frpProvider.SelectedIndex == 2)
                {
                    object o = serversList.SelectedValue;
                    if (Equals(o, null))
                    {
                        MessageBox.Show("请确保选择了节点", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string id = nodelist[o.ToString()];
                    File.WriteAllText(@"MSL\frpc", $"-u {token} -p {id}");
                    JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
                    jobject["frpcServer"] = "0";
                    string convertString = Convert.ToString(jobject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                }
            }
            //Close();
        }

        private void frpcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frpcType.SelectedIndex == 0)
            {
                portBox.Text = "25565";
            }
            if (frpcType.SelectedIndex == 1)
            {
                portBox.Text = "19132";
            }
        }

        private void frpProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                serversList.SelectedIndex = -1;
                if (frpProvider.SelectedIndex == 0)
                {
                    usePaidProtocol.IsEnabled = true;
                    usePaidProtocol.SelectedIndex = 0;
                    serversList.Items.Clear();
                    list1.Clear();
                    list2.Clear();
                    list3.Clear();
                    list4.Clear();
                    Task.Run(() => GetFrpsInfo());
                }
                else if (frpProvider.SelectedIndex == 1) //OpenFrp(新建隧道)
                {
                    APIControl control = new APIControl();
                    if (id == "" || auth == "")
                    {
                        usePaidProtocol.SelectedIndex = 2;
                        usePaidProtocol.IsEnabled = false;
                        bool input_account = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱", out string account);
                        if (input_account)
                        {
                            bool input_paswd = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), account + "请输入密码", out string password);
                            if (input_paswd)
                            {
                                (UserwithSessionID, string) login = control.Login(account, password, (MainWindow)Window.GetWindow(this));
                                UserwithSessionID userwithSessionID = login.Item1;
                                token = login.Item2;
                                if (userwithSessionID != null)
                                {
                                    auth = userwithSessionID.auth;
                                    id = userwithSessionID.session;
                                }
                            }
                            else
                            {
                                frpProvider.SelectedIndex = 0;
                                return;
                            }
                        }
                        else
                        {
                            frpProvider.SelectedIndex = 0;
                            return;
                        }
                    }
                    serversList.Items.Clear();
                    list1.Clear();
                    list2.Clear();
                    list3.Clear();
                    list4.Clear();
                    try
                    {
                        (Dictionary<string, string>, JArray) process = control.GetNodeList(id, auth, (MainWindow)Window.GetWindow(this));
                        Dictionary<string, string> item1 = process.Item1;
                        nodelist = item1;
                        jArray = process.Item2;
                        foreach (var node in item1)
                        {
                            serversList.Items.Add(node.Key);
                        }
                    }
                    catch
                    {
                        frpProvider.SelectedIndex = 0;
                        return;
                    }
                }

                else if (frpProvider.SelectedIndex == 2)//OpenFrp(使用现有)
                {
                    APIControl control = new APIControl();
                    if (id == "" || auth == "")
                    {
                        usePaidProtocol.SelectedIndex = 2;
                        usePaidProtocol.IsEnabled = false;
                        bool input_account = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱", out string account);
                        if (input_account)
                        {
                            bool input_paswd = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), account + "请输入密码", out string password);
                            if (input_paswd)
                            {
                                //APIControl control = new APIControl();
                                (UserwithSessionID, string) login = control.Login(account, password, (MainWindow)Window.GetWindow(this));
                                UserwithSessionID userwithSessionID = login.Item1;
                                token = login.Item2;
                                if (userwithSessionID != null)
                                {
                                    auth = userwithSessionID.auth;
                                    id = userwithSessionID.session;
                                }
                            }
                            else
                            {
                                frpProvider.SelectedIndex = 0;
                                return;
                            }
                        }
                        else
                        {
                            frpProvider.SelectedIndex = 0;
                            return;
                        }
                    }
                    serversList.Items.Clear();
                    list1.Clear();
                    list2.Clear();
                    list3.Clear();
                    list4.Clear();
                    try
                    {
                        Dictionary<string, string> process = control.GetUserNodes(id, auth, (MainWindow)Window.GetWindow(this));
                        if (process.Count != 0)
                        {
                            nodelist = process;
                            foreach (KeyValuePair<string, string> node in process)
                            {
                                serversList.Items.Add(node.Key);
                            }
                        }
                        else
                        {
                            DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "你的账户看起来一条隧道也没有……", "错误");
                            frpProvider.SelectedIndex = 0;
                            return;
                        }
                    }
                    catch
                    {
                        frpProvider.SelectedIndex = 0;
                        return;
                    }
                }
                /*
                else if (frpProvider.SelectedIndex == 3)//OpenFrp(手动输入)
                {
                    bool input_account = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "账户Token", out string account_token);
                    if (input_account)
                    {
                        bool input_id = DialogShow.ShowInput((MainWindow)Window.GetWindow(this), "隧道ID", out string proxy_id);
                        if (input_id)
                        {
                            File.WriteAllText(frpchome, $"-u {account_token} -p {proxy_id}");
                            DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "Frpc配置已保存", "信息");
                            frpProvider.SelectedIndex = 0;
                        }
                        else
                        {
                            DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "请输入隧道ID", "错误");
                            frpProvider.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        DialogShow.ShowMsg((MainWindow)Window.GetWindow(this), "请输入账户token", "错误");
                        frpProvider.SelectedIndex = 0;
                    }
                }
                */
            }
        }
    }
}
