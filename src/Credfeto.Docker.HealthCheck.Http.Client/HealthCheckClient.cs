using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace Credfeto.Docker.HealthCheck.Http.Client;

public static class HealthCheckClient
{
    private const int HEALTHCHECK_SUCCESS = 0;
    private const int HEALTHCHECK_FAIL = 1;

    public static async ValueTask<int> ExecuteAsync(
        string targetUrl,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        if (!Uri.TryCreate(uriString: targetUrl, uriKind: UriKind.Absolute, out Uri? uri))
        {
            return HEALTHCHECK_FAIL;
        }

        using (HttpClient httpClient = CreateHttpClient())
        {
            return await ExecuteWithClientAsync(
                uri: uri,
                httpClient: httpClient,
                logger: logger,
                cancellationToken: cancellationToken
            );
        }
    }

    public static async ValueTask<int> ExecuteAsync(
        string targetUrl,
        HttpMessageHandler handler,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        if (!Uri.TryCreate(uriString: targetUrl, uriKind: UriKind.Absolute, out Uri? uri))
        {
            return HEALTHCHECK_FAIL;
        }

        using (HttpClient httpClient = CreateHttpClient(handler))
        {
            return await ExecuteWithClientAsync(
                uri: uri,
                httpClient: httpClient,
                logger: logger,
                cancellationToken: cancellationToken
            );
        }
    }

    private static async ValueTask<int> ExecuteWithClientAsync(
        Uri uri,
        HttpClient httpClient,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using (
                HttpResponseMessage r = await httpClient.GetAsync(requestUri: uri, cancellationToken: cancellationToken)
            )
            {
                if (!r.IsSuccessStatusCode)
                {
                    logger.HealthCheckUnhealthy(statusCode: r.StatusCode, uri: uri);

                    return HEALTHCHECK_FAIL;
                }

                return HEALTHCHECK_SUCCESS;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.HealthCheckFailed(uri, exception);

            return HEALTHCHECK_FAIL;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        return ConfigureHttpClient(new HttpClient());
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return ConfigureHttpClient(new HttpClient(handler, disposeHandler: false));
    }

    private static HttpClient ConfigureHttpClient(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.ConnectionClose = true;

        return httpClient;
    }

    public static bool IsHealthCheck(in ReadOnlySpan<string> args, [NotNullWhen(true)] out string? target)
    {
        if (args.Length == 2 && StringComparer.OrdinalIgnoreCase.Equals(args[0], y: "--health-check"))
        {
            target = args[1];

            return true;
        }

        target = null;

        return false;
    }
}
