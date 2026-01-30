using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace NetWorthTracker.Integration.Tests;

[TestFixture]
public class HealthEndpointTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task HealthEndpoint_ReturnsSuccessOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Health endpoint returns 200 (healthy) or 503 (unhealthy), both are valid responses
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task HealthEndpoint_ReturnsJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task HealthEndpoint_IncludesStatusField()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("status", out var statusProperty).Should().BeTrue();
        statusProperty.GetString().Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task HealthEndpoint_IncludesChecksArray()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        json.RootElement.TryGetProperty("checks", out var checksProperty).Should().BeTrue();
        checksProperty.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Test]
    public async Task HealthEndpoint_ChecksHaveNameAndStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        var checks = json.RootElement.GetProperty("checks");
        foreach (var check in checks.EnumerateArray())
        {
            check.TryGetProperty("name", out _).Should().BeTrue();
            check.TryGetProperty("status", out _).Should().BeTrue();
        }
    }

    [Test]
    public async Task HealthEndpoint_IncludesDatabaseCheck()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        var checks = json.RootElement.GetProperty("checks");
        var checkNames = checks.EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        checkNames.Should().Contain("database");
    }
}
