using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;


namespace MSL.controls
{

    /// <summary>
    /// InstallForgeDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstallForgeDialog : HandyControl.Controls.Window
    {
        public static bool suc;
        public string forgePath;
        public string installPath;
        public string tempPath;
        public string libPath;
        public string javaPath;
        public InstallForgeDialog(string forge,string downPath,string java)
        {
            InitializeComponent();
            log_in("准备开始安装Forge···");
            suc = false;//初始化suc
            forgePath = forge;//传递路径过来
            installPath = downPath;
            tempPath = downPath + "/temp";
            libPath = downPath + "/libraries";
            javaPath = java;
            Thread thread = new Thread(Install);//新建线程开始安装
            thread.Start();
        }

        //安装forge的主方法
        private void Install()
        {
            //第一步，解压Installer
            //创建一个文件夹存放解压的installer
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            status_change("正在解压Forge安装器···");
            log_in("开始解压forge安装器！");
            bool unzip = ExtractJar(forgePath, tempPath);//解压
            if (!unzip)
            {
                //解压失败，不干了！
                log_in("forge安装器解压失败！安装失败！");
                return;
            }
            log_in("解压forge安装器成功！");
            //第二步，下载原版核心
            status_change("正在下载原版服务端核心···");
            log_in("正在下载原版服务端核心···");
            var installJobj = GetJsonObj(tempPath + "/install_profile.json");
            string serverJarPath = replaceStr(installJobj["serverJarPath"].ToString());
            string vanillaUrl = Functions.Get("download/server/vanilla/"+ installJobj["minecraft"].ToString(), out _);
            Dispatcher.Invoke(() => //下载
            {
                bool dwnDialog = Shows.ShowDownloader(this, vanillaUrl, Path.GetDirectoryName(serverJarPath), Path.GetFileName(serverJarPath), "下载原版核心中···");
                if (!dwnDialog)
                {
                    //下载失败，跑路了！
                    log_in("原版核心下载失败！安装失败！");
                    return;
                }
            });
            log_in("下载原版服务端核心成功！");
            log_in("正在解压原版LIB！");
            //解压原版服务端中的lib
            if (!Directory.Exists(tempPath + "/vanilla"))
            {
                Directory.CreateDirectory(tempPath + "/vanilla");
            }
            bool result = ExtractJar(serverJarPath, tempPath + "/vanilla");
            if (result)
            {
                try
                {
                    // 指定源文件夹和目标文件夹
                    string sourceDirectory = Path.Combine(tempPath + "/vanilla", "META-INF", "libraries");
                    string targetDirectory = installPath;

                    // 确保目标文件夹存在
                    Directory.CreateDirectory(targetDirectory);

                    // 获取源文件夹中的所有文件
                    string[] files = Directory.GetFiles(sourceDirectory);

                    // 复制所有文件到目标文件夹
                    foreach (string file in files)
                    {
                        string name = Path.GetFileName(file);
                        string dest = Path.Combine(targetDirectory, name);
                        File.Copy(file, dest);
                    }
                }
                catch (Exception ex)
                {
                    log_in("原版LIB解压失败！" + ex);
                    return ;
                }
            }
            //复制shim jar（鬼知道什么版本加进来的哦！）
            File.Copy(tempPath +  "/maven/"+ NameToPath(installJobj["path"].ToString()),installPath + "/" + Path.GetFileName(libPath + "/" + NameToPath(installJobj["path"].ToString())));
            //下载运行库
            status_change("正在下载Forge运行Lib···");
            log_in("正在下载Forge运行Lib···");
            var versionlJobj = GetJsonObj(tempPath + "/version.json");
            JArray libraries2 = (JArray)installJobj["libraries"];//获取lib数组 这是install那个json
            JArray libraries = (JArray)versionlJobj["libraries"];//获取lib数组
            int libALLCount = libraries.Count + libraries2.Count;//总数
            int libCount = 0;//用于计数
            
            foreach (JObject lib in libraries)//遍历数组，进行文件下载
            {
                libCount++;
                string _dlurl= replaceStr(lib["downloads"]["artifact"]["url"].ToString());
                string _savepath = libPath + "/" + lib["downloads"]["artifact"]["path"].ToString();
                string _sha1 = lib["downloads"]["artifact"]["sha1"].ToString();
                log_in("[LIB]正在下载："+ lib["downloads"]["artifact"]["path"].ToString());

                    bool dlStatus = DownloadFile(_dlurl, _savepath, _sha1);
                status_change("正在下载Forge运行Lib···(" + libCount + "/" + libALLCount +")");

                /*Dispatcher.Invoke(() =>
                {
                    bool dwnDialog = DialogShow.ShowDownloader(this, _dlurl, Path.GetDirectoryName(_savepath), Path.GetFileName(_savepath), "下载LIB("+ libCount + "/" + libALLCount+")中···");
                    if (!dwnDialog)
                    {
                        //下载失败，跑路了！
                        log_in(lib["downloads"]["artifact"]["path"].ToString() + "下载失败！安装失败！");
                        return;
                    }
                }); */ //调用downloader的下载窗口太慢了！
            }
            //2024.02.27 下午11：25 写的时候bmclapi炸了，导致被迫暂停，望周知（
            foreach (JObject lib in libraries2.Cast<JObject>())//遍历数组，进行文件下载
            {
                libCount++;
                string _dlurl = replaceStr(lib["downloads"]["artifact"]["url"].ToString());
                string _savepath = libPath + "/" + lib["downloads"]["artifact"]["path"].ToString();
                string _sha1 = lib["downloads"]["artifact"]["sha1"].ToString();
                log_in("[LIB]正在下载：" + lib["downloads"]["artifact"]["path"].ToString());
                if (!_dlurl.Contains("mcp")) //mcp那个zip会用js redirect，所以只能用downloader，真神奇！
                {
                    bool dlStatus = DownloadFile(_dlurl, _savepath, _sha1);
                    status_change("正在下载Forge运行Lib···(" + libCount + "/" + libALLCount + ")");
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        status_change("正在下载Forge运行Lib···(" + libCount + "/" + libALLCount + ")");
                        bool dwnDialog = Shows.ShowDownloader(this, _dlurl, Path.GetDirectoryName(_savepath), Path.GetFileName(_savepath), "下载LIB(" + libCount + "/" + libALLCount + ")中···");
                        if (!dwnDialog)
                        {
                            //下载失败，跑路了！
                            log_in(lib["downloads"]["artifact"]["path"].ToString() + "下载失败！安装失败！");
                            return;
                        }
                    });
                }
                /*
                Dispatcher.Invoke(() =>
                {
                    bool dwnDialog = DialogShow.ShowDownloader(this, _dlurl, Path.GetDirectoryName(_savepath), Path.GetFileName(_savepath), "下载LIB(" + libCount + "/" + libALLCount + ")中···");
                    if (!dwnDialog)
                    {
                        //下载失败，跑路了！
                        log_in(lib["downloads"]["artifact"]["path"].ToString() + "下载失败！安装失败！");
                        return;
                    }
                }); */
            } 
            log_in("下载Forge运行Lib成功！");
            status_change("正在编译Forge···");
            log_in("正在编译Forge···");
            string batData = "";
            //接下来开始编译咯~
            JArray processors = (JArray)installJobj["processors"]; //获取processors数组
            int i = 0;
            foreach (JObject processor in processors)
            {
                i++;
                string buildarg;
                JArray sides = (JArray)processor["sides"]; //获取sides数组
                if (sides == null || sides.Values<string>().Contains("server"))
                {
                    buildarg = @"-cp """;
                    //处理classpath
                    buildarg = buildarg + libPath + "/" + NameToPath((string)processor["jar"]) + ";";
                    JArray classpath = (JArray)processor["classpath"];
                    foreach (string path in classpath.Values<string>())
                    {

                        buildarg = buildarg + libPath+ "/" +NameToPath(path) + ";";
                    }
                    buildarg += @""" ";//结束cp处理
                    if (buildarg.Contains("installertools"))//主类
                    {
                        buildarg += "net.minecraftforge.installertools.ConsoleTool ";
                    }
                    else if(buildarg.Contains("ForgeAutoRenamingTool"))
                    {
                        buildarg += "net.minecraftforge.fart.Main ";
                    }
                    else
                    {
                        buildarg += "net.minecraftforge.binarypatcher.ConsoleTool ";
                    }
                    
                    //处理args
                    JArray args = (JArray)processor["args"];
                    foreach (string arg in args.Values<string>())
                    {
                        if (arg.StartsWith("[") && arg.EndsWith("]")) //在[]中，表明要转换
                        {
                            buildarg = buildarg + @"""" + libPath + "\\" + replaceStr(NameToPath(arg)) + @""" ";
                        }
                        else
                        {
                            buildarg = buildarg + @"""" + replaceStr(arg) + @""" ";
                        }
                    
                    }
                    log_in("启动参数：" + buildarg);
                    //启动编译,算了，不启动了，麻瓜
                    if (javaPath == "Java")
                    {
                        batData = batData + "\n" + "java " + buildarg;
                    }
                    else
                    {
                        batData = batData + "\n" + @"""" + javaPath + @""" " + buildarg;
                    }
                    

