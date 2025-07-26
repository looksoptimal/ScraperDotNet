using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScraperDotNet.Ai;
using ScraperDotNet.Browser;
using ScraperDotNet.Db;
using ScraperDotNet.Models;
using ScraperDotNet.Parsing;
using System.Data;
using System.Threading.Tasks;

namespace ScraperDotNet.Services
{
    public class DownloadService(ILogger<DownloadService> logger, ScraperContext context, IBrowserRunnerAsync browser, AppSettings settings, IAddressService addressService, IAiClient aiClient, IPageParser pageParser, IFileService fileService, IFtpDownloader ftpDownloader) : IDownloadService
    {
        private const string PageInsertCommand = "EXEC InsertPage  @addressId, @content, @contentType, @pageId OUTPUT";

        private ScraperContext _context = context;
        private readonly IBrowserRunnerAsync _browser = browser;
        private readonly AppSettings _settings = settings;
        private readonly IAddressService _addressService = addressService;
        private readonly IAiClient _aiClient = aiClient;
        private readonly IPageParser _pageParser = pageParser;
        private readonly IFtpDownloader _ftpDownloader = ftpDownloader;
        private readonly string _pageSaveLocation = settings.PageSaveLocation;

        public async Task DownloadDomain(string startUrl)
        {
            var groupName = _addressService.CreateDomainBasedGroupName(startUrl);
            var (startAddress, created) = _addressService.GetOrCreate(startUrl, false, "entered for domain download", groupName);
            if (!created)
            {
                if (string.IsNullOrEmpty(startAddress.ContentGroup))
                {
                    await _addressService.SetAddressGroupName(startAddress, groupName);
                }
                else
                {
                    groupName = startAddress.ContentGroup;
                }
            }

            var uri = new Uri(startUrl.ToLower());
            if (!string.IsNullOrEmpty(uri.PathAndQuery.TrimStart('/')))
            {
                var domainBaseAddress = uri.Authority;
                if (domainBaseAddress.ToLower() != startUrl.ToLower().TrimEnd('/'))
                {
                    var (domainAddress, _) = _addressService.GetOrCreate(domainBaseAddress, false, $"entered as domain of {startUrl}", groupName);
                }
            }

            long? id = startAddress.Id;
            while (id != null)
            {
                logger.LogDebug($"Downloading address {id} for domain {uri.Host}");
                var pageId = await DownloadPageByAddressId(id.Value);
                logger.LogDebug($"Populating fresh domain addresses for page {pageId} from address {id}");
                await _pageParser.PopulateFreshAddressesFromPageForDomain(pageId.Value);
                id = _context.Addresses.FirstOrDefault(x => x.Status == AddressStatus.Fresh && x.Domain.ToLower() == uri.Host)?.Id;
            }

            logger.LogDebug($"No more fresh addresses for domain {uri.Host} found");
        }

        public async Task<long?> DownloadPageByAddressId(long id)
        {
            logger.LogDebug($"Loading address {id}");
            var address = await LoadAddress(id);
            if (address == null)
            {
                logger.LogDebug($"Address {id} will not be saved");
                return null;
            }

            await DelayService.WaitASecAsync();
            await _browser.ScrollToBottom();
            logger.LogDebug($"Saving address {id}");
            long pageId = await SavePageSource(id);
            logger.LogDebug($"Marking address {id} as visited");
            MarkAddressAsVisited(address);
            logger.LogDebug($"Cycle for address {id} finished");

            return pageId;
        }

        public async Task<IEnumerable<long>> DownloadPagesForFreshAdresses()
        {
            var result = new List<long>();
            var id = _context.Addresses.FirstOrDefault(x => x.Status == AddressStatus.Fresh)?.Id;
            while (id != null)
            {
                logger.LogDebug($"Found fresh address {id}");
                var pageId = await DownloadPageByAddressId(id.Value);
                if (pageId.HasValue)
                {
                    result.Add(pageId.Value);
                    await _pageParser.PopulateFreshAddressesFromPage(pageId.Value);
                }
                id = _context.Addresses.FirstOrDefault(x => x.Status == AddressStatus.Fresh)?.Id;
            }
            logger.LogDebug("No more fresh addresses found");
            return result;
        }

        private async Task<long> SavePageSource(long addressId)
        {
            var pageIdParam = new SqlParameter("@pageId", SqlDbType.BigInt)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            var content = await _browser.PageContent();
            var sqlParams = new SqlParameter[] {
                new SqlParameter("@content", content),
                new SqlParameter("@addressId", addressId),
                new SqlParameter("@contentType", "Html"),
                pageIdParam
            };
            var result = _context.Database.ExecuteSqlRaw(PageInsertCommand, sqlParams);
            var pageId = (long)(pageIdParam.Value);
            return pageId;
        }

        public async Task<string> SavePagePdf(string fileName)
        {
            string path = GetFilePath(fileName);
            await _browser.SavePagePdf(path);
            return path;
        }

        public async Task<string> SaveScreenshot(string fileName)
        {
            string path = GetFilePath(fileName);
            await _browser.SaveScreenshot(path);
            return path;
        }

