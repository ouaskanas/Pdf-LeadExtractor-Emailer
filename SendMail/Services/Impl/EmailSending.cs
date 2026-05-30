using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendMail.Dtos;
using SendMail.Exceptions;
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

        public int ProcessAndSend(SendRequestDto sendRequestDto)
        {
            _logger.LogInformation("Initialisation SMTP pour : {SenderEmail}", sendRequestDto.SenderEmail);

            int sent = 0;

            using (var smtp = new SmtpClient())
            {
                smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate(new SaslMechanismOAuth2(sendRequestDto.SenderEmail, sendRequestDto.AccessToken));

                foreach (var lead in sendRequestDto.SelectedLeads)
                {
                    if (string.IsNullOrWhiteSpace(lead.EMAIL)) continue;

                    _logger.LogInformation("[~] Validation anti-bounce pour : {Email}", lead.EMAIL);

                    if (!_emailValidator.IsEmailValid(lead.EMAIL))
                    {
                        _logger.LogWarning("[-] {Email} rejeté (bounce détecté).", lead.EMAIL);
                        continue;
                    }

                    try
                    {
                        string finalSubject = sendRequestDto.SubjectTemplate
                            .Replace("{SOCIETE}", lead.SOCIETE ?? "")
                            .Replace("{VILLE}", lead.VILLE ?? "");

                        string finalBody = sendRequestDto.BodyTemplate
                            .Replace("{PERSONNE}", lead.PERSONNE ?? "")
                            .Replace("{SOCIETE}", lead.SOCIETE ?? "")
                            .Replace("{ACTIVITE}", lead.ACTIVITE ?? "")
                            .Replace("{SERVICE}", lead.SERVICE ?? "")
                            .Replace("{VILLE}", lead.VILLE ?? "");

                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(sendRequestDto.SenderName, sendRequestDto.SenderEmail));
                        message.To.Add(new MailboxAddress("", lead.EMAIL));
                        message.Subject = finalSubject;

                        var bodyBuilder = new BodyBuilder { TextBody = finalBody };

                        if (!string.IsNullOrEmpty(sendRequestDto.AttachmentPath) && File.Exists(sendRequestDto.AttachmentPath))
                            bodyBuilder.Attachments.Add(sendRequestDto.AttachmentPath);

                        message.Body = bodyBuilder.ToMessageBody();

                        smtp.Send(message);
                        sent++;
                        _logger.LogInformation("[SUCCESS] Mail envoyé à : {Email} ({Sent} total)", lead.EMAIL, sent);

                        int delay = new Random().Next(5000, 12000);
                        _logger.LogInformation("[*] Pause anti-block de {Delay}ms...", delay);
                        Thread.Sleep(delay);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ERROR] Échec d'envoi pour {Email} : {Message}", lead.EMAIL, ex.Message);
                    }
                }

                smtp.Disconnect(true);
            }

            return sent;
        }
    }
}
