using MSL.controls;
using MSL.utils;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSL.pages.frpProviders.MSLFrp
{
    /// <summary>
    /// MSLFrpProfile.xaml 的交互逻辑
    /// </summary>
    public partial class MSLFrpProfile : Page
    {
        public MSLFrpProfile()
        {
            InitializeComponent();
        }

        bool isInit = false;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInit)
                return;
            isInit = true;

            // 获取Token并尝试登录
            var token = string.IsNullOrEmpty(MSLFrpApi.UserToken)
                ? Config.Read("MSLUserAccessToken")?.ToString()
                : MSLFrpApi.UserToken;

            if (string.IsNullOrEmpty(token))
            {
                ShowLoginControl();
                return;
            }
            else
            {
                if (await PerformLogin(token) == false)
                {
                    ShowLoginControl();
                    return;
                }
            }
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
            await GetUserInfo();
            await GetGoods();
            magicDialog.CloseTextDialog();
        }

        private void ShowLoginControl()
        {
            // 显示登录页面
            MSLFrpLogin loginControl = new MSLFrpLogin();
            loginControl.LoginSuccess += async delegate
            {
                if (await PerformLogin(MSLFrpApi.UserToken) == true)
                {
                    LoginControl.Visibility = Visibility.Collapsed;
                    MainGrid.Visibility = Visibility.Visible;
                    MagicDialog magicDialog = new MagicDialog();
                    magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
                    await GetUserInfo();
                    await GetGoods();
                    magicDialog.CloseTextDialog();
                }
            };
            LoginControl.Content = loginControl;
            LoginControl.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
        }

        private async Task<bool> PerformLogin(string token)
        {
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "登录中……");

            (int Code, string Msg,_) = await MSLFrpApi.UserLogin(token);

            magicDialog.CloseTextDialog();

            if (Code != 200)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！\n" + Msg, "错误");
                return false;
            }

            return true;
        }

        private async Task GetUserInfo()
        {
            var (Code, Data, Msg) = await MSLFrpApi.ApiGet("/user/info");
            if (Code != 200)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Msg, "ERR");
                ShowLoginControl();
                return;
            }
            JObject userData = JObject.Parse(Data.ToString());
            Name_Label.Content = "用户名：" + userData["username"].ToString();
            Uid_Label.Content = "UID：" + ((int)userData["uid"] + 10000).ToString();
            Email_Label.Content = "电子邮箱：\n" + userData["email"].ToString();
            QQ_Label.Content = "QQ：" + userData["qq"].ToString();
            Score_Label.Content = "积分：" + userData["score"].ToString();
            RegTime_Label.Content = "注册时间：" + Functions.ConvertUnixTimeSeconds((long)userData["regTime"]);
            LastLogin_Label.Content = "最后登录：" + Functions.ConvertUnixTimeSeconds((long)userData["lastLoginTime"]);
            Perm_Label.Content = "权限组：" + (userData["permission"].ToString() == "1" ? "超级管理员" : "普通用户");
            RealnameVerify_Label.Content = "实名认证：" + ((bool)userData["realName"] == true ? "已通过" : "未通过");
            if ((bool)userData["realName"])
            {
                RealnameVerify_Button.Visibility = Visibility.Collapsed;
            }
            else
            {
                RealnameVerify_Button.Visibility = Visibility.Visible;
            }
            HeadImage.Source = BitmapFrame.Create(new Uri(userData["avatar"].ToString()), BitmapCreateOptions.None, BitmapCacheOption.Default);
        }

        private async void RealnameVerify_Button_Click(object sender, RoutedEventArgs e)
        {
            if (await MagicShow.ShowMsgDialogAsync("请选择实名方式: \n#MSL用户中心网页实名: 支持微信、支付宝实名(其中支付宝支持港澳台居民实名);\n#MSL内实名: 仅支持中国大陆居民身份证支付宝实名 (二维码较小，有扫不到的可能);\n实名费用: 会员免费支付宝实名，支付宝实名150积分，微信实名200积分。", "提示", true,"MSL内实名","前往MSL用户中心实名"))
            {
                Process.Start("https://user.mslmc.net/user/profile");
                return;
            }
            RealnameVerify_Button.IsEnabled = false;
            string certID = await MagicShow.ShowInput(Window.GetWindow(this), "请输入您的身份证号码", "");
            if(certID == null)
            {
                return;
            }
            string certName = await MagicShow.ShowInput(Window.GetWindow(this), "请输入您的真实姓名", "");
            if (certName == null)
            {
                return;
            }

            var parameterData = new Dictionary<string, string> {
                { "cert_id", certID },
                { "cert_name", certName },
                { "cert_type", "IDENTITY_CARD" },
                { "verify_type", "alipay" }
            };
            var (Code, Data, Msg) = await MSLFrpApi.ApiPost("/user/submitRealNameVerify", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                Image qrCodeImageBox = new()
                {
                    Width = 172,
                    Height = 172
                };
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(Data["url"].ToString(), QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                using (System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(10))
                {
                    qrCodeImageBox.Source = BitmapConverter.ConvertToImageSource(qrCodeImage);
                }
                if (await MagicShow.ShowMsgDialogAsync(Msg, "实名认证", true, "取消实名", "认证完成", qrCodeImageBox))
                {
                    (Code, Data, Msg) = await MSLFrpApi.ApiGet("/user/getRealNameVerifyResult");
                    if (Code == 200)
                    {
                        if ((bool)Data["passed"])
                            MagicShow.ShowMsgDialog("实名认证成功！", "成功");
                        else
                            MagicShow.ShowMsgDialog("实名认证未通过！", "失败");
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Msg, "错误");
                    }
                    await GetUserInfo();
                }
            }
            else
            {
                MagicShow.ShowMsgDialog(Msg, "错误");
            }
            RealnameVerify_Button.IsEnabled = true;
        }

        private async Task GetGoods()
        {
            var (Code, Data, Msg) = await MSLFrpApi.ApiGet("/shop/getGoods");
            if (Code != 200)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Msg, "ERR");
                return;
            }
            JArray userData = JArray.Parse(Data.ToString());
            foreach (var item in userData)
            {
                Grid grid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                };

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock textBlock = new TextBlock
                {
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    Text = $"#{item["id"]} {item["name"]}\n{item["price"]}积分\n{item["description"]}",
                };
                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);

                var buyButton = new Button
                {
                    Tag = (int)item["id"],
                    Content = "购买",
                    Width = 80,
                };
                buyButton.Click += async (sender, e) =>
                {
                    var button = sender as Button;
                    button.IsEnabled = false;
                    await BuyGood((int)button.Tag);
                    button.IsEnabled = true;
                };
                Grid.SetColumn(buyButton, 1);
                grid.Children.Add(buyButton);

                GoodsList.Children.Add(grid);

            }
        }

        private async Task BuyGood(int id)
        {
            var parameterData = new Dictionary<string, string> { { "good", id.ToString() } };
            var (Code, _, Msg) = await MSLFrpApi.ApiPost("/shop/buy", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "提示");
                await GetUserInfo();
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
            }
        }

        private async void PayOrder_Click(object sender, RoutedEventArgs e)
        {
            PayOrder.IsEnabled = false;
            string payMethod = "alipay";
            if (PayMethod.SelectedIndex == 1)
            {
                payMethod = "wxpay";
            }
            var parameterData = new Dictionary<string, string> {
                { "price", AmountText.Text },
                { "pay", payMethod }
            };
            var (Code, Data, Msg) = await MSLFrpApi.ApiPost("/shop/pay", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                Image qrCodeImageBox = new()
                {
                    Width = 172,
                    Height = 172
                };
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(Data["payUrl"].ToString(), QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                using (System.Drawing.Bitmap qrCodeImage = qrCode.GetGraphic(10))
                {
                    qrCodeImageBox.Source = BitmapConverter.ConvertToImageSource(qrCodeImage);
                }
                if (await MagicShow.ShowMsgDialogAsync("订单号：" + Data["out_trade_no"], "支付", true, "取消支付", "支付完成", qrCodeImageBox))
                {
                    (Code, _, Msg) = await MSLFrpApi.ApiGet("/shop/getPayResult?order=" + Data["out_trade_no"]);
                    if (Code == 200)
                    {
                        MagicShow.ShowMsgDialog(Msg, "成功");
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Msg, "错误");
                    }
                    await GetUserInfo();
                }
            }
            else
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
            }
            PayOrder.IsEnabled = true;
        }

        private void AskBtn_Click(object sender, RoutedEventArgs e)
        {
            MagicShow.ShowMsgDialog("若您遇到了付款的问题，请加Q群1145888872联系管理。\n请勿直接在订单发起投诉，这样只会加长处理周期和麻烦。\n感谢您的配合~", "付款问题");
        }
    }
}
