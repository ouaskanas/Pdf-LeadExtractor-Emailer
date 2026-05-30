using SendMail.Models;

namespace SendMail.Dtos
{
    public class SendRequestDto
    {
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string AccessToken { get; set; }
        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }
        public List<PdfFormat> SelectedLeads { get; set; }
        public string AttachmentPath { get; set; }
    }
}
