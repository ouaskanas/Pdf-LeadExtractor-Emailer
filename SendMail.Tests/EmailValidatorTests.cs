using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SendMail.Services.Impl;
using Xunit;

namespace SendMail.Tests
{
    public class EmailValidatorTests
    {
        private EmailValidator CreateValidator(string contentRootPath)
        {
            var logger = new Mock<ILogger<EmailValidator>>();
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(contentRootPath);
            return new EmailValidator(logger.Object, env.Object);
        }

        [Fact]
        public void IsEmailValid_ScriptNotFound_ReturnsTrue()
        {
            var validator = CreateValidator(@"C:\nonexistent\path\that\does\not\exist");
            var result = validator.IsEmailValid("test@example.com");
            Assert.True(result);
        }

        [Fact]
        public void IsEmailValid_ScriptNotFound_StillReturnsTrueForAnyEmail()
        {
            var validator = CreateValidator(@"C:\nonexistent\path");
            Assert.True(validator.IsEmailValid("fake@fakefake.xyz"));
            Assert.True(validator.IsEmailValid("real@gmail.com"));
        }
    }
}
