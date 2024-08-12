using Microsoft.Terminal.Wpf;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace ConPtyTermEmulatorLib
{
    /// <summary>
    /// Class for managing communication with the underlying console, and communicating with its pseudoconsole.
    /// </summary>
    public class Term : ITerminalConnection
    {

        private static bool IsDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
        private SafeFileHandle _consoleInputPipeWriteHandle;
        private StreamWriter _consoleInputWriter;
        private BinaryWriter _consoleInputWriterB;
        public Term(int READ_BUFFER_SIZE = 1024 * 16, bool USE_BINARY_WRITER = false)
        {
            this.READ_BUFFER_SIZE = READ_BUFFER_SIZE;
            this.USE_BINARY_WRITER = USE_BINARY_WRITER;
        }
        private bool USE_BINARY_WRITER;

        public bool CanOutLog { get; set; }
        public StringBuilder ConsoleOutputLog { get; private set; }
        private static Regex NewlineReduce = new(@"\n\s*?\n\s*?[\s]+", RegexOptions.Singleline);
        public static Regex colorStrip = new(@"((\x1B\[\??[0-9;]*[a-zA-Z])|\uFEFF|\u200B|\x1B\]0;|[\a\b])", RegexOptions.Compiled | RegexOptions.ExplicitCapture); //also strips BOM, bells, backspaces etc
        public string GetConsoleText(bool stripVTCodes = true) => NewlineReduce.Replace((stripVTCodes ? StripColors(ConsoleOutputLog.ToString()) : ConsoleOutputLog.ToString()).Replace("\r", ""), "\n\n").Trim();

        public static string StripColors(String str)
        {
            return colorStrip.Replace(str, "");
        }

        /// <summary>
        /// A stream of VT-100-enabled output from the console.
        /// </summary>
        public FileStream ConsoleOutStream { get; private set; }

        /// <summary>
        /// Fired once the console has been hooked up and is ready to receive input.
        /// </summary>
        public event EventHandler TermExited;
        public event EventHandler TermReady;
        public event EventHandler<TerminalOutputEventArgs> TerminalOutput;//how we send data to the UI terminal
        public bool TermProcIsRunning { get; private set; }


        /// <summary>
        /// Start the pseudoconsole and run the process as shown in 
        /// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
        /// </summary>
        /// <param name="command">the command to run, e.g. cmd.exe</param>
        /// <param name="consoleHeight">The height (in characters) to start the pseudoconsole with. Defaults to 80.</param>
        /// <param name="consoleWidth">The width (in characters) to start the pseudoconsole with. Defaults to 30.</param>
        public void Start(string command, string workingDirectory = null, bool logOutput = false)
        {
            if (Process != null)
            {
                if (Process.Process.HasExited != true)
                {
                    throw new Exception("Called Start on ConPTY term after already started");
                }
            }

            if (logOutput)
                ConsoleOutputLog = new();
            using (var inputPipe = new PseudoConsolePipe())
            using (var outputPipe = new PseudoConsolePipe())
            using (var pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide))
            using (var process = ProcessFactory.Start(command, PInvoke.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, pseudoConsole, workingDirectory))
            {
                Process = process;
                TheConsole = pseudoConsole;
                // copy all pseudoconsole output to a FileStream and expose it to the rest of the app
                ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
                TermProcIsRunning = true;

                TermReady?.Invoke(this, EventArgs.Empty);

                // Store input pipe handle, and a writer for later reuse
                _consoleInputPipeWriteHandle = inputPipe.WriteSide;
                var st = new FileStream(_consoleInputPipeWriteHandle, FileAccess.Write);
                if (!USE_BINARY_WRITER)
                    _consoleInputWriter = new StreamWriter(st) { AutoFlush = true };
                else
                    _consoleInputWriterB = new BinaryWriter(st);
                // free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
                ReadOutputLoop();
                OnClose(() => DisposeResources(process, pseudoConsole, outputPipe, inputPipe, _consoleInputWriter));

                process.Process.WaitForExit();
                //WriteToUITerminal("Session Terminated".AsSpan());
                TermProcIsRunning = false;
                TheConsole.Dispose();
                CloseStdinToApp();
                TermExited?.Invoke(this, EventArgs.Empty);
            }
        }
        public ProcessFactory.WrappedProcess Process;
        PseudoConsole TheConsole;
        /// <summary>
        /// Sends the given string to the anonymous pipe that writes to the active pseudoconsole.
        /// </summary>
        /// <param name="input">A string of characters to write to the console. Supports VT-100 codes.</param>
        public void WriteToTerm(ReadOnlySpan<char> input)
        {
            if (IsDesignMode)
                return;
            if (TheConsole.IsDisposed)
                return;
            if (_consoleInputWriter == null && _consoleInputWriterB == null)
                throw new InvalidOperationException("There is no writer attached to a pseudoconsole. Have you called Start on this instance yet?");
            //Debug.WriteLine($"Term.cs WriteToTerm writing: {input.ToString().Replace("\n", "\n\t").Trim()}");
            if (!USE_BINARY_WRITER)
                _consoleInputWriter.Write(input.ToString());

            else
                WriteToTermBinary(Encoding.UTF8.GetBytes(input.ToString()));
        }
        public void WriteToTermBinary(ReadOnlySpan<byte> input)
        {
            if (!USE_BINARY_WRITER)
            {
                WriteToTerm(Encoding.UTF8.GetString(input.ToArray()).AsSpan());
                return;
            }
            _consoleInputWriterB.Write(input.ToString());
            _consoleInputWriterB.Flush();
        }
        /// <summary>
        /// Close the input stream to the process (will send EOF if attempted to be read). 
        /// </summary>
        public void CloseStdinToApp()
        {
            _consoleInputWriter?.Close();
            _consoleInputWriter?.Dispose();
            _consoleInputWriterB?.Close();
            _consoleInputWriterB?.Dispose();
            _consoleInputWriter = null;
            _consoleInputWriterB = null;
        }
        public void StopTerm()
        {
            if (Process?.Process?.HasExited != false) return;
            Process.Process?.Kill();
            Process?.Dispose();
            TheConsole?.Dispose();
            ConsoleOutStream?.Dispose();
        }
        /// <summary>
        /// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
        /// Intended for resource cleanup logic.
        /// </summary>
        private static void OnClose(Action handler)
        {
            PInvoke.SetConsoleCtrlHandler(eventType =>
            {
                if (eventType == PInvoke.CTRL_CLOSE_EVENT)
                {
                    handler();
                }
                return false;
            }, true);
        }

        private void DisposeResources(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public void Start()
        {
            if (IsDesignMode)
            {
                WriteToUITerminal("MyShell DesignMode:> Your command window content here\r\n".AsSpan());
                return;
            }

            Task.Run(ReadOutputLoop);
        }

        /// <summary>
        /// Note if you change the span to a 0 length then no input will be passed on
        /// </summary>
        /// <param name="str"></param>
        public delegate void InterceptDelegate(ref Span<char> str);

        public InterceptDelegate InterceptOutputToUITerminal;
        public InterceptDelegate InterceptInputToTermApp;
        /// <summary>
        /// This simulates output from the program itself to the terminal window, ANSI sequences can be sent here as well
        /// </summary>
        /// <param name="str"></param>
        public void WriteToUITerminal(ReadOnlySpan<char> str)
        {
            var output = str.ToString();
            TerminalOutput?.Invoke(this, new TerminalOutputEventArgs(output));
        }

        public bool ReadLoopStarted = false;

        /// <summary>
        /// Sets if the GUI Terminal control communicates to ConPTY using extended key events (handles certain control sequences better)
        /// https://github.com/microsoft/terminal/blob/main/doc/specs/%234999%20-%20Improved%20keyboard%20handling%20in%20Conpty.md
        /// </summary>
        /// <param name="enable"></param>
        public void Win32DirectInputMode(bool enable)
        {
            var decSet = enable ? "h" : "l";
            var str = $"\x1b[?9001{decSet}";
            WriteToUITerminal(str.AsSpan());
        }
        int READ_BUFFER_SIZE;
        protected ref struct ReadState
        {
            public Span<char> entireBuffer;
            public Span<char> curBuffer;
            public int readChars;
        }

        public delegate void OutputHandler(string output);
        public event OutputHandler OnOutputReceived;
        protected virtual void ReadOutputLoop()
        {
            if (ReadLoopStarted)
                return;
            ReadLoopStarted = true;
            try
            {
                // We have a few ways to handle the buffer with a delimiter but given the size of the buffer and the fairly cheap cost of copying, the ability to let the span be modified before passing it on, we will just copy any parts before the next delimiter to the start of the buffer when reaching the end.
                using (StreamReader reader = new StreamReader(ConsoleOutStream))
                {
                    ReadState state = new() { entireBuffer = new char[READ_BUFFER_SIZE] };

                    state.curBuffer = state.entireBuffer.Slice(0);
                    var empty = Span<char>.Empty;

                    char[] tempBuffer = new char[state.curBuffer.Length];

                    while ((state.readChars = reader.Read(tempBuffer, 0, tempBuffer.Length)) != 0)
                    {
                        if (!CanOutLog)
                        {
                            CanOutLog = true;
                            Thread.Sleep(100);
                            continue;
                        }
                        tempBuffer.AsSpan(0, state.readChars).CopyTo(state.curBuffer);

                        var sendSpan = HandleRead(ref state);

                        if (sendSpan != empty)
                        {
                            InterceptOutputToUITerminal?.Invoke(ref sendSpan);
                            if (sendSpan.Length > 0)
                            {
                                var str = sendSpan.ToString();
                                WriteToUITerminal(str.AsSpan());
                                ConsoleOutputLog?.Append(str);
                                Console.WriteLine(str);
                                OnOutputReceived?.Invoke(str);
                            }
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }
        /// <summary>
        /// return the span (if any) to send to client, and the curBuffer
        /// </summary>
        /// <param name="curBuffer"></param>
        /// <returns></returns>
        protected virtual Span<char> HandleRead(ref ReadState state)
        {
            return state.curBuffer.Slice(0, state.readChars);
        }

        /// <summary>
        /// When set to true and input from the UI WPF Terminal Control will be ignored, will still invoke the intercept event.  You can still call WriteToTerm to write to the app yourself.
        /// </summary>
        /// <param name="readOnly">Enable / Disable readonly mode</param>
        /// <param name="updateCursor">Will hide/show the cursor depending on readonly setting</param>
        public void SetReadOnly(bool readOnly = true, bool updateCursor = true)
        {
            _ReadOnly = readOnly;
            if (updateCursor)
                SetCursorVisibility(!readOnly);
        }
        protected bool _ReadOnly;

        void ITerminalConnection.WriteInput(string data)
        {
            Span<char> span = data.ToCharArray();
            InterceptInputToTermApp?.Invoke(ref span);
            if (span.Length > 0 && !_ReadOnly)
                WriteToTerm(span);
        }

        void ITerminalConnection.Resize(uint row_height, uint column_width)
        {
            TheConsole?.Resize((int)column_width, (int)row_height);
        }
        public void Resize(int column_width, int row_height)
        {
            TheConsole?.Resize(column_width, row_height);
        }
        public void SetCursorVisibility(bool visible) => WriteToUITerminal(("\x1b[?25" + (visible ? 'h' : 'l')).AsSpan());
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullReset">Means near all parameters of the term are reset to defaults rather than just clearing the screen</param>
        public void ClearUITerminal(bool fullReset = false) => WriteToUITerminal((fullReset ? "\x001bc\x1b]104\x1b\\" : "\x1b[H\x1b[2J\u001b[3J").AsSpan());


        void ITerminalConnection.Close()
        {
            TheConsole?.Dispose();
        }
    }
}
