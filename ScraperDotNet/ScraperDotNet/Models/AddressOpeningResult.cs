namespace ScraperDotNet.Models
{
    public class AddressOpeningResult
    {
        public string OriginalUrl { get; set; }
        public string FinalUrl { get; set; }
        public string? UserActionNeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public AddressOpeningStatus? AddressStatus { get; set; }
        public string? ContentName { get; set; }
        public string? TextContent { get; set; }
        public byte[]? BinaryContent { get; set; }
        public Stream? DownloadStream { get; set; }
    }
}
