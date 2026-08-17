using MSL.utils;
using MSL.langs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using MSL.utils.Config;

namespace MSL.pages.frpProviders.MSLFrp
{
    /// <summary>
    /// MSLFrpLogin.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrpLogin : UserControl
    {
        public Action<JObject> LoginSuccess { get; set; }

        private CancellationTokenSource _pollingCts; // 取消轮询
        private const string AppId = "eixl7BLlidSZ7POjdhZsAGTXKyu"; // AppId

        private string _currentBrowserLoginUrl = string.Empty;


        public MSLFrpLogin()
        {
            InitializeComponent();
            LogHelper.Write.Info("MSLFrp 登录控件初始化完成。");
        }

        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            string userAccount = UserAccount.Text;
            string userPassword = UserPassword.Password;
            if (string.IsNullOrEmpty(userAccount) || string.IsNullOrEmpty(userPassword))
            {
                LogHelper.Write.Warn("登录尝试中止：账号或密码为空。");
                MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_EnterAccountPassword, 2);
                return;
            }
            LogHelper.Write.Info($"用户 '{userAccount}' 正在尝试登录...");
            var (Success, Msg, ContentInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword);
            if (!Success)
            {
                if (Require2FA)
                {
                    LogHelper.Write.Info($"用户 '{userAccount}' 需要进行2FA验证。");
                    if (ContentInfo == null)
                    {
                        LogHelper.Write.Error("2FA流程中止：ContentInfo为空，可能为API异常。");
                        MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_UnknownError, 2);
                        return;
                    }
                    LoginGrid.Visibility = Visibility.Collapsed;
                    Auth2FAGrid.Visibility = Visibility.Visible;

                    if (ContentInfo["type"].Value<string>() == "email")
                    {
                        Auth2FARemark.Text = Lang.Frp_MSLFrpLogin_2FAEmail;
                        Auth2FAResend.Visibility = Visibility.Visible;
                        await Resend2FA();
                    }
                    else
                    {
                        Auth2FARemark.Text = Lang.Frp_MSLFrpLogin_2FATypeApp;
                        Auth2FAResend.Visibility = Visibility.Collapsed;
                    }
                    return;
                }
                LogHelper.Write.Error($"用户 '{userAccount}' 登录失败。错误信息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
                return;
            }

