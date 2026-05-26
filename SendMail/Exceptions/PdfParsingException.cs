using System;

namespace SendMail.Exceptions
{
    public class PdfParsingException : Exception
    {
        public string PdfPath { get; private set; }

        public PdfParsingException(string message, string pdfPath, Exception innerException)
            : base(message, innerException)
        {
            PdfPath = pdfPath;
        }
    }
}