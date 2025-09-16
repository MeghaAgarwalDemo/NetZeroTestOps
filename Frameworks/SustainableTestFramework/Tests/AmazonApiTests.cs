using Xunit;
using GreenerTestFramework.Utilities;
using GreenerTestFramework.Config;
using System.Threading.Tasks;

namespace GreenerTestFramework.Tests
{
    public class AmazonApiTests
    {
        [Fact]
        public async Task HomePage_ShouldReturnSuccess()
        {
            var baseUrl = ConfigManager.GetBaseUrl();
            var client = new ApiClient(baseUrl);
            var response = await client.GetAsync(""); // Home page
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
