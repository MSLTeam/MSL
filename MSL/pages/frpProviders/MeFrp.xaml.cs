using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// MEFrp.xaml 的交互逻辑
    /// </summary>
    public partial class MEFrp : Page
    {
        private string ApiUrl { get; } = "https://api.mefrp.com/api";
        private string UserToken { get; set; }

        public MEFrp()
        {
            InitializeComponent();
        }

        private bool isInit = false;
        private async void Page_Loaded(object sender, EventArgs e)
        {
            if (!isInit)
            {
                isInit = true;
                LogHelper.Write.Info("ME Frp页面已加载，检查本地Token。");
                //显示登录页面
                LoginGrid.Visibility = Visibility.Visible;
                MainCtrl.Visibility = Visibility.Collapsed;
                var token = Config.Read("MEFrpToken")?.ToString() ?? "";
                if (token != "")
                {
                    LogHelper.Write.Info("检测到本地ME Frp Token，尝试自动登录。");
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await VerifyUserToken(token, false); //移除空格，防止笨蛋
                    MagicDialog.CloseTextDialog();
                }
            }
        }

        private async void MainCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }
            if (!ReferenceEquals(e.OriginalSource, this.MainCtrl))
            {
                return;
            }
            switch (MainCtrl.SelectedIndex)
            {
                case 0:
                    LogHelper.Write.Info("切换到隧道列表标签页，正在获取隧道列表。");
                    await GetTunnelList();
                    break;
                case 1:
                    LogHelper.Write.Info("切换到创建隧道标签页，正在获取节点列表。");
                    await GetNodeList();
                    Create_Name.Text = Functions.RandomString("MSL_", 6);
                    break;
            }
        }

        private async void UserTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token = await MagicShow.ShowInput(Window.GetWindow(this), "请输入ME Frp账户Token", "", true);
            if (token != null)
            {
                LogHelper.Write.Info("用户尝试使用Token登录ME Frp。");
                bool save = (bool)SaveToken.IsChecked;
                MagicDialog MagicDialog = new MagicDialog();
                MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                await VerifyUserToken(token.Trim(), save); //移除空格，防止笨蛋
                MagicDialog.CloseTextDialog();
            }
        }

        private async void userPasswordLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = await MagicShow.ShowInput(Window.GetWindow(this), "请输入ME Frp账户", "");
            if (user != null)
            {
                string password = await MagicShow.ShowInput(Window.GetWindow(this), "请输入ME Frp账户的密码", "", true);
                if (password != null)
                {
                    Process.Start("https://www.mefrp.com/3rdparty/captcha?client=MSL");
                    string captchaCallback = await MagicShow.ShowInput(Window.GetWindow(this), "请在完成打开网页的人机验证后，获取验证码并填写到此处", "", true);
                    if (captchaCallback != null)
                    {
                        LogHelper.Write.Info($"用户尝试使用 '{user}' 登录ME Frp。");
                        bool save = (bool)SaveToken.IsChecked;
                        MagicDialog MagicDialog = new MagicDialog();
                        MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
                        await GetUserToken(user.Trim(), password.Trim(), captchaCallback.Trim(), save); //移除空格，防止笨蛋
                        MagicDialog.CloseTextDialog();
                    }
                }
            }
        }

        private string[] ParseCaptchaArguments(string encString)
        {
            // base64 decode first  
            byte[] bytes = Convert.FromBase64String(encString);
            string decodedString = Encoding.UTF8.GetString(bytes);
            // split by ||  
            string[] args = decodedString.Split(["||"], StringSplitOptions.RemoveEmptyEntries);
            return args;
        }

        private async Task GetUserToken(string user, string password, string captchaCallback, bool save)
        {
            try
            {
                LogHelper.Write.Info($"正在为用户 '{user}' 获取ME Frp Token...");
                //解析验证码参数
                string[] captchaArgs = ParseCaptchaArguments(captchaCallback);
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/public/login", 0, new JObject
                {
                    ["username"] = user,
                    ["password"] = password,
                    ["vaptchaToken"] = captchaArgs[0],
                    ["vaptchaServer"] = captchaArgs[1],
                });
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jres = JObject.Parse((string)res.HttpResponseContent);
                    if (jres["code"].Value<int>() == 200)
                    {
                        await VerifyUserToken(jres["data"]["token"].ToString(), save);
                    }
                    else
                    {
                        LogHelper.Write.Error($"ME Frp登录失败，API返回错误: {jres["message"]}");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + jres["message"], "错误");
                    }
                }
                else
                {
                    LogHelper.Write.Error($"获取ME Frp Token失败，HTTP状态码: {res.HttpResponseCode}");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！请检查账号密码！", "错误");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取ME Frp Token时发生异常: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + ex.Message, "错误");
            }
        }

        private async Task VerifyUserToken(string token, bool save)
        {
            try
            {
                LogHelper.Write.Info("正在验证ME Frp用户Token...");
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/user/info", headers =>
                {
                    headers.Add("Authorization", $"Bearer {token}");
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    UserToken = token;
                    if (save)
                    {
                        Config.Write("MEFrpToken", token);
                    }

                    //显示main页面
                    LoginGrid.Visibility = Visibility.Collapsed; ;
                    MainCtrl.Visibility = Visibility.Visible;
                    JObject JsonUserInfo = JObject.Parse((string)res.HttpResponseContent);
                    LogHelper.Write.Info($"ME Frp用户 '{JsonUserInfo["data"]["username"]}' Token验证成功，已登录。");
                    if (JsonUserInfo["data"]["todaySigned"].Value<bool>() == true)
                    {
                        SignBtn.IsEnabled = false;
                        SignBtn.Content = "已签到";
                    }
                    else
                    {
                        SignBtn.IsEnabled = true;
                        SignBtn.Content = "签到";
                    }
                    if (JsonUserInfo["data"]["friendlyGroup"].Value<string>() != "未实名")
                    {
                        RealNameTips.Visibility = Visibility.Collapsed;
                    }
                    UserInfo.Text = $"用户名: {JsonUserInfo["data"]["username"]}\n用户类型: {JsonUserInfo["data"]["friendlyGroup"]}\n限速: {int.Parse(JsonUserInfo["data"]["outBound"]?.ToString() ?? "") / 128} Mbps\n隧道数: {JsonUserInfo["data"]["usedProxies"]} / {JsonUserInfo["data"]["maxProxies"]}\n剩余流量: {long.Parse(JsonUserInfo["data"]["traffic"]?.ToString() ?? "") / 1024} GB";
                    //UserLevel = (string)JsonUserInfo["data"]["group"];
                    //获取隧道
                    await GetTunnelList();
                }
                else
                {
                    LogHelper.Write.Warn($"ME Frp Token验证失败，可能是Token已失效。HTTP状态码: {res.HttpResponseCode}");
                    if (Config.Read("MEFrpToken") != null)
                        Config.Remove("MEFrpToken");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！", "错误");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"验证ME Frp Token时发生异常: {ex.ToString()}");
                if (Config.Read("MEFrpToken") != null)
                    Config.Remove("MEFrpToken");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！" + ex.Message, "错误");
            }
        }

        //隧道相关
        internal class TunnelInfo
        {
            public int ID { get; set; }
            public string LPort { get; set; }
            public string RPort { get; set; }
            public string LIP { get; set; }
            public string Name { get; set; }
            public string Node { get; set; }
            public int NodeID { get; set; }
            public bool Online { get; set; }
        }

        private async Task GetTunnelList()
        {
            try
            {
                LogHelper.Write.Info("正在获取ME Frp隧道列表...");
                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;

                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/proxy/list", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });

                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JObject jsonRes = JObject.Parse((string)res.HttpResponseContent);

                    // 解析节点字典
                    Dictionary<int, string> nodeDict = new Dictionary<int, string>();
                    JArray jsonNodes = (JArray)jsonRes["data"]["nodes"];
                    if (jsonNodes != null)
                    {
                        foreach (var node in jsonNodes)
                        {
                            nodeDict[node["nodeId"].Value<int>()] = node["name"].ToString();
                        }
                    }

                    // 解析隧道列表
                    JArray jsonTunnels = (JArray)jsonRes["data"]["proxies"];
                    if (jsonTunnels != null)
                    {
                        foreach (var item in jsonTunnels)
                        {
                            int nodeId = item["nodeId"].Value<int>();
                            string nodeName = nodeDict.TryGetValue(nodeId, out var name) ? name : nodeId.ToString();

                            tunnels.Add(new TunnelInfo
                            {
                                Name = item["proxyName"].ToString(),
                                NodeID = nodeId,
                                Node = nodeName,
                                ID = item["proxyId"].Value<int>(),
                                LIP = item["localIp"].ToString(),
                                LPort = item["localPort"].ToString(),
                                RPort = item["remotePort"].ToString(),
                                Online = item["isOnline"].Value<bool>(),
                            });
                        }
                    }
                    LogHelper.Write.Info($"成功获取到 {jsonTunnels?.Count ?? 0} 个ME Frp隧道。");
                }
                else
                {
                    LogHelper.Write.Error($"获取ME Frp隧道列表失败，HTTP状态码: {res.HttpResponseCode}");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败！HTTP状态码: " + res.HttpResponseCode, "错误");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取ME Frp隧道列表时发生错误: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取隧道列表失败: " + ex.Message, "错误");
            }
        }

        //显示隧道信息
        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                TunnelInfo_Text.Text = $"隧道名: {selectedTunnel.Name}" +
                    $"\n隧道ID: {selectedTunnel.ID}" + $"\n远程端口: {selectedTunnel.RPort}" + $"\n隧道状态: {(selectedTunnel.Online ? "在线" : "离线")}";
                LocalIp.Text = selectedTunnel.LIP;
                LocalPort.Text = selectedTunnel.LPort;
            }
        }

        //确定 输出config
        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var listBox = FrpList;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                LogHelper.Write.Info($"用户选择隧道 '{selectedTunnel.Name}' (ID: {selectedTunnel.ID})，正在生成Frpc配置文件。");
                //输出配置文件
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/user/frpToken", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });
                if (res.HttpResponseCode == System.Net.HttpStatusCode.OK)
                {
                    JObject tokenJson = JObject.Parse((string)res.HttpResponseContent);
                    if (Config.WriteFrpcConfig(4, $"MEFrp - {selectedTunnel.Name}", $"-t {tokenJson["data"]["token"]} -p {selectedTunnel.ID}", "") == true)
                    {
                        LogHelper.Write.Info("Frpc配置文件写入成功。");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                        Window.GetWindow(this).Close();
                    }
                    else
                    {
                        LogHelper.Write.Error("Frpc配置文件写入失败。");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "配置输出失败！", "错误");
                    }
                }
                else
                {
                    LogHelper.Write.Error("Frpc配置文件写入失败。\n获取FrpToken失败！");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "配置输出失败！", "错误");
                }

            }
            else
            {
                LogHelper.Write.Warn("用户点击生成配置，但未选择任何隧道。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            await GetTunnelList();
            (sender as Button).IsEnabled = true;
        }

        //获取某个隧道的配置文件
        private async Task DelTunnel(string token, int id)
        {
            try
            {
                LogHelper.Write.Info($"正在请求删除ME Frp隧道，ID: {id}。");
                //请求头 token
                var headersAction = new Action<HttpRequestHeaders>(headers =>
                {
                    headers.Add("Authorization", $"Bearer {token}");
                });

                //请求body
                var body = new JObject
                {
                    ["proxyId"] = id
                };
                HttpResponse res = await HttpService.PostAsync(ApiUrl + "/auth/proxy/delete", 0, body, headersAction);
                //MessageBox.Show((string)res.HttpResponseContent);
                LogHelper.Write.Info($"删除隧道 ID: {id} 的请求已成功发送。");
                await GetTunnelList();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"删除隧道 ID: {id} 时发生异常: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "删除失败！" + ex.Message, "错误");
            }
        }

        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            var listBox = FrpList;
            if (listBox.SelectedItem is TunnelInfo selectedTunnel)
            {
                await DelTunnel(UserToken, selectedTunnel.ID);
            }
            else
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何隧道！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户登出ME Frp账户，清除本地Token。");
            //显示登录页面
            LoginGrid.Visibility = Visibility.Visible;
            MainCtrl.Visibility = Visibility.Collapsed;
            UserToken = null;
            Config.Remove("MEFrpToken");
        }

        //下面是创建隧道相关

        internal class NodeInfo
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Host { get; set; }
            public string Description { get; set; }
            public int Vip { get; set; }
            public string VipName { get; set; }
            public int Flag { get; set; }
            public string Band { get; set; }
        }

        private async Task GetNodeList()
        {
            try
            {
                LogHelper.Write.Info("正在获取ME Frp节点列表(用于创建隧道)。");
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/node/list", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });
                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
                    NodeList.ItemsSource = nodes;
                    JObject json = JObject.Parse((string)res.HttpResponseContent);

                    //遍历查询
                    foreach (var nodeProperty in (JArray)json["data"])
                    {
                        JObject nodeData = (JObject)nodeProperty;

                        nodes.Add(new NodeInfo
                        {
                            ID = int.Parse((string)nodeData["nodeId"]),
                            Name = (string)nodeData["name"],
                            Host = (string)nodeData["hostname"],
                            Description = nodeData["description"]?.ToString() ?? "",
                        });
                    }
                    LogHelper.Write.Info($"成功获取到 {nodes.Count} 个ME Frp节点。");
                }
                else
                {
                    LogHelper.Write.Error($"获取ME Frp节点列表失败，HTTP状态码: {res.HttpResponseCode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取ME Frp节点列表时发生异常: {ex.ToString()}");
            }
        }

        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = NodeList;
            if (listBox.SelectedItem is NodeInfo selectedNode)
            {
                NodeTips.Text = (selectedNode.Description == "" ? "节点没有备注" : selectedNode.Description);
            }
            Create_RemotePort.Text = Functions.GenerateRandomNumber(10000, 65535).ToString();
        }

        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                var listBox = NodeList;
                if (listBox.SelectedItem is NodeInfo selectedNode)
                {
                    LogHelper.Write.Info($"尝试在节点 '{selectedNode.Name}' 上创建隧道 '{Create_Name.Text}'，类型: {Create_Protocol.Text}...");
                    //请求头 token
                    var headersAction = new Action<HttpRequestHeaders>(headers =>
                    {
                        headers.Add("Authorization", $"Bearer {UserToken}");
                    });

                    //请求body
                    var body = new JObject
                    {
                        ["accessKey"] = "",
                        ["headerXFromWhere"] = "",
                        ["hostHeaderRewrite"] = "",
                        ["proxyProtocolVersion"] = "",
                        ["nodeId"] = selectedNode.ID,
                        ["proxyName"] = Create_Name.Text,
                        ["proxyType"] = Create_Protocol.Text,
                        ["localIp"] = Create_LocalIP.Text,
                        ["localPort"] = int.Parse(Create_LocalPort.Text),
                        ["remotePort"] = int.Parse(Create_RemotePort.Text),
                        ["domain"] = Create_BindDomain.Text,
                        ["useCompression"] = false,
                        ["useEncryption"] = false,
                    };
                    HttpResponse res = await HttpService.PostAsync(ApiUrl + "/auth/proxy/create", 0, body, headersAction);
                    if (res.HttpResponseCode == HttpStatusCode.OK)
                    {
                        JObject jsonres = JObject.Parse((string)res.HttpResponseContent);
                        LogHelper.Write.Info($"隧道 '{jsonres["name"]}' 创建成功。");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"{jsonres["name"]}隧道创建成功！\n远程端口: {Create_RemotePort.Text}", "成功");
                        MainCtrl.SelectedIndex = 0;
                    }
                    else
                    {
                        LogHelper.Write.Error($"创建隧道失败，HTTP状态码: {res.HttpResponseCode}，响应: {(string)res.HttpResponseContent}");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建失败！请尝试更换隧道名称/节点！", "错误");
                    }
                }
                else
                {
                    LogHelper.Write.Warn("用户点击创建隧道，但未选择任何节点。");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您似乎没有选择任何节点！", "错误");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"创建隧道时发生异常: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建失败！发生内部错误。", "错误");
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }
        private async void SignBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogHelper.Write.Info("用户正在尝试进行ME Frp每日签到。");
                Button btnSign = sender as Button;
                btnSign.IsEnabled = false;

                // 获取签到前的用户信息（包括流量数据）
                double beforeTraffic = await GetUserTraffic();

                // 开始签到请求
                Process.Start("https://www.mefrp.com/3rdparty/sign?client=MSL&&token=" + UserToken);

                // 开始轮询逻辑
                int pollCount = 0;
                const int maxPollCount = 15; // 最多轮询15次
                const int pollInterval = 6000; // 每次轮询间隔6秒
                bool signSuccess = false;

                while (pollCount < maxPollCount && !signSuccess)
                {
                    try
                    {
                        HttpResponse pollRes = await HttpService.GetAsync(ApiUrl + "/auth/user/sign", headers =>
                        {
                            headers.Add("Authorization", $"Bearer {UserToken}");
                        });

                        if (pollRes.HttpResponseCode == HttpStatusCode.OK)
                        {
                            JObject jsonres = JObject.Parse((string)pollRes.HttpResponseContent);
                            string message = jsonres["message"]?.ToString() ?? string.Empty;

                            if (message.Contains("今日已签到"))
                            {
                                LogHelper.Write.Info($"ME Frp轮询检测到签到成功，今日已签到。");
                                signSuccess = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write.Warn($"ME Frp签到轮询第{pollCount + 1}次失败，发生异常: {ex.Message}");
                    }

                    pollCount++;
                    if (!signSuccess && pollCount < maxPollCount)
                    {
                        await Task.Delay(pollInterval);
                    }
                }

                if (signSuccess)
                {
                    await VerifyUserToken(UserToken, false);
                    double afterTraffic = await GetUserTraffic();
                    double extraTraffic = afterTraffic - beforeTraffic;
                    if (extraTraffic > 0)
                    {
                        LogHelper.Write.Info($"ME Frp 每日签到成功，获得流量: {extraTraffic:F2} GB。");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), $"签到成功！\n获得流量: {extraTraffic:F2} GB", "签到成功");
                    }
                    else
                    {
                        LogHelper.Write.Info($"ME Frp 每日签到成功。");
                        await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "签到成功！", "签到成功");
                    }
                }
                else if (pollCount >= maxPollCount)
                {
                    LogHelper.Write.Warn($"ME Frp 签到轮询达到最大次数({maxPollCount}次)，签到可能失败");
                    await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "签到操作可能未成功完成，请稍后在网页端查看签到状态。", "提示");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"ME Frp 签到失败，发生异常: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "签到失败！", "错误");
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }

        /// <summary>
        /// 获取用户当前可用流量（GB）
        /// </summary>
        /// <returns>用户流量（GB）</returns>
        private async Task<double> GetUserTraffic()
        {
            try
            {
                // 请求用户信息
                HttpResponse res = await HttpService.GetAsync(ApiUrl + "/auth/user/info", headers =>
                {
                    headers.Add("Authorization", $"Bearer {UserToken}");
                });

                if (res.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject jsonUserInfo = JObject.Parse((string)res.HttpResponseContent);

                    // 获取流量信息（KB），转换为GB
                    if (jsonUserInfo["data"] != null && jsonUserInfo["data"]["traffic"] != null)
                    {
                        int trafficKB = int.Parse(jsonUserInfo["data"]["traffic"]?.ToString() ?? "0");
                        return trafficKB / 1024.0; // KB 转 GB
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取ME Frp用户流量信息时发生异常: {ex.Message}");
                return 0;
            }
        }
    }
}