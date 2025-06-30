using Microsoft.Extensions.Configuration;

namespace ScraperDotNet
{
    public class AppSettings(IConfiguration configuration)
    {
        public string PageSaveLocation => configuration.GetValue<string>("PageSaveLocation") ??
            System.Reflection.Assembly.GetExecutingAssembly().Location + "/Pages";

        public bool AiEnabled => configuration.GetSection("Ai").Exists();

        public string OllamaModelName => configuration.GetValue<string>("Ai:OllamaModelName", "gemma3:12b");
        
        public string OllamaEndpoint => configuration.GetValue<string>("Ai:OllamaEndpoint", "http://localhost:11434");
        public bool HideBrowserUI => configuration.GetValue<bool>("Browser:HideUI", false);

        public bool WaitForUserActionOnBlockedPages => configuration.GetValue<bool>("WaitForUserActionOnBlockedPages", true);
    }
}
