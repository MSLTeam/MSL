using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// HACK: 假设 LogHelper 和 LogLevel 在此命名空间或上层可见
// 如果不可见，您可能需要添加 using 声明
// using YourNamespace.Containing.LogHelper; 

namespace MSL.pages.frpProviders.MSLFrp
{
    /// <summary>
    /// MSLFrpLogin.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrpLogin : UserControl
    {
        public Action<JObject> LoginSuccess { get; set; }
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
    }
}