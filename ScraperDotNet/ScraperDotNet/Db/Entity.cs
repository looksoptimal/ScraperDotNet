namespace ScraperDotNet.Db
{
    public class Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? EntityType { get; set; }
        public string? Comment { get; set; }
    }
}
