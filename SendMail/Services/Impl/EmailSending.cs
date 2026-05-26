using Microsoft.Extensions.Logging;
using SendMail.Dtos;
using SendMail.Models;
using SendMail.Exceptions;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
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

        public void ProcessAndSend(SendRequestDto sendRequestDto)
        {
            _logger.LogInformation("Initialisation de la connexion SMTP pour le compte : {SenderEmail}", sendRequestDto.SenderEmail);

            using (SmtpClient smtpServer = new SmtpClient("smtp.gmail.com"))
            {
                smtpServer.Port = 587;
                smtpServer.Credentials = new NetworkCredential(sendRequestDto.SenderEmail, sendRequestDto.AppPassword);
                smtpServer.EnableSsl = true;

                foreach (var lead in sendRequestDto.SelectedLeads)
                {
                    if (string.IsNullOrWhiteSpace(lead.EMAIL)) continue;

                    _logger.LogInformation("[~] Analyse anti-bounce via le script Python pour : {Email}", lead.EMAIL);

                    var isRealEmail = _emailValidator.IsEmailValid(lead.EMAIL);

                    if (isRealEmail)
                    {
                        try
                        {
                            string finalSubject = sendRequestDto.SubjectTemplate
                                .Replace("{SOCIETE}", lead.SOCIETE)
                                .Replace("{VILLE}", lead.VILLE);

                            string finalBody = sendRequestDto.BodyTemplate
                                .Replace("{PERSONNE}", lead.PERSONNE)
                                .Replace("{SOCIETE}", lead.SOCIETE)
                                .Replace("{ACTIVITE}", lead.ACTIVITE)
                                .Replace("{SERVICE}", lead.SERVICE);

                            MailMessage mail = new MailMessage();
                            mail.From = new MailAddress(sendRequestDto.SenderEmail, "Votre Nom / Entreprise");
                            mail.To.Add(lead.EMAIL);
                            mail.Subject = finalSubject;
                            mail.Body = finalBody;

                            smtpServer.Send(mail);
                            _logger.LogInformation("[SUCCESS] Mail envoyé avec succès à : {Email}", lead.EMAIL);

                            int delay = new Random().Next(10000, 22000);
                            _logger.LogInformation("[*] Pause anti-block de {Delay}ms pour préserver le quota...", delay);
                            Thread.Sleep(delay);
                        }
                        catch (Exception ex)
                        {
                            var smtpException = new SmtpSendingException("Échec de la transmission SMTP lors de l'envoi du message.", lead.EMAIL, ex);
                            _logger.LogError(ex, "[ERROR] Échec critique d'envoi pour {Email}. Cause : {Message}", smtpException.TargetEmail, ex.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[-] {Email} a été rejeté (Bounce détecté par l'analyseur Python).", lead.EMAIL);
                    }
                }
            }
        }
    }
}