namespace SendMail.Exceptions
{
    public class SmtpSendingException : Exception
    {
        public string TargetEmail { get; private set; }

        public SmtpSendingException(string message, string targetEmail, Exception innerException)
            : base(message, innerException)
        {
            TargetEmail = targetEmail;
        }
    }
}
