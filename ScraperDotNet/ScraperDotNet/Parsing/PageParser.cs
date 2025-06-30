using HtmlAgilityPack;
using ScraperDotNet.Models;
using System.Text;
using ScraperDotNet.Db;
using ScraperDotNet.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScraperDotNet.Ai;

namespace ScraperDotNet.Parsing
{
    public interface IPageParser
    {
        Task<IEnumerable<Address>> ExtractAndCreateAddresses(Page page, bool ignoreQueryString = true, string? contentGroup = null, bool sameDomainOnly = false);
        string? GetBodyHtml(string htmlContent);
        IEnumerable<LinkModel> GetLinks(string? content);
        Task PopulateFreshAddressesFromPage(long pageId, CancellationToken cancellationToken = default);
        Task PopulateFreshAddressesFromPageForDomain(long pageId, CancellationToken cancellationToken = default);
        Task ProcessPagesInChunksAsync(CancellationToken cancellationToken = default);
    }

    public class PageParser : IPageParser
    {
        private readonly IAddressService _addressService;
        private readonly ScraperContext _context;
        private readonly ILogger<PageParser> _logger;
        private readonly IAiClient _aiClient;
        private readonly HashSet<string> _existingUrls = new HashSet<string>();

        public PageParser(IAddressService addressService, ScraperContext context, ILogger<PageParser> logger, IAiClient aiClient)
        {
            _addressService = addressService;
            _context = context;
            _logger = logger;
            _aiClient = aiClient;
        }

        public string? GetBodyHtml(string htmlContent)
        {
            if (!string.IsNullOrWhiteSpace(htmlContent))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);
                return htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerHtml;
            }

