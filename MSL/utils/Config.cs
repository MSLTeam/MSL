using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MSL.utils
{
    public class ConfigStore
    {
        public static string ServerLink { get; set; }
        public static Version MSLVersion { get; set; }

        // public static bool IsOldVersion { get; set; } 旧版本会加一个叹号提示（现在暂没使用此功能），后续可能会用上此字段
        public static string DeviceID { get; set; }
        public static bool GetServerInfo { get; set; } = false;
        public static bool GetPlayerInfo { get; set; } = false;

        public class LogColor
        {
            public static SolidColorBrush INFO { get; set; } = Brushes.Green;
            public static SolidColorBrush WARN { get; set; } = Brushes.Orange;
            public static SolidColorBrush ERROR { get; set; } = Brushes.Red;
            public static SolidColorBrush HIGHLIGHT { get; set; } = Brushes.Blue;
        }
    }

    internal class Config
    {
        private static readonly ConcurrentQueue<KeyValuePair<string, JToken>> _queue = new ConcurrentQueue<KeyValuePair<string, JToken>>();
        private static readonly string _configPath = "MSL\\config.json";//配置文件路径
        private static Task _writeTask = Task.CompletedTask;

        // 读取配置
        public static object Read(string key)
        {
            JObject jobject = JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
            return jobject[key] ?? null;
        }

        // 写入配置
        public static void Write(string key, JToken value)
        {
            //感谢newbing写的队列
            _queue.Enqueue(new KeyValuePair<string, JToken>(key, value));

            if (_writeTask.IsCompleted)
            {
                _writeTask = Task.Run(() =>
                {
                    while (_queue.TryDequeue(out var kv))
                    {
                        JObject jobject = JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
                        jobject[kv.Key] = kv.Value;
                        string convertString = Convert.ToString(jobject);
                        File.WriteAllText(_configPath, convertString, Encoding.UTF8);
                    }
                });
            }
        }

        // 删除配置
        public static void Remove(string key)
        {
            JObject jobject = JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
            jobject.Remove(key);
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(_configPath, convertString, Encoding.UTF8);
        }

        /// <summary>
        /// 写Frp信息存储文件和配置文件
        /// </summary>
        /// <param name="ServerID">Frp服务ID</param>
        /// /// <param name="Name">MSL显示的Frpc隧道名字</param>
        /// <param name="Content">FRPC配置文件内容</param>
        /// <param name="Suffix">保存到FRPC文件的后缀名，默认为.toml</param>
        /// <returns>bool</returns>
        public static bool WriteFrpcConfig(int ServerID, string Name, string Content, string Suffix = ".toml")
        {
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                int number = Functions.Frpc_GenerateRandomInt();
                if (!File.Exists(@"MSL\frp\config.json"))
                {
                    File.WriteAllText(@"MSL\frp\config.json", string.Format("{{{0}}}", "\n"));
                }
                Directory.CreateDirectory("MSL\\frp\\" + number);
                File.WriteAllText($"MSL\\frp\\{number}\\frpc" + Suffix, Content);
                JObject keyValues = new JObject()
                {
                    ["frpcServer"] = ServerID,
                    ["name"] = Name,
                };
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                jobject.Add(number.ToString(), keyValues);
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                return true;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }
    }
}
