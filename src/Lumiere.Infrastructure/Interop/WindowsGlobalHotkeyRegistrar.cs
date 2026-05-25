using System.Runtime.InteropServices;

namespace Lumiere.Infrastructure.Interop;

public sealed class WindowsGlobalHotkeyRegistrar : IGlobalHotkeyRegistrar
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly WndProc wndProc;
    private readonly string className;
    private readonly IntPtr hInstance;
    private readonly Dictionary<int, HotkeyCommand> registeredCommands = [];
    private IntPtr messageHwnd;
    private bool disposed;

    public WindowsGlobalHotkeyRegistrar()
    {
        wndProc = WindowProcedure;
        className = $"LumiereHotkeys_{Guid.NewGuid():N}";
        hInstance = GetModuleHandle(null);
        RegisterWindowClass();
        CreateMessageWindow();
    }

    public event EventHandler<GlobalHotkeyPressedEventArgs>? HotkeyPressed;

    public IReadOnlyList<GlobalHotkeyRegistrationResult> Register(IReadOnlyCollection<GlobalHotkeyRegistration> registrations)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(registrations);

        UnregisterAll();
        var results = new List<GlobalHotkeyRegistrationResult>(registrations.Count);
        foreach (var registration in registrations)
        {
            var modifiers = CreateModifiers(registration);
            if (RegisterHotKey(messageHwnd, registration.Id, modifiers, (uint)registration.VirtualKey))
            {
                registeredCommands[registration.Id] = registration.Command;
                results.Add(new GlobalHotkeyRegistrationResult(
                    registration.Command,
                    registration.DisplayText,
                    Registered: true,
                    "Registered with Windows."));
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                results.Add(new GlobalHotkeyRegistrationResult(
                    registration.Command,
                    registration.DisplayText,
                    Registered: false,
                    $"RegisterHotKey failed with Win32 error {error}."));
            }
        }

        return results;
    }

    public void UnregisterAll()
    {
        foreach (var id in registeredCommands.Keys.ToArray())
        {
            UnregisterHotKey(messageHwnd, id);
        }

        registeredCommands.Clear();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        UnregisterAll();
        if (messageHwnd != IntPtr.Zero)
        {
            DestroyWindow(messageHwnd);
            messageHwnd = IntPtr.Zero;
        }

        UnregisterClass(className, hInstance);
    }

    private IntPtr WindowProcedure(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmHotkey && registeredCommands.TryGetValue(wParam.ToInt32(), out var command))
        {
            HotkeyPressed?.Invoke(this, new GlobalHotkeyPressedEventArgs(command));
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    private static uint CreateModifiers(GlobalHotkeyRegistration registration)
    {
        var modifiers = ModNoRepeat;
        if (registration.Control)
        {
            modifiers |= ModControl;
        }

        if (registration.Alt)
        {
            modifiers |= ModAlt;
        }

        if (registration.Shift)
        {
            modifiers |= ModShift;
        }

        if (registration.Windows)
        {
            modifiers |= ModWin;
        }

        return modifiers;
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
            throw CreateInteropException("RegisterClassEx", "HotkeyInit");
        }
    }

    private void CreateMessageWindow()
    {
        messageHwnd = CreateWindowEx(
            0,
            className,
            "LumiereHotkeyMessageWindow",
            0,
            0,
            0,
            0,
            0,
            HwndMessage,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (messageHwnd == IntPtr.Zero)
        {
            throw CreateInteropException("CreateWindowEx", "HotkeyInit");
        }
    }

    private static NativeInteropException CreateInteropException(string operationName, string stage)
    {
        var error = Marshal.GetLastWin32Error();
        return new NativeInteropException(
            operationName,
            stage,
            error,
            $"{operationName} failed while initializing global hotkeys.",
            "Global shortcuts are unavailable.");
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

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
