using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SendMail.Services.IServices;

namespace SendMail.Services.Impl
{
    public class EmailValidator : IEmailValidator
    {
        private readonly ILogger<EmailValidator> _logger;
        private readonly IWebHostEnvironment _env;

        public EmailValidator(ILogger<EmailValidator> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public bool IsEmailValid(string email)
        {
            string scriptPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "Scripts", "AntiBoucing.py"));

            if (!File.Exists(scriptPath))
            {
                _logger.LogWarning("[VALIDATOR] Script Python introuvable à {Path}. Validation basique uniquement.", scriptPath);
                return true;
            }

            var startScript = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" \"{email}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startScript))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("[VALIDATOR] Impossible de démarrer Python pour {Email}. Email autorisé par défaut.", email);
                        return true;
                    }

                    string result = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (bool.TryParse(result, out bool isValid))
                        return isValid;

                    _logger.LogWarning("[VALIDATOR] Réponse Python non parseable pour {Email} : '{Result}'. Email autorisé par défaut.", email, result);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VALIDATOR] Erreur lors de la validation Python pour {Email}. Email autorisé par défaut.", email);
                return true;
            }
        }
    }
}