            return null;
        }

        public string? GetBodyText(string htmlContent)
        {
            if (!string.IsNullOrWhiteSpace(htmlContent))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);
                return htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerText;
            }

            return null;
        }

        public IEnumerable<LinkModel> GetLinks(string? content)
        {
            var result = new List<LinkModel>();
            if (content != null)
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);
                var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a");
                if (linkNodes != null)
                {
                    foreach (var linkNode in linkNodes)
                    {
                        if (linkNode.Attributes.Contains("href"))
                        {
                            var url = linkNode.Attributes["href"].Value;
                            var text = ConvertToPlainText(linkNode);
                            result.Add(new LinkModel { Href = url, Text = text });
                        }
                    }
                }
            }
            return result;
        }

        private static string ConvertToPlainText(HtmlNode node)
        {
            StringBuilder result = new StringBuilder();
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                if (subnode.NodeType == HtmlNodeType.Text)
                {
                    result.Append(subnode.InnerText);
                }
                else if (subnode.NodeType == HtmlNodeType.Element)
                {
                    result.Append(ConvertToPlainText(subnode));
                }
            }
            return result.ToString();
        }

        public async Task<IEnumerable<Address>> ExtractAndCreateAddresses(Page page, bool ignoreQueryString = true, string? contentGroup = null, bool sameDomainOnly = false)
        {
            var newAddresses = new List<Address>();
            if (page == null || string.IsNullOrWhiteSpace(page.Content))
            {
                return newAddresses; // No content to parse
            }

            var address = await _context.Addresses.FindAsync(page.AddressId);

            var bodyText = GetBodyText(page.Content);
            if (bodyText == null) return newAddresses;

//            var prompt = @"Classify the following text, which was copied from a webpage. Respond with ONLY ONE WORD representing the most
//likely page type from these options: Error, ForSale, Login, CAPTCHA, Content. If unsure, respond with 'Content'.";
//            var aiResponse = await _aiClient.GetResponseAsync($"{prompt}\n\n{bodyText}");
//            switch (aiResponse.ToLower())
//            {
//                case "error":
//                    _logger.LogWarning("Page {PageId} classified as error", page.Id);
//                    //_context.Attach(page.Address);
//                    address.Status = AddressStatus.ErrorOnPage;
//                    if (!string.IsNullOrWhiteSpace(address.Comment) && !address.Comment.EndsWith(' '))
//                    {
//                        address.Comment += "; ";
//                    }
//                    address.Comment += "Page classified as error by AI; ";
//                    await _context.SaveChangesAsync();
//                    return newAddresses;
//                case "forsale":
//                    _logger.LogWarning("Page {PageId} classified as ForSale", page.Id);
//                    //_context.Attach(address);
//                    address.Status = AddressStatus.ErrorOnPage;
//                    if (!string.IsNullOrWhiteSpace(address.Comment) && !address.Comment.EndsWith(' '))
//                    {
//                        address.Comment += "; ";
//                    }
//                    address.Comment += "Page classified as ForSale by AI; ";
//                    await _context.SaveChangesAsync();
//                    return newAddresses;
//                case "login":
//                    _logger.LogWarning("Page {PageId} classified as login; ", page.Id);
//                    //_context.Attach(address);
//                    address.Status = AddressStatus.RequiresUserAction;
//                    if (!string.IsNullOrWhiteSpace(address.Comment) && !address.Comment.EndsWith(' '))
//                    {
//                        address.Comment += "; ";
//                    }
//                    address.Comment += "Page classified as login by AI; ";
//                    await _context.SaveChangesAsync();
//                    return newAddresses;
//                case "captcha":
//                    _logger.LogWarning("Page {PageId} classified as captcha", page.Id);
//                    //_context.Attach(address);
//                    address.Status = AddressStatus.RequiresUserAction;
//                    if (!string.IsNullOrWhiteSpace(address.Comment) && !address.Comment.EndsWith(' '))
//                    {
//                        address.Comment += "; ";
//                    }
//                    address.Comment += "Page classified as captcha by AI; ";
//                    await _context.SaveChangesAsync();
//                    return newAddresses;
//            }

//            if (aiResponse.ToLower() != "content")
//            {
//                _logger.LogWarning("The AI failed to classify the page {PageId}. It responded with: {Classification}", page.Id, aiResponse);
//                _context.Attach(address);
//                address.Comment = $"Page classified as '{aiResponse}' by AI";
//                await _context.SaveChangesAsync();
//            }

            var bodyHtml = GetBodyHtml(page.Content);
            var links = GetLinks(bodyHtml);
            var baseUri = _addressService.GetUriWithoutQueryString(address);
            var baseUriWithoutPath = _addressService.GetUriWithoutQueryString(address);

            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link.Href)) continue;

                Uri? absoluteUri;
                if (Uri.TryCreate(link.Href, UriKind.RelativeOrAbsolute, out var parsedUri))
                {
                    if (parsedUri.IsAbsoluteUri)
                    {
                        absoluteUri = parsedUri;
                    }
                    else if (link.Href.StartsWith("/"))
                    {
                        // Root-relative path - combine with domain only
                        absoluteUri = new Uri(baseUriWithoutPath, link.Href);
                    }
                    else
                    {
                        // Document-relative path - combine with full current URL
                        absoluteUri = new Uri(baseUri, link.Href);
                    }
                }
                else
                {
                    continue; // Skip invalid URLs
                }

                if (sameDomainOnly && absoluteUri.Host != baseUri.Host)
                {
                    continue; // Skip links that are not in the same domain
                }

                var absoluteUriString = absoluteUri.AbsoluteUri;
                if (!_existingUrls.Contains(absoluteUriString))
                {
                    var (newAddress, created) = _addressService.GetOrCreate(
                        absoluteUriString,
                        ignoreQueryString,
                        $"Found on page {page.Id}, titled: {link.Text?.Substring(0, Math.Min(link.Text?.Length ?? 0, 100))}",
                        contentGroup);

                    if (created)
                    {
                        newAddresses.Add(newAddress);
                    }

                    _existingUrls.Add(absoluteUriString);
                }
            }

            return newAddresses;
        }

        public async Task PopulateFreshAddressesFromPage(long pageId, CancellationToken cancellationToken = default)
        {
            var page = await _context.Pages.Include(p => p.Address).FirstOrDefaultAsync(p => p.Id == pageId);
            if (page == null)
            {
                _logger.LogWarning("Page with ID {PageId} not found", pageId);
                return;
            }

            var groupName = _addressService.GetAddressGroupName(page.Address);
            var newAddresses = await ExtractAndCreateAddresses(page, false, groupName);
            if (newAddresses.Any() && page.Address.ContentGroup == null)
            {
                page.Address.ContentGroup = groupName;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task PopulateFreshAddressesFromPageForDomain(long pageId, CancellationToken cancellationToken = default)
        {
            var page = await _context.Pages.Include(p => p.Address).FirstOrDefaultAsync(p => p.Id == pageId);
            if (page == null)
            {
                _logger.LogWarning("Page with ID {PageId} not found", pageId);
                return;
            }

            var groupName = _addressService.GetAddressGroupName(page.Address);
            var newAddresses = await ExtractAndCreateAddresses(page, false, groupName, true);
            if (newAddresses.Any() && page.Address.ContentGroup == null)
            {
                page.Address.ContentGroup = groupName;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ProcessPagesInChunksAsync(CancellationToken cancellationToken = default)
        {
            long lastProcessedId = 0;
            int processedPagesCount = 0;
            const int chunkSize = 100;

            try
            {
                while (true)
                {
                    var pagesToProcess = await _context.Pages
                        .Where(p => p.Id > lastProcessedId)
                        .OrderBy(p => p.Id)
                        .Take(chunkSize)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    if (!pagesToProcess.Any())
                    {
                        _logger.LogInformation("No more pages to process. Total processed: {Count}", processedPagesCount);
                        break;
                    }

                    foreach (var page in pagesToProcess)
                    {
                        var address = await _context.Addresses.FindAsync(page.AddressId);
                        var groupName = address.ContentGroup ?? address.Domain.Replace('.', '_').Replace("www.", string.Empty, StringComparison.InvariantCultureIgnoreCase);
                        try
                        {
                            var newAddresses = await ExtractAndCreateAddresses(page, false, groupName);
                            if (newAddresses.Any())
                            {
                                if (address.ContentGroup == null)
                                {
                                    address.ContentGroup = groupName;
                                }
                                await _context.SaveChangesAsync(cancellationToken);
                            }
                            processedPagesCount++;

                            _logger.LogInformation(
                                "Processed page {PageId} for address {AddressId}. Found {NewAddressCount} new addresses",
                                page.Id, page.AddressId, newAddresses.Count());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "Error processing page {PageId} for address {AddressId}", 
                                page.Id, page.AddressId);
                        }
                    }

                    lastProcessedId = pagesToProcess.Max(p => p.Id);
                    _logger.LogInformation("Chunk processed. Last processed page ID: {LastId}", lastProcessedId);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Processing cancelled. Last processed ID: {LastId}", lastProcessedId);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during page processing. Last processed ID: {LastId}", lastProcessedId);
                throw;
            }
        }
    }
}
