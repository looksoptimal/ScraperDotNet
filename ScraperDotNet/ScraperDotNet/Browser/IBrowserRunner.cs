using OpenQA.Selenium;
using ScraperDotNet.Models;

namespace ScraperDotNet.Browser
{
    public interface IBrowserRunner
    {
        Task StartBrowser();
        bool CanScrollDown { get; }

        void Dispose();
        IWebElement? FindElementByName(string name);
        void KeepScrollingDown();
        bool ScrollDown(int scrollDistanceInPixels);
        bool ScrollToBottom();
        void TypeIntoElement(IWebElement element);
        string? PageContent { get; }
        bool IsOriginalWindowShown { get; }

        void SavePageImage(string path);
        void SavePagePdf(string path);
        Task<AddressOpeningResult> OpenPageAndWaitUntilItLoads(string pageUrl);
        void ResetBrowser();
    }
}