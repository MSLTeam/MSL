using HandyControl.Controls;
using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// OpenFrp.xaml 的交互逻辑
    /// </summary>
    public partial class OpenFrp : Page
    {
        //private CancellationTokenSource cts;
        private string token;
        private JArray jArray;
        private Dictionary<string, string> nodelist;

        public OpenFrp()
        {
            InitializeComponent();
        }

        private bool isInit = false;
        private async void Page_Loaded(object sender, EventArgs e)
        {
            if (!isInit)
            {
                isInit = true;
                //显示登录页面
                LoginGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Collapsed;
                OpenFrpApi.AuthId = Config.Read("OpenFrpToken")?.ToString() ?? "";
                if (OpenFrpApi.AuthId != "")
                {
                    await TokenLogin();
                    return;
                }
            }
        }

        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            await TokenLogin();
        }

        private async Task TokenLogin()
        {
            if (string.IsNullOrEmpty(OpenFrpApi.AuthId))
            {
                string authId = await MagicShow.ShowInput(Window.GetWindow(this), "请输入Authorization");
                if (authId == null)
                {
                    return;
                }
                OpenFrpApi.AuthId = authId;
            }

            LoginGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var data = await OpenFrpApi.GetUserInfo();
            MagicDialog.CloseTextDialog();
            if (data.HttpResponseCode == HttpStatusCode.OK)
            {
                GetFrpsInfo(JObject.Parse(data.HttpResponseContent.ToString()));
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的Authorization是否正确！\n" + data.HttpResponseCode, "错误！");
                OpenFrpApi.AuthId = string.Empty;
                LoginGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Hidden;
                return;
            }
        }

        private async void userLogin_Click(object sender, RoutedEventArgs e)
        {
            string userPass;
            OpenFrpApi control = new OpenFrpApi();
            string userAccount = await MagicShow.ShowInput(Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱");
            if (userAccount != null)
            {
                userPass = await MagicShow.ShowInput(Window.GetWindow(this), "请输入" + userAccount + "的密码", "", true);
                if (userPass == null)
                {
                    return;
                }
            }
            else
            {
                return;
            }
            LoginGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            string usr_info = await OpenFrpApi.Login(userAccount, userPass);
            MagicDialog.CloseTextDialog();
            JObject userdata = null;
            try
            {
                userdata = JObject.Parse(usr_info);
            }
            catch
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的用户名或密码是否正确！\n" + usr_info, "错误！");
                OpenFrpApi.AuthId = string.Empty;
                LoginGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Hidden;
                return;
            }
            GetFrpsInfo(userdata);
        }

        private void GetFrpsInfo(JObject userdata)
        {
            if ((bool)userdata["flag"] == false)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您输入的信息是否正确！\n" + userdata["msg"], "错误！");
                OpenFrpApi.AuthId = string.Empty;
                LoginGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Hidden;
                return;
            }
            if (SaveToken.IsChecked == true)
            {
                Config.Write("OpenFrpToken", OpenFrpApi.AuthId);
            }
            string welcome = $"用户名：{userdata["data"]["username"]}[{userdata["data"]["friendlyGroup"]}]\n";
            string userid = $"ID：{userdata["data"]["id"]}\n";
            string email = $"邮箱：{userdata["data"]["email"]}\n";
            string traffic = $"剩余流量：{userdata["data"]["traffic"]}Mib\n";
            string limit = $"带宽限制：{userdata["data"]["outLimit"]}↑ | ↓ {userdata["data"]["inLimit"]}\n";
            string used = $"已用隧道：{userdata["data"]["used"]}条";
            string showusrinfo = welcome + userid + email + traffic + limit + used;
            token = userdata["data"]["token"].ToString();
            userInfo.Content = showusrinfo;
            try
            {
                int loadMode = 0;
                serversList.Items.Clear();
                loadMode = toggleProxies.SelectedIndex;
                if (loadMode == 0)
                {
                    Dictionary<string, string> process = OpenFrpApi.GetUserNodes();
                    if (process.Count != 0)
                    {
                        nodelist = process;
                        foreach (KeyValuePair<string, string> node in process)
                        {
                            serversList.Items.Add(node.Key);
                        }
                    }
                }
                else
                {
                    (Dictionary<string, string>, JArray) process = OpenFrpApi.GetNodeList(Window.GetWindow(this));
                    Dictionary<string, string> item1 = process.Item1;
                    nodelist = item1;
                    jArray = process.Item2;
                    foreach (var node in item1)
                    {
                        serversList.Items.Add(node.Key);
                    }
                }
            }
            catch
            {
                MessageBox.Show("err");
            }
            try
            {
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                MainGrid.Children.Remove(loadingCircle);
                MainGrid.UnregisterName("loadingBar");
            }
            catch
            { }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (toggleProxies.SelectedIndex != 0 || serversList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请确保您选择了一个隧道", "错误");
                toggleProxies.SelectedIndex = 0;
                return;
            }
            if (portBox.Text == "")
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请确保内网端口不为空", "错误");
                return;
            }

            //现有隧道
            object o = serversList.SelectedValue;
            if (Equals(o, null))
            {
                MessageBox.Show("请确保选择了节点", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string id = nodelist[o.ToString()];
            Config.WriteFrpcConfig(1, $"OpenFrp节点 - {o.ToString()}", $"-u {token} -p {id}", "");
            await MagicShow.ShowMsgDialogAsync(window, "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            window.Close();
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

        private void gotoWeb_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.openfrp.net/");
        }

        private void userRegister_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.openfrp.net/");
        }

        private async void addProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            try
            {
                if (toggleProxies.SelectedIndex != 1 || serversList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    toggleProxies.SelectedIndex = 1;
                    return;
                }
                addProxieBtn.IsEnabled = false;
                delProxieBtn.IsEnabled = false;
                logoutBtn.IsEnabled = false;
                toggleProxies.IsEnabled = false;
                toggleAddProxiesGroup.IsEnabled = false;
                doneBtn.IsEnabled = false;
                string type;
                if (frpcType.SelectedIndex == 0) type = "tcp";
                else type = "udp";
                bool zip;
                if ((bool)enableCompression.IsChecked) zip = true;
                else zip = false;
                string selected_node = serversList.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(nodelist[selected_node]);
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    return;
                }
                string proxy_name = await MagicShow.ShowInput(Window.GetWindow(this), "隧道名称(不支持中文)");
                if (proxy_name != null)
                {
                    string returnMsg = "";
                    bool createReturn = OpenFrpApi.CreateProxy(type, portBox.Text, zip, selected_node_id, remotePortBox.Text, proxy_name, out returnMsg);
                    if (createReturn)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！", "提示");
                        toggleProxies.SelectedIndex = 0;
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "创建失败！" + returnMsg, "错误");
                    }
                }
                addProxieBtn.IsEnabled = true;
                delProxieBtn.IsEnabled = true;
                logoutBtn.IsEnabled = true;
                toggleProxies.IsEnabled = true;
                toggleAddProxiesGroup.IsEnabled = true;
                doneBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
        }

        private async void toggleProxies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            serversList.Items.Clear();
            OpenFrpApi control = new OpenFrpApi();
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (toggleProxies.SelectedIndex == 0)
            {
                Dictionary<string, string> process = await Task.Run(() => OpenFrpApi.GetUserNodes());
                if (process.Count != 0)
                {
                    nodelist = process;
                    foreach (KeyValuePair<string, string> node in process)
                    {
                        serversList.Items.Add(node.Key);
                    }
                }
            }
            else
            {
                (Dictionary<string, string>, JArray) process = await Task.Run(() => OpenFrpApi.GetNodeList(window));
                Dictionary<string, string> item1 = process.Item1;
                nodelist = item1;
                jArray = process.Item2;
                foreach (var node in item1)
                {
                    Dispatcher.Invoke(() =>
                    {
                        serversList.Items.Add(node.Key);
                    });
                }
            }
        }

        private async void delProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (toggleProxies.SelectedIndex != 0 || serversList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个隧道", "错误");
                    toggleProxies.SelectedIndex = 0;
                    return;
                }
                delProxieBtn.IsEnabled = false;
                addProxieBtn.IsEnabled = false;
                logoutBtn.IsEnabled = false;
                toggleProxies.IsEnabled = false;
                doneBtn.IsEnabled = false;
                object o = serversList.SelectedValue;
                string id = nodelist[o.ToString()];
                serversList.Items.Clear();
                string returnMsg = "";
                bool delReturn = await Task.Run(() => OpenFrpApi.DeleteProxy(id, out returnMsg));
                if (delReturn)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "删除成功！", "提示");
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "删除失败！" + returnMsg, "错误");
                }
                Dictionary<string, string> process = OpenFrpApi.GetUserNodes();
                if (process.Count != 0)
                {
                    nodelist = process;
                    foreach (KeyValuePair<string, string> node in process)
                    {
                        serversList.Items.Add(node.Key);
                    }
                }
                delProxieBtn.IsEnabled = true;
                addProxieBtn.IsEnabled = true;
                logoutBtn.IsEnabled = true;
                toggleProxies.IsEnabled = true;
                doneBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
        }

        private void randomRemotePort_Click(object sender, RoutedEventArgs e)
        {
            RandRemotePort(true);
        }

        void RandRemotePort(bool tips)
        {
            if (tips)
            {
                if (toggleProxies.SelectedIndex != 1 || serversList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    toggleProxies.SelectedIndex = 1;
                    return;
                }
                (int, int) remote_port_limit = (10000, 99999);
                string selected_node = serversList.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(nodelist[selected_node]);
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    return;
                }
                foreach (var node in jArray)
                {
                    if (Convert.ToInt32(node["id"]) == selected_node_id)
                    {
                        try
                        {
                            var s = node["allowPort"].ToString().Trim('(', ')', ' ');
                            remote_port_limit = ValueTuple.Create(Array.ConvertAll(s.Split(','), int.Parse)[0], Array.ConvertAll(s.Split(','), int.Parse)[1]);
                        }
                        catch { remote_port_limit = (10000, 99999); }
                        break;
                    }
                }
                Random random = new Random();
                string remote_port;
                remote_port = random.Next(remote_port_limit.Item1, remote_port_limit.Item2).ToString();
                remotePortBox.Text = remote_port;
            }
            else
            {
                (int, int) remote_port_limit = (10000, 99999);
                Random random = new Random();
                string remote_port;
                remote_port = random.Next(remote_port_limit.Item1, remote_port_limit.Item2).ToString();
                remotePortBox.Text = remote_port;
            }
        }

        private void toggleAddProxiesGroup_Click(object sender, RoutedEventArgs e)
        {
            if (toggleAddProxiesGroup.Content.ToString() == "收起")
            {
                toggleAddProxiesGroup.Content = "新建隧道（点击展开）";
                userInfoGrid.Visibility = Visibility.Visible;
                delProxieBtn.IsEnabled = true;
                addProxiesGroup.Visibility = Visibility.Collapsed;
                toggleProxies.SelectedIndex = 0;
            }
            else
            {
                toggleAddProxiesGroup.Content = "收起";
                userInfoGrid.Visibility = Visibility.Collapsed;
                delProxieBtn.IsEnabled = false;
                addProxiesGroup.Visibility = Visibility.Visible;
                toggleProxies.SelectedIndex = 1;
                RandRemotePort(false);
            }
        }

        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFrpApi.AuthId = string.Empty;
            Config.Remove("OpenFrpToken");
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Hidden;
            userInfo.Content = string.Empty;
            signBtn.IsEnabled = false;
            logoutBtn.IsEnabled = false;
        }
    }
}
