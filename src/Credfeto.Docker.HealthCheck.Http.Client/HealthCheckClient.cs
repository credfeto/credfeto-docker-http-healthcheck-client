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

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

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

        using (HttpClient httpClient = CreateHttpClient(timeout: null))
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
        TimeSpan? timeout,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        if (!Uri.TryCreate(uriString: targetUrl, uriKind: UriKind.Absolute, out Uri? uri))
        {
            return HEALTHCHECK_FAIL;
        }

        using (HttpClient httpClient = CreateHttpClient(timeout))
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
        ArgumentNullException.ThrowIfNull(handler);

        if (!Uri.TryCreate(uriString: targetUrl, uriKind: UriKind.Absolute, out Uri? uri))
        {
            return HEALTHCHECK_FAIL;
        }

        using (HttpClient httpClient = CreateHttpClient(handler: handler, timeout: null))
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
        TimeSpan? timeout,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!Uri.TryCreate(uriString: targetUrl, uriKind: UriKind.Absolute, out Uri? uri))
        {
            return HEALTHCHECK_FAIL;
        }

        using (HttpClient httpClient = CreateHttpClient(handler: handler, timeout: timeout))
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
                HttpResponseMessage r = await httpClient.GetAsync(
                    requestUri: uri,
                    completionOption: HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken: cancellationToken
                )
            )
            {
                if (!r.IsSuccessStatusCode)
                {
                    logger.HealthCheckUnhealthy(statusCode: r.StatusCode, uri: SafeUri(uri));

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
            logger.HealthCheckFailed(SafeUri(uri), exception);

            return HEALTHCHECK_FAIL;
        }
    }

    private static string SafeUri(Uri uri)
    {
        return uri.GetComponents(
            UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path,
            UriFormat.UriEscaped
        );
    }

    private static HttpClient CreateHttpClient(TimeSpan? timeout)
    {
        return ConfigureHttpClient(httpClient: new HttpClient(), timeout: timeout);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler, TimeSpan? timeout)
    {
        return ConfigureHttpClient(httpClient: new HttpClient(handler, disposeHandler: false), timeout: timeout);
    }

    private static HttpClient ConfigureHttpClient(HttpClient httpClient, TimeSpan? timeout)
    {
        httpClient.DefaultRequestHeaders.ConnectionClose = true;
        httpClient.Timeout = timeout ?? DefaultTimeout;

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
