using System.Runtime.InteropServices;
using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

public sealed class WindowsTrayMenu : ITrayMenu
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Infrastructure);
    private const int MaxTipLength = 128;
    private const int WmCommand = 0x0111;
    private const int WmNull = 0x0000;
    private const int WmTrayIcon = 0x8000 + 42;
    private const int WmLButtonDblClick = 0x0203;
    private const int WmRButtonUp = 0x0205;
    private const int WmContextMenu = 0x007B;
    private const int NifMessage = 0x00000001;
    private const int NifIcon = 0x00000002;
    private const int NifTip = 0x00000004;
    private const int NimAdd = 0x00000000;
    private const int NimModify = 0x00000001;
    private const int NimDelete = 0x00000002;
    private const int NimSetVersion = 0x00000004;
    private const int NotifyIconVersion4 = 4;
    private const uint MfString = 0x00000000;
    private const uint MfSeparator = 0x00000800;
    private const uint MfGrayed = 0x00000001;
    private const uint MfChecked = 0x00000008;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmLeftAlign = 0x0000;
    private static readonly IntPtr IdiApplication = new(32512);
    private const int SwHide = 0;

    private readonly IntPtr ownerHwnd;
    private readonly WndProc wndProc;
    private readonly string className;
    private readonly IntPtr hInstance;
    private readonly IntPtr hIcon;
    private IntPtr messageHwnd;
    private TrayMenuSnapshot snapshot;
    private volatile bool disposed;

    private WindowsTrayMenu(IntPtr ownerHwnd, TrayMenuSnapshot initialSnapshot)
    {
        this.ownerHwnd = ownerHwnd == IntPtr.Zero
            ? throw new ArgumentException("Owner window handle is required.", nameof(ownerHwnd))
            : ownerHwnd;
        snapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
        wndProc = WindowProcedure;
        className = $"LumiereTrayMenu_{Guid.NewGuid():N}";
        hInstance = GetModuleHandle(null);
        hIcon = LoadIcon(IntPtr.Zero, IdiApplication);

        try
        {
            RegisterWindowClass();
            CreateMessageWindow();
            AddIcon();
        }
        catch
        {
            DisposePartialInit();
            throw;
        }
    }

    public event EventHandler<TrayMenuCommandRequestedEventArgs>? CommandRequested;
    public event EventHandler<TrayMenuShowRequestedEventArgs>? MenuShowRequested;

    public static WindowsTrayMenu CreateForWindow(Window owner, TrayMenuSnapshot initialSnapshot) =>
        new(WindowNative.GetWindowHandle(owner ?? throw new ArgumentNullException(nameof(owner))), initialSnapshot);

    public void Update(TrayMenuSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        ModifyIcon();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        RemoveIcon();
        if (messageHwnd != IntPtr.Zero)
        {
            DestroyWindow(messageHwnd);
            messageHwnd = IntPtr.Zero;
        }

        UnregisterClass(className, hInstance);
    }

    private IntPtr WindowProcedure(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (disposed)
        {
            return DefWindowProc(hWnd, message, wParam, lParam);
        }

        if (message == WmTrayIcon)
        {
            var mouseMessage = (int)(lParam.ToInt64() & 0xFFFF);
            if (mouseMessage == WmLButtonDblClick)
            {
                CommandRequested?.Invoke(this, new TrayMenuCommandRequestedEventArgs(TrayMenuCommand.OpenMainWindow));
                return IntPtr.Zero;
            }

            if (mouseMessage == WmRButtonUp || mouseMessage == WmContextMenu)
            {
                ShowMenu();
                return IntPtr.Zero;
            }
        }

        if (message == WmCommand)
        {
            int commandId = unchecked((int)(wParam.ToInt64() & 0xFFFF));
            if (commandId >= (int)TrayMenuCommand.FullscreenCapture
                && commandId <= (int)TrayMenuCommand.Quit)
            {
                CommandRequested?.Invoke(this, new TrayMenuCommandRequestedEventArgs((TrayMenuCommand)commandId));
                return IntPtr.Zero;
            }
        }

        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    private void ShowMenu()
    {
        if (disposed)
        {
            return;
        }

        GetCursorPos(out var point);
        SetForegroundWindow(ownerHwnd);
        var menuShowRequested = MenuShowRequested;
        if (menuShowRequested is null)
        {
            ShowNativeMenu(point);
            return;
        }

        try
        {
            menuShowRequested.Invoke(this, new TrayMenuShowRequestedEventArgs(point.X, point.Y, snapshot));
        }
        catch (Exception exception)
        {
            var diagnostic = DiagnosticContext.TrayWarning(
                stage: "ShowMenu",
                userFacingState: "Tray menu display failed",
                technicalDetail: $"Custom tray menu failed to show: {exception.GetType().Name}: {exception.Message}. Falling back to native popup menu.",
                exception: exception);
            diagnostic.LogTo(Logger);

            ShowNativeMenu(point);
        }
    }

    private static void AppendDisabledHeader(IntPtr menu, string label) =>
        AppendMenu(menu, MfString | MfGrayed, UIntPtr.Zero, label);

    private static void AppendCommand(IntPtr menu, TrayMenuCommand command, TrayMenuItemSnapshot item)
    {
        var flags = MfString
            | (item.IsEnabled ? 0 : MfGrayed)
            | (item.IsActive ? MfChecked : 0);
        var label = string.IsNullOrWhiteSpace(item.ShortcutText)
            ? item.Label
            : $"{item.Label}\t{item.ShortcutText}";

        AppendMenu(menu, flags, new UIntPtr((uint)command), label);
    }

    private void ShowNativeMenu(POINT point)
    {
        var currentSnapshot = snapshot;
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            AppendDisabledHeader(menu, currentSnapshot.AppName);
            AppendDisabledHeader(menu, currentSnapshot.HdrStatusLabel);
            AppendMenu(menu, MfSeparator, UIntPtr.Zero, null);
            AppendCommand(menu, TrayMenuCommand.FullscreenCapture, currentSnapshot.FullscreenCapture);
            AppendCommand(menu, TrayMenuCommand.RegionCapture, currentSnapshot.RegionCapture);
            AppendMenu(menu, MfSeparator, UIntPtr.Zero, null);
            AppendCommand(menu, TrayMenuCommand.OpenMainWindow, currentSnapshot.OpenMainWindow);
            AppendCommand(menu, TrayMenuCommand.OpenSettings, currentSnapshot.OpenSettings);
            AppendCommand(menu, TrayMenuCommand.Quit, currentSnapshot.Quit);

            SetForegroundWindow(ownerHwnd);
            TrackPopupMenu(menu, TpmLeftAlign | TpmRightButton, point.X, point.Y, 0, messageHwnd, IntPtr.Zero);
            PostMessage(ownerHwnd, WmNull, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private void RegisterWindowClass()
    {
        var wndClass = new WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            hInstance = hInstance,
            lpszClassName = className,
        };

        if (RegisterClassEx(ref wndClass) == 0)
        {
            throw CreateInteropException("RegisterClassEx", "TrayInit");
        }
    }

    private void CreateMessageWindow()
    {
        messageHwnd = CreateWindowEx(
            0,
            className,
            "LumiereTrayMessageWindow",
            0,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (messageHwnd == IntPtr.Zero)
        {
            throw CreateInteropException("CreateWindowEx", "TrayInit");
        }

        ShowWindow(messageHwnd, SwHide);
    }

    private void AddIcon()
    {
        var data = CreateNotifyIconData();
        if (!Shell_NotifyIcon(NimAdd, ref data))
        {
            throw CreateInteropException("Shell_NotifyIcon", "TrayAdd");
        }

        data.uVersion = NotifyIconVersion4;
        Shell_NotifyIcon(NimSetVersion, ref data);
    }

    private void ModifyIcon()
    {
        var data = CreateNotifyIconData();
        if (!Shell_NotifyIcon(NimModify, ref data))
        {
            var diagnostic = DiagnosticContext.TrayWarning(
                stage: "ModifyIcon",
                userFacingState: "Tray icon update failed",
                technicalDetail: "Shell_NotifyIcon(NIM_MODIFY) failed; tray icon state may be stale.");
            diagnostic.LogTo(Logger);
        }
    }

    private void RemoveIcon()
    {
        if (messageHwnd == IntPtr.Zero)
        {
            return;
        }

        var data = CreateNotifyIconData();
        Shell_NotifyIcon(NimDelete, ref data);
    }

    private void DisposePartialInit()
    {
        RemoveIcon();
        if (messageHwnd != IntPtr.Zero)
        {
            DestroyWindow(messageHwnd);
            messageHwnd = IntPtr.Zero;
        }

        UnregisterClass(className, hInstance);
    }

    private NOTIFYICONDATA CreateNotifyIconData()
    {
        var tooltip = $"{snapshot.AppName} - {snapshot.HdrStatusLabel}";
        if (tooltip.Length >= MaxTipLength)
        {
            tooltip = tooltip[..(MaxTipLength - 1)];
        }

        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = messageHwnd,
            uID = 1,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = WmTrayIcon,
            hIcon = hIcon,
            szTip = tooltip,
        };
    }

    private static NativeInteropException CreateInteropException(string operationName, string stage)
    {
        var error = Marshal.GetLastWin32Error();
        return new NativeInteropException(
            operationName,
            stage,
            error,
            $"{operationName} failed while initializing the native tray menu.",
            "Tray menu is unavailable.");
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public int uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxTipLength)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "LoadIconW", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool TrackPopupMenu(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        int nReserved,
        IntPtr hWnd,
        IntPtr prcRect);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true, SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
