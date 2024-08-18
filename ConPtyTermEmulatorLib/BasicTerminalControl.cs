using Microsoft.Terminal.Wpf;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ConPtyTermEmulatorLib
{
    public class BasicTerminalControl : UserControl
    {
        public BasicTerminalControl()
        {
            InitializeComponent();
            SetKBCaptureOptions();
        }
        [Flags]
        [System.ComponentModel.TypeConverter(typeof(System.ComponentModel.EnumConverter))]
        public enum INPUT_CAPTURE { None = 1 << 0, TabKey = 1 << 1, DirectionKeys = 1 << 2 };


        private static void InputCaptureChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var cntrl = target as BasicTerminalControl;
            cntrl.SetKBCaptureOptions();
        }
        private void SetKBCaptureOptions()
        {
            KeyboardNavigation.SetTabNavigation(this, InputCapture.HasFlag(INPUT_CAPTURE.TabKey) ? KeyboardNavigationMode.Contained : KeyboardNavigationMode.Continue);
            KeyboardNavigation.SetDirectionalNavigation(this, InputCapture.HasFlag(INPUT_CAPTURE.DirectionKeys) ? KeyboardNavigationMode.Contained : KeyboardNavigationMode.Continue);
        }
        /// <summary>
        /// Helper property for setting KeyboardNavigation.Set*Navigation commands to prevent arrow keys or tabs from causing us to leave the control (aka pass through to conpty)
        /// </summary>
        public INPUT_CAPTURE InputCapture
        {
            get => (INPUT_CAPTURE)GetValue(InputCaptureProperty);
            set => SetValue(InputCaptureProperty, value);
        }

        [Description("Write only, sets the terminal theme"), Category("Common")]
        public TerminalTheme? Theme { set => SetTheme(_Theme = value); private get => _Theme; }
        private TerminalTheme? _Theme;
        private void SetTheme(TerminalTheme? v) { if (v != null) Terminal?.SetTheme(v.Value, FontFamilyWhenSettingTheme.Source, (short)FontSizeWhenSettingTheme); }



        [Description("Write only, When true user cannot give input through the Terminal UI (can still write to the Term from code behind using Term.WriteToTerm)"), Category("Common")]
        public bool? IsReadOnly { set => SetReadOnly(_IsReadOnly = value); private get => _IsReadOnly; }
        private bool? _IsReadOnly;
        private void SetReadOnly(bool? v) { if (v != null) ConPTYTerm?.SetReadOnly(v.Value, false); }//no cursor auto update if user wants that they can use the separate dependency property for the cursor visibility

        [Description("Write only, if the type cursor shows on the Terminal UI"), Category("Common")]
        public bool? IsCursorVisible { set => SetCursor(_IsCursorVisible = value); private get => _IsCursorVisible; }
        private bool? _IsCursorVisible;
        private void SetCursor(bool? v) { if (v != null) ConPTYTerm?.SetCursorVisibility(v.Value); }


        public TerminalControl Terminal
        {
            get => (TerminalControl)GetValue(TerminalPropertyKey.DependencyProperty);
            set => SetValue(TerminalPropertyKey, value);
        }

        private static void OnTermChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var cntrl = (target as BasicTerminalControl);
            var newTerm = e.NewValue as Term;
            if (newTerm != null)
            {
                if (cntrl.Terminal.IsLoaded)
                    cntrl.Terminal_Loaded(cntrl.Terminal, null);

                if (newTerm.TermProcIsRunning)
                    cntrl.Term_TermReady(newTerm, null);
                else
                    newTerm.TermReady += cntrl.Term_TermReady;
            }
        }
        /// <summary>
        /// Update the Term if you want to set to an existing
        /// </summary>
        public Term ConPTYTerm
        {
            get => (Term)GetValue(ConPTYTermProperty);
            set => SetValue(ConPTYTermProperty, value);
        }


        public Term DisconnectConPTYTerm()
        {
            if (Terminal != null)
                Terminal.Connection = null;
            if (ConPTYTerm != null)
                ConPTYTerm.TermReady -= Term_TermReady;
            var ret = ConPTYTerm;
            ConPTYTerm = null;
            return ret;
        }

        public string StartupCommandLine
        {
            get => (string)GetValue(StartupCommandLineProperty);
            set => SetValue(StartupCommandLineProperty, value);
        }

        public string WorkingDirectory
        {
            get => (string)GetValue(WorkingDirectoryProperty);
            set => SetValue(WorkingDirectoryProperty, value);
        }

        public bool LogConPTYOutput
        {
            get => (bool)GetValue(LogConPTYOutputProperty);
            set => SetValue(LogConPTYOutputProperty, value);
        }
        /// <summary>
        /// Sets if the GUI Terminal control communicates to ConPTY using extended key events (handles certain control sequences better)
        /// https://github.com/microsoft/terminal/blob/main/doc/specs/%234999%20-%20Improved%20keyboard%20handling%20in%20Conpty.md
        /// </summary>
        public bool Win32InputMode
        {
            get => (bool)GetValue(Win32InputModeProperty);
            set => SetValue(Win32InputModeProperty, value);
        }

        public FontFamily FontFamilyWhenSettingTheme
        {
            get => (FontFamily)GetValue(FontFamilyWhenSettingThemeProperty);
            set => SetValue(FontFamilyWhenSettingThemeProperty, value);
        }

        public int FontSizeWhenSettingTheme
        {
            get => (int)GetValue(FontSizeWhenSettingThemeProperty);
            set => SetValue(FontSizeWhenSettingThemeProperty, value);
        }
        private void InitializeComponent()
        {
            Terminal = new();
            ConPTYTerm = new();
            Focusable = true;
            Terminal.Focusable = true;
            Terminal.AutoResize = true;
            Terminal.Loaded += Terminal_Loaded;
            var grid = new Grid() { };
            grid.Children.Add(Terminal);
            this.Content = grid;
            this.GotFocus += (_, _) => Terminal.Focus();
        }

        private void Term_TermReady(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                Terminal.Connection = ConPTYTerm;
                ConPTYTerm.Win32DirectInputMode(Win32InputMode);
                ConPTYTerm.Resize(Terminal.Columns, Terminal.Rows);//fix the size being partially off on first load
            });
        }

        public void StartTerm()
        {
            if (ConPTYTerm == null)
                return;
            ConPTYTerm.TermReady += Term_TermReady;
            this.Dispatcher.Invoke(() =>
            {
                var cmd = StartupCommandLine;//thread safety for dp
                var dir = WorkingDirectory;
                var term = ConPTYTerm;
                var logOutput = LogConPTYOutput;
                Task.Run(() => term.Start(cmd, dir, logOutput));
            });
        }

        public void ResetTerm()
        {
            if (ConPTYTerm?.TermProcIsRunning == true)
            {
                ConPTYTerm.ClearUITerminal();
                ConPTYTerm.StopTerm();
            }
            DisconnectConPTYTerm();
            ConPTYTerm = new();
            StartTerm();
        }

        private async void Terminal_Loaded(object sender, RoutedEventArgs e)
        {
            SetTheme(Theme);
            SetCursor(IsCursorVisible);
            SetReadOnly(IsReadOnly);
            Terminal.Focus();
            await Task.Delay(1000);
            SetCursor(IsCursorVisible);
        }

        #region Depdendency Properties
        public static readonly DependencyProperty InputCaptureProperty = DependencyProperty.Register(nameof(InputCapture), typeof(INPUT_CAPTURE), typeof(BasicTerminalControl), new
        PropertyMetadata(INPUT_CAPTURE.TabKey | INPUT_CAPTURE.DirectionKeys, InputCaptureChanged));

        public static readonly DependencyProperty ThemeProperty = PropHelper.GenerateWriteOnlyProperty((c) => c.Theme);

        protected static readonly DependencyPropertyKey TerminalPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Terminal), typeof(TerminalControl), typeof(BasicTerminalControl), new PropertyMetadata());

        public static readonly DependencyProperty TerminalProperty = TerminalPropertyKey.DependencyProperty;
        public static readonly DependencyProperty ConPTYTermProperty = DependencyProperty.Register(nameof(ConPTYTerm), typeof(Term), typeof(BasicTerminalControl), new(OnTermChanged));
        public static readonly DependencyProperty StartupCommandLineProperty = DependencyProperty.Register(nameof(StartupCommandLine), typeof(string), typeof(BasicTerminalControl), new PropertyMetadata("powershell.exe"));
        public static readonly DependencyProperty WorkingDirectoryProperty = DependencyProperty.Register(nameof(WorkingDirectory), typeof(string), typeof(BasicTerminalControl), new PropertyMetadata(null));

        public static readonly DependencyProperty LogConPTYOutputProperty = DependencyProperty.Register(nameof(LogConPTYOutput), typeof(bool), typeof(BasicTerminalControl), new PropertyMetadata(false));
        public static readonly DependencyProperty Win32InputModeProperty = DependencyProperty.Register(nameof(Win32InputMode), typeof(bool), typeof(BasicTerminalControl), new PropertyMetadata(true));
        public static readonly DependencyProperty IsReadOnlyProperty = PropHelper.GenerateWriteOnlyProperty((c) => c.IsReadOnly);
        public static readonly DependencyProperty IsCursorVisibleProperty = PropHelper.GenerateWriteOnlyProperty((c) => c.IsCursorVisible);

        public static readonly DependencyProperty FontFamilyWhenSettingThemeProperty = DependencyProperty.Register(nameof(FontFamilyWhenSettingTheme), typeof(FontFamily), typeof(BasicTerminalControl), new PropertyMetadata(new FontFamily("Cascadia Code")));

        public static readonly DependencyProperty FontSizeWhenSettingThemeProperty = DependencyProperty.Register(nameof(FontSizeWhenSettingTheme), typeof(int), typeof(BasicTerminalControl), new PropertyMetadata(12));

        private class PropHelper : DepPropHelper<BasicTerminalControl> { }
        private class DepPropHelper<CONTROL_TYPE> where CONTROL_TYPE : UserControl
        {
            protected DepPropHelper() => throw new Exception("Should not be instanced");
            public static DependencyProperty GenerateWriteOnlyProperty<PROP_TYPE>(Expression<Func<CONTROL_TYPE, PROP_TYPE>> PropToSet)
            {

                var me = PropToSet.Body as MemberExpression;
                if (me == null)
                    throw new ArgumentException(nameof(PropToSet));
                var propName = me.Member.Name;
                var prop = typeof(CONTROL_TYPE).GetProperty(me.Member.Name, BindingFlags.Instance | BindingFlags.Public);

                if (prop == null)
                    throw new ArgumentException(nameof(PropToSet));

                return DependencyProperty.Register(propName, typeof(PROP_TYPE), typeof(CONTROL_TYPE), new FrameworkPropertyMetadata(null, (target, value) => CoerceReadOnlyHandle(prop.SetMethod, target, value)));
            }
            private static object CoerceReadOnlyHandle(MethodInfo SetMethod, DependencyObject target, object value)
            {
                SetMethod.Invoke(target, new object[] { value });
                return null;
            }
        }

        #endregion
    }
}
