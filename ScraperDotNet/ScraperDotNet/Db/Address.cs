using System.ComponentModel.DataAnnotations;

namespace ScraperDotNet.Db
{
    public class Address
    {
        public long Id { get; set; }
        public string? Comment { get; set; }
        public AddressStatus Status { get; set; }
        public string? Tags { get; set; }

        [MaxLength(30)]
        public string Scheme { get; set; }
        
        [MaxLength(253)]
        public string Domain { get; set; }      // max length 253 

        public int? Port { get; set; }

        [MaxLength(2083)]
        public string? Path { get; set; }      // assume max length 2083

        [MaxLength(2083)]
        public string? QueryString { get; set; }      // assume max length 2083 
        public string? ContentGroup { get; set; }
    }
}
