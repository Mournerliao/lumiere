using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainManager
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private const string OperationName = "IDXGIFactory2.CreateSwapChainForComposition";

    private static readonly Dictionary<SwapChainTargetHint, HdrDisplayCapability> CachedHdrCapabilities = [];

    public SwapChainResources CreateAttachedCompositionSwapChain(
        GraphicsDeviceResources deviceResources,
        SwapChainCreationOptions options,
        ISwapChainPreviewSurface previewSurface,
        SwapChainTargetHint? targetHint = null)
    {
        ArgumentNullException.ThrowIfNull(previewSurface);

        return CreateCompositionSwapChain(
            deviceResources,
            options,
            previewSurface.AttachSwapChain,
            previewSurface.DetachSwapChain,
            targetHint);
    }

    private static SwapChainResources CreateCompositionSwapChain(
        GraphicsDeviceResources deviceResources,
        SwapChainCreationOptions options,
        Action<IDXGISwapChain> attachPreview,
        Action detachPreview,
        SwapChainTargetHint? targetHint = null)
    {
        ArgumentNullException.ThrowIfNull(deviceResources);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(attachPreview);
        ArgumentNullException.ThrowIfNull(detachPreview);

        IDXGIFactory2? factory = null;
        IDXGISwapChain1? swapChain = null;
        IDXGISwapChain3? swapChain3 = null;

        try
        {
            factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(debug: false);
            swapChain = factory.CreateSwapChainForComposition(
                deviceResources.Device,
                options.CreateDescription(),
                restrictToOutput: null);
            swapChain3 = swapChain.QueryInterface<IDXGISwapChain3>();

            Logger.LogDebug("SwapChain created: format={Format}, colorSpace={ColorSpace}", options.CreateDescription().Format, options.ColorSpace);

            var displayCapability = GetDisplayCapability(factory, targetHint);

            var presentationEvidence = SwapChainColorSpaceConfigurator.Configure(
                new SwapChainColorSpaceController(swapChain3),
                options.ColorSpace,
                displayCapability,
                requireTargetedDisplayCapability: targetHint is not null);

            attachPreview(swapChain);

            Logger.LogDebug("SwapChainPanel attached: success");

            return new SwapChainResources(swapChain, presentationEvidence, detachPreview);
        }
        catch (Exception exception)
        {
            swapChain?.Dispose();

            var diagnostic = DiagnosticContext.PreviewFailure(
                stage: "SwapChainCreation",
                userFacingState: "Preview initialization failed",
                technicalDetail: $"Operation={OperationName}, Detail={FormatExceptionDetail(exception)}",
                exception: exception);
            diagnostic.LogTo(Logger);

            throw CreateFailure(FormatExceptionDetail(exception), exception);
        }
        finally
        {
            swapChain3?.Dispose();
            factory?.Dispose();
        }
    }

    private static HdrDisplayCapability GetDisplayCapability(
        IDXGIFactory2 factory,
        SwapChainTargetHint? targetHint)
    {
        if (targetHint is null)
        {
            return HdrDisplayCapability.Probe(factory);
        }

        var normalizedHint = targetHint.Normalize();
        if (!CachedHdrCapabilities.TryGetValue(normalizedHint, out var capability))
        {
            capability = HdrDisplayCapability.Probe(
                factory,
                normalizedHint.DisplayName,
                normalizedHint.Width,
                normalizedHint.Height);
            CachedHdrCapabilities[normalizedHint] = capability;
        }

        return capability;
    }

    public static PreviewReadinessStatus FormatFailureAsReadiness(Exception exception) =>
        PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Presentation,
            "Preview presentation setup failed before HDR correctness could be validated.",
            FormatExceptionDetail(exception));

    private static SwapChainPresentationException CreateFailure(
        string technicalDetail,
        Exception? innerException = null) =>
        new(OperationName, innerException?.HResult ?? 0, technicalDetail, innerException);

    private static string FormatExceptionDetail(Exception exception)
    {
        if (exception is SwapChainPresentationException presentationException)
        {
            return presentationException.Message;
        }

        var hResult = exception.HResult == 0
            ? string.Empty
            : $" HRESULT {NativeInteropException.FormatHResult(exception.HResult)}.";

        return $"{exception.GetType().Name}:{hResult} {exception.Message}";
    }
}

public sealed record SwapChainTargetHint(
    string? DisplayName,
    int Width,
    int Height)
{
    public SwapChainTargetHint Normalize() =>
        new(
            string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim(),
            Math.Max(0, Width),
            Math.Max(0, Height));
}
