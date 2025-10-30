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
        private readonly IMongoDBContext _context;      //using interface for testing also to just fix dependencies
        private readonly JwtSettings _jwtSettings;      //adding jwt settings for key creation, using options pattern
        private readonly IPasswordHasher<object> _passwordHasher;

        public AuthenticationService(IMongoDBContext context, IOptions<JwtSettings> jwtOptions, IPasswordHasher<object> passwordHasher)
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
                // STEP 1: Find User by Email (Check both Staff and Client)
                // ============================================================

                // Try to find in Staff collection first
                var staff = await _context.StaffCollection
                    .Find(s => s.Email == email)
                    .FirstOrDefaultAsync();

                // Try to find in Client collection if not found in Staff
                var client = staff == null
                    ? await _context.ClientCollection
                        .Find(c => c.UserEmail == email)  // Note: Client uses UserEmail, not Email
                        .FirstOrDefaultAsync()
                    : null;

                // If neither staff nor client found, return failure
                if (staff == null && client == null)
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
                ObjectId authId = staff != null ? staff.AuthId : client!.AuthId!.Value;

                var auth = await _context.AuthenticationCollection
                    .Find(a => a.Id == authId)
                    .FirstOrDefaultAsync();

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
                var verificationResult = _passwordHasher.VerifyHashedPassword(
                    new object(),
                    auth.HashedPassword,
                    password
                );

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
                ObjectId roleId = staff != null ? staff.RoleId : client!.RoleId;

                var role = await _context.RolesCollection
                    .Find(r => r.Id == roleId)
                    .FirstOrDefaultAsync();

                // ============================================================
                // STEP 5: Generate JWT Token
                // ============================================================
                string userId = staff != null ? staff.Id.ToString() : client!.Id.ToString();
                string userEmail = staff != null ? staff.Email : client!.UserEmail;
                string userName = staff != null ? staff.FirstName : client!.Username;

                var token = GenerateJwtToken(userId, userEmail, userName, role?.Name ?? "Unknown");
                var refreshToken = GenerateRefreshToken();
                var refreshHash = HashToken(refreshToken.Token);

                var refreshEntry = new RefreshToken
                {
                    TokenId = refreshToken.TokenId,
                    TokenHash = refreshHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow
                };

                var update = Builders<Authentication>.Update
                    .Push(a => a.RefreshTokens, refreshEntry);
                await _context.AuthenticationCollection.UpdateOneAsync(a => a.Id == auth.Id, update);

                // ============================================================
                // STEP 6: Return Success Response
                // ============================================================
                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    UserId = userId,
                    Email = userEmail,
                    FirstName = userName,
                    Role = role?.Name ?? "Unknown",
                    Token = token,
                    RefreshToken = refreshToken.Token
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

        private string GenerateJwtToken(string userId, string email, string name, string role)       //could incorperate expiry time here, need to look into it a bit more
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
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.GivenName, name),
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
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException("Refresh token is required", nameof(refreshToken));
            }

            var refreshHash = HashToken(refreshToken);
            //find auth record with matching refresh token hash
            var auth = await _context.AuthenticationCollection
                .Find(a => a.RefreshTokens.Any(rt => rt.TokenHash == refreshHash))
                .FirstOrDefaultAsync();
            if (auth == null)
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
            if (staff == null)
            {
                throw new SecurityTokenException("User not found for the given refresh token");
            }

            var role = await _context.RolesCollection
                .Find(r => r.Id == staff.RoleId)
                .FirstOrDefaultAsync();

            //issue new tokens
            var newAccessToken = GenerateJwtToken(
                staff.Id.ToString(),
                staff.Email,
                staff.FirstName,
                role?.Name ?? "Unknown");

            //rotate refresh token
            var (newId, newToken) = GenerateRefreshToken();
            var newHash = HashToken(newToken);
            /* issues here in mongo
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
            */
            //Remove (pull) the old refresh token
            var pull = Builders<Authentication>.Update
                .PullFilter(a => a.RefreshTokens, rt => rt.TokenHash == refreshHash);
            await _context.AuthenticationCollection.UpdateOneAsync(a => a.Id == auth.Id, pull);

            //Add (push) the new refresh token
            var push = Builders<Authentication>.Update
                .Push(a => a.RefreshTokens, new RefreshToken
                {
                    TokenId = newId,
                    TokenHash = newHash,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow
                });
            await _context.AuthenticationCollection.UpdateOneAsync(a => a.Id == auth.Id, push);

            return (newAccessToken, newToken);



        }

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