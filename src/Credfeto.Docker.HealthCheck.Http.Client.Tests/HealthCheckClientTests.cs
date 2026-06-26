using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Docker.HealthCheck.Http.Client.Tests;

public sealed class HealthCheckClientTests : TestBase
{
    private const string HEALTH_CHECK_FLAG = "--health-check";
    private const string VALID_URL = "https://example.com/health";
    private const string INVALID_URL = "not-a-valid-url";

    [Fact]
    public void IsHealthCheck_WithMatchingArgs_ReturnsTrue()
    {
        string[] args = [HEALTH_CHECK_FLAG, VALID_URL];

        bool result = HealthCheckClient.IsHealthCheck(args.AsSpan(), out string? target);

        Assert.True(result, userMessage: "Expected IsHealthCheck to return true for matching args");
        Assert.Equal(expected: VALID_URL, actual: target);
    }

    [Theory]
    [InlineData("--health-check")]
    [InlineData("--HEALTH-CHECK")]
    [InlineData("--Health-Check")]
    public void IsHealthCheck_WithCaseInsensitiveFlag_ReturnsTrue(string flag)
    {
        string[] args = [flag, VALID_URL];

        bool result = HealthCheckClient.IsHealthCheck(args.AsSpan(), out string? target);

        Assert.True(result, userMessage: "Expected IsHealthCheck to return true for case-insensitive flag");
        Assert.Equal(expected: VALID_URL, actual: target);
    }

    [Fact]
    public void IsHealthCheck_WithEmptyArgs_ReturnsFalse()
    {
        string[] args = [];

        bool result = HealthCheckClient.IsHealthCheck(args.AsSpan(), out string? target);

        Assert.False(result, userMessage: "Expected IsHealthCheck to return false for empty args");
        Assert.Null(target);
    }

    [Fact]
    public void IsHealthCheck_WithOnlyOneArg_ReturnsFalse()
    {
        string[] args = [HEALTH_CHECK_FLAG];

        bool result = HealthCheckClient.IsHealthCheck(args.AsSpan(), out string? target);

        Assert.False(result, userMessage: "Expected IsHealthCheck to return false when only one arg is provided");
        Assert.Null(target);
    }

    [Fact]
    public void IsHealthCheck_WithWrongFlag_ReturnsFalse()
    {
        string[] args = ["--wrong-flag", VALID_URL];

        bool result = HealthCheckClient.IsHealthCheck(args.AsSpan(), out string? target);

        Assert.False(result, userMessage: "Expected IsHealthCheck to return false for wrong flag");
        Assert.Null(target);
    }

    [Fact]
    public async ValueTask ExecuteAsync_WithInvalidUrlFormat_ReturnsOne()
    {
        ILogger<HealthCheckClientTests> logger = this.GetTypedLogger<HealthCheckClientTests>();
        CancellationToken cancellationToken = this.CancellationToken();

        int result = await HealthCheckClient.ExecuteAsync(
            targetUrl: INVALID_URL,
            logger: logger,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: 1, actual: result);
    }

    [Fact]
    public async ValueTask ExecuteAsync_WithSuccessResponse_ReturnsZero()
    {
        ILogger<HealthCheckClientTests> logger = this.GetTypedLogger<HealthCheckClientTests>();
        CancellationToken cancellationToken = this.CancellationToken();

        using FixedResponseHandler handler = new(HttpStatusCode.OK);

        int result = await HealthCheckClient.ExecuteAsync(
            targetUrl: VALID_URL,
            handler: handler,
            logger: logger,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: 0, actual: result);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async ValueTask ExecuteAsync_WithNonSuccessResponse_ReturnsOne(HttpStatusCode statusCode)
    {
        ILogger<HealthCheckClientTests> logger = this.GetTypedLogger<HealthCheckClientTests>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        CancellationToken cancellationToken = this.CancellationToken();

        using FixedResponseHandler handler = new(statusCode);

        int result = await HealthCheckClient.ExecuteAsync(
            targetUrl: VALID_URL,
            handler: handler,
            logger: logger,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: 1, actual: result);
        Assert.Contains(
            logger.ReceivedCalls(),
            call =>
                StringComparer.Ordinal.Equals(call.GetMethodInfo().Name, "Log")
                && call.GetArguments()[0] is LogLevel logLevel
                && logLevel == LogLevel.Error
        );
    }

    [Fact]
    public async ValueTask ExecuteAsync_WithInvalidUrlFormatAndHandler_ReturnsOne()
    {
        ILogger<HealthCheckClientTests> logger = this.GetTypedLogger<HealthCheckClientTests>();
        CancellationToken cancellationToken = this.CancellationToken();

        using FixedResponseHandler handler = new(HttpStatusCode.OK);

        int result = await HealthCheckClient.ExecuteAsync(
            targetUrl: INVALID_URL,
            handler: handler,
            logger: logger,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: 1, actual: result);
    }

    [Fact]
    public async ValueTask ExecuteAsync_WhenHttpThrows_ReturnsOne()
    {
        ILogger<HealthCheckClientTests> logger = this.GetTypedLogger<HealthCheckClientTests>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        CancellationToken cancellationToken = this.CancellationToken();

        using ThrowingHandler handler = new();

        int result = await HealthCheckClient.ExecuteAsync(
            targetUrl: VALID_URL,
            handler: handler,
            logger: logger,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: 1, actual: result);
        Assert.Contains(
            logger.ReceivedCalls(),
            call =>
                StringComparer.Ordinal.Equals(call.GetMethodInfo().Name, "Log")
                && call.GetArguments()[0] is LogLevel logLevel
                && logLevel == LogLevel.Error
        );
    }

    private sealed class FixedResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new HttpRequestException("Simulated connection failure");
        }
    }
}
