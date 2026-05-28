using System;
using Microsoft.Extensions.Logging;

namespace Credfeto.Docker.HealthCheck.Http.Client.LoggingExtensions;

internal static partial class HealthCheckClientLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Healthcheck failed for {uri}")]
    public static partial void HealthCheckFailed(this ILogger logger, Uri uri, Exception exception);
}
