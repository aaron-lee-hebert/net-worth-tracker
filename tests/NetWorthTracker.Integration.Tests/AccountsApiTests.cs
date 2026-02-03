using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace NetWorthTracker.Integration.Tests;

[TestFixture]
public class AccountsApiTests
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
    public async Task AccountsIndex_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task AccountsCreate_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Accounts/Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task AccountsDetails_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync($"/Accounts/Details/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task AccountsEdit_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync($"/Accounts/Edit/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task AccountsDelete_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync($"/Accounts/Delete/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task ReportsQuarterly_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Reports/Quarterly");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task Forecasts_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Forecasts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

}
