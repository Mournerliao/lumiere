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
        ISwapChainPanelNative? nativePanel = null;

        try
        {
            inspectable = Marshal.GetIUnknownForObject(panel);
            var result = Marshal.QueryInterface(
                inspectable,
                typeof(ISwapChainPanelNative).GUID,
                out var nativePanelPointer);

            if (result != 0)
            {
                throw CreateFailure(
                    result,
                    "SwapChainPanel did not expose ISwapChainPanelNative.");
            }

            try
            {
                nativePanel = Marshal.GetObjectForIUnknown(nativePanelPointer) as ISwapChainPanelNative;
                if (nativePanel is null)
                {
                    throw CreateFailure(
                        unchecked((int)0x80004002),
                        "SwapChainPanel native pointer could not be marshaled.");
                }

                var setResult = nativePanel.SetSwapChain(swapChain);
                if (setResult != 0)
                {
                    throw CreateFailure(
                        setResult,
                        "SetSwapChain returned a failing HRESULT. Ensure the call runs on the owning UI thread.");
                }
            }
            finally
            {
                Marshal.Release(nativePanelPointer);
            }
        }
        catch (COMException exception)
        {
            throw SwapChainPanelNativeInterop.CreateFailure(operationName, exception.HResult, exception.Message, exception);
        }
        finally
        {
            if (nativePanel is not null)
            {
                Marshal.ReleaseComObject(nativePanel);
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

    [ComImport]
    [Guid("63AAD0B8-7C24-40FF-85A8-640D944CC325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        [PreserveSig]
        int SetSwapChain(IntPtr swapChain);
    }
}
