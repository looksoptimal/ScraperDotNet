using ScraperDotNet.Models;

public interface IFtpDownloader
{
    Task<AddressOpeningResult> DownloadFromFtpAsync(FtpDownloadModel model, CancellationToken cancellationToken);
}