        public async Task<string> SavePageImage(string fileName)
        {
            string path = GetFilePath(fileName);
            await _browser.SavePageImage(path);
            return path;
        }

        private string GetFilePath(string fileName)
        {
            if (!Directory.Exists(_pageSaveLocation))
            {
                Directory.CreateDirectory(_pageSaveLocation);
            }
            var path = Path.Combine(_pageSaveLocation, fileName);
            return path;
        }

        private async Task<Address?> LoadAddress(long id)
        {
            var address = _context.Addresses.Find(id);
            if (address == null)
            {
                throw new InvalidOperationException($"No address for Id={id}");
            }

            var url = _addressService.GetUriString(address);
            var openingResult = await GetContentFromAddress(address);

            var noIssues = ProcessOpeningResultIssues(address, openingResult);
            if (!noIssues)
            {
                return null;
            }

            openingResult = await ProcessRedirection(address, openingResult);
            if (openingResult == null)
            {
                return null;
            }

            var isDownloadableContent = await ProcessDownloadableContent(address, openingResult);
            if (isDownloadableContent)
            {
                return null;
            }

            // use ai to check if this is a fine page
            // take a screenshot
            var screenshotPath = fileService.GetScreenshotPath(address);
            var screenshot = await _browser.SaveScreenshot(screenshotPath);
            var imagePath = fileService.GetWholePageImagePath(address);
            var wholePageImage = await _browser.SavePageImage(imagePath);
            var pdfPath = fileService.GetPdfPath(address);
            var pagePdf = await _browser.SavePagePdf(pdfPath);

            if (_settings.AiEnabled)
            {
                var classificationOfPageFromAi = await GetContentClassFromAi(screenshotPath);
                var isPageOk = UpdateAddressAndCheckIfPageIsOk(address, classificationOfPageFromAi);
                if (!isPageOk)
                {
                    return null;
                }
            }

            var (newAddress, created) = _addressService.GetOrCreate(openingResult.FinalUrl, true, $"redirected from {url}");
            if (created)
            {
                ChangeAddressStatus(address, AddressStatus.Duplicate, $"redirects to {openingResult.FinalUrl}");
                address = newAddress;
            }

            string? comment = openingResult.AddressStatus == AddressOpeningStatus.Ok ? null : $"AddressOpeningStatus: {openingResult.AddressStatus.ToString()}";
            MarkAddressAsOpening(address, comment);

            return address;
        }

        private async Task<string?> GetContentClassFromAi(string? screenshotPath)
        {
            const string promptForAccessAssessment = "If this page shows an error then answer 'error' without any other words. If this page a login page, a capcha or some other mechanism blocking a user from accessing the content which is not a cookie consent then answer with just one word: 'blocked'. Otherwise answer 'ok'. Always use only 1 word in your answer.";
            var response = await _aiClient.AskAboutImageAsync(promptForAccessAssessment, screenshotPath);
            return response.ToLower().Trim();
        }

        private bool UpdateAddressAndCheckIfPageIsOk(Address address, string? aiResponse)
        {
            switch (aiResponse)
            {
                case "error":
                    ChangeAddressStatus(address, AddressStatus.ErrorOnPage, "The page shows an error");
                    return false;
                case "blocked":
                    {

                        if (!_settings.WaitForUserActionOnBlockedPages)
                        {
                            ChangeAddressStatus(address, AddressStatus.RequiresUserAction, "The page is blocked by a login or captcha");
                            return false;
                        }
                        Console.WriteLine("It looks like the page requires to log in.");
                        var userWantsToContinue = DelayService.WaitForUserAction();
                        if (!userWantsToContinue)
                        {
                            ChangeAddressStatus(address, AddressStatus.RequiresUserAction, "The page is blocked by a login or captcha");
                            return false;
                        }
                        return true;
                    }
                case "ok":
                    return true;
                default:
                    logger.LogWarning($"Unexpected response from AI for address '{address.Id}': {aiResponse}");
                    AddAddressComment(address, $"Unexpected AI response: {aiResponse}");
                    return true;
            }
        }

        private async Task<bool> ProcessDownloadableContent(Address address, AddressOpeningResult openingResult)
        {
            if (openingResult.AddressStatus == AddressOpeningStatus.DownloadableContent || openingResult.AddressStatus == AddressOpeningStatus.PageWithAttachment)
            {
                var (filePath, contentType) = await fileService.SaveDownloadableContent(address, openingResult);
                if (filePath != null && contentType != null)
                {
                    var pageForContent = new Page
                    {
                        AddressId = address.Id,
                        ContentPath = filePath,
                        ContentType = contentType.Value
                    };

                    AddAddressComment(address, $"Content for address {address.Id} saved as a file: {filePath}.");
                }

                MarkAddressAsVisited(address);
                return true;
            }

            return false;
        }

