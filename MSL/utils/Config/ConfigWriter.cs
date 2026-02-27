using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSL.utils.Config
{
    internal static class ConfigWriter
    {
        // 抽象写入任务：可以是"整体序列化 AppConfig"，也可以是"单键写入"
        private abstract class WriteJob { public abstract void Execute(); }

        // 整体序列化写入（AppConfig.Save 用）
        private sealed class FullSaveJob : WriteJob
        {
            private readonly AppConfig _cfg;
            private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
            public FullSaveJob(AppConfig cfg) => _cfg = cfg;
            public override void Execute()
            {
                string json = JsonConvert.SerializeObject(_cfg, _settings);
                File.WriteAllText(AppConfig.ConfigPath, json, Encoding.UTF8);
            }
        }

        // 单键写入（Config.Write 用）
        private sealed class KeyValueJob : WriteJob
        {
            private readonly string _path, _key;
            private readonly JToken _value;
            public KeyValueJob(string path, string key, JToken value)
            { _path = path; _key = key; _value = value; }
            public override void Execute()
            {
                JObject obj = JObject.Parse(File.ReadAllText(_path, Encoding.UTF8));
                obj[_key] = _value;
                File.WriteAllText(_path, obj.ToString(), Encoding.UTF8);
            }
        }

        // 单键删除（Config.Remove 用）
        private sealed class RemoveKeyJob : WriteJob
        {
            private readonly string _path, _key;
            public RemoveKeyJob(string path, string key) { _path = path; _key = key; }
            public override void Execute()
            {
                JObject obj = JObject.Parse(File.ReadAllText(_path, Encoding.UTF8));
                obj.Remove(_key);
                File.WriteAllText(_path, obj.ToString(), Encoding.UTF8);
            }
        }

        // 队列&调度
        private static readonly ConcurrentQueue<WriteJob> _queue = new ConcurrentQueue<WriteJob>();
        private static Task _task = Task.CompletedTask;
        private const int MaxRetries = 5;

        private static void Enqueue(WriteJob job)
        {
            _queue.Enqueue(job);
            // 若上一个 Task 已完成，启动新的消费循环
            if (_task.IsCompleted)
            {
                _task = Task.Run(DrainQueue);
            }
        }

        private static void DrainQueue()
        {
            while (_queue.TryDequeue(out WriteJob job))
            {
                ExecuteWithRetry(job);
            }
        }

        private static void ExecuteWithRetry(WriteJob job)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    job.Execute();
                    return;
                }
                catch (IOException ex) when (i < MaxRetries - 1)
                {
                    LogHelper.Write.Warn($"[ConfigWriter] 写入失败，第{i + 1}次重试: {ex.Message}");
                    Thread.Sleep(100 * (i + 1));
                }
                catch (IOException ex)
                {
                    LogHelper.Write.Error($"[ConfigWriter] 写入失败，已放弃: {ex.Message}");
                }
            }
        }

        // 对外入口
        internal static void EnqueueFullSave(AppConfig cfg) => Enqueue(new FullSaveJob(cfg));
        internal static void EnqueueKeyValue(string path, string key, JToken value) => Enqueue(new KeyValueJob(path, key, value));
        internal static void EnqueueRemoveKey(string path, string key) => Enqueue(new RemoveKeyJob(path, key));

        /// <summary>同步立即执行（仅初始化场景使用）</summary>
        internal static void ExecuteNow(AppConfig cfg) => ExecuteWithRetry(new FullSaveJob(cfg));
    }
}
