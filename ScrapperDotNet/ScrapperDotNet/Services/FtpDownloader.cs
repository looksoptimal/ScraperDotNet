using FluentFTP;
using FluentFTP.Exceptions;
using ScrapperDotNet.Models;

public class FtpDownloader : IFtpDownloader
{
    public async Task<AddressOpeningResult> DownloadFromFtpAsync(FtpDownloadModel model, CancellationToken cancellationToken)
    {
        var result = new AddressOpeningResult { OriginalUrl = model.FtpUrl };
        AsyncFtpClient? client = null;
        var localFilePath = Path.Combine(model.WritingDirectory, Path.GetFileName(model.FtpUrl));

        try
        {
            var uri = new Uri(model.FtpUrl);
            if (!string.IsNullOrEmpty(model.Username) && !string.IsNullOrEmpty(model.Password))
            {
                client = new AsyncFtpClient(uri.Host, model.Username, model.Password);
            }
            else
            {
                client = new AsyncFtpClient(uri.Host);
            }
            var connectionProfile = await client.AutoConnect();

            using var downloadFileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var downloadSuccessful = await client.DownloadStream(downloadFileStream, uri.AbsolutePath, token: cancellationToken);

            if (downloadSuccessful)
            {
                result.AddressStatus = AddressOpeningStatus.Ok;
                result.FinalUrl = model.FtpUrl;
            }
            else
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = "Download failed or file not found.";
            }
        }
        catch (FtpMissingObjectException ex)
        {
            result.AddressStatus = AddressOpeningStatus.FailedToLoad;
            result.ErrorMessage = $"the file under '{model.FtpUrl}' does not exist on the server";
        }
        catch (Exception ex)
        {
            result.AddressStatus = AddressOpeningStatus.FailedToLoad;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            client?.Dispose();
        }

        return result;
    }
}
