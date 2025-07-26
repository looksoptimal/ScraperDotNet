using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using ScraperDotNet.Ai;
using ScraperDotNet.Browser;
using ScraperDotNet.Parsing;
using ScraperDotNet.Services;

namespace ScraperDotNet
{
    public class ApplicationRunner(ILogger<ApplicationRunner> logger, IDownloadService downloadService, IBrowserRunnerAsync browser, IAddressService addressService, IAiClient ollamaClient, IPageParser pageParser) : IApplicationRunner
    {
        private readonly IDownloadService _downloadService = downloadService;
        private readonly IBrowserRunnerAsync _browser = browser;
        private readonly IAddressService _addressService = addressService;
        private readonly IAiClient _ollamaClient = ollamaClient;
        private readonly IPageParser _pageParser = pageParser;
        private readonly TaskScheduler _browserScheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, maxConcurrencyLevel: 1).ExclusiveScheduler;
        private string? fileName;
        private CancellationTokenSource _cancelationTokenSource = new CancellationTokenSource();

        public void Run()
        {
            logger.LogInformation($"Scraper app {VersionInfo.VersionWithBuildDate} has started");
            Console.WriteLine($"ScraperDotNet {VersionInfo.VersionWithBuildDate}");
            Console.WriteLine("Copyright © {0}", DateTime.Now.Year);
            Console.WriteLine();

            try
            {
                // Create a new task using TaskCreationOptions.LongRunning to force a new thread
                var browserStartTask = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await _browser.StartBrowser();
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "Couldn't start a browser.");
                        throw;
                    }
                },
                _cancelationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                _browserScheduler).Unwrap();

                // Wait for browser to start with a timeout
                if (!browserStartTask.Wait(TimeSpan.FromMinutes(2)))
                {
                    throw new TimeoutException("Browser startup timed out after 2 minutes");
                }

                if (browserStartTask.IsFaulted && browserStartTask.Exception != null)
                {
                    throw browserStartTask.Exception;
                }

                ProcessCommands(_browser);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An exception occurred while the app was running.");
                throw;
            }
        }

        private void ProcessCommands(IBrowserRunnerAsync browser)
        {
            Console.WriteLine("Press a key:");
            Console.WriteLine("A - auto download all fresh pages");
            //Console.WriteLine("T - download fresh pages with a specific tag ");
            Console.WriteLine("1 - open a page by its Address.Id and save its source");
            Console.WriteLine("U - add a new address and open it ");
            Console.WriteLine("D - domain download - for a given address, traverse it and get everything that can be found within its domain");
            Console.WriteLine("P - save current page as a PDF ");
            Console.WriteLine("S - make a screenshot of the currently displayed page (as PNG)");
            Console.WriteLine("I - save the whole page as an Image (as PNG)");
            Console.WriteLine("O - ask an Ollama model about an image");
            Console.WriteLine("X - extract links from downloaded pages and populate more addresses");
            Console.WriteLine("Y - extract links from a given page and populate addresses");
            Console.WriteLine("Z - extract links from a given page and populate addresses WITHIN DOMAIN");
            Console.WriteLine("V - display version information");
            Console.WriteLine("<Esc> - exit ");
            var keyPressed = Console.ReadKey().Key;
            while (keyPressed != ConsoleKey.Escape)
            {
                Task? actionToExecute = null;
                switch (keyPressed)
                {
                    case ConsoleKey.A: actionToExecute = DownloadAll(); break;
                    case ConsoleKey.D1: actionToExecute = DownloadById(); break;
                    case ConsoleKey.U: actionToExecute = OpenUrlAndSave(); break;
                    case ConsoleKey.D: actionToExecute = DownloadDomain(); break;
                    //case ConsoleKey.T: Console.WriteLine("download fresh pages with a specific tag"); break;
                    case ConsoleKey.P: actionToExecute = SavePdf(); break;
                    case ConsoleKey.S: actionToExecute = SaveScreenshot(); break;
                    case ConsoleKey.I: actionToExecute = SaveImage(); break;
                    case ConsoleKey.DownArrow: actionToExecute = browser.ScrollDown(50); break;
                    case ConsoleKey.PageDown: actionToExecute = browser.ScrollDown(500); break;
                    case ConsoleKey.B: actionToExecute = browser.KeepScrollingDown(); break;
                    case ConsoleKey.O: actionToExecute = AskOllamaImage(); break;
                    case ConsoleKey.X: actionToExecute = PopulateAddressesFromAllPages(); break;
                    case ConsoleKey.Y: actionToExecute = PopulateAddressesFromAPage(); break;
                    case ConsoleKey.Z: actionToExecute = PopulateAddressesFromAPageForDomain(); break;
                    case ConsoleKey.V: DisplayVersionInfo(); break;
                }

                Console.WriteLine("\nPress a key:");
                keyPressed = Console.ReadKey().Key;
                if (keyPressed == ConsoleKey.Escape)
                {
                    _cancelationTokenSource.Cancel();
                    break;
                }

                var actionTask = Task.Run(() => actionToExecute).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Console.WriteLine($"Exception caught: {t.Exception.InnerException?.Message}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private Task<IEnumerable<long>> DownloadAll()
        {
            logger.LogInformation("Downloading all fresh pages");
            return _downloadService.DownloadPagesForFreshAdresses();
        }

        private async Task DownloadDomain()
        {
            Console.WriteLine("Enter the address (remember to start with the schema e.g. https://...): ");
            var enteredUrl = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(enteredUrl))
            {
                try
                {
                    var uri = new Uri(enteredUrl);
                }
                catch (UriFormatException)
                {
                    Console.WriteLine("The address is invalid");
                }

                await _downloadService.DownloadDomain(enteredUrl);
            }
            else
            {
                Console.WriteLine("no address specified");
            }
        }

        private async Task DownloadById()
        {
            Console.WriteLine("Enter the Id of the address: ");
            var idString = Console.ReadLine();
            if (idString != null && long.TryParse(idString, out long id))
            {
                logger.LogInformation($"Downloading a page for address id = {id}");
                try
                {
                    var pageId = await _downloadService.DownloadPageByAddressId(id);
                    logger.LogInformation($"New page downloaded: {pageId}");
                }
                catch (WebDriverException ex)
                {
                    logger.LogError(ex, $"Browser issue - can't reach the page for address {id}");
                    Console.WriteLine("Browser issue - can't reach the page");
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to download a page for address {id}");
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("invalid Id");
            }
        }

        private async Task OpenUrlAndSave()
        {
            Console.WriteLine("Enter the address (remember to start with the schema e.g. https://...): ");
            var url = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(url))
            {
                long? addressId = null; try
                {
                    var (address, created) = _addressService.GetOrCreate(url, false, "entered manually");
                    addressId = address.Id;
                    if (!created)
                    {
                        Console.WriteLine($"The address was already in the db (Id = {address.Id})");
                    }
                }
                catch (UriFormatException)
                {
                    Console.WriteLine("The address is invalid");
                }

                if (addressId.HasValue)
                {
                    var pageId = await _downloadService.DownloadPageByAddressId(addressId.Value);
                    Console.WriteLine($"new page: {pageId}");
                }
            }
            else
            {
                Console.WriteLine("no address specified");
            }
        }

        private async Task SavePdf()
        {
            Console.WriteLine("Enter the name for the pdf file: ");
            var fileName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string nameWithExtension = GetNameWithExtension(fileName, "pdf");
                var path = await _downloadService.SavePagePdf(nameWithExtension);
                Console.WriteLine($"safved as: {path}");
            }
            else
            {
                Console.WriteLine("no file name specified");
            }
        }

        private async Task SaveScreenshot()
        {
            Console.WriteLine("Enter the name for the png file: ");
            var fileName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string nameWithExtension = GetNameWithExtension(fileName, "png");
                var path = await _downloadService.SaveScreenshot(nameWithExtension);
                Console.WriteLine($"safved as: {path}");
            }
            else
            {
                Console.WriteLine("no file name specified");
            }
        }

        private async Task SaveImage()
        {
            Console.WriteLine("Enter the name for the png file: ");
            var fileName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string nameWithExtension = GetNameWithExtension(fileName, "png");
                var path = await _downloadService.SavePageImage(nameWithExtension);
                Console.WriteLine($"safved as: {path}");
            }
            else
            {
                Console.WriteLine("no file name specified");
            }
        }

        private async Task AskOllamaImage()
        {
            Console.WriteLine("Enter the full path for the image file: ");
            var filePath = Console.ReadLine();
            Console.WriteLine("Enter the prompt: ");
            var prompt = Console.ReadLine();
            var modelResponse = await _ollamaClient.AskAboutImageAsync(prompt ?? string.Empty, filePath ?? string.Empty);
            Console.WriteLine($"Ollama response: {modelResponse}");
        }

        private Task PopulateAddressesFromAllPages()
        {
            // Create a new task using TaskCreationOptions.LongRunning to force a new thread
            var parsingTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await _pageParser.ProcessPagesInChunksAsync();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Couldn't start a browser.");
                    throw;
                }
            },
            _cancelationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            _browserScheduler).Unwrap();

            return parsingTask;
        }

        private async Task PopulateAddressesFromAPage()
        {
            Console.WriteLine("Enter the Id of the page: ");
            var idString = Console.ReadLine();
            if (idString != null && long.TryParse(idString, out long id))
            {
                logger.LogInformation($"Getting addresses from page id = {id}");
                try
                {
                    var cancelationTokenSource = new CancellationTokenSource();
                    await _pageParser.PopulateFreshAddressesFromPage(id, cancelationTokenSource.Token);
                    logger.LogInformation($"Page processed: {id}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to process a page {id}");
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("invalid Id");
            }
        }

        private async Task PopulateAddressesFromAPageForDomain()
        {
            Console.WriteLine("Enter the Id of the page: ");
            var idString = Console.ReadLine();
            if (idString != null && long.TryParse(idString, out long id))
            {
                logger.LogInformation($"Getting addresses within domain from page id = {id}");
                try
                {
                    var cancelationTokenSource = new CancellationTokenSource();
                    await _pageParser.PopulateFreshAddressesFromPageForDomain(id, cancelationTokenSource.Token);
                    logger.LogInformation($"Page processed: {id}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to process a page {id}");
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("invalid Id");
            }
        }

        private static string GetNameWithExtension(string fileName, string extension)
        {
            var extensionLowercase = extension.ToLower();
            var sanitizedFileName = ReplaceInvalidChars(fileName);
            return sanitizedFileName.ToLower().EndsWith("." + extensionLowercase) ? sanitizedFileName : $"{sanitizedFileName}.{extensionLowercase}";
        }
        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        private void DisplayVersionInfo()
        {
            Console.WriteLine("\nScraperDotNet Version Information:");
            Console.WriteLine($"Product Version: {VersionInfo.ProductVersion}");
            Console.WriteLine($"Full Version: {VersionInfo.FullVersion}");
            Console.WriteLine($"Build Date: {VersionInfo.BuildDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Running on: {Environment.OSVersion}");
            Console.WriteLine($".NET Runtime: {Environment.Version}");
            Console.WriteLine($"Machine Name: {Environment.MachineName}");
            Console.WriteLine($"Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
        }
    }
}
