using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendMail.Dtos;
using SendMail.Models;
using SendMail.Services.IServices;

namespace SendMail.Controllers
{
    public class EmailAutomationController : Controller
    {
        private readonly IPdfSerializer _pdfParser;
        private readonly IEmailSending _emailSending;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmailAutomationController> _logger;

        public EmailAutomationController(
            IPdfSerializer pdfParser,
            IEmailSending emailSending,
            IWebHostEnvironment env,
            ILogger<EmailAutomationController> logger)
        {
            _pdfParser = pdfParser;
            _emailSending = emailSending;
            _env = env;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ValidateSmtpCredentials(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return Json(new { success = false, message = "Identifiants incomplets." });

            try
            {
                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    smtp.Authenticate(email, password);

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("SendMail Test", email));
                    message.To.Add(new MailboxAddress("", email));
                    message.Subject = "SendMail – Test de connexion SMTP";
                    message.Body = new TextPart("plain") { Text = "Votre configuration Gmail SMTP est opérationnelle." };

                    smtp.Send(message);
                    smtp.Disconnect(true);
                }

                _logger.LogInformation("[SMTP CHECK] Authentification réussie pour {Email}", email);
                return Json(new { success = true, message = "Connexion validée ! Un email de test a été envoyé à votre adresse." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[SMTP CHECK] Échec pour {Email} : {Msg}", email, ex.Message);
                string hint = ex.Message.Contains("5.7.8") || ex.Message.Contains("BadCredentials")
                    ? "Identifiants refusés par Gmail. Utilisez un mot de passe d'application (myaccount.google.com → Sécurité → Mots de passe des applications). La vérification en 2 étapes doit être activée."
                    : $"Connexion SMTP échouée : {ex.Message}";
                return Json(new { success = false, message = hint });
            }
        }

        [HttpPost]
        public IActionResult LoadPdfLeads()
        {
            string pdfPath = Path.Combine(_env.WebRootPath, "files", "RH Emails - The bigest data base-1.pdf");

            if (!System.IO.File.Exists(pdfPath))
                return Json(new { success = false, message = "Fichier PDF introuvable dans wwwroot/files/." });

            var leads = _pdfParser.ProcessPdfAndSend(pdfPath);
            return Json(new { success = true, data = leads });
        }

        [HttpPost]
        public IActionResult SubmitCampaign([FromForm] SendCampaignPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.SelectedLeadsJson))
                return BadRequest(new { message = "Données de campagne incomplètes." });

            var leads = JsonSerializer.Deserialize<List<PdfFormat>>(payload.SelectedLeadsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            string savedAttachmentPath = null;
            if (payload.AttachedFile != null && payload.AttachedFile.Length > 0)
            {
                string tempFolder = Path.Combine(_env.ContentRootPath, "TempAttachments");
                Directory.CreateDirectory(tempFolder);

                savedAttachmentPath = Path.Combine(tempFolder, payload.AttachedFile.FileName);
                using (var stream = new FileStream(savedAttachmentPath, FileMode.Create))
                {
                    payload.AttachedFile.CopyTo(stream);
                }
            }

            var sendRequest = new SendRequestDto
            {
                SenderEmail = payload.SenderEmail,
                AppPassword = payload.AppPassword,
                SubjectTemplate = payload.SubjectTemplate,
                BodyTemplate = payload.BodyTemplate,
                SelectedLeads = leads,
                AttachmentPath = savedAttachmentPath
            };

            try
            {
                int sent = _emailSending.ProcessAndSend(sendRequest);
                if (savedAttachmentPath != null && System.IO.File.Exists(savedAttachmentPath))
                    System.IO.File.Delete(savedAttachmentPath);

                int total = leads?.Count ?? 0;
                string msg = sent == 0
                    ? $"Aucun email envoyé sur {total} contact(s). Vérifiez les logs serveur."
                    : $"{sent}/{total} email(s) envoyé(s) avec succès.";

                return Ok(new { success = sent > 0, message = msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}