﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace HandyControl.Tools.Interop;

public class InteropValues
{
    public static class ExternDll
    {
        public const string
            User32 = "user32.dll",
            Gdi32 = "gdi32.dll",
            GdiPlus = "gdiplus.dll",
            Kernel32 = "kernel32.dll",
            Shell32 = "shell32.dll",
            MsImg = "msimg32.dll",
            NTdll = "ntdll.dll",
            WinInet = "wininet.dll",
            UxTheme = "uxtheme.dll",
            DwmApi = "dwmapi.dll";
    }

    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public MARGINS(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        /// <summary>Width of left border that retains its size.</summary>
        public int Left;

        /// <summary>Width of right border that retains its size.</summary>
        public int Right;

        /// <summary>Height of top border that retains its size.</summary>
        public int Top;

        /// <summary>Height of bottom border that retains its size.</summary>
        public int Bottom;
    }
    public const int
        BITSPIXEL = 12,
        PLANES = 14,
        BI_RGB = 0,
        DIB_RGB_COLORS = 0,
        E_FAIL = unchecked((int) 0x80004005),
        HWND_TOP = 0,
        GWL_STYLE = -16,
        NIF_MESSAGE = 0x00000001,
        NIF_ICON = 0x00000002,
        NIF_TIP = 0x00000004,
        NIF_INFO = 0x00000010,
        NIM_ADD = 0x00000000,
        NIM_MODIFY = 0x00000001,
        NIM_DELETE = 0x00000002,
        NIIF_NONE = 0x00000000,
        NIIF_INFO = 0x00000001,
        NIIF_WARNING = 0x00000002,
        NIIF_ERROR = 0x00000003,
        WM_ACTIVATE = 0x0006,
        WM_QUIT = 0x0012,
        WM_GETMINMAXINFO = 0x0024,
        WM_WINDOWPOSCHANGING = 0x0046,
        WM_WINDOWPOSCHANGED = 0x0047,
        WM_CREATE = 0x0001,
        WM_SETICON = 0x0080,
        WM_NCCREATE = 0x0081,
        WM_NCDESTROY = 0x0082,
        WM_NCHITTEST = 0x0084,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCACTIVATE = 0x0086,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCUAHDRAWCAPTION = 0x00AE,
        WM_NCUAHDRAWFRAME = 0x00AF,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105,
        WM_SYSCOMMAND = 0x112,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONUP = 0x0205,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        WM_CLIPBOARDUPDATE = 0x031D,
        WM_USER = 0x0400,
        WS_VISIBLE = 0x10000000,
        MF_BYCOMMAND = 0x00000000,
        MF_BYPOSITION = 0x400,
        MF_GRAYED = 0x00000001,
        MF_SEPARATOR = 0x800,
        TB_GETBUTTON = WM_USER + 23,
        TB_BUTTONCOUNT = WM_USER + 24,
        TB_GETITEMRECT = WM_USER + 29,
        HTCAPTION = 0x02,
        VERTRES = 10,
        DESKTOPVERTRES = 117,
        LOGPIXELSX = 88,
        LOGPIXELSY = 90,
        SC_CLOSE = 0xF060,
        SC_SIZE = 0xF000,
        SC_MOVE = 0xF010,
        SC_MINIMIZE = 0xF020,
        SC_MAXIMIZE = 0xF030,
        SC_RESTORE = 0xF120,
        SRCCOPY = 0x00CC0020,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002,
        WM_COPYDATA = 0x004A;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class NOTIFYICONDATA
    {
        public int cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip = string.Empty;
        public int dwState = 0x01;
        public int dwStateMask = 0x01;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo = string.Empty;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle = string.Empty;
        public int dwInfoFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TBBUTTON
    {
        public int iBitmap;
        public int idCommand;
        public IntPtr fsStateStylePadding;
        public IntPtr dwData;
        public IntPtr iString;
    }

    [Flags]
    public enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    public enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRAYDATA
    {
        public IntPtr hwnd;
        public uint uID;
        public uint uCallbackMessage;
        public uint bReserved0;
        public uint bReserved1;
        public IntPtr hIcon;
    }

    [Flags]
    public enum FreeType
    {
        Decommit = 0x4000,
        Release = 0x8000,
    }

    public enum ShowWindowCommands : int
    {
        Hide = 0,
        Normal = 1,
        ShowMinimized = 2,
        Maximize = 3, // is this the right value?
        ShowMaximized = 3,
        ShowNoActivate = 4,
        Show = 5,
        Minimize = 6,
        ShowMinNoActive = 7,
        ShowNA = 8,
        Restore = 9,
        ShowDefault = 10,
        ForceMinimize = 11
    }

    public enum MouseEvent : int
    {
        LEFT_DOWN = 0x02,
        LEFT_UP = 0x04,
        RIGHT_DOWN = 0x08,
        RIGHT_UP = 0x10,
    }

    [Flags()]
    public enum SetWindowPosFlags : uint
    {
        SynchronousWindowPosition = 0x4000,
        DeferErase = 0x2000,
        DrawFrame = 0x0020,
        FrameChanged = 0x0020,
        HideWindow = 0x0080,
        DoNotActivate = 0x0010,
        DoNotCopyBits = 0x0100,
        IgnoreMove = 0x0002,
        DoNotChangeOwnerZOrder = 0x0200,
        DoNotRedraw = 0x0008,
        DoNotReposition = 0x0200,
        DoNotSendChangingEvent = 0x0400,
        IgnoreResize = 0x0001,
        IgnoreZOrder = 0x0004,
        ShowWindow = 0x0040,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    public enum HookType
    {
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEHOOKSTRUCT
    {
        public POINT pt;
        public IntPtr hwnd;
        public uint wHitTestCode;
        public IntPtr dwExtraInfo;
    }

    [Flags]
    public enum ProcessAccess
    {
        AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
        CreateThread = 0x2,
        DuplicateHandle = 0x40,
        QueryInformation = 0x400,
        SetInformation = 0x200,
        Terminate = 0x1,
        VMOperation = 0x8,
        VMRead = 0x10,
        VMWrite = 0x20,
        Synchronize = 0x100000
    }

    [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public RECT(Rect rect)
        {
            Left = (int) rect.Left;
            Top = (int) rect.Top;
            Right = (int) rect.Right;
            Bottom = (int) rect.Bottom;
        }

        public Point Position => new(Left, Top);
        public Size Size => new(Width, Height);

        public int Height
        {
            get => Bottom - Top;
            set => Bottom = Top + value;
        }

        public int Width
        {
            get => Right - Left;
            set => Right = Left + value;
        }

        public bool Equals(RECT other)
        {
            return Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;
        }

        public override bool Equals(object obj)
        {
            return obj is RECT rectangle && Equals(rectangle);
        }

        public static bool operator ==(RECT left, RECT right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RECT left, RECT right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left;
                hashCode = (hashCode * 397) ^ Top;
                hashCode = (hashCode * 397) ^ Right;
                hashCode = (hashCode * 397) ^ Bottom;
                return hashCode;
            }
        }
    }

    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    public enum GWL
    {
        STYLE = -16,
        EXSTYLE = -20
    }

    public enum GWLP
    {
        WNDPROC = -4,
        HINSTANCE = -6,
        HWNDPARENT = -8,
        USERDATA = -21,
        ID = -12
    }

    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [Flags]
    public enum RedrawWindowFlags : uint
    {
        Invalidate = 1u,
        InternalPaint = 2u,
        Erase = 4u,
        Validate = 8u,
        NoInternalPaint = 16u,
        NoErase = 32u,
        NoChildren = 64u,
        AllChildren = 128u,
        UpdateNow = 256u,
        EraseNow = 512u,
        Frame = 1024u,
        NoFrame = 2048u
    }

    [StructLayout(LayoutKind.Sequential)]
    public class WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public SW showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;

        /// <summary>
        /// Gets the default (empty) value.
        /// </summary>
        public static WINDOWPLACEMENT Default
        {
            get
            {
                WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                result.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                return result;
            }
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT2
    {
        public int Length;
        public int Flags;
        public ShowWindowCommands ShowCmd;
        public POINT MinPosition;
        public POINT MaxPosition;
        public RECT NormalPosition;
        public static WINDOWPLACEMENT2 Default
        {
            get
            {
                WINDOWPLACEMENT2 result = new WINDOWPLACEMENT2();
                result.Length = Marshal.SizeOf(result);
                return result;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SIZE
    {
        [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
        public int cx;
        [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]
        public int cy;
    }

    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public enum SM
    {
        CXSCREEN = 0,
        CYSCREEN = 1,
        CXVSCROLL = 2,
        CYHSCROLL = 3,
        CYCAPTION = 4,
        CXBORDER = 5,
        CYBORDER = 6,
        CXFIXEDFRAME = 7,
        CYFIXEDFRAME = 8,
        CYVTHUMB = 9,
        CXHTHUMB = 10,
        CXICON = 11,
        CYICON = 12,
        CXCURSOR = 13,
        CYCURSOR = 14,
        CYMENU = 15,
        CXFULLSCREEN = 16,
        CYFULLSCREEN = 17,
        CYKANJIWINDOW = 18,
        MOUSEPRESENT = 19,
        CYVSCROLL = 20,
        CXHSCROLL = 21,
        DEBUG = 22,
        SWAPBUTTON = 23,
        CXMIN = 28,
        CYMIN = 29,
        CXSIZE = 30,
        CYSIZE = 31,
        CXFRAME = 32,
        CXSIZEFRAME = CXFRAME,
        CYFRAME = 33,
        CYSIZEFRAME = CYFRAME,
        CXMINTRACK = 34,
        CYMINTRACK = 35,
        CXDOUBLECLK = 36,
        CYDOUBLECLK = 37,
        CXICONSPACING = 38,
        CYICONSPACING = 39,
        MENUDROPALIGNMENT = 40,
        PENWINDOWS = 41,
        DBCSENABLED = 42,
        CMOUSEBUTTONS = 43,
        SECURE = 44,
        CXEDGE = 45,
        CYEDGE = 46,
        CXMINSPACING = 47,
        CYMINSPACING = 48,
        CXSMICON = 49,
        CYSMICON = 50,
        CYSMCAPTION = 51,
        CXSMSIZE = 52,
        CYSMSIZE = 53,
        CXMENUSIZE = 54,
        CYMENUSIZE = 55,
        ARRANGE = 56,
        CXMINIMIZED = 57,
        CYMINIMIZED = 58,
        CXMAXTRACK = 59,
        CYMAXTRACK = 60,
        CXMAXIMIZED = 61,
        CYMAXIMIZED = 62,
        NETWORK = 63,
        CLEANBOOT = 67,
        CXDRAG = 68,
        CYDRAG = 69,
        SHOWSOUNDS = 70,
        CXMENUCHECK = 71,
        CYMENUCHECK = 72,
        SLOWMACHINE = 73,
        MIDEASTENABLED = 74,
        MOUSEWHEELPRESENT = 75,
        XVIRTUALSCREEN = 76,
        YVIRTUALSCREEN = 77,
        CXVIRTUALSCREEN = 78,
        CYVIRTUALSCREEN = 79,
        CMONITORS = 80,
        SAMEDISPLAYFORMAT = 81,
        IMMENABLED = 82,
        CXFOCUSBORDER = 83,
        CYFOCUSBORDER = 84,
        TABLETPC = 86,
        MEDIACENTER = 87,
        REMOTESESSION = 0x1000,
        REMOTECONTROL = 0x2001
    }

    public enum CacheSlot
    {
        DpiX,

        FocusBorderWidth,
        FocusBorderHeight,
        HighContrast,
        MouseVanish,

        DropShadow,
        FlatMenu,
        WorkAreaInternal,
        WorkArea,

        IconMetrics,

        KeyboardCues,
        KeyboardDelay,
        KeyboardPreference,
        KeyboardSpeed,
        SnapToDefaultButton,
        WheelScrollLines,
        MouseHoverTime,
        MouseHoverHeight,
        MouseHoverWidth,

        MenuDropAlignment,
        MenuFade,
        MenuShowDelay,

        ComboBoxAnimation,
        ClientAreaAnimation,
        CursorShadow,
        GradientCaptions,
        HotTracking,
        ListBoxSmoothScrolling,
        MenuAnimation,
        SelectionFade,
        StylusHotTracking,
        ToolTipAnimation,
        ToolTipFade,
        UIEffects,

        MinimizeAnimation,
        Border,
        CaretWidth,
        ForegroundFlashCount,
        DragFullWindows,
        NonClientMetrics,

        ThinHorizontalBorderHeight,
        ThinVerticalBorderWidth,
        CursorWidth,
        CursorHeight,
        ThickHorizontalBorderHeight,
        ThickVerticalBorderWidth,
        MinimumHorizontalDragDistance,
        MinimumVerticalDragDistance,
        FixedFrameHorizontalBorderHeight,
        FixedFrameVerticalBorderWidth,
        FocusHorizontalBorderHeight,
        FocusVerticalBorderWidth,
        FullPrimaryScreenWidth,
        FullPrimaryScreenHeight,
        HorizontalScrollBarButtonWidth,
        HorizontalScrollBarHeight,
        HorizontalScrollBarThumbWidth,
        IconWidth,
        IconHeight,
        IconGridWidth,
        IconGridHeight,
        MaximizedPrimaryScreenWidth,
        MaximizedPrimaryScreenHeight,
        MaximumWindowTrackWidth,
        MaximumWindowTrackHeight,
        MenuCheckmarkWidth,
        MenuCheckmarkHeight,
        MenuButtonWidth,
        MenuButtonHeight,
        MinimumWindowWidth,
        MinimumWindowHeight,
        MinimizedWindowWidth,
        MinimizedWindowHeight,
        MinimizedGridWidth,
        MinimizedGridHeight,
        MinimumWindowTrackWidth,
        MinimumWindowTrackHeight,
        PrimaryScreenWidth,
        PrimaryScreenHeight,
        WindowCaptionButtonWidth,
        WindowCaptionButtonHeight,
        ResizeFrameHorizontalBorderHeight,
        ResizeFrameVerticalBorderWidth,
        SmallIconWidth,
        SmallIconHeight,
        SmallWindowCaptionButtonWidth,
        SmallWindowCaptionButtonHeight,
        VirtualScreenWidth,
        VirtualScreenHeight,
        VerticalScrollBarWidth,
        VerticalScrollBarButtonHeight,
        WindowCaptionHeight,
        KanjiWindowHeight,
        MenuBarHeight,
        VerticalScrollBarThumbHeight,
        IsImmEnabled,
        IsMediaCenter,
        IsMenuDropRightAligned,
        IsMiddleEastEnabled,
        IsMousePresent,
        IsMouseWheelPresent,
        IsPenWindows,
        IsRemotelyControlled,
        IsRemoteSession,
        ShowSounds,
        IsSlowMachine,
        SwapButtons,
        IsTabletPC,
        VirtualScreenLeft,
        VirtualScreenTop,

        PowerLineStatus,

        IsGlassEnabled,
        UxThemeName,
        UxThemeColor,
        WindowCornerRadius,
        WindowGlassColor,
        WindowGlassBrush,
        WindowNonClientFrameThickness,
        WindowResizeBorderThickness,

        NumSlots
    }

    public static class Win32Constant
    {
        public const int MAX_PATH = 260;
        public const int INFOTIPSIZE = 1024;
        public const int TRUE = 1;
        public const int FALSE = 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASS
    {
        public uint style;
        public Delegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class WNDCLASS4ICON
    {
        public int style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct BITMAPINFO
    {
        public int biSize;

        public int biWidth;

        public int biHeight;

        public short biPlanes;

        public short biBitCount;

        public int biCompression;

        public int biSizeImage;

        public int biXPelsPerMeter;

        public int biYPelsPerMeter;

        public int biClrUsed;

        public int biClrImportant;

        public BITMAPINFO(int width, int height, short bpp)
        {
            biSize = SizeOf();
            biWidth = width;
            biHeight = height;
            biPlanes = 1;
            biBitCount = bpp;
            biCompression = 0;
            biSizeImage = 0;
            biXPelsPerMeter = 0;
            biYPelsPerMeter = 0;
            biClrUsed = 0;
            biClrImportant = 0;
        }

        [SecuritySafeCritical]
        private static int SizeOf()
        {
            return Marshal.SizeOf(typeof(BITMAPINFO));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ICONINFO
    {
        public bool fIcon = false;
        public int xHotspot = 0;
        public int yHotspot = 0;
        public BitmapHandle hbmMask = null;
        public BitmapHandle hbmColor = null;
    }

    public enum WINDOWCOMPOSITIONATTRIB
    {
        WCA_ACCENT_POLICY = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINCOMPATTRDATA
    {
        public WINDOWCOMPOSITIONATTRIB Attribute;
        public IntPtr Data;
        public int DataSize;
    }

    public enum ACCENTSTATE
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ACCENTPOLICY
    {
        public ACCENTSTATE AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [ComImport, Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IStream
    {
        int Read([In] IntPtr buf, [In] int len);

        int Write([In] IntPtr buf, [In] int len);

        [return: MarshalAs(UnmanagedType.I8)]
        long Seek([In, MarshalAs(UnmanagedType.I8)] long dlibMove, [In] int dwOrigin);

        void SetSize([In, MarshalAs(UnmanagedType.I8)] long libNewSize);

        [return: MarshalAs(UnmanagedType.I8)]
        long CopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In, MarshalAs(UnmanagedType.I8)] long cb, [Out, MarshalAs(UnmanagedType.LPArray)] long[] pcbRead);

        void Commit([In] int grfCommitFlags);

        void Revert();

        void LockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

        void UnlockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

        void Stat([In] IntPtr pStatstg, [In] int grfStatFlag);

        [return: MarshalAs(UnmanagedType.Interface)]
        IStream Clone();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class StreamConsts
    {
        public const int LOCK_WRITE = 0x1;
        public const int LOCK_EXCLUSIVE = 0x2;
        public const int LOCK_ONLYONCE = 0x4;
        public const int STATFLAG_DEFAULT = 0x0;
        public const int STATFLAG_NONAME = 0x1;
        public const int STATFLAG_NOOPEN = 0x2;
        public const int STGC_DEFAULT = 0x0;
        public const int STGC_OVERWRITE = 0x1;
        public const int STGC_ONLYIFCURRENT = 0x2;
        public const int STGC_DANGEROUSLYCOMMITMERELYTODISKCACHE = 0x4;
        public const int STREAM_SEEK_SET = 0x0;
        public const int STREAM_SEEK_CUR = 0x1;
        public const int STREAM_SEEK_END = 0x2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public class ImageCodecInfoPrivate
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Guid Clsid;
        [MarshalAs(UnmanagedType.Struct)]
        public Guid FormatID;

        public IntPtr CodecName = IntPtr.Zero;
        public IntPtr DllName = IntPtr.Zero;
        public IntPtr FormatDescription = IntPtr.Zero;
        public IntPtr FilenameExtension = IntPtr.Zero;
        public IntPtr MimeType = IntPtr.Zero;

        public int Flags;
        public int Version;
        public int SigCount;
        public int SigSize;

        public IntPtr SigPattern = IntPtr.Zero;
        public IntPtr SigMask = IntPtr.Zero;
    }

    public class ComStreamFromDataStream : IStream
    {
        protected Stream DataStream;

        // to support seeking ahead of the stream length...
        private long _virtualPosition = -1;

        public ComStreamFromDataStream(Stream dataStream)
        {
            this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
        }

        private void ActualizeVirtualPosition()
        {
            if (_virtualPosition == -1) return;

            if (_virtualPosition > DataStream.Length)
                DataStream.SetLength(_virtualPosition);

            DataStream.Position = _virtualPosition;

            _virtualPosition = -1;
        }

        public virtual IStream Clone()
        {
            NotImplemented();
            return null;
        }

        public virtual void Commit(int grfCommitFlags)
        {
            DataStream.Flush();
            ActualizeVirtualPosition();
        }

        public virtual long CopyTo(IStream pstm, long cb, long[] pcbRead)
        {
            const int bufsize = 4096; // one page
            var buffer = Marshal.AllocHGlobal(bufsize);
            if (buffer == IntPtr.Zero) throw new OutOfMemoryException();
            long written = 0;

            try
            {
                while (written < cb)
                {
                    var toRead = bufsize;
                    if (written + toRead > cb) toRead = (int) (cb - written);
                    var read = Read(buffer, toRead);
                    if (read == 0) break;
                    if (pstm.Write(buffer, read) != read)
                    {
                        throw EFail("Wrote an incorrect number of bytes");
                    }
                    written += read;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            if (pcbRead != null && pcbRead.Length > 0)
            {
                pcbRead[0] = written;
            }

            return written;
        }

        public virtual Stream GetDataStream() => DataStream;

        public virtual void LockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        protected static ExternalException EFail(string msg) => throw new ExternalException(msg, E_FAIL);

        protected static void NotImplemented() => throw new NotImplementedException();

        public virtual int Read(IntPtr buf, int length)
        {
            var buffer = new byte[length];
            var count = Read(buffer, length);
            Marshal.Copy(buffer, 0, buf, length);
            return count;
        }

        public virtual int Read(byte[] buffer, int length)
        {
            ActualizeVirtualPosition();
            return DataStream.Read(buffer, 0, length);
        }

        public virtual void Revert() => NotImplemented();

        public virtual long Seek(long offset, int origin)
        {
            var pos = _virtualPosition;
            if (_virtualPosition == -1)
            {
                pos = DataStream.Position;
            }
            var len = DataStream.Length;

            switch (origin)
            {
                case StreamConsts.STREAM_SEEK_SET:
                    if (offset <= len)
                    {
                        DataStream.Position = offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = offset;
                    }
                    break;
                case StreamConsts.STREAM_SEEK_END:
                    if (offset <= 0)
                    {
                        DataStream.Position = len + offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = len + offset;
                    }
                    break;
                case StreamConsts.STREAM_SEEK_CUR:
                    if (offset + pos <= len)
                    {
                        DataStream.Position = pos + offset;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = offset + pos;
                    }
                    break;
            }

            return _virtualPosition != -1 ? _virtualPosition : DataStream.Position;
        }

        public virtual void SetSize(long value) => DataStream.SetLength(value);

        public virtual void Stat(IntPtr pstatstg, int grfStatFlag) => NotImplemented();

        public virtual void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        public virtual int Write(IntPtr buf, int length)
        {
            var buffer = new byte[length];
            Marshal.Copy(buf, buffer, 0, length);
            return Write(buffer, length);
        }

        public virtual int Write(byte[] buffer, int length)
        {
            ActualizeVirtualPosition();
            DataStream.Write(buffer, 0, length);
            return length;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RTL_OSVERSIONINFOEX
    {
#if NET5_0_OR_GREATER
        public RTL_OSVERSIONINFOEX(uint dwMajorVersion, uint dwMinorVersion, uint dwBuildNumber, uint dwRevision, uint dwPlatformId, string szCSDVersion) : this()
        {
            this.dwMajorVersion = dwMajorVersion;
            this.dwMinorVersion = dwMinorVersion;
            this.dwBuildNumber = dwBuildNumber;
            this.dwRevision = dwRevision;
            this.dwPlatformId = dwPlatformId;
            this.szCSDVersion = szCSDVersion;
        }
        public readonly uint dwOSVersionInfoSize { get; init; } = (uint) Marshal.SizeOf<RTL_OSVERSIONINFOEX>();
#else
        public uint dwOSVersionInfoSize;
#endif
        public uint dwMajorVersion;
        public uint dwMinorVersion;
        public uint dwBuildNumber;
        public uint dwRevision;
        public uint dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
    }

    [Flags]
    public enum WindowPositionFlags
    {
        /// <summary>
        ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
        ///     the request to the thread that owns the window. This prevents the calling thread from blocking its execution while
        ///     other threads process the request.
        /// </summary>
        SWP_ASYNCWINDOWPOS = 0x4000,

        /// <summary>
        ///     Prevents generation of the WM_SYNCPAINT message.
        /// </summary>
        SWP_DEFERERASE = 0x2000,

        /// <summary>
        ///     Draws a frame (defined in the window's class description) around the window.
        /// </summary>
        SWP_DRAWFRAME = 0x0020,

        /// <summary>
        ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if
        ///     the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's
        ///     size is being changed.
        /// </summary>
        SWP_FRAMECHANGED = 0x0020,

        /// <summary>
        ///     Hides the window.
        /// </summary>
        SWP_HIDEWINDOW = 0x0080,

        /// <summary>
        ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the
        ///     topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOACTIVATE = 0x0010,

        /// <summary>
        ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client
        ///     area are saved and copied back into the client area after the window is sized or repositioned.
        /// </summary>
        SWP_NOCOPYBITS = 0x0100,

        /// <summary>
        ///     Retains the current position (ignores X and Y parameters).
        /// </summary>
        SWP_NOMOVE = 0x0002,

        /// <summary>
        ///     Does not change the owner window's position in the Z order.
        /// </summary>
        SWP_NOOWNERZORDER = 0x0200,

        /// <summary>
        ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area,
        ///     the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a
        ///     result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any
        ///     parts of the window and parent window that need redrawing.
        /// </summary>
        SWP_NOREDRAW = 0x0008,

        /// <summary>
        ///     Same as the SWP_NOOWNERZORDER flag.
        /// </summary>
        SWP_NOREPOSITION = 0x0200,

        /// <summary>
        ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// </summary>
        SWP_NOSENDCHANGING = 0x0400,

        /// <summary>
        ///     Retains the current size (ignores the cx and cy parameters).
        /// </summary>
        SWP_NOSIZE = 0x0001,

        /// <summary>
        ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOZORDER = 0x0004,

        /// <summary>
        ///     Displays the window.
        /// </summary>
        SWP_SHOWWINDOW = 0x0040
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPosition
    {
        public IntPtr Hwnd;
        public IntPtr HwndZOrderInsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public WindowPositionFlags Flags;
    }

    [Flags]
    public enum DwmWindowAttribute : uint
    {
        DWMWA_NCRENDERING_ENABLED = 1,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_LAST
    }

    [Flags]
    public enum WindowStyles
    {
        /// <summary>
        ///     The window is initially maximized.
        /// </summary>
        WS_MAXIMIZE = 0x01000000,

        /// <summary>
        ///     The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must
        ///     also be specified.
        /// </summary>
        WS_MAXIMIZEBOX = 0x00010000,

        /// <summary>
        ///     The window is initially minimized. Same as the WS_ICONIC style.
        /// </summary>
        WS_MINIMIZE = 0x20000000,

        /// <summary>
        ///     The window has a sizing border. Same as the WS_SIZEBOX style.
        /// </summary>
        WS_THICKFRAME = 0x00040000,
    }

    /// <summary>
    /// ShowWindow options
    /// </summary>
    public enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        NORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        MAXIMIZE = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
        FORCEMINIMIZE = 11,
    }
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }

    #region Mica
    /// <summary>
    /// Collection of backdrop types.
    /// </summary>
    [Flags]
    public enum DWMSBT : uint
    {
        /// <summary>
        /// Automatically selects backdrop effect.
        /// </summary>
        DWMSBT_AUTO = 0,

        /// <summary>
        /// Turns off the backdrop effect.
        /// </summary>
        DWMSBT_DISABLE = 1,

        /// <summary>
        /// Sets Mica effect with generated wallpaper tint.
        /// </summary>
        DWMSBT_MAINWINDOW = 2,

        /// <summary>
        /// Sets acrlic effect.
        /// </summary>
        DWMSBT_TRANSIENTWINDOW = 3,

        /// <summary>
        /// Sets blurred wallpaper effect, like Mica without tint.
        /// </summary>
        DWMSBT_TABBEDWINDOW = 4
    }

    /// <summary>
    /// DWMWINDOWATTRIBUTE enumeration. (dwmapi.h)
    /// <para><see href="https://github.com/electron/electron/issues/29937"/></para>
    /// </summary>
    [Flags]
    public enum DWMWINDOWATTRIBUTE : uint
    {
        /// <summary>
        /// Enables content rendered in the non-client area to be visible on the frame drawn by DWM.
        /// </summary>
        DWMWA_ALLOW_NCPAINT = 4,

        /// <summary>
        /// Retrieves the bounds of the caption button area in the window-relative space.
        /// </summary>
        DWMWA_CAPTION_BUTTON_BOUNDS = 5,

        /// <summary>
        /// Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available.
        /// </summary>
        DWMWA_FORCE_ICONIC_REPRESENTATION = 7,

        /// <summary>
        /// Cloaks the window such that it is not visible to the user.
        /// </summary>
        DWMWA_CLOAK = 13,

        /// <summary>
        /// If the window is cloaked, provides one of the following values explaining why.
        /// </summary>
        DWMWA_CLOAKED = 14,

        /// <summary>
        /// Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents.
        /// </summary>
        DWMWA_FREEZE_REPRESENTATION = 15,

        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19,

        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

        /// <summary>
        /// Controls the policy that rounds top-level window corners.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,

        /// <summary>
        /// The color of the thin border around a top-level window.
        /// </summary>
        DWMWA_BORDER_COLOR = 34,

        /// <summary>
        /// The color of the caption.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_CAPTION_COLOR = 35,

        /// <summary>
        /// The color of the caption text.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_TEXT_COLOR = 36,

        /// <summary>
        /// Width of the visible border around a thick frame window.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37,

        /// <summary>
        /// Allows to enter a value from 0 to 4 deciding on the imposed backdrop effect.
        /// </summary>
        DWMWA_SYSTEMBACKDROP_TYPE = 38,

        /// <summary>
        /// Indicates whether the window should use the Mica effect.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        DWMWA_MICA_EFFECT = 1029
    }
    #endregion
}
