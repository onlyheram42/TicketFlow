using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketFlow.Desktop.Models;

namespace TicketFlow.Desktop.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // Ensure this matches the port your Web API is running on
        private const string BaseUrl = "https://localhost:7031/api/";

        public ApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private void AttachBearerToken()
        {
            var token = SessionManager.Instance.Token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            AttachBearerToken();
            var response = await _httpClient.GetAsync(endpoint);
            return await ParseResponseAsync<ApiResponse<T>>(response);
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            return await ParseResponseAsync<ApiResponse<T>>(response);
        }

        public async Task<ApiResponse> PostNoDataAsync(string endpoint, object data)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            return await ParseResponseAsync<ApiResponse>(response);
        }

        public async Task<ApiResponse> PutAsync(string endpoint, object data)
        {
            AttachBearerToken();
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(endpoint, content);
            return await ParseResponseAsync<ApiResponse>(response);
        }

        private async Task<T> ParseResponseAsync<T>(HttpResponseMessage response) where T : new()
        {
            var json = await response.Content.ReadAsStringAsync();
            
            try
            {
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (result == null)
                {
                    throw new Exception("Failed to deserialize the API response.");
                }

                // Treat unauthorized or bad requests as failed operations
                if (!response.IsSuccessStatusCode)
                {
                    // Assuming T is either ApiResponse or ApiResponse<T>, we can dynamically set Success = false
                    var successProperty = typeof(T).GetProperty("Success");
                    successProperty?.SetValue(result, false);

                    var messageProperty = typeof(T).GetProperty("Message");
                    if (string.IsNullOrEmpty((string?)messageProperty?.GetValue(result)))
                    {
                        messageProperty?.SetValue(result, $"HTTP Error: {response.StatusCode}");
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error parsing response: {ex.Message}. Response Body: {json}");
            }
        }
    }
}
