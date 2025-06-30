namespace ScraperDotNet.Browser
{
    public class JsScripts
    {
        public const string MaxScrollHeight = @"() => {let getElementScrollHeight = element => {
                if (!element) return 0
                let { scrollHeight, offsetHeight, clientHeight } = element
                return Math.max(scrollHeight, offsetHeight, clientHeight)
              };
                return getElementScrollHeight(document.body);
                }";

        public const string WindowSize = "() => window.innerHeight;";

        public const string CurrentScrollPosition = "() => window.scrollY";

        public const string DocumentReady = "() => document.readyState";

        public static string ScrollBy(long pixels) => $"() => window.scrollBy(0,{pixels})";
        public static string TriggerUrlDownload(string url) 
        {
            const string jsCodeToTriggerDownload = @"
url => {
  const a = document.createElement('a');
  a.href = url;
  //a.download = url.split('/').pop(); // optional: sets the filename
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
}";
            return $"({jsCodeToTriggerDownload})('{url}');";
        }
    }
}
