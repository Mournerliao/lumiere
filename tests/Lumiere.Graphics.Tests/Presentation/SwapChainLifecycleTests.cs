using System.Reflection;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Presentation;

public sealed class SwapChainLifecycleTests
{
    [Fact]
    public void DisposalCoordinatorDetachesPreviewBeforeReleasingResources()
    {
        var calls = new List<string>();

        SwapChainDisposalCoordinator.DisposeOnce(
            () => calls.Add("detach"),
            () => calls.Add("release"));

        Assert.Equal(new[] { "detach", "release" }, calls);
    }

    [Fact]
    public void DisposalCoordinatorDoesNotReleaseResourcesWhenDetachFails()
    {
        var releaseCalled = false;

        var exception = Assert.Throws<SwapChainPresentationException>(
            () => SwapChainDisposalCoordinator.DisposeOnce(
                () => throw new SwapChainPresentationException(
                    "ISwapChainPanelNative.SetSwapChain(null)",
                    unchecked((int)0x8001010E),
                    "SetSwapChain(null) must run on the owning UI thread."),
                () => releaseCalled = true));

        Assert.False(releaseCalled);
        Assert.Equal("ISwapChainPanelNative.SetSwapChain(null)", exception.OperationName);
        Assert.Contains("0x8001010E", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SwapChainManagerDoesNotExposeRawUnattachedCreationPath()
    {
        var rawCreateMethod = typeof(SwapChainManager).GetMethod(
            "CreateCompositionSwapChain",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Null(rawCreateMethod);
    }

    [Fact]
    public void DisposalCoordinatorCanBeRetriedAfterDetachFailure()
    {
        var detachShouldFail = true;
        var calls = new List<string>();

        Assert.Throws<SwapChainPresentationException>(
            () => SwapChainDisposalCoordinator.DisposeOnce(
                () =>
                {
                    calls.Add("detach-failed");
                    if (detachShouldFail)
                    {
                        throw new SwapChainPresentationException(
                            "ISwapChainPanelNative.SetSwapChain(null)",
                            unchecked((int)0x8001010E),
                            "SetSwapChain(null) must run on the owning UI thread.");
                    }
                },
                () => calls.Add("release")));

        detachShouldFail = false;

        SwapChainDisposalCoordinator.DisposeOnce(
            () => calls.Add("detach-retried"),
            () => calls.Add("release"));

        Assert.Equal(
            new[] { "detach-failed", "detach-retried", "release" },
            calls);
    }
}
