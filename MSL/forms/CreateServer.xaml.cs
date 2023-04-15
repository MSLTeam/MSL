using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;
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

        DispatcherTimer timer1 = new DispatcherTimer();
        public CreateServer()
        {
            timer1.Tick += new EventHandler(timer1_Tick);
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
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
                if (isImportPack)
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                }
                return;
            }
            if (usejvPath.IsChecked == true)
            {
                serverjava = "Java";
                //MainWindow.serverserver = txb3.Text;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
                if (isImportPack)
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                }
                return;
            }
            try
            {
                WebClient MyWebClient = new WebClient();
                byte[] pageData = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/otherdownload.json");
                string _javaList = Encoding.UTF8.GetString(pageData);

                JObject javaList0 = JObject.Parse(_javaList);
                JObject javaList = (JObject)javaList0["java"];

                next3.IsEnabled = false;
                return1.IsEnabled = false;
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
                DialogShow.ShowMsg(this, "出现错误！请检查您的网络连接！", "信息", false, "确定");
            }
        }

        private void DownloadJava(string fileName, string downUrl)
        {
            //MessageBox.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
            {
                DialogShow.ShowMsg(this, "下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false, "确定");
                //MessageDialog messageDialog = new MessageDialog();
                //messageDialog.Owner = this;
                //messageDialog.ShowDialog();
                DownjavaName = fileName;
                DialogShow.ShowDownload(this, downUrl, AppDomain.CurrentDomain.BaseDirectory + "MSL", "Java.zip", "下载" + fileName + "中……");
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
                    timer1.Interval = TimeSpan.FromSeconds(3);
                    timer1.Start();
                    if (AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName != AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName)
                    {
                        MoveFolder(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName, AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName);
                    }
                }
                catch
                {
                    MessageBox.Show("安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    outlog.Content = "安装失败！";
                    next3.IsEnabled = true;
                    return1.IsEnabled = true;
                }
                /*
                Form4 fw = new Form4();
                fw.ShowDialog();*/
            }
            else
            {
                serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe";
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
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
                            next3.IsEnabled = true;
                            return1.IsEnabled = true;
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
                            next3.IsEnabled = true;
                            return1.IsEnabled = true;
                        }
                    }
                    else
                    {
                        Growl.Info("整合包中通常会附带一个服务端核心，您可点击第二个按钮进行手动选择，若找不到的话，请返回这一步并点击第一个按钮进行下载");
                    }
                }
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe"))
            {
                try
                {
                    outlog.Content = "完成";
                    serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe";
                    next3.IsEnabled = true;
                    return1.IsEnabled = true;
                    javagrid.Visibility = Visibility.Hidden;
                    servergrid.Visibility = Visibility.Visible;
                    label3.Visibility = Visibility.Visible;
                    downloadjava.Visibility = Visibility.Visible;
                    selectjava.Visibility = Visibility.Visible;
                    return2.Visibility = Visibility.Visible;
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
                                next3.IsEnabled = true;
                                return1.IsEnabled = true;
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
                                next3.IsEnabled = true;
                                return1.IsEnabled = true;
                            }
                        }
                        else
                        {
                            Growl.Info("整合包中通常会附带一个服务端核心，您可点击第二个按钮进行手动选择，若找不到的话，请返回这一步并点击第一个按钮进行下载");
                        }
                    }
                    timer1.Stop();
                }
                catch
                {
                    return;
                }
            }
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
            label3.Visibility = Visibility.Visible;
            downloadjava.Visibility = Visibility.Visible;
            selectjava.Visibility = Visibility.Visible;
            label5.Visibility = Visibility.Hidden;
            usedownloadjv.Visibility = Visibility.Hidden;
            selectJavaComb.Visibility = Visibility.Hidden;
            outlog.Visibility = Visibility.Hidden;
            jvhelp.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Hidden;
            usejvPath.Visibility = Visibility.Hidden;
            useJVself.Visibility = Visibility.Hidden;
            txjava.Visibility = Visibility.Hidden;
            a0002_Copy.Visibility = Visibility.Hidden;
            next3.Visibility = Visibility.Hidden;
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

        private void useJVself_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = true;
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            servername = serverNameBox.Text;
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                var result = MessageBox.Show("使用带中文的路径可能会出现无法开服的致命bug，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if (txb6.Text.IndexOf(" ") + 1 != 0)
            {
                var result = MessageBox.Show("使用带空格的路径可能会出现无法开服的致命bug，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

        private void usejvPath_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                txjava.IsEnabled = false;
            }
        }

        private void downloadserver_Click(object sender, RoutedEventArgs e)
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
                next3.IsEnabled = true;
                return1.IsEnabled = true;
            }
            else if (DownloadServer.downloadServerArgs != "")
            {
                servercore = "";
                serverargs = DownloadServer.downloadServerArgs;
                sJVM.IsSelected = true;
                sJVM.IsEnabled = true;
                sserver.IsEnabled = false;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
            }
            else
            {
                DialogShow.ShowMsg(this, "出现错误，下载失败！", "错误");
            }
        }

        private void selectserver_Click(object sender, RoutedEventArgs e)
        {
            servertips.Visibility = Visibility.Hidden;
            downloadserver.Visibility = Visibility.Hidden;
            selectserver.Visibility = Visibility.Hidden;
            label1.Visibility = Visibility.Hidden;
            return2.Visibility = Visibility.Visible;
            txb3.Visibility = Visibility.Visible;
            next2.Visibility = Visibility.Visible;
            a0002.Visibility = Visibility.Visible;
            label2.Visibility = Visibility.Visible;
        }

        private void next2_Click(object sender, RoutedEventArgs e)
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
                return1.IsEnabled = true;
            }
            catch (Exception ex)
            {
                DialogShow.ShowMsg(this, ex.Message, "错误");
            }
        }

        private void selectjava_Click(object sender, RoutedEventArgs e)
        {
            DialogShow.ShowMsg(this, "请确保您要选择的Java版本适用于将要运行的MC服务器版本，如果版本不匹配，服务器将无法开启！\nJava版本和MC版本兼容列表：\n1.8之前：可以使用Java7-Java8\n1.8-1.12.2 可以使用Java8-Java11\n1.13-1.17.1 可以使用Java11-Java16\n1.18-最新版 可以使用Java18-Java19", "警告");
            tipsjv1.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Visible;
            usejvPath.Visibility = Visibility.Visible;
            usejvPath.IsChecked = true;
            useJVself.Visibility = Visibility.Visible;
            txjava.Visibility = Visibility.Visible;
            a0002_Copy.Visibility = Visibility.Visible;
            jvhelp.Visibility = Visibility.Visible;
            next3.Visibility = Visibility.Visible;
            return1.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            downloadjava.Visibility = Visibility.Hidden;
            selectjava.Visibility = Visibility.Hidden;
        }

        private void downloadjava_Click(object sender, RoutedEventArgs e)
        {
            tipsjv1.Visibility = Visibility.Hidden;
            label5.Visibility = Visibility.Visible;
            usedownloadjv.Visibility = Visibility.Visible;
            selectJavaComb.Visibility = Visibility.Visible;
            usedownloadjv.IsChecked = true;
            selectJavaComb.SelectedIndex = 0;
            outlog.Visibility = Visibility.Visible;
            jvhelp.Visibility = Visibility.Visible;
            next3.Visibility = Visibility.Visible;
            return1.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            downloadjava.Visibility = Visibility.Hidden;
            selectjava.Visibility = Visibility.Hidden;
        }

        private void return1_Click(object sender, RoutedEventArgs e)
        {
            servertips.Visibility = Visibility.Visible;
            tipsjv1.Visibility = Visibility.Visible;
            downloadserver.Visibility = Visibility.Visible;
            selectserver.Visibility = Visibility.Visible;
            label1.Visibility = Visibility.Visible;
            javagrid.Visibility = Visibility.Visible;
            label3.Visibility = Visibility.Visible;
            downloadjava.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Visible;
            selectjava.Visibility = Visibility.Visible;
            servergrid.Visibility = Visibility.Hidden;
            label5.Visibility = Visibility.Hidden;
            usedownloadjv.Visibility = Visibility.Hidden;
            selectJavaComb.Visibility = Visibility.Hidden;
            outlog.Visibility = Visibility.Hidden;
            jvhelp.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Hidden;
            return1.Visibility = Visibility.Hidden;
            return2.Visibility = Visibility.Hidden;
            usejvPath.Visibility = Visibility.Hidden;
            useJVself.Visibility = Visibility.Hidden;
            txjava.Visibility = Visibility.Hidden;
            a0002_Copy.Visibility = Visibility.Hidden;
            next3.Visibility = Visibility.Hidden;
            txb3.Visibility = Visibility.Hidden;
            next2.Visibility = Visibility.Hidden;
            a0002.Visibility = Visibility.Hidden;
            label2.Visibility = Visibility.Hidden;
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
    }
}
