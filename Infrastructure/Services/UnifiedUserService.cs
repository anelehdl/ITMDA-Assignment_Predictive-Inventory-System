using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{

    /// <summary>
    /// Unified User Service to manage both Staff and Client users
    /// it provides a single interface for user management operations
    /// Key responsibilities:
    /// - retrieve users from both collections
    /// - create new staff memebers with authentication
    /// - create new clients with optional authentication
    /// - delete users from either collection
    /// </summary>

    public class UnifiedUserService : IUnifiedUserService
    {
        private readonly MongoDBContext _context;
        private readonly IRoleService _roleService;     //refactored to use interface

        public UnifiedUserService(MongoDBContext context, IRoleService roleService)
        {
            _context = context;
            _roleService = roleService;
        }


        /// <summary>
        /// Get all users from both Staff and Client collections;
        /// returns unified DTO list that combines both user types;
        /// and also supports filtering by user type, role, and search term.
        /// </summary>
        
        public async Task<List<UnifiedUserDto>> GetAllUsersAsync(UserFilterDto? filter = null)
        {
            var unifiedUsers = new List<UnifiedUserDto>();


            // ============================================================
            // RETRIEVE STAFF USERS
            // ============================================================

            //get staff members if not filtered to Client only
            if (string.IsNullOrEmpty(filter?.UserType) || filter.UserType == "Staff")
            {
                //get all staff members from the database
                var staffMembers = await _context.StaffCollection.Find(_ => true).ToListAsync();

                foreach (var staff in staffMembers)
                {
                    //get role information
                    var role = await _roleService.GetRoleByIdAsync(staff.RoleId.ToString());

                    //apply role filter if specified
                    if (filter?.RoleId != null && staff.RoleId.ToString() != filter.RoleId)
                        continue;

                    //apply search filter (firstname or email)
                    if (!string.IsNullOrEmpty(filter?.SearchTerm))
                    {
                        var searchLower = filter.SearchTerm.ToLower();
                        if (!staff.FirstName.ToLower().Contains(searchLower) &&
                            !staff.Email.ToLower().Contains(searchLower))
                            continue;
                    }

                    //add to unified user list with "Staff" type
                    unifiedUsers.Add(new UnifiedUserDto
                    {
                        Id = staff.Id.ToString(),
                        UserType = "Staff",
                        FirstName = staff.FirstName,
                        Email = staff.Email,
                        Phone = staff.Phone,
                        RoleName = role?.Name ?? "Unknown",
                        RoleId = staff.RoleId.ToString()
                    });
                }
            }


            // ============================================================
            // RETRIEVE CLIENT USERS
            // ============================================================

            //get clients if not filtered to Staff only
            if (string.IsNullOrEmpty(filter?.UserType) || filter.UserType == "Client")
            {
                //get all clients from the database
                var clients = await _context.ClientCollection.Find(_ => true).ToListAsync();

                foreach (var client in clients)
                {
                    //get role information
                    var role = await _roleService.GetRoleByIdAsync(client.RoleId.ToString());

                    //apply role filter if specified
                    if (filter?.RoleId != null && client.RoleId.ToString() != filter.RoleId)
                        continue;

                    //apply search filter (searches user_code or username)
                    if (!string.IsNullOrEmpty(filter?.SearchTerm))
                    {
                        var searchLower = filter.SearchTerm.ToLower();
                        if (!client.UserCode.ToLower().Contains(searchLower) &&
                            !client.Username.ToLower().Contains(searchLower))
                            continue;
                    }

                    //add client to unified user list with "Client" type
                    unifiedUsers.Add(new UnifiedUserDto
                    {
                        Id = client.Id.ToString(),
                        UserType = "Client",
                        UserCode = client.UserCode,
                        Username = client.Username,
                        RoleName = role?.Name ?? "Unknown",
                        RoleId = client.RoleId.ToString()
                    });
                }
            }

            return unifiedUsers;
        }



        /// <summary>
        ///Get a specific user by ID and type (Staff or Client);
        ///returns unified DTO regardless of user type.
        /// </summary>
        
        public async Task<UnifiedUserDto?> GetUserByIdAsync(string userId, string userType)
        {
            //converts string ID to Mongo ObjectId
            if (!ObjectId.TryParse(userId, out var objectId))
                return null;


            // ============================================================
            // RETRIEVE STAFF USER
            // ============================================================
            if (userType == "Staff")
            {
                var staff = await _context.StaffCollection
                    .Find(s => s.Id == objectId)
                    .FirstOrDefaultAsync();

                if (staff == null)
                    return null;

                var role = await _roleService.GetRoleByIdAsync(staff.RoleId.ToString());

                return new UnifiedUserDto
                {
                    Id = staff.Id.ToString(),
                    UserType = "Staff",
                    FirstName = staff.FirstName,
                    Email = staff.Email,
                    Phone = staff.Phone,
                    RoleName = role?.Name ?? "Unknown",
                    RoleId = staff.RoleId.ToString()
                };
            }
            // ============================================================
            // RETRIEVE CLIENT USER
            // ============================================================
            else
            {
                var client = await _context.ClientCollection
                    .Find(c => c.Id == objectId)
                    .FirstOrDefaultAsync();

                if (client == null)
                    return null;

                var role = await _roleService.GetRoleByIdAsync(client.RoleId.ToString());

                return new UnifiedUserDto
                {
                    Id = client.Id.ToString(),
                    UserType = "Client",
                    UserCode = client.UserCode,
                    Username = client.Username,
                    RoleName = role?.Name ?? "Unknown",
                    RoleId = client.RoleId.ToString()
                };
            }
        }



        /// <summary>
        ///Creates a new Staff user with authentication;
        ///validates the role, checks for duplicates, creates auth records,
        ///and inserts staff records in the database.
        /// </summary>

        public async Task<string> CreateStaffUserAsync(CreateStaffDto createStaff)
        {
            // ============================================================
            // STEP 1: Validate Role
            // ============================================================
            //ensure the role exists and is not "client" (staff can't have client role)
            var role = await _roleService.GetRoleByIdAsync(createStaff.RoleId);
            if (role == null)
            {
                throw new ArgumentException("Invalid role specified");
            }

            if (role.Name.ToLower() == "client")
            {
                throw new ArgumentException("Cannot assign client role to staff user");
            }

            // ============================================================
            // STEP 2: Check for duplicate email
            // ============================================================
            var existingStaff = await _context.StaffCollection
                .Find(s => s.Email == createStaff.Email)
                .FirstOrDefaultAsync();

            if (existingStaff != null)
            {
                throw new InvalidOperationException("Staff user with this email already exists");
            }

            // ============================================================
            // STEP 3: Create Authentication Record
            // ============================================================
            //hash the password and generate salt and store in auth collection
            var (hashedPassword, salt) = HashPassword(createStaff.Password);
            var auth = new Authentication
            {
                AuthID = Guid.NewGuid().ToString(),
                Salt = salt,
                HashedPassword = hashedPassword
            };
            await _context.AuthenticationCollection.InsertOneAsync(auth);

            // ============================================================
            // STEP 4: Create Staff Record
            // ============================================================
            //link to the authentication record via AuthId
            var staff = new Staff
            {
                FirstName = createStaff.FirstName,
                Email = createStaff.Email,
                Phone = createStaff.Phone,
                RoleId = ObjectId.Parse(createStaff.RoleId),
                AuthId = auth.Id //link to auth record
            };
            await _context.StaffCollection.InsertOneAsync(staff);

            return staff.Id.ToString();
        }



        /// <summary>
        ///Create a new Client user with optional authentication;
        ///automatically assigns "client role;
        ///authentication is only created if password is provided.
        /// </summary>
        
        public async Task<string> CreateClientUserAsync(CreateClientDto createClient)
        {
            // ============================================================
            // STEP 1: Get Client Role
            // ============================================================
            //automatically assigns "client" role
            var clientRole = await _roleService.GetRoleByNameAsync("client");
            if (clientRole == null)
            {
                throw new InvalidOperationException("Client role not found in database. Please ensure 'client' role exists.");
            }

            // ============================================================
            // STEP 2: Check for Duplicate UserCode
            // ============================================================
            var existingClient = await _context.ClientCollection
                .Find(c => c.UserCode == createClient.UserCode)
                .FirstOrDefaultAsync();

            if (existingClient != null)
            {
                throw new InvalidOperationException($"Client with user code '{createClient.UserCode}' already exists");
            }

            // ============================================================
            // STEP 3: Create Authentication Record (Optional)
            // ============================================================
            //only creates auth if password is provided;
            //this allows clients without login access (data-only clients)
            ObjectId? authId = null;
            if (!string.IsNullOrEmpty(createClient.Password))
            {
                var (hashedPassword, salt) = HashPassword(createClient.Password);
                var auth = new Authentication
                {
                    AuthID = Guid.NewGuid().ToString(),
                    Salt = salt,
                    HashedPassword = hashedPassword
                };
                await _context.AuthenticationCollection.InsertOneAsync(auth);
                authId = auth.Id;
            }

            // ============================================================
            // STEP 4: Create Client Record
            // ============================================================
            var client = new Client
            {
                UserCode = createClient.UserCode,
                Username = createClient.Username,
                RoleId = clientRole.Id //automatically assigns "client" role
            };
            await _context.ClientCollection.InsertOneAsync(client);

            return client.Id.ToString();
        }



        /// <summary>
        ///Delete user (works for both Staff and Client)
        ///for staff: also deletes associated authentication record
        /// </summary>

        public async Task<bool> DeleteUserAsync(string userId, string userType)
        {
            //convert string ID to Mongo ObjectId
            if (!ObjectId.TryParse(userId, out var objectId))
                return false;


            // ============================================================
            // DELETE STAFF USER
            // ============================================================
            if (userType == "Staff")
            {
                //find staff member
                var staff = await _context.StaffCollection
                    .Find(s => s.Id == objectId)
                    .FirstOrDefaultAsync();

                if (staff == null)
                    return false;

                //delete associated authentication record
                await _context.AuthenticationCollection
                    .DeleteOneAsync(a => a.Id == staff.AuthId);

                //delete staff record
                var result = await _context.StaffCollection
                    .DeleteOneAsync(s => s.Id == objectId);

                return result.DeletedCount > 0;
            }
            // ============================================================
            // DELETE CLIENT USER
            // ============================================================
            else
            {
                //find client
                var client = await _context.ClientCollection
                    .Find(c => c.Id == objectId)
                    .FirstOrDefaultAsync();

                if (client == null)
                    return false;

                //delete client record
                //may not have associated auth record (if created without password)
                var result = await _context.ClientCollection
                    .DeleteOneAsync(c => c.Id == objectId);

                return result.DeletedCount > 0;
            }
        }



        /// <summary>
        ///Hash password using SHA256 with a random salt;
        ///returns hashed password and salt as base64 strings (needed for verification).
        /// </summary>

        private (string hashedPassword, string salt) HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // ============================================================
                // STEP 1: Generate Random Salt
                // ============================================================
                byte[] saltBytes = new byte[32];
                rng.GetBytes(saltBytes);
                string salt = Convert.ToBase64String(saltBytes);

                // ============================================================
                // STEP 2: Hash Password + Salt
                // ============================================================
                using (var sha256 = SHA256.Create())
                {
                    var saltedPassword = password + salt;
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                    string hashed = Convert.ToBase64String(hashBytes);

                    return (hashed, salt);
                }
            }
        }
    }
}