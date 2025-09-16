using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace GreenerTestFramework.Utilities
{
    public class ApiClient
    {
        private readonly HttpClient _client;
        public ApiClient(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new System.Uri(baseUrl) };
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await _client.GetAsync(endpoint);
        }

        // Add more methods for POST, PUT, DELETE as needed
    }
}
