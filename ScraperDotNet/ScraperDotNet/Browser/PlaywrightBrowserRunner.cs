using Microsoft.Playwright;
using ScraperDotNet.Models;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using HeyRed.Mime;
using System.Text.RegularExpressions;
using System;
using OpenQA.Selenium.BiDi.Communication;

namespace ScraperDotNet.Browser
{
    public class PlaywrightBrowserRunner(ILogger<PlaywrightBrowserRunner> logger, AppSettings appSettings) : IBrowserRunnerAsync, IDisposable, IAsyncDisposable
    {
        private const int scrollStepsLimit = 90;
        private readonly ILogger<PlaywrightBrowserRunner> _logger = logger;
        private readonly AppSettings _appSettings = appSettings;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext _browserContext;
        private IPage? _page;
        private AddressOpeningResult? _downloadOpeningResult = null;

        // Event to notify when an attachment is downloaded
        public event EventHandler<AddressOpeningResult>? DownloadAttachment;

        public Task<string?> PageContent() => _page?.ContentAsync();

        public bool IsOriginalWindowShown => OpenWindowCount == 1;

        public int OpenWindowCount => _browser?.Contexts.Sum(ctx => ctx.Pages.Count) ?? 0;


        public async Task StartBrowser()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _appSettings.HideBrowserUI
            });
            _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                AcceptDownloads = true // Enable handling downloads
            });
            _page = await _browserContext.NewPageAsync();
        }

        public ILocator? FindElementByName(string name)
        {
            return _page.Locator($"[name='{name}']");
        }

        public async Task KeepScrollingDown()
        {
            var bottomReached = false;
            bool canScrollDown;
            for (var i = 0; i < scrollStepsLimit && !bottomReached; i++)
            {
                canScrollDown = await CanScrollDown();
                if (!canScrollDown)
                {
                    await DelayService.WaitAFewSecAsync();
                }
                if (canScrollDown)
                {
                    await ScrollDown(500);
                    if (i % 3 == 0)
                    {
                        await DelayService.WaitASecAsync();
                    }
                    else
                    {
                        await DelayService.WaitSplitASecAsync();
                    }
                }
                else
                {
                    bottomReached = true;
                }
            }

            var logMessage = bottomReached ? "KeepScrollingDown: reached page bottom" : "KeepScrollingDown: reached limit of scrolls";
        }

        public async Task<AddressOpeningResult> OpenPageAndWaitUntilItLoads(string pageUrl)
        {
            var result = new AddressOpeningResult { OriginalUrl = pageUrl };
            IResponse? response = null;
            if (!(pageUrl.StartsWith("http://") || pageUrl.StartsWith("https://")))
            {
                result.AddressStatus = AddressOpeningStatus.UnsupportedScheme;
                return result;
            }
            ValidatePage();

            try
            {
                // Navigate to the URL and wait for network idle
                response = await _page!.GotoAsync(pageUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 30000 // 30 seconds timeout
                });
            }
            catch (TimeoutException ex)
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = $"Page load timed out after 30 seconds: {ex.Message}";
                return result;
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("net::ERR_CONNECTION_REFUSED") ||
                                               ex.Message.Contains("net::ERR_CONNECTION_TIMED_OUT") ||
                                               ex.Message.Contains("net::ERR_NAME_NOT_RESOLVED"))
            {
                result.AddressStatus = AddressOpeningStatus.CantConnect;
                result.ErrorMessage = $"Connection error: {ex.Message}";
                return result;
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("net::ERR_ABORTED"))
            {
                try
                {
                    var downloadTask = _page.WaitForDownloadAsync();
                    await _page.EvaluateAsync(JsScripts.TriggerUrlDownload(pageUrl)); // or ClickAsync if it's a button
                    var download = await downloadTask;
                    var downloadStream = await download.CreateReadStreamAsync();
                    result.AddressStatus = AddressOpeningStatus.PageWithAttachment;
                    result.FinalUrl = download.Url;
                    result.ContentName = download.SuggestedFilename;
                    result.DownloadStream = downloadStream;
                    return result;
                }
                catch
                {
                    result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                    result.ErrorMessage = $"Playwright error - page aborted for url: {pageUrl} (potentially triggered by a download link). Message: {ex.Message}";
                    return result;
                }
            }
            catch (PlaywrightException ex)
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = $"Playwright error: {ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = $"Unexpected error loading page: {ex.Message}";
                return result;
            }

            if (response == null)
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = "No response received from the page";
                return result;
            }

            // Check if the navigation was successful
            if (!response.Ok)
            {
                result.AddressStatus = AddressOpeningStatus.FailedToLoad;
                result.ErrorMessage = $"HTTP {response.Status}: {response.StatusText}";
                return result;
            }

            // wait until network idle or 30s timeout
            // if timeout occurs, set a LoadedWithNetworkActive status
            var taskWhichFinished1st = Task.WaitAny(
                    _page.WaitForLoadStateAsync(LoadState.NetworkIdle),
                    Task.Delay(30000) // Fallback delay to avoid indefinite wait
                );

            result.FinalUrl = _page.Url;

            var contentDisposition = response.Headers.ContainsKey("content-disposition") ? response.Headers["content-disposition"].ToLower() : null;
            var contentType = response.Headers.ContainsKey("content-type") ? response.Headers["content-type"].ToLower() : null;
            if (contentDisposition != null)
            {
                result.AddressStatus = AddressOpeningStatus.DownloadableContent;
                if (contentDisposition.Contains("filename="))
                {
                    result.ContentName = contentDisposition.Split("filename=")[1].Trim('"');
                }
                else if (contentType != null && !(contentType.Contains("text/html") && contentDisposition != "inline"))
                {
                    result.ContentName = GetFileNameFromUrlAndContentType(result.FinalUrl, contentType);
                }
            }
            if (contentType != null)
            {
                if (IsContentTypeSupported(contentType))
                {
                    result.AddressStatus = AddressOpeningStatus.DownloadableContent;
                    result.ContentName = GetFileNameFromUrlAndContentType(result.FinalUrl, contentType);
                }
                else if (!(contentType.Contains("text/html")))
                {
                    result.AddressStatus = AddressOpeningStatus.UnsupportedContentType; // Unknown content type
                    result.ErrorMessage = $"Unknown content type: {contentType}";
                }
            }

            if (result.AddressStatus == AddressOpeningStatus.DownloadableContent)
            {
                try
                {
                    // Attempt to retrieve the response body as text
                    result.TextContent = await response.TextAsync();
                }
                catch
                {
                    try
                    {
                        // Attempt to retrieve the response body as text
                        result.BinaryContent = await response.BodyAsync();
                    }
                    catch
                    {
                        // If TextAsync fails, assume it's binary content
                        result.AddressStatus = AddressOpeningStatus.UnsupportedContentType;
                        result.ErrorMessage = "Content-type not set and content doesn't seem to be text.";
                        return result;
                    }
                }
            }
            else
            {
                result.AddressStatus = taskWhichFinished1st == 0 ? AddressOpeningStatus.Ok : AddressOpeningStatus.OkButNetworkActive;
            }

            return result;
        }

        private static string GetFileNameFromUrlAndContentType(string url, string contentType)
        {

            var extension = MimeTypesMap.GetExtension(GetMediaTypeOnly(contentType));
            var fileName = SanitizeUriForFileName(url.TrimEnd('/'));
            return $"{fileName}.{extension}";
        }

        public static string GetMediaTypeOnly(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return string.Empty;

            int semicolonIndex = contentType.IndexOf(';');
            return semicolonIndex > -1
                ? contentType.Substring(0, semicolonIndex).Trim()
                : contentType.Trim();
        }
        public static bool IsContentTypeSupported(string contentType)
        {
            // Define lists of MIME types for documents, images, and compressed files
            var documentTypes = new HashSet<string>
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "text/plain",
                "application/xml",
                "text/xml",
                "application/json"
            };

            var compressedTypes = new HashSet<string>
            {
                "application/zip",
                "application/gzip",
                "application/x-tar",
                "application/x-7z-compressed",
                "application/x-rar-compressed"
            };

            return documentTypes.Contains(contentType.ToLower()) || contentType.Contains("image/") || compressedTypes.Contains(contentType);
        }

        private static string SanitizeUriForFileName(string uri)
        {
            // Replace invalid characters with an underscore
            string sanitized = Regex.Replace(uri, @"[\\/:*?""<>|\.]", "_");

            // Optionally, trim the length to ensure compatibility with file systems
            const int maxFileNameLength = 255;
            if (sanitized.Length > maxFileNameLength)
            {
                sanitized = sanitized.Substring(0, maxFileNameLength);
            }

            return sanitized;
        }

        public async Task<bool> CanScrollDown()
        {
            ValidatePage();
            var scriptResult = await _page.EvaluateAsync<double>(JsScripts.CurrentScrollPosition);
            var currentScrollPosition = GetRoundedUpNumberFromJsResult(scriptResult);
            scriptResult = await _page.EvaluateAsync<double>(JsScripts.WindowSize);
            var windowSize = GetRoundedUpNumberFromJsResult(scriptResult);
            scriptResult = await _page.EvaluateAsync<double>(JsScripts.MaxScrollHeight);
            var maxHeight = GetRoundedDownNumberFromJsResult(scriptResult);
            if (!ValidateScriptResults(currentScrollPosition, windowSize, maxHeight))
            {
                return false;
            }

            return currentScrollPosition + windowSize < maxHeight;
        }

        private bool ValidateScriptResults(long? currentScrollPosition, long? windowSize, long? maxHeight)
        {
            if (!currentScrollPosition.HasValue)
            {
                _logger.LogWarning("Can't determine current Y scroll position");
                return false;
            }
            if (!windowSize.HasValue)
            {
                _logger.LogWarning("Can't determine the window size");
                return false;
            }
            if (!maxHeight.HasValue)
            {
                _logger.LogWarning("Can't determine max height");
                return false;
            }

            return true;
        }

        private static long? GetRoundedDownNumberFromJsResult(object scriptExecutionResult)
        {
            var result = scriptExecutionResult switch
            {
                string s => RoundDown(double.Parse(s)),
                double d => RoundDown(d),
                long i => i,
                _ => (long?)null
            };

            return result;
        }

        private static long? GetRoundedUpNumberFromJsResult(object scriptExecutionResult)
        {
            var result = scriptExecutionResult switch
            {
                string s => RoundUp(double.Parse(s)),
                double d => RoundUp(d),
                long i => i,
                _ => (long?)null
            };

            return result;
        }

        private static long RoundUp(double d)
        {
            return Convert.ToInt64(Math.Ceiling(d));
        }

        private static long RoundDown(double d)
        {
            return Convert.ToInt64(Math.Floor(d));
        }

        [MemberNotNull(nameof(_page))]
        private void ValidatePage()
        {
            if (_page == null)
            {
                throw new InvalidOperationException("The browser page has not been initialized. Call StartBrowser first.");
            }
        }

        public void ResetBrowser()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> SaveScreenshot(string path)
        {
            ValidatePage();
            return _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                Type = ScreenshotType.Png,
                FullPage = false // Capture just what is visible in the viewport
            });
        }

        public Task<byte[]> SavePageImage(string path)
        {
            ValidatePage();
            return _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                Type = ScreenshotType.Png,
                FullPage = true // Capture the full page 
            });
        }

        public Task<byte[]> SavePagePdf(string path)
        {
            ValidatePage();
            return _page.PdfAsync(new PagePdfOptions
            {
                Path = path,
                PrintBackground = true // Include background graphics
            });
        }

        public async Task<bool> ScrollDown(int scrollDistanceInPixels)
        {
            ValidatePage();

            var canScrollDown = await CanScrollDown();
            if (canScrollDown)
            {
                await _page.EvaluateAsync($"window.scrollBy(0,{scrollDistanceInPixels})");
                return true;
            }

            return false;
        }

        public async Task<bool> ScrollToBottom()
        {
            ValidatePage();
            var canScrollDown = await CanScrollDown();
            if (canScrollDown)
            {
                var scriptResult = await _page.EvaluateAsync<double>(JsScripts.MaxScrollHeight);
                var maxHeight = GetRoundedDownNumberFromJsResult(scriptResult);
                if (maxHeight > 3000)
                {
                    await _page.EvaluateAsync(JsScripts.ScrollBy(maxHeight.Value));
                    await DelayService.WaitASecAsync();
                    _logger.LogInformation("scrolled to bottom, will try to scroll more...");
                }

                await KeepScrollingDown();
                _logger.LogInformation("finished scrollong");

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_page != null)
            {
                _page.CloseAsync().GetAwaiter().GetResult();
                _page = null;
            }

            _browser?.CloseAsync().GetAwaiter().GetResult();
            _playwright?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }
            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
        }
    }
}
