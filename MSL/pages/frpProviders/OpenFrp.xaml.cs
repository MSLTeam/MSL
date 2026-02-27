using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Window = System.Windows.Window;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// OpenFrp.xaml 的交互逻辑
    /// </summary>
    public partial class OpenFrp : Page
    {
        private string token;
        private Dictionary<string, string> UserTunnelList;
        // private JArray ApiNodeJArray;
        // private Dictionary<string, string> ApiNodeList;

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
                LogHelper.Write.Info($"[OpenFrp] 页面加载。");
                //显示登录页面
                LoginGrid.Visibility = Visibility.Visible;
                MainCtrl.Visibility = Visibility.Collapsed;

                // 获取Token并尝试登录
                var authId = string.IsNullOrEmpty(OpenFrpApi.AuthId)
                    ? Config.Read("OpenFrpToken")?.ToString()
                    : OpenFrpApi.AuthId;

                if (string.IsNullOrEmpty(authId))
                {
                    LogHelper.Write.Info($"[OpenFrp] 未找到本地存储的Token。");
                    return;
                }
                if (authId != "")
                {
                    LogHelper.Write.Info($"[OpenFrp] 发现本地Token，尝试自动登录。");
                    await TokenLogin(authId);
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
                    LogHelper.Write.Info($"[OpenFrp] 切换到“我的隧道”页面。");
                    await GetUserTunnels();
                    break;
                case 1:
                    LogHelper.Write.Info($"[OpenFrp] 切换到“创建隧道”页面。");
                    await GetNodeList();
                    break;
            }
        }

        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            // 生成密钥对
            var keyGen = new X25519KeyPairGenerator();
            keyGen.Init(new X25519KeyGenerationParameters(new SecureRandom()));
            var keyPair = keyGen.GenerateKeyPair();

            // 获取公钥和私钥（Base64格式）
            var publicKeyBytes = keyPair.Public.GetEncoded();
            var privateKeyBytes = keyPair.Private.GetEncoded();
            string publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);
            publicKeyBase64 = publicKeyBase64.Trim().Replace('+', '-').Replace('/', '_');
            string privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

            // Console.WriteLine($"公钥: {publicKeyBase64}");

            var postData = new { public_key = publicKeyBase64 };
            UserLogin.IsEnabled = false;
            LogHelper.Write.Info($"[OpenFrp] 开始Argo Access扫码登录流程。");
            var response = await HttpService.PostAsync(
                "https://access.openfrp.net/argoAccess/requestLogin",
                contentType: 0,
                parameterData: postData
            );
            UserLogin.IsEnabled = true;

            if (response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                LogHelper.Write.Error($"[OpenFrp] Argo Access登录请求失败。状态码: {response.HttpResponseCode}, 内容: {response.HttpResponseContent}, 异常: {response.HttpResponseException}");
                if (string.IsNullOrEmpty(response.HttpResponseContent.ToString()))
                {
                    MagicShow.ShowMsgDialog("请求失败！请重试！" + (string.IsNullOrEmpty((string)response.HttpResponseException) ? string.Empty : $"\n{response.HttpResponseException}"), "错误");
                    return;
                }
                MagicShow.ShowMsgDialog(JObject.Parse(response.HttpResponseContent.ToString())["msg"] + "\n请重试！", "错误");
                return;
            }

            // 解析响应
            dynamic responseData = JsonConvert.DeserializeObject(response.HttpResponseContent.ToString());
            Process.Start(responseData.data.authorization_url.ToString());
            string requestUuid = responseData.data.request_uuid.ToString();
            LogHelper.Write.Info($"[OpenFrp] Argo Access登录请求成功，开始轮询授权结果。Request UUID: {requestUuid}");

            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog("请在打开的浏览器网页中确认授权……");
            var (PubKey, PollData) = await GetPublicKey(requestUuid);
            magicDialog.CloseTextDialog();

            if (PollData == null)
            {
                LogHelper.Write.Error($"[OpenFrp] 获取服务器公钥失败，轮询超时或出错。");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取公钥失败！", "错误");
                return;
            }
            else
            {
                if (PollData["code"].ToString() != "200")
                {
                    LogHelper.Write.Error($"[OpenFrp] 获取服务器公钥失败，API返回错误。消息: {PollData["msg"]}");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取公钥失败！" + PollData["msg"].ToString(), "错误");
                    return;
                }
            }

            try
            {
                // 处理 base64 URL 安全格式
                PubKey = PubKey.Trim().Replace('-', '+').Replace('_', '/');
                switch (PubKey.Length % 4)
                {
                    case 2: PubKey += "=="; break;
                    case 3: PubKey += "="; break;
                }
                // Console.WriteLine($"处理后的服务器公钥: {PubKey}");

                // 获取服务器公钥
                var serverPublicKeyBytes = Convert.FromBase64String(PubKey);

                // 获取加密数据
                var encryptedDataBase64 = PollData["data"]["authorization_data"].ToString();
                var encryptedData = Convert.FromBase64String(encryptedDataBase64);

                // 标准 NaCl Box 格式：前24字节是 nonce
                const int NONCE_SIZE = 24; // NaCl box 使用的标准 nonce 大小
                byte[] nonce = new byte[NONCE_SIZE];
                Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);

                // 提取密文部分 (去掉nonce后的部分)
                byte[] cipherText = new byte[encryptedData.Length - nonce.Length];
                Buffer.BlockCopy(encryptedData, nonce.Length, cipherText, 0, cipherText.Length);

                /*
                Console.WriteLine($"Nonce 长度: {nonce.Length}");
                Console.WriteLine($"密文长度: {cipherText.Length}");

                // 确保密钥长度正确
                Console.WriteLine($"公钥长度: {publicKeyBytes.Length}");
                Console.WriteLine($"私钥长度: {privateKeyBytes.Length}");
                Console.WriteLine($"服务器公钥长度: {serverPublicKeyBytes.Length}");
                */

                try
                {
                    byte[] decryptedBytes = PublicKeyBoxCompat.Open(
                        cipherText,
                        nonce,
                        privateKeyBytes,
                        serverPublicKeyBytes
                    );

                    string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
                    LogHelper.Write.Info($"[OpenFrp] 成功解密获取到Authorization。");
                    MagicFlowMsg.ShowMessage($"成功解密： {decryptedText.Substring(0, 5)}***{decryptedText.Substring(decryptedText.Length - 6, 5)}");
                    await TokenLogin(decryptedText);
                    return;
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"[OpenFrp] 解密Authorization失败: {ex.ToString()}");
                    MagicFlowMsg.ShowMessage($"解密失败: {ex.Message}", 2);
                    // Console.WriteLine($"解密失败: {ex.Message}");
                }

                MagicShow.ShowMsgDialog("登陆失败！", "err");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"[OpenFrp] Argo Access登录解密过程中出现未知错误: {ex.ToString()}");
                MagicShow.ShowMsgDialog($"解密过程中出错: {ex.Message}\n{ex.StackTrace}", "err");
            }
        }

        private async Task<(string PubKey, JObject PollData)> GetPublicKey(string requestUuid)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{ConfigStore.MSLVersion}");
            HttpResponse httpResponse = new HttpResponse();
            JObject pollData = null;
            string serverPublicKeyBase64 = null;
            int i = 0;
            while (httpResponse.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                if (i >= 60)
                {
                    LogHelper.Write.Warn($"[OpenFrp] 轮询授权结果超时。Request UUID: {requestUuid}");
                    break;
                }
                i++;
                await Task.Delay(5000);

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync($"https://access.openfrp.net/argoAccess/pollLogin?request_uuid={requestUuid}");
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        continue;
                    httpResponse.HttpResponseCode = response.StatusCode;
                    httpResponse.HttpResponseContent = response.IsSuccessStatusCode
                        ? await response.Content.ReadAsStringAsync()
                        : response.ReasonPhrase;

                    // 从 Headers 获取服务器公钥
                    if (response.Headers.TryGetValues("x-request-public-key", out var values))
                    {
                        serverPublicKeyBase64 = values.First();
                    }
                    pollData = JObject.Parse(httpResponse.HttpResponseContent.ToString());
                }
                catch (Exception ex)
                {
                    httpResponse.HttpResponseCode = 0;
                    httpResponse.HttpResponseContent = ex.Message;
                    LogHelper.Write.Error($"[OpenFrp] 轮询授权结果时发生HTTP请求异常: {ex.ToString()}");
                    break;
                }
            }
            httpClient.Dispose();
            return (serverPublicKeyBase64, pollData);
        }

        private async void UserTokenLogin_Click(object sender, RoutedEventArgs e)
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

            LogHelper.Write.Info($"[OpenFrp] 开始使用Authorization进行登录。");
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg) = await OpenFrpApi.Login(token, SaveToken.IsChecked == true);
            MagicDialog.CloseTextDialog();
            if (Code == 200)
            {
                LogHelper.Write.Info($"[OpenFrp] Authorization登录成功。");
                LoginGrid.Visibility = Visibility.Collapsed;
                MainCtrl.Visibility = Visibility.Visible;
                GetUserInfo(JObject.Parse(Msg));
            }
            else
            {
                LogHelper.Write.Error($"[OpenFrp] Authorization登录失败。消息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的Authorization是否正确！" + Msg, "错误！");
                if (Config.Read("OpenFrpToken") != null)
                    Config.Remove("OpenFrpToken");
                return;
            }
            return;
        }

        private async void GetUserInfo(JObject userdata)
        {
            string username = userdata["data"]["username"].ToString();
            LogHelper.Write.Info($"[OpenFrp] 获取用户信息成功。用户名: {username}");
            string showusrinfo = $"用户名：{username}[{userdata["data"]["friendlyGroup"]}]\n" +
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
            TunnelList.Items.Clear();
            LogHelper.Write.Info($"[OpenFrp] 开始获取用户隧道列表。");
            var (Code, Data, Msg) = await OpenFrpApi.GetUserNodes();
            if (Code == 200)
            {
                LogHelper.Write.Info($"[OpenFrp] 获取用户隧道列表成功，共 {Data.Count} 条隧道。");
                if (Data.Count != 0)
                {
                    UserTunnelList = Data;
                    foreach (KeyValuePair<string, string> node in UserTunnelList)
                    {
                        TunnelList.Items.Add(node.Key);
                    }
                }
            }
            else
            {
                LogHelper.Write.Error($"[OpenFrp] 获取用户隧道列表失败。消息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取失败！" + Msg, "错误！");
            }
        }

        private async Task GetNodeList()
        {
            LogHelper.Write.Info($"[OpenFrp] 开始获取节点列表。");
            var (Flag, NodeInfos) = await OpenFrpApi.GetNodeList();
            if (!Flag)
            {
                LogHelper.Write.Error($"[OpenFrp] 获取节点列表失败。");
                MagicShow.ShowMsgDialog("获取节点列表失败！", "ERR");
                return;
            }
            LogHelper.Write.Info($"[OpenFrp] 获取节点列表成功。");
            NodeList.ItemsSource = NodeInfos;
            RandomPort(false);
        }

        private void NodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NodeList.SelectedIndex == -1)
            {
                return;
            }
            var SelectItem = NodeList.SelectedItem as OpenFrpApi.NodeInfo;
            List<string> ProtocolList = new List<string>();
            foreach (var protocol in SelectItem.Protocol)
            {
                if ((bool)protocol.Value)
                {
                    ProtocolList.Add(protocol.Key);
                }
            }
            frpcType.ItemsSource = ProtocolList;
            frpcType.SelectedIndex = 0;
            /*
            NodeTips.Text = $"节点ID：{SelectItem.ID}\n" +
                $"节点名称：{SelectItem.Name}\n" +
                $"节点状态：{SelectItem.Status}\n" +
                $"节点标签：{SelectItem.Tags}\n" +
                $"节点备注：{SelectItem.Remark}\n" +
                $"节点带宽：{SelectItem.Band}\n";
            */
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TunnelList.SelectedIndex == -1)
            {
                MagicShow.ShowMsgDialog("请确保您选择了一个隧道", "错误");
                return;
            }

            object o = TunnelList.SelectedValue;
            string id = UserTunnelList[o.ToString()];
            LogHelper.Write.Info($"[OpenFrp] 准备启动映射，选择的隧道: {o}, ID: {id}");
            Config.WriteFrpcConfig(1, $"OpenFrp节点 - {o}", $"-u {token} -p {id}", "");
            LogHelper.Write.Info($"[OpenFrp] 映射配置写入成功。");
            await MagicShow.ShowMsgDialogAsync("映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            Window.GetWindow(this).Close();
        }

        private async void AddProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NodeList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog("请先选择一个节点", "错误");
                    return;
                }
                string type;
                if (frpcType.SelectedIndex == 0) type = "tcp";
                else type = "udp";
                bool zip;
                if ((bool)enableCompression.IsChecked) zip = true;
                else zip = false;
                OpenFrpApi.NodeInfo selected_node = NodeList.SelectedItem as OpenFrpApi.NodeInfo;
                int selected_node_id;
                if (selected_node != null) selected_node_id = selected_node.ID;
                else
                {
                    addProxieBtn.IsEnabled = true;
                    MagicShow.ShowMsgDialog("请先选择一个节点", "错误");
                    return;
                }
                string proxy_name = await MagicShow.ShowInput(Window.GetWindow(this), "给隧道取个名称吧（不支持中文）");
                if (proxy_name != null)
                {
                    LogHelper.Write.Info($"[OpenFrp] 开始创建隧道。名称: {proxy_name}, 节点ID: {selected_node_id}, 类型: {type}, 本地端口: {portBox.Text}, 远程端口: {remotePortBox.Text}, 压缩: {zip}");
                    addProxieBtn.IsEnabled = false;
                    var (_return, msg) = await OpenFrpApi.CreateProxy(type, portBox.Text, zip, selected_node_id, remotePortBox.Text, proxy_name);
                    addProxieBtn.IsEnabled = true;
                    if (_return)
                    {
                        LogHelper.Write.Info($"[OpenFrp] 隧道创建成功。");
                        MainCtrl.SelectedIndex = 0;
                        MagicShow.ShowMsgDialog("隧道创建成功！", "提示");
                    }
                    else
                    {
                        LogHelper.Write.Error($"[OpenFrp] 隧道创建失败。消息: {msg}");
                        MagicShow.ShowMsgDialog("创建失败！" + msg, "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"[OpenFrp] 创建隧道时发生未知错误: {ex.ToString()}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误！" + ex.Message, "错误");
            }
            addProxieBtn.IsEnabled = true;
        }

        private void RefreshTunnelList_Click(object sender, RoutedEventArgs e)
        {
            _ = GetUserTunnels();
        }

        private async void DelProxieBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TunnelList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog("请先选择一个隧道", "错误");
                    return;
                }
                object o = TunnelList.SelectedValue;
                string id = UserTunnelList[o.ToString()];
                LogHelper.Write.Info($"[OpenFrp] 准备删除隧道。名称: {o}, ID: {id}");
                delProxieBtn.IsEnabled = false;
                var (_return, msg) = await OpenFrpApi.DeleteProxy(id);
                if (_return)
                {
                    LogHelper.Write.Info($"[OpenFrp] 删除隧道成功。");
                    MagicShow.ShowMsgDialog("删除成功！", "提示");
                }
                else
                {
                    LogHelper.Write.Error($"[OpenFrp] 删除隧道失败。消息: {msg}");
                    MagicShow.ShowMsgDialog("删除失败！" + msg, "错误");
                }
                await GetUserTunnels();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"[OpenFrp] 删除隧道时发生未知错误: {ex.ToString()}");
                MagicShow.ShowMsgDialog("出现错误！" + ex.Message, "错误");
            }
            finally
            {
                delProxieBtn.IsEnabled = true;
            }
        }

        private void RandomRemotePortBtn_Click(object sender, RoutedEventArgs e)
        {
            RandomPort();
        }

        private void RandomPort(bool tip = true)
        {
            if (NodeList.SelectedIndex == -1)
            {
                if (tip)
                    MagicShow.ShowMsgDialog("请先选择一个节点", "错误");
                return;
            }
            OpenFrpApi.NodeInfo selected_node = NodeList.SelectedItem as OpenFrpApi.NodeInfo;
            Random random = new Random();
            string remote_port;
            remote_port = random.Next(selected_node.AllowPorts.Item1, selected_node.AllowPorts.Item2).ToString();
            remotePortBox.Text = remote_port;
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info($"[OpenFrp] 用户登出。");
            OpenFrpApi.AuthId = string.Empty;
            Config.Remove("OpenFrpToken");
            LoginGrid.Visibility = Visibility.Visible;
            MainCtrl.Visibility = Visibility.Collapsed;
            userInfo.Content = string.Empty;
        }
    }
}