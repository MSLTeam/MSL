using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MSL.utils
{
    internal class Functions
    {
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

        public static void MoveFolder(string sourcePath, string destPath, bool deleteSource = true)
        {
            if (Directory.Exists(sourcePath))
            {
                // 检查目标路径是否是源路径的子目录
                if (destPath.StartsWith(sourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("目标目录不能是源目录的子目录。");
                }
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
                    if (deleteSource)
                    {
                        File.Move(c, destFile);
                    }
                    else
                    {
                        File.Copy(c, destFile);
                    }
                });
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    MoveFolder(c, destDir, deleteSource);
                });
                if (deleteSource)
                {
                    Directory.Delete(sourcePath);
                }
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }

        public static string GetDeviceID()
        {
            // 获取windows-sid
            try
            {
                System.Security.Principal.WindowsIdentity currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
                string sid = currentUser.User.ToString() + "==Ovo**#MSL#**ovO==";
                //MessageBox.Show(sid);
                byte[] dataToHash = new ASCIIEncoding().GetBytes(sid);
                byte[] hashvalue = ((System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("MD5")).ComputeHash(dataToHash);
                string ATR = "";
                // 32 hash
                for (int i = 0; i < 16; i++)
                {
                    ATR += hashvalue[i].ToString("x2");
                }
                return ATR.ToUpper();
            }
            catch
            {
                return null;
            }
        }

        public static int Frpc_GenerateRandomInt()
        {
            Random random = new Random();
            int a = random.Next();
            if (!Directory.Exists("MSL\\frp\\" + a.ToString()))
            {
                return a;
            }
            else
            {
                return Frpc_GenerateRandomInt();
            }
        }

        public static string RandomString(string prefix, int length)
        {
            Random rand = new Random();
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomString = prefix;

            for (int i = 0; i < length; i++)
            {
                randomString += chars[rand.Next(chars.Length)];
            }
            return randomString.ToString();
        }

        #region Install Forge
        public static bool CheckForgeInstaller(string _filename)
        {
            try
            {
                using (FileStream fs = File.OpenRead(_filename))
                using (ZipFile zipFile = new ZipFile(fs))
                {
                    foreach (ZipEntry entry in zipFile)
                    {
                        if (entry.Name.Equals("install_profile.json", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

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
                //string forgeVersion;
                if (!fastMode)
                {
                    Process process = new Process();
                    process.StartInfo.WorkingDirectory = _base;
                    process.StartInfo.FileName = _java;
                    process.StartInfo.Arguments = "-jar " + filename + " -installServer";
                    process.Start();
                    process.WaitForExit();
                }
                try
                {
                    /*
                    try
                    {
                        //删除Forge安装器文件
                        File.Delete(_base + "\\" + filename);
                    }
                    catch
                    {
                        Console.WriteLine("Delete Forge Installer Failed");
                    }
                    */

                    /*
                    if (File.Exists(Path.Combine(_base, "run.bat")))
                    {
                        string[] args = File.ReadAllLines(Path.Combine(_base, "run.bat"));
                        foreach (string arg in args)
                        {
                            if (arg.Contains("@libraries"))
                            {
                                string _arg= arg.Substring(arg.IndexOf("@libraries"));
                                if(_arg.Contains(" %*"))
                                {
                                    _arg = _arg.Replace(" %*", string.Empty);
                                }
                                return _arg;
                            }
                        }
                    }
                    */

                    string argsFile = CheckArgsFile(_base, "libraries");
                    //MessageBox.Show(argsFile);
                    if (argsFile != null)
                    {
                        return argsFile;
                    }

                    DirectoryInfo directoryInfo = new DirectoryInfo(_base);
                    FileInfo[] fileInfo = directoryInfo.GetFiles();
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name.Contains(mcVersion) && file.Name.EndsWith(".jar"))
                        {
                            if (file.Name.Contains("forge") && (file.Name != filename) && (!file.Name.Contains("installer")) && (!file.Name.Contains("universal")) && (!file.Name.Contains("server") && (!file.Name.Contains("shim"))))
                            {
                                if (!CheckForgeInstaller(file.FullName))
                                {
                                    return file.FullName.Replace(_base + @"\", "");
                                }
                            }
                        }
                    }
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name.Contains(mcVersion) && file.Name.EndsWith(".jar"))
                        {
                            if (file.Name.Contains("forge") && (file.Name != filename) && (!file.Name.Contains("installer")) && (!file.Name.Contains("server") && (!file.Name.Contains("shim"))))
                            {
                                if (!CheckForgeInstaller(file.FullName))
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

        private static string CheckArgsFile(string basePath, string relativePath)
        {
            string fullPath = Path.Combine(basePath, relativePath);
            if (Directory.Exists(fullPath))
            {
                string[] subFolders = Directory.GetDirectories(fullPath);
                foreach (string subFolder in subFolders)
                {
                    string argsFilePath = Path.Combine(subFolder, "win_args.txt");
                    if (File.Exists(argsFilePath))
                    {
                        string forgeVersion = Path.GetFileName(subFolder);
                        string resultPath = $"@{Path.Combine(relativePath, forgeVersion, "win_args.txt").Replace("\\", "/")}";
                        //MessageBox.Show(resultPath);
                        return resultPath;
                    }
                    else
                    {
                        // Recursive call to check subdirectories
                        string result = CheckArgsFile(basePath, Path.Combine(relativePath, Path.GetFileName(subFolder)));
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
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
