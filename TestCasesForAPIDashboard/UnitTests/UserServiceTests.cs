
using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace TestCasesForAPIDashboard.UnitTests
{
    public class UserServiceTests       //this is where we will be testing the UnifiedUserService
    {
        //mokcing dependencies
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IPasswordHasher<object>> _mockPasswordHasher;
        private readonly Mock<IMongoDBContext> _mockMongoDBContext;     //use the interface for mocking
        //private readonly UnifiedUserService _userService;     //dont think its needed atm
        //mocking the collections for staff and client
        private readonly Mock<IMongoCollection<Staff>> _mockStaffCollection;
        private readonly Mock<IMongoCollection<Client>> _mockClientCollection;
        //testing output to get values during test runs
        private readonly ITestOutputHelper _output;

        public UserServiceTests(ITestOutputHelper output)       //needs to be injected via contructor
        {
            //initialize mocks
            _mockRoleService = new Mock<IRoleService>();
            _mockPasswordHasher = new Mock<IPasswordHasher<object>>();      //need to specify the type of password hasher
            _mockMongoDBContext = new Mock<IMongoDBContext>();              //updated to use the interface instead of the concrete class
            //_userService = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            _mockStaffCollection = new Mock<IMongoCollection<Staff>>();
            _mockClientCollection = new Mock<IMongoCollection<Client>>();

            //attach the fake collections to the mocked context
            _mockMongoDBContext.SetupGet(c => c.StaffCollection).Returns(_mockStaffCollection.Object);
            _mockMongoDBContext.SetupGet(c => c.ClientCollection).Returns(_mockClientCollection.Object);

            //output
            _output = output;       //store injected instance
        }

        // ----------------------------
        // Helper Methods
        // ----------------------------
        //adding helper method for mocking cursor
        private static Mock<IAsyncCursor<T>> CreateMockCursor<T>(List<T> data)
        {
            var mockCursor = new Mock<IAsyncCursor<T>>();
            mockCursor.Setup(c => c.Current).Returns(data);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            return mockCursor;
        }

        //creating reusable helper for FindAsync setup
        //staff
        private void SetupFindAsyncStaff(List<Staff> staffList)
        {
            var mockStaffCursor = CreateMockCursor(staffList);
            _mockStaffCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Staff>>(),
                    It.IsAny<FindOptions<Staff, Staff>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockStaffCursor.Object);
        }
        //client
        private void SetupFindAsyncClient(List<Client> clientList)
        {
            var mockClientCursor = CreateMockCursor(clientList);
            _mockClientCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Client>>(),
                    It.IsAny<FindOptions<Client, Client>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockClientCursor.Object);
        }



        // ----------------------------
        // GetAllUsersAsync
        // ----------------------------
        [Fact]      //needed for xunit test method
        public async Task GetAllUsersAsync_ReturnsCombinedList()        //should return combined list of staff and clients  --need to add this to name of test for better clarity and practice
        {
            //populate list for staff using in-memory data
            var staffList = new List<Staff>
            {
                new Staff { Id = ObjectId.GenerateNewId(), FirstName = "John Johnson", Email = "JJ@test.com", Phone = "N/A", RoleId = ObjectId.GenerateNewId()  },
                new Staff { Id = ObjectId.GenerateNewId(), FirstName = "Jack Jackson", Email = "JackJack@test.com", Phone = "N/A", RoleId = ObjectId.GenerateNewId() }
            };

            //populate list for clients using in-memory data
            var clientList = new List<Client>
            {
                new Client { Id = ObjectId.GenerateNewId(), UserCode = "C001", Username = "ClientOne", RoleId = ObjectId.GenerateNewId() },
                new Client { Id = ObjectId.GenerateNewId(), UserCode = "C002", Username = "ClientTwo", RoleId = ObjectId.GenerateNewId() }
            };

            //create fake find() results for staff      //this mimics the find interfaces .Find.ToListAsync()
            /*
            var mockStaffFind = new Mock<IFindFluent<Staff, Staff>>();
            mockStaffFind.Setup(f => f.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(staffList);     //error here

            //create fake find() results for clients
            var mockClientFind = new Mock<IFindFluent<Client, Client>>();
            mockClientFind.Setup(f => f.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(clientList);

            //make collection.Find() return our mock find object
            
            _mockStaffCollection.Setup(c => c.Find(It.IsAny<FilterDefinition<Staff>>(), null)).Returns(mockStaffFind.Object);
            _mockClientCollection.Setup(c => c.Find(It.IsAny<FilterDefinition<Client>>(), null)).Returns(mockClientFind.Object);
            */
            //amm getting an error, as Moq cannot mock extension methods because they're not actually part of the interface
            /*Message: 
            
            System.NotSupportedException : Unsupported expression: f => f.ToListAsync<Staff>(It.IsAny<CancellationToken>())
            Extension methods(here: IAsyncCursorSourceExtensions.ToListAsync) may not be used in setup / verification expressions.

            this is due to:
            var staffMembers = await _context.StaffCollection.Find(_ => true).ToListAsync();

            Find returns an IAsyncCursor<Staff> which is like an iterator
            ToListAsync is an extension method that operates on IAsyncCursor<Staff>

            so solution would be to mock the iterator itself instead of trying to mock the extension method

            */
            /*
            //create fake async cursor for staff        //this should fix the errors i was getting above -  refactored into helper method
            var mockStaffCursor = new Mock<IAsyncCursor<Staff>>();          //mock the cursor directly    
            mockStaffCursor.Setup(c => c.Current).Returns(staffList);       //setup the data to return
            mockStaffCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))       //sequential calls to next async move
                .ReturnsAsync(true)      //first call returns true ie contains data
                .ReturnsAsync(false);        //2nd call returns false ie no more data

            //create fake async cursor for clients
            var mockClientCursor = new Mock<IAsyncCursor<Client>>();
            mockClientCursor.Setup(c => c.Current).Returns(clientList);
            mockClientCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            */

            //using helper method to create mock cursors
            var mockStaffCursor = CreateMockCursor(staffList);
            var mockClientCursor = CreateMockCursor(clientList);


            //make collection.Find() return our mock cursor
            /*
            _mockStaffCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Staff>>(),
                    It.IsAny<FindOptions<Staff, Staff>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockStaffCursor.Object);

            _mockClientCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Client>>(),
                    It.IsAny<FindOptions<Client, Client>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockClientCursor.Object);
            */
            //refactored above using helper methods
            SetupFindAsyncStaff(staffList);
            SetupFindAsyncClient(clientList);

            //mock role service to return role names        //mimic role lookups
            /*
            _mockRoleService.SetupSequence(r => r.GetRoleByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Role { Id = ObjectId.GenerateNewId(), Name = "Admin" })
                .ReturnsAsync(new Role { Id = ObjectId.GenerateNewId(), Name = "Client" });
            */
            _mockRoleService
                .Setup(r => r.GetRoleByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Role { Id = ObjectId.GenerateNewId(), Name = "Admin" });

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            //act
            var result = await service.GetAllUsersAsync();      //real method call

            //assert            //verify results with contains and linq
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);      //2 staff + 2 clients
            //staff
            Assert.Contains(result, u => u.UserType == "Staff" && u.FirstName == "John Johnson");
            Assert.Contains(result, u => u.UserType == "Staff" && u.FirstName == "Jack Jackson");
            //clients
            Assert.Contains(result, u => u.UserType == "Client" && u.UserCode == "C001");
            Assert.Contains(result, u => u.UserType == "Client" && u.UserCode == "C002");

            //output results for verification
            _output.WriteLine($"Total users returned: {result.Count}");
            foreach (var u in result)
                _output.WriteLine($"{u.UserType}: {u.FirstName ?? u.Username}");

            //verify that we looked up roles for each user
            _mockRoleService.Verify(r => r.GetRoleByIdAsync(It.IsAny<string>()), Times.Exactly(4));

        }

        // ----------------------------
        // GetUserByIdAsync
        // ----------------------------

        [Fact]
        public async Task GetUserByIdAsync_StaffExists_ReturnsUser()
        {
            // Arrange      setup mock data
            var staffId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();

            var staffMember = new Staff
            {
                Id = staffId,
                FirstName = "John Johnson",
                Email = "JJ@test.com",
                Phone = "N/A",
                RoleId = roleId
            };
            //setup list of staff memebers
            var staffList = new List<Staff> { staffMember };

            //using helper method to create mock cursor
            var mockStaffCursor = CreateMockCursor(staffList);

            //setup find to return the mock cursor
            //refactored to use helper method
            SetupFindAsyncStaff(staffList);
            //setup role service to return a role
            _mockRoleService.Setup(r => r.GetRoleByIdAsync(roleId.ToString()))
                .ReturnsAsync(new Role { Id = roleId, Name = "Admin" });

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.GetUserByIdAsync(staffId.ToString(), "Staff");

            // Assert
            Assert.NotNull(result);
            //can use equal to check if equal
            Assert.Equal(staffId.ToString(), result.Id);
            Assert.Equal(staffMember.FirstName, result.FirstName);
            Assert.Equal(staffMember.Email, result.Email);
            Assert.Equal(staffMember.Phone, result.Phone);
            Assert.Equal("Staff", result.UserType);
            Assert.Equal(roleId.ToString(), result.RoleId);     //not sure

            //output for verification
            _output.WriteLine($"User found: {result.FirstName}, Email: {result.Email}, RoleId: {result.RoleId}");

            //verify role lookup
            _mockRoleService.Verify(r => r.GetRoleByIdAsync(roleId.ToString()), Times.Once);
        }

        // ----------------------------
        // CreateStaffUserAsync
        // ----------------------------

        [Fact]
        public async Task CreateStaffUserAsync_CreatesStaffSuccessfully()
        {
            var roleId = ObjectId.GenerateNewId();
            var emptyStaffList = new List<Staff>();      //no existing staff

            // Arrange
            var newStaff = new CreateStaffDto
            {
                FirstName = "Jack Jackson",
                Email = "Jack@test.com",
                Password = "PlainTextPassword",
                Phone = "N/A",
                RoleId = roleId.ToString()
            };

            //setup role service to return a role
            _mockRoleService.Setup(r => r.GetRoleByIdAsync(roleId.ToString()))
                .ReturnsAsync(new Role { Id = roleId, Name = "Staff" });


            //using helper method to create mock cursor
            SetupFindAsyncStaff(emptyStaffList);

            //setup password hasher to return a hashed password
            _mockPasswordHasher
                .Setup(h => h.HashPassword(It.IsAny<object>(), newStaff.Password))
                .Returns("HashedPassword");

            //setup mock authentication collection InsertOneAsync
            var mockAuthCollection = new Mock<IMongoCollection<Authentication>>();
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(mockAuthCollection.Object);

            mockAuthCollection.Setup(a => a.InsertOneAsync(
                It.IsAny<Authentication>(),
                null,
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            //setup mock staff collection InsertOneAsync
            _mockStaffCollection.Setup(s => s.InsertOneAsync(
                It.IsAny<Staff>(),
                null,
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.CreateStaffUserAsync(newStaff);
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            //verify interactions

            _mockRoleService.Verify(r => r.GetRoleByIdAsync(roleId.ToString()), Times.Once);
            _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<object>(), newStaff.Password), Times.Once);
            _mockStaffCollection.Verify(s => s.InsertOneAsync(It.IsAny<Staff>(), null, It.IsAny<CancellationToken>()), Times.Once);

            //output for verification
            _output.WriteLine($"New staff user created with Name: {newStaff.FirstName}\n" +
                $"Email: {newStaff.Email}\nPhone: {newStaff.Phone}\nRoleId: {newStaff.RoleId}");

        }


        // ----------------------------
        // CreateClientUserAsync Test
        // ----------------------------

        [Fact]
        public async Task CreateClientUserAsync_CreatesClientSuccessfully()
        {
            var roleId = ObjectId.GenerateNewId();
            var emptyClientList = new List<Client>();      //no existing clients
            // Arrange
            var newClient = new CreateClientDto
            {
                UserCode = "C003",
                Username = "ClientThree",
                Password = "PlainTextPassword"

            };
            //setup role service to return a role
            _mockRoleService.Setup(r => r.GetRoleByNameAsync("client"))
                .ReturnsAsync(new Role { Id = roleId, Name = "Client" });
            //using helper method to create mock cursor
            SetupFindAsyncClient(emptyClientList);
            //setup password hasher to return a hashed password
            _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<object>(), newClient.Password)).Returns("HashedPassword");
            //setup mock client collection + auth collection InsertOneAsync
            var mockAuthCollection = new Mock<IMongoCollection<Authentication>>();
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(mockAuthCollection.Object);

            mockAuthCollection.Setup(a => a.InsertOneAsync(
                It.IsAny<Authentication>(),
                null,
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockClientCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<Client>(),
                null,
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.CreateClientUserAsync(newClient);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            //verify interactions
            _mockRoleService.Verify(r => r.GetRoleByNameAsync("client"), Times.Once);
            _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<object>(), newClient.Password), Times.Once);
            _mockClientCollection.Verify(c => c.InsertOneAsync(It.IsAny<Client>(), null, It.IsAny<CancellationToken>()), Times.Once);
            //output for verification
            _output.WriteLine($"New client user created with UserCode: {newClient.UserCode}\n" +
                $"Username: {newClient.Username}");
        }



        // ----------------------------
        // Delete Staff and Client Tests
        // ----------------------------

        [Fact]
        public async Task DeleteUserAsync_DeleteStaffSuccessfully()
        {
            //Arrange 
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();

            var staff = new Staff
            {
                Id = staffId,
                FirstName = "John Johnson",
                Email = "jj@test.com",
                Phone = "N/A",
                RoleId = ObjectId.GenerateNewId(),
                AuthId = authId     //staff has auth record
            };

            var staffList = new List<Staff> { staff };  //list with 1 staff

            //using helper method to create mock cursor
            SetupFindAsyncStaff(staffList);

            //setup mock delete for staff collection
            _mockStaffCollection.Setup(s => s.DeleteOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteResult.Acknowledged(1));      //simulate 1 record deleted

            //setup mock delete for auth collection
            var mockAuthCollection = new Mock<IMongoCollection<Authentication>>();
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(mockAuthCollection.Object);
            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.SetupGet(d => d.DeletedCount).Returns(1);      //simulate 1 record deleted

            mockAuthCollection.Setup(a => a.DeleteOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDeleteResult.Object);      //simulate 1 record deleted

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.DeleteUserAsync(staffId.ToString(), "Staff");

            // Assert
            Assert.True(result);      //deletion should be successful

            //verify interactions
            _mockStaffCollection.Verify(s => s.DeleteOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            mockAuthCollection.Verify(a => a.DeleteOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            //output for verification
            _output.WriteLine($"Staff user with ID: {staffId} deleted successfully.");

        }

        [Fact]
        public async Task DeleteUserAsync_DeleteClientSuccessfully()
        {
            //Arrange 
            var clientId = ObjectId.GenerateNewId();
            var client = new Client
            {
                Id = clientId,
                UserCode = "C001",
                Username = "ClientOne",
                RoleId = ObjectId.GenerateNewId()
            };
            var clientList = new List<Client> { client };  //list with 1 client
            //using helper method to find and return client
            SetupFindAsyncClient(clientList);
            //setup mock delete for client collection
            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.SetupGet(d => d.DeletedCount).Returns(1);      //simulate 1 record deleted

            _mockClientCollection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Client>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDeleteResult.Object);      //simulate 1 record deleted
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.DeleteUserAsync(clientId.ToString(), "Client");
            // Assert
            Assert.True(result);      //deletion should be successful
            //verify interactions
            _mockClientCollection.Verify(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Client>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            //output for verification
            _output.WriteLine($"Client user with ID: {clientId} deleted successfully.");
        }

        // ----------------------------
        // Updating Staff User Test Cases
        // ----------------------------

        [Fact]
        public async Task UpdateStaffUserAsync_Successfully()
        {
            //arrange 
            var staffId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();      //need to add this for staff

            var existingStaff = new Staff
            {
                Id = staffId,
                FirstName = "John Johnson",
                Email = "jj@test.com",
                Phone = "N/A",
                RoleId = roleId,
                AuthId = authId
            };

            var staffList = new List<Staff> { existingStaff };  //list with 1 staff

            //using helper method to find and return staff
            SetupFindAsyncStaff(staffList);

            //setup mock update for staff collection
            _mockStaffCollection.Setup(s => s.UpdateOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<UpdateDefinition<Staff>>(),  // Note: UpdateDefinition, not Staff object
                It.IsAny<UpdateOptions>(),      //can say null but keeping it strongly typed
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));  // UpdateResult

            //need to add mock auth collection for password update
            var mockAuthCollection = new Mock<IMongoCollection<Authentication>>();
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(mockAuthCollection.Object);

            //setup password hasher
            _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<object>(), "NewPlainTextPassword"))
                .Returns("NewHashedPassword");

            //setup update for auth collection
            mockAuthCollection.Setup(u => u.UpdateOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<UpdateDefinition<Authentication>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));      //simulate 1 record updated

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            //create update dto
            var updateDto = new UpdateStaffDto
            {
                FirstName = "Jack Jackson",
                Email = "jackjackson@test.com",
                Phone = "N/A",
                RoleId = roleId.ToString(),
                NewPassword = "NewPlainTextPassword"
            };
            // Act
            var result = await service.UpdateStaffUserAsync(staffId.ToString(), updateDto);
            // Assert
            Assert.True(result);      //update should be successful

            //verify interactions
            _mockStaffCollection.Verify(s => s.UpdateOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<UpdateDefinition<Staff>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
            mockAuthCollection.Verify(u => u.UpdateOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<UpdateDefinition<Authentication>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);

            //output for verification
            _output.WriteLine($"Staff user with ID: {staffId} updated successfully.");

        }


        // ----------------------------
        // Negative Test Cases
        // ----------------------------
        //need to add "not happy" tests too


        [Fact]
        public async Task GetAllUserAsync_NoUsersFound_ReturnsEmptyList()
        {
            // Arrange
            var emptyStaffList = new List<Staff>();
            var emptyClientList = new List<Client>();
            //using helper method to create mock cursors
            var mockStaffCursor = CreateMockCursor(emptyStaffList);
            var mockClientCursor = CreateMockCursor(emptyClientList);

            //make collection.Find() return our mock cursor
            //refactored to use helper methods
            SetupFindAsyncStaff(emptyStaffList);
            SetupFindAsyncClient(emptyClientList);

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.GetAllUsersAsync();
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);      //should be empty list
            //output results for verification
            _output.WriteLine($"Total users returned: {result.Count} (List is Empty)");

        }

        [Fact]
        public async Task GetAllUsersAsync_StaffExists_ClientDoesNotExist_ReturnsOnlyStaff()
        {
            // Arrange
            var staffList = new List<Staff>
            {
                new Staff { Id = ObjectId.GenerateNewId(), FirstName = "John Johnson", Email = "JJ@test.com", Phone = "N/A", RoleId = ObjectId.GenerateNewId()  },
            };
            var emptyClientList = new List<Client>();      //no clients
            //using helper method to create mock cursors
            SetupFindAsyncStaff(staffList);
            SetupFindAsyncClient(emptyClientList);
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.GetAllUsersAsync();
            // Assert
            Assert.NotNull(result);     //only 1 staff
            Assert.Single(result);      //only 1 staff
            Assert.Equal("John Johnson", result[0].FirstName);      //ensure correct staff returned
            //output results for verification
            _output.WriteLine($"Total users returned: {result.Count}\nName: {result[0].FirstName}");

        }

        [Fact]
        public async Task GetUserByIdAsync_StaffNotFound_ReturnsNull()
        {
            // Arrange
            var staffList = new List<Staff>();      //empty list
            //using helper method to create mock cursor
            var mockStaffCursor = CreateMockCursor(staffList);
            //setup find to return the mock cursor
            //refactored to use helper method
            SetupFindAsyncStaff(staffList);
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.GetUserByIdAsync(ObjectId.GenerateNewId().ToString(), "Staff");
            // Assert
            Assert.Null(result);      //should be null if not found

            //output for verification
            _output.WriteLine($"No user found with the given ID.\nID: {result} (Blank if null)");

        }

        [Fact]
        public async Task CreateStaffUserAsync_ShouldNotCreate_WhenEmailAlreadyExists()
        {
            var roleId = ObjectId.GenerateNewId();

            // Arrange
            var existingStaff = new List<Staff>
            {
                new Staff { Email = "Jack@test.com" }
            };

            var newStaff = new CreateStaffDto
            {
                FirstName = "Jack Jackson",
                Email = "Jack@test.com",   // duplicate
                Password = "PlainTextPassword",
                Phone = "N/A",
                RoleId = roleId.ToString()
            };

            // Mock find existing staff
            SetupFindAsyncStaff(existingStaff);

            // Mock role service
            _mockRoleService.Setup(r => r.GetRoleByIdAsync(roleId.ToString()))
                .ReturnsAsync(new Role { Id = roleId, Name = "Staff" });

            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act -- this would throw an exception as defined in the service
            //var result = await service.CreateStaffUserAsync(newStaff);
            //we need to handle it here
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStaffUserAsync(newStaff));

            //assert
            Assert.NotNull(exception);      //expecting exception
            Assert.Contains("Already exists", exception.Message, StringComparison.OrdinalIgnoreCase);       //interpret message     //needed ordinalignorecase to fix failure for test


            // Verify no insert was called
            _mockStaffCollection.Verify(s => s.InsertOneAsync(
                It.IsAny<Staff>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _output.WriteLine($"User creation failed as expected due to duplicate email: {newStaff.Email}\nException: {exception.Message}");
        }

        [Fact]
        public async Task CreateClientUserAsync_ShouldNotCreate_WhenUserCodeAlreadyExists()
        {
            //Arrange
            var existingClients = new List<Client>
            {
                new Client { UserCode = "C003" }
            };
            var newClient = new CreateClientDto
            {
                UserCode = "C003",   // duplicate
                Username = "ClientThree",
                Password = "PlainTextPassword"
            };

            // Mock find existing clients
            SetupFindAsyncClient(existingClients);

            // Mock role service
            _mockRoleService.Setup(r => r.GetRoleByNameAsync("client"))
                .ReturnsAsync(new Role { Id = ObjectId.GenerateNewId(), Name = "Client" });

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            //var result = await service.CreateClientUserAsync(newClient);      //expection here too
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateClientUserAsync(newClient));


            // Assert
            Assert.NotNull(exception);
            Assert.Contains("Already exists", exception.Message, StringComparison.OrdinalIgnoreCase);       //updated with ordinalignorecase to fix test failure

            // Verify no insert was called
            _mockClientCollection.Verify(c => c.InsertOneAsync(
                It.IsAny<Client>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);

            //output for verification
            _output.WriteLine($"Client creation failed as expected due to duplicate UserCode: {newClient.UserCode}\nException: {exception.Message}");

        }

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ReturnsFalse()
        {
            //Arrange 
            var staffList = new List<Staff>();  //empty list simulating no user found
            //using helper method to create mock cursor
            SetupFindAsyncStaff(staffList);
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.DeleteUserAsync(ObjectId.GenerateNewId().ToString(), "Staff");
            // Assert
            Assert.False(result);      //deletion should fail as user not found
            //verify that delete was never called
            _mockStaffCollection.Verify(s => s.DeleteOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<CancellationToken>()), Times.Never);
            //output for verification
            _output.WriteLine($"Deletion attempt for non-existent user returned: {result} (Expected: False)");
        }

        [Fact]
        public async Task DeleteUserAsync_DeleteStaffFails_ReturnsFalse()
        {
            //Arrange 
            var staffId = ObjectId.GenerateNewId();
            var authId = ObjectId.GenerateNewId();

            var staff = new Staff
            {
                Id = staffId,
                FirstName = "John Johnson",
                Email = "jj@test.com",
                Phone = "N/A",
                RoleId = ObjectId.GenerateNewId(),
                AuthId = authId     //staff has auth record
            };

            var staffList = new List<Staff> { staff };  //list with 1 staff
            //using helper method to find staff
            SetupFindAsyncStaff(staffList);

            //setup mock delete for staff collection to simulate failure
            //need to include auth deletion mock too even though it won't be called
            var mockAuthCollection = new Mock<IMongoCollection<Authentication>>();
            _mockMongoDBContext.SetupGet(c => c.AuthenticationCollection).Returns(mockAuthCollection.Object);

            var mockAuthDeleteResult = new Mock<DeleteResult>();
            mockAuthDeleteResult.SetupGet(d => d.DeletedCount).Returns(1);      //simulate 1 record deleted         


            //come back and double check
            mockAuthCollection.Setup(a => a.DeleteOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAuthDeleteResult.Object);      //simulate 1 record deleted

            //staff deltion will fail (this is a pass for the test)

            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.SetupGet(d => d.DeletedCount).Returns(0);      //simulate 0 records deleted

            _mockStaffCollection.Setup(s => s.DeleteOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDeleteResult.Object);      //simulate 0 record deleted

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);

            // Act
            var result = await service.DeleteUserAsync(staffId.ToString(), "Staff");

            // Assert
            Assert.False(result);      //deletion should fail

            //verify interactions
            mockAuthCollection.Verify(d => d.DeleteOneAsync(
                It.IsAny<FilterDefinition<Authentication>>(),
                It.IsAny<CancellationToken>()), Times.Once);      //auth delete should not be called if staff delete fails however due to service logic it is called before checking delete count           --will look at this later

            //staff deletion also happend but failed
            _mockStaffCollection.Verify(s => s.DeleteOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<CancellationToken>()), Times.Once);

            //output for verification
            _output.WriteLine($"Staff user deletion attempt returned: {result} (Expected: False), but Auth was deleted.");
        }

        [Fact]
        public async Task DeleteUserAsync_DeleteClientFails_ReturnsFalse()
        {
            //Arrange 
            var clientId = ObjectId.GenerateNewId();
            var client = new Client
            {
                Id = clientId,
                UserCode = "C001",
                Username = "ClientOne",
                RoleId = ObjectId.GenerateNewId()
            };
            var clientList = new List<Client> { client };  //list with 1 client
            //using helper method to find and return client
            SetupFindAsyncClient(clientList);
            //setup mock delete for client collection to simulate failure
            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.SetupGet(d => d.DeletedCount).Returns(0);      //simulate 0 records deleted
            _mockClientCollection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Client>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDeleteResult.Object);      //simulate 0 record deleted
            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.DeleteUserAsync(clientId.ToString(), "Client");
            // Assert
            Assert.False(result);      //deletion should fail
            //verify interactions
            _mockClientCollection.Verify(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Client>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            //output for verification
            _output.WriteLine($"Client user deletion attempt returned: {result} (Expected: False)");
        }

        [Fact]
        public async Task UpdateStaffUserAsync_StaffNotFound_ReturnsFalse()
        {
            //arrange
            var staffId = ObjectId.GenerateNewId();
            var roleId = ObjectId.GenerateNewId();
            var updateDto = new UpdateStaffDto
            {
                FirstName = "John Updated",
                Email = "john.updated@test.com",
                Phone = "123-456-7890",
                RoleId = roleId.ToString(),
                NewPassword = "NewPlainTextPassword"

            };
            var staffList = new List<Staff>();  //empty list simulating staff not found

            //using helper method to find and return staff
            SetupFindAsyncStaff(staffList);

            //instantiate the service
            var service = new UnifiedUserService(_mockMongoDBContext.Object, _mockRoleService.Object, _mockPasswordHasher.Object);
            // Act
            var result = await service.UpdateStaffUserAsync(staffId.ToString(), updateDto);
            // Assert
            Assert.False(result);     //update should fail if staff not found
            //verify interactions
            _mockStaffCollection.Verify(s => s.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Staff>>(),
                It.IsAny<Staff>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
            //output for verification
            _output.WriteLine($"No staff user found with ID: {staffId}.");

        }
    }
}
