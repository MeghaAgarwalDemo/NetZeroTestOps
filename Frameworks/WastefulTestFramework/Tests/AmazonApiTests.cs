using Xunit;
using NonGreenTestFramework.Utilities;
using NonGreenTestFramework.Config;
using System.Threading.Tasks;
using System.Net.Http;
using System;

namespace NonGreenTestFramework.Tests
{
    public class AmazonApiTests
    {
        [Fact(DisplayName = "Home Page - Inefficient Test")]
        public async Task HomePage_ShouldReturnSuccess()
        {
            for (int i = 0; i < 10; i++)
            {
                var baseUrl = ConfigManager.GetBaseUrl();
                var client = new ApiClient(baseUrl);
                var response = await client.GetAsync("");
                Assert.True(response.IsSuccessStatusCode);
            }
        }
    }
}
