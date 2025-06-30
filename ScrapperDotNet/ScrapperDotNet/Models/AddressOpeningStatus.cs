namespace ScrapperDotNet.Models
{
    public enum AddressOpeningStatus
    {
        Ok,
        OkButNetworkActive,
        CantConnect,
        FailedToLoad,
        RequiresUserAction,
        DownloadableContent,
        UnsupportedContentType,
        UnsupportedScheme,
        PageWithAttachment
    }
}
