namespace ScraperDotNet.Services
{
    public interface IDownloadService
    {
        Task DownloadDomain(string startUrl);
        Task<long?> DownloadPageByAddressId(long id);
        Task<IEnumerable<long>> DownloadPagesForFreshAdresses();
        Task<string> SavePageImage(string fileName);
        Task<string> SavePagePdf(string fileName);
        Task<string> SaveScreenshot(string fileName);
    }
}