using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Credfeto.Docker.HealthCheck.Http.Client.LoggingExtensions;

internal static partial class HealthCheckClientLoggingExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Healthcheck failed for {uri}")]
    internal static partial void HealthCheckFailed(this ILogger logger, Uri uri, Exception exception);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Healthcheck returned unhealthy status {statusCode} for {uri}"
    )]
    internal static partial void HealthCheckUnhealthy(this ILogger logger, HttpStatusCode statusCode, Uri uri);
}
