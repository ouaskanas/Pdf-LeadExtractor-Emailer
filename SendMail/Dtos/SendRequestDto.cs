using SendMail.Models;

namespace SendMail.Dtos
{
    public class SendRequestDto
    {
        public string SenderEmail { get; set; }
        public string AppPassword { get; set; }

        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }

        public List<PdfFormat> SelectedLeads { get; set; }
    }
}
