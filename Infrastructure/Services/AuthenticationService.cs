using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<object> _passwordHasher;

        public AuthenticationService(MongoDBContext context, IOptions<JwtSettings> jwtOptions, IPasswordHasher<object> passwordHasher)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
            _passwordHasher = passwordHasher;
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
                //hash the password with IPasswordHasher
                //thinking of updating to use better hashing algo ie bcrypt or IPasswordHasher
                var verificationResult = _passwordHasher.VerifyHashedPassword(null!, auth.HashedPassword, password);

                //checks if password verification succeeded
                if (verificationResult == PasswordVerificationResult.Failed)
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
                //generate refresh token
                var refreshToken = GenerateRefreshToken();
                var refreshHash = HashToken(refreshToken.Token);
                //store refresh token hash in authentication record
                var refreshEntry = new RefreshToken
                {
                    TokenId = refreshToken.TokenId,
                    TokenHash = refreshHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays), 
                    CreatedAt = DateTime.UtcNow
                };
                var update = Builders<Authentication>.Update
                    .Push(a => a.RefreshTokens, refreshEntry);      //push to array
                await _context.AuthenticationCollection.UpdateOneAsync(a => a.Id == auth.Id, update);       //update auth record with new refresh token


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
                    Token = token, //JWT token for API calls
                    RefreshToken = refreshToken.Token           //plain refresh token for client to store securely
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
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),     //shortened for better security might even reduce further     //apparently its best to use utc time        //replacing to use within jwt settings to ensure central area to change
                signingCredentials: credentials //digital signature
            );

            //serialize token to string format
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            return token;
        }

        //Generating A Refresh Token
        // Helpers for refresh token issuance and validation
        private (string TokenId, string Token) GenerateRefreshToken()
        {
            // TokenId to track rotation chain
            var tokenId = Guid.NewGuid().ToString();
            // 64 bytes crypto random
            var bytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(bytes);
            return (tokenId, token);
        }

        private string HashToken(string token)
        {
            using var sha512 = SHA512.Create();
            var bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return; // nothing to do if the client didn’t send one

            var refreshHash = HashToken(refreshToken);

            // Find authentication record that contains this refresh token
            var auth = await _context.AuthenticationCollection
                .Find(a => a.RefreshTokens.Any(rt => rt.TokenHash == refreshHash))
                .FirstOrDefaultAsync();

            if (auth == null)
                return; // already invalid or doesn't exist

            // Remove the matching refresh token
            var update = Builders<Authentication>.Update
                .PullFilter(a => a.RefreshTokens, rt => rt.TokenHash == refreshHash);

            await _context.AuthenticationCollection.UpdateOneAsync(
                a => a.Id == auth.Id,
                update
            );

        }

        public async Task<(string Token, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            if(string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException("Refresh token is required", nameof(refreshToken));
            }

            var refreshHash = HashToken(refreshToken);
            //find auth record with matching refresh token hash
            var auth = await _context.AuthenticationCollection
                .Find(a => a.RefreshTokens.Any(rt => rt.TokenHash == refreshHash))
                .FirstOrDefaultAsync();
            if(auth == null)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            var tokenEntry = auth.RefreshTokens.FirstOrDefault(rt => rt.TokenHash == refreshHash);

            // Validate token existence and expiry
            if (tokenEntry == null || tokenEntry.ExpiresAt <= DateTime.UtcNow)
            {
                throw new SecurityTokenException("Refresh token is invalid or expired");
            }


            //load user and role
            var staff = await _context.StaffCollection
                .Find(s => s.AuthId == auth.Id)
                .FirstOrDefaultAsync();
            if(staff == null)
            {
                throw new SecurityTokenException("User not found for the given refresh token");
            }

            var role = await _context.RolesCollection
                .Find(r => r.Id == staff.RoleId)
                .FirstOrDefaultAsync();

            //issue new tokens
            var newAccessToken = GenerateJwtToken(staff, role?.Name ?? "Unknown");

            //rotate refresh token
            var (newId, newToken) = GenerateRefreshToken();
            var newHash = HashToken(newToken);

            var update = Builders<Authentication>.Update
                .PullFilter(a => a.RefreshTokens, rt => rt.TokenHash == refreshHash) //remove old token
                .Push(a => a.RefreshTokens, new RefreshToken
                {
                    TokenId = newId,
                    TokenHash = newHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow
                }); //add new token
            await _context.AuthenticationCollection.UpdateOneAsync(a => a.Id == auth.Id, update);

            return (newAccessToken, newToken);



        }




        /// <summary>
        ///Implementing RefreshToken functionality
        /// </summary>
        




        /// <summary>
        /// Implementing the interface methods
        /// </summary>
        //currently they are not being used, but will be needed for user management (CRUD operations)
        //refactoring as not used right now
        /*
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
                
                if (auth != null)
                {
                    //verify password with IPasswordHasher
                    var verificationResult = _passwordHasher.VerifyHashedPassword(null!, auth.HashedPassword, password);

                    if (verificationResult != PasswordVerificationResult.Failed)
                    {
                        return auth; //successful authentication
                    }
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
        */

    }
}