using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScraperDotNet.Db;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.Property(p => p.Content)
            .HasComputedColumnSql(@"CAST(DECOMPRESS(CompressedContent) AS NVARCHAR(MAX))", stored: true)
            .HasColumnName("Content");
    }
}