using MSL.controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
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
        string ChmlFrpApiUrl = "https://panel.chmlfrp.cn";
        public ChmlFrp()
        {
            InitializeComponent();
        }

        //使用token登录
        private async void userTokenLogin_Click(object sender, RoutedEventArgs e)
        {
            string token;
            token = await Shows.ShowInput(Window.GetWindow(this), "请输入Chml账户Token", "", true);
            Task.Run(() => verifyUserToken(token));
        }

        //账号密码
        private async void userLogin_Click(object sender, RoutedEventArgs e)
        {
            string frpUser, frpPassword;
           frpUser = await Shows.ShowInput(Window.GetWindow(this), "请输入ChmlFrp的账户名/邮箱/QQ号");
            frpPassword = await Shows.ShowInput(Window.GetWindow(this), "请输入密码","", true);
            Task.Run(() => getUserToken(frpUser, frpPassword));
        }

        //注册一个可爱的账户
        private void userRegister_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://panel.chmlfrp.cn/register");
        }


        //异步登录，获取到用户token
        private void getUserToken(string user,string pwd)
        {
            string response = Functions.Post("api/login.php", 2, $"username={user}&password={pwd}", ChmlFrpApiUrl);
            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (jsonResponse.ContainsKey("code"))
            {
                if (jsonResponse["code"].ToString() == "200")
                {
                    string token = jsonResponse["token"].ToString();
                    //这里就拿到token了
                    MessageBox.Show(token);
                }
                else
                {
                    MessageBox.Show("登陆失败！"+ jsonResponse["message"].ToString());
                }
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    MessageBox.Show("登陆失败！\n" + jsonResponse["error"].ToString());
                }
                    
            }
            
            
        }

        //直接token登录，那么验证下咯~
        private void verifyUserToken(string userToken)
        {
            string response = Functions.Get($"api/userinfo.php?usertoken={userToken}", ChmlFrpApiUrl);
            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
            if (jsonResponse.ContainsKey("userid"))
            {
                MessageBox.Show(userToken);
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    MessageBox.Show("登陆失败！\n" + jsonResponse["error"].ToString());
                }

            }
        }

        //然后就是获取隧道

    }
}
