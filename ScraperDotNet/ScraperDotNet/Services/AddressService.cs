using Microsoft.Extensions.Logging;
using ScraperDotNet.Db;

namespace ScraperDotNet.Services
{
    public class AddressService(ScraperContext context, ILogger<AddressService> logger) : IAddressService
    {
        private readonly ScraperContext _context = context;
        private readonly ILogger<AddressService> _logger = logger;

        public (Address address, bool created) GetOrCreate(string uri, bool ignoreQueryString, string comment)
        {
            return GetOrCreate(uri, ignoreQueryString, comment, null);
        }

        public (Address address, bool created) GetOrCreate(string uri, bool ignoreQueryString, string comment, string? contentGroup)
        {
            var address = GetAddressByUrl(ignoreQueryString, uri);
            var created = false;
            if (address == null)
            {
                created = true;
                address = CreateAddress(uri, comment, contentGroup);
            }

            return (address, created);
        }

        public Address? GetAddressByUrl(bool ignoreQueryString, string uri)
        {
            var parsedUri = new Uri(uri);
            return _context.Addresses.FirstOrDefault(x =>
                x.Domain == parsedUri.Host.ToLower() &&
                (x.Port == null || x.Port == parsedUri.Port) &&
                (x.Path == parsedUri.AbsolutePath || x.Path + "/" == parsedUri.AbsolutePath) &&
                (ignoreQueryString || x.QueryString == parsedUri.Query)
            );
        }

        public bool AreUrisEqual(string uri1, string uri2, bool ignoreQueryString)
        {
            var parsedUri1 = new Uri(uri1);
            var parsedUri2 = new Uri(uri2);
            var partsToCompare = ignoreQueryString ? UriComponents.Host | UriComponents.Path : UriComponents.Host | UriComponents.PathAndQuery;
            var comparisonResult = Uri.Compare(parsedUri1, parsedUri2, partsToCompare,
                UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
            return comparisonResult == 0;
        }

        public string GetUriString(Address address)
        {
            UriBuilder uri = GetUriBuilderForRoot(address);
            uri.Path = address.Path;
            uri.Query = address.QueryString;
            return uri.ToString();
        }

        public Uri GetRootUri(Address address)
        {
            UriBuilder uri = GetUriBuilderForRoot(address);
            return uri.Uri;
        }

        public Uri GetUriWithoutQueryString(Address address)
        {
            UriBuilder uri = GetUriBuilderForRoot(address);
            uri.Path = address.Path;
            return uri.Uri;
        }

        private static UriBuilder GetUriBuilderForRoot(Address address)
        {
            var uri = new UriBuilder();
            uri.Scheme = address.Scheme;
            uri.Host = address.Domain;
            if (address.Port.HasValue)
            {
                uri.Port = address.Port.Value;
            }

            return uri;
        }

        private Address CreateAddress(string uri, string comment, string? contentGroup = null)
        {
            var parsedUri = new Uri(uri);
            var address = new Address
            {
                Status = AddressStatus.Fresh,
                Scheme = parsedUri.Scheme,
                Domain = parsedUri.Host.ToLower(),
                Port = parsedUri.IsDefaultPort ? null : parsedUri.Port,
                Path = parsedUri.AbsolutePath,
                QueryString = parsedUri.Query,
                Comment = comment,
                ContentGroup = contentGroup,
            };
            _context.Addresses.Add(address);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating an address for URI: {uri}");
                throw;
            }

            return address;
        }

        public string GetAddressGroupName(Address address)
        {
            return address.ContentGroup ?? address.Domain.Replace("www.", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace('.', '_');
        }

        public string CreateDomainBasedGroupName(string addressUri)
        {
            var uri = new Uri(addressUri);
            return uri.Host.Replace("www.", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace('.', '_');
        }

        public async Task SetAddressGroupName(Address address, string groupName)
        {
            address.ContentGroup = groupName;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting group name '{groupName}' for address: {address.Id}");
                throw;
            }
        }
    }
}
