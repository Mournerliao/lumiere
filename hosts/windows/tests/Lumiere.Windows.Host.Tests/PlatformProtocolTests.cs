using System.Text.Json;
using Lumiere.Windows.Host;
using Xunit;

namespace Lumiere.Windows.Host.Tests;

public sealed class PlatformProtocolTests
{
    [Fact]
    public void GetCapabilities_ReportsAvailableHostWithoutUnimplementedCaptureClaims()
    {
        var response = PlatformProtocol.ProcessLine(
            """{"version":2,"id":"capabilities-1","method":"getCapabilities","params":{}}""");

        using var document = JsonDocument.Parse(response.ResponseLine);
        var root = document.RootElement;
        var result = root.GetProperty("result");
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.Equal("capabilities-1", root.GetProperty("id").GetString());
        Assert.Equal("windows", result.GetProperty("platform").GetString());
        Assert.Equal("available", result.GetProperty("hostStatus").GetString());
        Assert.Empty(result.GetProperty("captureModes").EnumerateArray());
        Assert.Empty(result.GetProperty("deliveryTargets").EnumerateArray());
        Assert.Equal("unavailable", result.GetProperty("hdrCapture").GetString());
        Assert.Equal("srgb-visual-match", result.GetProperty("outputProfiles")[0].GetString());
        Assert.Null(response.Diagnostic);
    }

    [Fact]
    public void Capture_ReturnsTypedUnavailableResultUntilEngineIsConnected()
    {
        var response = PlatformProtocol.ProcessLine(
            """{"version":2,"id":"capture-1","method":"capture","params":{"mode":"display","delivery":"folder"}}""");

        using var document = JsonDocument.Parse(response.ResponseLine);
        var result = document.RootElement.GetProperty("result");
        Assert.Equal("failed", result.GetProperty("status").GetString());
        Assert.Equal(
            "capture-unavailable",
            result.GetProperty("failure").GetProperty("code").GetString());
        Assert.Equal("capture-1", response.Diagnostic?.RequestId);
    }

    [Fact]
    public void InvalidRequest_PreservesValidRequestIdForCorrelation()
    {
        var response = PlatformProtocol.ProcessLine(
            """{"version":2,"id":"bad-1","method":"getCapabilities","params":{},"extra":true}""");

        using var document = JsonDocument.Parse(response.ResponseLine);
        var root = document.RootElement;
        Assert.Equal("bad-1", root.GetProperty("id").GetString());
        Assert.Equal("invalid-request", root.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("bad-1", response.Diagnostic?.RequestId);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"version\":1,\"id\":\"bad-2\",\"method\":\"getCapabilities\",\"params\":{}}")]
    [InlineData("{\"version\":2,\"id\":\"bad-3\",\"method\":\"unknown\",\"params\":{}}")]
    [InlineData("{\"version\":2,\"id\":\"bad-4\",\"method\":\"capture\",\"params\":{\"mode\":\"region\",\"delivery\":\"both\"}}")]
    public void InvalidRequests_ReturnProtocolErrors(string line)
    {
        var response = PlatformProtocol.ProcessLine(line);

        using var document = JsonDocument.Parse(response.ResponseLine);
        Assert.Equal(
            "invalid-request",
            document.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.False(document.RootElement.GetProperty("error").GetProperty("retryable").GetBoolean());
        Assert.NotNull(response.Diagnostic);
    }
}
