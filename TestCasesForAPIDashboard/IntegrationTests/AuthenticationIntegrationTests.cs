extern alias APIProgram;        //needed to add this according to ms docs, this ensures theres no binding conflicts with API and Dashboard program files
using Core.Models.DTO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

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
        private readonly JwtSettings _jwtSettings;

        public AuthenticationIntegrationTests(CustomWebApplicationFactory<APIProgram.Program> factory, ITestOutputHelper output)        
        {
            _client = factory.CreateClient();
            _output = output;
            
            //manually need to load the JWT Settings 

            _jwtSettings = new JwtSettings          //i have to hardcode the values here to match the API settings for the tests to work, i tried using configuration but couldnt get it to work
            {
                SecretKey = "SuperSecretSecureKeyThatLooksAwesomeAndVeryLongTHISNEEDStobe512BitsIthinkInOtherWords64CharsroundsoImJustWritingforFunAlsoItCanBeGreaterThan512bitsAsFarAsIKnow",
                Issuer = "PrototypeAPI",
                Audience = "PrototypeDashboard"
            };

        }

        // ----------------------------
        // Helper Method for the JWT Token Generation for the mocked client
        // ----------------------------
        private string GenerateTestJwtToken(string role)
        {
            // Use the same secret and issuer as your API for test consistency
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "C009-Testing"),
                new Claim(ClaimTypes.Email, "client@test.com"),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken
            (
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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


        [Fact]      //failing due to client logic       --update bypassed this by generating a jwt token manually for client role, specifically for the test
        public async Task UnifiedUserManagement_ProtectedEndpoint_WithJwtWrongRole_ReturnsForbidden()
        {
            //Arrange
            /*  --update since my db doesnt have user passwords for the clients setup in this build, im going to just generate a jwt token for a client role manually and use that instead of loggin in as a client
            // Suppose you have a login user with role "client" but endpoint requires "admin" 
            var loginDto = new              //my client db doesnt have pw so i needa check with aneleh about that, not sure how clients setup their profiles if they can even login to dashboard        
            {                    
                UserEmail = "C001@test.com", 
                Password = "123" 
            };

            // Act - login to get JWT
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();

            // Deserialize login response
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            */

            // Manually create a JWT token for a user with "client" role
            var token = GenerateTestJwtToken("client");         // Implement the helper to create a valid JWT

            // Act
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);         //using the token generated above

            // Call protected endpoint
            var response = await _client.GetAsync("/api/unifiedusermanagement/users");

            // Assert       //should be forbidden since client role not allowed
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
