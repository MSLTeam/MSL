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

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 这个事件可以阻止程序崩溃
            e.Handled = true; // 关键！表示异常已经被处理，阻止程序默认的崩溃行为

            // 检查 e.Exception 是否为 null
            var exception = e.Exception;
            string errorMessage = exception?.Message ?? "发生了一个未知错误。";
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";
            LogHelper.WriteLog($"捕获到UI线程异常: {fullTrace}", LogLevel.FATAL);
            var msg = MessageBox.Show(
                $"程序在运行的时候发生了异常（UI线程），异常信息：\n{errorMessage}\n" +
                "请检查您是否安装了.NET Framework 4.7.2，若软件闪退，请联系作者进行反馈！\n\n" +
                "点击“是”以查看详细异常追踪。",
                "UI线程错误", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (msg == MessageBoxResult.Yes)
            {
                MessageBox.Show(fullTrace, "详细异常信息");
            }
        }

        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 这个事件无法阻止程序崩溃！
            // e.IsTerminating 在 .NET Framework 中通常为 true，表示程序即将终止
            // 它的主要作用是在程序崩溃前记录致命错误日志

            // 获取异常对象
            var exception = e.ExceptionObject as Exception;
            string errorMessage = exception?.Message ?? "发生了一个无法恢复的未知错误。";
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";

            // 在这里，你不能安全地显示MessageBox，因为程序可能处于不稳定状态。
            // 最好的做法是记录日志到文件。
            try
            {
                // 尝试记录日志
                LogHelper.WriteLog($"捕获到致命的非UI线程异常，程序即将退出: {fullTrace}", LogLevel.FATAL);

                // 你可以尝试弹出一个简单的消息框，但它可能不会显示，或显示后程序立刻关闭
                MessageBox.Show(
                    $"程序遇到了一个致命错误（非UI线程），即将关闭。\n错误信息已记录到日志文件中。\n\n错误详情: {errorMessage}",
                    "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // 连记录日志都失败了，那就没办法了
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