                    /*
                    Process process = new Process();
                    process.StartInfo.FileName = "java"; //java路径
                    process.StartInfo.Arguments = buildarg;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WorkingDirectory= installPath;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                            log_in(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                            log_in("Error: " + e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit(); */ //麻瓜

                }
            }
            //输出并启动bat
            using (StreamWriter sw = new StreamWriter(installPath +  "/install.bat"))
            {
                sw.WriteLine("@echo off");
                sw.WriteLine(@"title ""MSL is compiling Forge""");
                sw.WriteLine(batData);
            }
            Process process = new Process();
            process.StartInfo.WorkingDirectory = installPath;
            process.StartInfo.FileName = "cmd";
            process.StartInfo.Arguments = "/c install.bat";
            //由于未知原因，监听日志会假死（可能是日志太多了？），直接使用cmd窗口
            process.Start();

            process.WaitForExit();
            log_in("安装结束！");
            status_change("结束！");
        }

        void log_in(string logStr)
        {
            Dispatcher.Invoke(() =>
            {
                log.Text = log.Text + "\n" +logStr;
                log.ScrollToEnd();
            });
        }

        void status_change(string textStr)
        {
            Dispatcher.Invoke(() =>
            {
                status.Text = textStr;
            });
        }

        //获取json对象
        public JObject GetJsonObj(string file)
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), file);
            var json = File.ReadAllText(jsonPath);
            var jsonObj = JObject.Parse(json);
            return jsonObj;
        }

        //替换json变量的东东
        public string replaceStr(string str)
        {
            var installJobj = GetJsonObj(tempPath + "/install_profile.json");
            str = str.Replace("{LIBRARY_DIR}", libPath);
            str=str.Replace("{MINECRAFT_VERSION}", installJobj["minecraft"].ToString());
            //改成镜像源的部分
            str = str.Replace("https://maven.minecraftforge.net", "https://bmclapi2.bangbang93.com/maven");
            str = str.Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven");
            //构建时候的变量
            str = str.Replace("{INSTALLER}", forgePath);
            str = str.Replace("{ROOT}", installPath);
            str = str.Replace("{MINECRAFT_JAR}", installJobj["serverJarPath"].ToString().Replace("{LIBRARY_DIR}", libPath).Replace("{MINECRAFT_VERSION}", installJobj["minecraft"].ToString()));
            str = str.Replace("{MAPPINGS}",libPath + "\\" + NameToPath(installJobj["data"]["MAPPINGS"]["server"].ToString()));
            str = str.Replace("{MC_UNPACKED}", libPath + "\\" + NameToPath(installJobj["data"]["MC_UNPACKED"]["server"].ToString()));
            str = str.Replace("{SIDE}", "server");
            str = str.Replace("{MOJMAPS}", libPath + "\\" + NameToPath(installJobj["data"]["MOJMAPS"]["server"].ToString()));
            str = str.Replace("{MERGED_MAPPINGS}", libPath + "\\" + NameToPath(installJobj["data"]["MERGED_MAPPINGS"]["server"].ToString()));
            str = str.Replace("{MC_SRG}", libPath + "\\" + NameToPath(installJobj["data"]["MC_SRG"]["server"].ToString()));
            str = str.Replace("{PATCHED}", libPath + "\\" + NameToPath(installJobj["data"]["PATCHED"]["server"].ToString()));
            str = str.Replace("{BINPATCH}", tempPath+"\\" + installJobj["data"]["BINPATCH"]["server"].ToString());
          
            return str;
        }

        //解压jar的函数
        public bool ExtractJar(string jarPath, string extractPath)
        {
            try
            {
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(jarPath, extractPath, null);
                return true;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }

        //路径转换函数，参考：https://rechalow.gitee.io/lmaml/FirstChapter/GetCpLibraries.html 非常感谢！
        public string NameToPath(string name)
        {
            string extentTag = "";

            if (name.StartsWith("[") && name.EndsWith("]")) //部分包含在[]中，干掉
            {
                name = name.Substring(1, name.Length - 2);
            }
            if (name.Contains("@"))
            {
                string[] parts = name.Split('@');

                name = parts[0]; //第一部分，按照原版处理
                extentTag = parts[1]; //这里等下添加后缀
            }
            List<string> c1 = new List<string>();
            List<string> c2 = new List<string>();
            List<string> all = new List<string>();
            StringBuilder sb = new StringBuilder();

            try
            {
                string n1 = name.Substring(0, name.IndexOf(":"));
                string n2 = name.Substring(name.IndexOf(":") + 1);

                c1.AddRange(n1.Split('.'));
                foreach (var i in c1)
                {
                    all.Add(i + "/");
                }

                c2.AddRange(n2.Split(':'));
                for (int i = 0; i < c2.Count; i++)
                {
                    if (c2.Count >= 3)
                    {
                        if (i < c2.Count - 1)
                        {
                            all.Add(c2[i] + "/");
                        }
                    }
                    else
                    {
                        all.Add(c2[i] + "/");
                    }
                }

                for (int i = 0; i < c2.Count; i++)
                {
                    if (i < c2.Count - 1)
                    {
                        all.Add(c2[i] + "-");
                    }
                    else
                    {
                        all.Add(c2[i] + ".jar");
                    }
                }

                foreach (var i in all)
                {
                    sb.Append(i);
                }

                if (extentTag != "")
                {
                    return sb.ToString().Replace(".jar","") + "." +extentTag;
                }
                return sb.ToString();
            }
            catch
            {
                return null;
            }
            /*
            finally
            {
                c1 = null;
                c2 = null;
                all = null;
                sb = null;
            }
            */
        }

        //下面是有关下载的东东（由于小文件调用原有下载窗口特别慢，就不用了qaq）

        private const int MaxRetryCount = 3; //这是最大重试次数

        public bool DownloadFile(string url, string targetPath, string expectedSha1)
        {
            log_in("开始下载：" + url);
            for (int i = 0; i < MaxRetryCount; i++)
            {
                try
                {
                    //检查下文件夹在不在
                    string directory = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    //下载
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537");
                        client.DownloadFile(url, targetPath);
                    }

                    //校验SHA1
                    using (FileStream fs = new FileStream(targetPath, FileMode.Open))
                    using (BufferedStream bs = new BufferedStream(fs))
                    {
                        using (SHA1Managed sha1 = new SHA1Managed())
                        {
                            byte[] hash = sha1.ComputeHash(bs);
                            string formatted = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                            if (formatted == expectedSha1)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    //处理下载失败
                    log_in("下载失败！" + err);
                    continue;
                }
            }
            //重试爆表了
            return false;
        }

    }
}



