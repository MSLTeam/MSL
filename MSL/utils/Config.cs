using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MSL.utils
{
    internal class Config
    {
        private static readonly ConcurrentQueue<KeyValuePair<string, string>> _queue = new ConcurrentQueue<KeyValuePair<string, string>>();
        private static readonly string _configPath = "MSL\\config.json";//配置文件路径
        private static Task _writeTask = Task.CompletedTask;

        //读取配置
        public static string Read(string key)
        {
            JObject jobject = JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
            return (string)jobject[key] ?? "";
        }

        //写入配置
        public static void Write(string key, string value)
        {
            //感谢newbing写的队列
            _queue.Enqueue(new KeyValuePair<string, string>(key, value));

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

        //输出frpc配置文件
        public static bool WriteFrpcConfig(int ServerID,string Content,string Name)
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
                File.WriteAllText($"MSL\\frp\\{number}\\frpc", Content);
                JObject keyValues = new JObject()
                {
                    ["frpcServer"] = ServerID, //服务ID
                    ["name"]=Name,//备注
                };
                JObject jobject = JObject.Parse(File.ReadAllText(@"MSL\frp\config.json", Encoding.UTF8));
                jobject.Add(number.ToString(), keyValues);
                string convertString = Convert.ToString(jobject);
                File.WriteAllText(@"MSL\frp\config.json", convertString, Encoding.UTF8);
                return true;
            }
            catch (Exception ex) { 
                return false;
            }
        }
    }
}
