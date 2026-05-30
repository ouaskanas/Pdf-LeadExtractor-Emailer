using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using SendMail.Dtos;
using SendMail.Models;
using SendMail.Services.IServices;
using SendMail.Services.Impl;
using Xunit;

namespace SendMail.Tests
{
    public class EmailSendingTests
    {
        private readonly Mock<ILogger<EmailSending>> _logger = new();
        private readonly Mock<IEmailValidator> _validator = new();

        private EmailSending CreateService() => new EmailSending(_logger.Object, _validator.Object);

        private static SendRequestDto BaseDto(List<PdfFormat> leads) => new SendRequestDto
        {
            SenderEmail = "sender@gmail.com",
            SenderName = "Test Sender",
            AccessToken = "fake-token",
            SubjectTemplate = "Objet {SOCIETE}",
            BodyTemplate = "Bonjour {PERSONNE}",
            SelectedLeads = leads
        };

        [Fact]
        public void ProcessAndSend_NullLeads_ReturnsZero()
        {
            var dto = BaseDto(null);
            var result = CreateService().ProcessAndSend(dto);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessAndSend_EmptyLeads_ReturnsZero()
        {
            var dto = BaseDto(new List<PdfFormat>());
            var result = CreateService().ProcessAndSend(dto);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessAndSend_LeadWithEmptyEmail_IsSkipped()
        {
            var dto = BaseDto(new List<PdfFormat> { new PdfFormat { EMAIL = "" } });
            var result = CreateService().ProcessAndSend(dto);

            _validator.Verify(v => v.IsEmailValid(It.IsAny<string>()), Times.Never);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessAndSend_LeadWithWhitespaceEmail_IsSkipped()
        {
            var dto = BaseDto(new List<PdfFormat> { new PdfFormat { EMAIL = "   " } });
            var result = CreateService().ProcessAndSend(dto);

            _validator.Verify(v => v.IsEmailValid(It.IsAny<string>()), Times.Never);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessAndSend_ValidatorRejectsAllLeads_ReturnsZeroWithoutSmtpConnect()
        {
            _validator.Setup(v => v.IsEmailValid(It.IsAny<string>())).Returns(false);

            var dto = BaseDto(new List<PdfFormat>
            {
                new PdfFormat { EMAIL = "a@example.com" },
                new PdfFormat { EMAIL = "b@example.com" }
            });

            var result = CreateService().ProcessAndSend(dto);
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProcessAndSend_ValidatorCalledForEachNonEmptyEmail()
        {
            _validator.Setup(v => v.IsEmailValid(It.IsAny<string>())).Returns(false);

            var dto = BaseDto(new List<PdfFormat>
            {
                new PdfFormat { EMAIL = "a@example.com" },
                new PdfFormat { EMAIL = "" },
                new PdfFormat { EMAIL = "b@example.com" }
            });

            CreateService().ProcessAndSend(dto);

            _validator.Verify(v => v.IsEmailValid("a@example.com"), Times.Once);
            _validator.Verify(v => v.IsEmailValid("b@example.com"), Times.Once);
            _validator.Verify(v => v.IsEmailValid(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void ProcessAndSend_ValidatorCalledWithExactEmail()
        {
            _validator.Setup(v => v.IsEmailValid("target@example.com")).Returns(false);

            var dto = BaseDto(new List<PdfFormat> { new PdfFormat { EMAIL = "target@example.com" } });

            CreateService().ProcessAndSend(dto);

            _validator.Verify(v => v.IsEmailValid("target@example.com"), Times.Once);
        }
    }
}
