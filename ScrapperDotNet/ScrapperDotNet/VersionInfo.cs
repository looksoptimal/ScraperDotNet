using System.Diagnostics;
using System.Reflection;

namespace ScrapperDotNet
{
    public static class VersionInfo
    {
        /// <summary>
        /// Gets the product version in format Major.Minor.Build (e.g., 1.0.0)
        /// </summary>
        public static string ProductVersion
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
            }
        }

        /// <summary>
        /// Gets the full version information in format Major.Minor.Build.Revision (e.g., 1.0.0.0)
        /// </summary>
        public static string FullVersion
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
        }

        /// <summary>
        /// Gets application build date based on the linked binary
        /// </summary>
        public static DateTime BuildDate
        {
            get
            {
                string filePath = Assembly.GetExecutingAssembly().Location;
                if (File.Exists(filePath))
                {
                    return File.GetLastWriteTime(filePath);
                }
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets a formatted version string with build date
        /// </summary>
        public static string VersionWithBuildDate
        {
            get
            {
                return $"v{ProductVersion} (Built on {BuildDate:yyyy-MM-dd})";
            }
        }
    }
}
