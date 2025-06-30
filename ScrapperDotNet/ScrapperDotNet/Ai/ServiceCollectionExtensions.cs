using Microsoft.Extensions.DependencyInjection;

namespace ScrapperDotNet.Ai
{
    /// <summary>
    /// Extension methods for registering AI services with the dependency injection container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Ollama AI client to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddOllamaClient(this IServiceCollection services)
        {
            // Register the AI client as a singleton since it's expensive to create
            services.AddSingleton<IAiClient, OllamaClient>();
            
            return services;
        }
    }
}
