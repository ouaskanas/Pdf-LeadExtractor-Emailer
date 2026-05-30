using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SendMail.Controllers;
using SendMail.Dtos;
using SendMail.Services.IServices;
using Xunit;

namespace SendMail.Tests
{
    public class EmailAutomationControllerTests
    {
        private readonly Mock<IPdfSerializer> _pdfParser = new();
        private readonly Mock<IEmailSending> _emailSending = new();
        private readonly Mock<IWebHostEnvironment> _env = new();
        private readonly Mock<ILogger<EmailAutomationController>> _logger = new();
        private readonly Mock<IConfiguration> _config = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactory = new();

        private EmailAutomationController CreateController()
        {
            _config.Setup(c => c["Google:ClientId"]).Returns("test-client-id");
            _config.Setup(c => c["Google:RedirectUri"]).Returns("http://localhost:5206/EmailAutomation/OAuth2Callback");
            return new EmailAutomationController(
                _pdfParser.Object, _emailSending.Object, _env.Object,
                _logger.Object, _config.Object, _httpClientFactory.Object);
        }

        [Fact]
        public void OAuth2Authorize_ReturnsRedirectToGoogle()
        {
            var controller = CreateController();
            var result = controller.OAuth2Authorize("test@gmail.com") as RedirectResult;

            Assert.NotNull(result);
            Assert.Contains("accounts.google.com", result.Url);
            Assert.Contains("test-client-id", result.Url);
            Assert.Contains("https%3A%2F%2Fmail.google.com%2F", result.Url);
        }

        [Fact]
        public void OAuth2Authorize_EmailPassedAsLoginHintAndState()
        {
            var controller = CreateController();
            var result = controller.OAuth2Authorize("user@gmail.com") as RedirectResult;

            Assert.Contains("login_hint", result.Url);
            Assert.Contains("state", result.Url);
        }

        [Fact]
        public void SubmitCampaign_NullPayload_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = controller.SubmitCampaign(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void SubmitCampaign_EmptyLeadsJson_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = controller.SubmitCampaign(new SendCampaignPayload { SelectedLeadsJson = "" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void SubmitCampaign_MissingAccessToken_ReturnsBadRequest()
        {
            var controller = CreateController();
            var payload = new SendCampaignPayload
            {
                SelectedLeadsJson = "[]",
                AccessToken = null
            };
            var result = controller.SubmitCampaign(payload);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void SubmitCampaign_ZeroSent_ReturnsOkWithFailureMessage()
        {
            _emailSending.Setup(s => s.ProcessAndSend(It.IsAny<SendRequestDto>())).Returns(0);

            var controller = CreateController();
            var payload = new SendCampaignPayload
            {
                SenderEmail = "sender@gmail.com",
                AccessToken = "token",
                SubjectTemplate = "Objet",
                BodyTemplate = "Corps",
                SelectedLeadsJson = JsonSerializer.Serialize(new[] { new { EMAIL = "a@b.com" } })
            };

            var result = controller.SubmitCampaign(payload) as OkObjectResult;
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result.Value);
            Assert.Contains("false", json);
        }

        [Fact]
        public void SubmitCampaign_SomeSent_ReturnsOkWithSuccessMessage()
        {
            _emailSending.Setup(s => s.ProcessAndSend(It.IsAny<SendRequestDto>())).Returns(1);

            var controller = CreateController();
            var payload = new SendCampaignPayload
            {
                SenderEmail = "sender@gmail.com",
                AccessToken = "token",
                SubjectTemplate = "Objet",
                BodyTemplate = "Corps",
                SelectedLeadsJson = JsonSerializer.Serialize(new[] { new { EMAIL = "a@b.com" } })
            };

            var result = controller.SubmitCampaign(payload) as OkObjectResult;
            Assert.NotNull(result);

            var json = JsonSerializer.Serialize(result.Value);
            Assert.Contains("true", json);
            Assert.Contains("1/", json);
        }

        [Fact]
        public void LoadPdfLeads_FileNotFound_ReturnsFailureJson()
        {
            _env.Setup(e => e.WebRootPath).Returns(@"C:\nonexistent");

            var controller = CreateController();
            var result = controller.LoadPdfLeads() as JsonResult;

            Assert.NotNull(result);
            var json = JsonSerializer.Serialize(result.Value);
            Assert.Contains("false", json);
        }
    }
}
