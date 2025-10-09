using Core.Models.DTO;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

    public class AuthenticationService
    {
        private readonly MongoDBContext _context;
        private readonly IConfiguration _configuration;

        public AuthenticationService(MongoDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                //hash the password with stored salt and compare
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

        private string GenerateJwtToken(Core.Models.Staff staff, string role)
        {
            //create security key from secret in appsettings.json
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            
            //create signing credentials using HMAC SHA256 algorithm
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

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
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials //digital signature
            );

            //serialize token to string format
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        /// <summary>
        ///Verifies a password against a stored hash and salt,
        ///uses SHA256 hashing algorithm.
        /// </summary>

        private bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
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
    }
}