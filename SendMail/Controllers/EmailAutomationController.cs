using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailAutomationController(
            IPdfSerializer pdfParser,
            IEmailSending emailSending,
            IWebHostEnvironment env,
            ILogger<EmailAutomationController> logger,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _pdfParser = pdfParser;
            _emailSending = emailSending;
            _env = env;
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult OAuth2Authorize(string email)
        {
            var clientId = _config["Google:ClientId"];
            var redirectUri = _config["Google:RedirectUri"];

            var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString("https://mail.google.com/ profile email")}" +
                      $"&access_type=offline" +
                      $"&prompt=consent" +
                      $"&login_hint={Uri.EscapeDataString(email ?? "")}" +
                      $"&state={Uri.EscapeDataString(email ?? "")}";

            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> OAuth2Callback(string code, string state, string error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                var errPayload = JsonSerializer.Serialize(new { type = "oauth2_error", message = error });
                return Content($"<script>window.opener?.postMessage({errPayload}, window.location.origin); window.close();</script>", "text/html");
            }

            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            var http = _httpClientFactory.CreateClient();
            var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }));

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = doc.RootElement.TryGetProperty("error_description", out var ed) ? ed.GetString() : "Token exchange failed";
                var errPayload = JsonSerializer.Serialize(new { type = "oauth2_error", message = errMsg });
                return Content($"<script>localStorage.setItem('oauth2_result', JSON.stringify({errPayload})); window.close();</script>", "text/html");
            }

            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var email = state;

            var userInfoResponse = await http.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo")
            {
                Headers = { { "Authorization", $"Bearer {accessToken}" } }
            });
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            using var userInfoDoc = JsonDocument.Parse(userInfoJson);
            var senderName = userInfoDoc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : email;

            _logger.LogInformation("[OAUTH2] Token obtenu pour {Email} ({Name})", email, senderName);

            var payload = JsonSerializer.Serialize(new { type = "oauth2_success", email, token = accessToken, name = senderName });
            return Content($"<script>localStorage.setItem('oauth2_result', JSON.stringify({payload})); window.close();</script>", "text/html");
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

            if (string.IsNullOrEmpty(payload.AccessToken))
                return BadRequest(new { message = "Token OAuth2 manquant. Veuillez vous authentifier avec Google." });

            var leads = JsonSerializer.Deserialize<List<PdfFormat>>(payload.SelectedLeadsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            string savedAttachmentPath = null;
            if (payload.AttachedFile != null && payload.AttachedFile.Length > 0)
            {
                string tempFolder = Path.Combine(_env.ContentRootPath, "TempAttachments");
                Directory.CreateDirectory(tempFolder);
                savedAttachmentPath = Path.Combine(tempFolder, payload.AttachedFile.FileName);
                using (var stream = new FileStream(savedAttachmentPath, FileMode.Create))
                    payload.AttachedFile.CopyTo(stream);
            }

            var sendRequest = new SendRequestDto
            {
                SenderEmail = payload.SenderEmail,
                SenderName = payload.SenderName,
                AccessToken = payload.AccessToken,
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
