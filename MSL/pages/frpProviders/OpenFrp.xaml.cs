using HandyControl.Controls;
using MSL.controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private bool isInitialize = false;

        public OpenFrp()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialize)
            {
                isInitialize = true;
                //cts = new CancellationTokenSource();
                if (OpenFrpApi.authId != "")
                {
                    //Task.Run(() => GetFrpsInfo(cts.Token));
                    Task.Run(() => GetFrpsInfo());
                    return;
                }
                LoginGrid.Visibility = Visibility.Visible;
                MainGrid.Visibility = Visibility.Hidden;
            }
        }

        /*
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
        */

        private async Task GetFrpsInfo()
        {
            OpenFrpApi control = new OpenFrpApi();
            if (OpenFrpApi.userAccount == "" || OpenFrpApi.userPass == "")
            {
                await Dispatcher.Invoke(async () =>
                {
                    OpenFrpApi.userAccount = await Shows.ShowInput(Window.GetWindow(this), "请输入OpenFrp的账户名/邮箱");
                });

                if (OpenFrpApi.userAccount != null)
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        OpenFrpApi.userPass = await Shows.ShowInput(Window.GetWindow(this), "请输入" + OpenFrpApi.userAccount + "的密码", "", true);
                    });

                    if (OpenFrpApi.userPass == null)
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
            string usr_info = await control.Login(OpenFrpApi.userAccount, OpenFrpApi.userPass);
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
                    OpenFrpApi.authId = string.Empty;
                    OpenFrpApi.userAccount = string.Empty;
                    OpenFrpApi.userPass = string.Empty;
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
            string welcome = $"用户名：{userdata["data"]["username"]}[{userdata["data"]["friendlyGroup"]}]\n";
            string userid = $"ID：{userdata["data"]["id"]}\n";
            string email = $"邮箱：{userdata["data"]["email"]}\n";
            string traffic = $"剩余流量：{userdata["data"]["traffic"]}Mib\n";
            string limit = $"带宽限制：{userdata["data"]["outLimit"]}↑ | ↓ {userdata["data"]["inLimit"]}\n";
            string used = $"已用隧道：{userdata["data"]["used"]}条";
            string showusrinfo = welcome + userid + email + traffic + limit + used;
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
            Directory.CreateDirectory("MSL\\frp");
            File.WriteAllText(@"MSL\frp\frpc", $"-u {token} -p {id}");
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
            //cts = new CancellationTokenSource();
            //Task.Run(() => GetFrpsInfo(cts.Token));
            Task.Run(() => GetFrpsInfo());
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
                OpenFrpApi control = new OpenFrpApi();
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
            OpenFrpApi control = new OpenFrpApi();
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
                OpenFrpApi control = new OpenFrpApi();
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
                toggleAddProxiesGroup.Margin = new Thickness(300, 245, 0, 0);
                delProxieBtn.Margin = new Thickness(515, 245, 0, 0);
                signBtn.Margin = new Thickness(630, 245, 0, 0);
                userInfo.Height = 235;
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
            OpenFrpApi.authId = string.Empty;
            OpenFrpApi.userAccount = string.Empty;
            OpenFrpApi.userPass = string.Empty;
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Hidden;
            userInfo.Content = string.Empty;
            signBtn.IsEnabled = false;
            logoutBtn.IsEnabled = false;
        }
    }

    #region OpenFrp Api
    internal class OpenFrpApi
    {
        public static string userAccount = "";
        public static string userPass = "";
        public static string authId = "";

        public Dictionary<string, string> GetUserNodes()
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            var responseMessage = Functions.Post("getUserProxies", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);
            try
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                JArray jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (JToken node in jArray)
                {
                    Nodes.Add(node["proxyName"].ToString(), node["id"].ToString());
                }
                return Nodes;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var _reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = _reader.ReadToEnd();
                        }
                    }
                }
                return null;
            }
        }

        public (Dictionary<string, string>, JArray) GetNodeList(Window window)
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            var responseMessage = Functions.Post("getNodeList", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);

            try
            {
                Dictionary<string, string> Nodes = new Dictionary<string, string>();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseMessage);
                var jArray = JArray.Parse(jo["data"]["list"].ToString());
                foreach (var node in jArray)
                {
                    if (node["port"].ToString() != "您无权查询此节点的地址" && Convert.ToInt16(node["status"]) == 200 && !Convert.ToBoolean(node["fullyLoaded"]))
                    {
                        string[] targetGroup = node["group"].ToString().Split(';');
                        string nodename = "";
                        if (node["comments"].ToString() == "")
                        {
                            nodename = $"{node["name"]}";

                        }
                        else
                        {
                            nodename = $"[{node["comments"]}]{node["name"]}";
                        }
                        Nodes.Add(nodename, node["id"].ToString());
                    }
                }
                return (Nodes, jArray);
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string error = reader.ReadToEnd();
                            Shows.ShowMsgDialog(window, error, "获取用户信息失败");
                        }
                    }
                }
                return (null, null);
            }
        }

        /*
        public void UserSign(Window window)
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            string responseMessage = "";
            try
            {
                Functions.Post("userSign", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);
            }
            catch
            {
                Shows.ShowMsgDialog(window, "签到失败！请登录OpenFrp官网进行签到！", "错误");
                return;
            }
            try
            {
                if ((bool)JObject.Parse(responseMessage)["flag"] == true && JObject.Parse(responseMessage)["msg"].ToString() == "OK")
                {
                    Shows.ShowMsgDialog(window, JObject.Parse(responseMessage)["data"].ToString(), "签到成功");
                }
                else
                {
                    Shows.ShowMsgDialog(window, "签到失败", "签到失败");
                }
            }
            catch (Exception ex)
            {
                Shows.ShowMsgDialog(window, "签到失败,产生的错误:\n" + ex.Message, "签到失败");
            }
        }
        */

        public string GetUserInfo()
        {
            WebHeaderCollection header = new WebHeaderCollection
            {
                authId
            };
            string responseMessage = Functions.Post("getUserInfo", 0, string.Empty, "https://of-dev-api.bfsea.xyz/frp/api", header);
            return responseMessage;
        }

        public async Task<string> Login(string account, string password)
        {
            HttpClient client = new HttpClient();
            JObject logininfo = new JObject
            {
                ["user"] = account,
                ["password"] = password
            };
            string json = JsonConvert.SerializeObject(logininfo);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            // 发送 POST 请求到登录 API 地址
            HttpResponseMessage loginResponse = await client.PostAsync("https://openid.17a.ink/api/public/login", content);
            // 检查响应状态码是否为 OK
            if (loginResponse.IsSuccessStatusCode)
            {
                await loginResponse.Content.ReadAsStringAsync();
                string authUrl;
                try
                {
                    WebClient webClient = new WebClient
                    {
                        Credentials = CredentialCache.DefaultCredentials
                    };
                    byte[] pageData = await webClient.DownloadDataTaskAsync("https://of-dev-api.bfsea.xyz/oauth2/login");
                    authUrl = JObject.Parse(Encoding.UTF8.GetString(pageData))["data"].ToString();
                    if (!authUrl.Contains("https://openid.17a.ink/api/") && authUrl.Contains("https://openid.17a.ink/"))
                    {
                        authUrl = authUrl.Replace("https://openid.17a.ink/", "https://openid.17a.ink/api/");
                    }
                }
                catch (Exception ex)
                {
                    return $"Get-Login-Url request failed: {ex.Message}";
                }
                HttpResponseMessage authResponse = await client.GetAsync(authUrl);
                // 检查响应状态码是否为 OK
                if (authResponse.IsSuccessStatusCode)
                {
                    // 读取响应内容
                    string authData = await authResponse.Content.ReadAsStringAsync();
                    // 显示响应内容
                    //MessageBox.Show(authData);
                    // 从响应内容中提取 code
                    authId = JObject.Parse(authData)["data"]["code"].ToString();

                    HttpResponseMessage _loginResponse = await client.GetAsync("https://of-dev-api.bfsea.xyz/oauth2/callback?code=" + authId);
                    // 检查响应状态码是否为 OK
                    if (authResponse.IsSuccessStatusCode)
                    {
                        authId = _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:"), _loginResponse.Headers.ToString().Substring(_loginResponse.Headers.ToString().IndexOf("Authorization:")).IndexOf("\n") - 1);
                        //MessageBox.Show(authId);
                        string ret = GetUserInfo();
                        return ret;
                    }
                    else
                    {
                        // 如果响应状态码不是 OK，抛出异常
                        return $"Login request failed: {authResponse.StatusCode}";
                    }

                }
                else
                {
                    // 如果响应状态码不是 OK，抛出异常
                    return $"Auth request failed: {authResponse.StatusCode}";
                }
            }
            else
            {
                // 如果响应状态码不是 OK，抛出异常
                return $"Pre-Login request failed: {loginResponse.StatusCode}";
            }
        }

        public bool CreateProxy(string type, string port, bool EnableZip, int nodeid, string remote_port, string proxy_name, out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/newProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(authId);
            string json = JsonConvert.SerializeObject(new JObject()
            {
                ["node_id"] = nodeid,
                ["name"] = proxy_name,
                ["type"] = type,
                ["local_addr"] = "127.0.0.1",
                ["local_port"] = port,
                ["remote_port"] = remote_port,
                ["domain_bind"] = "",
                ["dataGzip"] = EnableZip,
                ["dataEncrypt"] = false,
                ["url_route"] = "",
                ["host_rewrite"] = "",
                ["request_from"] = "",
                ["request_pass"] = "",
                ["custom"] = ""
            });//转换json格式
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseMessage = reader.ReadToEnd();
                var deserializedMessage = JObject.Parse(responseMessage);
                reader.Close();
                dataStream.Close();
                response.Close();
                if ((bool)deserializedMessage["flag"] == true)
                {
                    returnMsg = "";
                    return true;
                }
                else
                {
                    returnMsg = deserializedMessage["msg"].ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                returnMsg = ex.Message;
                return false;
            }
        }

        public bool DeleteProxy(string id, out string returnMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://of-dev-api.bfsea.xyz/frp/api/removeProxy");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(authId);
            JObject json = new JObject()
            {
                ["proxy_id"] = id,
            };//转换json格式
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json));
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseMessage = reader.ReadToEnd();
                var deserializedMessage = JObject.Parse(responseMessage);
                reader.Close();
                dataStream.Close();
                response.Close();
                if ((bool)deserializedMessage["flag"] == true)
                {
                    returnMsg = "";
                    return true;
                }
                else
                {
                    returnMsg = deserializedMessage["msg"].ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                returnMsg = ex.Message;
                return false;
            }
        }
    }
    #endregion
}
