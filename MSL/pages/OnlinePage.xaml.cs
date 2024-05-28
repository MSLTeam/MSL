using HandyControl.Controls;
using MSL.controls;
using MSL.i18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// OnlinePage.xaml 的交互逻辑
    /// </summary>
    public partial class OnlinePage : Page
    {
        //public delegate void DelReadStdOutput(string result);
        public static Process FRPCMD = new Process();
        //public event DelReadStdOutput ReadStdOutput;
        //string _dnfrpc;
        private bool isMaster;
        private string ipAddress = "";
        private string ipPort = "";

        public OnlinePage()
        {
            InitializeComponent();
            FRPCMD.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            //ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    visiterExp.IsExpanded = true;
                }
                else
                {
                    masterExp.IsExpanded = true;
                }
            }
            else
            {
                //Shows.ShowMsgDialog(Window.GetWindow(this),"注意：此功能目前不稳定，无法穿透所有类型的NAT，若联机失败，请尝试开服务器并使用内网映射联机！\r\n该功能可能需要正版账户，若无法联机，请从网络上寻找解决方法或尝试开服务器并使用内网映射联机！", "警告");
                Shows.ShowMsgDialog(Window.GetWindow(this), LanguageManager.Instance["Pages_OnlinePage_Dialog_Tips"], LanguageManager.Instance["Dialog_Warning"]);
                masterExp.IsExpanded = true;
            }
            Thread thread = new Thread(GetFrpcInfo);
            thread.Start();
        }

        private void GetFrpcInfo()
        {
            try
            {
                string mslFrpInfo = Functions.Get("query/MSLFrps");
                JObject valuePairs = (JObject)JsonConvert.DeserializeObject(mslFrpInfo);
                foreach (var valuePair in valuePairs)
                {
                    string serverInfo = valuePair.Key;
                    JObject serverDetails = (JObject)valuePair.Value;
                    foreach (var value in serverDetails)
                    {
                        string serverName = value.Key;
                        string serverAddress = value.Value["server_addr"].ToString();
                        string serverPort = value.Value["server_port"].ToString();
                        string minPort = value.Value["min_open_port"].ToString();
                        string maxPort = value.Value["max_open_port"].ToString();

                        ipAddress = serverAddress;
                        ipPort = serverPort;
                        break;
                    }
                    break;
                }
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(ipAddress, 2000); //一个可爱的ip，ping一下
                if (reply.Status == IPStatus.Success)
                {
                    //服务器活着，太好了！
                    Dispatcher.Invoke(() =>
                    {
                        serverState.Text = LanguageManager.Instance["Pages_Online_ServerStatusOK"];
                    });
                }
                else
                {
                    //跑路了.jpg
                    Dispatcher.Invoke(() =>
                    {
                        serverState.Text = LanguageManager.Instance["Pages_Online_ServerStatusDown"];
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    serverState.Text = LanguageManager.Instance["Pages_Online_ServerStatusDown"];
                });
            }
        }
        private void masterExp_Expanded(object sender, RoutedEventArgs e)
        {
            visiterExp.IsExpanded = false;
            if (File.Exists("MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 == 0)
                {
                    string pattern = @"\[(\w+)\]\s*type\s*=\s*xtcp\s*local_ip\s*=\s*(\S+)\s*local_port\s*=\s*(\d+)\s*sk\s*=\s*(\S+)";
                    Match match = Regex.Match(a, pattern);
                    if (match.Success)
                    {
                        masterQQ.Text = match.Groups[1].Value;
                        masterKey.Text = match.Groups[4].Value;
                        masterPort.Text = match.Groups[3].Value;
                    }
                }
            }
        }
        private void visiterExp_Expanded(object sender, RoutedEventArgs e)
        {
            masterExp.IsExpanded = false;
            if (File.Exists("MSL\\P2Pfrpc"))
            {
                string a = File.ReadAllText("MSL\\P2Pfrpc");
                if (a.IndexOf("role = visitor") + 1 != 0)
                {
                    string pattern = @"server_name\s*=\s*(\S+)\s*sk\s*=\s*(\S+)\s*bind_port\s*=\s*(\d+)";
                    Match match = Regex.Match(a, pattern);
                    if (match.Success)
                    {
                        visiterQQ.Text = match.Groups[1].Value;
                        visiterKey.Text = match.Groups[2].Value;
                        visiterPort.Text = match.Groups[3].Value;
                    }
                }
            }
        }


        private void createRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (createRoom.Content.ToString() != LanguageManager.Instance["Pages_Online_Close"])
                {
                    string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[" + masterQQ.Text + "]\r\ntype = xtcp\r\nlocal_ip = 127.0.0.1\r\nlocal_port = " + masterPort.Text + "\r\nsk = " + masterKey.Text + "\r\n";
                    File.WriteAllText("MSL\\P2Pfrpc", a);
                    isMaster = true;
                    visiterExp.IsEnabled = false;
                    StartFrpc();
                }
                else
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    visiterExp.IsEnabled = true;
                    Growl.Success(LanguageManager.Instance["Pages_Online_CloseSuc"]);
                    FRPCMD.CancelOutputRead();
                    //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    createRoom.Content = LanguageManager.Instance["Pages_Online_CreateBtn"];
                }
            }
            catch
            {
                try
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    FRPCMD.CancelOutputRead();
                    //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                }
                catch
                {
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    { }
                }
                createRoom.Content = LanguageManager.Instance["Pages_Online_CreateBtn"];
            }
        }

        private void joinRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (joinRoom.Content.ToString() != LanguageManager.Instance["Pages_Online_ExitRoom"])
                {
                    string a = "[common]\r\nserver_port = " + ipPort + "\r\nserver_addr = " + ipAddress + "\r\n\r\n[p2p_ssh_visitor]\r\ntype = xtcp\r\nrole = visitor\r\nbind_addr = 127.0.0.1\r\nbind_port = " + visiterPort.Text + "\r\nserver_name = " + visiterQQ.Text + "\r\nsk = " + visiterKey.Text + "\r\n";
                    File.WriteAllText("MSL\\P2Pfrpc", a);
                    isMaster = false;
                    masterExp.IsEnabled = false;
                    StartFrpc();
                }
                else
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    masterExp.IsEnabled = true;
                    Growl.Success(LanguageManager.Instance["Pages_Online_CloseSuc"]);
                    FRPCMD.CancelOutputRead();
                    //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    joinRoom.Content = LanguageManager.Instance["Pages_Online_EnterBtn"];
                }
            }
            catch
            {
                try
                {
                    FRPCMD.Kill();
                    Thread.Sleep(200);
                    FRPCMD.CancelOutputRead();
                    //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                }
                catch
                {
                    try
                    {
                        FRPCMD.CancelOutputRead();
                        //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    { }
                }
                joinRoom.Content = LanguageManager.Instance["Pages_Online_EnterBtn"];
            }
        }

        private async void StartFrpc()
        {
            try
            {
                //内网映射版本检测
                try
                {
                    StreamReader reader = File.OpenText("MSL\\config.json");
                    JObject jobject2 = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    reader.Close();
                    if (jobject2["frpcversion"] == null)
                    {
                        string jsonString = File.ReadAllText(@"MSL\config.json", Encoding.UTF8);
                        JObject jobject = JObject.Parse(jsonString);
                        jobject.Add("frpcversion", "6");
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                        if (!File.Exists("MSL\\frpc.exe"))
                        {
                            string _dnfrpc = Functions.Get("/download/frpc/MSLFrp/amd64");
                            await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", "frpc.exe", LanguageManager.Instance["Pages_Online_DlFrpc"]);
                        }
                    }
                    else if (jobject2["frpcversion"].ToString() != "6")
                    {
                        string _dnfrpc = Functions.Get("/download/frpc/MSLFrp/amd64");
                        await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", "frpc.exe", LanguageManager.Instance["Pages_Online_UdFrpc"]);
                        JObject jobject3 = JObject.Parse(File.ReadAllText("MSL\\config.json", Encoding.UTF8));
                        jobject3["frpcversion"] = "6";
                        string convertString2 = Convert.ToString(jobject3);
                        File.WriteAllText("MSL\\config.json", convertString2, Encoding.UTF8);
                    }
                    else if (!File.Exists("MSL\\frpc.exe"))
                    {
                        string _dnfrpc = Functions.Get("/download/frpc/MSLFrp/amd64");
                        await Shows.ShowDownloader(Window.GetWindow(this), _dnfrpc, "MSL", "frpc.exe", LanguageManager.Instance["Pages_Online_DlFrpc"]);
                    }
                }
                catch
                {
                    return;
                }
                if (isMaster)
                {
                    createRoom.Content = LanguageManager.Instance["Pages_Online_Close"];
                }
                else
                {
                    joinRoom.Content = LanguageManager.Instance["Pages_Online_ExitRoom"];
                }
                frpcOutlog.Text = "";
                //Directory.SetCurrentDirectory("MSL");
                FRPCMD.StartInfo.WorkingDirectory = "MSL";
                FRPCMD.StartInfo.FileName = "MSL\\frpc.exe";
                FRPCMD.StartInfo.Arguments = "-c P2Pfrpc";
                FRPCMD.StartInfo.CreateNoWindow = true;
                FRPCMD.StartInfo.UseShellExecute = false;
                FRPCMD.StartInfo.RedirectStandardInput = true;
                FRPCMD.StartInfo.RedirectStandardOutput = true;
                FRPCMD.Start();
                FRPCMD.BeginOutputReadLine();
            }
            catch (Exception e)
            {
                MessageBox.Show(LanguageManager.Instance["Pages_Online_ErrMsg1"] + e.Message, LanguageManager.Instance["Dialog_Err"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() =>
                {
                    ReadStdOutputAction(e.Data);
                });
            }
        }
        private void ReadStdOutputAction(string msg)
        {
            frpcOutlog.Text = frpcOutlog.Text + msg + "\n";
            if (msg.IndexOf("login") + 1 != 0)
            {
                if (msg.IndexOf("failed") + 1 != 0)
                {
                    Growl.Error(LanguageManager.Instance["Pages_Online_Err"]);
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        }
                        catch
                        { }
                    }
                    if (isMaster)
                    {
                        createRoom.Content = LanguageManager.Instance["Pages_Online_CreateBtn"];
                        visiterExp.IsEnabled = true;
                    }
                    else
                    {
                        joinRoom.Content = LanguageManager.Instance["Pages_Online_EnterBtn"];
                        masterExp.IsEnabled = true;
                    }
                }
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Pages_Online_LoginSuc"] + "\n";
                }
            }
            if (msg.IndexOf("start") + 1 != 0)
            {
                if (msg.IndexOf("success") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Pages_Online_Suc"] + "\n";
                    Growl.Success(LanguageManager.Instance["Pages_Online_Suc"]);
                }
                if (msg.IndexOf("error") + 1 != 0)
                {
                    frpcOutlog.Text = frpcOutlog.Text + LanguageManager.Instance["Pages_Online_Err"] + "\n";
                    Growl.Error(LanguageManager.Instance["Pages_Online_Err"]);
                    try
                    {
                        FRPCMD.Kill();
                        Thread.Sleep(200);
                        FRPCMD.CancelOutputRead();
                        //ReadStdOutput = null;
                        //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                    }
                    catch
                    {
                        try
                        {
                            FRPCMD.CancelOutputRead();
                            //ReadStdOutput = null;
                            //FRPCMD.OutputDataReceived -= new DataReceivedEventHandler(p_OutputDataReceived);
                        }
                        catch
                        { }
                    }
                    if (isMaster)
                    {
                        createRoom.Content = LanguageManager.Instance["Pages_Online_CreateBtn"];
                        visiterExp.IsEnabled = true;
                    }
                    else
                    {
                        joinRoom.Content = LanguageManager.Instance["Pages_Online_EnterBtn"];
                        masterExp.IsEnabled = true;
                    }
                }
            }
            frpcOutlog.ScrollToEnd();
        }

    }
}
