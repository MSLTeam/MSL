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
        public delegate void DeleControl();

        public App()
        {
            // 1. 订阅 UI 线程的未处理异常事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 2. 订阅非 UI 线程的未处理异常事件
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        }

        // --- UI 线程异常处理 ---
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 阻止程序崩溃
            e.Handled = true;

            var exception = e.Exception;
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";

            // 记录本地日志
            LogHelper.Write.Fatal($"捕获到UI线程异常: {fullTrace}");

            // 准备提示信息
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"程序在运行的时候发生了异常。");
            messageBuilder.AppendLine(exception.Message?? "未知错误");
            messageBuilder.AppendLine($"请检查是否安装了.NET Framework 4.7.2运行库。");
            messageBuilder.AppendLine($"若确定运行环境正常，请将错误提交给开发者处理哦！");

            // 向用户显示所有信息
            MessageBox.Show(messageBuilder.ToString(), "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        // --- 非 UI 线程异常处理 ---
        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            string fullTrace = exception?.ToString() ?? "没有可用的堆栈跟踪信息。";

            // 写入本地日志
            LogHelper.Write.Fatal($"捕获到致命的非UI线程异常，程序即将退出: {fullTrace}");

            MessageBox.Show(
                "程序遇到了一个无法恢复的致命错误，即将关闭。" + (exception.Message ?? "未知错误" )+ "\n\n" +
                "请查看本地日志文件获取详细信息。建议提交给开发者哦！",
                "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                LogHelper.Write.Info("MSL，启动！");
                
                if (jsonObject["Lang"] == null)
                {
                    jsonObject.Add("Lang", "zh-CN");
                    string convertString = Convert.ToString(jsonObject);
                    File.WriteAllText(@"MSL\config.json", convertString, Encoding.UTF8);
                    LogHelper.Write.Info("语言: " + "zh-CN");
                }
                else
                {
                    if (jsonObject["Lang"].ToString() != "zh-CN")
                        LanguageManager.Instance.ChangeLanguage(new CultureInfo(jsonObject["Lang"].ToString()));
                    LogHelper.Write.Info("语言: " + jsonObject["Lang"].ToString().ToUpper());
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
            LogHelper.Write.Info("程序正在退出...");
            _mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
