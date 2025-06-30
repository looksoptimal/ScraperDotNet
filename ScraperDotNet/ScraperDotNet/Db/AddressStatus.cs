namespace ScraperDotNet.Db
{
    public enum AddressStatus
    {
        Fresh = 0,
        Opening = 1,
        Visited = 2,
        Duplicate = 3,
        Unsupported = 4,
        FailedToOpen = 5,
        ErrorOnPage = 6,
        RequiresUserAction = 7,
        FlaggedToSkip = 8
    }
}
