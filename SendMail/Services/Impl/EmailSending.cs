using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendMail.Dtos;
using SendMail.Models;
using SendMail.Services;
using SendMail.Services.IServices;

namespace SendMail.Services.Impl
{
    public class EmailSending : IEmailSending
    {
        private readonly ILogger<EmailSending> _logger;
        private readonly IEmailValidator _emailValidator;

        public EmailSending(ILogger<EmailSending> logger, IEmailValidator emailValidator)
        {
            _logger = logger;
            _emailValidator = emailValidator;
        }

        public int ProcessAndSend(SendRequestDto dto)
        {
            _logger.LogInformation("Début de campagne pour : {SenderEmail}", dto.SenderEmail);

            int sent = 0;
            SmtpClient smtp = null;

            try
            {
                foreach (var lead in dto.SelectedLeads ?? new List<PdfFormat>())
                {
                    if (string.IsNullOrWhiteSpace(lead.EMAIL)) continue;

                    _logger.LogInformation("[~] Validation anti-bounce pour : {Email}", lead.EMAIL);

                    if (!_emailValidator.IsEmailValid(lead.EMAIL))
                    {
                        _logger.LogWarning("[-] {Email} rejeté (bounce détecté).", lead.EMAIL);
                        continue;
                    }

                    if (smtp == null)
                    {
                        smtp = new SmtpClient();
                        smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                        smtp.Authenticate(new SaslMechanismOAuth2(dto.SenderEmail, dto.AccessToken));
                    }

                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(dto.SenderName, dto.SenderEmail));
                        message.To.Add(new MailboxAddress("", lead.EMAIL));
                        message.Subject = TemplateEngine.ApplySubject(dto.SubjectTemplate, lead);

                        var bodyBuilder = new BodyBuilder { TextBody = TemplateEngine.ApplyBody(dto.BodyTemplate, lead) };

                        if (!string.IsNullOrEmpty(dto.AttachmentPath) && File.Exists(dto.AttachmentPath))
                            bodyBuilder.Attachments.Add(dto.AttachmentPath);

                        message.Body = bodyBuilder.ToMessageBody();

                        smtp.Send(message);
                        sent++;
                        _logger.LogInformation("[SUCCESS] Mail envoyé à : {Email} ({Sent} total)", lead.EMAIL, sent);

                        Thread.Sleep(new Random().Next(5000, 12000));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ERROR] Échec d'envoi pour {Email} : {Message}", lead.EMAIL, ex.Message);
                    }
                }

                smtp?.Disconnect(true);
            }
            finally
            {
                smtp?.Dispose();
            }

            return sent;
        }
    }
}
