namespace SendMail.Dtos
{
    public class SendCampaignPayload
    {
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string AccessToken { get; set; }
        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }
        public string SelectedLeadsJson { get; set; }
        public IFormFile AttachedFile { get; set; }
    }
}