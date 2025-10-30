using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace TestCasesForAPIDashboard.UnitTests
{
    public class AuthenticationServiceTests
    {
        //mokcing dependencies
        private readonly Mock<IMongoDBContext> _mockMongoDBContext;
        private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
        private readonly Mock<IPasswordHasher<object>> _mockPasswordHasher;

        //mocking mongodb collections
        private readonly Mock<IMongoCollection<Staff>> _mockStaffCollection;
        private readonly Mock<IMongoCollection<Client>> _mockClientCollection;
        private readonly Mock<IMongoCollection<Authentication>> _mockAuthenticationCollection;
        private readonly Mock<IMongoCollection<Role>> _mockRoleCollection;

        //testing output
        private readonly ITestOutputHelper _output;

        //JWT settings for testing
        private readonly JwtSettings _jwtSettings;
        public AuthenticationServiceTests(ITestOutputHelper output)
        {
            //initialize mocks
            _mockMongoDBContext = new Mock<IMongoDBContext>();
            _mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            _mockPasswordHasher = new Mock<IPasswordHasher<object>>();

            _mockStaffCollection = new Mock<IMongoCollection<Staff>>();
            _mockClientCollection = new Mock<IMongoCollection<Client>>();
            _mockAuthenticationCollection = new Mock<IMongoCollection<Authentication>>();
            _mockRoleCollection = new Mock<IMongoCollection<Role>>();

            //setup JWT Settings with test values
            _jwtSettings = new JwtSettings
            {
                SecretKey = "SuperSecretSecureKeyThatLooksAwesomeAndVeryLongTHISNEEDStobe512BitsIthinkInOtherWords64CharsroundsoImJustWritingforFunAlsoItCanBeGreaterThan512bitsAsFarAsIKnow",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 5,
                RefreshTokenExpiryDays = 7
            };
            _mockJwtSettings.Setup(j => j.Value).Returns(_jwtSettings);

            //atach collections to mocked context
            _mockMongoDBContext.SetupGet(c => c.StaffCollection).Returns(_mockStaffCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.ClientCollection).Returns(_mockClientCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(_mockAuthenticationCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.RolesCollection).Returns(_mockRoleCollection.Object);


            //output
            _output = output;
        }

        // ----------------------------
        // Helper Methods
        // ----------------------------

        private static Mock<IAsyncCursor<T>> CreateMockCursor<T>(List<T> data)
        {
            var mockCursor = new Mock<IAsyncCursor<T>>();
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                      .Returns(true)
                      .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true)
                      .ReturnsAsync(false);
            mockCursor.SetupGet(_ => _.Current).Returns(data);
            return mockCursor;
        }


        // Helper for Staff Find().FirstOrDefaultAsync() setup (mock IFindFluent)
        private void SetupFindStaff(Staff staff)
        {
            var list = staff != null ? new List<Staff> { staff } : new List<Staff>();
            var mockCursor = CreateMockCursor(list);

            _mockStaffCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Staff>>(),
                    It.IsAny<FindOptions<Staff, Staff>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        // Helper for Client Find().FirstOrDefaultAsync()
        private void SetupFindClient(Client client)
        {
            var list = client != null ? new List<Client> { client } : new List<Client>();
            var mockCursor = CreateMockCursor(list);

            _mockClientCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Client>>(),
                    It.IsAny<FindOptions<Client, Client>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        // Helper for Authentication Find().FirstOrDefaultAsync()
        private void SetupFindAuthentication(Authentication auth)
        {
            var list = auth != null ? new List<Authentication> { auth } : new List<Authentication>();
            var mockCursor = CreateMockCursor(list);

            _mockAuthenticationCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Authentication>>(),
                    It.IsAny<FindOptions<Authentication, Authentication>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        // Helper for Role Find().FirstOrDefaultAsync()
        private void SetupFindRole(Role role)
        {
            var list = role != null ? new List<Role> { role } : new List<Role>();
            var mockCursor = CreateMockCursor(list);

            _mockRoleCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Role>>(),
                    It.IsAny<FindOptions<Role, Role>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }


        // ----------------------------
        // Login Tests
        // ----------------------------

        // ----------------------------
        // Staff Login Tests - Happy and Negative Paths
        // ----------------------------

        [Fact]
        public async Task LoginAsync_LoginSuccessfully_Staff()
        {
            //Arange
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var staff = new Staff
            {
                Id = staffId,
                Email = "jack@test.com",
                FirstName = "Jack",
                Phone = "N/A",
                RoleId = roleId,
                AuthId = authId
            };


            // Setup Find for Staff collection
            SetupFindStaff(staff);

            // Setup Client collection to return null (not found)
            SetupFindClient(null);

            //setup authentication record
            var auth = new Authentication
            {
                Id = authId,
                AuthID = Guid.NewGuid().ToString(),     //random authID
                HashedPassword = "HashedPassword",   //dummy hashed password
                Salt = string.Empty,            //removed due to legacy reasons
                RefreshTokens = new List<RefreshToken>()        //empty for this test
            };


            var authList = new List<Authentication> { auth };   //only one auth record for this test

            // Setup Find for Authentication collection
            SetupFindAuthentication(auth);

            //setup role record
            var role = new Role     //leaving description and permissions empty for this test
            {
                Id = roleId,
                Name = "staff"
            };

            // Setup Find for Role collection
            SetupFindRole(role);


            //setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(It.IsAny<object>(), auth.HashedPassword, "PlainTextPassword"))
                .Returns(PasswordVerificationResult.Success);

            //setup UpdateOneAsync for refresh token storage
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.ModifiedCount).Returns(1);

            _mockAuthenticationCollection.Setup(t => t.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Authentication>>(),
                    It.IsAny<UpdateDefinition<Authentication>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUpdateResult.Object);

            //instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            //Act
            var result = await service.LoginAsync("jack@test.com", "PlainTextPassword");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
            Assert.Equal("jack@test.com", result.Email);
            Assert.Equal("Jack", result.FirstName);
            Assert.Equal("staff", result.Role);
            Assert.Equal(staffId.ToString(), result.UserId);

            // Verify refresh token updated in DB
            _mockAuthenticationCollection.Verify(t => t.UpdateOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<UpdateDefinition<Authentication>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Output debug info as i cant seem to find the error.     ---omw it was the jwt secret not being long enough
            _output.WriteLine($"Staff found: {staff != null}");
            _output.WriteLine($"Client found: {false}");
            _output.WriteLine($"Authentication record found: {auth != null}");
            _output.WriteLine($"Password verification result: {PasswordVerificationResult.Success}");
            _output.WriteLine($"Role found: {role != null}");
            _output.WriteLine($"Result Success: {result.Success}");
            _output.WriteLine($"Result Message: {result.Message}");
            _output.WriteLine($"UserId: {result.UserId}");
            _output.WriteLine($"Email: {result.Email}");
            _output.WriteLine($"FirstName: {result.FirstName}");
            _output.WriteLine($"Role: {result.Role}");
            _output.WriteLine($"JWT Token: {result.Token}");
            _output.WriteLine($"Refresh Token: {result.RefreshToken}");
        }



        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFailure_Staff()
        {
            // Arrange
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var staff = new Staff
            {
                Id = staffId,
                Email = "jack@test.com",
                FirstName = "Jack",
                RoleId = roleId,
                AuthId = authId
            };

            SetupFindStaff(staff);
            SetupFindClient(null);

            var auth = new Authentication
            {
                Id = authId,
                HashedPassword = "HashedPassword",
                RefreshTokens = new List<RefreshToken>()
            };

            SetupFindAuthentication(auth);

            // Setup password hasher to fail
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(
                    It.IsAny<object>(),
                    auth.HashedPassword,
                    "WrongPassword"))
                .Returns(PasswordVerificationResult.Failed);

            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.LoginAsync("jack@test.com", "WrongPassword");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid email or password", result.Message);

            //output debug info just to be sure
            _output.WriteLine("Staff found: " + (staff != null));
            _output.WriteLine("Client found: " + false); // we returned null
            _output.WriteLine("Authentication record found: " + (auth != null));
            _output.WriteLine("Password verification result: Failed");
            _output.WriteLine("Result Success: " + result.Success);
            _output.WriteLine("Result Message: " + result.Message);
            _output.WriteLine("UserId: " + result.UserId);
            _output.WriteLine("Email: " + result.Email);
            _output.WriteLine("FirstName: " + result.FirstName);
            _output.WriteLine("Role: " + (roleId != ObjectId.Empty ? "staff" : "Unknown"));
            _output.WriteLine("JWT Token: " + result.Token);
            _output.WriteLine("Refresh Token: " + result.RefreshToken);
        }

        // ----------------------------
        // Client Login Tests - Positive and Negative Paths
        // ----------------------------

        [Fact]
        public async Task LoginAsync_LoginSuccessfully_Client()
        {
            //Arange
            var clientId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var client = new Client
            {
                Id = clientId,
                UserCode = "C001",
                Username = "firstclient",
                UserEmail = "firstclient@test.com",
                RoleId = roleId,
                AuthId = authId
            };


            // Setup Find for client collection
            SetupFindClient(client);

            // Setup Staff collection to return null (not found)
            SetupFindStaff(null);

            //setup authentication record
            var auth = new Authentication
            {
                Id = authId,
                AuthID = Guid.NewGuid().ToString(),     //random authID
                HashedPassword = "HashedPassword",   //dummy hashed password
                Salt = string.Empty,            //removed due to legacy reasons
                RefreshTokens = new List<RefreshToken>()        //empty for this test
            };


            var authList = new List<Authentication> { auth };   //only one auth record for this test

            // Setup Find for Authentication collection
            SetupFindAuthentication(auth);

            //setup role record
            var role = new Role     //leaving description and permissions empty for this test
            {
                Id = roleId,
                Name = "client"
            };

            // Setup Find for Role collection
            SetupFindRole(role);


            //setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(It.IsAny<object>(), auth.HashedPassword, "PlainTextPassword"))
                .Returns(PasswordVerificationResult.Success);

            //setup UpdateOneAsync for refresh token storage
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.ModifiedCount).Returns(1);

            _mockAuthenticationCollection.Setup(t => t.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Authentication>>(),
                    It.IsAny<UpdateDefinition<Authentication>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUpdateResult.Object);

            //instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            //Act
            var result = await service.LoginAsync("firstclient@test.com", "PlainTextPassword");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
            Assert.Equal(client.Username, result.FirstName); // matches Username
            Assert.Equal(client.UserEmail, result.Email);    // matches UserEmail
            Assert.Equal("client", result.Role);
            Assert.Equal(clientId.ToString(), result.UserId);

            // Verify refresh token updated in DB
            _mockAuthenticationCollection.Verify(t => t.UpdateOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<UpdateDefinition<Authentication>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Output debug info as i cant seem to find the error.     ---omw it was the jwt secret not being long enough
            _output.WriteLine($"Staff found: {false}");
            _output.WriteLine($"Client found: {client != null}");
            _output.WriteLine($"Authentication record found: {auth != null}");
            _output.WriteLine($"Password verification result: {PasswordVerificationResult.Success}");
            _output.WriteLine($"Role found: {role != null}");
            _output.WriteLine($"Result Success: {result.Success}");
            _output.WriteLine($"Result Message: {result.Message}");
            _output.WriteLine($"UserId: {result.UserId}");
            _output.WriteLine($"Email: {result.Email}");
            _output.WriteLine($"UserName: {result.FirstName}");
            _output.WriteLine($"Role: {result.Role}");
            _output.WriteLine($"JWT Token: {result.Token}");
            _output.WriteLine($"Refresh Token: {result.RefreshToken}");
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsFailure_Client()
        {
            // Arrange
            var clientId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var client = new Client
            {
                Id = clientId,
                UserCode = "C001",
                UserEmail = "firstclient@test.com",
                Username = "FirstClient",
                RoleId = roleId,
                AuthId = authId
            };

            SetupFindStaff(null);
            SetupFindClient(client);

            var auth = new Authentication
            {
                Id = authId,
                HashedPassword = "HashedPassword",
                RefreshTokens = new List<RefreshToken>()
            };

            SetupFindAuthentication(auth);

            // Setup password hasher to fail
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(
                    It.IsAny<object>(),
                    auth.HashedPassword,
                    "WrongPassword"))
                .Returns(PasswordVerificationResult.Failed);

            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.LoginAsync("firstclient@test.com", "WrongPassword");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid email or password", result.Message);

            //output debug info just to be sure
            _output.WriteLine("Staff found: " + false);
            _output.WriteLine("Client found: " + (client != null));
            _output.WriteLine("Authentication record found: " + (auth != null));
            _output.WriteLine("Password verification result: Failed");
            _output.WriteLine("Result Success: " + result.Success);
            _output.WriteLine("Result Message: " + result.Message);
            _output.WriteLine("UserId: " + result.UserId);
            _output.WriteLine("Email: " + result.Email);
            _output.WriteLine("Username: " + result.FirstName);
            _output.WriteLine("Role: " + (roleId != ObjectId.Empty ? "staff" : "Unknown"));
            _output.WriteLine("JWT Token: " + result.Token);
            _output.WriteLine("Refresh Token: " + result.RefreshToken);
        }



        // ----------------------------
        // Generic user not found test
        // ----------------------------

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsFailure_Staff()
        {
            // Arrange
            // Setup Staff and Client collections to return null (not found)
            SetupFindStaff(null);
            SetupFindClient(null);

            //dont need to setup auth and role as user not found
            //instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.LoginAsync("N/A@test.com", "Password");

            // Output debug info
            _output.WriteLine($"Staff found: {false}");
            _output.WriteLine($"Client found: {false}");
            _output.WriteLine($"Result Success: {result.Success}");
            _output.WriteLine($"Result Message: {result.Message}");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Invalid email or password", result.Message);
        }


        // ----------------------------
        // JWT Specific Tests
        // ----------------------------
        [Fact]
        public async Task LoginAsync_JwtTokenContainsCorrectClaims_Staff()
        {
            // Arrange
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            // Arrange staff
            var staff = new Staff
            {
                Id = staffId,
                Email = "jwt@test.com",
                FirstName = "TestStaff",
                RoleId = roleId,
                AuthId = authId
            };

            // Setup Find for Staff collection
            SetupFindStaff(staff);
            // Setup Client collection to return null (not found)
            SetupFindClient(null);
            // Setup Authentication
            var auth = new Authentication
            {
                Id = authId,
                HashedPassword = "HashedPassword",
                RefreshTokens = new List<RefreshToken>()
            };
            SetupFindAuthentication(auth);
            // Setup Role
            var role = new Role
            {
                Id = roleId,
                Name = "staff"
            };
            SetupFindRole(role);
            // Setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(
                    It.IsAny<object>(),
                    auth.HashedPassword,
                    "PlainTextPassword"))
                .Returns(PasswordVerificationResult.Success);
            // Instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.LoginAsync("test@test.com", "PlainTextPassword");
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            // Validate JWT token claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);
            var claims = jwtToken.Claims;
            // Check expected claims
            Assert.Equal(staffId.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("jwt@test.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal("staff", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);

            //output debug info
            _output.WriteLine("JWT Claims:");
            foreach (var claim in claims)
            {
                _output.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }
        }

        [Fact]
        public async Task LoginAsync_JwtTokenContainsCorrectClaims_Client()
        {
            // Arrange
            var clientId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();
            // Arrange client
            var client = new Client
            {
                Id = clientId,
                UserCode = "C001",
                UserEmail = "jwt@test.com",
                Username = "TestClient",
                RoleId = roleId,
                AuthId = authId
            };

            // Setup Find for Client collection
            SetupFindClient(client);
            // Setup Staff collection to return null (not found)
            SetupFindStaff(null);
            // Setup Authentication
            var auth = new Authentication
            {
                Id = authId,
                HashedPassword = "HashedPassword",
                RefreshTokens = new List<RefreshToken>()
            };
            SetupFindAuthentication(auth);
            // Setup Role
            var role = new Role
            {
                Id = roleId,
                Name = "client"
            };
            SetupFindRole(role);
            // Setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(
                    It.IsAny<object>(),
                    auth.HashedPassword,
                    "PlainTextPassword"))
                .Returns(PasswordVerificationResult.Success);
            // Instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.LoginAsync("test@test.com", "PlainTextPassword");
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            // Validate JWT token claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);
            var claims = jwtToken.Claims;
            // Check expected claims
            Assert.Equal(clientId.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("jwt@test.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal("client", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);

            //output debug info
            _output.WriteLine("JWT Claims:");
            foreach (var claim in claims)
            {
                _output.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }
        }




        [Fact]
        public async Task LoginAsync_JwtRefreshTokenGeneration_WorksCorrectly_Staff()
        {
            // Arrange
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();
            // Arrange staff
            var staff = new Staff
            {
                Id = staffId,
                Email = "Jwt@test.com",
                FirstName = "TestStaff",
                RoleId = roleId,
                AuthId = authId
            };

            // Setup Find for Staff collection
            SetupFindStaff(staff);
            // Setup Client collection to return null (not found)
            SetupFindClient(null);
            // Setup Authentication
            // Create a refresh token that is valid
            var refreshToken = "ExistingRefreshToken";
            var hashedRefreshToken = Convert.ToBase64String(SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(refreshToken)));

            var auth = new Authentication
            {
                Id = authId,
                RefreshTokens = new List<RefreshToken>
                {   
                    new RefreshToken
                    {
                        TokenId = Guid.NewGuid().ToString(),
                        TokenHash = hashedRefreshToken,
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
            //setup Find for Authentication collection
            SetupFindAuthentication(auth);

            // Setup Role
            var role = new Role
            {
                Id = roleId,
                Name = "staff"
            };
            SetupFindRole(role);
            // Setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(
                    It.IsAny<object>(),
                    auth.HashedPassword,
                    "PlainTextPassword"))
                .Returns(PasswordVerificationResult.Success);

            // Setup UpdateOneAsync mocks for pull and push
            _mockAuthenticationCollection
                .Setup(a => a.UpdateOneAsync(It.IsAny<FilterDefinition<Authentication>>(),
                                             It.IsAny<UpdateDefinition<Authentication>>(),
                                             It.IsAny<UpdateOptions>(),
                                             It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<UpdateResult>());

            // Instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);
            // Act
            var (newJwt, newRefresh) = await service.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.False(string.IsNullOrEmpty(newJwt));
            Assert.False(string.IsNullOrEmpty(newRefresh));
            Assert.NotEqual(refreshToken, newRefresh);

            _output.WriteLine("=== REFRESH TOKEN SUCCESS ===");
            _output.WriteLine($"Old Refresh Token: {refreshToken}");
            _output.WriteLine($"New JWT: {newJwt}");
            _output.WriteLine($"New Refresh Token: {newRefresh}");
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ThrowsException()
        {
            // Arrange
            var authId = ObjectId.GenerateNewId();
            var refreshToken = "ExpiredToken";
            var hashedToken = Convert.ToBase64String(SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(refreshToken)));

            var auth = new Authentication
            {
                Id = authId,
                RefreshTokens = new List<RefreshToken>
        {
            new RefreshToken
            {
                TokenId = Guid.NewGuid().ToString(),
                TokenHash = hashedToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // expired
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        }
            };

            SetupFindAuthentication(auth);
            //instantiate the service
            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            // Act
            Exception ex = await Assert.ThrowsAsync<SecurityTokenException>(() => service.RefreshTokenAsync(refreshToken));

            // Assert
            _output.WriteLine("=== REFRESH TOKEN EXPIRED ===");
            _output.WriteLine($"Refresh Token: {refreshToken}");
            _output.WriteLine($"Exception Message: {ex.Message}");
        }


        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ThrowsException()
        {
            // Arrange
            var refreshToken = "InvalidToken";
            SetupFindAuthentication(null); // auth record not found

            var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);


            // Act
            Exception ex = await Assert.ThrowsAsync<SecurityTokenException>(() => service.RefreshTokenAsync(refreshToken));

            // Assert
            _output.WriteLine("=== REFRESH TOKEN EXPIRED ===");
            _output.WriteLine($"Refresh Token: {refreshToken}");
            _output.WriteLine($"Exception Message: {ex.Message}");
        }

    }
}

