using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace MSL.forms
{
    /// <summary>
    /// CreateServer.xaml 的交互逻辑
    /// </summary>
    public partial class CreateServer : Window
    {
        string DownjavaName;

        string servername;
        string serverjava;
        string serverbase;
        string servercore;
        string servermemory;
        string serverargs;

        public CreateServer()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int a = 1; a != 0; a++)
            {
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server") && !(Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server").Length > 0 || Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server").Length > 0))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server";
                    return;
                }
                else if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server"))
                {
                    //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server");
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server";
                    return;
                }
                else if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString()) && !(Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString()).Length > 0 || Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString()).Length > 0))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString();
                    return;
                }
                else if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString()))
                {
                    //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString());
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString();
                    return;
                }
            }
        }

        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    //采用递归的方法实现
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
        private void next3_Click(object sender, RoutedEventArgs e)
        {
            if (useJVself.IsChecked == true)
            {
                serverjava = txjava.Text;
                //MainWindow.serverserver = txb3.Text;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                CheckServerPackCore();
            }
            else if (usejvPath.IsChecked == true)
            {
                serverjava = "Java";
                //MainWindow.serverserver = txb3.Text;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                CheckServerPackCore();
            }
            else if (usecheckedjv.IsChecked == true)
            { 
                string a = selectCheckedJavaComb.Items[selectCheckedJavaComb.SelectedIndex].ToString();
                serverjava = a.Substring(a.IndexOf(":")+1);
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                CheckServerPackCore();
            }
            else if (usedownloadjv.IsChecked==true)
            {
                try
                {
                    WebClient MyWebClient = new WebClient();
                    byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/otherdownload.json");
                    string _javaList = Encoding.UTF8.GetString(pageData);

                    JObject javaList0 = JObject.Parse(_javaList);
                    JObject javaList = (JObject)javaList0["java"];

                    next3.IsEnabled = false;
                    return5.IsEnabled = false;
                    if (usedownloadjv.IsChecked == true)
                    {
                        try
                        {
                            switch (selectJavaComb.SelectedIndex)
                            {
                                case 0:
                                    DownloadJava("Java8", javaList["Java8"].ToString());
                                    break;
                                case 1:
                                    DownloadJava("Java11", javaList["Java11"].ToString());
                                    break;
                                case 2:
                                    DownloadJava("Java16", javaList["Java16"].ToString());
                                    break;
                                case 3:
                                    DownloadJava("Java17", javaList["Java17"].ToString());
                                    break;
                                case 4:
                                    DownloadJava("Java18", javaList["Java18"].ToString());
                                    break;
                                case 5:
                                    DownloadJava("Java19", javaList["Java19"].ToString());
                                    break;
                                default:
                                    Growl.Error("请选择一个版本以下载！");
                                    break;
                            }
                        }
                        catch
                        {
                            Growl.Error("出现错误，请检查网络连接！");
                        }
                    }
                }
                catch
                {
                    next3.IsEnabled = true;
                    return5.IsEnabled = true;
                    DialogShow.ShowMsg(this, "出现错误！请检查您的网络连接！", "信息", false, "确定");
                }
            }
        }
        void CheckServerPackCore()
        {
            if (isImportPack)
            {
                if (File.Exists(serverbase + "\\server.jar"))
                {
                    DialogShow.ShowMsg(this, "开服器在整合包中检测到了服务端核心文件server.jar，是否选择此文件为开服核心？", "提示", true, "取消");
                    if (MessageDialog._dialogReturn == true)
                    {
                        MessageDialog._dialogReturn = false;
                        servercore = "server.jar";
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                    }
                }
                else if (File.Exists(serverbase + "\\Server.jar"))
                {
                    DialogShow.ShowMsg(this, "开服器在整合包中检测到了服务端核心文件Server.jar，是否选择此文件为开服核心？", "提示", true, "取消");
                    if (MessageDialog._dialogReturn == true)
                    {
                        MessageDialog._dialogReturn = false;
                        servercore = "Server.jar";
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                    }
                }
                else
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，您可进行手动选择，若找不到的话，请选择下载选项并点击下一步以下载");
                }
            }
        }
        private void DownloadJava(string fileName, string downUrl,bool changePage=true)
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
            {
                DialogShow.ShowMsg(this, "下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false, "确定");
                DownjavaName = fileName;
                DialogShow.ShowDownload(this, downUrl, AppDomain.CurrentDomain.BaseDirectory + "MSL", "Java.zip", "下载" + fileName + "中……");
                if(changePage)
                {
                    outlog.Content = "解压中...";
                    try
                    {
                        string javaDirName = "";
                        using (ZipFile zip = new ZipFile(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip"))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                if (entry.IsDirectory == true)
                                {
                                    int c0 = entry.Name.Length - entry.Name.Replace("/", "").Length;
                                    if (c0 == 1)
                                    {
                                        javaDirName = entry.Name.Replace("/", "");
                                        break;
                                    }
                                }
                            }
                        }
                        FastZip fastZip = new FastZip();
                        fastZip.ExtractZip(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip", AppDomain.CurrentDomain.BaseDirectory + "MSL", "");
                        outlog.Content = "解压完成，移动中...";
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip");
                        if (AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName != AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName)
                        {
                            MoveFolder(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName, AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName);
                        } 
                        while (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe"))
                        {
                            Thread.Sleep(1000);
                        }
                        outlog.Content = "完成";
                        serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe";
                        next3.IsEnabled = true;
                        return5.IsEnabled = true;
                        javagrid.Visibility = Visibility.Hidden;
                        servergrid.Visibility = Visibility.Visible;
                        CheckServerPackCore();
                    }
                    catch
                    {
                        MessageBox.Show("安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        outlog.Content = "安装失败！";
                        next3.IsEnabled = true;
                        return5.IsEnabled = true;
                    }
                }
                else
                {
                    try
                    {
                        string javaDirName = "";
                        using (ZipFile zip = new ZipFile(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip"))
                        {
                            foreach (ZipEntry entry in zip)
                            {
                                if (entry.IsDirectory == true)
                                {
                                    int c0 = entry.Name.Length - entry.Name.Replace("/", "").Length;
                                    if (c0 == 1)
                                    {
                                        javaDirName = entry.Name.Replace("/", "");
                                        break;
                                    }
                                }
                            }
                        }
                        FastZip fastZip = new FastZip();
                        fastZip.ExtractZip(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip", AppDomain.CurrentDomain.BaseDirectory + "MSL", "");
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip");
                        if (AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName != AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName)
                        {
                            MoveFolder(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName, AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName);
                        }
                        while(!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe"))
                        {
                            Thread.Sleep(1000);
                        }
                        serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe";
                        MessageBox.Show("安装成功", "success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        MessageBox.Show("安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe";
                if (changePage)
                {
                    next3.IsEnabled = true;
                    return5.IsEnabled = true;
                    javagrid.Visibility = Visibility.Hidden;
                    servergrid.Visibility = Visibility.Visible;
                    CheckServerPackCore();
                }
                else
                {
                    MessageBox.Show("安装成功", "success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        private void return2_Click(object sender, RoutedEventArgs e)
        {
            javagrid.Visibility = Visibility.Visible;
            servergrid.Visibility = Visibility.Hidden;
        }
        private void usedefault_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                txb4.IsEnabled = false;
                txb5.IsEnabled = false;
            }
        }

        private void useJVM_Checked(object sender, RoutedEventArgs e)
        {
            txb4.IsEnabled = true;
            txb5.IsEnabled = true;
        }

        private void done_Click(object sender, RoutedEventArgs e)
        {
            if (usedefault.IsChecked == true)
            {
                servermemory = "";
            }
            else
            {
                servermemory = "-Xms" + txb4.Text + "M -Xmx" + txb5.Text + "M";
            }
            serverargs += txb7.Text;
            try
            {
                if (!Directory.Exists(serverbase))
                {
                    Directory.CreateDirectory(serverbase);
                }

                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json"))
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", string.Format("{{{0}}}", "\n"));
                }
                JObject _json = new JObject
                {
                    { "name", servername },
                    { "java", serverjava },
                    { "base", serverbase },
                    { "core", servercore },
                    { "memory", servermemory },
                    { "args", serverargs }
                };
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                List<string> keys = jsonObject.Properties().Select(p => p.Name).ToList();
                var _keys = keys.Select(x => Convert.ToInt32(x));
                int[] ikeys = _keys.ToArray();
                Array.Sort(ikeys);
                int i = 0;
                
                foreach (int key in ikeys)
                {
                    if (i == key)
                    {
                        //jsonObject.Add(i.ToString(), _json);
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
                    jsonObject.Add(i.ToString(), _json);
                /*
                JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                int i = 0;
                foreach (var item in jsonObject)
                {
                    if (item.Key == i.ToString())
                    {
                        i++;
                    }
                }
                jsonObject.Add(i.ToString(),_json);
                */
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);

                //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", text + "*|-n " + MainWindow.servername + "|-j " + MainWindow.serverjava + "|-s " + MainWindow.serverserver + "|-a " + MainWindow.serverJVM + "|-b " + MainWindow.serverbase + "|-c " + MainWindow.serverJVMcmd + "|*\n");
                DialogShow.ShowMsg(this, "创建完毕，请点击“开启服务器”按钮以开服", "信息");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void return3_Click(object sender, RoutedEventArgs e)
        {
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            sJVM.IsEnabled = false;
        }

        private void a0002_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = serverbase;
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                txb3.Text = openfile.FileName;
            }
        }

        private void a0003_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txb6.Text = dialog.SelectedPath;
            }
        }

        private void return5_Click(object sender, RoutedEventArgs e)
        {
            welcome.IsSelected = true;
            welcome.IsEnabled = true;
            sserver.IsEnabled = false;
        }

        private void a0002_Copy_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件，通常为java.exe";
            openfile.Filter = "EXE文件|*.exe|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                txjava.Text = openfile.FileName;
            }
        }
        private void usedownloadjv_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                txjava.IsEnabled = false;
                a0002_Copy.IsEnabled = false;
            }
        }
        private void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = false;
            a0002_Copy.IsEnabled = false;
            selectCheckedJavaComb.Items.Clear();
            CheckJava();
        }
        private void usejvPath_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = false;
            a0002_Copy.IsEnabled = false;
            Process process = new Process();
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = "-version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow=true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Match match = Regex.Match(output, @"java version \""([\d\._]+)\""");
            if (match.Success)
            {
                outlog.Content = "环境变量可用性检查完毕，您的环境变量正常！";
                usejvPath.Content = "使用环境变量:" + "Java" + match.Groups[1].Value;
            }
            else 
            {
                DialogShow.ShowMsg(this, "检测环境变量失败，您的环境变量似乎不存在！", "错误");
                usedownloadjv.IsChecked = true;
            }
        }
        private void useJVself_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = true;
            a0002_Copy.IsEnabled = true;
        }
        private void usedownloadserver_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                txb3.IsEnabled = false;
                a0002.IsEnabled=false;
            }
        }
        private void useServerself_Checked(object sender, RoutedEventArgs e)
        {
            txb3.IsEnabled = true;
            a0002.IsEnabled = true;
        }
        void CheckJava()
        {
            string[] javaPaths = new string[] 
            {
                @"C:\Program Files\Java",
                @"C:\Java",
                @"D:\Program Files\Java",
                @"D:\Java",
                @"E:\Program Files\Java",
                @"E:\Java",
                @"F:\Program Files\Java",
                @"F:\Java",
                @"G:\Program Files\Java",
                @"G:\Java"
            };
            foreach (string javaPath in javaPaths)
            {
                if (Directory.Exists(javaPath))
                {
                    string[] javaVersions = Directory.GetDirectories(javaPath);
                    foreach (string version in javaVersions)
                    {
                        string javaExePath = Path.Combine(version, "bin\\java.exe");
                        if (File.Exists(javaExePath))
                        {
                            Process process = new Process();
                            process.StartInfo.FileName = javaExePath;
                            process.StartInfo.Arguments = "-version";
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.Start();

                            string output = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            Match match = Regex.Match(output, @"java version \""([\d\._]+)\""");
                            if (match.Success)
                            {
                                selectCheckedJavaComb.Items.Add("Java" + match.Groups[1].Value + ":" + javaExePath);
                            }
                        }
                    }
                }
            }
            if(selectCheckedJavaComb.Items.Count > 0)
            {
                outlog.Content = "检测完毕！";
                selectCheckedJavaComb.SelectedIndex = 0;
            }
            else
            {
                outlog.Content = "检测完毕，暂未找到Java";
                usedownloadjv.IsChecked = true;
            }
        }
        private void next_Click(object sender, RoutedEventArgs e)
        {
            servername = serverNameBox.Text;
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                var result = MessageBox.Show("使用带有中文的路径可能造成编码错误，导致无法开服，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if (txb6.Text.IndexOf(" ") + 1 != 0)
            {
                var result = MessageBox.Show("使用带有空格的路径可能造成编码错误，导致无法开服，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            if (Path.IsPathRooted(txb6.Text))
            {
                serverbase = txb6.Text;
            }
            else
            {
                txb6.Text = AppDomain.CurrentDomain.BaseDirectory + txb6.Text;
                serverbase = txb6.Text;
            }
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            welcome.IsEnabled = false;
        }

        bool isImportPack = false;
        private void importPack_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "如果您要导入的是模组整合包，请确保您下载的整合包是服务器专用包（如RlCraft下载界面就有一个ServerPack的压缩包），否则可能会出现无法开服或者崩溃的问题！", "提示", true, "取消");
            if (MessageDialog._dialogReturn == true)
            {
                MessageDialog._dialogReturn = false;
                servername = serverNameBox.Text;
                string serverPath = "";
                for (int a = 1; a != 0; a++)
                {
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server"))
                    {
                        serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                        break;
                    }
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString()))
                    {
                        serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                        break;
                    }
                }
                OpenFileDialog openfile = new OpenFileDialog();
                openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "MSL";
                openfile.Title = "请选择整合包压缩文件";
                openfile.Filter = "ZIP文件|*.zip|所有文件类型|*.*";
                var res = openfile.ShowDialog();
                if (res == true)
                {
                    new FastZip().ExtractZip(openfile.FileName, serverPath, "");
                    DirectoryInfo[] dirs = new DirectoryInfo(serverPath).GetDirectories();
                    if (dirs.Length == 1)
                    {
                        MoveFolder(dirs[0].FullName, serverPath);
                    }
                    isImportPack = true;
                    serverbase = serverPath;
                    Growl.Info("整合包解压完成，接下来请按照正常步骤进行操作");
                    sserver.IsSelected = true;
                    sserver.IsEnabled = true;
                    welcome.IsEnabled = false;
                    return5.IsEnabled = false;
                }
            }
        }


        private void next2_Click(object sender, RoutedEventArgs e)
        {
            if (usedownloadserver.IsChecked == true)
            {
                DownloadServer.downloadServerJava = serverjava;
                DownloadServer.downloadServerBase = serverbase;
                DownloadServer downloadServer = new DownloadServer();
                downloadServer.Owner = this;
                downloadServer.ShowDialog();
                if (File.Exists(serverbase + @"\" + DownloadServer.downloadServerName))
                {
                    servercore = DownloadServer.downloadServerName;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                }
                else if (DownloadServer.downloadServerArgs != "")
                {
                    servercore = "";
                    serverargs = DownloadServer.downloadServerArgs;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                }
                else
                {
                    DialogShow.ShowMsg(this, "出现错误，下载失败！", "错误");
                }
            }
            else
            {
                try
                {
                    if (!Directory.Exists(serverbase))
                    {
                        Directory.CreateDirectory(serverbase);
                    }
                    string _filename = Path.GetFileName(txb3.Text);
                    if (Path.GetDirectoryName(txb3.Text) != serverbase)
                    {
                        File.Copy(txb3.Text, serverbase + @"\" + _filename, true);
                        DialogShow.ShowMsg(this, "已将服务端文件移至服务器文件夹中！您可将源文件删除！", "提示");
                        txb3.Text = serverbase + @"\" + _filename;
                    }
                    servercore = _filename;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                    next3.IsEnabled = true;
                    return5.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    DialogShow.ShowMsg(this, ex.Message, "错误");
                }
            }
        }


        private void skip_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void usebasicfastJvm_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            useJVM.IsChecked = true;
            usefastJvm.IsChecked = false;
            txb7.Text = "-XX:+AggressiveOpts";
            txb4.Text = "2048";
            txb5.Text = "2048";
        }
        private void usefastJvm_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            useJVM.IsChecked = true;
            usebasicfastJvm.IsChecked = false;
            txb7.Text = "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dusing.aikars.flags=https://mcflags.emc.gs -Daikars.new.flags=true";
            txb4.Text = "4096";
            txb5.Text = "4096";
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FastModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Hidden;
            FastModeGrid.Visibility = Visibility.Visible;
            Thread thread = new Thread(FastModeGetCore);
            thread.Start();
        }

        Dictionary<string, List<string>> serverCoreTypes = new Dictionary<string, List<string>>
            {
                {"pluginsCore",new List<string>()
                    {"Paper","Purpur","Spigot","CraftBukkit","Folia"}
                },
                {"modsCore_Forge",new List<string>()
                    {"Forge"}
                },
                {"modsCore_Fabric",new List<string>()
                    {"Fabric"}
                },
                {"pluginsAndModsCore",new List<string>()
                    {"Mohist","Catserver"}
                },
                {"vanillaCore",new List<string>()
                    {"Vanilla"}
                },
                {"bedrockCore",new List<string>()
                    {"Nukkit"}
                },
                {"proxyCore",new List<string>()
                    {"BungeeCord"}
                }
            };
        Dictionary<string, Dictionary<string, string>> serverCores;
        void FastModeGetCore()
        {
            try
            {
                Ping pingSender = new Ping();
                string serverAddr = MainWindow.serverLink;
                if(serverAddr!= "https://msl.waheal.top")
                {
                    if (serverAddr.Contains("http://")) { serverAddr = serverAddr.Remove(0, 7); }
                    PingReply reply = pingSender.Send(serverAddr, 2000); // 替换成您要 ping 的 IP 地址
                    if (reply.Status != IPStatus.Success)
                    {
                        MainWindow.serverLink = "https://msl.waheal.top";
                        Growl.Info("MSL主服务器连接超时，已切换至备用服务器！");
                    }
                }
                var coreVersions = new Dictionary<string, List<string>>();
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + "/msl/CC/versions.json");
                string versionsList = Encoding.UTF8.GetString(pageData);
                //分类服务端
                //serverVersions = JObject.Parse(versionsList);
                serverCores = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(versionsList);
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    ServerCoreCombo.SelectedIndex = 0;
                });
            }
            catch (Exception a)
            {
                Growl.Info("获取服务端失败！请重试" + a.Message);
            }
        }

        private void ServerCoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerVersionCombo.Items.Clear();
            int i = 0;
            // 遍历所有核心类型
            foreach (var coreType in serverCoreTypes)
            {
                if (i == ServerCoreCombo.SelectedIndex)
                {
                    // 遍历所有核心版本
                    foreach (var core in coreType.Value)
                    {
                        // 获取该核心类型下的所有版本
                        var versions = serverCores.FirstOrDefault(c => c.Key.Contains(core)).Value;

                        if (versions != null)
                        {
                            foreach (var version in versions)
                            {
                                string _ver = version.Key;
                                if (version.Key.Contains("-"))
                                {
                                    _ver = version.Key.Substring(0, _ver.IndexOf("-"));
                                }
                                // 添加版本到版本列表
                                if (!ServerVersionCombo.Items.Contains(_ver) && version.Value != null)
                                {
                                    ServerVersionCombo.Items.Add(_ver);
                                }
                            }
                        }
                    }
                    break;
                }
                else { i++; }
            }
            ServerVersionCombo.SelectedIndex = 0;
            switch (ServerCoreCombo.SelectedIndex)
            {
                case 0:
                    ServerCoreDescrip.Text = "插件服务器：";
                    break;
                case 1:
                    ServerCoreDescrip.Text = "模组服务器（Forge加载器）：";
                    break;
                case 2:
                    ServerCoreDescrip.Text = "模组服务器（Fabric加载器）：";
                    break;
                case 3:
                    ServerCoreDescrip.Text = "插件模组二合一服务器（Forge加载器）：";
                    break;
                case 4:
                    ServerCoreDescrip.Text = "原版服务器：";
                    break;
                case 5:
                    ServerCoreDescrip.Text = "基岩版服务器：";
                    break;
                case 6:
                    ServerCoreDescrip.Text = "代理服务器：";
                    break;
            }
        }

        List<string> downloadCoreUrl = new List<string>();
        private void FastModeNextBtn_Click(object sender, RoutedEventArgs e)
        {
            servername = ServerNameBox.Text;
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                var result = MessageBox.Show("开服器被放置于带有中文的目录里，中文目录可能会造成编码错误导致无法开服，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if (txb6.Text.IndexOf(" ") + 1 != 0)
            {
                var result = MessageBox.Show("开服器被放置于带有空格的目录里，这种目录可能会造成编码错误导致无法开服，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            serverbase = txb6.Text;
            FastModeGrid.Visibility = Visibility.Hidden;
            InstallGrid.Visibility = Visibility.Visible;
            int i = 0;
            
            // 遍历所有核心类型
            foreach (var coreType in serverCoreTypes)
            {
                if (i == ServerCoreCombo.SelectedIndex)
                {
                    int _coreType = 0;
                    // 遍历所有核心版本
                    foreach (var core in coreType.Value)
                    {
                        // 获取该核心类型下的所有版本
                        var versions = serverCores.FirstOrDefault(c => c.Key.Contains(core)).Value;

                        if (versions != null)
                        {
                            foreach (var version in versions)
                            {
                                string _ver = version.Key;
                                if (version.Key.Contains("-"))
                                {
                                    _ver = version.Key.Substring(0, _ver.IndexOf("-"));
                                }
                                // 添加版本到版本列表
                                if (_ver==ServerVersionCombo.Items[ServerVersionCombo.SelectedIndex].ToString() && version.Value != null)
                                {
                                    FinallyCoreCombo.Items.Add(coreType.Value[_coreType]+"-" + version.Key);
                                    downloadCoreUrl.Add(version.Value);
                                }
                            }
                        }
                        _coreType++;
                    }
                    break;
                }
                else { i++; }
            }
            string versionString = ServerVersionCombo.Items[ServerVersionCombo.SelectedIndex].ToString();
            string[] components = versionString.Split('.');
            if (components.Length >= 3 && int.TryParse(components[2], out int _))
            {
                versionString = $"{components[0]}.{components[1]}"; // remove the last component
            }

            Version _version = new Version(versionString);
            Version targetVersion1 = new Version("1.7");
            Version targetVersion2 = new Version("1.12");
            Version targetVersion3 = new Version("1.17");

            if (_version <= targetVersion1)
            {
                //_version <=1.7
                FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java7-Java8";
                FinallyJavaCombo.SelectedIndex = 0;
            }
            else if (_version <= targetVersion2)
            {
                //1.7< _version <=1.12
                FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java8-Java11";
                FinallyJavaCombo.SelectedIndex = 0;
            }
            else if (_version <= targetVersion3)
            {
                //1.12< _version <=1.17
                FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java11-Java17（或更高）";
                FinallyJavaCombo.SelectedIndex = 3;
            }
            else
            {
                //_version >1.17
                FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java18-Java19（或更高）";
                FinallyJavaCombo.SelectedIndex = 5;
            }
            FinallyCoreCombo.SelectedIndex = 0;
        }

        private void FastModeInstallBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WebClient MyWebClient = new WebClient();
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/msl/otherdownload.json");
                string _javaList = Encoding.UTF8.GetString(pageData);

                JObject javaList0 = JObject.Parse(_javaList);
                JObject javaList = (JObject)javaList0["java"];
                switch (FinallyJavaCombo.SelectedIndex)
                {
                    case 0:
                        DownloadJava("Java8", javaList["Java8"].ToString(),false);
                        break;
                    case 1:
                        DownloadJava("Java11", javaList["Java11"].ToString(), false);
                        break;
                    case 2:
                        DownloadJava("Java16", javaList["Java16"].ToString(), false);
                        break;
                    case 3:
                        DownloadJava("Java17", javaList["Java17"].ToString(), false);
                        break;
                    case 4:
                        DownloadJava("Java18", javaList["Java18"].ToString(), false);
                        break;
                    case 5:
                        DownloadJava("Java19", javaList["Java19"].ToString(), false);
                        break;
                    default:
                        Growl.Error("请选择一个版本以下载！");
                        break;
                }
                FastModeInstallCore();
            }
            catch
            {
                Growl.Error("出现错误，请检查网络连接！");
            }
        }
        void FastModeInstallCore()
        {
            string filename = FinallyCoreCombo.Items[FinallyCoreCombo.SelectedIndex].ToString()+".jar";
            DialogShow.ShowDownload(this, downloadCoreUrl[FinallyCoreCombo.SelectedIndex], serverbase, filename, "下载服务端中……");
            if (File.Exists(serverbase + @"\" + filename))
            {
                servercore = filename;
                bool installReturn = true;
                if (filename.IndexOf("Forge") + 1 != 0)
                {
                    
                    DialogShow.ShowMsg(this, "检测到您下载的是Forge端，开服器将自动进行安装操作，稍后请您不要随意移动鼠标且不要随意触碰键盘，耐心等待安装完毕！\n注：开服器已经把安装地址复制，如果Forge安装窗口弹出很久后没有任何改动的话，请手动选择第二个选项，然后把地址粘贴进去进行安装", "提示");
                    installReturn = InstallForge();
                }
                if (installReturn)
                {
                    try
                    {
                        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json"))
                        {
                            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", string.Format("{{{0}}}", "\n"));
                        }
                        JObject _json = new JObject
                {
                    { "name", servername },
                    { "java", serverjava },
                    { "base", serverbase },
                    { "core", servercore },
                    { "memory", servermemory },
                    { "args", serverargs }
                };
                        JObject jsonObject = JObject.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Encoding.UTF8));
                        List<string> keys = jsonObject.Properties().Select(p => p.Name).ToList();
                        var _keys = keys.Select(x => Convert.ToInt32(x));
                        int[] ikeys = _keys.ToArray();
                        Array.Sort(ikeys);
                        int i = 0;

                        foreach (int key in ikeys)
                        {
                            if (i == key)
                            {
                                i++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        jsonObject.Add(i.ToString(), _json);
                        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                        DialogShow.ShowMsg(this, "创建完毕，请点击“开启服务器”按钮以开服", "信息");
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                DialogShow.ShowMsg(this, "下载失败！", "错误");
            }
        }

        #region InstallForge
        /// <summary>
        /// 找到窗口
        /// </summary>
        /// <param name="lpClassName">窗口类名(例：Button)</param>
        /// <param name="lpWindowName">窗口标题</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 找到窗口
        /// </summary>
        /// <param name="hwndParent">父窗口句柄（如果为空，则为桌面窗口）</param>
        /// <param name="hwndChildAfter">子窗口句柄（从该子窗口之后查找）</param>
        /// <param name="lpszClass">窗口类名(例：Button</param>
        /// <param name="lpszWindow">窗口标题</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private extern static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="hwnd">消息接受窗口句柄</param>
        /// <param name="wMsg">消息</param>
        /// <param name="wParam">指定附加的消息特定信息</param>
        /// <param name="lParam">指定附加的消息特定信息</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr hwnd, uint wMsg, int wParam, int lParam);

        const int WM_SETFOCUS = 0x07;
        bool InstallForge()
        {
            string filename = FinallyCoreCombo.Items[FinallyCoreCombo.SelectedIndex].ToString()+".jar";
            string forgeVersion;
            if (downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("bmcl") + 1 != 0)
            {
                forgeVersion = FinallyCoreCombo.Items[FinallyCoreCombo.SelectedIndex].ToString().Replace("Forge","") + downloadCoreUrl[FinallyCoreCombo.SelectedIndex].Substring(downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("&version=") + 9,
                    downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("&category") - (downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("&version=") + 9));
            }
            else
            {
                forgeVersion = downloadCoreUrl[FinallyCoreCombo.SelectedIndex].Substring(downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("forge-") + 6,
                downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("-installer") - (downloadCoreUrl[FinallyCoreCombo.SelectedIndex].IndexOf("forge-") + 6));
            }
            if (forgeVersion.Length - forgeVersion.Replace("-", "").Length > 1)
            {
                forgeVersion = forgeVersion.Substring(0, forgeVersion.LastIndexOf("-"));
            }
            Process process = new Process();
            process.StartInfo.FileName = serverjava;
            process.StartInfo.Arguments = "-jar " + serverbase + @"\" + filename;
            Directory.SetCurrentDirectory(serverbase);
            process.Start();
            try
            {
                while (!process.HasExited)
                {
                    IntPtr maindHwnd = FindWindow(null, "Mod system installer");//主窗口标题
                    if (maindHwnd != IntPtr.Zero)
                    {
                        SendMessage(maindHwnd, WM_SETFOCUS, 0, 0);
                        System.Windows.Clipboard.SetDataObject(serverbase);
                        if (filename.IndexOf("1.12") + 1 != 0 || filename.IndexOf("1.13") + 1 != 0 || filename.IndexOf("1.14") + 1 != 0 || filename.IndexOf("1.15") + 1 != 0)
                        {
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{DOWN}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{ENTER}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{DELETE}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("^{v}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{ENTER}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{ENTER}");
                            break;
                        }
                        else
                        {
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{DOWN}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{ENTER}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{DELETE}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("^{v}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{ENTER}");
                            Thread.Sleep(500);
                            SendKeys.SendWait("{Tab}");
                            Thread.Sleep(200);
                            SendKeys.SendWait("{ENTER}");
                            break;
                        }
                    }
                    Thread.Sleep(1000);
                }

                while (!process.HasExited)
                {
                    Thread.Sleep(1000);
                }
                if (File.Exists(serverbase + "\\libraries\\net\\minecraftforge\\forge\\" + forgeVersion + "\\win_args.txt"))
                {
                    servercore = "";
                    serverargs = "@libraries/net/minecraftforge/forge/" + forgeVersion + "/win_args.txt %*";
                    return true;
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(serverbase);
                    FileInfo[] fileInfo = directoryInfo.GetFiles();
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name.IndexOf("forge-" + forgeVersion) + 1 != 0)
                        {
                            servercore = file.FullName.Replace(serverbase + @"\", "");
                            return true;
                        }
                        else
                        {
                            DialogShow.ShowMsg(this, "下载失败,请多次尝试或使用代理再试！", "错误");
                            servercore = "";
                            return false;
                        }
                    }
                }
                return false;
            }
            catch
            {
                DialogShow.ShowMsg(this, "下载失败！", "错误");
                return false;
            }
        }
        #endregion

        private void CustomModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Hidden;
            tabCtrl.Visibility = Visibility.Visible;
        }

        private void FastModeReturnBtn_Click(object sender, RoutedEventArgs e)
        {
            FinallyCoreCombo.Items.Clear();
            downloadCoreUrl.Clear();
            InstallGrid.Visibility = Visibility.Hidden;
            FastModeGrid.Visibility = Visibility.Visible;
            
        }
    }
}
