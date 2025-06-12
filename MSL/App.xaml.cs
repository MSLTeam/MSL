using HandyControl.Tools;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace MSL
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 1. 订阅 UI 线程的未处理异常事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 2. 订阅非 UI 线程的未处理异常事件
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        }

        // --- UI 线程异常处理 ---
        private async void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 阻止程序崩溃
            e.Handled = true;

            var exception = e.Exception;
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";

            // 记录本地日志
            LogHelper.WriteLog($"捕获到UI线程异常: {fullTrace}", LogLevel.FATAL);

            // 准备提示信息
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"程序在运行的时候发生了异常（UI线程）。");
            messageBuilder.AppendLine("\n我们正在尝试自动上传错误报告...");

            // 异步上传日志并处理结果
            try
            {
                int logId = await HttpService.UploadCrashLogAsync(fullTrace);

                // 上传成功
                string successMessage = $"报告上传成功！您的错误报告ID为: {logId}\n在联系技术支持时请提供此ID。";
                LogHelper.WriteLog(successMessage, LogLevel.INFO);
                messageBuilder.AppendLine($"\n{successMessage}");
            }
            catch (Exception uploadEx)
            {
                // 上传失败
                string failureMessage = $"错误报告上传失败: {uploadEx.Message}";
                LogHelper.WriteLog(failureMessage, LogLevel.ERROR);
                messageBuilder.AppendLine($"\n{failureMessage}");
                messageBuilder.AppendLine("\n错误详情已记录在本地日志中。");
            }

            // 向用户显示所有信息
            MessageBox.Show(messageBuilder.ToString(), "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        // --- 非 UI 线程异常处理 ---
        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 程序即将终止，操作必须快速且可靠 无法阻止崩溃

            var exception = e.ExceptionObject as Exception;
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";

            // 写入本地日志
            LogHelper.WriteLog($"捕获到致命的非UI线程异常，程序即将退出: {fullTrace}", LogLevel.FATAL);

            // 同步上传日志
            try
            {
                // 使用同步方法，因为它会阻塞直到完成（或失败）
                int logId = HttpService.UploadCrashLog(fullTrace);

                // 如果上传成功，追加一条记录到日志文件
                LogHelper.WriteLog($"致命异常报告上传成功。Log ID: {logId}", LogLevel.INFO);
                MessageBox.Show(
    "程序遇到了一个无法恢复的致命错误，即将关闭。\n" +
    "我们已经记录并上传错误报告。\n\n" +
    "错误日志ID：" + logId + "\n\n" + "请将此信息提供给开发者以便更快地解决问题。",
    "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception uploadEx)
            {
                // 如果上传失败，也记录下来
                LogHelper.WriteLog($"致命异常报告上传失败: {uploadEx.Message}", LogLevel.ERROR);
                MessageBox.Show(
    "程序遇到了一个无法恢复的致命错误，即将关闭。\n" +
    "我们已尝试记录并上传错误报告，但是失败了。\n" + uploadEx.Message + "\n\n" +
    "请查看本地日志文件获取详细信息。",
    "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        //以创建Mutex的方式防止同目录多开，避免奇奇怪怪的文件占用错误
        private Mutex _mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            string mutexId = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');

            _mutex = new Mutex(true, mutexId, out bool createdNew);

            if (!createdNew)
            {
                System.Diagnostics.Process progress1 = GetExistProcess();
                if (progress1 != null)
                {
                    ShowMainWindow(progress1);
                    Environment.Exit(0);
                }
            }

            if (Directory.GetCurrentDirectory() + "\\" != AppDomain.CurrentDomain.BaseDirectory)
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }

            try
            {
                Directory.CreateDirectory("MSL");
                if (!File.Exists(@"MSL\config.json"))
                {
                    File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_InitErr"] + ex.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.Instance["MainWindow_GrowlMsg_ConfigErr2"] + ex.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                File.WriteAllText(@"MSL\config.json", string.Format("{{{0}}}", "\n"));
                jsonObject = JObject.Parse(File.ReadAllText(@"MSL\config.json", Encoding.UTF8));
            }
            try
            {
                // 初始化日志系统
                LogHelper.Init();
                LogHelper.WriteLog("MSL正在启动...", LogLevel.INFO);
                
                if (jsonObject["lang"] == null)
                {
                    jsonObject.Add("lang", "zh-CN");
                    string convertString = Convert.ToString(jsonObject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    LogHelper.WriteLog("语言: " + "ZH-CN");
                }
                else
                {
                    if (jsonObject["lang"].ToString() != "zh-CN")
                        LanguageManager.Instance.ChangeLanguage(new CultureInfo(jsonObject["lang"].ToString()));
                    LogHelper.WriteLog("语言: " + jsonObject["lang"].ToString().ToUpper());
                }
            }
            finally
            {
                base.OnStartup(e);
            }
        }

        /// <summary>
        /// 获取运行中的MSL软件进程
        /// </summary>
        /// <returns></returns>
        private static System.Diagnostics.Process GetExistProcess()
        {
            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                foreach (System.Diagnostics.Process process1 in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if ((process1.Id != currentProcess.Id) &&
                         (Assembly.GetExecutingAssembly().Location == currentProcess.MainModule.FileName))
                    {
                        return process1;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #region DllImport...

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        private const int SW_SHOW = 1;
        #endregion

        /// <summary>
        /// 最前端显示主窗体
        /// </summary>
        /// <param name="process"></param>
        private void ShowMainWindow(System.Diagnostics.Process process)
        {
            IntPtr mainWindowHandle1 = process.MainWindowHandle;
            if (mainWindowHandle1 != IntPtr.Zero)
            {
                ShowWindowAsync(mainWindowHandle1, SW_SHOW);
                SetForegroundWindow(mainWindowHandle1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogHelper.WriteLog("程序正在退出...");
            //Logger.Dispose();
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }

    /*
    public class Logger
    {
        public static bool CanWriteLog = false;
        private static StreamWriter writer;
        private static readonly object lockObj = new object();

        static Logger()
        {
            // 初始化 StreamWriter，开启追加模式
            writer = new StreamWriter("MSL\\log.txt", true)
            {
                AutoFlush = true // 自动刷新，保证每次写入都立即保存到文件
            };
        }

        // 清除日志文件
        public static void Clear()
        {
            lock (lockObj)
            {
                if (File.Exists("MSL\\log.txt"))
                {
                    writer.Close();  // 先关闭流再清空文件
                    File.WriteAllText("MSL\\log.txt", string.Empty); // 清空文件
                    writer = new StreamWriter("MSL\\log.txt", true) { AutoFlush = true }; // 重新打开流
                }
            }
        }

        // 日志记录方法
        public static void LogInfo(string message)
        {
            if (CanWriteLog)
                LogMessage("INFO", message);
        }

        public static void LogWarning(string message)
        {
            if (CanWriteLog)
                LogMessage("WARNING", message);
        }

        public static void LogError(string message)
        {
            if (CanWriteLog)
                LogMessage("ERROR", message);
        }

        // 核心日志写入方法
        private static void LogMessage(string level, string message)
        {
            string logEntry = $"{DateTime.Now} [{level}] {message}";
            Console.WriteLine(logEntry);

            lock (lockObj) // 保证线程安全
            {
                writer.WriteLine(logEntry); // 直接写入日志文件
            }
        }

        // 释放资源
        public static void Dispose()
        {
            lock (lockObj)
            {
                writer?.Dispose(); // 确保程序结束时关闭文件流
            }
        }
    }
    */
}
