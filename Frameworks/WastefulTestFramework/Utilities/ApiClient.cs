using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace NonGreenTestFramework.Utilities
{
    public class ApiClient
    {
        private readonly HttpClient _client;
        public ApiClient(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            // Inefficient: Creating a new client for every request (anti-pattern)
            using (var client = new HttpClient { BaseAddress = _client.BaseAddress })
            {
                await Task.Delay(1000); // Artificial delay to waste resources
                return await client.GetAsync(endpoint);
            }
        }
    }
}
