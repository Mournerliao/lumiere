using System.Runtime.InteropServices;

namespace Lumiere.Infrastructure.Interop;

public sealed class WindowsWicJpegXrEncoder : IWicJpegXrEncoder
{
    private const string OperationName = "EncodeWicJpegXrRgbaHalf";
    private const uint GenericWrite = 0x40000000;
    private const int WicBitmapEncoderNoCache = 0x2;

    private static readonly Guid ClsidWicImagingFactory2 = new("317D06E8-5F24-433D-BDF7-79CE68D8ABC2");
    private static readonly Guid GuidContainerFormatWmp = new("57A37CAA-367A-4540-916B-F183C5093A4B");
    private static readonly Guid GuidWicPixelFormat64bppRgbaHalf = new("6FDDC324-4E03-4BFE-B185-3D77768DC93A");

    private readonly Lazy<WicJpegXrEncoderReadiness> readiness =
        new(ProbeReadiness, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public WicJpegXrEncoderReadiness Readiness => readiness.Value;

    public byte[] EncodeRgbaHalf(WicJpegXrEncodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var temporaryPath = Path.Combine(
            Path.GetTempPath(),
            $"lumiere-hdr10-{Guid.NewGuid():N}.jxr");

        try
        {
            EncodeToFile(request, temporaryPath);
            return File.ReadAllBytes(temporaryPath);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static WicJpegXrEncoderReadiness ProbeReadiness()
    {
        try
        {
            var sample = new WicJpegXrEncodeRequest(
                width: 1,
                height: 1,
                strideBytes: WicJpegXrEncodeRequest.RgbaHalfBytesPerPixel,
                rgbaHalfPixels: new byte[WicJpegXrEncodeRequest.RgbaHalfBytesPerPixel]);
            _ = new WindowsWicJpegXrEncoder().EncodeRgbaHalf(sample);
            return new WicJpegXrEncoderReadiness(
                HasWindowsWicFactory: true,
                HasJpegXrContainerEncoder: true,
                AcceptsRgbaHalfPixelFormat: true,
                Blockers: []);
        }
        catch (Exception exception) when (exception is NativeInteropException or COMException or IOException)
        {
            return new WicJpegXrEncoderReadiness(
                HasWindowsWicFactory: false,
                HasJpegXrContainerEncoder: false,
                AcceptsRgbaHalfPixelFormat: false,
                Blockers: [$"Windows WIC JPEG XR readiness probe failed: {exception.Message}"]);
        }
    }

    private static void EncodeToFile(WicJpegXrEncodeRequest request, string path)
    {
        IWICImagingFactory? factory = null;
        IWICStream? stream = null;
        IWICBitmapEncoder? encoder = null;
        IWICBitmapFrameEncode? frame = null;

        try
        {
            factory = CreateFactory();
            ThrowIfFailed(
                factory.CreateStream(out stream),
                "CreateStream",
                "WIC factory could not create a stream for the temporary JPEG XR file.");
            ThrowIfFailed(
                stream.InitializeFromFilename(path, GenericWrite),
                "InitializeStream",
                "WIC stream could not open the temporary JPEG XR file.");

            var containerFormat = GuidContainerFormatWmp;
            ThrowIfFailed(
                factory.CreateEncoder(ref containerFormat, IntPtr.Zero, out encoder),
                "CreateEncoder",
                "WIC factory could not create the JPEG XR/WMP container encoder.");
            ThrowIfFailed(
                encoder.Initialize(stream, WicBitmapEncoderNoCache),
                "InitializeEncoder",
                "WIC JPEG XR encoder could not initialize against the WMP container stream.");

            encoder.CreateNewFrame(out frame, out var encoderOptions);
            if (encoderOptions != IntPtr.Zero)
            {
                Marshal.Release(encoderOptions);
            }

            ThrowIfFailed(
                frame.Initialize(IntPtr.Zero),
                "InitializeFrame",
                "WIC JPEG XR frame could not initialize.");
            ThrowIfFailed(
                frame.SetSize((uint)request.Width, (uint)request.Height),
                "SetFrameSize",
                "WIC JPEG XR frame rejected the capture dimensions.");

            var pixelFormat = GuidWicPixelFormat64bppRgbaHalf;
            ThrowIfFailed(
                frame.SetPixelFormat(ref pixelFormat),
                "SetPixelFormat",
                "WIC JPEG XR frame rejected RGBA half input.");
            if (pixelFormat != GuidWicPixelFormat64bppRgbaHalf)
            {
                throw CreateFailure(
                    unchecked((int)0x80004005),
                    "SetPixelFormat",
                    $"WIC JPEG XR encoder changed pixel format to {pixelFormat}; HDR FP16 preservation is not accepted.");
            }

            WritePixels(frame, request);

            ThrowIfFailed(
                frame.Commit(),
                "CommitFrame",
                "WIC JPEG XR frame commit failed.");
            ThrowIfFailed(
                encoder.Commit(),
                "CommitEncoder",
                "WIC JPEG XR encoder commit failed.");
        }
        catch (COMException exception)
        {
            throw CreateFailure(
                exception.HResult,
                "Interop",
                exception.Message,
                exception);
        }
        finally
        {
            ReleaseComObject(frame);
            ReleaseComObject(encoder);
            ReleaseComObject(stream);
            ReleaseComObject(factory);
        }
    }

    private static void WritePixels(
        IWICBitmapFrameEncode frame,
        WicJpegXrEncodeRequest request)
    {
        var pinned = GCHandle.Alloc(request.RgbaHalfPixels, GCHandleType.Pinned);
        try
        {
            ThrowIfFailed(
                frame.WritePixels(
                    (uint)request.Height,
                    (uint)request.StrideBytes,
                    (uint)request.RgbaHalfPixels.Length,
                    pinned.AddrOfPinnedObject()),
                "WritePixels",
                "WIC JPEG XR frame failed while writing RGBA half pixels.");
        }
        finally
        {
            pinned.Free();
        }
    }

    private static IWICImagingFactory CreateFactory()
    {
        var type = Type.GetTypeFromCLSID(ClsidWicImagingFactory2, throwOnError: true)
            ?? throw CreateFailure(
                unchecked((int)0x80040154),
                "CreateFactory",
                "WIC ImagingFactory2 COM class is not registered.");
        var instance = Activator.CreateInstance(type)
            ?? throw CreateFailure(
                unchecked((int)0x80004005),
                "CreateFactory",
                "WIC ImagingFactory2 COM class could not be instantiated.");
        return (IWICImagingFactory)instance;
    }

    private static void ThrowIfFailed(
        int hResult,
        string stage,
        string technicalDetail)
    {
        if (hResult < 0)
        {
            throw CreateFailure(hResult, stage, technicalDetail);
        }
    }

    private static NativeInteropException CreateFailure(
        int hResult,
        string stage,
        string technicalDetail,
        Exception? innerException = null) =>
        new(
            OperationName,
            stage,
            hResult,
            technicalDetail,
            "Windows WIC JPEG XR encoding failed for the HDR output artifact.",
            innerException);

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [ComImport]
    [Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICImagingFactory
    {
        void CreateDecoderFromFilename();
        void CreateDecoderFromStream();
        void CreateDecoderFromFileHandle();
        void CreateComponentInfo();
        void CreateDecoder();

        [PreserveSig]
        int CreateEncoder(
            ref Guid guidContainerFormat,
            IntPtr guidVendor,
            [MarshalAs(UnmanagedType.Interface)] out IWICBitmapEncoder encoder);

        void CreatePalette();
        void CreateFormatConverter();
        void CreateBitmapScaler();
        void CreateBitmapClipper();
        void CreateBitmapFlipRotator();

        [PreserveSig]
        int CreateStream([MarshalAs(UnmanagedType.Interface)] out IWICStream stream);
    }

    [ComImport]
    [Guid("135FF860-22B7-4DDF-B0F6-218F4F299A43")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICStream
    {
        void Read();
        void Write();
        void Seek();
        void SetSize();
        void CopyTo();
        void Commit();
        void Revert();
        void LockRegion();
        void UnlockRegion();
        void Stat();
        void Clone();
        void InitializeFromIStream();

        [PreserveSig]
        int InitializeFromFilename(
            [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            uint desiredAccess);
    }

    [ComImport]
    [Guid("00000103-A8F2-4877-BA0A-FD2B6645FB94")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapEncoder
    {
        [PreserveSig]
        int Initialize(
            [MarshalAs(UnmanagedType.Interface)] IWICStream stream,
            int cacheOption);

        void GetContainerFormat();
        void GetEncoderInfo();
        void SetColorContexts();
        void SetPalette();
        void SetThumbnail();
        void SetPreview();

        [PreserveSig]
        int CreateNewFrame(
            [MarshalAs(UnmanagedType.Interface)] out IWICBitmapFrameEncode frameEncode,
            out IntPtr encoderOptions);

        [PreserveSig]
        int Commit();
    }

    [ComImport]
    [Guid("00000105-A8F2-4877-BA0A-FD2B6645FB94")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapFrameEncode
    {
        [PreserveSig]
        int Initialize(IntPtr encoderOptions);

        [PreserveSig]
        int SetSize(uint width, uint height);

        void SetResolution();

        [PreserveSig]
        int SetPixelFormat(ref Guid pixelFormat);

        void SetColorContexts();
        void SetPalette();
        void SetThumbnail();

        [PreserveSig]
        int WritePixels(
            uint lineCount,
            uint strideBytes,
            uint bufferSize,
            IntPtr pixels);

        void WriteSource();

        [PreserveSig]
        int Commit();
    }
}
