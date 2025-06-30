using ScraperDotNet.Db;

namespace ScraperDotNet.Services
{
    public interface IAddressService
    {
        bool AreUrisEqual(string uri1, string uri2, bool ignoreQueryString);
        string CreateDomainBasedGroupName(string addressUri);
        Address? GetAddressByUrl(bool ignoreQueryString, string uri);
        string GetAddressGroupName(Address address);
        (Address address, bool created) GetOrCreate(string uri, bool ignoreQueryString, string comment);
        (Address address, bool created) GetOrCreate(string uri, bool ignoreQueryString, string comment, string? contentGroup);
        Uri GetRootUri(Address address);
        string GetUriString(Address address);
        Uri GetUriWithoutQueryString(Address address);
        Task SetAddressGroupName(Address address, string groupName);
    }
}