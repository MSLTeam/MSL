using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSL.utils
{
    public class MCSLogHandler : IDisposable
    {
        private readonly Action<string, Color> _logAction;
        private readonly Action<string> _infoHandler;
        private readonly Action<string> _warnHandler;
        private readonly Action _encodingIssueHandler;
        private readonly Action<string> _solveCrashHandler;

        // 日志缓冲区相关
        public readonly ConcurrentQueue<string> _logBuffer = new ConcurrentQueue<string>();
        public readonly DispatcherTimer _logProcessTimer = new DispatcherTimer();

        public bool IsShieldStackOut = true;
        public bool IsShowOutLog = true;
        public bool IsFormatLogPrefix = true;
        public bool IsMSLFormatedLog = true;
        public string[] ShieldLog;
        public string[] HighLightLog;
        public class LogConfig
        {
            public string Prefix { get; set; }
            public Color Color { get; set; }
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

        public MCSLogHandler(MCServerService service,
        Action<string, Color> logAction,
        Action<string> infoHandler,
        Action<string> warnHandler,
        Action encodingIssueHandler,
        Action<string> solveCrashHandler)
        {
            _logAction = logAction;
            _infoHandler = infoHandler;
            _warnHandler = warnHandler;
            _encodingIssueHandler = encodingIssueHandler;
            _solveCrashHandler = solveCrashHandler;

            // 初始化日志处理定时器
            _logProcessTimer.Interval = TimeSpan.FromMilliseconds(100);
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

            // 从队列中取出日志，最多取300条
            for (int i = 0; i < 300 && !_logBuffer.IsEmpty; i++)
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
                _solveCrashHandler.Invoke(msg);

                // 过滤不需要显示的日志
                if ((msg.Contains("\tat ") && IsShieldStackOut) ||
                    (ShieldLog != null && ShieldLog.Any(s => msg.Contains(s))) || !IsShowOutLog)
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
                            PrintLog(emsg, (HandyControl.Themes.ThemeResources.Current.AccentColor as SolidColorBrush)?.Color ?? Colors.White);
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
            }
            
            // 处理剩余的日志
            ProcessLogBuffer(null, null);
        }

        public void Dispose()
        {
            CleanupResources();
            _logProcessTimer.Tick -= ProcessLogBuffer;
            ShieldLog = null;
            HighLightLog = null;
        }

        private void HandleEncodingIssue()
        {
            _encodingIssueHandler.Invoke();
        }

        private void PrintLog(string message, Color color)
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
