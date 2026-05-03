using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Vortice.DXGI;

namespace Lumiere.Infrastructure.Interop;

public static class SwapChainPanelNativeInterop
{
    private const string AttachOperationName = "ISwapChainPanelNative.SetSwapChain";
    private const string DetachOperationName = "ISwapChainPanelNative.SetSwapChain(null)";

    public static void AttachSwapChain(
        SwapChainPanel panel,
        IDXGISwapChain swapChain)
    {
        ArgumentNullException.ThrowIfNull(panel);
        ArgumentNullException.ThrowIfNull(swapChain);

        SetSwapChain(panel, swapChain.NativePointer, AttachOperationName);
    }

    public static void DetachSwapChain(SwapChainPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        SetSwapChain(panel, IntPtr.Zero, DetachOperationName);
    }

    public static NativeInteropException CreateFailure(
        string operationName,
        int hResult,
        string technicalDetail,
        Exception? innerException = null) =>
        new(
            operationName,
            "Interop",
            hResult,
            technicalDetail,
            "Preview surface could not attach or detach the DirectX swap chain.",
            innerException);

    private static void SetSwapChain(
        SwapChainPanel panel,
        IntPtr swapChain,
        string operationName)
    {
        IntPtr inspectable = IntPtr.Zero;
        IntPtr nativePanelPointer = IntPtr.Zero;

        try
        {
            inspectable = Marshal.GetIUnknownForObject(panel);
            var result = Marshal.QueryInterface(
                inspectable,
                typeof(ISwapChainPanelNativeMarker).GUID,
                out nativePanelPointer);

            if (result != 0)
            {
                throw CreateFailure(
                    result,
                    "SwapChainPanel did not expose ISwapChainPanelNative.");
            }

            var setResult = InvokeSetSwapChain(nativePanelPointer, swapChain);
            if (setResult != 0)
            {
                throw CreateFailure(
                    setResult,
                    "SetSwapChain returned a failing HRESULT. Ensure the call runs on the owning UI thread.");
            }
        }
        catch (COMException exception)
        {
            throw SwapChainPanelNativeInterop.CreateFailure(operationName, exception.HResult, exception.Message, exception);
        }
        finally
        {
            if (nativePanelPointer != IntPtr.Zero)
            {
                Marshal.Release(nativePanelPointer);
            }

            if (inspectable != IntPtr.Zero)
            {
                Marshal.Release(inspectable);
            }
        }

        NativeInteropException CreateFailure(
            int hResult,
            string technicalDetail,
            Exception? innerException = null) =>
            SwapChainPanelNativeInterop.CreateFailure(operationName, hResult, technicalDetail, innerException);
    }

    private static int InvokeSetSwapChain(
        IntPtr nativePanelPointer,
        IntPtr swapChain)
    {
        var vtable = Marshal.ReadIntPtr(nativePanelPointer);
        var setSwapChainPointer = Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);
        var setSwapChain = Marshal.GetDelegateForFunctionPointer<SetSwapChainDelegate>(setSwapChainPointer);

        return setSwapChain(nativePanelPointer, swapChain);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetSwapChainDelegate(
        IntPtr nativePanelPointer,
        IntPtr swapChain);

    [ComImport]
    [Guid("63AAD0B8-7C24-40FF-85A8-640D944CC325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNativeMarker
    {
    }
}
