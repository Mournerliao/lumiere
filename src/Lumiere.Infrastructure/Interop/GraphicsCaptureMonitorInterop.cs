using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace Lumiere.Infrastructure.Interop;

public static class GraphicsCaptureMonitorInterop
{
    private const string OperationName = "IGraphicsCaptureItemInterop.CreateForMonitor";
    private const string GraphicsCaptureItemRuntimeClassName = "Windows.Graphics.Capture.GraphicsCaptureItem";
    private static readonly Guid GraphicsCaptureItemInteropId = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    private static readonly Guid GraphicsCaptureItemInterfaceId = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    public static GraphicsCaptureItem CreateForMonitor(MonitorHandle monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (monitor.IsInvalid)
        {
            throw new ArgumentNullException(
                nameof(monitor),
                "Monitor handle cannot be NULL.");
        }

        IntPtr captureItemFactory = IntPtr.Zero;
        IntPtr interopPointer = IntPtr.Zero;
        IntPtr itemPointer = IntPtr.Zero;

        try
        {
            captureItemFactory = GetGraphicsCaptureItemFactory();
            var interopResult = Marshal.QueryInterface(
                captureItemFactory,
                GraphicsCaptureItemInteropId,
                out interopPointer);

            if (interopResult < 0)
            {
                throw CreateFailure(
                    interopResult,
                    "GraphicsCaptureItem activation factory did not expose IGraphicsCaptureItemInterop.");
            }

            var createResult = InvokeCreateForMonitor(
                interopPointer,
                monitor.RawHandle,
                GraphicsCaptureItemInterfaceId,
                out itemPointer);

            if (createResult < 0)
            {
                throw CreateFailure(
                    createResult,
                    $"CreateForMonitor failed for monitor handle 0x{monitor.RawHandle:X}.");
            }

            // FromAbi takes ownership of the COM reference — do not release itemPointer after this call.
            return GraphicsCaptureItem.FromAbi(itemPointer);
        }
        catch (COMException exception)
        {
            throw CreateFailure(
                exception.HResult,
                exception.Message,
                exception);
        }
        finally
        {
            if (captureItemFactory != IntPtr.Zero)
            {
                Marshal.Release(captureItemFactory);
            }

            if (interopPointer != IntPtr.Zero)
            {
                Marshal.Release(interopPointer);
            }
        }
    }

    private static IntPtr GetGraphicsCaptureItemFactory()
    {
        var classId = IntPtr.Zero;
        var factoryPtr = IntPtr.Zero;
        try
        {
            classId = CreateHString(GraphicsCaptureItemRuntimeClassName);
            var interfaceId = typeof(IGraphicsCaptureItemInterop).GUID;
            var result = RoGetActivationFactory(
                classId,
                in interfaceId,
                out factoryPtr);

            if (result < 0)
            {
                throw CreateFailure(
                    result,
                    $"RoGetActivationFactory failed for {GraphicsCaptureItemRuntimeClassName}.");
            }

            return factoryPtr;
        }
        catch
        {
            if (factoryPtr != IntPtr.Zero)
            {
                Marshal.Release(factoryPtr);
            }

            throw;
        }
        finally
        {
            if (classId != IntPtr.Zero)
            {
                _ = WindowsDeleteString(classId);
            }
        }
    }

    private static IntPtr CreateHString(string value)
    {
        var result = WindowsCreateString(value, value.Length, out var hstring);
        if (result < 0)
        {
            throw CreateFailure(
                result,
                $"WindowsCreateString failed for {value}.");
        }

        return hstring;
    }

    private static int InvokeCreateForMonitor(
        IntPtr interopPointer,
        IntPtr monitorHandle,
        Guid resultInterfaceId,
        out IntPtr resultPointer)
    {
        // IGraphicsCaptureItemInterop inherits IUnknown. Slot 3 is CreateForWindow;
        // slot 4 is CreateForMonitor(HMONITOR, REFIID, void**).
        var vtable = Marshal.ReadIntPtr(interopPointer);
        var createForMonitorPtr = Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);
        var createForMonitor = Marshal.GetDelegateForFunctionPointer<CreateForMonitorDelegate>(createForMonitorPtr);

        return createForMonitor(interopPointer, monitorHandle, in resultInterfaceId, out resultPointer);
    }

    [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int RoGetActivationFactory(
        IntPtr activatableClassId,
        in Guid interfaceId,
        out IntPtr factory);

    [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        int length,
        out IntPtr hstring);

    [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int WindowsDeleteString(IntPtr hstring);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateForMonitorDelegate(
        IntPtr interopPointer,
        IntPtr monitorHandle,
        in Guid resultInterfaceId,
        out IntPtr resultPointer);

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
    }

    public static NativeInteropException CreateFailure(
        int hResult,
        string technicalDetail,
        Exception? innerException = null) =>
        new(
            OperationName,
            "Interop",
            hResult,
            technicalDetail,
            "Could not create a capture target for the selected display.",
            innerException);
}
