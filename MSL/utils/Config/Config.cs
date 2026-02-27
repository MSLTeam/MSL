using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

namespace MSL.utils.Config
{
    internal class Config
    {
        private static readonly string _configPath = AppConfig.ConfigPath;

        /// <summary>读取单个配置项，不存在返回 null。</summary>
        public static JToken Read(string key)
        {
            JObject obj = JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
            return obj[key];
        }

        /// <summary>异步写入单个配置项（加入共享队列）。</summary>
        public static void Write(string key, JToken value)
            => ConfigWriter.EnqueueKeyValue(_configPath, key, value);

        /// <summary>异步删除单个配置项（加入共享队列）。</summary>
        public static void Remove(string key)
            => ConfigWriter.EnqueueRemoveKey(_configPath, key);

        /// <summary>写 Frpc 配置（与队列无关，直接同步写）</summary>
        public static bool WriteFrpcConfig(int serverID, string name, string content, string suffix = ".toml")
        {
            try
            {
                Directory.CreateDirectory("MSL\\frp");
                int number = Functions.Frpc_GenerateRandomInt();
                string frpConfigPath = @"MSL\frp\config.json";
                if (!File.Exists(frpConfigPath))
                    File.WriteAllText(frpConfigPath, "{\n}");

                Directory.CreateDirectory($"MSL\\frp\\{number}");
                File.WriteAllText($"MSL\\frp\\{number}\\frpc{suffix}", content);

                JObject entry = new JObject
                {
                    ["frpcServer"] = serverID,
                    ["name"] = name,
                };
                JObject root = JObject.Parse(File.ReadAllText(frpConfigPath, Encoding.UTF8));
                root.Add(number.ToString(), entry);
                File.WriteAllText(frpConfigPath, root.ToString(), Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
