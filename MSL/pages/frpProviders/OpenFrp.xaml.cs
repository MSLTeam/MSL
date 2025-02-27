using MSL.utils;
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
        private JArray ApiNodeJArray;
        private Dictionary<string, string> ApiNodeList;

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
                MainCtrl.Visibility = Visibility.Collapsed;

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
                    await GetUserTunnels();
                    break;
                case 1:
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
            var publicKeyBytes = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
            var privateKeyBytes = ((X25519PrivateKeyParameters)keyPair.Private).GetEncoded();
            string publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);
            string privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

            var postData = new { public_key = publicKeyBase64 };
            UserLogin.IsEnabled = false;
            var response = await HttpService.PostAsync(
                "https://access.openfrp.net/argoAccess/requestLogin",
                contentType: 0,
                parameterData: postData
            );
            UserLogin.IsEnabled = true;

            if (response.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                MagicShow.ShowMsgDialog(response.HttpResponseContent.ToString() + "\n请重试！", "错误");
                return;
            }

            // 解析响应
            dynamic responseData = JsonConvert.DeserializeObject(response.HttpResponseContent.ToString());
            Process.Start(responseData.data.authorization_url.ToString());
            string requestUuid = responseData.data.request_uuid.ToString();

            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog("请在打开的浏览器网页中确认授权……");
            var (PubKey, PollData) = await GetPublicKey(requestUuid);
            magicDialog.CloseTextDialog();

            if (PollData == null)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取公钥失败！", "错误");
                return;
            }
            else
            {
                if (PollData["code"].ToString() != "200")
                {
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
                    MagicFlowMsg.ShowMessage($"成功解密： {decryptedText.Substring(0, 5)}***{decryptedText.Substring(decryptedText.Length - 6, 5)}");
                    await TokenLogin(decryptedText);
                    return;
                }
                catch (Exception ex)
                {
                    MagicFlowMsg.ShowMessage($"解密失败: {ex.Message}", 2);
                    // Console.WriteLine($"解密失败: {ex.Message}");
                }

                MagicShow.ShowMsgDialog("登陆失败！", "err");
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog($"解密过程中出错: {ex.Message}\n{ex.StackTrace}", "err");
            }
        }

        private async Task<(string PubKey, JObject PollData)> GetPublicKey(string requestUuid)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"MSLTeam-MSL/{MainWindow.MSLVersion}");
            HttpResponse httpResponse = new HttpResponse();
            JObject pollData = null;
            string serverPublicKeyBase64 = null;
            int i = 0;
            while (httpResponse.HttpResponseCode != System.Net.HttpStatusCode.OK)
            {
                if (i >= 60)
                    break;
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

            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg) = await OpenFrpApi.Login(token, SaveToken.IsChecked == true);
            MagicDialog.CloseTextDialog();
            if (Code == 200)
            {
                LoginGrid.Visibility = Visibility.Collapsed;
                MainCtrl.Visibility = Visibility.Visible;
                GetUserInfo(JObject.Parse(Msg));
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录失败！请检查您的Authorization是否正确！" + Msg, "错误！");
                if (Config.Read("OpenFrpToken") != null)
                    Config.Remove("OpenFrpToken");
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
            TunnelList.Items.Clear();
            var (Code, Data, Msg) = await OpenFrpApi.GetUserNodes();
            if (Code == 200)
            {
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
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "获取失败！" + Msg, "错误！");
            }
        }

        private async Task GetNodeList()
        {
            NodeList.Items.Clear();
            (Dictionary<string, string>, JArray) process = await OpenFrpApi.GetNodeList();
            if (process == (null, null))
            {
                MagicShow.ShowMsgDialog("获取节点列表失败！", "ERR");
                return;
            }
            ApiNodeList = process.Item1;
            ApiNodeJArray = process.Item2;
            foreach (var node in ApiNodeList)
            {
                NodeList.Items.Add(node.Key);
            }
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
            Config.WriteFrpcConfig(1, $"OpenFrp节点 - {o}", $"-u {token} -p {id}", "");
            await MagicShow.ShowMsgDialogAsync("映射配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            Window.GetWindow(this).Close();
        }

        private void FrpcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                string selected_node = NodeList.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(ApiNodeList[selected_node]);
                else
                {
                    addProxieBtn.IsEnabled = true;
                    MagicShow.ShowMsgDialog("请先选择一个节点", "错误");
                    return;
                }
                string proxy_name = await MagicShow.ShowInput(Window.GetWindow(this), "隧道名称(不支持中文)");
                if (proxy_name != null)
                {
                    addProxieBtn.IsEnabled = false;
                    var (_return, msg) = await OpenFrpApi.CreateProxy(type, portBox.Text, zip, selected_node_id, remotePortBox.Text, proxy_name);
                    addProxieBtn.IsEnabled = true;
                    if (_return)
                    {
                        MainCtrl.SelectedIndex = 0;
                        MagicShow.ShowMsgDialog("隧道创建成功！", "提示");
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog("创建失败！" + msg, "错误");
                    }
                }
            }
            catch (Exception ex)
            {
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
                delProxieBtn.IsEnabled = false;
                var (_return, msg) = await OpenFrpApi.DeleteProxy(id);
                if (_return)
                {
                    MagicShow.ShowMsgDialog("删除成功！", "提示");
                }
                else
                {
                    MagicShow.ShowMsgDialog("删除失败！" + msg, "错误");
                }
                await GetUserTunnels();
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog("出现错误！" + ex.Message, "错误");
            }
            finally
            {
                delProxieBtn.IsEnabled = true;
            }
        }

        private void RandomRemotePortBtn_Click(object sender, RoutedEventArgs e)
        {
            RandRemotePort(true);
        }

        void RandRemotePort(bool tips)
        {
            if (tips)
            {
                if (NodeList.SelectedIndex == -1)
                {
                    MagicShow.ShowMsgDialog("请先选择一个节点", "错误");
                    return;
                }
                (int, int) remote_port_limit = (10000, 99999);
                string selected_node = NodeList.SelectedItem.ToString();
                int selected_node_id;
                if (selected_node != null) selected_node_id = Convert.ToInt16(ApiNodeList[selected_node]);
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先选择一个节点", "错误");
                    return;
                }
                foreach (var node in ApiNodeJArray)
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

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFrpApi.AuthId = string.Empty;
            Config.Remove("OpenFrpToken");
            LoginGrid.Visibility = Visibility.Visible;
            MainCtrl.Visibility = Visibility.Collapsed;
            userInfo.Content = string.Empty;
        }
    }
}
