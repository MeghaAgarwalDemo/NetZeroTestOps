using System.IO;
using System.Text.Json;

namespace GreenerTestFramework.Config
{
    public class ConfigManager
    {
        public static string GetBaseUrl()
        {
            var json = File.ReadAllText("Config/TestSettings.json");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("BaseUrl").GetString();
        }
    }
}
