namespace ScrapperDotNet.Models
{
    public class FtpDownloadModel
    {
        public string FtpUrl { get; set; }
        public string WritingDirectory { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
