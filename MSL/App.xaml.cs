using MSL.langs;
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
            // 崩溃处理事件
            DispatcherUnhandledException += (s, e) =>
            {
                e.Handled = true; // 设置为已处理，阻止应用程序崩溃
                //Logger.LogError("An error has occurred:" + e.Exception.ToString());

                var msg = MessageBox.Show("程序在运行的时候发生了异常，异常信息：\n" + e.Exception.Message + "\n请检查您是否安装了.NET Framework 4.7.2，若软件闪退，请联系作者进行反馈！\n\n点击“是”以查看详细异常追踪。", "错误", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (msg == MessageBoxResult.Yes)
                {
                    MessageBox.Show(e.Exception.ToString());
                }
            };
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
                /*
                if (jsonObject["debugMode"] != null && (bool)jsonObject["debugMode"] == true)
                {
                    Logger.CanWriteLog = true;
                    Logger.Clear();
                    Logger.LogWarning("DEBUGMODE ON");
                }
                */
                if (jsonObject["lang"] == null)
                {
                    jsonObject.Add("lang", "zh-CN");
                    string convertString = Convert.ToString(jsonObject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    //Logger.LogInfo("Language: " + "ZH-CN");
                }
                else
                {
                    if (jsonObject["lang"].ToString() != "zh-CN")
                        LanguageManager.Instance.ChangeLanguage(new CultureInfo(jsonObject["lang"].ToString()));
                    //Logger.LogInfo("Language: " + jsonObject["lang"].ToString().ToUpper());
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
            //Logger.LogInfo("Exiting Application.");
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
