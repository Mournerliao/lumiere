using System.Runtime.InteropServices;

namespace Lumiere.Infrastructure.Interop;

public sealed class WindowsWicJpegXrMetadataReader : IWicJpegXrMetadataReader
{
    private const string OperationName = "ReadWicJpegXrMetadata";
    private const uint GenericRead = 0x80000000;
    private const int WicDecodeMetadataCacheOnDemand = 0x0;
    private const ushort VtLpwstr = 31;
    private const ushort VtBstr = 8;

    private static readonly Guid ClsidWicImagingFactory2 = new("317D06E8-5F24-433D-BDF7-79CE68D8ABC2");

    public IReadOnlyDictionary<string, string> ReadStringMetadata(
        byte[] jpegXrBytes,
        IReadOnlyList<string> queryPaths)
    {
        ArgumentNullException.ThrowIfNull(jpegXrBytes);
        ArgumentNullException.ThrowIfNull(queryPaths);

        if (jpegXrBytes.Length == 0)
        {
            throw new ArgumentException("JPEG XR bytes must be provided.", nameof(jpegXrBytes));
        }

        if (queryPaths.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var temporaryPath = Path.Combine(
            Path.GetTempPath(),
            $"lumiere-hdr10-metadata-{Guid.NewGuid():N}.jxr");

        try
        {
            File.WriteAllBytes(temporaryPath, jpegXrBytes);
            return ReadFromFile(temporaryPath, queryPaths);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static IReadOnlyDictionary<string, string> ReadFromFile(
        string path,
        IReadOnlyList<string> queryPaths)
    {
        IWICImagingFactory? factory = null;
        IWICStream? stream = null;
        IWICBitmapDecoder? decoder = null;
        IWICBitmapFrameDecode? frame = null;
        IWICMetadataQueryReader? reader = null;

        try
        {
            factory = CreateFactory();
            ThrowIfFailed(
                factory.CreateStream(out stream),
                "CreateStream",
                "WIC factory could not create a stream for the JPEG XR artifact.");
            ThrowIfFailed(
                stream.InitializeFromFilename(path, GenericRead),
                "InitializeStream",
                "WIC stream could not open the JPEG XR artifact.");
            ThrowIfFailed(
                factory.CreateDecoderFromStream(
                    stream,
                    IntPtr.Zero,
                    WicDecodeMetadataCacheOnDemand,
                    out decoder),
                "CreateDecoderFromStream",
                "WIC factory could not create a decoder for the JPEG XR artifact.");
            ThrowIfFailed(
                decoder.GetFrame(0, out frame),
                "GetFrame",
                "WIC JPEG XR decoder could not read the first frame.");
            ThrowIfFailed(
                frame.GetMetadataQueryReader(out reader),
                "GetMetadataQueryReader",
                "WIC JPEG XR frame did not provide a metadata query reader.");

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var queryPath in queryPaths)
            {
                if (string.IsNullOrWhiteSpace(queryPath))
                {
                    throw new ArgumentException(
                        "WIC JPEG XR metadata query path must be provided.",
                        nameof(queryPaths));
                }

                var value = ReadStringMetadata(reader, queryPath);
                if (value is not null)
                {
                    values[queryPath] = value;
                }
            }

            return values;
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
            ReleaseComObject(reader);
            ReleaseComObject(frame);
            ReleaseComObject(decoder);
            ReleaseComObject(stream);
            ReleaseComObject(factory);
        }
    }

    private static string? ReadStringMetadata(
        IWICMetadataQueryReader reader,
        string queryPath)
    {
        using var propVariant = PropVariantHandle.Allocate();
        var hResult = reader.GetMetadataByName(queryPath, propVariant.DangerousGetHandle());
        if (hResult == unchecked((int)0x80070490))
        {
            return null;
        }

        ThrowIfFailed(
            hResult,
            "GetMetadataByName",
            $"WIC JPEG XR metadata reader failed for query path '{queryPath}'.");

        return propVariant.ReadString();
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
            "Windows WIC JPEG XR metadata inspection failed for the HDR output artifact.",
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

        [PreserveSig]
        int CreateDecoderFromStream(
            [MarshalAs(UnmanagedType.Interface)] IWICStream stream,
            IntPtr guidVendor,
            int metadataOptions,
            [MarshalAs(UnmanagedType.Interface)] out IWICBitmapDecoder decoder);

        void CreateDecoderFromFileHandle();
        void CreateComponentInfo();
        void CreateDecoder();
        void CreateEncoder();
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
    [Guid("9EDDE9E7-8DEE-47EA-99DF-E6FAF2ED44BF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapDecoder
    {
        void QueryCapability();
        void Initialize();
        void GetContainerFormat();
        void GetDecoderInfo();
        void CopyPalette();
        void GetMetadataQueryReader();
        void GetPreview();
        void GetColorContexts();
        void GetThumbnail();
        void GetFrameCount();

        [PreserveSig]
        int GetFrame(
            uint index,
            [MarshalAs(UnmanagedType.Interface)] out IWICBitmapFrameDecode frame);
    }

    [ComImport]
    [Guid("3B16811B-6A43-4EC9-A813-3D930C13B940")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICBitmapFrameDecode
    {
        void GetSize();
        void GetPixelFormat();
        void GetResolution();
        void CopyPalette();
        void CopyPixels();

        [PreserveSig]
        int GetMetadataQueryReader(
            [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryReader metadataQueryReader);

        void GetColorContexts();
        void GetThumbnail();
    }

    [ComImport]
    [Guid("30989668-E1C9-4597-B395-458EEDB808DF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWICMetadataQueryReader
    {
        void GetContainerFormat();
        void GetLocation();

        [PreserveSig]
        int GetMetadataByName(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            IntPtr propVariant);

        void GetEnumerator();
    }

    private sealed class PropVariantHandle : SafeHandle
    {
        private PropVariantHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static PropVariantHandle Allocate()
        {
            var propVariant = new PropVariantHandle
            {
                handle = Marshal.AllocCoTaskMem(IntPtr.Size == 8 ? 24 : 16),
            };
            Span<byte> zero = stackalloc byte[IntPtr.Size == 8 ? 24 : 16];
            Marshal.Copy(zero.ToArray(), 0, propVariant.handle, zero.Length);
            return propVariant;
        }

        public string? ReadString()
        {
            var variantType = unchecked((ushort)Marshal.ReadInt16(handle));
            var valuePointer = Marshal.ReadIntPtr(handle, IntPtr.Size == 8 ? 8 : 4);
            if (valuePointer == IntPtr.Zero)
            {
                return null;
            }

            return variantType switch
            {
                VtLpwstr => Marshal.PtrToStringUni(valuePointer),
                VtBstr => Marshal.PtrToStringBSTR(valuePointer),
                _ => throw CreateFailure(
                    unchecked((int)0x80004005),
                    "ReadPropVariant",
                    $"WIC JPEG XR metadata query returned unsupported PROPVARIANT type {variantType}."),
            };
        }

        protected override bool ReleaseHandle()
        {
            _ = PropVariantClear(handle);
            Marshal.FreeCoTaskMem(handle);
            return true;
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int PropVariantClear(IntPtr propVariant);
}
