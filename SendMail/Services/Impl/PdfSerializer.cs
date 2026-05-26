using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using SendMail.Models;
using SendMail.Exceptions;
using SendMail.Services.IServices;

namespace SendMail.Services.Impl
{
    public class PdfParserService : IPdfSerializer
    {
        private readonly ILogger<PdfParserService> _logger;

        public PdfParserService(ILogger<PdfParserService> logger)
        {
            _logger = logger;
        }

        public List<PdfFormat> ProcessPdfAndSend(string pdfPath)
        {
            var leads = new List<PdfFormat>();
            int limit = 100;

            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");

            _logger.LogInformation("[*] Début de l'extraction des contacts du document PDF : {Path}", pdfPath);

            try
            {
                using (PdfDocument document = PdfDocument.Open(pdfPath))
                {
                    foreach (var page in document.GetPages())
                    {
                        if (leads.Count >= limit) break;

                        string pageText = page.Text;
                        if (string.IsNullOrWhiteSpace(pageText)) continue;

                        string[] lines = pageText.Split('\n');

                        foreach (var line in lines)
                        {
                            if (leads.Count >= limit) break;

                            var emailMatch = emailRegex.Match(line);
                            if (emailMatch.Success)
                            {
                                string detectedEmail = emailMatch.Value.Trim().ToLower();
                                string[] parts = line.Split(new[] { "  ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                                var newLead = new PdfFormat
                                {
                                    EMAIL = detectedEmail,
                                    SOCIETE = parts.Length > 0 ? parts[0].Trim() : "",
                                    GROUPE = parts.Length > 1 ? parts[1].Trim() : "",
                                    VILLE = parts.Length > 2 ? parts[2].Trim() : "",
                                    ACTIVITE = parts.Length > 3 ? parts[3].Trim() : "",
                                    PERSONNE = parts.Length > 4 ? parts[4].Trim() : "",
                                    SERVICE = parts.Length > 5 ? parts[5].Trim() : "",
                                    TEL = "",
                                    FAX = "",
                                    GSM = "",
                                    ADRESSE = line.Trim()
                                };

                                leads.Add(newLead);
                            }
                        }
                    }
                }

                _logger.LogInformation("[SUCCESS] Extraction réussie. {Count} leads stockés en mémoire.", leads.Count);
            }
            catch (Exception ex)
            {
                var parsingException = new PdfParsingException("Une erreur est survenue lors de l'ouverture ou du traitement structurel du PDF.", pdfPath, ex);
                _logger.LogError(ex, "[ERROR] Échec de parsing pour le fichier {Path}. Message : {Message}", parsingException.PdfPath, ex.Message);
            }

            return leads;
        }
    }
}