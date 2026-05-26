using SendMail.Models;

namespace SendMail.Services.IServices
{
    public interface IPdfSerializer
    {
        public List<PdfFormat> ProcessPdfAndSend(string pdfPath);
    }
}
