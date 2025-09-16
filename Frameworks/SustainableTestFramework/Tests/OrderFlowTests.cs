using Xunit;
using GreenerTestFramework.Utilities;
using GreenerTestFramework.Config;
using System.Threading.Tasks;
using System.Net.Http;

namespace GreenerTestFramework.Tests
{
    public class OrderFlowTests
    {
        private readonly ApiClient _client;
        public OrderFlowTests()
        {
            var baseUrl = ConfigManager.GetBaseUrl();
            _client = new ApiClient(baseUrl);
        }

        [Fact(DisplayName = "Search Product - Should Return Success")]
        public async Task SearchProduct_ShouldReturnSuccess()
        {
            // Placeholder: Replace with actual search endpoint and query params
            var response = await _client.GetAsync("/s?k=book");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact(DisplayName = "Add to Cart - Should Return Success")]
        public async Task AddToCart_ShouldReturnSuccess()
        {
            // Placeholder: Replace with actual add-to-cart endpoint and payload
            var response = await _client.GetAsync("/gp/cart/view.html");
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact(DisplayName = "Proceed to Checkout - Should Return Success")]
        public async Task ProceedToCheckout_ShouldReturnSuccess()
        {
            // Placeholder: Replace with actual checkout endpoint
            var response = await _client.GetAsync("/gp/buy/spc/handlers/display.html?hasWorkingJavascript=1");
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
