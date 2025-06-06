using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSL.utils
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        INFO,  // 普通信息
        WARN,  // 警告
        ERROR, // 错误
        FATAL  // 致命错误
    }

    /// <summary>
    /// 一个简单的、线程安全的日志工具类
    /// </summary>
    public static class LogHelper
    {
        // 核心：私有静态只读对象，专门用于锁定，确保线程安全
        private static readonly object _lock = new object();
        private static string _logDirectory = string.Empty;

        // 【新增】用于存储本次程序运行所使用的日志文件的完整路径
        private static string _currentLogFilePath = string.Empty;

        private const int MaxLogFiles = 5; // 最多保留的历史日志文件数量

        /// <summary>
        /// 初始化日志工具。请在应用程序启动时调用此方法。
        /// </summary>
        public static void Init()
        {
            // 获取程序根目录
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // 拼接日志文件夹路径：[程序根目录]/MSL/logs
            _logDirectory = Path.Combine(baseDirectory, "MSL", "logs");

            // 如果目录不存在，则创建它
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // 【修改点】在创建新文件之前，先执行一次日志清理，清理的是之前运行产生的旧日志
            CleanupLogs();

            // 【核心修改】为本次程序启动创建一个唯一的日志文件名
            // 格式包含年月日时分秒毫秒，确保每次启动都是新文件，且文件名能自然排序
            string fileName = $"MSL_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";

            // 【修改点】将本次运行的日志文件完整路径保存到静态变量中
            _currentLogFilePath = Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="content">日志内容</param>
        /// <param name="level">日志级别（默认为INFO）</param>
        public static void WriteLog(string content, LogLevel level = LogLevel.INFO)
        {
            // 检查 _currentLogFilePath 是否已在 Init() 中被赋值
            if (string.IsNullOrEmpty(_currentLogFilePath))
            {
                // 抛出异常比直接在控制台输出错误更好，因为它能更早地暴露配置问题。
                throw new InvalidOperationException("日志帮助类尚未初始化，请先调用 LogHelper.Init() 方法。");
            }

            // 使用 lock 确保线程安全。
            lock (_lock)
            {
                try
                {
                    // 不再动态计算文件名，直接使用初始化时生成的路径
                    // string fileName = $"MSL_{DateTime.Now:yyyyMMdd}.log"; // <- 旧代码
                    // string filePath = Path.Combine(_logDirectory, fileName); // <- 旧代码

                    // 构造日志条目，格式：[时间戳] [级别] 内容
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {content}";

                    // 使用 'using' 语句和 StreamWriter 将日志条目追加到当前日志文件中
                    using (StreamWriter writer = new StreamWriter(_currentLogFilePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    // 如果写入日志时发生异常，可以考虑在此处处理
                    Console.WriteLine($"写入日志失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理旧的日志文件，只保留最新的指定数量的文件
        /// </summary>
        private static void CleanupLogs()
        {
            // 使用 lock 确保在清理时不会与写入操作冲突
            lock (_lock)
            {
                try
                {
                    // 获取日志目录中所有符合命名规则的日志文件
                    // 新的文件名 "MSL_yyyyMMdd_HHmmss_fff.log" 仍然匹配 "MSL_*.log" 模式
                    var logFiles = Directory.GetFiles(_logDirectory, "MSL_*.log");

                    // 如果文件数量未超过限制，则无需清理
                    if (logFiles.Length <= MaxLogFiles)
                    {
                        return;
                    }

                    // 【无需修改】按文件名排序非常可靠，因为 "yyyyMMdd_HHmmss_fff" 格式保证了文件名越新，字符串越大。
                    var filesToDelete = logFiles
                        .Select(f => new FileInfo(f))
                        .OrderBy(fi => fi.Name) // 按文件名升序排序，最早的文件在前
                        .Take(logFiles.Length - MaxLogFiles) // 计算出要删除的旧文件
                        .ToList();

                    // 删除这些旧文件
                    foreach (var fileInfo in filesToDelete)
                    {
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                            // 如果删除单个文件失败，记录错误并继续
                            Console.WriteLine($"删除旧日志文件 {fileInfo.Name} 失败: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 如果在清理过程中发生其他异常
                    Console.WriteLine($"清理日志文件时发生错误: {ex.Message}");
                }
            }
        }
    }
}