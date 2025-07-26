using Microsoft.Extensions.Logging;
using ScraperDotNet.Db;
using ScraperDotNet.Models;

namespace ScraperDotNet.Services
{
    public interface IFileService
    {
        string GetOrCreatePageSaveDirectory(Address address);
        string? GetPdfPath(Address address);
        string? GetScreenshotPath(Address address);
        string? GetWholePageImagePath(Address address);
        Task<(string? filePath, ContentType? contentType)> SaveDownloadableContent(Address address, AddressOpeningResult openPageResult);
    }

    public class FileService(AppSettings settings, ILogger<FileService> logger) : IFileService
    {
        public string GetOrCreatePageSaveDirectory(Address address)
        {
            var contentFolder = string.IsNullOrWhiteSpace(address.ContentGroup) ? $"{settings.PageSaveLocation}/{address.Id}" : $"{settings.PageSaveLocation}/{address.ContentGroup}";
            if (!Directory.Exists(contentFolder))
            {
                Directory.CreateDirectory(contentFolder);
            }

            return contentFolder;
        }

        public async Task<(string? filePath, ContentType? contentType)> SaveDownloadableContent(Address address, AddressOpeningResult openResult)
        {
            ContentType? contentType = null;
            if (openResult.AddressStatus == Models.AddressOpeningStatus.DownloadableContent || openResult.AddressStatus == Models.AddressOpeningStatus.PageWithAttachment)
            {
                var contentFolder = GetOrCreatePageSaveDirectory(address);
                logger.LogInformation($"Downloading contents from address {address.Id} into {contentFolder}");
                var filePath = Path.Combine(contentFolder, openResult.ContentName ?? "downloaded_content");

                if (openResult.TextContent != null)
                {
                    await File.WriteAllTextAsync(filePath, openResult.TextContent);
                    contentType = filePath.ToLower().EndsWith(".html") ? ContentType.Html : ContentType.Text;
                }
                else if (openResult.BinaryContent != null)
                {
                    await File.WriteAllBytesAsync(filePath, openResult.BinaryContent);
                    if (filePath.ToLower().EndsWith(".pdf"))
                    {
                        contentType = ContentType.Pdf;
                    }
                    else if (filePath.ToLower().EndsWith(".png") || filePath.ToLower().EndsWith(".jpg") || filePath.ToLower().EndsWith(".jpeg") || filePath.ToLower().EndsWith(".svg") || filePath.ToLower().EndsWith(".gif"))
                    {
                        contentType = ContentType.Image;
                    }
                }
                else if (openResult.DownloadStream != null)
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await openResult.DownloadStream.CopyToAsync(fileStream);
                    }
                    openResult.DownloadStream.Dispose();

                    contentType = filePath.ToLower().EndsWith(".html") ? ContentType.Html : ContentType.Text;
                    if (filePath.ToLower().EndsWith(".pdf"))
                    {
                        contentType = ContentType.Pdf;
                    }
                    else if (filePath.ToLower().EndsWith(".html") || filePath.ToLower().EndsWith(".htm"))
                    {
                        contentType = ContentType.Html;
                    }
                    else if (filePath.ToLower().EndsWith(".png") || filePath.ToLower().EndsWith(".jpg") || filePath.ToLower().EndsWith(".jpeg") || filePath.ToLower().EndsWith(".svg") || filePath.ToLower().EndsWith(".gif"))
                    {
                        contentType = ContentType.Image;
                    }
                    else if (filePath.ToLower().EndsWith(".txt") || filePath.ToLower().EndsWith(".xml") || filePath.ToLower().EndsWith(".js") || filePath.ToLower().EndsWith(".json"))
                    {
                        contentType = ContentType.Text;
                    }
                    else
                    {
                        contentType = ContentType.Binary;
                    }
                }
                else
                {
                    logger.LogError($"Neither TextContent nor BinaryContent is set for downloadable content for address Id={address.Id} (url: {openResult.FinalUrl})");
                    return (null, null);
                }

                return (filePath, contentType);
            }

            logger.LogError($"The content for address Id={address.Id} is not downloadable (url: {openResult.FinalUrl})");
            return (null, null);
        }

        public string? GetScreenshotPath(Address address)
        {
            var pageFolder = GetOrCreatePageSaveDirectory(address);
            return $"{pageFolder}/{address.Id}-screenshot.png";
        }

        public string? GetWholePageImagePath(Address address)
        {
            var pageFolder = GetOrCreatePageSaveDirectory(address);
            return $"{pageFolder}/{address.Id}-wholePage.png";
        }

        public string? GetPdfPath(Address address)
        {
            var pageFolder = GetOrCreatePageSaveDirectory(address);
            return $"{pageFolder}/{address.Id}-page.pdf";
        }
    }
}
