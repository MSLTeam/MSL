using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSL.utils
{
    internal class MCServerService : IDisposable
    {
        public bool ProblemSolveSystem = false;
        public string ProblemFound;

        public MCServerService() { }

        private readonly List<(string pattern, string message)> errorPatterns = new()
        {
            (@"UnsupportedClassVersionError.*\(class file version (\d+)", "*不支持的Class版本：您的Java版本可能太低！\n 请使用Java{0}或以上版本！\n"),
            (@"Unsupported Java detected.*Only up to (\S+)", "*不匹配的Java版本：\n 请使用{0}！\n"),
            (@"requires running the server with (\S+)", "*不匹配的Java版本：\n 请使用{0}！\n"),
            (@"Invalid or corrupt jarfile", "*服务端核心不完整，请重新下载！\n"),
            (@"OutOfMemoryError", "*服务器内存分配过低或过高！\n"),
            (@"Invalid maximum heap size.*", "*服务器最大内存分配有误！\n {0}\n"),
            (@"Unrecognized VM option '([^']+)'", "*服务器JVM参数有误！请前往设置界面进行查看！\n 错误的参数为：{0}\n"),
            (@"There is insufficient memory for the Java Runtime Environment to continue", "*JVM内存分配不足，请尝试增加系统的虚拟内存（不是内存条！具体方法请自行上网查找）！\n"),
            (@"进程无法访问", "*文件被占用，您的服务器可能多开，可尝试重启电脑解决！\n"),
            (@"FAILED TO BIND TO PORT", "*端口被占用，您的服务器可能多开，可尝试重启电脑解决！\n"),
            (@"Unable to access jarfile", "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符，请及时修改！\n"),
            (@"加载 Java 代理时出错", "*无法访问JAR文件！您的服务端可能已损坏或路径中含有中文或其他特殊字符，请及时修改！\n"),
            (@"ArrayIndexOutOfBoundsException", "*开启服务器时发生数组越界错误，请尝试更换服务端再试！\n"),
            (@"ClassCastException", "*开启服务器时发生类转换异常，请检查Java版本是否匹配，或者让开服器为您下载Java环境（设置界面更改）！\n"),
            (@"could not open.*jvm.cfg", "*Java异常，请检查Java环境是否正常，或者让开服器为您下载Java环境（设置界面更改）！\n"),
            (@"Failed to download vanilla jar", "*下载原版核心文件失败，您可尝试使用代理或更换服务端为Spigot端！\n"),
            (@"Exception in thread ""main""", "*服务端核心Main方法报错，可能是Java版本不正确或服务端（及库文件）不完整，请尝试更换Java版本或重新下载安装服务端核心！\n"),
            (@"@libraries.net|找不到或无法加载主类", "*Java版本过低，请勿使用Java8及以下版本的Java！\n"),
            (@"Could not load '([^']+)' in folder '([^']+)'", "*无法加载{1}！\n 名称：{0}\n"),
            (@"Could not load '([^']+)' plugin", "*无法加载插件！\n 插件名称：{0}\n"),
            (@"Failed to open plugin jar ([^']+).jar", "*无法加载插件！\n 插件名称：{0}.jar\n"),
            ("Failed to open plugin jar ([^']+)\n", "*无法加载插件！\n 插件名称：{0}\n"),
            (@"Error loading plugin '([^']+)'", "*无法加载插件！\n 插件名称：{0}\n"),
            (@"Error occurred while enabling (\S+) ", "*在启用 {0} 时发生了错误\n"),
            (@"Encountered an unexpected exception", "*服务器出现意外崩溃，可能是由于模组冲突，请检查您的模组列表（如果使用的是整合包，请使用整合包制作方提供的Server专用包开服）\n"),
            (@"net.minecraft.client.Main", "*您使用的似乎是客户端核心，无法开服，请使用正确的服务端核心再试！\n"),
        };

        public void ProblemSystemHandle(string msg)
        {
            foreach (var (pattern, message) in errorPatterns)
            {
                var match = Regex.Match(msg, pattern);
                if (match.Success)
                {
                    var resolvedMessage = message;
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        resolvedMessage = resolvedMessage.Replace($"{{{i - 1}}}", match.Groups[i].Value);
                        // Console.WriteLine(resolvedMessage);
                    }

                    if (!string.IsNullOrEmpty(resolvedMessage) && (string.IsNullOrEmpty(ProblemFound) || !ProblemFound.Contains(resolvedMessage)))
                    {
                        ProblemFound += resolvedMessage;
                    }

                }
            }

            if (msg.Contains("Mod") && msg.Contains("requires"))
            {
                string resolvedMessage = HandleModRequirement(msg);
                if (!string.IsNullOrEmpty(resolvedMessage) && (string.IsNullOrEmpty(ProblemFound) || !ProblemFound.Contains(resolvedMessage)))
                {
                    ProblemFound += resolvedMessage;
                }
            }
        }

        private string HandleModRequirement(string msg)
        {
            string modNamePattern = @"Mod (\w+) requires";
            string preModPattern = @"requires (\w+ \d+\.\d+\.\d+)";

            Match modNameMatch = Regex.Match(msg, modNamePattern);
            Match preModMatch = Regex.Match(msg, preModPattern);

            if (modNameMatch.Success && preModMatch.Success)
            {
                string modName = modNameMatch.Groups[1].Value;
                string preMod = preModMatch.Groups[1].Value;
                string resolvedMessage = $"*{modName} 模组出现问题！该模组需要 {preMod}！\n";

                if (msg.Contains("or above"))
                {
                    resolvedMessage = $"*{modName} 模组出现问题！该模组需要 {preMod} 或以上版本！\n";
                }

                return resolvedMessage;
            }
            return string.Empty;
        }

        public void Dispose()
        {
            ProblemFound = null;
        }
    }

    internal class MCSLogHandler : IDisposable
    {
        public void Dispose()
        {
            CleanupResources();
            ServerService.Dispose();
            ShieldLog = null;
            HighLightLog = null;
        }

        private readonly Action<string, SolidColorBrush> _logAction;
        private readonly Action<string> _infoHandler;
        private readonly Action<string> _warnHandler;
        private readonly Action _encodingIssueHandler;
        public bool IsShieldStackOut = true;
        public bool IsShowOutLog = true;
        public bool IsFormatLogPrefix = true;
        public bool IsMSLFormatedLog = true;
        public string[] ShieldLog;
        public string[] HighLightLog;
        public MCServerService ServerService { get; private set; } = new MCServerService();

        public class LogConfig
        {
            public string Prefix { get; set; }
            public SolidColorBrush Color { get; set; }
        }

        public Dictionary<int, LogConfig> LogInfo = new()
        {
            { 1, new LogConfig { Prefix = "信息", Color = ConfigStore.LogColor.INFO } }, // 以“[”开头并含有INFO字样的日志
            { 2, new LogConfig { Prefix = "警告", Color = ConfigStore.LogColor.WARN } }, // 以“[”开头并含有WARN字样的日志
            { 3, new LogConfig { Prefix = "错误", Color = ConfigStore.LogColor.ERROR } }, // 以“[”开头并含有ERROR字样的日志
            { 11, new LogConfig { Prefix = string.Empty, Color = ConfigStore.LogColor.INFO } }, // 不以“[”开头但含有INFO字样的日志
            { 12, new LogConfig { Prefix = string.Empty, Color = ConfigStore.LogColor.WARN } }, // 不以“[”开头但含有WARN字样的日志
            { 13, new LogConfig { Prefix = string.Empty, Color = ConfigStore.LogColor.ERROR } }, // 不以“[”开头但含有ERROR字样的日志
            { 0, new LogConfig { Prefix = string.Empty, Color = ConfigStore.LogColor.INFO } }, // 啥也不含的日志
            { 100, new LogConfig { Prefix = string.Empty, Color = ConfigStore.LogColor.HIGHLIGHT } } // 高亮日志
        };

        public MCSLogHandler(
        Action<string, SolidColorBrush> logAction,
        Action<string> infoHandler = null,
        Action<string> warnHandler = null,
        Action encodingIssueHandler = null)
        {
            _logAction = logAction;
            _infoHandler = infoHandler;
            _warnHandler = warnHandler;
            _encodingIssueHandler = encodingIssueHandler;

            // 初始化日志处理定时器
            _logProcessTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PROCESS_INTERVAL_MS)
            };
            _logProcessTimer.Tick += ProcessLogBuffer;
        }

        public void ProcessLogMessage(string message, int? _level = null, bool noPrefix = false, bool noFormatPrefix = false)
        {
            int level;
            string content;
            if (_level != null)
                (level, content) = (_level.Value, message);
            else
                (level, content) = ParseLogMessage(message);

            if (level == 1 || level - 10 == 1)
                LogHandleInfo(message);
            else if (level == 2 || level - 10 == 2)
                LogHandleWarn(message);

            if (noFormatPrefix)
                PrintFormattedLog(level, message, true);
            else
                PrintFormattedLog(level, content, noPrefix);

            if (message.Contains("�") || message.Contains("□"))
                HandleEncodingIssue();
        }

        public void ProcessGroupLogMessage(string message, int level)
        {
            if (level == 1 || level - 10 == 1)
                LogHandleInfo(message);
            else if ((level == 2 || level - 10 == 2))
                LogHandleWarn(message);

            PrintFormattedLog(level, message, true);

            if (message.Contains("�") || message.Contains("□"))
                HandleEncodingIssue();
        }

        private (int Level, string Content) ParseLogMessage(string message)
        {
            if (message.StartsWith("["))
            {
                foreach (var level in new[] { "INFO]", "WARN]", "ERROR]" })
                {
                    if (message.Contains(level))
                    {
                        var logLevel = GetLogLevelFromString(level.TrimEnd(']'));
                        var content = message.Substring(message.IndexOf(level) + level.Length);
                        return (logLevel, content);
                    }
                }
            }
            else
            {
                foreach (var level in new[] { "INFO", "WARN", "ERROR" })
                {
                    if (message.Contains(level))
                    {
                        return (GetLogLevelFromString(level) + 10, message);
                    }
                }
            }

            return (0, message);
        }

        private int GetLogLevelFromString(string level) => level switch
        {
            "INFO" => 1,
            "WARN" => 2,
            "ERROR" => 3,
            _ => 0
        };

        private void PrintFormattedLog(int level, string content, bool noPrefix)
        {
            if (level != 0)
            {
                if (level > 10)
                {
                    var tempColor = LogInfo[0].Color;
                    PrintLog(content, LogInfo[level].Color);
                    LogInfo[0].Color = tempColor;
                }
                else
                {
                    if (noPrefix)
                        PrintLog(content, LogInfo[level].Color);
                    else
                        PrintLog($"[{DateTime.Now:T} {LogInfo[level].Prefix}]{content}", LogInfo[level].Color);
                }
            }
            else
            {
                PrintLog(content, LogInfo[level].Color);
            }
        }

        // 日志缓冲区相关
        public readonly ConcurrentQueue<string> _logBuffer = new ConcurrentQueue<string>();
        public readonly DispatcherTimer _logProcessTimer;
        private const int MAX_BATCH_SIZE = 100; // 每次处理的最大日志数量
        private const int PROCESS_INTERVAL_MS = 150; // 日志处理间隔(毫秒)

        // 批量处理日志缓冲区
        private void ProcessLogBuffer(object sender, EventArgs e)
        {
            // 如果没有日志，不处理
            if (_logBuffer.IsEmpty)
            {
                return;
            }

            // 创建批处理列表
            var batch = new List<string>();

            // 从队列中取出日志，最多取MAX_BATCH_SIZE条
            for (int i = 0; i < MAX_BATCH_SIZE && !_logBuffer.IsEmpty; i++)
            {
                if (_logBuffer.TryDequeue(out string entry))
                {
                    batch.Add(entry);
                }
            }

            // 如果取出了日志，则处理它们
            if (batch.Count > 0)
            {
                ProcessLogBatch(batch);
            }
        }

        // 批量处理日志
        private void ProcessLogBatch(List<string> batch)
        {
            // 按日志类型分组处理
            var filteredLogs = new Dictionary<int, (bool, List<string>)>();

            int i = 0;
            filteredLogs[i] = (false, []);
            foreach (var msg in batch)
            {
                // 崩溃分析系统
                if (ServerService.ProblemSolveSystem)
                {
                    ServerService.ProblemSystemHandle(msg);
                }

                // 过滤不需要显示的日志
                if ((msg.Contains("\tat ") && IsShieldStackOut) ||
                    (ShieldLog != null && ShieldLog.Any(s => msg.Contains(s))) ||
                    !IsShowOutLog || msg.Contains("Advanced terminal features"))
                {
                    continue;
                }

                if (HighLightLog != null && HighLightLog.Any() &&
                    HighLightLog.Any(s => msg.Contains(s)))
                {
                    i++;
                    filteredLogs[i] = (true, [msg]);
                    i++;
                    filteredLogs[i] = (false, []);
                    continue;
                }

                filteredLogs[i].Item2.Add(msg);
            }

            // 批量展示日志
            if (filteredLogs.Count > 0)
            {
                // 如果启用了MCS日志处理
                if (IsMSLFormatedLog)
                {
                    foreach (var everyFilter in filteredLogs)
                    {
                        if (everyFilter.Value.Item1 == true)
                            ProcessLogMessage(everyFilter.Value.Item2.First(), 100);
                        else
                        {
                            // 分组处理相同类型的日志
                            var logGroups = GroupSimilarLogs(everyFilter.Value.Item2);
                            foreach (var group in logGroups)
                            {
                                // 对于每组日志，一次性添加到UI
                                ProcessLogGroup(group);
                            }
                        }
                    }
                }
                else
                {
                    // 标准处理模式
                    foreach (var msg in filteredLogs)
                    {
                        foreach (var emsg in msg.Value.Item2)
                        {
                            PrintLog(emsg, (SolidColorBrush)HandyControl.Themes.ThemeResources.Current.AccentColor);
                        }
                    }
                }
            }
        }

        // 将相似日志分组
        public List<List<string>> GroupSimilarLogs(List<string> logs)
        {
            var result = new List<List<string>>();
            var currentGroup = new List<string>();
            int? currentLogType = null;

            foreach (var entry in logs)
            {
                var (level, _) = ParseLogMessage(entry);

                // 如果这是一个新的日志类型，或者组太大了，开始一个新组
                if (currentLogType != level || currentGroup.Count >= 20)
                {
                    if (currentGroup.Count > 0)
                    {
                        result.Add(currentGroup);
                        currentGroup = new List<string>();
                    }
                    currentLogType = level;
                }

                currentGroup.Add(entry);
            }

            // 添加最后一组
            if (currentGroup.Count > 0)
            {
                result.Add(currentGroup);
            }

            return result;
        }

        // 处理一组相同类型的日志
        public void ProcessLogGroup(List<string> group)
        {
            if (group.Count == 1)
            {
                // 单条日志直接处理
                if (IsFormatLogPrefix)
                    ProcessLogMessage(group[0]);
                else
                    ProcessLogMessage(group[0], noFormatPrefix: true);
                return;
            }

            // 多条相同类型的日志，合并处理
            // 构建合并后的日志文本
            int level = -1;
            var sb = new StringBuilder();
            foreach (var msg in group)
            {
                if (level == -1)
                {
                    string _msg = string.Empty;
                    (level, _msg) = ParseLogMessage(msg);
                    if (IsFormatLogPrefix && msg.StartsWith("["))
                        sb.AppendLine($"[{DateTime.Now:T} {LogInfo[level].Prefix}]" + _msg);
                    else
                        sb.AppendLine(msg);
                    continue;
                }
                if (IsFormatLogPrefix && msg.StartsWith("["))
                {
                    sb.AppendLine($"[{DateTime.Now:T} {LogInfo[level].Prefix}]" + ParseLogMessage(msg).Content);
                }
                else
                    sb.AppendLine(msg);
            }

            // 一次性输出
            string combinedMessage = sb.ToString().TrimEnd();
            ProcessGroupLogMessage(combinedMessage, level);
        }

        // 应用程序退出时的清理工作
        public void CleanupResources()
        {
            // 停止定时器
            if (_logProcessTimer != null && _logProcessTimer.IsEnabled)
            {
                _logProcessTimer.Stop();
                _logProcessTimer.IsEnabled = false;
            }

            // 处理剩余的日志
            ProcessLogBuffer(null, null);
        }

        private void HandleEncodingIssue()
        {
            _encodingIssueHandler.Invoke();
        }

        private void PrintLog(string message, SolidColorBrush color)
        {
            _logAction?.Invoke(message, color);
        }

        private void LogHandleInfo(string message)
        {
            _infoHandler?.Invoke(message);
        }

        private void LogHandleWarn(string message)
        {
            _warnHandler?.Invoke(message);
        }
    }
}
