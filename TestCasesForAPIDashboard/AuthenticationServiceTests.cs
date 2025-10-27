
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit.Abstractions;

namespace TestCasesForAPIDashboard
{
    public class AuthenticationServiceTests
    {
        //mokcing dependencies
        private readonly Mock<IMongoDBContext> _mockMongoDBContext;
        private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
        private readonly Mock <IPasswordHasher<object>> _mockPasswordHasher;            

        //mocking mongodb collections
        private readonly Mock<IMongoCollection<Staff>> _mockStaffCollection;
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
            _mockAuthenticationCollection = new Mock<IMongoCollection<Authentication>>();
            _mockRoleCollection = new Mock<IMongoCollection<Role>>();

            //setup JWT Settings with test values
            _jwtSettings = new JwtSettings
            {
                SecretKey = "ThisIsATestSecretKeyForJwtTokenGenerationThatIsLongEnough12345",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60,
                RefreshTokenExpiryDays = 7
            };
            _mockJwtSettings.Setup(j => j.Value).Returns(_jwtSettings);

            //atach collections to mocked context
            _mockMongoDBContext.SetupGet(c => c.StaffCollection).Returns(_mockStaffCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(_mockAuthenticationCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.RolesCollection).Returns(_mockRoleCollection.Object);

            //output
            _output = output;
        }

        // ----------------------------
        // Helper Methods
        // ----------------------------

        // Helper method for mocking cursor (reuse from UserServiceTests)
        private static Mock<IAsyncCursor<T>> CreateMockCursor<T>(List<T> data)
        {
            var mockCursor = new Mock<IAsyncCursor<T>>();
            mockCursor.Setup(c => c.Current).Returns(data);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            return mockCursor;
        }

        // Helper for Staff FindAsync setup
        private void SetupFindAsyncStaff(List<Staff> staffList)
        {
            var mockCursor = CreateMockCursor(staffList);
            _mockStaffCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Staff>>(),
                    It.IsAny<FindOptions<Staff, Staff>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        // Helper for Authentication FindAsync setup
        private void SetupFindAsyncAuthentication(List<Authentication> authList)
        {
            var mockCursor = CreateMockCursor(authList);
            _mockAuthenticationCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Authentication>>(),
                    It.IsAny<FindOptions<Authentication, Authentication>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        // Helper for Role FindAsync setup
        private void SetupFindAsyncRole(List<Role> roleList)
        {
            var mockCursor = CreateMockCursor(roleList);
            _mockRoleCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Role>>(),
                    It.IsAny<FindOptions<Role, Role>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        //need login jwt generation and jwt refresh token generation tests here

        // ----------------------------
        // Login Tests
        // ----------------------------

        [Fact]
        public async Task LoginAsync_LoginSuccessfully()
        {
            //Arange
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var staff = new Staff { 
                Id = staffId,
                Email = "jack@test.com",
                FirstName = "Jack",
                Phone = "N/A",
                RoleId = roleId,
                AuthId = authId
            };

            //setup list of staff
            var staffList = new List<Staff> { staff };      //only one staff for this test

            //setup FindAsync for Staff collection
            SetupFindAsyncStaff(staffList);

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
            //setup FindAsync for Authentication collection
            SetupFindAsyncAuthentication(authList);

            //setup role record

            var role = new Role     //leaving description and permissions empty for this test
            {
                Id = roleId,
                Name = "staff"
            };

            var roleList = new List<Role> { role };     //only one role for this test

            //setup FindAsync for Role collection
            SetupFindAsyncRole(roleList);

            //setup password hasher
            _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(It.IsAny<object>(), auth.HashedPassword, "PlainTextPassword"))    
                .Returns(PasswordVerificationResult.Success);

            //setup UpdateOneAsync for refresh token storage

            _mockAuthenticationCollection.Setup(t => t.UpdateOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<UpdateDefinition<Authentication>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));      //simulate 1 record updated)

            //instantiate the service
            //var service = new AuthenticationService(_mockMongoDBContext.Object, _mockJwtSettings.Object, _mockPasswordHasher.Object);

            //Act

            //Assert



        }

    }
}
