using HandyControl.Controls;
using MSL.controls.dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Window = System.Windows.Window;

namespace MSL.utils
{
    // ServerCoreInstaller.cs
    // 将服务端核心的下载、依赖处理、安装逻辑统一封装
    // 供 DownloadServerWindow 和 FastModeInstallControl 直接调用

    /// <summary>
    /// 服务端核心下载与安装的封装结果
    /// </summary>
    public class ServerInstallResult
    {
        /// <summary> 是否成功 </summary>
        public bool Success { get; set; }

        /// <summary> 最终的服务端核心文件名（安装后可能与原始文件名不同，如 forge 安装后） </summary>
        public string FinalFileName { get; set; }

        /// <summary> 失败原因（Success=false 时有效） </summary>
        public string ErrorMessage { get; set; }
    }

    public class ServerCoreInstaller
    {
        private readonly Window _owner;
        private readonly string _savingPath;
        private readonly string _javaPath;
        private readonly bool _useMirror;

        public ServerCoreInstaller(Window owner, string savingPath, string javaPath, bool useMirror)
        {
            _owner = owner;
            _savingPath = savingPath;
            _javaPath = javaPath;
            _useMirror = useMirror;
        }

        // ────
        // 公共入口：下载 + 处理依赖 + 安装，返回最终文件名

        /// <summary>
        /// 下载指定服务端核心，处理依赖并（如有必要）执行安装器。
        /// </summary>
        /// <param name="serverType">服务端类型，如 "forge"、"fabric"、"paper" 等</param>
        /// <param name="version">MC 版本号</param>
        /// <param name="build">构建号（"latest" 或具体构建）</param>
        public async Task<ServerInstallResult> DownloadAndInstallAsync(string serverType, string version, string build = "latest")
        {
            // 获取主文件下载信息
            string buildQuery = build.Contains("latest") ? "latest" : build;
            JObject dlContext = await HttpService.GetApiContentAsync(
                $"download/server/{serverType}/{version}?build={buildQuery}");

            string dlUrl = MirrorCheck(dlContext["data"]["url"].ToString(), serverType);
            string sha256Exp = dlContext["data"]["sha256"]?.ToString() ?? string.Empty;
            string filename = $"{serverType}-{version}.jar";

            bool enableParallel = !IsSequentialDownload(serverType);

            // 创建下载组，加入主文件
            var dwnManager = DownloadManager.Instance;
            string groupId = dwnManager.CreateDownloadGroup(isTempGroup: true);
            dwnManager.AddDownloadItem(groupId, dlUrl, _savingPath, filename, sha256Exp,
                                       enableParallel: enableParallel);

            // 按服务端类型追加额外依赖项并执行安装
            return await HandleServerTypeAsync(serverType, version, filename, groupId, dwnManager, enableParallel);
        }

        // ────
        // 私有：各服务端类型的分支处理

        private async Task<ServerInstallResult> HandleServerTypeAsync(
            string serverType,
            string version,
            string filename,
            string groupId,
            DownloadManager dwnManager,
            bool enableParallel)
        {
            string installReturn;

            switch (serverType)
            {
                // ── SpongeForge ──
                case "spongeforge":
                    // 还需下载对应的 Forge 安装器
                    string forgeName = "forge";
                    string forgeFilename = $"{forgeName}-{version}.jar";

                    JObject forgeDlContext = await HttpService.GetApiContentAsync(
                        $"download/server/{forgeName}/{version}");

                    string forgeDlUrl = MirrorCheck(forgeDlContext["data"]["url"].ToString(), serverType);
                    string forgeSha256 = forgeDlContext["data"]["sha256"]?.ToString() ?? string.Empty;

                    dwnManager.AddDownloadItem(groupId, forgeDlUrl, _savingPath, forgeFilename,
                                               forgeSha256, enableParallel: enableParallel);

                    if (!await StartDownloadAndWaitAsync(dwnManager, groupId))
                        return Fail("下载失败！");

                    // Sponge 本体作为 Mod 加载，移入 mods 文件夹
                    var moveResult = TryMoveSpongeToMods(filename);
                    if (!moveResult.Success)
                        return moveResult;

                    // 安装 Forge
                    installReturn = await InstallForgeAsync(forgeFilename);
                    if (installReturn == null)
                        return Fail("安装失败！");

                    return Ok(installReturn);

                // ── NeoForge / Forge ──
                case "neoforge":
                case "forge":
                    if (!await StartDownloadAndWaitAsync(dwnManager, groupId))
                        return Fail("下载失败！");

                    installReturn = await InstallForgeAsync(filename);
                    if (installReturn == null)
                        return Fail("安装失败！");

                    return Ok(installReturn);

                // ── Fabric ──
                case "fabric":
                    // 依赖：原版服务端放到 .fabric/server 目录
                    await AddVanillaToGroupAsync(
                        $"{_savingPath}\\.fabric\\server",
                        $"{version}-server.jar",
                        version, dwnManager, groupId, enableParallel);

                    if (!await StartDownloadAndWaitAsync(dwnManager, groupId))
                        return Fail("下载失败！");

                    return Ok(filename);

                // ── Paper / Leaves / Folia / Purpur / Leaf ──
                case "paper":
                case "leaves":
                case "folia":
                case "purpur":
                case "leaf":
                    // 依赖：原版服务端放到 cache 目录
                    await AddVanillaToGroupAsync(
                        $"{_savingPath}\\cache",
                        $"mojang_{version}.jar",
                        version, dwnManager, groupId, enableParallel);

                    if (!await StartDownloadAndWaitAsync(dwnManager, groupId))
                        return Fail("下载失败！");

                    return Ok(filename);

                // ── 其他（Vanilla、Spigot 等无需特殊处理的） ──
                default:
                    if (!await StartDownloadAndWaitAsync(dwnManager, groupId))
                        return Fail("下载失败！");

                    return Ok(filename);
            }
        }

