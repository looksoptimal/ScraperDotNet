using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScraperDotNet.Db
{
    public class ScraperContextFactory: IDesignTimeDbContextFactory<ScraperContext>
    {
        public ScraperContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.local.json");

            var config = builder.Build();

            var optionsBuilder = new DbContextOptionsBuilder<ScraperContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("ScraperContext"));

            return new ScraperContext(optionsBuilder.Options);
        }
    }
}
