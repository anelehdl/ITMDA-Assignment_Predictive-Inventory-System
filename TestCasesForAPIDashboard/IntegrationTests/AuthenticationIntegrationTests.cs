extern alias APIProgram;        //needed to add this according to ms docs, this ensures theres no binding conflicts with API and Dashboard program files
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net;
using Core.Models.DTO;
using System.Net.Http.Headers;

namespace TestCasesForAPIDashboard.IntegrationTests
{

    // Custom factory
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        // Optional overrides go here i need custom configuration for tests
        // (e.g., in-memory DB, mock settings, etc but i dont need them im going to use real mongo db for tests not sure if this best prac)
    }


    public class AuthenticationIntegrationTests : IClassFixture<CustomWebApplicationFactory<APIProgram.Program>>        //explicitly specify APIProgram.Program to avoid ambiguity
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public AuthenticationIntegrationTests(CustomWebApplicationFactory<APIProgram.Program> factory, ITestOutputHelper output)        
        {
            _client = factory.CreateClient();
            _output = output;
        }

        // ----------------------------
        // Integration Tests for /api/auth/login
        // ----------------------------

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccessAndJwt()
        {
            // Arrange
            //needs to be legitimate user in the database for this integration test to work  -- so im using my db and one of the users i setup
            var loginDto = new
            {
                Email = "travis@test.com",          
                Password = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();

            // Debug output this will show user details!
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);       //just comparing 200 OK status codes

            //deserialize and check token
            var result = JsonSerializer.Deserialize<LoginResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

        }
    
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new
            {
                Email = "fakeuser@test.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();

            // Debug output
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);     //should both be 401 unauthorized
        }

        // ----------------------------
        // Protected Endpoint Access Tests
        // ----------------------------

        [Fact]
        public async Task StockMetrics_ProtectedEndpoint_WithValidJwt_ReturnsSuccess()
        {
            // Arrange - get JWT first
            var loginDto = new { Email = "travis@test.com", Password = "123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

            // Act - call protected endpoint
            var response = await _client.GetAsync("/api/StockMetrics/overview"); // gonna use stock metrics overview endpoint as protected endpoint
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _output.WriteLine($"Status Code: {response.StatusCode}");
            _output.WriteLine($"Response: {content}");
        }

        [Fact]
        public async Task StockMetrics_ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized()
        {
            // Ensure no Authorization header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/StockMetrics/overview");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _output.WriteLine($"Status Code: {response.StatusCode}");
        }

        [Fact]      //failing due to client logic
        public async Task StockMetrics_ProtectedEndpoint_WithJwtWrongRole_ReturnsForbidden()
        {
            // Suppose you have a login user with role "client" but endpoint requires "admin" 
            var loginDto = new { UserEmail = "C001@test.com", Password = "123" };     //my client db doesnt have pw so i needa check with aneleh about that, not sure how clients setup their profiles if they can even login to dashboard
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Act
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", loginResult.Token);

            var response = await _client.GetAsync("/api/StockMetrics/overview");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _output.WriteLine($"Status Code: {response.StatusCode}");
        }



        // ----------------------------
        // Refresh Token Integration Test
        // ----------------------------
        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsNewAccessToken()
        {
            // Arrange
            var loginDto = new
            {
                Email = "travis@test.com",      //existing user in db
                Password = "123"
            };

            //Act - login to try get the tokens
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();

            _output.WriteLine($"Login Status Code: {loginResponse.StatusCode}");
            _output.WriteLine($"Login Response: {loginContent}");

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            // Deserialize login response
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(loginResult);
            Assert.False(string.IsNullOrEmpty(loginResult.RefreshToken));

            var refreshToken = loginResult.RefreshToken;

            // Act - call refresh endpoint

            var refreshDto = new
            {
                RefreshToken = refreshToken
            };

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);
            var refreshContent = await refreshResponse.Content.ReadAsStringAsync();

            _output.WriteLine($"Refresh Status Code: {refreshResponse.StatusCode}");
            _output.WriteLine($"Refresh Response: {refreshContent}");


            //Assert

            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            var refreshResult = JsonSerializer.Deserialize<LoginResponseDto>(refreshContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(refreshResult);
            Assert.False(string.IsNullOrEmpty(refreshResult.Token));
            Assert.False(string.IsNullOrEmpty(refreshResult.RefreshToken));
            Assert.NotEqual(refreshToken, refreshResult.RefreshToken); // new refresh token should be rotated

            _output.WriteLine($"New JWT Token: {refreshResult.Token}");
            _output.WriteLine($"New Refresh Token: {refreshResult.RefreshToken}");
        }

        [Fact]
        public async Task Logout_RevokesRefreshToken_PreventsFurtherRefresh()
        {
             
            // Arrange - Login to get refresh token
            var loginDto = new
            {
                Email = "travis@test.com",   // exisiting user in db
                Password = "123"
            };

            // Act - login to get tokens
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();

            _output.WriteLine($"Login Status Code: {loginResponse.StatusCode}");
            _output.WriteLine($"Login Response: {loginContent}");

            //Assert login success
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            // Deserialize login response
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            //Assert tokens present
            Assert.NotNull(loginResult);
            Assert.False(string.IsNullOrEmpty(loginResult.RefreshToken));

            // Get refresh token
            var refreshToken = loginResult.RefreshToken;

            //Arrange - Logout to revoke refresh token
            var logoutDto = new
            {
                RefreshToken = refreshToken
            };
            // Act - call logout endpoint
            var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutDto);
            var logoutContent = await logoutResponse.Content.ReadAsStringAsync();

            _output.WriteLine($"Logout Status Code: {logoutResponse.StatusCode}");
            _output.WriteLine($"Logout Response: {logoutContent}");

            // Expecting 200 OK after successful logout
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

            // Act - attempt to refresh with the old refresh token
            var refreshDto = new
            {
                RefreshToken = refreshToken
            };

            // Act - call refresh endpoint
            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);
            var refreshContent = await refreshResponse.Content.ReadAsStringAsync();

            _output.WriteLine($"Refresh After Logout Status Code: {refreshResponse.StatusCode}");
            _output.WriteLine($"Refresh After Logout Response: {refreshContent}");

            // Assert that old refresh token is invalid
            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode); // thinking its 401 Unauthorized
        }

    }
}
