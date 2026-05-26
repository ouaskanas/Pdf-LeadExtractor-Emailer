namespace SendMail.Services.IServices
{
    public interface IEmailValidator
    {
        public bool IsEmailValid(string email);
    }
}
