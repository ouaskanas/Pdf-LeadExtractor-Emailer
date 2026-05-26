using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using SendMail.Services.IServices;

namespace SendMail.Services.Impl
{
    public class EmailValidator : IEmailValidator
    {
        private readonly ILogger<EmailValidator> _logger;

        public EmailValidator(ILogger<EmailValidator> logger)
        {
            _logger = logger;
        }

        public bool IsEmailValid(string email)
        {
            ProcessStartInfo startScript = new ProcessStartInfo();
            startScript.FileName = "python";

            startScript.Arguments = $"\"C:\\Users\\anaso\\source\\repos\\SendMail\\Script\\AntiBoucing.py\" \"{email}\"";

            startScript.UseShellExecute = false;
            startScript.RedirectStandardOutput = true;
            startScript.RedirectStandardError = true;
            startScript.CreateNoWindow = true;

            try
            {
                using (Process process = Process.Start(startScript))
                {
                    if (process == null) return false;

                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd().Trim();
                        if (bool.TryParse(result, out bool isValid))
                        {
                            return isValid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Impossible de lancer le script de validation pour {Email}. Vérifie l'installation de Python et le chemin d'accès.", email);
            }

            return false;
        }
    }
}