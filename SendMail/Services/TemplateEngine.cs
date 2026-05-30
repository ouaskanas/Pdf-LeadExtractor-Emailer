using SendMail.Models;

namespace SendMail.Services
{
    public static class TemplateEngine
    {
        public static string ApplySubject(string template, PdfFormat lead) =>
            template
                .Replace("{SOCIETE}", lead.SOCIETE ?? "")
                .Replace("{VILLE}", lead.VILLE ?? "");

        public static string ApplyBody(string template, PdfFormat lead) =>
            template
                .Replace("{PERSONNE}", lead.PERSONNE ?? "")
                .Replace("{SOCIETE}", lead.SOCIETE ?? "")
                .Replace("{ACTIVITE}", lead.ACTIVITE ?? "")
                .Replace("{SERVICE}", lead.SERVICE ?? "")
                .Replace("{VILLE}", lead.VILLE ?? "");
    }
}
