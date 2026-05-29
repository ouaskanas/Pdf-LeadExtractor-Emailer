using SendMail.Dtos;

namespace SendMail.Services.IServices
{
    public interface IEmailSending
    {
        public int ProcessAndSend(SendRequestDto sendRequestDto);
    }
}
