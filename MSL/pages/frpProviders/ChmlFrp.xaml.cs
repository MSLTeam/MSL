using MSL.langs;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace MSL.pages.frpProviders
{
    /// <summary>
    /// ChmlFrp.xaml 的交互逻辑
    /// </summary>
    public partial class ChmlFrp : Page
    {
        private readonly string ChmlFrpApiUrl = "https://cf-v2.uapis.cn";
        private readonly string OAuthIssuer = "https://account-api.qzhua.net";
        private readonly string OAuthClientId = "019d5ca3e01c75a3919eb0022069e9c1"; // 使用MSLX的ID
        private readonly string OAuthScope = "profile email offline_access chmlfrp_api"; // 希腊奶
        private string ChmlToken = string.Empty;   // usertoken（frpc 鉴权用）
        private string AccessToken = string.Empty; // OAuth access_token（API 调用鉴权用）
        private string ChmlID = string.Empty;

        private CancellationTokenSource _pollCts;

        private static readonly HttpClient _oauthClient = new HttpClient();

        public Action _onReturn;

        public ChmlFrp(Action onReturn)
        {
            InitializeComponent();
            _onReturn = onReturn;
        }

        // ──────────────────────────────────────────────
        // 页面生命周期
        // ──────────────────────────────────────────────

        private bool isInit = false;
        private async void Page_Loaded(object sender, EventArgs e)
        {
            if (!isInit)
            {
                isInit = true;
                ShowLoginPage();

                // 尝试用已保存的 access_token 自动登录
                var savedToken = Config.Read("ChmlAccessToken")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(savedToken))
                {
                    LogHelper.Write.Info("检测到已保存的 ChmlFrp AccessToken，尝试自动登录。");
                    MagicDialog dlg = new MagicDialog();
                    dlg.ShowTextDialog(Window.GetWindow(this), "登录中……");
                    await LoginWithAccessToken(savedToken, save: false);
                    dlg.CloseTextDialog();
                }
            }
        }

        private async void MainCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (!ReferenceEquals(e.OriginalSource, this.MainCtrl)) return;

            switch (MainCtrl.SelectedIndex)
            {
                case 0:
                    await GetFrpList();
                    break;
                case 1:
                    await GetNodeList();
                    Random rand = new Random();
                    Create_RemotePort.Text = rand.Next(10000, 65536).ToString();
                    Create_Name.Text = Functions.RandomString("MSL_", 5);
                    break;
            }
        }

        // ──────────────────────────────────────────────
        // UI 辅助
        // ──────────────────────────────────────────────

        private void ShowLoginPage()
        {
            MainCtrl.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            // 重置 OAuth 授权界面状态
            OAuthStatusText.Text = "";
        }

        private void ShowMainPage()
        {
            MainCtrl.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Collapsed;
        }

        // ──────────────────────────────────────────────
        // OAuth Device Flow Login
        // ──────────────────────────────────────────────

        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户选择使用 OAuth Device Flow 登录 ChmlFrp。");
            await StartDeviceFlow();
        }

        private async Task StartDeviceFlow()
        {
            // 取消上一次未完成的轮询
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();

            OAuthStatusText.Text = "正在获取授权码……";
            OAuthErrorText.Text = "";
            OAuthLoginBtn.IsEnabled = false;

            try
            {
                // Step 1: 请求设备码
                var deviceResp = await RequestDeviceAuthorization(_pollCts.Token);

                DeviceCodeText.Text = deviceResp.UserCode;
                OAuthStatusText.Text = "请在浏览器中完成授权……";

                // Step 2: 打开授权页
                string target = deviceResp.VerificationUriComplete ?? deviceResp.VerificationUri;
                if (!string.IsNullOrEmpty(target))
                {
                    try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
                    catch { /* 打开失败不影响轮询 */ }
                }

                // Step 3: 轮询 token
                int intervalSeconds = Math.Max(deviceResp.Interval, 5);
                await PollForToken(deviceResp.DeviceCode, intervalSeconds, _pollCts.Token);
            }
            catch (OperationCanceledException)
            {
                OAuthStatusText.Text = "授权已取消。";
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"OAuth Device Flow 失败: {ex}");
                OAuthErrorText.Text = ex.Message;
                OAuthStatusText.Text = "";
            }
            finally
            {
                OAuthLoginBtn.IsEnabled = true;
            }
        }

        private async Task PollForToken(string deviceCode, int intervalSeconds, CancellationToken ct)
        {
            bool save = SaveToken.IsChecked == true;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(intervalSeconds * 1000, ct);

                var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["device_code"] = deviceCode,
                    ["client_id"] = OAuthClientId
                });

                string raw;
                try
                {
                    var resp = await _oauthClient.PostAsync($"{OAuthIssuer}/oauth2/token", body, ct);
                    raw = await resp.Content.ReadAsStringAsync();
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogHelper.Write.Warn($"轮询 token 请求异常: {ex.Message}");
                    continue;
                }

                Dictionary<string, object> json;
                try { json = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw); }
                catch { continue; }

                if (json == null) continue;

                if (json.TryGetValue("access_token", out var at) && at != null)
                {
                    string accessToken = at.ToString();
                    LogHelper.Write.Info("OAuth Device Flow 获取 access_token 成功。");
                    OAuthStatusText.Text = "授权成功，正在加载数据……";
                    await LoginWithAccessToken(accessToken, save);
                    return;
                }

                if (json.TryGetValue("error", out var errVal))
                {
                    string err = errVal.ToString();
                    switch (err)
                    {
                        case "authorization_pending":
                            OAuthStatusText.Text = "等待用户在浏览器中确认授权……";
                            break;
                        case "slow_down":
                            intervalSeconds += 5;
                            OAuthStatusText.Text = "请求过于频繁，已自动降低频率……";
                            break;
                        case "expired_token":
                            OAuthErrorText.Text = "授权码已过期，请重新开始授权。";
                            return;
                        case "access_denied":
                            OAuthErrorText.Text = "用户已拒绝授权。";
                            return;
                        default:
                            string desc = json.TryGetValue("error_description", out var d) ? d.ToString() : err;
                            OAuthErrorText.Text = $"授权失败：{desc}";
                            return;
                    }
                }
            }
        }

        // ──────────────────────────────────────────────
        // 通用：用 access_token 完成登录
        // ──────────────────────────────────────────────

        private async Task LoginWithAccessToken(string accessToken, bool save)
        {
            try
            {
                string response = (await HttpService.GetAsync(
                    $"{ChmlFrpApiUrl}/userinfo?access_token={accessToken}")).HttpResponseContent.ToString();

                LogHelper.Write.Info($"userinfo 响应: {response}");

                if (string.IsNullOrWhiteSpace(response))
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this),
                        "登录失败！\n服务器返回内容为空。", LanguageManager.Instance["Error"]);
                    return;
                }

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                if (jsonResponse == null)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this),
                        "登录失败！\n服务器返回了无效数据。", LanguageManager.Instance["Error"]);
                    return;
                }

                if (jsonResponse.TryGetValue("state", out var sv) && sv.ToString() == "success"
                    && jsonResponse.TryGetValue("data", out var dv))
                {
                    var data = JObject.FromObject(dv);
                    AccessToken = accessToken;
                    ChmlToken = data["usertoken"]?.ToString() ?? accessToken; // frpc 用 usertoken
                    ChmlID = data["id"]?.ToString() ?? "";

                    if (save)
                    {
                        Config.Write("ChmlAccessToken", accessToken);
                        LogHelper.Write.Info("AccessToken 已保存。");
                    }

                    ShowMainPage();
                    await GetFrpList(response);
                    return;
                }

                string errMsg = jsonResponse.TryGetValue("msg", out var m) ? m.ToString() : "未知错误";
                LogHelper.Write.Warn($"ChmlFrp 登录失败: {errMsg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this),
                    "登录失败！\n" + errMsg, LanguageManager.Instance["Error"]);

                if (Config.Read("ChmlAccessToken") != null)
                    Config.Remove("ChmlAccessToken");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"LoginWithAccessToken 异常: {ex}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this),
                    "登录失败！\n" + ex.Message, LanguageManager.Instance["Error"]);
                if (Config.Read("ChmlAccessToken") != null)
                    Config.Remove("ChmlAccessToken");
            }
        }

        // ──────────────────────────────────────────────
        // OAuth 工具：请求设备码
        // ──────────────────────────────────────────────

        private class DeviceAuthResponse
        {
            public string DeviceCode { get; set; }
            public string UserCode { get; set; }
            public string VerificationUri { get; set; }
            public string VerificationUriComplete { get; set; }
            public int ExpiresIn { get; set; }
            public int Interval { get; set; }
        }

        private async Task<DeviceAuthResponse> RequestDeviceAuthorization(CancellationToken ct)
        {
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = OAuthClientId,
                ["scope"] = OAuthScope
            });

            var httpResp = await _oauthClient.PostAsync(
                $"{OAuthIssuer}/oauth2/device_authorization", body, ct);
            string raw = await httpResp.Content.ReadAsStringAsync();

            LogHelper.Write.Info($"device_authorization 响应: {raw}");

            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw);
            if (json == null || !json.ContainsKey("device_code"))
                throw new Exception("账户中心返回了无效的授权响应，请稍后重试。");

            return new DeviceAuthResponse
            {
                DeviceCode = json["device_code"]?.ToString(),
                UserCode = json.TryGetValue("user_code", out var uc) ? uc?.ToString() : "",
                VerificationUri = json.TryGetValue("verification_uri", out var vu) ? vu?.ToString() : "",
                VerificationUriComplete = json.TryGetValue("verification_uri_complete", out var vuc) ? vuc?.ToString() : null,
                ExpiresIn = json.TryGetValue("expires_in", out var ei) && int.TryParse(ei?.ToString(), out int e2) ? e2 : 300,
                Interval = json.TryGetValue("interval", out var iv) && int.TryParse(iv?.ToString(), out int i2) ? i2 : 5,
            };
        }

        // ──────────────────────────────────────────────
        // 数据模型
        // ──────────────────────────────────────────────

        internal class TunnelInfo
        {
            public string ID { get; set; }
            public string Type { get; set; }
            public string LPort { get; set; }
            public string RPort { get; set; }
            public string LIP { get; set; }
            public string Name { get; set; }
            public string Node { get; set; }
            // 隧道列表直接含 ip / server_port / node_token，无需再调 /nodeinfo
            public string NodeIp { get; set; }
            public string ServerPort { get; set; }
            public string NodeToken { get; set; }
            public string Encryption { get; set; }
            public string Compression { get; set; }
        }

        public class NodeInfo
        {
            public string Area { get; set; }
            public string Name { get; set; }
            public string Notes { get; set; }
            public string NodeGroup { get; set; }
            public string NodeGroupName { get; set; }
        }

        // ──────────────────────────────────────────────
        // 获取用户信息 + 隧道列表
        // ──────────────────────────────────────────────

        private async Task GetFrpList(string uResponse="")
        {
            try
            {
                LogHelper.Write.Info("开始获取 ChmlFrp 隧道列表及用户信息。");
                if (uResponse == "")
                {
                    uResponse = (await HttpService.GetAsync(
                        $"{ChmlFrpApiUrl}/userinfo?access_token={AccessToken}")).HttpResponseContent.ToString();
                }

                var userInfoRaw = JsonConvert.DeserializeObject<Dictionary<string, object>>(uResponse);

                var userInfo = JObject.FromObject(userInfoRaw["data"]);
                UserInfo.Text = $"用户：#{userInfo["id"]} {userInfo["username"]}\n" +
                                $"邮箱：{userInfo["email"]}\n" +
                                $"会员类型：{userInfo["usergroup"]}\n" +
                                $"隧道数：{userInfo["tunnnelCount"]}/{userInfo["tunnel"]}";
                LogHelper.Write.Info($"成功获取用户信息: {userInfo["username"]}");

                ObservableCollection<TunnelInfo> tunnels = new ObservableCollection<TunnelInfo>();
                FrpList.ItemsSource = tunnels;

                string response = (await HttpService.GetAsync(
                    $"{ChmlFrpApiUrl}/tunnel?access_token={AccessToken}")).HttpResponseContent.ToString();

                try
                {
                    var tunnelRoot = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                    if (tunnelRoot.TryGetValue("state", out var ts) && ts.ToString() == "success"
                        && tunnelRoot.TryGetValue("data", out var dataVal))
                    {
                        foreach (JObject item in JArray.FromObject(dataVal))
                        {
                            tunnels.Add(new TunnelInfo
                            {
                                Name = $"{item["name"]}",
                                Node = $"{item["node"]}",
                                ID = $"{item["id"]}",
                                Type = $"{item["type"]}",
                                LIP = $"{item["localip"]}",
                                LPort = $"{item["nport"]}",
                                RPort = $"{item["dorp"]}",
                                NodeIp = item["ip"]?.Type != JTokenType.Null ? $"{item["ip"]}" : "",
                                ServerPort = item["server_port"]?.Type != JTokenType.Null ? $"{item["server_port"]}" : "7000",
                                NodeToken = item["node_token"]?.Type != JTokenType.Null ? $"{item["node_token"]}" : "",
                                Compression = $"{item["compression"]}",
                                Encryption = $"{item["encryption"]}"
                            });
                        }
                        LogHelper.Write.Info($"成功获取并加载了 {tunnels.Count} 条隧道。");
                    }
                    else
                    {
                        LogHelper.Write.Warn("未能解析隧道列表，可能是因为该账户下没有隧道。");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"解析隧道列表时发生异常: {ex}");
                    MagicShow.ShowMsgDialog(Functions.GetWindow(this), ex.Message, "错误");
                }
            }
            catch (Exception e)
            {
                LogHelper.Write.Error($"获取 ChmlFrp 列表时发生严重错误: {e}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), e.Message, "出错了！");
            }
        }

        private void FrpList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is TunnelInfo t)
            {
                TunnelInfo_Text.Text = $"隧道名:{t.Name}\n隧道ID:{t.ID}\n协议:{t.Type}\n地域:{t.Node}\n远程端口:{t.RPort}";
                LocalIp.Text = t.LIP;
                LocalPort.Text = t.LPort;
            }
        }

        // ──────────────────────────────────────────────
        // 使用隧道：调 /tunnel-config 拿配置文本
        // ──────────────────────────────────────────────

        private async void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(FrpList.SelectedItem is TunnelInfo selectedTunnel))
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "请您选择一个隧道再按确定哦~", "隧道呢？");
                return;
            }

            try
            {
                LogHelper.Write.Info($"准备获取隧道 {selectedTunnel.Name} ({selectedTunnel.ID}) 的配置文件。");
                (sender as Button).IsEnabled = false;

                string configResponse = (await HttpService.GetAsync(
                    $"{ChmlFrpApiUrl}/tunnel_config" +
                    $"?node={Uri.EscapeDataString(selectedTunnel.Node)}" +
                    $"&tunnelName={Uri.EscapeDataString(selectedTunnel.Name)}" +
                    $"&token={AccessToken}")).HttpResponseContent.ToString();

                string frpcConfig = configResponse;
                try
                {
                    var configRoot = JsonConvert.DeserializeObject<Dictionary<string, object>>(configResponse);
                    if (configRoot != null && configRoot.TryGetValue("data", out var dataVal))
                        frpcConfig = dataVal?.ToString() ?? configResponse;
                }
                catch { /* 直接是 ini 文本 */ }

                if (string.IsNullOrWhiteSpace(frpcConfig))
                    throw new Exception("服务器返回的配置内容为空");

                Config.WriteFrpcConfig(2, $"ChmlFrp - {selectedTunnel.Name}({selectedTunnel.Node})", frpcConfig, "");
                LogHelper.Write.Info("frpc 配置文件获取并写入成功。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "映射配置成功，请您点击 启动内网映射 以启动映射！", "信息");
                _onReturn.Invoke();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取/写入配置失败: {ex}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "获取配置失败！\n" + ex.Message, "出错");
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }

        // ──────────────────────────────────────────────
        // 删除隧道：CHML 文档说还在开发中，故不使用
        // ──────────────────────────────────────────────

        private async void Del_Tunnel_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            bool confirm = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "确定删除所选隧道吗？", "删除隧道", true);
            if (confirm)
            {
                try
                {
                    if (!(FrpList.SelectedItem is TunnelInfo selectedTunnel))
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "请选择一个隧道再操作！", "失败！");
                    }
                    else
                    {
                        LogHelper.Write.Info($"用户请求删除隧道 ID: {selectedTunnel.ID}。");

                        string res = (await HttpService.GetAsync(
                            $"{ChmlFrpApiUrl}/delete_tunnel?tunnelId={selectedTunnel.ID}&token={AccessToken}")).HttpResponseContent.ToString();

                        var postResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(res);
                        bool success = postResponse.TryGetValue("state", out var sv) && sv.ToString() == "success";
                        if (!success && postResponse.TryGetValue("code", out var cv))
                            success = cv.ToString() == "200";

                        if (success)
                        {
                            LogHelper.Write.Info($"隧道 ID: {selectedTunnel.ID} 删除成功。");
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道删除成功！", "删除");
                            _ = GetFrpList();
                        }
                        else
                        {
                            string errMsg = postResponse.TryGetValue("msg", out var m) ? m.ToString()
                                          : postResponse.TryGetValue("error", out var em) ? em.ToString()
                                          : "未知错误";
                            LogHelper.Write.Warn($"隧道删除失败: {errMsg}");
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), $"隧道删除失败！\n{errMsg}", "失败！");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"删除隧道时发生异常: {ex}");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), ex.Message, "失败！");
                }
            }
            (sender as Button).IsEnabled = true;
        }

        // ──────────────────────────────────────────────
        // 获取节点列表
        // ──────────────────────────────────────────────

        private async Task GetNodeList()
        {
            ObservableCollection<NodeInfo> nodes = new ObservableCollection<NodeInfo>();
            NodeList.ItemsSource = nodes;
            LogHelper.Write.Info("开始获取 ChmlFrp 节点列表。");

            var _response = await HttpService.GetAsync($"{ChmlFrpApiUrl}/node");
            if (_response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                LogHelper.Write.Error($"获取节点列表 API 请求失败，状态码: {_response.HttpResponseCode}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "节点列表获取失败！\n" + _response.HttpResponseContent, "获取失败！");
                return;
            }

            try
            {
                var nodeRoot = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    _response.HttpResponseContent.ToString());

                if (nodeRoot.TryGetValue("state", out var ns) && ns.ToString() == "success"
                    && nodeRoot.TryGetValue("data", out var dataVal))
                {
                    foreach (JObject item in JArray.FromObject(dataVal))
                    {
                        nodes.Add(new NodeInfo
                        {
                            Name = $"{item["name"]}",
                            Area = $"{item["area"]}",
                            Notes = $"{item["notes"]}",
                            NodeGroup = $"{item["nodegroup"]}",
                            NodeGroupName = item["nodegroup"].ToString() == "vip" ? "VIP节点" : "普通节点"
                        });
                    }
                    LogHelper.Write.Info($"成功获取并加载了 {nodes.Count} 个节点。");
                }
                else
                {
                    string errMsg = nodeRoot.TryGetValue("msg", out var m) ? m.ToString() : "未知错误";
                    LogHelper.Write.Warn($"获取节点列表失败: {errMsg}");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "无法加载节点信息！\n" + errMsg, "错误");
                }
            }
            catch (JsonSerializationException ex)
            {
                LogHelper.Write.Error($"解析节点列表时发生 JSON 反序列化错误: {ex}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "无法加载节点信息！", "错误");
            }
        }

        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is NodeInfo selectedNode)
                NodeTips.Text = selectedNode.Notes;
        }

        // ──────────────────────────────────────────────
        // 创建隧道
        // ──────────────────────────────────────────────

        private async void Create_OKBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            if (NodeList.SelectedItem is NodeInfo selectedNode)
            {
                LogHelper.Write.Info($"用户请求在节点 {selectedNode.Name} 上创建新隧道: {Create_Name.Text}");
                await PostCreate(
                    Create_LocalIP.Text, Create_Name.Text, selectedNode.Name,
                    Create_Protocol.Text, Create_LocalPort.Text, Create_RemotePort.Text,
                    Create_Encryption.IsChecked == true, Create_Compression.IsChecked == true);
            }
            else
            {
                LogHelper.Write.Warn("用户尝试创建隧道，但未选择任何节点。");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "您似乎没有选择节点！", "错误");
            }
            (sender as Button).IsEnabled = true;
        }

        private async Task PostCreate(string localip, string name, string node, string type,
            string nport, string dorp, bool encryption, bool compression)
        {
            var body = new JObject
            {
                ["token"] = AccessToken,
                ["tunnelname"] = name,
                ["node"] = node,
                ["porttype"] = type,
                ["localip"] = localip,
                ["localport"] = int.TryParse(nport, out int lp) ? lp : 0,
                ["encryption"] = encryption,
                ["compression"] = compression,
                ["extraparams"] = string.Empty
            };

            if (type == "http" || type == "https")
                body["banddomain"] = dorp;
            else
                body["remoteport"] = int.TryParse(dorp, out int rp) ? rp : 0;

            LogHelper.Write.Info($"向 API 发送创建隧道请求。名称: {name}, 节点: {node}, 类型: {type}");
            var _response = await HttpService.PostAsync($"{ChmlFrpApiUrl}/create_tunnel", 0, body);

            if (_response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                LogHelper.Write.Error($"创建隧道 API 请求失败，状态码: {_response.HttpResponseCode}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建失败！\n" + _response.HttpResponseContent, "创建失败！");
                return;
            }

            var postResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                _response.HttpResponseContent.ToString());

            if (postResponse.TryGetValue("state", out var sv) && sv.ToString() == "success")
            {
                LogHelper.Write.Info($"隧道 {name} 创建成功。");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "隧道创建成功！", "创建成功！");
                MainCtrl.SelectedIndex = 0;
            }
            else
            {
                string errMsg = postResponse.TryGetValue("msg", out var m) ? m.ToString() : "未知错误";
                LogHelper.Write.Warn($"隧道创建失败，API返回信息: {errMsg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), $"隧道创建失败！\n{errMsg}", "创建失败！");
            }
        }

        // ──────────────────────────────────────────────
        // 其他操作
        // ──────────────────────────────────────────────

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户点击刷新隧道列表。");
            (sender as Button).IsEnabled = false;
            await GetFrpList();
            (sender as Button).IsEnabled = true;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户退出 ChmlFrp 登录。");
            _pollCts?.Cancel();
            AccessToken = null;
            ChmlToken = null;
            ChmlID = null;
            Config.Remove("ChmlAccessToken");
            ShowLoginPage();
        }
    }
}