using System.Text.Json;

namespace Lumiere.Windows.Host;

public sealed record PlatformFailure(string Code, string Message, bool Retryable);

public sealed record HostDiagnostic(
    string Event,
    string RequestId,
    PlatformFailure Failure);

public sealed record ProtocolLineResult(
    string ResponseLine,
    HostDiagnostic? Diagnostic = null);

public static class PlatformProtocol
{
    public const int ContractVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ProtocolLineResult ProcessLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var requestId = "invalid-request";
        try
        {
            using var document = JsonDocument.Parse(line);
            var envelope = document.RootElement;
            if (envelope.ValueKind == JsonValueKind.Object
                && envelope.TryGetProperty("id", out var candidateId)
                && candidateId.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(candidateId.GetString()))
            {
                requestId = candidateId.GetString()!;
            }

            RequireExactProperties(envelope, "version", "id", "method", "params");
            RequireVersion(envelope);
            requestId = RequireNonEmptyString(envelope, "id", "Request id");
            var method = RequireNonEmptyString(envelope, "method", "Request method");
            var parameters = RequireObject(envelope, "params", "Request params");

            return method switch
            {
                "getCapabilities" => ProcessGetCapabilities(requestId, parameters),
                "capture" => ProcessCapture(requestId, parameters),
                _ => throw new PlatformProtocolException("Unknown platform-host method."),
            };
        }
        catch (JsonException)
        {
            return Failure(
                requestId,
                "invalid-request",
                "Request must be one complete JSON object.",
                retryable: false);
        }
        catch (PlatformProtocolException exception)
        {
            return Failure(
                requestId,
                "invalid-request",
                exception.Message,
                retryable: false);
        }
    }

    private static ProtocolLineResult ProcessGetCapabilities(
        string requestId,
        JsonElement parameters)
    {
        RequireExactProperties(parameters);
        var capabilities = new
        {
            contractVersion = ContractVersion,
            platform = "windows",
            hostStatus = "available",
            captureModes = Array.Empty<string>(),
            deliveryTargets = Array.Empty<string>(),
            hdrCapture = "unavailable",
            outputProfiles = new[] { "srgb-visual-match" },
        };
        return Success(requestId, capabilities);
    }

    private static ProtocolLineResult ProcessCapture(
        string requestId,
        JsonElement parameters)
    {
        ValidateCaptureParameters(parameters);
        var failure = new PlatformFailure(
            "capture-unavailable",
            "Windows capture is not connected to the platform Host yet.",
            Retryable: false);
        var result = new
        {
            status = "failed",
            failure,
        };
        return new ProtocolLineResult(
            Serialize(new { version = ContractVersion, id = requestId, result }),
            new HostDiagnostic("capture-unavailable", requestId, failure));
    }

    private static void ValidateCaptureParameters(JsonElement parameters)
    {
        var mode = RequireNonEmptyString(parameters, "mode", "Capture mode");
        var delivery = RequireNonEmptyString(parameters, "delivery", "Capture delivery");
        if (delivery is not ("clipboard" or "folder" or "both"))
        {
            throw new PlatformProtocolException(
                "Capture delivery must be clipboard, folder, or both.");
        }

        if (mode == "display")
        {
            RequireExactProperties(parameters, "mode", "delivery");
            return;
        }

        if (mode != "region")
        {
            throw new PlatformProtocolException("Capture mode must be region or display.");
        }

        RequireExactProperties(parameters, "mode", "delivery", "targetId", "geometry");
        _ = RequireNonEmptyString(parameters, "targetId", "Region target id");
        var geometry = RequireObject(parameters, "geometry", "Region geometry");
        RequireExactProperties(geometry, "coordinateSpace", "x", "y", "width", "height");
        if (RequireNonEmptyString(geometry, "coordinateSpace", "Region coordinate space")
            != "target-logical")
        {
            throw new PlatformProtocolException(
                "Region geometry must use target-logical coordinates.");
        }

        RequireFiniteNumber(geometry, "x", positive: false);
        RequireFiniteNumber(geometry, "y", positive: false);
        RequireFiniteNumber(geometry, "width", positive: true);
        RequireFiniteNumber(geometry, "height", positive: true);
    }

    private static ProtocolLineResult Success(string requestId, object result) =>
        new(Serialize(new { version = ContractVersion, id = requestId, result }));

    private static ProtocolLineResult Failure(
        string requestId,
        string code,
        string message,
        bool retryable)
    {
        var failure = new PlatformFailure(code, message, retryable);
        return new ProtocolLineResult(
            Serialize(new { version = ContractVersion, id = requestId, error = failure }),
            new HostDiagnostic("request-failed", requestId, failure));
    }

    private static string Serialize(object value) =>
        JsonSerializer.Serialize(value, SerializerOptions);

    private static void RequireVersion(JsonElement envelope)
    {
        if (!envelope.TryGetProperty("version", out var version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var value)
            || value != ContractVersion)
        {
            throw new PlatformProtocolException("Protocol version must be 2.");
        }
    }

    private static JsonElement RequireObject(
        JsonElement value,
        string propertyName,
        string description)
    {
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Object)
        {
            throw new PlatformProtocolException($"{description} must be an object.");
        }

        return property;
    }

    private static string RequireNonEmptyString(
        JsonElement value,
        string propertyName,
        string description)
    {
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrEmpty(property.GetString()))
        {
            throw new PlatformProtocolException($"{description} must be a non-empty string.");
        }

        return property.GetString()!;
    }

    private static void RequireFiniteNumber(
        JsonElement value,
        string propertyName,
        bool positive)
    {
        if (!value.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDouble(out var number)
            || !double.IsFinite(number)
            || (positive ? number <= 0 : number < 0))
        {
            var requirement = positive ? "a finite positive number" : "a finite non-negative number";
            throw new PlatformProtocolException(
                $"Region {propertyName} must be {requirement}.");
        }
    }

    private static void RequireExactProperties(
        JsonElement value,
        params string[] expectedProperties)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new PlatformProtocolException("Request value must be an object.");
        }

        var expected = expectedProperties.ToHashSet(StringComparer.Ordinal);
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!actual.Add(property.Name) || !expected.Contains(property.Name))
            {
                throw new PlatformProtocolException(
                    "Request contains missing, duplicate, or unknown fields.");
            }
        }

        if (!actual.SetEquals(expected))
        {
            throw new PlatformProtocolException(
                "Request contains missing, duplicate, or unknown fields.");
        }
    }

    private sealed class PlatformProtocolException(string message) : Exception(message);
}
