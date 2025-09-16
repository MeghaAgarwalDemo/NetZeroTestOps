using Xunit;
using NonGreenTestFramework.Utilities;
using NonGreenTestFramework.Config;
using System.Threading.Tasks;
using System.Net.Http;
using System;

namespace NonGreenTestFramework.Tests
{
    public class OrderFlowTests
    {
        [Fact(DisplayName = "Search Product - Inefficient Test")]
        public async Task SearchProduct_ShouldReturnSuccess()
        {
            for (int i = 0; i < 10; i++) // Redundant loop, unnecessary requests
            {
                var baseUrl = ConfigManager.GetBaseUrl(); // Read config every time
                var client = new ApiClient(baseUrl); // Create new client every time
                var response = await client.GetAsync("/s?k=book");
                Assert.True(response.IsSuccessStatusCode);
            }
        }

        [Fact(DisplayName = "Add to Cart - Inefficient Test")]
        public async Task AddToCart_ShouldReturnSuccess()
        {
            for (int i = 0; i < 10; i++)
            {
                var baseUrl = ConfigManager.GetBaseUrl();
                var client = new ApiClient(baseUrl);
                var response = await client.GetAsync("/gp/cart/view.html");
                Assert.True(response.IsSuccessStatusCode);
            }
        }

        [Fact(DisplayName = "Proceed to Checkout - Inefficient Test")]
        public async Task ProceedToCheckout_ShouldReturnSuccess()
        {
            for (int i = 0; i < 10; i++)
            {
                var baseUrl = ConfigManager.GetBaseUrl();
                var client = new ApiClient(baseUrl);
                var response = await client.GetAsync("/gp/buy/spc/handlers/display.html?hasWorkingJavascript=1");
                Assert.True(response.IsSuccessStatusCode);
            }
        }
    }
}
