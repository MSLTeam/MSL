using ConPtyTermEmulatorLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSL.utils
{
    /// <summary>
    /// 用 ConPTY 启动 Minecraft 服务器
    /// 输入输出全部自己管理，不再依赖 WPF TerminalControl。
    /// </summary>
    public class MinecraftServerTerm : IDisposable
    {
        private PseudoConsolePipe _inputPipe;
        private PseudoConsolePipe _outputPipe;
        private PseudoConsole _pseudoConsole;
        public ProcessFactory.WrappedProcess _process;
        private IntPtr _processHandle = IntPtr.Zero;
        private StreamWriter _inputWriter;
        private FileStream _outputStream;

        private int _exitCode = 0;
        public int ExitCode => _exitCode;

        // 历史指令
        private readonly List<string> _history = new();
        private int _historyIndex = -1;

        public event Action<string> OnOutput;   // 原始输出（含ANSI码）
        public event Action OnProcessExited;

        public bool IsRunning => _process?.Process?.HasExited == false;

        public void Start(string javaPath, string jarArgs, string workingDir)
        {
            // 构造完整命令行
            // 关键：不加 --nogui 的话某些服务端会开Swing窗口
            // 关键：nogui 让服务器走控制台模式，jline 在 ConPTY 下会识别为 TTY
            string command = $"\"{javaPath}\" {jarArgs}";

            _inputPipe = new PseudoConsolePipe();
            _outputPipe = new PseudoConsolePipe();
            _pseudoConsole = PseudoConsole.Create(_inputPipe.ReadSide, _outputPipe.WriteSide);

            // 调整 ConPTY 尺寸，宽度影响 jline 的换行和补全列表排版
            _pseudoConsole.Resize(220, 50);

            _process = ProcessFactory.Start(
                command,
                (nuint)0x00020016,
                _pseudoConsole,
                workingDir
            );

            _processHandle = _process.Process.Handle;

            // 输入：写入 ConPTY 的 input pipe
            var inputFileStream = new FileStream(_inputPipe.WriteSide, FileAccess.Write);
            _inputWriter = new StreamWriter(inputFileStream, new UTF8Encoding(false))
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            // 输出：从 ConPTY 的 output pipe 读取
            _outputStream = new FileStream(_outputPipe.ReadSide, FileAccess.Read);

            Task.Run(ReadLoop);
            Task.Run(WaitForExit);
        }
        private bool _capturingCompletion = false;
        private readonly StringBuilder _completionBuffer = new();
        private TaskCompletionSource<List<string>> _completionTcs;
        private Timer _completionTimer;


        /// <summary>
        /// 发送Tab并等待补全结果，超时返回空列表
        /// </summary>
        public async Task<List<string>> RequestCompletionAsync(string currentInput, int timeoutMs = 400)
        {
            if (!IsRunning) return new List<string>();
            if (string.IsNullOrWhiteSpace(currentInput)) return new List<string>(); // 拒绝空输入

            _completionBuffer.Clear();
            _completionTcs = new TaskCompletionSource<List<string>>();
            _capturingCompletion = true;

            _inputWriter.Write("\x15");
            // 先把用户当前输入同步到ConPTY当前行（不带回车）
            if (!string.IsNullOrEmpty(currentInput))
                _inputWriter.Write(currentInput);

            // 发送Tab触发补全
            _inputWriter.Write("\t");

            // 超时后强制结束捕获
            _completionTimer = new Timer(_ =>
            {
                FlushCompletion();
            }, null, timeoutMs, Timeout.Infinite);

            var result = await _completionTcs.Task;

            // 捕获完毕后，发送 Ctrl+U 清除ConPTY当前行
            _inputWriter.Write("\x15");

            return result;
        }

        private void FlushCompletion()
        {
            _capturingCompletion = false;
            _completionTimer?.Dispose();

            var raw = _completionBuffer.ToString();
            var results = ParseCompletionOutput(raw);
            _completionTcs?.TrySetResult(results);
        }

        private List<string> ParseCompletionOutput(string raw)
        {
            // 去掉ANSI转义码
            var stripped = StripAnsi(raw);

            // 按空白符切分，过滤掉提示符行（含 '>' 或 '[' 的行）
            var candidates = stripped
                .Split(new[] { '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith("/") || (!s.Contains('[') && !s.Contains('>')))
                .Distinct()
                .ToList();

            return candidates;
        }

        private static string StripAnsi(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                @"\x1B[\[\(][?!>]?[0-9;]*[A-Za-z]|\x1B\][^\x07]*\x07|\x1B[A-Za-z]|\x1B.",
                ""
            );
        }

        // ReadLoop 里改造：捕获模式下分流
        private void ReadLoop()
        {
            try
            {
                var buffer = new byte[4096];
                int read;
                while ((read = _outputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string text;
                    try { text = Encoding.UTF8.GetString(buffer, 0, read); }
                    catch { text = Encoding.Default.GetString(buffer, 0, read); }

                    if (_capturingCompletion)
                    {
                        _completionBuffer.Append(text);
                        // 重置超时计时器（数据还在来）
                        _completionTimer?.Change(400, Timeout.Infinite);
                    }
                    else
                    {
                        OnOutput?.Invoke(text);
                    }
                }
            }
            catch { }
        }


        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
        private void WaitForExit()
        {
            try
            {
                _process?.Process?.WaitForExit();

                // 进程退出后用保存的句柄读退出码
                GetExitCodeProcess(_processHandle, out uint code);
                _exitCode = (int)code;
            }
            finally
            {
                OnProcessExited?.Invoke();
            }
        }

        /// <summary>
        /// 发送普通指令（回车结尾）
        /// </summary>
        public void SendCommand(string cmd)
        {
            if (!IsRunning) return;

            // 加入历史
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                // 避免重复
                if (_history.Count == 0 || _history[_history.Count - 1] != cmd)
                    _history.Add(cmd);
                _historyIndex = _history.Count; // 重置到末尾
            }

            _inputWriter.WriteLine(cmd);
        }

        /// <summary>
        /// 发送原始字节（用于 Tab 键等控制字符）
        /// </summary>
        public void SendRaw(string raw)
        {
            if (!IsRunning) return;
            _inputWriter.Write(raw);
        }

        /// <summary>
        /// 发送 Tab 键触发补全
        /// </summary>
        public void SendTab() => SendRaw("\t");

        /// <summary>
        /// 获取上一条历史指令，返回 null 表示已到顶
        /// </summary>
        public string GetHistoryUp()
        {
            if (_history.Count == 0) return null;
            _historyIndex = Math.Max(0, _historyIndex - 1);
            return _history[_historyIndex];
        }

        /// <summary>
        /// 获取下一条历史指令，返回 "" 表示已到底（清空输入框）
        /// </summary>
        public string GetHistoryDown()
        {
            if (_history.Count == 0) return null;
            _historyIndex = Math.Min(_history.Count, _historyIndex + 1);
            return _historyIndex >= _history.Count ? "" : _history[_historyIndex];
        }

        /// <summary>
        /// 调整终端尺寸（在窗口大小改变时调用）
        /// </summary>
        public void Resize(int width, int height)
        {
            _pseudoConsole?.Resize(width, height);
        }

        public void Stop()
        {
            try { _inputWriter?.WriteLine("stop"); } catch { }
        }

        public void Kill()
        {
            try { _process?.Process?.Kill(); } catch { }
        }

        public void Dispose()
        {
            _inputWriter?.Dispose();
            _outputStream?.Dispose();
            _process?.Dispose();
            _pseudoConsole?.Dispose();
            _outputPipe?.Dispose();
            _inputPipe?.Dispose();
        }
    }
}
