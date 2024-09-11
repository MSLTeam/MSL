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

        /*
        private void WebBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        */

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("MSL\\frp");
            int number = Functions.Frpc_GenerateRandomInt();
            if (EasyMode.IsChecked == true)
            {
                //可视化模式
                //检测必备数据写了没，防止小傻瓜
                if (ServerIP.Text == "" || ServerPort.Text == "" || ClientRemotePort.Text == "" || ClientIP.Text == "" || ClientPort.Text == "")
                {
                    Shows.ShowMsg(Window.GetWindow(this), "存在未填写的数据！\n请检查！", "错误！");
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
                    Directory.CreateDirectory("MSL\\frp\\" + number);
                    File.WriteAllText($"MSL\\frp\\{number}\\frpc.toml", FrpcConfig);
                    SetFrpcPath(number);
                }
            }
            else
            {
                //直接丢配置文件模式
                Directory.CreateDirectory("MSL\\frp\\" + number);
                File.WriteAllText($"MSL\\frp\\{number}\\frpc.toml", ConfigBox.Text);
                SetFrpcPath(number);
            }
        }

        private async void SetFrpcPath(int number)
        {
            string sn = await Shows.ShowInput(Window.GetWindow(this), "给此隧道取一个名字吧：", "我的自定义Frp节点");
            if (sn == null)
            {
                return;
            }
            Directory.CreateDirectory(@"MSL\frp");
            if (!File.Exists(@"MSL\frp\config.json"))
            {
                //Logger.LogWarning("未检测到config.json文件，创建config.json……");
                File.WriteAllText(@"MSL\frp\config.json", string.Format("{{{0}}}", "\n"));
            }
            
            JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
            if (CustomFrpcClient.IsChecked == true)//自定义的话要导入进MSL文件夹
            {
                //选择文件对还款
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "exe应用程序 (*.exe)|*.exe";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "请选择您的Frpc客户端";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //文件路径
                    string filePath = openFileDialog.FileName;
                    File.Copy(filePath, @"MSL/frp/frpc_custom.exe", true);
                    JObject keyValues = new JObject()
                    {
                        ["frpcServer"] = "-2",
                        ["name"] = "自定义隧道 - " + sn
                    };
                    jobject.Add(number.ToString(), keyValues);
                    //自定义模式-自定义frpc客户端
                }
                else
                {
                    return;
                }
            }
            else
            {
                JObject keyValues = new JObject()
                {
                    ["frpcServer"] = "-1",
                    ["name"] = "自定义隧道(官方客户端) - " + sn
                };
                jobject.Add(number.ToString(), keyValues);
                //自定义模式-Gh上的客户端
            }
            //最后结束
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
            await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "隧道配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            Window.GetWindow(this).Close();
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
