using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{

    /// <summary>
    /// AuthenticationService handles user login and JWT token generation.
    /// Resposible for:
    /// - validating user credentials
    /// - verifying hashed passwords with salts
    /// - generating JWT tokens for authenticated users
    /// - includes user claims in tokens
    /// </summary>

    public class AuthenticationService : IAuthenticationService      //dependency injection in startup
    {
        private readonly MongoDBContext _context;
        private readonly JwtSettings _jwtSettings;      //adding jwt settings for key creation, using options pattern

        public AuthenticationService(MongoDBContext context, IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
        }


        /// <summary>
        ///Authenticates a user by email and password;
        ///returns LoginResponseDto with JWT token if successful.
        /// </summary>

        public async Task<LoginResponseDto> LoginAsync(string email, string password)
        {
            try
            {
                // ============================================================
                // STEP 1: Find Staff by Email
                // ============================================================
                //queries staff collection for matching email
                var staff = await _context.StaffCollection
                    .Find(s => s.Email == email)
                    .FirstOrDefaultAsync();

                //if no staff member found, return failure response
                if (staff == null)
                {
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // ============================================================
                // STEP 2: Retrieve Authentication Record
                // ============================================================
                //get the hashed password and salt using staff's AuthId
                var auth = await _context.AuthenticationCollection
                    .Find(a => a.Id == staff.AuthId)
                    .FirstOrDefaultAsync();

                //if auth record not found (data integrity issue), return failure response
                if (auth == null)
                {
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Authentication record not found"
                    };
                }

                // ============================================================
                // STEP 3: Verify Password
                // ============================================================
                //hash the password with stored salt and compare        //thinking of updating to use better hashing algo ie bcrypt or IPasswordHasher
                bool isPasswordValid = VerifyPassword(password, auth.HashedPassword, auth.Salt);

                //if password does not match, return failure response
                if (!isPasswordValid)
                {
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // ============================================================
                // STEP 4: Retrieve Role Information
                // ============================================================
                //get the user's role for authorization
                var role = await _context.RolesCollection
                    .Find(r => r.Id == staff.RoleId)
                    .FirstOrDefaultAsync();

                // ============================================================
                // STEP 5: Generate JWT Token
                // ============================================================
                //create JWT token with user claims
                var token = GenerateJwtToken(staff, role?.Name ?? "Unknown");

                // ============================================================
                // STEP 6: Return Success Response
                // ============================================================
                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    UserId = staff.Id.ToString(),
                    Email = staff.Email,
                    FirstName = staff.FirstName,
                    Role = role?.Name ?? "Unknown",
                    Token = token //JWT token for API calls
                    
                };
            }
            catch (Exception ex)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"An error occurred during login: {ex.Message}"
                };
            }
        }



        /// <summary>
        ///Generates a JWT token for the authenticated user,
        ///token contains user claims and is signed with a secret key.
        /// </summary>

        private string GenerateJwtToken(Staff staff, string role)       //could incorperate expiry time here, need to look into it a bit more
        {
            //first check to see if configuration values are present
            if (string.IsNullOrEmpty(_jwtSettings.SecretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured.");       //throw exception
            }
            //create security key from secret in appsettings.json       //refactoring to use options pattern
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            //create signing credentials using HMAC SHA256 algorithm        //changing to use 512 bit key
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            // ============================================================
            // DEFINE TOKEN CLAIMS
            // ============================================================
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, staff.Id.ToString()),
                new Claim(ClaimTypes.Email, staff.Email),
                new Claim(ClaimTypes.GivenName, staff.FirstName),
                new Claim(ClaimTypes.Role, role)
            };

            // ============================================================
            // CREATE JWT TOKEN
            // ============================================================
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),     //shortened for better security might even reduce further     //apparently its best to use utc time not sure why
                signingCredentials: credentials //digital signature
            );

            //serialize token to string format
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            return token;
        }


        /// <summary>
        ///Verifies a password against a stored hash and salt,
        ///uses SHA256 hashing algorithm.
        /// </summary>

        private bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)       //can improve security here by using better algo ie bcrypt or IPasswordHasher, and need to use it here if i change
        {
            using (var sha256 = SHA256.Create())
            {
                //combine entered password with stored salt
                var saltedPassword = enteredPassword + storedSalt;

                //hash the salted password
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                var computedHash = Convert.ToBase64String(hashBytes);

                //compare computed hash with stored hash
                return computedHash == storedHash;
            }
        }

        /// <summary>
        /// Implementing the interface methods
        /// </summary>
        //currently they are not being used, but will be needed for user management (CRUD operations)
        //refactoring
        public async Task<Authentication?> ValidateUserAsync(string username, string password)
        {
            //trying staff by email first
            var staff = await _context
                .StaffCollection
                .Find(s => s.Email == username)
                .FirstOrDefaultAsync();
            if (staff != null)
            {
                var auth = await _context.AuthenticationCollection
                    .Find(a => a.Id == staff.AuthId)
                    .FirstOrDefaultAsync();
                if (auth != null && VerifyPassword(password, auth.HashedPassword, auth.Salt))
                {
                    return auth;
                }
            }
            //fallback if false will look how to handle later
            return null;
        }

        public async Task<Authentication?> GetUserByIdAsync(string userId)
        {
            //converting to oid
            if(!ObjectId.TryParse(userId, out var objectId))
            {
                return null; //invalid id format
            }
            //checking user id
            var auth =  _context.AuthenticationCollection
                .Find(a => a.Id == objectId)      //need to convert to ObjectId     --done
                .FirstOrDefaultAsync();
            return await auth;
        }

        public Task<Authentication?> GetUserByUsernameAsync(string username)
        {
            var auth =  _context.AuthenticationCollection
                .Find(a => a.AuthID == username)        //not sure here     need to test
                .FirstOrDefaultAsync();
            return auth;        //nullable warning need to handle in controller    
        }

        
        public async Task<bool> CreateUserAsync(Authentication user)
        {
            var existingUser = _context.AuthenticationCollection
                .Find(a => a.AuthID == user.AuthID)
                .FirstOrDefaultAsync().Result;
            if (existingUser != null)
            {
                await _context.AuthenticationCollection.InsertOneAsync(user);
                return true;
            }
            return false; //user already exists need to handle this in controller

        }

        public async Task<bool> UpdateUserAsync(string userId, Authentication user)
        {
            if(!ObjectId.TryParse(userId, out var objectId))
            {
                return false; //invalid id format
            }
            //user.Id = objectId; //ensure the id is set correctly      //think redundant
            //updating user record
            var result = await _context.AuthenticationCollection
                .ReplaceOneAsync(a => a.Id == objectId, user);
            //need to check if modified count is 1      //gpt (not 100% sure of this, or how this works)
            return result.ModifiedCount == 1;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            if(!ObjectId.TryParse(userId, out var objectId))
            {
                return false; //invalid id format need to handle in controller
            }
            var result = await _context.AuthenticationCollection
                .DeleteOneAsync(a => a.Id == objectId);
            //added(not 100% of how this is done)
            return result.DeletedCount == 1;    //keeping track
        }
        
    }
}