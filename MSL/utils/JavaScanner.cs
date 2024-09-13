using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSL.utils
{
    public class JavaScanner
    {
        public class JavaInfo
        {
            public string Path { get; set; }
            public string Version { get; set; }

            /*
            public string Architecture { get; set; }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + (Path != null ? Path.GetHashCode() : 0);
                hash = hash * 23 + (Version != null ? Version.GetHashCode() : 0);
                return hash;
            }

            public override bool Equals(object Obj)
            {
                return Obj is JavaInfo TargetObj &&
                       Path == TargetObj.Path &&
                       Version == TargetObj.Version;
            }
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
            */
        }

        #region MCSL Java Scan
        /* -----------------------------------------------------------
           MCServerLauncher Future Java Scanner
           Original Author: LxHTT & AresConnor & Tigercrl
           You can only use this file if you are permitted to do so,
           otherwise you may be prosecuted for violating the law.
           Copyright (c) 2022-2024 MCSLTeam. All rights reserved.
        -------------------------------------------------------------- */
        private readonly List<string> MatchedKeys = new()
        {
            "1.", "bin", "cache", "client", "craft", "data", "download", "eclipse", "mine", "mc", "launch",
            "hotspot", "java", "jdk", "jre", "zulu", "dragonwell", "jvm", "microsoft", "corretto", "sigma",
            "mod", "mojang", "net", "netease", "forge", "liteloader", "fabric", "game", "vanilla", "server",
            "optifine", "oracle", "path", "program", "roaming", "local", "run", "runtime", "software", "daemon",
            "temp", "users", "users", "x64", "x86", "lib", "usr", "env", "ext", "file", "data", "green",
            "我的", "世界", "前置", "原版", "启动", "启动", "国服", "官启", "官方", "客户", "应用", "整合", "组件",
            Environment.UserName, "新建文件夹", "服务", "游戏", "环境", "程序", "网易", "软件", "运行", "高清",
            "badlion", "blc", "lunar", "tlauncher", "soar", "cheatbreaker", "hmcl", "pcl", "bakaxl", "fsm", "vape",
            "jetbrains", "intellij", "idea", "pycharm", "webstorm", "clion", "goland", "rider", "datagrip",
            "rider", "appcode", "phpstorm", "rubymine", "jbr", "android", "mcsm", "msl", "mcsl", "3dmark", "arctime",
        };
        private readonly List<string> ExcludedKeys = new() { "$", "{", "}", "__", "office" };
        //private List<JavaInfo> FoundedJava = [];

        private Process TryStartJava(string Path)
        {
            ProcessStartInfo JavaInfo = new()
            {
                FileName = Path,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process JavaProcess = new() { StartInfo = JavaInfo };
            JavaProcess.Start();
            return JavaProcess;
        }

        private string TryRegexJavaVersion(string JavaOutput)
        {
            var VersionPattern = @"(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:[._](\d+))?(?:-(.+))?";
            var ReMatch = Regex.Match(JavaOutput, VersionPattern);
            if (ReMatch.Success)
            {
                return string.Join(".", ReMatch.Groups.Cast<Group>().Skip(1).Where(g => g.Success).Select(g => g.Value));
            }
            return "Unknown";
        }

        private bool IsMatchedKey(string Path)
        {
            foreach (string excludedKey in ExcludedKeys)
            {
                if (Path.Contains(excludedKey))
                {
                    return false;
                }
            }
            foreach (string matchedKey in MatchedKeys)
            {
                if (Path.Contains(matchedKey))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsMatchedWindows(string Dir)
        {
            return Dir.EndsWith("java.exe");
        }

        private bool IsMatchedUnix(string Dir)
        {
            return Dir.EndsWith("java");
        }

        private async Task<List<JavaInfo>> StartScan(string Path)
        {
            Func<string, bool> Matcher = Environment.OSVersion.Platform == PlatformID.Win32NT ? IsMatchedWindows : IsMatchedUnix;
            List<Process> JavaProcesses = await SingleScanJob(Path, Matcher);
            List<JavaInfo> javaInfos = [];
            foreach (Process JavaProcess in JavaProcesses)
            {
                JavaProcess.WaitForExit();
                string PossibleJavaOutput = JavaProcess.StandardError.ReadToEnd();
                string PossibleJavaVersion = TryRegexJavaVersion(PossibleJavaOutput);
                if (PossibleJavaVersion != "Unknown")
                {
                    JavaInfo javaInfo = new()
                    {
                        Path = JavaProcess.StartInfo.FileName,
                        Version = PossibleJavaVersion,
                        //Architecture = RuntimeInformation.OSArchitecture.ToString()
                    };
                    javaInfos.Add(javaInfo);
                }
            }
            return javaInfos;
        }

        private async Task<List<Process>> SingleScanJob(string WorkingPath, Func<string, bool> Matcher)
        {
            List<Process> JavaProcesses = new();
            if (File.Exists(WorkingPath))
            {
                //Log.Information($"[JVM] \"{WorkingPath}\" is a file, skipping");
                return JavaProcesses; // Skip if it is a file
            }
            try
            {
                foreach (string PossibleFile in Directory.GetFileSystemEntries(WorkingPath))
                {
                    string AbsoluteFilePath = Path.GetFullPath(PossibleFile);
                    if (File.Exists(AbsoluteFilePath))
                    {
                        if (Matcher(Path.GetFileName(PossibleFile)))
                        {
                            //Log.Information($"[JVM] Found possible Java \"{AbsoluteFilePath}\", plan to check it");
                            JavaProcesses.Add(TryStartJava(AbsoluteFilePath));
                        }
                        else { }
                    }
                    else if (IsMatchedKey(Path.GetFileName(PossibleFile).ToLower()))  // Deliver a deeper search
                    {
                        //Log.Information($"[JVM] Found possible Java path \"{AbsoluteFilePath}\", deliver a deeper search");
                        JavaProcesses.AddRange(await SingleScanJob(AbsoluteFilePath, Matcher));
                    }
                    else { }
                }
            }
            catch// (Exception ex) 
            {
                //Log.Error($"[JVM] A error occured while searching dir \"{WorkingPath}\", Reason: {ex.Message}");
            }
            return JavaProcesses;
        }

        public async Task<List<JavaInfo>> ScanJava()
        {
            //Log.Information("[JVM] Start scanning available Java");

            Stopwatch sw = new();
            sw.Start();

            List<JavaInfo> PossibleJavaPathList = new();
            for (var i = 65; i <= 90; i++)
            {
                string drive = $"{(char)i}:\\";
                if (Directory.Exists(drive))
                {
                    PossibleJavaPathList.AddRange(await StartScan(drive));
                }
            }

            sw.Stop();
            TimeSpan ts2 = sw.Elapsed;

            int foo = 0;
            foreach (JavaInfo PossibleJavaPath in PossibleJavaPathList)
            {
                Console.WriteLine($"Java: {PossibleJavaPath.Path}, Version: {PossibleJavaPath.Version}");
                foo++;
            }
            Console.WriteLine($"Total: {foo}, Elapsed time: {ts2.TotalMilliseconds}ms");
            return PossibleJavaPathList;
        }
        #endregion

        #region DeepScan
        public List<JavaInfo> SearchJava()
        {
            // 黑名单文件夹
            string[] blackList =
            [
                "Windows",
                "Temp",
                "ProgramData",
                "System Volume Information",
                "$Recycle.Bin",
                "$RECYCLE.BIN",
                "Recovery"
            ];
            // 存储检测到的 Java 安装
            HashSet<JavaInfo> javaInstalls = [];
            // 获取所有磁盘
            DriveInfo[] drives = DriveInfo.GetDrives();
            // 使用并行处理来加速扫描过程
            Parallel.ForEach(drives, drive =>
            {
                // 获取磁盘的驱动器字母
                string driveLetter = drive.Name.Substring(0, 1);
                // 拼接完整的磁盘路径
                string diskPath = Path.Combine(driveLetter + ":\\");
                // 搜索该磁盘下的所有子文件夹和 release 文件
                SearchJava(diskPath, javaInstalls, blackList);
            });
            // 输出检测到的 Java 安装
            List<JavaInfo> strings = [.. javaInstalls];
            return strings;
        }


        // 搜索指定文件夹下的所有子文件夹和 release 文件
        private void SearchJava(string folderPath, HashSet<JavaInfo> javaInstalls, string[] blackList)
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
                        JavaInfo javaInstall = SearchJavaRelease(releaseFile);
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
        private JavaInfo SearchJavaRelease(string releaseFile)
        {
            // 读取 release 文件的内容
            string releaseContent = File.ReadAllText(releaseFile);
            // 使用正则表达式匹配 Java 版本
            Match match = Regex.Match(releaseContent, @"JAVA_VERSION=""([\d\._a-zA-Z]+)""");
            // 如果匹配成功，返回 Java 版本和可执行文件的路径
            if (match.Success)
            {
                string javaVersion = match.Groups[1].Value;
                JavaInfo info = new()
                {
                    Version = javaVersion,
                    Path = Path.Combine(Path.GetDirectoryName(releaseFile), "bin", "java.exe")
                };
                //return "Java" + javaVersion + ":" + Path.Combine(Path.GetDirectoryName(releaseFile), "bin", "java.exe");
                return info;
            }
            // 如果匹配失败，返回 null
            return null;
        }
        #endregion
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
    }
}