        private async Task<AddressOpeningResult?> ProcessRedirection(Address address, AddressOpeningResult initialOpeningResult)
        {
            var originalUrl = initialOpeningResult.OriginalUrl;
            var finalUrl = initialOpeningResult.FinalUrl;
            if (!_addressService.AreUrisEqual(initialOpeningResult.OriginalUrl, finalUrl, true))
            {
                if (!_browser.IsOriginalWindowShown)
                {
                    if (!_settings.WaitForUserActionOnBlockedPages)
                    {
                        ChangeAddressStatus(address, AddressStatus.RequiresUserAction, $"It seems a new window/tab has been opened");
                        return null;
                    }
                    logger.LogInformation($"It seems that from the address {address.Id} ({originalUrl}) a new window/tab has been opened. Please check the browser and set the right window.");
                    var userWantsToContinue = DelayService.WaitForUserAction();
                    if (!userWantsToContinue)
                    {
                        ChangeAddressStatus(address, AddressStatus.Unsupported, $"It seems a new window/tab has been opened and the user didn't want to continue");
                        return null;
                    }

                    // assume the user performed necessery actions in the new window so reload the original url
                    return await _browser.OpenPageAndWaitUntilItLoads(originalUrl);
                }
                else if (finalUrl.Contains("signin") || finalUrl.Contains("auth") || finalUrl.Contains("login"))
                {
                    if (!_settings.WaitForUserActionOnBlockedPages)
                    {
                        ChangeAddressStatus(address, AddressStatus.RequiresUserAction, $"The page requires to sign in");
                        return null;
                    }
                    logger.LogInformation($"It looks like the address {address.Id} ({originalUrl}) requires to log in.");
                    var userWantsToContinue = DelayService.WaitForUserAction();
                    if (!userWantsToContinue)
                    {
                        ChangeAddressStatus(address, AddressStatus.RequiresUserAction, $"The page requires to sign in");
                        return null;
                    }

                    // assume the user has logged in so reload the original url
                    return await _browser.OpenPageAndWaitUntilItLoads(originalUrl);
                }
            }

            return initialOpeningResult;
        }

        private bool ProcessOpeningResultIssues(Address address, AddressOpeningResult? openingResult)
        {
            var url = _addressService.GetUriString(address);

            if (openingResult == null)
            {
                ChangeAddressStatus(address, AddressStatus.Unsupported, null);
                logger.LogInformation($"Downloading for Address {address.Id} ({url}) is not supported");
                return false;
            }
            if (openingResult.ErrorMessage != null)
            {
                logger.LogError(openingResult.ErrorMessage);
                var statusToSet = openingResult.AddressStatus == AddressOpeningStatus.CantConnect ?
                    AddressStatus.FailedToOpen : AddressStatus.ErrorOnPage;
                ChangeAddressStatus(address, statusToSet, openingResult.ErrorMessage);

                if (address.Scheme.ToLower() == "http")
                {
                    var httpsUrl = url.Replace("http://", "https://");
                    _addressService.GetOrCreate(httpsUrl, false, "Added by changing http to https");
                }

                return false;
            }
            else if (openingResult.UserActionNeeded != null)
            {
                logger.LogError(openingResult.UserActionNeeded);
                ChangeAddressStatus(address, AddressStatus.RequiresUserAction, openingResult.UserActionNeeded);

                return false;
            }
            else if (openingResult.AddressStatus == AddressOpeningStatus.UnsupportedScheme)
            {
                ChangeAddressStatus(address, AddressStatus.Unsupported, $"Unsupported URI scheme: {address.Scheme}");

                return false;
            }

            return true;
        }

        private async Task<AddressOpeningResult?> GetContentFromAddress(Address address)
        {
            AddressOpeningResult openingResult;
            var url = _addressService.GetUriString(address);
            switch (address.Scheme.ToLower())
            {
                case "http":
                case "https":
                    {
                        openingResult = await _browser.OpenPageAndWaitUntilItLoads(url);
                    }
                    break;
                case "ftp":
                case "ftps":
                    {
                        var ftpModel = new FtpDownloadModel
                        {
                            FtpUrl = url,
                            WritingDirectory = fileService.GetOrCreatePageSaveDirectory(address)
                        };
                        openingResult = await _ftpDownloader.DownloadFromFtpAsync(ftpModel, CancellationToken.None);
                    }
                    break;
                default: return null;
            }

            return openingResult;
        }

        private void MarkAddressAsOpening(Address address, string? comment)
        {
            ChangeAddressStatus(address, AddressStatus.Opening, null);
        }

        private void MarkAddressAsVisited(Address address)
        {
            ChangeAddressStatus(address, AddressStatus.Visited, null);
        }

        private void ChangeAddressStatus(Address address, AddressStatus statusToSet, string? comment)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            address.Status = statusToSet;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                address.Comment = string.IsNullOrWhiteSpace(address.Comment) ? comment : $"{address.Comment}; {comment}; ";
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, $"Error updating address {address.Id} status to {statusToSet}");
                throw;
            }
        }

        private void AddAddressComment(Address address, string comment)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (!string.IsNullOrWhiteSpace(comment))
            {
                address.Comment = string.IsNullOrWhiteSpace(address.Comment) ? comment : $"{address.Comment}; {comment}; ";
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, $"Error updating address {address.Id} comment to '{comment}'");
                    throw;
                }
            }
        }
    }
}
