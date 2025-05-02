using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        }

        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            string userAccount = UserAccount.Text;
            string userPassword = UserPassword.Password;
            if (string.IsNullOrEmpty(userAccount) || string.IsNullOrEmpty(userPassword))
            {
                MagicFlowMsg.ShowMessage("请输入账号和密码！", 2);
                return;
            }
            var (Success, Msg, ContentInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword);
            if (!Success)
            {
                if (Require2FA)
                {
                    if(ContentInfo == null)
                    {
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
                MagicShow.ShowMsgDialog(Msg, "错误");
                return;
            }
            
            // LoginGrid.Visibility = Visibility.Collapsed;
            // MainCtrl.Visibility = Visibility.Visible;
            UserAccount.Text = string.Empty;
            UserPassword.Password = string.Empty;
            LoginSuccess.Invoke(ContentInfo);
            // 解析用户信息并更新UI
            // UpdateUserInfo(JObject.Parse(UserInfo));
            // await GetTunnelList();
        }

        private void User2FAReturn_Click(object sender, RoutedEventArgs e)
        {
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
                MagicFlowMsg.ShowMessage("请输入验证代码！", 2);
                return;
            }
            var (Success, Msg, ContentInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword, userAuth2FA);
            if (!Success)
            {
                MagicShow.ShowMsgDialog(Msg, "错误");
                return;
            }

            // Auth2FAGrid.Visibility = Visibility.Collapsed;
            // MainCtrl.Visibility = Visibility.Visible;
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
            var (Code, _, Msg) = await MSLFrpApi.ApiPost("/user/getVerifyCode", HttpService.PostContentType.FormUrlEncoded, new Dictionary<string, string> {
                { "email", userAccount },
                { "action", "verify-2fa" }
            }, true);
            if (Code != 200)
            {
                Auth2FAResend.IsEnabled = true;
                MagicShow.ShowMsgDialog(Msg, "错误");
                return;
            }
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

            bool save = (bool)SaveToken.IsChecked;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg, ContentInfo) = await MSLFrpApi.UserLogin(string.Empty, userAccount, userPassword, auth2FA, save);
            MagicDialog.CloseTextDialog();

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
            if(e.Key == Key.Enter)
            {
                UserLogin_Click(null, null);
            }
        }

        private void Auth2FACode_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                User2FALogin_Click(null, null);
            }
        }
    }
}
