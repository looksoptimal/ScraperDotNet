using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ScraperDotNet.Db
{
    public class ScraperContext: DbContext
    {
        public ScraperContext(DbContextOptions<ScraperContext> options)
        : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.local.json")
                   .Build();
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("ScraperContext"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>()
                .Property(p => p.Status)
                .HasConversion<string>() // This stores the enum as its name
                .HasColumnType("nvarchar(30)");

            modelBuilder.Entity<Page>()
                .Property(p => p.ContentType)
                .HasConversion<string>() // This stores the enum as its name
                .HasColumnType("nvarchar(30)");

            modelBuilder.Entity<Page>()
                .Property(p => p.Content)
                .HasComputedColumnSql("CONVERT([nvarchar](max),Decompress([CompressedContent]))", stored: false);

            base.OnModelCreating(modelBuilder);
        }


        public DbSet<Address> Addresses { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Entity> Entities { get; set; }
    }
}
