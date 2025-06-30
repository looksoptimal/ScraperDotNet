//// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrapperDotNet;
using ScrapperDotNet.Ai;
using ScrapperDotNet.Browser;
using ScrapperDotNet.Db;
using ScrapperDotNet.Parsing;
using ScrapperDotNet.Services;
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
        services.AddDbContext<ScrapperContext>(options =>
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("ScrapperContext")));
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


