using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScrapperDotNet.Db
{
    public class ScrapperContextFactory: IDesignTimeDbContextFactory<ScrapperContext>
    {
        public ScrapperContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.local.json");

            var config = builder.Build();

            var optionsBuilder = new DbContextOptionsBuilder<ScrapperContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("ScrapperContext"));

            return new ScrapperContext(optionsBuilder.Options);
        }
    }
}
