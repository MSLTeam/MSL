using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MSL.controls
{
    internal class Functions
    {
        #region Http Service
        public static string Get(string path, string customUrl = "", bool hideHeader = false)
        {
            return WebGet(path, out _, customUrl, hideHeader);
        }

        public static string[] GetWithSha256(string path, string customUrl = "", bool hideHeader = false)
        {
            string context = WebGet(path, out string sha256, customUrl, hideHeader);
            string[] strings = new string[2];
            strings[0] = context;
            strings[1] = sha256;
            return strings;
        }

        private static string WebGet(string path, out string sha256, string customUrl = "", bool hideHeader = false)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = "https://api." + MainWindow.serverLink;
            if (customUrl == "")
            {
                if (MainWindow.serverLink == null)
                {
                    sha256 = string.Empty;
                    return string.Empty;
                }
            }
            else
            {
                url = customUrl;
            }
            WebClient webClient = new WebClient();
            if (!hideHeader)
            {
                webClient.Headers.Add("User-Agent", "MSL/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            }
            webClient.Credentials = CredentialCache.DefaultCredentials;
            byte[] pageData = webClient.DownloadData(url + "/" + path);
            sha256 = string.Empty; //先定义为空
            if (webClient.ResponseHeaders["sha256"] != null)
            {
                sha256 = webClient.ResponseHeaders["sha256"];
            }
            return Encoding.UTF8.GetString(pageData);
        }

        public static string Post(string path, int contentType = 0, string parameterData = "", string customUrl = "", WebHeaderCollection header = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = "https://api." + MainWindow.serverLink;
            if (customUrl == "")
            {
                if (MainWindow.serverLink == null)
                {
                    return string.Empty;
                }
            }
            else
            {
                url = customUrl;
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url + "/" + path);
            byte[] buf = Encoding.GetEncoding("UTF-8").GetBytes(parameterData);
            if (contentType == 0)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "application/json";
                myRequest.ContentType = "application/json; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }
            else if (contentType == 1)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "text/plain";
                myRequest.ContentType = "text/plain; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }
            else if (contentType == 2)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "application/x-www-form-urlencoded";
                myRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }

            if (header != null)
            {
                myRequest.Headers = header;
            }

            // 发送请求
            using (Stream stream = myRequest.GetRequestStream())
            {
                stream.Write(buf, 0, buf.Length);
            }

            // 获取响应
            using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
            using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8))
            {
                string returnData = reader.ReadToEnd();
                return returnData;
            }
        }
        #endregion

        public static Tuple<int, int, int, int> VersionCompare(string version)
        {
            if (version.StartsWith("*"))
            {
                return Tuple.Create(100, 100, 100, 100);
            }

            // 使用正则表达式从版本号中提取主要版本号
            Regex regex = new Regex(@"(\d+(\.\d+)+)");
            Match match = regex.Match(version);
            if (match.Success)
            {
                version = match.Groups[1].Value;
            }

            // 将版本号中的每个部分转换为整数，并进行比较
            string[] versionParts = version.Split('.');
            List<int> versionIntParts = new List<int>();
            foreach (string part in versionParts)
            {
                if (int.TryParse(part, out int parsedPart))
                {
                    versionIntParts.Add(parsedPart);
                }
            }

            // 添加0，以便对不完整的版本号进行比较（如1.7）
            while (versionIntParts.Count < 4)
            {
                versionIntParts.Add(0);
            }

            return Tuple.Create(versionIntParts[0], versionIntParts[1], versionIntParts[2], versionIntParts[3]);
        }

        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }

        #region Java Function
        public static async Task<(bool, string)> CheckJavaAvailabilityAsync(string application)
        {
            try
            {
                string javaInfo = null;
                Process process = new Process();
                process.StartInfo.FileName = application;
                process.StartInfo.Arguments = "-version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                string output = await process.StandardError.ReadToEndAsync();

                await Task.Run(() => process.WaitForExit());

                Match match = Regex.Match(output, @"java version \""([\d\._]+)\""");
                Match _match = Regex.Match(output, @"openjdk version \""([\d\._]+)\""");
                if (match.Success)
                {
                    javaInfo = "Java " + match.Groups[1].Value;
                    return (true, javaInfo);
                }
                else if (_match.Success)
                {
                    javaInfo = "OpenJDK " + _match.Groups[1].Value;
                    return (true, javaInfo);
                }
                else
                {
                    return (false, null);
                }
            }
            catch
            {
                return (false, null);
            }
        }

        public static List<string> SearchJava(bool isDeepCheck = false)
        {
            // 预定的文件夹
            string[] javaPaths = new string[]
            {
                @"Program Files\Java",
                @"Program Files (x86)\Java",
                @"Java"
            };
            // 黑名单文件夹
            string[] blackList = new string[]
            {
                "Windows",
                "AppData",
                "ProgramData",
                "System Volume Information",
                "$Recycle.Bin",
                "$RECYCLE.BIN",
                "Recovery"
            };
            // 存储检测到的 Java 安装
            HashSet<string> javaInstalls = new HashSet<string>();
            // 获取所有磁盘
            DriveInfo[] drives = DriveInfo.GetDrives();
            // 使用并行处理来加速扫描过程
            Parallel.ForEach(drives, drive =>
            {
                // 获取磁盘的驱动器字母
                string driveLetter = drive.Name.Substring(0, 1);
                // 如果是浅度扫描模式，只搜索预定的文件夹
                if (!isDeepCheck)
                {
                    // 对每个预定的文件夹，拼接完整的文件夹路径
                    foreach (string _javaPath in javaPaths)
                    {
                        // 拼接完整的文件夹路径
                        string javaPath = Path.Combine(driveLetter + ":\\", _javaPath);
                        //Console.WriteLine(javaPath);
                        // 搜索该文件夹下的所有子文件夹和 release 文件
                        SearchJava(javaPath, javaInstalls, blackList);
                    }
                }
                // 如果是深度扫描模式，搜索所有文件夹
                else
                {
                    // 拼接完整的磁盘路径
                    string diskPath = Path.Combine(driveLetter + ":\\");
                    // 搜索该磁盘下的所有子文件夹和 release 文件
                    SearchJava(diskPath, javaInstalls, blackList);
                }
            });
            // 输出检测到的 Java 安装
            List<string> strings = new List<string>();
            foreach (string javaInstall in javaInstalls)
            {
                strings.Add(javaInstall);
                //Console.WriteLine(javaInstall);
            }
            return strings;
        }


        // 搜索指定文件夹下的所有子文件夹和 release 文件
        private static void SearchJava(string folderPath, HashSet<string> javaInstalls, string[] blackList)
        {
            try
            {
                // 如果文件夹存在
                if (Directory.Exists(folderPath))
                {
                    // 获取文件夹的名称
                    string folderName = Path.GetFileName(folderPath);
                    // 如果文件夹在黑名单中，跳过该文件夹
                    if (blackList.Contains(folderName))
                    {
                        return;
                    }
                    // 获取文件夹下的所有子文件夹
                    string[] subFolders = Directory.GetDirectories(folderPath);
                    // 对每个子文件夹递归调用该方法
                    foreach (string subFolder in subFolders)
                    {
                        SearchJava(subFolder, javaInstalls, blackList);
                    }
                    // 获取文件夹下的所有 release 文件
                    string[] releaseFiles = Directory.GetFiles(folderPath, "release", SearchOption.TopDirectoryOnly);
                    // 对每个 release 文件检查是否是 Java 安装
                    foreach (string releaseFile in releaseFiles)
                    {
                        string javaInstall = SearchJavaRelease(releaseFile);
                        // 如果是 Java 安装，添加到集合中
                        if (javaInstall != null)
                        {
                            javaInstalls.Add(javaInstall);
                        }
                    }
                }
            }
            catch// (Exception ex)
            {
                // 处理异常，输出日志或调试信息
                //Console.WriteLine("Exception occurred while searching {0}: {1}", folderPath, ex.Message);
                //throw ex.InnerException;
            }
        }

        // 检查指定的 release 文件是否是 Java 安装
        private static string SearchJavaRelease(string releaseFile)
        {
            // 读取 release 文件的内容
            string releaseContent = File.ReadAllText(releaseFile);
            // 使用正则表达式匹配 Java 版本
            Match match = Regex.Match(releaseContent, @"JAVA_VERSION=""([\d\._a-zA-Z]+)""");
            // 如果匹配成功，返回 Java 版本和可执行文件的路径
            if (match.Success)
            {
                string javaVersion = match.Groups[1].Value;
                return "Java" + javaVersion + ":" + Path.Combine(Path.GetDirectoryName(releaseFile), "bin", "java.exe");
            }
            // 如果匹配失败，返回 null
            return null;
        }
        #endregion

        #region Install Forge
        /// <summary>
        /// 安装Forge函数
        /// </summary>
        /// <param name="_java">Java路径</param>
        /// <param name="_base">目录/安装目录</param>
        /// <param name="filename">安装器文件</param>
        /// <param name="fastMode">是否使用了自动安装模式（快速模式）</param>
        /// <returns></returns>
        public static string InstallForge(string _java, string _base, string filename, string mcVersion, bool fastMode = true/*, bool customMode = false*/)
        {
            try
            {
                string forgeVersion;
                if (!fastMode)
                {
                    Process process = new Process();
                    process.StartInfo.WorkingDirectory = _base;
                    process.StartInfo.FileName = _java;
                    process.StartInfo.Arguments = "-jar " + filename + " -installServer";
                    process.Start();

                    while (!process.HasExited)
                    {
                        Thread.Sleep(1000);
                    }
                }
                try
                {
                    bool checkRootBase = false;
                    if (Directory.Exists(_base + "\\libraries\\net\\minecraftforge\\forge"))
                    {
                        string[] subFolders = Directory.GetDirectories(_base + "\\libraries\\net\\minecraftforge\\forge");
                        foreach (string subFolder in subFolders)
                        {
                            if (File.Exists(subFolder + "\\win_args.txt"))
                            {
                                forgeVersion = Path.GetFileName(subFolder);
                                if (forgeVersion.Contains(mcVersion))
                                {
                                    return "@libraries/net/minecraftforge/forge/" + forgeVersion + "/win_args.txt %*";
                                }
                            }
                        }
                        checkRootBase = true;
                    }
                    else if (Directory.Exists(_base + "\\libraries\\net\\neoforged\\neoforge"))
                    {
                        string[] subFolders = Directory.GetDirectories(_base + "\\libraries\\net\\neoforged\\neoforge");
                        foreach (string subFolder in subFolders)
                        {
                            if (File.Exists(subFolder + "\\win_args.txt"))
                            {
                                forgeVersion = Path.GetFileName(subFolder);
                                if (forgeVersion.Contains(mcVersion))
                                {
                                    return "@libraries/net/neoforged/neoforge/" + forgeVersion + "/win_args.txt %*";
                                }
                            }
                        }
                        checkRootBase = true;
                    }
                    if (checkRootBase)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(_base);
                        FileInfo[] fileInfo = directoryInfo.GetFiles();
                        foreach (FileInfo file in fileInfo)
                        {
                            if (file.Name.Contains(mcVersion))
                            {
                                if (file.Name.Contains("forge") && (file.Name != filename) && (!file.Name.Contains("installer")) && (!file.Name.Contains("universal")) && (!file.Name.Contains("server")))
                                {
                                    return file.FullName.Replace(_base + @"\", "");
                                }
                            }
                        }
                        foreach (FileInfo file in fileInfo)
                        {
                            if (file.Name.Contains(mcVersion))
                            {
                                if (file.Name.Contains("forge") && (file.Name != filename) && (!file.Name.Contains("installer")) && (!file.Name.Contains("server")))
                                {
                                    return file.FullName.Replace(_base + @"\", "");
                                }
                            }
                        }
                    }
                    return null;
                }
                catch// (Exception ex)
                {
                    //Console.WriteLine(ex.Message);
                    return null;
                }
            }
            catch// (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                return null;
            }
        }
        #endregion

        #region Get File Encoding
        /// <summary>
        /// 获取文本文件的字符编码类型
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Encoding GetTextFileEncodingType(string fileName)
        {
            Encoding encoding = Encoding.Default;
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream, encoding);
            byte[] buffer = binaryReader.ReadBytes((int)fileStream.Length);
            binaryReader.Close();
            fileStream.Close();
            if (buffer.Length >= 3 && buffer[0] == 239 && buffer[1] == 187 && buffer[2] == 191)
            {
                encoding = Encoding.UTF8;
            }
            else if (buffer.Length >= 3 && buffer[0] == 254 && buffer[1] == 255 && buffer[2] == 0)
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (buffer.Length >= 3 && buffer[0] == 255 && buffer[1] == 254 && buffer[2] == 65)
            {
                encoding = Encoding.Unicode;
            }
            else if (IsUTF8Bytes(buffer))
            {
                encoding = Encoding.UTF8;
            }
            return encoding;
        }

        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// BOM（Byte Order Mark），字节顺序标记，出现在文本文件头部，Unicode编码标准中用于标识文件是采用哪种格式的编码。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
            byte curByte; //当前分析的字节. 
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }
        #endregion

        #region Close Process (Ctrl_C)
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
        }

        public static async Task StopProcess(Process process)
        {
            if (AttachConsole((uint)process.Id))
            {
                // NOTE: each of these functions could fail. Error-handling omitted
                // for clarity. A real-world program should check the result of each
                // call and handle errors appropriately.
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, 0);
                await Task.Run(() => ProcessExited(process));
                SetConsoleCtrlHandler(null, false);
                FreeConsole();
            }
            else
            {
                int hresult = Marshal.GetLastWin32Error();
                Exception e = Marshal.GetExceptionForHR(hresult);

                throw new InvalidOperationException(
                    $"ERROR: failed to attach console to process {process.Id}: {e?.Message ?? hresult.ToString()}");
            }
        }

        private static void ProcessExited(Process process)
        {
            while (!process.HasExited)
            {
                Thread.Sleep(1000);
            }
        }
        #endregion
    }
}
