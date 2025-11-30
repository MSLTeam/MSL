using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;

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
                MagicFlowMsg.ShowMessage("请输入账号和密码！", 2);
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
                        MagicFlowMsg.ShowMessage("未知错误，请稍后再试！", 2);
                        return;
                    }
                    LoginGrid.Visibility = Visibility.Collapsed;
                    Auth2FAGrid.Visibility = Visibility.Visible;

                    if (ContentInfo["type"].Value<string>() == "email")
                    {
                        Auth2FARemark.Text = "我们向您的邮箱发送了一个验证码，请输入验证码以完成登录。";
                        Auth2FAResend.Visibility = Visibility.Visible;
                        await Resend2FA();
                    }
                    else
                    {
                        Auth2FARemark.Text = "请输入您绑定的2FA软件的实时代码以登录。";
                        Auth2FAResend.Visibility = Visibility.Collapsed;
                    }
                    return;
                }
                LogHelper.Write.Error($"用户 '{userAccount}' 登录失败。错误信息: {Msg}");
                MagicShow.ShowMsgDialog(Msg, "错误");
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
                MagicFlowMsg.ShowMessage("请输入验证代码！", 2);
                return;
            }
            LogHelper.Write.Info($"用户 '{userAccount}' 正在提交2FA验证码...");
            // 调用 UserLogin，返回 (Code, Msg, ContentInfo)
            var (Success, Msg, ContentInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword, userAuth2FA);
            if (!Success)
            {
                LogHelper.Write.Error($"用户 '{userAccount}' 2FA登录失败。错误信息: {Msg}");
                MagicShow.ShowMsgDialog(Msg, "错误");
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
                MagicShow.ShowMsgDialog(Msg, "错误");
                return;
            }
            LogHelper.Write.Info("已成功请求发送2FA验证码。");
            MagicFlowMsg.ShowMessage("验证码已发送，请注意查收！", 1, panel: Auth2FAGrid);
            Auth2FACode.Focus();
            for (int i = 60; i > 0; i--)
            {
                Auth2FAResend.Content = $"重新发送({i}s)";
                await Task.Delay(1000);
            }
            Auth2FAResend.Content = "重新发送";
            Auth2FAResend.IsEnabled = true;
        }


        // 账户密码登录
        private async Task<(bool Success, string Msg, JObject ContentInfo, bool Require2FA)> UserLoginEvent(string userAccount, string userPassword, string auth2FA = "")
        {
            LogHelper.Write.Info($"执行登录API调用。账号: {userAccount}, 是否提供2FA码: {!string.IsNullOrEmpty(auth2FA)}");
            bool save = (bool)SaveToken.IsChecked;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");

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
            magicDialog.ShowTextDialog(Window.GetWindow(this), "正在准备浏览器登录...");

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
                    MagicFlowMsg.ShowMessage("自动打开浏览器失败，请手动复制链接", 3);
                }
            }
            else
            {
                LogHelper.Write.Error($"初始化浏览器登录失败: {msg}");
                MagicShow.ShowMsgDialog(msg, "错误");
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
                    MagicFlowMsg.ShowMessage($"打开失败: {ex.Message}", 2);
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
                    MagicFlowMsg.ShowMessage("登录链接已复制到剪贴板！",1);
                }
                catch (Exception ex)
                {
                    MagicFlowMsg.ShowMessage($"复制失败: {ex.Message}", 2);
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
                    return (false, null, null, "API返回数据格式不正确 (url/ssid为空)", null);
                }
            }
            else
            {
                string errorMsg = (Code == 200 && (ContentInfo == null || ContentInfo.Type == JTokenType.Null))
                                ? "API返回错误。"
                                : Msg;
                return (false, null, null, errorMsg ?? "请求失败，未知错误", null);
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
                        MagicShow.ShowMsgDialog(Msg ?? "登录已超时或失败，请重试。", "登录失败");
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
                MagicShow.ShowMsgDialog($"轮询时发生错误: {ex.Message}", "错误");
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
            magicDialog.ShowTextDialog(Window.GetWindow(this), "正在验证登录...");

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
                MagicShow.ShowMsgDialog(Msg, "登录失败");
                CancelBrowserLoginButton_Click(null, null); // 失败 返回登录界面
            }
        }
    }
}