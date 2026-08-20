using System.Runtime.InteropServices;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace Lumiere.Windows.Interop;

public static class Direct3D11Interop
{
    private const string OperationName = "CreateDirect3D11DeviceFromDXGIDevice";

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(
        IntPtr dxgiDevice,
        out IntPtr graphicsDevice);

    public static IDirect3DDevice CreateDirect3DDevice(IDXGIDevice dxgiDevice)
    {
        ArgumentNullException.ThrowIfNull(dxgiDevice);

        IntPtr inspectable = IntPtr.Zero;

        try
        {
            var result = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out inspectable);
            if (result < 0)
            {
                throw CreateFailure(result, "WinRT Direct3D device wrapping failed.");
            }

            return MarshalInspectable<IDirect3DDevice>.FromAbi(inspectable);
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
            if (inspectable != IntPtr.Zero)
            {
                MarshalInspectable<IDirect3DDevice>.DisposeAbi(inspectable);
            }
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
            "Graphics interop could not create a Windows-compatible Direct3D device.",
            innerException);
}
