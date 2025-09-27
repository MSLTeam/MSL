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
            LogHelper.Write.Info("MSLFrpProfile 页面加载，开始初始化...");

            // 获取Token并尝试登录
            var token = string.IsNullOrEmpty(MSLFrpApi.UserToken)
                ? Config.Read("MSLUserAccessToken")?.ToString()
                : MSLFrpApi.UserToken;

            if (string.IsNullOrEmpty(token))
            {
                LogHelper.Write.Warn("未找到本地或内存中的Token，显示登录页面。");
                ShowLoginControl();
                return;
            }
            else
            {
                LogHelper.Write.Info("找到Token，尝试使用Token自动登录。");
                if (await PerformLogin(token) == false)
                {
                    LogHelper.Write.Warn("Token自动登录失败，显示登录页面。");
                    ShowLoginControl();
                    return;
                }
            }
            MagicDialog magicDialog = new MagicDialog();
            magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
            LogHelper.Write.Info("登录成功，开始加载用户资料和商品信息。");
            await GetUserInfo();
            await GetGoods();
            magicDialog.CloseTextDialog();
            LogHelper.Write.Info("用户资料及商品信息加载完成。");
        }

        private void ShowLoginControl()
        {
            // 显示登录页面
            MSLFrpLogin loginControl = new MSLFrpLogin();
            loginControl.LoginSuccess += async delegate
            {
                LogHelper.Write.Info("接收到登录成功委托，开始执行登录后操作。");
                if (await PerformLogin(MSLFrpApi.UserToken) == true)
                {
                    LoginControl.Visibility = Visibility.Collapsed;
                    MainGrid.Visibility = Visibility.Visible;
                    MagicDialog magicDialog = new MagicDialog();
                    magicDialog.ShowTextDialog(Window.GetWindow(this), "加载信息……");
                    LogHelper.Write.Info("登录成功，开始加载用户资料和商品信息。");
                    await GetUserInfo();
                    await GetGoods();
                    magicDialog.CloseTextDialog();
                    LogHelper.Write.Info("用户资料及商品信息加载完成。");
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
            LogHelper.Write.Info("正在请求MSLFrp用户登录接口...");
            (int Code, string Msg, _) = await MSLFrpApi.UserLogin(token);

            magicDialog.CloseTextDialog();

            if (Code != 200)
            {
                LogHelper.Write.Error($"MSLFrp用户登录失败, Code: {Code}, Msg: {Msg}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "登陆失败！\n" + Msg, "错误");
                return false;
            }

            LogHelper.Write.Info("MSLFrp用户登录成功。");
            return true;
        }

        private async Task GetUserInfo()
        {
            LogHelper.Write.Info("开始获取用户信息...");
            var (Code, Data, Msg) = await MSLFrpApi.ApiGet("/user/info");
            if (Code != 200)
            {
                LogHelper.Write.Error($"获取用户信息失败, Code: {Code}, Msg: {Msg}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Msg, "ERR");
                ShowLoginControl();
                return;
            }
            LogHelper.Write.Info("获取用户信息成功，正在更新UI。");
            JObject userData = JObject.Parse(Data.ToString());
            Name_Label.Content = "用户名：" + userData["username"].ToString();
            Uid_Label.Content = "UID：" + ((int)userData["uid"]).ToString();
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
            LogHelper.Write.Info("用户点击实名认证按钮。");
            if (await MagicShow.ShowMsgDialogAsync("请选择实名方式: \n#MSL用户中心网页实名: 支持微信、支付宝实名(其中支付宝支持港澳台居民实名);\n#MSL内实名: 仅支持中国大陆居民身份证支付宝实名 (二维码较小，有扫不到的可能);\n实名费用: 会员免费支付宝实名，支付宝实名150积分，微信实名200积分。\n*特别提醒：由于用户自身填写错误的实名信息，积分一旦扣除将无法退还！", "提示", true, "MSL内实名(仅支持支付宝)", "MSL用户中心实名(支付宝/微信)"))
            {
                LogHelper.Write.Info("用户选择前往MSL用户中心网页进行实名。");
                Process.Start("https://user.mslmc.net/user/profile");
                return;
            }
            LogHelper.Write.Info("用户选择在MSL客户端内进行实名。");
            string certID = await MagicShow.ShowInput(Window.GetWindow(this), "请输入您的身份证号码", "");
            if (certID == null)
            {
                LogHelper.Write.Warn("用户取消输入身份证号。");
                return;
            }
            string certName = await MagicShow.ShowInput(Window.GetWindow(this), "请输入您的真实姓名", "");
            if (certName == null)
            {
                LogHelper.Write.Warn("用户取消输入真实姓名。");
                return;
            }

            var parameterData = new Dictionary<string, string> {
                { "cert_id", certID },
                { "cert_name", certName },
                { "cert_type", "IDENTITY_CARD" },
                { "verify_type", "alipay" }
            };
            if (!await MagicShow.ShowMsgDialogAsync($"请确认如下实名信息是否正确:\n姓名:{certName}\n身份证号码:{certID}\n确认无误后，请点击确认按钮进入支付宝扫码人脸识别流程\n*注意:非会员一旦确认提交信息，将会立即扣除150积分(若因用户自身填写错误的信息导致认证失败，实名小号积分不予退换。)，若需要取消上一次的实名订单，请加群联系管理员。(Q群" +
                $"信息在软件首页)", "实名信息确认", true, "取消", "我已确认信息无误"))
            {
                return;
            }
            LogHelper.Write.Info("开始提交实名认证信息...");
            RealnameVerify_Button.IsEnabled = false;
            var (Code, Data, Msg) = await MSLFrpApi.ApiPost("/user/submitRealNameVerify", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                LogHelper.Write.Info($"实名认证请求成功，返回URL: {Data["url"]}");
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
                    LogHelper.Write.Info("用户已扫码并点击'认证完成'，开始查询认证结果。");
                    (Code, Data, Msg) = await MSLFrpApi.ApiGet("/user/getRealNameVerifyResult");
                    if (Code == 200)
                    {
                        LogHelper.Write.Info($"查询实名认证结果成功, Passed: {(bool)Data["passed"]}");
                        if ((bool)Data["passed"])
                            MagicShow.ShowMsgDialog("实名认证成功！", "成功");
                        else
                            MagicShow.ShowMsgDialog("实名认证未通过！", "失败");
                    }
                    else
                    {
                        LogHelper.Write.Error($"查询实名认证结果失败, Code: {Code}, Msg: {Msg}");
                        MagicShow.ShowMsgDialog(Msg, "错误");
                    }
                    await GetUserInfo();
                }
                else
                {
                    LogHelper.Write.Warn("用户在扫码界面点击了'取消实名'。");
                }
            }
            else
            {
                LogHelper.Write.Error($"提交实名认证信息失败, Code: {Code}, Msg: {Msg}");
                MagicShow.ShowMsgDialog(Msg, "错误");
            }
            RealnameVerify_Button.IsEnabled = true;
        }

        private async Task GetGoods()
        {
            LogHelper.Write.Info("开始获取商品列表...");
            var (Code, Data, Msg) = await MSLFrpApi.ApiGet("/shop/getGoods");
            if (Code != 200)
            {
                LogHelper.Write.Error($"获取商品列表失败, Code: {Code}, Msg: {Msg}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), Msg, "ERR");
                return;
            }
            LogHelper.Write.Info("获取商品列表成功，正在渲染商品列表...");
            GoodsList.Children.Clear();

            JArray goodsData = JArray.Parse(Data.ToString());
            foreach (var item in goodsData)
            {
                var id = item.Value<int>("id");
                var name = item.Value<string>("name");
                var description = item.Value<string>("description");
                var price = item.Value<decimal>("price");
                var originPrice = item.Value<decimal?>("originPrice");

                Grid grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // 存放所有文字信息的垂直面板
                var textPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0) // 与购买按钮留出间距
                };

                // 商品名称
                var nameBlock = new TextBlock
                {
                    Text = $"#{id} {name}",
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                };
                textPanel.Children.Add(nameBlock);

                bool hasDiscount = originPrice.HasValue && originPrice.Value > price;

                if (hasDiscount)
                {
                    // 有折扣时的显示
                    var pricePanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // 现价
                    var salePriceBlock = new TextBlock
                    {
                        Text = $"{price}积分 ",
                        Foreground = new SolidColorBrush(Colors.OrangeRed), // 折扣价用醒目颜色
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 13
                    };
                    pricePanel.Children.Add(salePriceBlock);

                    // 原价
                    var originPriceBlock = new TextBlock
                    {
                        Text = $"{originPrice.Value}积分",
                        Foreground = (Brush)FindResource("SecondaryTextBrush"), // 灰色
                        TextDecorations = TextDecorations.Strikethrough,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 0, 0),
                        FontSize = 12
                    };
                    pricePanel.Children.Add(originPriceBlock);

                    // 折扣百分比标签
                    var discountPercentage = Math.Round(((originPrice.Value - price) / originPrice.Value) * 100);
                    var discountBadge = new Border
                    {
                        Background = new SolidColorBrush(Colors.Red),
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(10, 0, 0, 0),
                        Padding = new Thickness(4, 1, 4, 1),
                        Child = new TextBlock
                        {
                            Text = $"{discountPercentage}% OFF",
                            Foreground = new SolidColorBrush(Colors.White),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                        }
                    };
                    pricePanel.Children.Add(discountBadge);

                    textPanel.Children.Add(pricePanel);
                }
                else
                {
                    // 无折扣
                    var priceBlock = new TextBlock
                    {
                        Text = $"{price}积分",
                        Foreground = (Brush)FindResource("PrimaryTextBrush"),
                        FontSize = 13
                    };
                    textPanel.Children.Add(priceBlock);
                }

                // 描述
                var descriptionBlock = new TextBlock
                {
                    Text = description,
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                textPanel.Children.Add(descriptionBlock);

                // 文字面板添加到Grid第一列
                Grid.SetColumn(textPanel, 0);
                grid.Children.Add(textPanel);

                // 购买按钮
                var buyButton = new Button
                {
                    Tag = id,
                    Content = "兑换",
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

            LogHelper.Write.Info("商品列表渲染完成。");
        }

        private async Task BuyGood(int id)
        {
            LogHelper.Write.Info($"用户尝试购买商品, ID: {id}");
            var parameterData = new Dictionary<string, string> { { "good", id.ToString() } };
            var (Code, _, Msg) = await MSLFrpApi.ApiPost("/shop/buy", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                LogHelper.Write.Info($"商品购买成功, ID: {id}, Msg: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "提示");
                await GetUserInfo();
            }
            else
            {
                LogHelper.Write.Error($"商品购买失败, ID: {id}, Code: {Code}, Msg: {Msg}");
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
            LogHelper.Write.Info($"用户发起充值请求, 方式: {payMethod}, 金额: {AmountText.Text}");
            var (Code, Data, Msg) = await MSLFrpApi.ApiPost("/shop/pay", HttpService.PostContentType.FormUrlEncoded, parameterData);
            if (Code == 200)
            {
                LogHelper.Write.Info($"创建支付订单成功, 订单号: {Data["out_trade_no"]}, 支付URL: {Data["payUrl"]}");
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
                    LogHelper.Write.Info($"用户点击'支付完成'，开始查询订单支付结果, 订单号: {Data["out_trade_no"]}");
                    (Code, _, Msg) = await MSLFrpApi.ApiGet("/shop/getPayResult?order=" + Data["out_trade_no"]);
                    if (Code == 200)
                    {
                        LogHelper.Write.Info($"查询支付结果成功, Msg: {Msg}");
                        MagicShow.ShowMsgDialog(Msg, "成功");
                    }
                    else
                    {
                        LogHelper.Write.Error($"查询支付结果失败, Code: {Code}, Msg: {Msg}");
                        MagicShow.ShowMsgDialog(Msg, "错误");
                    }
                    await GetUserInfo();
                }
                else
                {
                    LogHelper.Write.Warn($"用户在支付界面点击了'取消支付', 订单号: {Data["out_trade_no"]}");
                }
            }
            else
            {
                LogHelper.Write.Error($"创建支付订单失败, Code: {Code}, Msg: {Msg}");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), Msg, "错误");
            }
            PayOrder.IsEnabled = true;
        }

        private void AskBtn_Click(object sender, RoutedEventArgs e)
        {
            MagicShow.ShowMsgDialog("若您遇到了付款的问题，请加Q群1023417539联系管理。\n请勿直接在订单发起投诉，这样只会加长处理周期和麻烦。\n感谢您的配合~", "付款问题");
        }
    }
}