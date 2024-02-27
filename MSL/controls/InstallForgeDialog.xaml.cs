using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


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
        public InstallForgeDialog(string forge,string downPath)
        {
            InitializeComponent();
            log_in("准备开始安装Forge···");
            suc = false;//初始化suc
            forgePath = forge;//传递路径过来
            installPath = downPath;
            tempPath = downPath + "/temp";
            libPath = downPath + "/libraries";
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
                bool dwnDialog = DialogShow.ShowDownload(this, vanillaUrl, Path.GetDirectoryName(serverJarPath), Path.GetFileName(serverJarPath), "下载原版核心中···");
                if (!dwnDialog)
                {
                    //下载失败，跑路了！
                    log_in("原版核心下载失败！安装失败！");
                    return;
                }
            });
            log_in("下载原版服务端核心成功！");
            status_change("正在下载Forge运行Lib···");
            log_in("正在下载Forge运行Lib···");
            var versionlJobj = GetJsonObj(tempPath + "/version.json");
            JArray libraries = (JArray)versionlJobj["libraries"];//获取lib数组
            foreach (JObject lib in libraries)//遍历数组，进行文件下载
            {
                string _dlurl= lib["downloads"]["artifact"]["url"].ToString();
                string _savepath = libPath + "/" + lib["downloads"]["artifact"]["path"].ToString();
                log_in("[LIB]正在下载："+ lib["downloads"]["artifact"]["path"].ToString());
                Dispatcher.Invoke(() =>
                {
                    bool dwnDialog = DialogShow.ShowDownload(this, _dlurl, Path.GetDirectoryName(_savepath), Path.GetFileName(_savepath), "下载LIB中···");
                    if (!dwnDialog)
                    {
                        //下载失败，跑路了！
                        log_in(lib["downloads"]["artifact"]["path"].ToString() + "下载失败！安装失败！");
                        return;
                    }
                });
            }
            //2024.02.27 下午11：25 写的时候bmclapi炸了，导致被迫暂停，望周知（
            JArray libraries2 = (JArray)installJobj["libraries"];//获取lib数组 这是install那个json
            foreach (JObject lib in libraries2)//遍历数组，进行文件下载
            {
                string _dlurl = lib["downloads"]["artifact"]["url"].ToString();
                string _savepath = libPath + "/" + lib["downloads"]["artifact"]["path"].ToString();
                log_in("[LIB]正在下载：" + lib["downloads"]["artifact"]["path"].ToString());
                Dispatcher.Invoke(() =>
                {
                    bool dwnDialog = DialogShow.ShowDownload(this, _dlurl, Path.GetDirectoryName(_savepath), Path.GetFileName(_savepath), "下载LIB中···");
                    if (!dwnDialog)
                    {
                        //下载失败，跑路了！
                        log_in(lib["downloads"]["artifact"]["path"].ToString() + "下载失败！安装失败！");
                        return;
                    }
                });
            }
        }

        void log_in(string logStr)
        {
            Dispatcher.Invoke(() =>
            {
                log.Text = log.Text + "\n" +logStr;
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
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}



