using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MSL.pages.frpProviders
{
    /// <summary>
    /// Custom.xaml 的交互逻辑
    /// </summary>
    public partial class Custom : Page
    {
        public Custom()
        {
            InitializeComponent();
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Write.Info("开始处理自定义Frp配置创建。");
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                int number = Functions.Frpc_GenerateRandomInt();
                LogHelper.Write.Info($"为新隧道生成了随机ID: {number}。");

                if (EasyMode.IsChecked == true)
                {
                    LogHelper.Write.Info("当前为可视化(EasyMode)配置模式。");
                    //可视化模式
                    //检测必备数据写了没，防止小傻瓜
                    if (ServerIP.Text == "" || ServerPort.Text == "" || ClientRemotePort.Text == "" || ClientIP.Text == "" || ClientPort.Text == "")
                    {
                        LogHelper.Write.Warn("用户提交的表单中存在未填写的必填项。");
                        MagicShow.ShowMsg(Window.GetWindow(this), "存在未填写的数据！\n请检查！", "错误！");
                    }
                    else
                    {
                        //既然都写了，那就继续
                        string FrpcConfig;
                        FrpcConfig = $"serverAddr = \"{ServerIP.Text}\"\n" +
                            $"serverPort = {ServerPort.Text}\n" +
                            $"user = \"{ServerUser.Text}\"\nauth.token = \"{ServerToken.Text}\"\n" +
                            $"dnsServer = \"{ServerDNS.Text}\"\n" +
                            $"transport.protocol = \"{ServerProtocol.Text}\"\n" +
                            $"transport.tcpMux = {ServerTcpMux.IsChecked.ToString().ToLower()}\n" +
                            $"transport.tls.enable = {ServerTls.IsChecked.ToString().ToLower()}\n\n" +
                            $"[[proxies]]\n" +
                            $"name = \"{ClientName.Text}\"\n" +
                            $"type = \"{ClientProtocol.Text}\"\r\nlocalIp = \"{ClientIP.Text}\"\n" +
                            $"localPort = {ClientPort.Text}\r\nremotePort = {ClientRemotePort.Text}\n" +
                            $"transport.useCompression = {ClientComp.IsChecked.ToString().ToLower()}\n" +
                            $"transport.useEncryption = {ClientEnc.IsChecked.ToString().ToLower()}\n" +
                            $"{moreData.Text}\n";

                        LogHelper.Write.Info($"隧道 {number} 的frpc.toml内容已生成，准备写入文件。");
                        Directory.CreateDirectory("MSL\\frp\\" + number);
                        File.WriteAllText($"MSL\\frp\\{number}\\frpc.toml", FrpcConfig);
                        LogHelper.Write.Info($"配置文件 frpc.toml 已成功写入到 MSL\\frp\\{number} 目录。");
                        SetFrpcPath(number);
                    }
                }
                else
                {
                    LogHelper.Write.Info("当前为直接配置文件(CustomMode)模式。");
                    //直接丢配置文件模式
                    Directory.CreateDirectory("MSL\\frp\\" + number);
                    File.WriteAllText($"MSL\\frp\\{number}\\frpc.toml", ConfigBox.Text);
                    LogHelper.Write.Info($"用户提供的直接配置文件 frpc.toml 已成功写入到 MSL\\frp\\{number} 目录。");
                    SetFrpcPath(number);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Fatal($"在 OKBtn_Click 事件处理中发生严重错误: {ex.ToString()}");
                MagicShow.ShowMsg(Window.GetWindow(this), "创建配置文件时发生未知错误，请检查日志！", "致命错误！");
            }
        }

        private async void SetFrpcPath(int number)
        {
            LogHelper.Write.Info($"开始为隧道 {number} 设置最终配置路径和名称。");
            string sn = await MagicShow.ShowInput(Window.GetWindow(this), "给此隧道取一个名字吧：", "我的自定义Frp节点");
            if (sn == null)
            {
                LogHelper.Write.Warn($"用户取消了为隧道 {number} 命名，配置流程中止。");
                return;
            }

            try
            {
                Directory.CreateDirectory(@"MSL\frp");
                if (!File.Exists(@"MSL\frp\config.json"))
                {
                    LogHelper.Write.Warn("未检测到 MSL\\frp\\config.json 文件，正在创建新文件。");
                    File.WriteAllText(@"MSL\frp\config.json", string.Format("{{{0}}}", "\n"));
                }

                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));

                if (CustomFrpcClient.IsChecked == true)//自定义的话要导入进MSL文件夹
                {
                    LogHelper.Write.Info("用户选择使用自定义Frpc客户端。");
                    //选择文件对话框
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "exe应用程序 (*.exe)|*.exe";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    openFileDialog.Title = "请选择您的Frpc客户端";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //文件路径
                        string filePath = openFileDialog.FileName;
                        LogHelper.Write.Info($"用户选择了Frpc客户端: {filePath}。");
                        File.Copy(filePath, @"MSL/frp/frpc_custom.exe", true);
                        LogHelper.Write.Info("自定义Frpc客户端已成功复制到 MSL/frp/frpc_custom.exe。");

                        JObject keyValues = new JObject()
                        {
                            ["frpcServer"] = -2,
                            ["name"] = "自定义隧道 - " + sn
                        };
                        jobject.Add(number.ToString(), keyValues);
                        LogHelper.Write.Info($"已为隧道 {number} 添加配置条目(自定义客户端模式)。");
                    }
                    else
                    {
                        LogHelper.Write.Warn("用户取消选择自定义Frpc客户端，配置流程中止。");
                        return;
                    }
                }
                else
                {
                    LogHelper.Write.Info("用户选择使用官方Frpc客户端。");
                    JObject keyValues = new JObject()
                    {
                        ["frpcServer"] = -1,
                        ["name"] = "自定义隧道(官方客户端) - " + sn
                    };
                    jobject.Add(number.ToString(), keyValues);
                    LogHelper.Write.Info($"已为隧道 {number} 添加配置条目(官方客户端模式)。");
                }

                //最后结束
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                LogHelper.Write.Info("最终配置 config.json 已成功保存。");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "隧道配置成功，请您点击“启动内网映射”以启动映射！", "信息");
                LogHelper.Write.Info($"隧道 {number} (名称: {sn}) 的配置流程已全部成功完成。");
                Window.GetWindow(this).Close();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Fatal($"在 SetFrpcPath 方法中发生严重错误: {ex.ToString()}");
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "保存最终配置时发生错误，请检查日志！", "致命错误！");
            }
        }

        private void EasyMode_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                LowLevelGrid.Visibility = Visibility.Visible;
                CustomGrid.Visibility = Visibility.Collapsed;
            }

        }

        private void CustomMode_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                LowLevelGrid.Visibility = Visibility.Collapsed;
                CustomGrid.Visibility = Visibility.Visible;
            }
        }
    }
}
// 最后是creeper镇楼⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡀⠤⠚⠉⢉⡑⠤⢀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠤⠊⠉⠠⣄⡀⠈⠓⢤⣀⠤⠈⠑⠢⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠤⠒⣉⠀⠀⠐⠀⠀⠀⠉⠐⠦⣉⠈⠁⠒⠤⣀⠀⠉⠐⠢⢄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⣤⠒⠙⠢⣄⣀⠀⠉⠢⣄⣠⠔⠂⠀⣀⠤⠚⠁⠀⠀⠀⠀⠑⠢⣄⡠⠔⠊⠒⣤⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⣀⠤⠲⢍⡀⠀⠈⠐⠧⣀⣀⠤⠒⢍⡀⠀⠈⠑⠫⣀⠀⠀⠀⠀⠀⣀⠥⠒⠯⣁⣀⠤⠒⠁⠀⢀⡨⠔⠤⣀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠙⠲⢤⣀⠈⠑⢢⡤⠔⠊⠑⣢⠤⠔⠊⠑⣢⠤⣔⠊⠁⣀⠤⠐⠉⠀⢀⡠⠐⠊⠑⠢⠤⣔⠊⠑⢢⡤⠔⠚⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⣄⡀⢈⠈⢱⠺⢥⣀⠀⠀⠫⢄⢀⠤⠒⠉⠀⠀⡠⠝⠫⢄⡀⡀⠔⠊⠁⠀⠀⠀⠀⠀⢀⣠⠽⡏⠁⠀⢀⡰⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠈⡏⠒⢼⠀⠀⠈⠉⠒⣖⡉⠀⢀⡠⠔⠊⠁⠀⠀⢖⡉⠀⢉⡲⠀⠀⠀⢀⣠⠔⠊⠁⠀⣠⡇⠀⠋⠁⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠐⠢⢇⡀⠀⠀⠀⠰⢄⡀⠇⠈⢳⠋⢄⣠⠔⠒⠂⢄⡀⠈⠛⠉⢀⡀⠄⢺⠉⠀⢀⡀⡔⠊⠁⡇⢀⡀⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⢸⣿⣶⢤⣀⠀⠀⠈⡗⠤⣼⠀⠀⠈⠑⢲⣎⠁⠀⣀⠴⠊⢹⠀⢀⣸⠀⠀⠁⠀⢀⣀⡤⠗⠉⡇⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⢸⣿⣿⡀⠈⡏⠐⠲⡇⠀⣀⠀⠀⠠⣀⢸⠀⠉⠉⠀⠀⣀⢼⠊⠁⢸⢠⡠⠔⠀⢸⠁⢀⣦⠔⠃⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠒⠤⣼⣿⣿⣿⣶⣧⣀⠀⡇⠀⢳⣤⣀⠀⠀⡏⠑⢢⡄⠀⢽⠀⢸⣀⠠⠾⠁⠀⠀⣀⠴⠒⠉⠀⠀⣀⡠⠴⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⢀⠀⠈⠉⢻⠿⣿⡇⠀⠉⠃⠀⢸⣿⣿⡏⠲⡇⠀⠀⡇⣀⡼⠚⠉⠃⠀⣠⠤⢲⠉⠀⠀⢀⡠⠔⠊⠁⠀⣀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠉⢲⢄⣸⠀⠈⢹⣦⣄⡀⠀⢸⣿⣿⣧⣀⡇⠀⠀⡇⠁⡇⠀⠀⠖⠊⠁⠀⢸⠀⠀⠘⠁⠀⠀⠀⡔⠈⠁⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠲⢄⣠⠀⢸⠓⠦⣼⣿⣿⣿⣷⡿⢿⣿⣿⣿⡗⠢⢄⡧⠚⡇⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⠈⠉⢺⣏⠀⢸⣿⣿⣿⣿⡷⠀⠈⡟⠻⡇⠀⠀⡇⢀⡇⠀⠈⠃⠀⢀⠀⢸⠀⠀⠀⠀⢀⡔⠋⠁⠀⢀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠉⠒⠴⢄⣸⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⣧⣀⡇⠀⠀⡗⠉⡇⠀⠀⡦⠒⡇⠀⢸⣀⠤⠆⠀⢸⡇⠀⢤⠖⠫⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⠸⢄⡀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠅⠈⡟⠢⢄⡇⠔⡇⠀⠀⡇⣠⢧⠖⢹⠀⢀⡆⠀⢸⡇⠀⢸⣀⡼⠇⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠈⠉⠒⢼⣿⣿⡏⠈⠙⠿⣿⣿⣿⣿⡄⠠⣇⠀⠀⡇⠀⡇⠀⠈⠃⠀⢸⣠⢼⠋⠁⠁⠀⢰⡧⠖⠋⠁⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡊⠙⡷⢄⡀⠀⠀⢹⣿⣿⣇⠀⠀⠉⠢⡗⠋⡇⠀⢀⠄⠀⠉⠀⢸⣀⡠⠖⠊⠑⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡇⠀⡏⠓⢮⣗⡢⣼⣿⣿⡏⠙⡆⠤⣀⣇⠀⣓⠊⠈⠀⢀⡴⠀⢸⠁⠀⣀⡠⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠉⠑⢧⣀⠀⡇⠀⠙⠦⣍⣦⣄⡇⠀⠀⡇⠀⣏⠤⠚⠆⠁⡇⢀⣸⠖⠈⠁⠀⢠⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⢠⡄⠉⡖⠤⡀⠀⠀⡏⠑⠏⠓⢤⠗⠊⢱⠦⣤⡤⠔⠻⠉⢸⠀⣀⠴⠖⢻⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠈⠓⠦⡇⠀⠁⠀⠀⠣⢀⠀⠀⠈⡗⠤⣸⡀⢸⠀⠀⠀⠀⠈⠉⠀⡀⠀⣼⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⢠⣀⠀⡇⠀⠰⢤⣀⠀⠀⠁⠀⠀⠇⠀⡇⠈⠙⠄⠀⠀⠀⢀⠔⠚⠏⠀⢨⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢄⡀⡀⠀⠉⡗⢤⣀⠀⢨⠑⠢⠄⠀⠀⠀⠈⢳⢄⣀⡄⠀⠀⠀⢸⠀⢀⣤⣴⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠑⠦⣄⡇⠀⠈⠉⠒⡄⠀⠀⠀⠀⠀⠠⣸⠀⢸⠀⠀⢀⣠⢾⣿⣿⣿⣿⡿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠒⠠⣤⣀⠀⠉⠒⠄⠀⠀⡇⠀⠀⠀⠀⠀⠀⢈⠙⠺⣴⠚⢹⡀⣸⠿⠛⠋⠁⠘⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣀⠀⡃⠈⠑⠢⢄⣄⠀⠀⠑⠢⣄⡀⠀⠀⠀⡼⢄⣀⡇⠀⠞⠋⠁⠀⢀⡄⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡆⠉⠳⠤⣀⠀⠀⠈⠁⠀⠀⠀⢸⠉⢹⠢⢄⡇⠀⠈⡇⠀⠀⠀⢠⠊⠁⠀⠀⣀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠓⠤⡄⠀⢈⡗⠢⡄⠀⠀⡀⠀⠘⠦⢼⡀⠈⡇⠀⠀⡇⠀⠀⠀⢸⣀⠠⠖⠋⠁⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡀⠀⠀⠀⠀⢇⡀⠀⠀⠀⠑⠠⠀⠀⠀⡏⠒⢧⣀⠀⡇⢀⡶⠚⢹⠁⠀⣀⣠⣴⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠉⠑⠲⠤⣀⠀⠉⡇⠀⠀⠀⠀⠀⠀⢠⠣⣀⢸⠈⠉⠉⠀⡇⢀⣸⣶⣾⡏⠀⣸⡧⢄⡀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠦⢄⡀⠀⠀⠙⠢⢇⡀⠀⠀⠀⢰⢄⣸⠀⠀⢹⠀⠀⣠⣴⡟⠁⢸⣿⠿⠟⠋⠀⣗⠊⠉⠑⠢⢄⡀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠆⠀⠁⠀⠀⣇⡀⠀⠈⠳⠲⢄⣸⠀⢸⠐⠢⣼⣀⠀⣿⣿⡧⠖⠉⠀⠀⢀⣠⢴⣧⠝⠒⠤⣀⠀⠈⠑⠢⢄
//⠀⠀⠀⠀⠀⢀⡠⠤⢖⡉⢀⡠⠴⢄⡀⡇⠈⢳⡤⣀⠀⠀⢸⠑⠺⢄⠀⢸⠈⠙⡏⠁⡇⠀⠀⠀⠀⠈⠀⢰⢋⡲⠤⢔⡁⠀⣀⠤⠒⠋
//⠀⠀⣀⡤⠚⠻⢄⣀⡠⠜⠣⢄⡀⠀⠈⠑⠤⣼⡇⢸⠉⠲⠼⠀⠀⠀⠉⠊⠀⠀⣧⠔⠃⠀⠀⠀⢀⡴⠚⠉⠃⠀⢀⣤⢾⡏⠁⠀⠀⠠
//⢾⡉⠀⢈⠵⠊⠉⠀⢈⡱⢮⡉⠀⣉⠖⠀⠀⠀⢉⡽⠂⠀⠀⠀⠀⠀⠀⣀⡀⠀⠀⠀⣤⠀⢀⠈⠀⢀⣠⠼⡖⠋⢹⠀⣸⡧⠒⣽⠀⠀
//⠀⠈⠙⠣⢤⣀⠀⠀⠁⣀⠤⠚⠉⠒⣠⣤⠒⠉⠓⣠⣤⠒⠉⠒⠤⢇⠀⡇⠉⠲⠖⠉⠀⢀⣸⡤⠒⠋⠀⠀⣇⡠⣼⠋⠀⠀⢀⣿⠀⠀
//⠀⠀⠀⠀⠀⠈⠑⡶⢍⡀⢀⠤⠒⠮⣁⣈⠵⠒⠮⣁⢈⠵⠢⢄⡀⠀⠉⠓⠤⣀⣄⣠⡞⠉⢹⠀⠤⣄⠀⠀⡟⠀⣟⡠⢔⡞⠁⠁⠀⠀
//⠀⠀⠀⠀⢰⠀⠀⡇⠀⢸⠷⠢⢤⡔⠊⠀⠀⠀⠔⠊⠁⣢⠤⣐⠉⠀⢀⡠⠔⢺⠁⠈⠱⠢⣼⠀⠀⠈⠑⠲⠗⠉⢁⠀⢀⡧⠀⢹⠀⠀
//⠀⠀⡟⠢⢼⢀⠀⡇⠀⢸⠀⠀⠀⠈⠑⡦⢀⣀⠠⠔⠊⠀⢀⣠⢽⠞⠉⠀⢀⣸⣶⣤⣄⠀⢸⠀⠀⠀⠀⠀⠀⠀⢸⠒⠉⠀⠀⠘⠀⠈
//⠀⠠⣇⠀⠀⠁⠙⠧⣀⢸⠀⠀⠀⠀⠀⡇⠀⠋⠑⠦⡴⠚⢹⠁⢸⡠⠔⣿⠉⠀⣿⣿⣿⣿⣶⣤⣀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀
//⣀⠀⠈⠉⢲⠀⠀⠀⠀⢹⠓⠢⣄⡀⠀⡁⠀⠀⠀⠀⡇⣠⡜⠊⢹⠀⠀⣿⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⠀⠀⢼⠀⢀⣀⠀⠀⠀⠀
//⣿⣷⣶⣄⣸⠀⠀⠀⠀⠘⣄⠀⠀⠈⠑⡆⢀⡆⠀⠀⡏⠀⣇⣠⠼⡖⠋⢻⠀⠀⠻⢿⣿⣿⣿⣿⣿⣿⣿⣿⡇⣀⣸⠒⠉⡇⠀⢀⡀⠐
//⣿⣿⣿⣿⣿⣿⣦⣄⡀⠀⠀⠈⠳⢤⣀⡇⠀⠈⠒⠤⠷⠊⢁⠀⢀⣇⠔⢻⠀⠀⠀⠀⠈⠙⠻⢿⣿⣿⣿⣿⣿⣿⡿⢀⣠⠗⠊⠁⠀⠀
//⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣄⡀⠀⠈⡇⠀⠀⠀⠀⠀⠀⢸⠔⠋⠀⠀⢸⠤⠚⠀⠀⠀⠀⠀⠀⠈⠙⠻⢿⡿⠟⠛⠉⠀⠀⠀⠀⠀⠀
//⠈⠙⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣅⡀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣄⡀⠀⢼⠀⠀⠀⠀⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢀⣼⠔⠊⡇⠀⣀⣀⠼⠂⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣶⣿⡇⠀⣠⠧⠚⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⠿⠓⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
