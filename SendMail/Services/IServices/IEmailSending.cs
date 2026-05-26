using SendMail.Dtos;

namespace SendMail.Services.IServices
{
    public interface IEmailSending
    {
        public void ProcessAndSend(SendRequestDto sendRequestDto);
    }
}
