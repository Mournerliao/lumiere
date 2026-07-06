using System.Runtime.InteropServices;
using Lumiere.Infrastructure.Interop;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace Lumiere.App;

public sealed partial class TrayMenuWindow : Window
{
    private const int ShadowMarginDips = 12;

    private readonly IntPtr hwnd;
    private TrayMenuSnapshot? currentSnapshot;
    private bool isShown;

    public TrayMenuWindow(IntPtr ownerHwnd)
    {
        InitializeComponent();

        hwnd = WindowNative.GetWindowHandle(this);

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception)
        {
            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            catch (Exception)
            {
                SystemBackdrop = null;
            }
        }

        ConfigureAsPopup(ownerHwnd);

        Activated += OnWindowActivated;
        FullscreenItem.Click += (_, _) => OnCommandSelected(TrayMenuCommand.FullscreenCapture);
        RegionItem.Click += (_, _) => OnCommandSelected(TrayMenuCommand.RegionCapture);
        OpenItem.Click += (_, _) => OnCommandSelected(TrayMenuCommand.OpenMainWindow);
        SettingsItem.Click += (_, _) => OnCommandSelected(TrayMenuCommand.OpenSettings);
        QuitItem.Click += (_, _) => OnCommandSelected(TrayMenuCommand.Quit);
    }

    public event EventHandler<TrayMenuCommand>? CommandSelected;

    public void ShowAt(int cursorX, int cursorY, TrayMenuSnapshot snapshot)
    {
        currentSnapshot = snapshot;
        ApplySnapshot(snapshot);

        var scale = GetDpiScaleForPoint(cursorX, cursorY);
        RootGrid.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var contentSize = RootGrid.DesiredSize;

        var windowWidth = (int)Math.Ceiling(contentSize.Width * scale);
        var windowHeight = (int)Math.Ceiling(contentSize.Height * scale);

        var workArea = GetWorkArea(cursorX, cursorY);

        var x = cursorX;
        var workRight = workArea.X + workArea.Width;
        if (x + windowWidth > workRight)
        {
            x = workRight - windowWidth;
        }

        var y = cursorY - windowHeight;
        if (y < workArea.Y)
        {
            y = cursorY;
        }

        isShown = true;
        AppWindow.Move(new PointInt32(x, y));
        AppWindow.Resize(new SizeInt32(windowWidth, windowHeight));
        AppWindow.Show(true);
        BringToTopmost(x, y, windowWidth, windowHeight);
        Activate();
    }

    public void Dismiss()
    {
        if (!isShown)
        {
            return;
        }

        isShown = false;
        AppWindow.Hide();
    }

    private void ApplySnapshot(TrayMenuSnapshot snapshot)
    {
        HdrStatusLabel.Text = snapshot.HdrStatusLabel;
        ToolTipService.SetToolTip(HdrStatusLabel, snapshot.HdrStatusDetail);
        if (!string.IsNullOrEmpty(snapshot.TrayAlertMessage))
        {
            HdrAlertLabel.Text = snapshot.TrayAlertMessage;
            HdrAlertLabel.Visibility = Visibility.Visible;
            HdrAlertLabel.Foreground = snapshot.TrayAlertSeverity >= 3
                ? (Brush)Application.Current.Resources["ErrorBrush"]
                : (Brush)Application.Current.Resources["WarningBrush"];
        }
        else
        {
            HdrAlertLabel.Text = string.Empty;
            HdrAlertLabel.Visibility = Visibility.Collapsed;
        }
        FullscreenItem.Label = snapshot.FullscreenCapture.Label;
        FullscreenItem.ShortcutText = snapshot.FullscreenCapture.ShortcutText ?? string.Empty;
        FullscreenItem.IsEnabled = snapshot.FullscreenCapture.IsEnabled;
        RegionItem.Label = snapshot.RegionCapture.Label;
        RegionItem.ShortcutText = snapshot.RegionCapture.ShortcutText ?? string.Empty;
        RegionItem.IsEnabled = snapshot.RegionCapture.IsEnabled;
        OpenItem.IsEnabled = snapshot.OpenMainWindow.IsEnabled;
        SettingsItem.IsEnabled = snapshot.OpenSettings.IsEnabled;
        QuitItem.IsEnabled = snapshot.Quit.IsEnabled;
    }

    private void ConfigureAsPopup(IntPtr ownerHwnd)
    {
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Title = "LumiereTrayMenu";

        var presenter = OverlappedPresenter.CreateForContextMenu();
        presenter.SetBorderAndTitleBar(false, false);
        AppWindow.SetPresenter(presenter);

        SetPopupWindowStyle(hwnd);

        AddWindowExStyle(hwnd, WsExTopmost | WsExToolwindow);

        if (ownerHwnd != IntPtr.Zero)
        {
            SetWindowLongPtr(hwnd, GwlHwndparent, ownerHwnd);
        }

        WindowFrameInterop.PreferRoundedCorners(hwnd);
        WindowFrameInterop.ExtendFrameIntoClientArea(hwnd);
        WindowFrameInterop.SuppressDwmBorder(hwnd);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            Dismiss();
        }
    }

    private void OnCommandSelected(TrayMenuCommand command)
    {
        Dismiss();
        CommandSelected?.Invoke(this, command);
    }

    private float GetDpiScale()
    {
        var dpi = GetDpiForWindow(hwnd);
        return dpi / 96f;
    }

    private float GetDpiScaleForPoint(int x, int y)
    {
        var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MonitorDefaultToNearest);
        if (GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _) == 0)
        {
            return dpiX / 96f;
        }

        return GetDpiScale();
    }

    private RectInt32 GetWorkArea(int x, int y)
    {
        var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (GetMonitorInfo(monitor, ref info))
        {
            return new RectInt32(
                info.rcWork.Left,
                info.rcWork.Top,
                info.rcWork.Right - info.rcWork.Left,
                info.rcWork.Bottom - info.rcWork.Top);
        }

        return new RectInt32(0, y - 600, 1920, 600);
    }

    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsCaption = 0x00C00000;
    private const int WsThickFrame = 0x00040000;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsMaximizeBox = 0x00010000;
    private const int WsExTopmost = 0x00000008;
    private const int WsExToolwindow = 0x00000080;
    private const int GwlHwndparent = -8;
    private const uint MonitorDefaultToNearest = 2;
    private const uint MdtEffectiveDpi = 0;
    private const int GwlStyle = -16;
    private const int GwlExstyle = -20;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopmost = new(-1);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, uint dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private static void SetPopupWindowStyle(IntPtr hWnd)
    {
        var current = GetWindowLongPtr(hWnd, GwlStyle);
        var updated = new IntPtr(
            current.ToInt64()
            | WsPopup
            & ~WsCaption
            & ~WsThickFrame
            & ~WsMinimizeBox
            & ~WsMaximizeBox);
        SetWindowLongPtr(hWnd, GwlStyle, updated);
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoActivate | SwpFrameChanged);
    }

    private static void AddWindowExStyle(IntPtr hWnd, int exStyle)
    {
        var current = GetWindowLongPtr(hWnd, GwlExstyle);
        SetWindowLongPtr(hWnd, GwlExstyle, new IntPtr(current.ToInt64() | (uint)exStyle));
    }

    private void BringToTopmost(int x, int y, int width, int height)
    {
        SetWindowPos(hwnd, HwndTopmost, x, y, width, height, SwpNoActivate | SwpShowWindow);
    }

}
