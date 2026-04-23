using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Infrastructure.Interop;

public static class Direct3D11SurfaceInterop
{
    private const string OperationName = "GetDXGIInterfaceFromObject";

    [DllImport("d3d11.dll", ExactSpelling = true, EntryPoint = "GetDXGIInterfaceFromObject")]
    private static extern int GetDxgiInterfaceFromObject(
        [MarshalAs(UnmanagedType.IInspectable)] object graphicsObject,
        in Guid iid,
        out IntPtr dxgiInterface);

    public static ID3D11Texture2D CreateTexture(IDirect3DSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        IntPtr texturePointer = IntPtr.Zero;

        try
        {
            var interfaceId = typeof(ID3D11Texture2D).GUID;
            var result = GetDxgiInterfaceFromObject(surface, interfaceId, out texturePointer);
            if (result < 0)
            {
                throw CreateFailure(
                    result,
                    "GetDXGIInterfaceFromObject could not unwrap the captured surface as ID3D11Texture2D.");
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
            "Capture interop could not unwrap a Direct3D surface for HDR preview rendering.",
            innerException);
}
