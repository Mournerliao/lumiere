using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Infrastructure.Interop;

public static class Direct3D11SurfaceInterop
{
    private const string OperationName = "IDirect3DDxgiInterfaceAccess.GetInterface";
    private static readonly Guid Direct3DDxgiInterfaceAccessId = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");

    public static ID3D11Texture2D CreateTexture(IDirect3DSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        IntPtr dxgiInterfaceAccess = IntPtr.Zero;
        IntPtr texturePointer = IntPtr.Zero;

        try
        {
            var surfacePointer = ((WinRT.IWinRTObject)surface).NativeObject.ThisPtr;
            var interfaceId = typeof(ID3D11Texture2D).GUID;
            var accessResult = Marshal.QueryInterface(
                surfacePointer,
                Direct3DDxgiInterfaceAccessId,
                out dxgiInterfaceAccess);

            if (accessResult < 0)
            {
                throw CreateFailure(
                    accessResult,
                    "Captured surface did not expose IDirect3DDxgiInterfaceAccess.");
            }

            var result = InvokeGetInterface(dxgiInterfaceAccess, interfaceId, out texturePointer);
            if (result < 0)
            {
                throw CreateFailure(
                    result,
                    "IDirect3DDxgiInterfaceAccess could not unwrap the captured surface as ID3D11Texture2D.");
            }

            return new ID3D11Texture2D(texturePointer);
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
            if (dxgiInterfaceAccess != IntPtr.Zero)
            {
                Marshal.Release(dxgiInterfaceAccess);
            }
        }
    }

    private static int InvokeGetInterface(
        IntPtr dxgiInterfaceAccess,
        Guid interfaceId,
        out IntPtr dxgiInterface)
    {
        var vtable = Marshal.ReadIntPtr(dxgiInterfaceAccess);
        var getInterfacePointer = Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);
        var getInterface = Marshal.GetDelegateForFunctionPointer<GetInterfaceDelegate>(getInterfacePointer);

        return getInterface(dxgiInterfaceAccess, interfaceId, out dxgiInterface);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetInterfaceDelegate(
        IntPtr dxgiInterfaceAccess,
        in Guid interfaceId,
        out IntPtr dxgiInterface);

    public static NativeInteropException CreateFailure(
        int hResult,
        string technicalDetail,
        Exception? innerException = null) =>
        new(
            OperationName,
            "Interop",
            hResult,
            technicalDetail,
            "Capture interop could not unwrap a Direct3D surface for HDR preview rendering.",
            innerException);
}