        // ────
        // 私有：向下载组追加原版服务端
        private async Task AddVanillaToGroupAsync(
            string savePath,
            string filename,
            string version,
            DownloadManager dwnManager,
            string groupId,
            bool enableParallel)
        {
            JObject downContext = await HttpService.GetApiContentAsync(
                $"download/server/vanilla/{version}");

            string downUrl = MirrorCheck(downContext["data"]["url"].ToString(), "vanilla");
            string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;

            dwnManager.AddDownloadItem(groupId, downUrl, savePath, filename, sha256Exp,
                                       enableParallel: enableParallel);
        }

        // ────
        // 私有：启动下载组并等待完成（显示下载管理器对话框）
        private async Task<bool> StartDownloadAndWaitAsync(DownloadManager dwnManager, string groupId)
        {
            var token = Guid.NewGuid().ToString();
            Dialog.SetToken(_owner, token);
            DownloadManagerDialog.Instance.LoadDialog(token, false);
            Dialog.Show(DownloadManagerDialog.Instance, token);

            dwnManager.StartDownloadGroup(groupId);
            DownloadManagerDialog.Instance.ManagerControl.AddDownloadGroup(groupId, true, true, true);

            bool success = await dwnManager.WaitForGroupCompletionAsync(groupId);
            Dialog.Close(token);

            if (!success)
                MagicShow.ShowMsgDialog(_owner, "下载失败！", "提示");

            return success;
        }

        // ────
        // 私有：调用 Forge/NeoForge 安装器
        private async Task<string> InstallForgeAsync(string filename)
        {
            string[] installForge = await MagicShow.ShowInstallForge(_owner, _savingPath, filename, _javaPath);

            switch (installForge[0])
            {
                case "0": // 自动安装失败，询问是否用命令行安装
                    if (await MagicShow.ShowMsgDialogAsync(_owner,
                            "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
                        return Functions.InstallForge(_javaPath, _savingPath, filename, string.Empty, false);
                    return null;

                case "1": // 有 mirror，优先用 mirror 安装，失败则回退命令行
                    string ret = Functions.InstallForge(_javaPath, _savingPath, filename, installForge[1]);
                    return ret ?? Functions.InstallForge(_javaPath, _savingPath, filename, string.Empty, false);

                case "3": // 直接命令行安装
                    return Functions.InstallForge(_javaPath, _savingPath, filename, string.Empty, false);

                default: // 用户取消
                    return null;
            }
        }

        // ────
        // 私有：将 Sponge 核心移动到 mods 目录
        private ServerInstallResult TryMoveSpongeToMods(string filename)
        {
            try
            {
                string modsDir = Path.Combine(_savingPath, "mods");
                string modsDest = Path.Combine(modsDir, filename);
                string src = Path.Combine(_savingPath, filename);

                Directory.CreateDirectory(modsDir);
                if (File.Exists(modsDest))
                    File.Delete(modsDest);
                File.Move(src, modsDest);

                return new ServerInstallResult { Success = true };
            }
            catch (Exception e)
            {
                MagicShow.ShowMsgDialog(_owner, $"Sponge核心移动失败！\n请重试！{e.Message}", "错误");
                return Fail($"Sponge核心移动失败：{e.Message}");
            }
        }

        // ────
        // 私有：判断是否需要顺序（非并行）下载
        /// <summary>
        /// Vanilla、Forge、NeoForge 的安装器不支持并行分片下载。
        /// </summary>
        private static bool IsSequentialDownload(string serverType) =>
            serverType is "vanilla" or "forge" or "neoforge";

        // ────
        // 辅助构造结果
        private static ServerInstallResult Ok(string finalFileName) =>
            new() { Success = true, FinalFileName = finalFileName };

        private static ServerInstallResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };

        // ────
        // 镜像检测：替换为官方源，或构造 Forge 官方备用 URL
        // serverType 参数用于判断是否需要做 Forge URL 替换
        private string MirrorCheck(string url, string serverType = "")
        {
            if (_useMirror)
                return url;

            // 原版文件：镜像地址 → Mojang 官方
            url = url.Replace(
                "file.mslmc.cn/mirrors/vanilla/",
                "piston-data.mojang.com/v1/objects/");

            // Forge：从镜像 URL 的 query 参数重建 Maven 官方地址
            if (serverType is "forge" or "spongeforge")
                url = BuildForgeMavenUrl(url);

            return url;
        }

        // ────
        // 从带 query 参数的镜像 URL 构建 Forge 官方 Maven 地址
        // 原散落在 MriiorCheck、FastModeInstallCore(forge/spongeforge 两处) 的重复逻辑统一至此
        private static string BuildForgeMavenUrl(string mirrorUrl)
        {
            var query = new Uri(mirrorUrl).Query;
            var queryDict = System.Web.HttpUtility.ParseQueryString(query);
            string mcVersion = queryDict["mcversion"];
            string forgeVersion = queryDict["version"];

            // 1.10 以下的版本号格式为 forgeVersion-mcVersion
            string[] parts = mcVersion.Split('.');
            string mcMajor = mcVersion;
            if (parts.Length >= 3 && int.TryParse(parts[2], out _))
                mcMajor = $"{parts[0]}.{parts[1]}";

            if (new Version(mcMajor) < new Version("1.10"))
                forgeVersion += "-" + mcVersion;

            return $"https://maven.minecraftforge.net/net/minecraftforge/forge/" +
                   $"{mcVersion}-{forgeVersion}/" +
                   $"forge-{mcVersion}-{forgeVersion}-installer.jar";
        }
    }
}
