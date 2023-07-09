using System.IO;
using System;
using System.Windows;
using MSL.controls;
using System.Diagnostics;

namespace MSL
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 添加崩溃处理事件
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show("程序在运行的时候发生了异常，异常代码：\n" + e.Exception.Message + "\n若软件闪退，请联系作者进行反馈", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // 设置为已处理，阻止应用程序崩溃
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (!Directory.Exists("MSL"))
            {
                Directory.CreateDirectory("MSL");
                Logger.LogWarning("未检测到MSL文件夹，已进行创建");
            }

            Logger.Clear();
            Logger.LogInfo("MSL，启动！");
        }
    }
    public class Logger
    {
        public static void Clear()
        {
            if (File.Exists("MSL\\log.txt"))
            {
                File.WriteAllText("MSL\\log.txt",string.Empty);
            }
        }
        public static void LogInfo(string message)
        {
            LogMessage("INFO", message);
        }

        public static void LogWarning(string message)
        {
            LogMessage("WARNING", message);
        }

        public static void LogError(string message)
        {
            LogMessage("ERROR", message);
        }
        
        private static void LogMessage(string level, string message)
        {
            string logEntry = $"{DateTime.Now} [{level}] {message}";

            // 写入日志文件
            using (StreamWriter writer = File.AppendText("MSL\\log.txt"))
            {
                writer.WriteLine(logEntry);
            }
        }
    }
}
