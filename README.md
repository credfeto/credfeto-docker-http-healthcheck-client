# credfeto-docker-http-healthcheck-client
Dotnet HTTP Healthcheck client

Simple healthcheck method that allows talking to the local server in distro-less or chiseled containers by running the main executable with parameters detailing how to check.

e.g. hit the ping endpoint in on the specified server URL and return an exit code of 0 if success or 1 if failed

```dockerfile
HEALTHCHECK --interval=5s --timeout=2s --retries=3 --start-period=5s CMD [ "/usr/src/app/Server", "--health-check", "http://127.0.0.1:8080/ping?source=docker" ]
```

Usage in Program.cs (assuming async Main):

```csharp
public static async Task<int> Main(string[] args)
{
    return HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl)
        ? await HealthCheckClient.ExecuteAsync(
            targetUrl: checkUrl,
            cancellationToken: CancellationToken.None
        )
        : await RunServerAsync(args);
}
```

## Build Status

| Branch  | Status                                                                                                                                                                                                                                |
|---------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| main    | [![Build: Pre-Release](https://github.com/credfeto/credfeto-docker-http-healthcheck-client/actions/workflows/build-and-publish-pre-release.yml/badge.svg)](https://github.com/credfeto/credfeto-docker-http-healthcheck-client/actions/workflows/build-and-publish-pre-release.yml) |
| release | [![Build: Release](https://github.com/credfeto/credfeto-docker-http-healthcheck-client/actions/workflows/build-and-publish-release.yml/badge.svg)](https://github.com/credfeto/credfeto-docker-http-healthcheck-client/actions/workflows/build-and-publish-release.yml)             |

## Changelog

View [changelog](CHANGELOG.md)

## Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->