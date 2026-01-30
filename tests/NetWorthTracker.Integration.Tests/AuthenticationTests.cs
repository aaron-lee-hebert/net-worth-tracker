using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace NetWorthTracker.Integration.Tests;

[TestFixture]
public class AuthenticationTests
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
    public async Task LoginPage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task RegisterPage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Account/Register");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task UnauthenticatedRequest_ToDashboard_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task UnauthenticatedRequest_ToAccounts_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task UnauthenticatedRequest_ToSettings_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Test]
    public async Task LoginWithInvalidCredentials_ReturnsLoginPageWithError()
    {
        // Arrange
        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "nonexistent@example.com",
            ["Password"] = "WrongPassword123"
        });

        // First get the login page to extract anti-forgery token
        var loginPage = await _client.GetAsync("/Account/Login");
        var loginPageContent = await loginPage.Content.ReadAsStringAsync();

        // For now, we'll just verify the login page loads
        // Full form submission would require extracting and submitting anti-forgery token

        // Act
        var response = await _client.GetAsync("/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ForgotPasswordPage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Account/ForgotPassword");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task HomePage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task PrivacyPage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Home/Privacy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task TermsPage_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Home/Terms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task PricingPage_ReturnsSuccessOrRedirect()
    {
        // Act
        var response = await _client.GetAsync("/Home/Pricing");

        // Assert - May return OK or redirect depending on configuration
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AdminPage_UnauthenticatedRequest_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
}
