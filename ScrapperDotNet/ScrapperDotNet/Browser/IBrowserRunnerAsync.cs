using Microsoft.Playwright;
using OpenQA.Selenium;
using ScrapperDotNet.Models;

namespace ScrapperDotNet.Browser
{
    public interface IBrowserRunnerAsync
    {
        event EventHandler<AddressOpeningResult>? DownloadAttachment;

        Task StartBrowser();
        Task<bool> CanScrollDown();

        Task KeepScrollingDown();
        Task<bool> ScrollDown(int scrollDistanceInPixels);
        Task<bool> ScrollToBottom();
        Task<string?> PageContent();
        bool IsOriginalWindowShown { get; }

        Task<byte[]> SavePageImage(string path);
        Task<byte[]> SaveScreenshot(string path);
        Task<byte[]> SavePagePdf(string path);
        Task<AddressOpeningResult> OpenPageAndWaitUntilItLoads(string pageUrl);
        void ResetBrowser();
        ILocator? FindElementByName(string name);
    }
}