using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace Lumiere.Infrastructure.Interop;

public static class Direct3D11Interop
{
    private const string OperationName = "CreateDirect3D11DeviceFromDXGIDevice";

    public static IDirect3DDevice CreateDirect3DDevice(IDXGIDevice dxgiDevice)
    {
        ArgumentNullException.ThrowIfNull(dxgiDevice);

        IntPtr inspectable = IntPtr.Zero;

        try
        {
            var result = D3D11.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out inspectable);
            if (result.Failure)
            {
                throw CreateFailure(result.Code, "WinRT Direct3D device wrapping failed.");
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
