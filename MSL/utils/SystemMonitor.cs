using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace MSL.utils
{
    /// <summary>
    /// 全局单例系统资源监控器。
    /// 只维护一个采集线程，多个订阅者共享系统级数据（CPU / 内存），
    /// 进程级内存由各订阅者自行通过 GetProcessMemoryGB 计算。
    /// </summary>
    public sealed class SystemMonitor
    {
        public class SystemInfoData
        {
            public float CpuUsage;
            public float RamAvailableMB;
            public double TotalMemoryGB;
        }

        private static readonly Lazy<SystemMonitor> _instance =
            new Lazy<SystemMonitor>(() => new SystemMonitor());
        public static SystemMonitor Instance => _instance.Value;

        private readonly ConcurrentDictionary<object, Action<SystemInfoData>> _subscribers = new();

        private volatile bool _running;
        private Thread _thread;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private float _physicalMemory;

        private SystemMonitor() { }

        /// <summary>
        /// 订阅系统资源更新。key 用于标识订阅者，同一 key 重复调用会替换旧回调。
        /// 第一个订阅者加入时自动启动采集线程。
        /// </summary>
        public void Subscribe(object key, Action<SystemInfoData> callback)
        {
            _subscribers[key] = callback;
            EnsureStarted();
        }

        /// <summary>
        /// 取消订阅。最后一个订阅者离开时自动停止采集线程。
        /// </summary>
        public void Unsubscribe(object key)
        {
            _subscribers.TryRemove(key, out _);
            if (_subscribers.IsEmpty) Stop();
        }

        /// <summary>当前是否有活跃的采集线程</summary>
        public bool IsRunning => _running;

        /// <summary>当前订阅者数量</summary>
        public int SubscriberCount => _subscribers.Count;

        /// <summary>强制停止采集并清空所有订阅者</summary>
        public void Dispose()
        {
            Stop();
            _subscribers.Clear();
        }

        private void EnsureStarted()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(MonitoringLoop) { IsBackground = true };
            _thread.Start();
        }

        private void Stop()
        {
            _running = false;
        }

        private void MonitoringLoop()
        {
            try
            {
                if (PerformanceCounterCategory.Exists("Processor Information")
                    && PerformanceCounterCategory.CounterExists("% Processor Utility", "Processor Information"))
                {
                    _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                }
                else
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                _physicalMemory = Functions.GetPhysicalMemoryGB();
            }
            catch
            {
                _running = false;
                return;
            }

            while (_running)
            {
                try
                {
                    var data = new SystemInfoData
                    {
                        CpuUsage = _cpuCounter.NextValue(),
                        RamAvailableMB = _ramCounter.NextValue(),
                        TotalMemoryGB = _physicalMemory
                    };

                    foreach (var kv in _subscribers)
                    {
                        try { kv.Value.Invoke(data); }
                        catch { /* 单个订阅者异常不影响其他 */ }
                    }
                }
                catch { /* 忽略采集异常，继续循环 */ }
                finally
                {
                    Thread.Sleep(3000);
                }
            }

            // 清理 PerformanceCounter
            try { _cpuCounter?.Dispose(); } catch { }
            try { _ramCounter?.Dispose(); } catch { }
            _cpuCounter = null;
            _ramCounter = null;
        }
    }
}
