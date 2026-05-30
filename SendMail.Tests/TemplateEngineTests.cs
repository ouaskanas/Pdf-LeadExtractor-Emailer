using SendMail.Models;
using SendMail.Services;
using Xunit;

namespace SendMail.Tests
{
    public class TemplateEngineTests
    {
        private static PdfFormat Lead(string societe = "Acme", string ville = "Paris",
            string personne = "Alice", string activite = "Tech", string service = "RH") =>
            new PdfFormat { SOCIETE = societe, VILLE = ville, PERSONNE = personne, ACTIVITE = activite, SERVICE = service };

        [Fact]
        public void ApplySubject_ReplacesAllPlaceholders()
        {
            var result = TemplateEngine.ApplySubject("Bonjour {SOCIETE} à {VILLE}", Lead());
            Assert.Equal("Bonjour Acme à Paris", result);
        }

        [Fact]
        public void ApplyBody_ReplacesAllPlaceholders()
        {
            var template = "Bonjour {PERSONNE} de {SOCIETE}, secteur {ACTIVITE}, service {SERVICE}, ville {VILLE}.";
            var result = TemplateEngine.ApplyBody(template, Lead());
            Assert.Equal("Bonjour Alice de Acme, secteur Tech, service RH, ville Paris.", result);
        }

        [Fact]
        public void ApplySubject_NullLeadValues_ReplacedWithEmpty()
        {
            var result = TemplateEngine.ApplySubject("{SOCIETE} - {VILLE}", new PdfFormat());
            Assert.Equal(" - ", result);
        }

        [Fact]
        public void ApplyBody_NullLeadValues_ReplacedWithEmpty()
        {
            var result = TemplateEngine.ApplyBody("Bonjour {PERSONNE}", new PdfFormat());
            Assert.Equal("Bonjour ", result);
        }

        [Fact]
        public void ApplyBody_UnknownPlaceholders_LeftAsIs()
        {
            var result = TemplateEngine.ApplyBody("Test {UNKNOWN}", Lead());
            Assert.Equal("Test {UNKNOWN}", result);
        }

        [Fact]
        public void ApplySubject_NoPlaceholders_ReturnsUnchanged()
        {
            var result = TemplateEngine.ApplySubject("Objet fixe", Lead());
            Assert.Equal("Objet fixe", result);
        }
    }
}
