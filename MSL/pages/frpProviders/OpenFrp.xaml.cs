using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                // 获取Token并尝试登录
                var authId = string.IsNullOrEmpty(OpenFrpApi.AuthId)
                    ? Config.Read("OpenFrpToken")?.ToString()
                    : OpenFrpApi.AuthId;

                if (string.IsNullOrEmpty(authId))
                {
                    return;
                }
                if (authId != "")
                {
                    await TokenLogin(authId);
                }
            }
        }

        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            await TokenLogin();
        }

        private async Task TokenLogin(string token = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入Authorization");
                if (token == null)
                {
                    return;
                }
            }

            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg) = await OpenFrpApi.Login("", "", token, SaveToken.IsChecked == true);
            MagicDialog.CloseTextDialog();
            if (Code == 200)
            {
                LoginGrid.Visibility = Visibility.Collapsed;
                MainGrid.Visibility = Visibility.Visible;
                GetUserInfo(JObject.Parse(Msg));
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的Authorization是否正确！" + Msg, "错误！");
                return;
            }
            return;
        }

        private async void userLogin_Click(object sender, RoutedEventArgs e)
        {
            string userAccount = await MagicShow.ShowInput(Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱");
            string userPass;
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

            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg) = await OpenFrpApi.Login(userAccount, userPass, save: SaveToken.IsChecked == true);
            MagicDialog.CloseTextDialog();
            if (Code == 200)
            {
                LoginGrid.Visibility = Visibility.Collapsed;
                MainGrid.Visibility = Visibility.Visible;
                GetUserInfo(JObject.Parse(Msg));
            }
            else
            {
                //Console.WriteLine(Msg);
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的用户名或密码是否正确！" + Msg, "错误！");
                return;
            }
            return;
        }

        private async void GetUserInfo(JObject userdata)
        {
            string showusrinfo = $"用户名：{userdata["data"]["username"]}[{userdata["data"]["friendlyGroup"]}]\n" +
                $"ID：{userdata["data"]["id"]}\n" +
                $"邮箱：{userdata["data"]["email"]}\n" +
                $"剩余流量：{userdata["data"]["traffic"]}Mib\n" +
                $"带宽限制：{userdata["data"]["outLimit"]}↑ | ↓ {userdata["data"]["inLimit"]}\n" +
                $"已用隧道：{userdata["data"]["used"]}条";
            token = userdata["data"]["token"].ToString();
            userInfo.Content = showusrinfo;

            await GetUserTunnels();
        }

        private async Task GetUserTunnels()
        {
            serversList.Items.Clear();
            if (toggleProxies.SelectedIndex == 0)
            {
                var (Code, Data, Msg) = await OpenFrpApi.GetUserNodes();
                if (Code == 200)
                {
                    if (Data.Count != 0)
                    {
                        nodelist = Data;
                        foreach (KeyValuePair<string, string> node in nodelist)
                        {
                            serversList.Items.Add(node.Key);
                        }
                    }
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取失败！" + Msg, "错误！");
                }
            }
            else
            {
                (Dictionary<string, string>, JArray) process = await OpenFrpApi.GetNodeList();
                if (process == (null, null))
                {
                    MagicShow.ShowMsgDialog("获取节点列表失败！", "ERR");
                    return;
                }
                Dictionary<string, string> item1 = process.Item1;
                nodelist = item1;
                jArray = process.Item2;
                foreach (var node in item1)
                {
                    serversList.Items.Add(node.Key);
                }
            }
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
            Config.WriteFrpcConfig(1, $"OpenFrp节点 - {o}", $"-u {token} -p {id}", "");
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
            addProxieBtn.IsEnabled = false;
            Window window = Window.GetWindow(Window.GetWindow(this));
            try
            {
                if (toggleProxies.SelectedIndex != 1 || serversList.SelectedIndex == -1)
                {
                    addProxieBtn.IsEnabled = true;
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    toggleProxies.SelectedIndex = 1;
                    return;
                }
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
                    addProxieBtn.IsEnabled = true;
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    return;
                }
                string proxy_name = await MagicShow.ShowInput(Window.GetWindow(this), "隧道名称(不支持中文)");
                if (proxy_name != null)
                {
                    var (_return, msg) = await OpenFrpApi.CreateProxy(type, portBox.Text, zip, selected_node_id, remotePortBox.Text, proxy_name);
                    if (_return)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！", "提示");
                        toggleProxies.SelectedIndex = 0;
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "创建失败！" + msg, "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
            addProxieBtn.IsEnabled = true;
        }

        private async void toggleProxies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            await GetUserTunnels();
        }

        private async void delProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            delProxieBtn.IsEnabled = false;
            try
            {
                if (toggleProxies.SelectedIndex != 0 || serversList.SelectedIndex == -1)
                {
                    delProxieBtn.IsEnabled = true;
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个隧道", "错误");
                    toggleProxies.SelectedIndex = 0;
                    return;
                }
                object o = serversList.SelectedValue;
                string id = nodelist[o.ToString()];
                var (_return, msg) = await OpenFrpApi.DeleteProxy(id);
                if (_return)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "删除成功！", "提示");
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "删除失败！" + msg, "错误");
                }
                await GetUserTunnels();
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
            delProxieBtn.IsEnabled = true;
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
                addProxiesGroup.Visibility = Visibility.Collapsed;
                toggleProxies.SelectedIndex = 0;
            }
            else
            {
                toggleAddProxiesGroup.Content = "收起";
                userInfoGrid.Visibility = Visibility.Collapsed;
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
        }
    }
}
