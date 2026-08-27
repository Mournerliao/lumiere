using System.Text;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Host;

internal static class Program
{
    public static async Task Main()
    {
        Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        ILogger logger = new StructuredStderrLogger(Console.Error);

        while (await Console.In.ReadLineAsync() is { } line)
        {
            var result = PlatformProtocol.ProcessLine(line);
            await Console.Out.WriteLineAsync(result.ResponseLine);
            await Console.Out.FlushAsync();

            if (result.Diagnostic is { } diagnostic)
            {
                logger.Log(
                    LogLevel.Warning,
                    new EventId(0, diagnostic.Event),
                    diagnostic,
                    exception: null,
                    static (state, _) => state.Failure.Message);
            }
        }
    }
}
