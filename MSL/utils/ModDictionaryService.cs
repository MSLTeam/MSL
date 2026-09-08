using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSL.utils
{
    public class ModDictionaryService
    {
        private static readonly Lazy<ModDictionaryService> _instance = new Lazy<ModDictionaryService>(() => new ModDictionaryService());
        public static ModDictionaryService Instance => _instance.Value;

        private readonly Dictionary<string, string> _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _reverseDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _cleanReverseDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _isLoaded = false;
        private readonly object _lock = new object();

        private ModDictionaryService()
        {
            Task.Run(() => EnsureLoaded());
        }

        public void EnsureLoaded()
        {
            if (_isLoaded) return;
            lock (_lock)
            {
                if (_isLoaded) return;
                LoadDictionary();
                _isLoaded = true;
            }
        }

        private void LoadDictionary()
        {
            try
            {
                Stream stream = null;
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("MSLX_ModDictionary.json", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(resourceName))
                {
                    stream = assembly.GetManifestResourceStream(resourceName);
                }

                if (stream == null)
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "MSLX_ModDictionary.json");
                    if (File.Exists(path))
                    {
                        stream = File.OpenRead(path);
                    }
                }

                if (stream != null)
                {
                    using (stream)
                    using (var reader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        var serializer = new JsonSerializer();
                        var rawDict = serializer.Deserialize<Dictionary<string, string>>(jsonReader);
                        if (rawDict != null)
                        {
                            foreach (var kvp in rawDict)
                            {
                                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                                    continue;

                                _dict[kvp.Key] = kvp.Value;

                                if (!_reverseDict.ContainsKey(kvp.Value))
                                {
                                    _reverseDict[kvp.Value] = kvp.Key;
                                }

                                string cleanCn = Regex.Replace(kvp.Value, @"^\[.*?\]\s*", "").Trim();
                                if (!string.IsNullOrEmpty(cleanCn) && !_cleanReverseDict.ContainsKey(cleanCn))
                                {
                                    _cleanReverseDict[cleanCn] = kvp.Key;
                                }
                            }
                        }
                    }
                    LogHelper.Write.Info($"[ModDictionaryService] 成功加载 {_dict.Count} 条模组对照词条。");
                }
                else
                {
                    LogHelper.Write.Warn("[ModDictionaryService] 未找到 MSLX_ModDictionary.json 资源。");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error("[ModDictionaryService] 词典加载失败：" + ex.Message);
            }
        }

        public string GetChineseName(string slugOrEnglishName)
        {
            if (string.IsNullOrWhiteSpace(slugOrEnglishName)) return null;
            EnsureLoaded();

            var slug = slugOrEnglishName.ToLower().Replace(" ", "-").Replace("'", "");
            if (_dict.TryGetValue(slug, out var chineseName))
            {
                return chineseName;
            }
            if (_dict.TryGetValue(slugOrEnglishName, out chineseName))
            {
                return chineseName;
            }
            return null;
        }

        public string TranslateChineseQueryToEnglish(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return query;
            if (!Regex.IsMatch(query, @"[\u4e00-\u9fa5]"))
            {
                return query;
            }

            EnsureLoaded();
            string cleanQuery = query.Trim();

            // 中文名精准匹配 (去掉 [xxx] 前缀的名称)
            if (_cleanReverseDict.TryGetValue(cleanQuery, out var exactCleanSlug))
            {
                return exactCleanSlug.Replace("-", " ");
            }

            // 原中文名精准匹配
            if (_reverseDict.TryGetValue(cleanQuery, out var exactSlug))
            {
                return exactSlug.Replace("-", " ");
            }

            // 纯中文名最短包含匹配
            var bestCleanMatch = _cleanReverseDict.Keys
                .Where(k => k.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(k => k.Length)
                .FirstOrDefault();

            if (bestCleanMatch != null)
            {
                return _cleanReverseDict[bestCleanMatch].Replace("-", " ");
            }

            // 4. 原中文名最短包含匹配
            var bestMatch = _reverseDict.Keys
                .Where(k => k.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(k => k.Length)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                return _reverseDict[bestMatch].Replace("-", " ");
            }

            return query;
        }
    }
}
