using System.ComponentModel.DataAnnotations.Schema;

namespace ScraperDotNet.Db
{
    public class Page
    {
        public long Id { get; set; }
        public byte[]? CompressedContent { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Content { get; set; }
        public string? ContentPath { get; set; }
        public ContentType ContentType { get; set; }
        public DateTime Downloaded { get; set; }
        public int? EntityId { get; set; }
        public Entity? Entity { get; set; }
        public long AddressId { get; set; }
        public Address Address { get; set; }
        public string? Tags { get; set; }
    }
}
