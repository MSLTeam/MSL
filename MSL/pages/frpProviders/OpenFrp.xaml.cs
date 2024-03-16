using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
        private CancellationTokenSource cts;
        private string token;
        private JArray jArray;
        private Dictionary<string, string> nodelist;
        public OpenFrp()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            if (APIControl.authId != "")
            {
                Task.Run(() => GetFrpsInfo(cts.Token));
                return;
            }
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Hidden;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            try
            {
                LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                MainGrid.Children.Remove(loadingCircle);
                MainGrid.UnregisterName("loadingBar");
            }
            catch
            { }
        }

        private async Task GetFrpsInfo(CancellationToken ct)
        {
            APIControl control = new APIControl();
            if (APIControl.userAccount == "" || APIControl.userPass == "")
            {
                await Dispatcher.Invoke(async () =>
                {
                    APIControl.userAccount = await Shows.ShowInput(Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱");
                });

                if (APIControl.userAccount != null)
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        APIControl.userPass = await Shows.ShowInput(Window.GetWindow(this), "请输入" + APIControl.userAccount + "的密码", "", true);
                    });

                    if (APIControl.userPass == null)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            Dispatcher.Invoke(() =>
            {
                LoginGrid.Visibility = Visibility.Hidden;
                MainGrid.Visibility = Visibility.Visible;
                signBtn.IsEnabled = false;
                logoutBtn.IsEnabled = false;
                addProxieBtn.IsEnabled = false;
                delProxieBtn.IsEnabled = false;
                toggleProxies.IsEnabled = false;
                toggleAddProxiesGroup.IsEnabled = false;
                doneBtn.IsEnabled = false;
                try
                {
                    LoadingCircle loadingCircle = new LoadingCircle();
                    loadingCircle.VerticalAlignment = VerticalAlignment.Top;
                    loadingCircle.HorizontalAlignment = HorizontalAlignment.Left;
                    loadingCircle.Margin = new Thickness(130, 150, 0, 0);
                    MainGrid.Children.Add(loadingCircle);
                    MainGrid.RegisterName("loadingBar", loadingCircle);
                }
                catch
                { }
            });
            //MessageBox.Show("111");
            string usr_info = await control.Login(APIControl.userAccount, APIControl.userPass);
            //MessageBox.Show("11");
            JObject userdata = null;
            try
            {
                userdata = JObject.Parse(usr_info);
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的用户名或密码是否正确！\n" + usr_info, "错误！");
                    //APIControl.sessionId = string.Empty;
                    APIControl.authId = string.Empty;
                    APIControl.userAccount = string.Empty;
                    APIControl.userPass = string.Empty;
                    LoginGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Hidden;
                    try
                    {
                        LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                        MainGrid.Children.Remove(loadingCircle);
                        MainGrid.UnregisterName("loadingBar");
                    }
                    catch
                    { }
                });
                return;
            }
            string welcome = $"欢迎,{userdata["data"]["username"]}\n";
            string limit = $"带宽限制: {userdata["data"]["outLimit"]}↑ | ↓ {userdata["data"]["inLimit"]}\n";
            string used = $"已用隧道:{userdata["data"]["used"]}条\n";
            string group = $"用户组:{userdata["data"]["friendlyGroup"]}\n";
            string userid = $"ID:{userdata["data"]["id"]}\n";
            string email = $"邮箱:{userdata["data"]["email"]}\n";
            string traffic = $"剩余流量:{userdata["data"]["traffic"]}Mib";
            string showusrinfo = welcome + traffic + limit + group + userid + email + used;
            token = userdata["data"]["token"].ToString();
            Dispatcher.Invoke(() =>
            {
                userInfo.Content = showusrinfo;
                signBtn.IsEnabled = true;
                logoutBtn.IsEnabled = true;
            });
            try
            {
                int loadMode = 0;
                Dispatcher.Invoke(() =>
                {
                    serversList.Items.Clear();
                    loadMode = toggleProxies.SelectedIndex;
                });
                if (loadMode == 0)
                {
                    Dictionary<string, string> process = control.GetUserNodes();
                    if (process.Count != 0)
                    {
                        nodelist = process;
                        foreach (KeyValuePair<string, string> node in process)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                serversList.Items.Add(node.Key);
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Shows.ShowMsgDialog(Window.GetWindow(this), "你的账户看起来一条隧道也没有……", "提示");
                        });
                    }
                }
                else
                {
                    (Dictionary<string, string>, JArray) process = control.GetNodeList(Window.GetWindow(this));
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
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("err");
                });
            }
            Dispatcher.Invoke(() =>
            {
                addProxieBtn.IsEnabled = true;
                delProxieBtn.IsEnabled = true;
                toggleProxies.IsEnabled = true;
                toggleAddProxiesGroup.IsEnabled = true;
                doneBtn.IsEnabled = true;
                try
                {
                    LoadingCircle loadingCircle = MainGrid.FindName("loadingBar") as LoadingCircle;
                    MainGrid.Children.Remove(loadingCircle);
                    MainGrid.UnregisterName("loadingBar");
                }
                catch
                { }
            });
            if (ct.IsCancellationRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    serversList.Items.Clear();
                });
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (toggleProxies.SelectedIndex != 0 || serversList.SelectedIndex == -1)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请确保您选择了一个隧道", "错误");
                toggleProxies.SelectedIndex = 0;
                return;
            }
            if (portBox.Text == "")
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "请确保内网端口不为空", "错误");
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
            File.WriteAllText(@"MSL\frpc", $"-u {token} -p {id}");
            JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            jobject["frpcServer"] = "1";
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
            await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
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

        private void userLogin_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            Task.Run(() => GetFrpsInfo(cts.Token));
        }

        private async void signBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
            APIControl apiControl = new APIControl();
            apiControl.UserSign(Window.GetWindow(Window.GetWindow(this)));
            */
            await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "目前暂不支持在软件内签到，请前往OpenFrp官网进行签到！", "提示");
            Process.Start("https://www.openfrp.net/");
        }

        private async void addProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(Window.GetWindow(this));
            try
            {
                if (toggleProxies.SelectedIndex != 1 || serversList.SelectedIndex == -1)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    toggleProxies.SelectedIndex = 1;
                    return;
                }
                addProxieBtn.IsEnabled = false;
                delProxieBtn.IsEnabled = false;
                logoutBtn.IsEnabled = false;
                toggleProxies.IsEnabled = false;
                toggleAddProxiesGroup.IsEnabled = false;
                doneBtn.IsEnabled = false;
                APIControl control = new APIControl();
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
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    return;
                }
                string proxy_name = await Shows.ShowInput(Window.GetWindow(this), "隧道名称(不支持中文)");
                if (proxy_name != null)
                {
                    string returnMsg = "";
                    bool createReturn = control.CreateProxy(type, portBox.Text, zip, selected_node_id, remotePortBox.Text, proxy_name, out returnMsg);
                    if (createReturn)
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！", "提示");
                        toggleProxies.SelectedIndex = 0;
                    }
                    else
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "创建失败！" + returnMsg, "错误");
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
                Shows.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
        }

        private async void toggleProxies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            serversList.Items.Clear();
            APIControl control = new APIControl();
            Window window = Window.GetWindow(Window.GetWindow(this));
            if (toggleProxies.SelectedIndex == 0)
            {
                Dictionary<string, string> process = await Task.Run(() => control.GetUserNodes());
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
                (Dictionary<string, string>, JArray) process = await Task.Run(() => control.GetNodeList(window));
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
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请先选择一个隧道", "错误");
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
                APIControl control = new APIControl();
                string returnMsg = "";
                bool delReturn = await Task.Run(() => control.DeleteProxy(id, out returnMsg));
                if (delReturn)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "删除成功！", "提示");
                }
                else
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "删除失败！" + returnMsg, "错误");
                }
                Dictionary<string, string> process = control.GetUserNodes();
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
                Shows.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
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
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    toggleProxies.SelectedIndex = 1;
                    return;
                }
                (int, int) remote_port_limit = (10000, 99999);
                string selected_node = serversList.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(nodelist[selected_node]);
                else
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
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
                toggleAddProxiesGroup.Margin = new Thickness(300, 250, 0, 0);
                delProxieBtn.Margin = new Thickness(515, 250, 0, 0);
                signBtn.Margin = new Thickness(630, 250, 0, 0);
                userInfo.Height = 240;
                addProxiesGroup.Visibility = Visibility.Hidden;
                toggleProxies.SelectedIndex = 0;
            }
            else
            {
                toggleAddProxiesGroup.Content = "收起";
                toggleAddProxiesGroup.Margin = new Thickness(300, 145, 0, 0);
                delProxieBtn.Margin = new Thickness(515, 145, 0, 0);
                signBtn.Margin = new Thickness(630, 145, 0, 0);
                userInfo.Height = 135;
                addProxiesGroup.Visibility = Visibility.Visible;
                toggleProxies.SelectedIndex = 1;
                RandRemotePort(false);
            }
        }

        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            //APIControl.sessionId = string.Empty;
            APIControl.authId = string.Empty;
            APIControl.userAccount = string.Empty;
            APIControl.userPass = string.Empty;
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Hidden;
            userInfo.Content = string.Empty;
            signBtn.IsEnabled = false;
            logoutBtn.IsEnabled = false;
        }
    }
}