            // LoginGrid.Visibility = Visibility.Collapsed;
            // MainCtrl.Visibility = Visibility.Visible;
            LogHelper.Write.Info($"用户 '{userAccount}' 登录成功。");
            UserAccount.Text = string.Empty;
            UserPassword.Password = string.Empty;
            LoginSuccess.Invoke(ContentInfo);
            // 解析用户信息并更新UI
            // UpdateUserInfo(JObject.Parse(UserInfo));
            // await GetTunnelList();
        }

        private void User2FAReturn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户从2FA验证界面返回登录界面。");
            LoginGrid.Visibility = Visibility.Visible;
            Auth2FAGrid.Visibility = Visibility.Collapsed;
            Auth2FACode.Text = string.Empty;
        }

        private async void User2FALogin_Click(object sender, RoutedEventArgs e)
        {
            string userAccount = UserAccount.Text;
            string userPassword = UserPassword.Password;
            string userAuth2FA = Auth2FACode.Text;
            if (string.IsNullOrEmpty(userAuth2FA))
            {
                LogHelper.Write.Warn("2FA登录尝试中止：验证码为空。");
                MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_EnterCode, 2);
                return;
            }
            LogHelper.Write.Info($"用户 '{userAccount}' 正在提交2FA验证码...");
            // 调用 UserLogin，返回 (Code, Msg, ContentInfo)
            var (Success, Msg, ContentInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword, userAuth2FA);
            if (!Success)
            {
                LogHelper.Write.Error($"用户 '{userAccount}' 2FA登录失败。错误信息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
                return;
            }

            // Auth2FAGrid.Visibility = Visibility.Collapsed;
            // MainCtrl.Visibility = Visibility.Visible;
            LogHelper.Write.Info($"用户 '{userAccount}' 2FA登录成功。");
            UserAccount.Text = string.Empty;
            UserPassword.Password = string.Empty;
            Auth2FACode.Text = string.Empty;
            LoginSuccess.Invoke(ContentInfo);
            // 解析用户信息并更新UI
            // UpdateUserInfo(JObject.Parse(Msg));
            // await GetTunnelList();
        }

        private async void Auth2FAResend_Click(object sender, RoutedEventArgs e)
        {
            await Resend2FA();
        }

        private async Task Resend2FA()
        {
            Auth2FAResend.IsEnabled = false;
            string userAccount = UserAccount.Text;
            LogHelper.Write.Info($"为用户 '{userAccount}' 请求重新发送2FA验证码。");

            var (Code, _, Msg) = await MSLFrpApi.ApiPost("/user/getVerifyCode", HttpService.PostContentType.FormUrlEncoded, new Dictionary<string, string> {
                { "email", userAccount },
                { "action", "verify-2fa" }
            }, true);
            if (Code != 200)
            {
                LogHelper.Write.Error($"请求重发2FA验证码失败。API返回代码: {Code}, 消息: {Msg}");
                Auth2FAResend.IsEnabled = true;
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
                return;
            }
            LogHelper.Write.Info("已成功请求发送2FA验证码。");
            MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_CodeSent, 1, panel: Auth2FAGrid);
            Auth2FACode.Focus();
            for (int i = 60; i > 0; i--)
            {
                Auth2FAResend.Content = string.Format(Lang.Frp_MSLFrpLogin_ResendCountdown, i);
                await Task.Delay(1000);
            }
            Auth2FAResend.Content = Lang.Frp_MSLFrpLogin_Resend;
            Auth2FAResend.IsEnabled = true;
        }


        // 账户密码登录
        private async Task<(bool Success, string Msg, JObject ContentInfo, bool Require2FA)> UserLoginEvent(string userAccount, string userPassword, string auth2FA = "")
        {
            LogHelper.Write.Info($"执行登录API调用。账号: {userAccount}, 是否提供2FA码: {!string.IsNullOrEmpty(auth2FA)}");
            bool save = (bool)SaveToken.IsChecked;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_LoggingIn);

            var (Code, Msg, ContentInfo) = await MSLFrpApi.UserLogin(string.Empty, userAccount, userPassword, auth2FA, save);

            MagicDialog.CloseTextDialog();
            LogHelper.Write.Info($"登录API调用完成。返回代码: {Code}, 消息: {Msg}");

            if (Code == 428)
            {
                return (false, string.Empty, ContentInfo, true);
            }

            if (Code != 200)
            {
                return (false, Msg, null, false);
            }

            return (true, string.Empty, ContentInfo, false);
        }

        private void UserAccount_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UserPassword.Focus();
            }
        }

        private void UserPassword_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UserLogin_Click(null, null);
            }
        }

        private void Auth2FACode_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                User2FALogin_Click(null, null);
            }
        }

        // ========== 浏览器登录 ==========

        private async void BrowserLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("开始浏览器登录流程...");
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_PreparingBrowser);

            var (success, ssid, url, msg, csrf) = await InitiateBrowserLogin();

            magicDialog.CloseTextDialog();

            if (success)
            {
                _currentBrowserLoginUrl = url;

                LogHelper.Write.Info($"获取浏览器登录URL成功, SSID: {ssid}。正在打开URL...");
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                    LoginGrid.Visibility = Visibility.Collapsed;
                    BrowserLoginGrid.Visibility = Visibility.Visible;

                    StartPolling(ssid, csrf); // 轮询
                }
                catch (Exception ex)
                {
                    LogHelper.Write.Error($"打开浏览器失败: {ex.Message}");
                    LoginGrid.Visibility = Visibility.Collapsed;
                    BrowserLoginGrid.Visibility = Visibility.Visible;
                    StartPolling(ssid, csrf);
                    MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_BrowserOpenFailed, 3);
                }
            }
            else
            {
                LogHelper.Write.Error($"初始化浏览器登录失败: {msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), msg, "错误");
            }
        }

        // 重新打开浏览器页面
        private void ReopenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentBrowserLoginUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_currentBrowserLoginUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_OpenFailed + ex.Message, 2);
                }
            }
        }

        // 复制链接
        private void CopyBrowserLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentBrowserLoginUrl))
            {
                try
                {
                    Clipboard.SetText(_currentBrowserLoginUrl);
                    MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_LinkCopied,1);
                }
                catch (Exception ex)
                {
                    MagicFlowMsg.ShowMessage(Lang.Frp_MSLFrpLogin_CopyFailed + ex.Message, 2);
                }
            }
        }

        private void CancelBrowserLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户取消了浏览器登录轮询。");
            // 取消轮询
            _pollingCts?.Cancel();

            // 切换回登录界面
            LoginGrid.Visibility = Visibility.Visible;
            BrowserLoginGrid.Visibility = Visibility.Collapsed;
        }

        private async Task<(bool success, string ssid, string url, string msg, string csrf)> InitiateBrowserLogin()
        {
            string csrf = Functions.RandomString("", 32);
            var postData = new Dictionary<string, string>
            {
                { "csrf", csrf },
                { "appid", AppId }
            };

            var (Code, ContentInfo, Msg) = await MSLFrpApi.ApiPost(
                "/oauth/createAppLogin",
                HttpService.PostContentType.FormUrlEncoded,
                postData,
                true
            );

            if (Code == 200 && ContentInfo != null && ContentInfo.Type != JTokenType.Null)
            {
                string url = ContentInfo["url"]?.Value<string>();
                string ssid = ContentInfo["ssid"]?.Value<string>();

                if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(ssid))
                {
                    return (true, ssid, url, null, csrf);
                }
                else
                {
                    return (false, null, null, Lang.Frp_MSLFrpLogin_ApiFormatError, null);
                }
            }
            else
            {
                string errorMsg = (Code == 200 && (ContentInfo == null || ContentInfo.Type == JTokenType.Null))
                                ? Lang.Frp_MSLFrpLogin_ApiError
                                : Msg;
                return (false, null, null, errorMsg ?? Lang.Frp_MSLFrpLogin_RequestFailed, null);
            }
        }

        private async void StartPolling(string ssid, string csrf)
        {
            _pollingCts = new CancellationTokenSource();
            var cancellationToken = _pollingCts.Token;

            LogHelper.Write.Info($"开始轮询 SSID: {ssid}");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var (Code, ContentInfo, Msg) = await MSLFrpApi.ApiGet(
                        $"/oauth/appLogin?ssid={ssid}&csrf={csrf}",
                        true
                    );

                    if (cancellationToken.IsCancellationRequested) return;

                    if (Code == 200)
                    {
                        var appToken = ContentInfo?["token"]?.Value<string>();
                        if (!string.IsNullOrEmpty(appToken))
                        {
                            LogHelper.Write.Info("轮询成功，获取到App Token。");
                            await CompleteBrowserLogin(appToken);
                            return; // 结束轮询
                        }
                        else
                        {
                            // 继续轮询
                            LogHelper.Write.Debug("轮询中... Token 尚未准备好。");
                        }
                    }
                    else
                    {
                        // 出现错误
                        LogHelper.Write.Error($"轮询失败。代码: {Code}, 消息: {Msg}");
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg ?? Lang.Frp_MSLFrpLogin_LoginTimeout, "登录失败");
                        CancelBrowserLoginButton_Click(null, null); // 自动取消
                        return; // 结束轮询
                    }

                    // 延迟
                    await Task.Delay(3000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                LogHelper.Write.Info("轮询被取消。");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"轮询时发生意外错误: {ex.Message}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_PollingError + ex.Message, "错误");
                CancelBrowserLoginButton_Click(null, null); // 自动取消
            }
            finally
            {
                if (_pollingCts != null)
                {
                    _pollingCts.Dispose();
                    _pollingCts = null;
                }
            }
        }

        private async Task CompleteBrowserLogin(string appToken)
        {
            LogHelper.Write.Info("使用App Token执行最终登录...");
            bool save = (bool)SaveToken.IsChecked;

            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_Verifying);

            var (Code, Msg, ContentInfo) = await MSLFrpApi.UserLogin(
                appToken,
                string.Empty, // email
                string.Empty, // password
                string.Empty, // auth2FA
                save
            );

            magicDialog.CloseTextDialog();

            if (Code == 200)
            {
                LogHelper.Write.Info("浏览器登录成功！");
                UserAccount.Text = string.Empty;
                UserPassword.Password = string.Empty;
                BrowserLoginGrid.Visibility = Visibility.Collapsed; // 隐藏等待界面
                LoginSuccess.Invoke(ContentInfo); // 成功回调
            }
            else
            {
                LogHelper.Write.Error($"使用App Token登录失败。代码: {Code}, 消息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "登录失败");
                CancelBrowserLoginButton_Click(null, null); // 失败 返回登录界面
            }
        }

        #region 微信登录

        private CancellationTokenSource _wechatPollingCts;
        private string _wechatLoginState = string.Empty;

        private async void WechatLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("开始微信扫码登录流程...");
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_GettingQrCode);

            // 1. 获取 state 并保持 Cookie
            var (Code, ContentInfo, Msg) = await MSLFrpApi.ApiGet("/oauth/redirect?provider=wechat_miniprogram&mode=login", true);
            if (Code != 200 || ContentInfo == null)
            {
                magicDialog.CloseTextDialog();
                LogHelper.Write.Error($"获取微信授权重定向失败: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg ?? Lang.Frp_MSLFrpLogin_GetAuthFailed, "错误");
                return;
            }

            string url = ContentInfo["uri"]?.Value<string>();
            if (string.IsNullOrEmpty(url) || !url.Contains("state="))
            {
                magicDialog.CloseTextDialog();
                LogHelper.Write.Error($"获取微信授权重定向解析状态失败");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_GetAuthFailed, "错误");
                return;
            }

            // 从 uri 中解析出 state (例如: /oauth/wechat-miniprogram?state=xxx&mode=login)
            var uriParts = url.Split('?', '&');
            foreach (var part in uriParts)
            {
                if (part.StartsWith("state="))
                {
                    _wechatLoginState = part.Substring("state=".Length);
                    break;
                }
            }

            // 2. 获取小程序二维码
            var (qrCode, qrData, qrMsg) = await MSLFrpApi.ApiGet("/oauthClient/wechat-mp/qrcode?type=qrcode", true);
            magicDialog.CloseTextDialog();

            if (qrCode == 200 && qrData != null)
            {
                string sessionId = qrData["sessionId"]?.Value<string>();
                string base64Image = qrData["qrcode"]?.Value<string>();

                if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(base64Image))
                {
                    if (base64Image.StartsWith("data:image"))
                    {
                        base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
                    }

                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(base64Image);
                        BitmapImage bitmapImage = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(imageBytes))
                        {
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = stream;
                            bitmapImage.EndInit();
                        }
                        WechatQrCodeImage.Source = bitmapImage;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write.Error($"解析二维码图片失败: {ex.Message}");
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_ParseQrFailed, "错误");
                        return;
                    }

                    LoginGrid.Visibility = Visibility.Collapsed;
                    WechatLoginGrid.Visibility = Visibility.Visible;
                    WechatLoginStatusText.Text = Lang.Frp_MSLFrpLogin_ScanQrCode;
                    WechatQrCodeImage.Visibility = Visibility.Visible;

                    StartWechatPolling(sessionId);
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_QrFormatError, "错误");
                }
            }
            else
            {
                LogHelper.Write.Error($"获取微信二维码失败: {qrMsg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), qrMsg ?? Lang.Frp_MSLFrpLogin_GetQrFailed, "错误");
            }
        }

        private async void StartWechatPolling(string sessionId)
        {
            _wechatPollingCts = new CancellationTokenSource();
            var cancellationToken = _wechatPollingCts.Token;

            LogHelper.Write.Info($"开始微信登录轮询 session_id: {sessionId}");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var (Code, ContentInfo, Msg) = await MSLFrpApi.ApiGet(
                        $"/oauthClient/wechat-mp/poll?session_id={sessionId}",
                        true
                    );

                    if (cancellationToken.IsCancellationRequested) return;

                    if (Code == 200 && ContentInfo != null)
                    {
                        string status = ContentInfo["status"]?.Value<string>();
                        if (status == "scanned")
                        {
                            string authCode = ContentInfo["auth_code"]?.Value<string>();
                            if (!string.IsNullOrEmpty(authCode))
                            {
                                LogHelper.Write.Info("微信扫码成功，开始执行登录...");
                                WechatLoginStatusText.Text = Lang.Frp_MSLFrpLogin_Scanned;
                                WechatQrCodeImage.Visibility = Visibility.Collapsed;
                                WechatLoadingCircle.Visibility = Visibility.Visible;

                                await CompleteWechatLogin(authCode);
                                return; // 结束轮询
                            }
                        }
                        else if (status == "waiting")
                        {
                            // 继续轮询
                            LogHelper.Write.Debug("微信轮询中... 尚未扫码。");
                        }
                        else if (status == "expired")
                        {
                            LogHelper.Write.Info("微信二维码已过期。");
                            WechatLoginStatusText.Text = Lang.Frp_MSLFrpLogin_QrExpired;
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_QrExpiredConfirm, "提示");
                            CancelWechatLoginButton_Click(null, null);
                            return;
                        }
                    }
                    else
                    {
                        // 出现错误
                        LogHelper.Write.Error($"微信轮询失败。代码: {Code}, 消息: {Msg}");
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg ?? "获取状态失败，请重试。", "错误");
                        CancelWechatLoginButton_Click(null, null); // 自动取消
                        return; // 结束轮询
                    }

                    // 延迟2秒
                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                LogHelper.Write.Info("微信轮询被取消。");
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"微信轮询时发生意外错误: {ex.Message}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Lang.Frp_MSLFrpLogin_PollingError + ex.Message, "错误");
                CancelWechatLoginButton_Click(null, null);
            }
            finally
            {
                if (_wechatPollingCts != null)
                {
                    _wechatPollingCts.Dispose();
                    _wechatPollingCts = null;
                }
                WechatLoadingCircle.Visibility = Visibility.Collapsed;
            }
        }

        private async Task CompleteWechatLogin(string authCode)
        {
            var postData = new Dictionary<string, string>
            {
                { "code", authCode },
                { "state", _wechatLoginState },
                { "provider", "wechat_miniprogram" },
                { "mode", "login" }
            };

            var (Code, ContentInfo, Msg) = await MSLFrpApi.ApiPost(
                "/oauth/login",
                HttpService.PostContentType.Json,
                postData,
                true
            );

            if (Code == 200 && ContentInfo != null)
            {
                string token = ContentInfo["token"]?.Value<string>();
                if (!string.IsNullOrEmpty(token))
                {
                    LogHelper.Write.Info("微信登录成功，获取到Token。");
                    
                    // 保存Token设置
                    bool save = (bool)SaveToken.IsChecked;
                    if (save)
                    {
                        Config.Write("MSLUserAccessToken", token);
                    }
                    MSLFrpApi.UserToken = token;

                    var (loginCode, loginMsg, userInfo) = await MSLFrpApi.UserLogin(token: token, saveToken: save);
                    if (loginCode == 200 && userInfo != null)
                    {
                        WechatLoginGrid.Visibility = Visibility.Collapsed;
                        LoginSuccess.Invoke(userInfo);
                    }
                    else
                    {
                        LogHelper.Write.Error($"微信登录成功，但获取用户信息失败: {loginMsg}");
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), $"登录成功，但获取用户信息失败: {loginMsg}", "错误");
                        CancelWechatLoginButton_Click(null, null);
                    }
                }
                else
                {
                    LogHelper.Write.Error("微信登录成功，但未返回Token。");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "登录成功，但未返回Token。", "错误");
                    CancelWechatLoginButton_Click(null, null);
                }
            }
            else
            {
                LogHelper.Write.Error($"微信登录失败。代码: {Code}, 消息: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg ?? "微信登录失败", "错误");
                CancelWechatLoginButton_Click(null, null);
            }
        }

        private void CancelWechatLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("用户取消了微信扫码登录。");
            _wechatPollingCts?.Cancel();

            LoginGrid.Visibility = Visibility.Visible;
            WechatLoginGrid.Visibility = Visibility.Collapsed;
            WechatLoadingCircle.Visibility = Visibility.Collapsed;
        }
    }

        #endregion
}