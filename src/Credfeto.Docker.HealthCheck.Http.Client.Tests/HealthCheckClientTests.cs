using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
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
}
