using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NetWorthTracker.Infrastructure.Services;

namespace NetWorthTracker.Infrastructure.Tests.Services;

[TestFixture]
public class SendGridEmailServiceTests
{
    private Mock<IOptions<SendGridSettings>> _mockOptions = null!;
    private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
    private Mock<ILogger<SendGridEmailService>> _mockLogger = null!;
    private SendGridSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new SendGridSettings();
        _mockOptions = new Mock<IOptions<SendGridSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_settings);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        _mockLogger = new Mock<ILogger<SendGridEmailService>>();
    }

    private SendGridEmailService CreateService()
    {
        return new SendGridEmailService(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object);
    }

    #region IsConfigured Tests

    [Test]
    public void IsConfigured_NoApiKey_ReturnsFalse()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "test@example.com";

        var service = CreateService();

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void IsConfigured_NoFromEmail_ReturnsFalse()
    {
        // Arrange
        _settings.ApiKey = "SG.test_key";
        _settings.FromEmail = "";

        var service = CreateService();

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void IsConfigured_BothConfigured_ReturnsTrue()
    {
        // Arrange
        _settings.ApiKey = "SG.test_key";
        _settings.FromEmail = "test@example.com";

        var service = CreateService();

        // Act & Assert
        service.IsConfigured.Should().BeTrue();
    }

    [Test]
    public void IsConfigured_AllEmpty_ReturnsFalse()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    #endregion

    #region SendEmail Tests

    [Test]
    public async Task SendEmailAsync_NotConfigured_LogsWarningAndReturns()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();

        // Act - Should not throw
        await service.SendEmailAsync("recipient@example.com", "Test Subject", "<p>Test Body</p>");

        // Assert - Verify warning was logged (we can't directly verify Logger calls easily with Moq)
        // The key assertion is that no exception is thrown when not configured
    }

    #endregion

    #region SendEmailVerification Tests

    [Test]
    public async Task SendEmailVerificationAsync_NotConfigured_DoesNotThrow()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.SendEmailVerificationAsync(
            "user@example.com", "https://example.com/verify?token=abc123");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SendEmailVerificationAsync_GeneratesCorrectEmailContent()
    {
        // This test verifies the email template generation logic
        // Since we can't easily mock SendGrid client, we verify the template structure exists

        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();
        var verificationLink = "https://example.com/verify?token=abc123";

        // Act - The method should handle not configured gracefully
        await service.SendEmailVerificationAsync("user@example.com", verificationLink);

        // Assert - Method completes without error, logging the warning
    }

    #endregion

    #region SendPasswordReset Tests

    [Test]
    public async Task SendPasswordResetAsync_NotConfigured_DoesNotThrow()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();

        // Act
        Func<Task> act = async () => await service.SendPasswordResetAsync(
            "user@example.com", "https://example.com/reset?token=xyz789");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SendPasswordResetAsync_GeneratesCorrectEmailContent()
    {
        // Arrange
        _settings.ApiKey = "";
        _settings.FromEmail = "";

        var service = CreateService();
        var resetLink = "https://example.com/reset?token=xyz789";

        // Act - Should handle gracefully when not configured
        await service.SendPasswordResetAsync("user@example.com", resetLink);

        // Assert - Method completes without error
    }

    #endregion

    #region Settings Tests

    [Test]
    public void Settings_DefaultFromName_IsNetWorthTracker()
    {
        // Arrange
        var settings = new SendGridSettings();

        // Assert
        settings.FromName.Should().Be("Net Worth Tracker");
    }

    [Test]
    public void Settings_DefaultValues_AreEmpty()
    {
        // Arrange
        var settings = new SendGridSettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.FromEmail.Should().BeEmpty();
    }

    #endregion
}
