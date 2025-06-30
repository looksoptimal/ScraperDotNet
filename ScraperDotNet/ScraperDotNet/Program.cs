//// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScraperDotNet;
using ScraperDotNet.Ai;
using ScraperDotNet.Browser;
using ScraperDotNet.Db;
using ScraperDotNet.Parsing;
using ScraperDotNet.Services;
using Serilog;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, configuration) =>
    {
        configuration.Sources.Clear();
        configuration.AddJsonFile("appsettings.local.json", optional: true);
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.ClearProviders(); // Clear any default providers
        logging.SetMinimumLevel(LogLevel.Information);

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(hostingContext.Configuration)
            .CreateLogger();
        logging.AddSerilog(logger);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<AppSettings>(s => new AppSettings(hostContext.Configuration));
        services.AddDbContext<ScraperContext>(options =>
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("ScraperContext")));
        var pdfLocation = hostContext.Configuration.GetValue<string>("PagePdfLocation");
        services.AddSingleton<IBrowserRunnerAsync, PlaywrightBrowserRunner>();
        services.AddSingleton<IApplicationRunner, ApplicationRunner>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IAddressService, AddressService>();
        services.AddSingleton<IPageParser, PageParser>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IFtpDownloader, FtpDownloader>();

        // Register AI services
        services.AddOllamaClient();
        //services.AddSingleton<AiExampleService>();
    })
    .Build();


var app = host.Services.GetRequiredService<IApplicationRunner>();
app.Run();


