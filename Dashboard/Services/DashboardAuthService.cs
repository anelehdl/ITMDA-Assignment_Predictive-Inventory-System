using Core.Models.DTO;
using System.Text;
using System.Text.Json;

namespace Dashboard.Services
{
    public class DashboardAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DashboardAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("DummyAPI");

                var loginRequest = new LoginRequestDto
                {
                    Email = email,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new LoginResponseDto { Success = false, Message = "Failed to parse response" };
                }
                else
                {
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Login failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        //attempting to fix refresh token:
        public async Task<(string Token, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("DummyAPI");

                var refreshRequest = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[RefreshTokenAsync] Sending refresh request with token: {refreshToken.Substring(0, 10)}...");         //debug within the console for quicker understanding

                var response = await client.PostAsync("/api/auth/refresh", content);

                Console.WriteLine($"[RefreshTokenAsync] Response status: {response.StatusCode}");             //debug

                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RefreshTokenAsync] Response body: {body}");              //debug

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Refresh token request failed");

                var result = JsonSerializer.Deserialize<LoginResponseDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null || !result.Success || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.RefreshToken))
                    throw new Exception("Invalid refresh token response");

                return (result.Token, result.RefreshToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RefreshTokenAsync] Exception: {ex.Message}");            //debug
                throw new Exception($"Failed to refresh token: {ex.Message}");
            }
        }
    }
}