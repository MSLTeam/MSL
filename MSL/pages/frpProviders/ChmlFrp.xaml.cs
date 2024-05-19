using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
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
                    Task.Run(() => GetFrpList(token));
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "登陆失败！" + jsonResponse["message"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
                    
                }
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    Dispatcher.Invoke(() =>
                    {

                        Shows.ShowMsgDialog(Window.GetWindow(this), "登陆失败！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
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
                //这里就拿到token了
                Task.Run(() => GetFrpList(userToken));
            }
            else
            {
                if (jsonResponse.ContainsKey("error"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "Token登陆失败！\n可以尝试账号密码登录！\n" + jsonResponse["error"].ToString(), LanguageManager.Instance["Dialog_Err"]);
                    });
                    }

            }
        }

        //登录成功了，然后就是获取隧道,丢到ui去
        private void GetFrpList(String token )
        {
            Dispatcher.Invoke(() =>
            {
                MainGrid.Visibility = Visibility.Visible;
                LoginGrid.Visibility = Visibility.Collapsed;
            });
            string response = Functions.Get($"api/usertunnel.php?token={token}", ChmlFrpApiUrl);
            try
            {
                var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                foreach ( var item in jsonArray )
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrpList.Items.Add($"{item["name"]}({item["node"]})");
                    });
                }
            }
            catch (JsonSerializationException)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "建议创建一个哦~" , "您似乎没有隧道");
                });
            }

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
        }
    }
}
