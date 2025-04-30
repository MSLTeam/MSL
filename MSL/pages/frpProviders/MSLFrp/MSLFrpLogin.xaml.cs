using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSL.pages.frpProviders.MSLFrp
{
    /// <summary>
    /// MSLFrpLogin.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrpLogin : UserControl
    {
        public Action<string> LoginSuccess { get; set; }
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
            var (Success, Msg, UserInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword);
            if (!Success)
            {
                if (Require2FA)
                {
                    LoginGrid.Visibility = Visibility.Collapsed;
                    Auth2FAGrid.Visibility = Visibility.Visible;
                    return;
                }
                MagicShow.ShowMsgDialog(Msg, "错误");
                return;
            }

            
            // LoginGrid.Visibility = Visibility.Collapsed;
            // MainCtrl.Visibility = Visibility.Visible;
            UserAccount.Text = string.Empty;
            UserPassword.Password = string.Empty;
            LoginSuccess.Invoke(UserInfo);
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
            var (Success, Msg, UserInfo, Require2FA) = await UserLoginEvent(userAccount, userPassword, userAuth2FA);
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
            LoginSuccess.Invoke(UserInfo);
            // 解析用户信息并更新UI
            // UpdateUserInfo(JObject.Parse(Msg));
            // await GetTunnelList();
        }

        private async Task<(bool Success, string Msg, string UserInfo, bool Require2FA)> UserLoginEvent(string userAccount, string userPassword, string auth2FA = "")
        {

            bool save = (bool)SaveToken.IsChecked;
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");
            var (Code, Msg, UserInfo) = await MSLFrpApi.UserLogin(string.Empty, userAccount, userPassword, auth2FA, save);
            MagicDialog.CloseTextDialog();

            if (Code == 428)
            {
                return (false, string.Empty, string.Empty, true);
            }

            if (Code != 200)
            {
                return (false, Msg, string.Empty, false);
            }

            return (true, string.Empty, UserInfo, false);

        }
    }
}
