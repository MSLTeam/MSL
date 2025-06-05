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
        private const int MaxLogFiles = 5; // 最多保留的日志文件数量

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

            // 在初始化时执行一次日志清理
            CleanupLogs();
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="content">日志内容</param>
        /// <param name="level">日志级别（默认为INFO）</param>
        public static void WriteLog(string content, LogLevel level = LogLevel.INFO)
        {
            // 检查是否已初始化
            if (string.IsNullOrEmpty(_logDirectory))
            {
                // 抛出异常比直接在控制台输出错误更好，因为它能更早地暴露配置问题。
                throw new InvalidOperationException("日志帮助类尚未初始化，请先调用 LogHelper.Init() 方法。");
            }

            // 使用 lock 确保线程安全。这是至关重要的，见下面的详细解释。
            lock (_lock)
            {
                try
                {
                    // 构造文件名：MSL_20231027.log
                    string fileName = $"MSL_{DateTime.Now:yyyyMMdd}.log";
                    // 构造完整文件路径
                    string filePath = Path.Combine(_logDirectory, fileName);

                    // 构造日志条目，格式：[时间戳] [级别] 内容
                    // 使用 Environment.NewLine 确保跨平台的换行符正确性
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {content}";

                    // 【修改点】使用 StreamWriter 来写入文件
                    // 'using' 语句确保 StreamWriter 在使用完毕后被正确关闭和释放资源，即使发生异常。
                    // 第二个参数 'true' 表示以追加模式打开文件。
                    // 第三个参数指定编码，以支持中文等字符。
                    using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        // 使用 WriteLine 方法会自动在末尾添加换行符
                        writer.WriteLine(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    // 如果写入日志时发生异常，可以考虑在此处处理
                    // 例如，写入到控制台或系统的事件查看器
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
                    var logFiles = Directory.GetFiles(_logDirectory, "MSL_*.log");

                    // 如果文件数量未超过限制，则无需清理
                    if (logFiles.Length <= MaxLogFiles)
                    {
                        return;
                    }

                    // 【优化点】按文件名排序比按创建时间更可靠
                    // 因为文件名（MSL_20231027.log）直接反映了日志的日期。
                    // 文件创建时间可能会因为复制、移动等操作而改变。
                    var filesToDelete = logFiles
                        .Select(f => new FileInfo(f))
                        .OrderBy(fi => fi.Name) // 按文件名升序排序，最早的日期在前
                        .Take(logFiles.Length - MaxLogFiles) // 计算出要删除的文件数量
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
                            // 如果删除单个文件失败，记录错误并继续，不影响其他文件删除
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