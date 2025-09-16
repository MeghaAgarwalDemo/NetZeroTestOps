using System.IO;
using System.Text.Json;
using System;

namespace NonGreenTestFramework.Config
{
    public class ConfigManager
    {
        public static string GetBaseUrl()
        {
            // Inefficient: Read config from disk every time (no caching)
            var json = File.ReadAllText("Config/TestSettings.json");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("BaseUrl").GetString();
        }
    }
}
