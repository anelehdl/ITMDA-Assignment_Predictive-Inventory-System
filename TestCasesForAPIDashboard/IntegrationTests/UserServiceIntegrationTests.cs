extern alias APIProgram;        //needed to add this according to ms docs, this ensures theres no binding conflicts with API and Dashboard program files
using Core.Models.DTO;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace TestCasesForAPIDashboard.IntegrationTests
{
    public class UserServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory<APIProgram.Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public UserServiceIntegrationTests(CustomWebApplicationFactory<APIProgram.Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }

        // ----------------------------
        // Adding a helper to ensure we are logged in to admin before calling protected APIs
        // ----------------------------

        private async Task<string> GetJwtTokenAsync(string email = "travis@test.com", string password = "123")          //hardcoded for testing purposes
        {
            // Arrange
            var loginDto = new
            {
                Email = email,
                Password = password
            };
            // Act - call login API
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            response.EnsureSuccessStatusCode();     //throw if login failed
            // Extract token from response
            var content = await response.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            // Return the JWT token
            return loginResult.Token;
        }

        // ----------------------------
        // Integration tests for User Service (CRUD) can be added here
        // ----------------------------

        [Fact]
        public async Task GetAllUsers_ReturnsSuccessStatusCode()
        {
            // Arrange
            var jwt = await GetJwtTokenAsync(); // get jwt token
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/unifiedusermanagement/users");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            //basic output to show pass
            _output.WriteLine("GetAllUsers_ReturnsSuccessStatusCode passed.");
        }

        //gonna have to look at this logic in the mornin
        [Fact]
        public async Task GetStaffUsers_ReturnsSuccessStatusCode()
        {
            // Arrange
            var jwt = await GetJwtTokenAsync(); // get jwt token
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            //note: need to provide an actual staff user id for this test to be valid
            var id = "68efc3a0ab145dd9b7a3b7f6"; //gonna have to look at user id could be mongoid?      --attempting to get my travis@test.com profile id
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/unifiedusermanagement/users/staff/{id}");         //[HttpGet("users/staff/{id}")]
            // Act
            var response = await _client.SendAsync(request);
            // Assert
            response.EnsureSuccessStatusCode();
            //basic output to show pass
            _output.WriteLine("GetStaffUsers_ReturnsSuccessStatusCode passed.");
        }




        [Fact]
        public async Task GetUserById_ReturnsSuccessStatusCode()
        {
            // Arrange
            var jwt = await GetJwtTokenAsync(); // get jwt token
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            var userId = 1; // gonna have to look at user id
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/unifiedusermanagement/users/{userId}");
            // Act
            var response = await _client.SendAsync(request);
            // Assert
            response.EnsureSuccessStatusCode();
            //basic output to show pass
            _output.WriteLine("GetUserById_ReturnsSuccessStatusCode passed.");
        }


    }
}
