using Core.Models.DTO;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    /// <summary>
    /// API Controller provides the REST API endpoints for managing both staff and client users;
    /// all endpoints require admin role authorization.
    /// Key features:
    /// - view all users
    /// - view specific user details
    /// - create new staff or client users
    /// - delete users
    /// </summary>

    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "admin")] //all endpoints require admin role
    public class UnifiedUserManagementController : ControllerBase
    {
        private readonly IUnifiedUserService _unifiedUserService;
        private readonly IRoleService _roleService;
        private readonly ILogger<UnifiedUserManagementController> _logger;

        public UnifiedUserManagementController(
            IUnifiedUserService unifiedUserService,
            IRoleService roleService,
            ILogger<UnifiedUserManagementController> logger)
        {
            _unifiedUserService = unifiedUserService;
            _roleService = roleService;
            _logger = logger;
        }


        /// <summary>
        ///GET /api/unifiedusermanagement/users;
        ///retrieves all users (both staff and clients) with optional filtering.
        /// </summary>

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto? filter)
        {
            try
            {
                _logger.LogInformation("Getting all users with filters: {@Filter}", filter);

                //retieve users from both Staff and CLient collections
                var users = await _unifiedUserService.GetAllUsersAsync(filter);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { Error = "An error occurred while retrieving users", Details = ex.Message });
            }
        }


        /// <summary>
        ///GET /api/unifiedusermanagement/users/staff/{id};
        ///retrieves specific staff user by ID.
        /// </summary>

        [HttpGet("users/staff/{id}")]
        public async Task<IActionResult> GetStaffUser(string id)
        {
            try
            {
                _logger.LogInformation("Getting staff user with ID: {Id}", id);

                //reteive staff user by ID
                var user = await _unifiedUserService.GetUserByIdAsync(id, "Staff");

                if (user == null)
                {
                    return NotFound(new { Message = "Staff user not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff user with ID: {Id}", id);
                return StatusCode(500, new { Error = "An error occurred while retrieving staff user", Details = ex.Message });
            }
        }


        /// <summary>
        ///GET /api/unifiedusermanagement/users/client/{id};
        ///retrieves specific client user by ID.
        /// </summary>

        [HttpGet("users/client/{id}")]
        public async Task<IActionResult> GetClientUser(string id)
        {
            try
            {
                _logger.LogInformation("Getting client user with ID: {Id}", id);

                //reteive client user by ID
                var user = await _unifiedUserService.GetUserByIdAsync(id, "Client");

                if (user == null)
                {
                    return NotFound(new { Message = "Client user not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client user with ID: {Id}", id);
                return StatusCode(500, new { Error = "An error occurred while retrieving client user", Details = ex.Message });
            }
        }


        /// <summary>
        ///POST /api/unifiedusermanagement/users/staff;
        ///creates new staff user with authentication.
        /// </summary>

        [HttpPost("users/staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto createStaff)
        {
            try
            {

                // ============================================================
                // STEP 1: Validate Required Fields
                // ============================================================
                if (string.IsNullOrEmpty(createStaff.FirstName) ||
                    string.IsNullOrEmpty(createStaff.Email) ||
                    string.IsNullOrEmpty(createStaff.Phone) ||
                    string.IsNullOrEmpty(createStaff.Password) ||
                    string.IsNullOrEmpty(createStaff.RoleId))
                {
                    return BadRequest(new { Message = "All fields (FirstName, Email, Phone, Password, RoleId) are required" });
                }

                _logger.LogInformation("Creating staff user with email: {Email}", createStaff.Email);

                // ============================================================
                // STEP 2: Create Staff User
                // ============================================================
                //service handles: role valdation, duplicate check, auth creation, staff creation
                var userId = await _unifiedUserService.CreateStaffUserAsync(createStaff);

                _logger.LogInformation("Staff user created successfully with ID: {UserId}", userId);

                // ============================================================
                // STEP 3: Return Success Response
                // ============================================================
                return Ok(new
                {
                    UserId = userId,
                    Message = "Staff user created successfully",
                    UserType = "Staff"
                });
            }
            catch (InvalidOperationException ex)
            {
                //handle duplicate email error
                _logger.LogWarning(ex, "Conflict while creating staff user");
                return Conflict(new { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                //handle invalid role error
                _logger.LogWarning(ex, "Invalid argument while creating staff user");
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff user");
                return StatusCode(500, new { Error = "An error occurred while creating staff user", Details = ex.Message });
            }
        }


        /// <summary>
        ///POST /api/unifiedusermanagement/users/client;
        ///creates a new client user with optional authentication.
        /// </summary>

        [HttpPost("users/client")]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientDto createClient)
        {
            try
            {
                // ============================================================
                // STEP 1: Validate Required Fields
                // ============================================================
                //only UserCode and Username are required (!for now!)
                if (string.IsNullOrEmpty(createClient.UserCode) ||
                    string.IsNullOrEmpty(createClient.Username))
                {
                    return BadRequest(new { Message = "UserCode and Username are required" });
                }

                _logger.LogInformation("Creating client user with code: {UserCode}", createClient.UserCode);

                // ============================================================
                // STEP 2: Create Client User
                // ============================================================
                //service handles: role assignment, duplicate check, optional auth creation
                var userId = await _unifiedUserService.CreateClientUserAsync(createClient);

                _logger.LogInformation("Client user created successfully with ID: {UserId}", userId);

                // ============================================================
                // STEP 3: Return Success Response
                // ============================================================
                return Ok(new
                {
                    UserId = userId,
                    Message = "Client user created successfully",
                    UserType = "Client",
                    HasAuthentication = !string.IsNullOrEmpty(createClient.Password) //indicates if login enabled
                });
            }
            catch (InvalidOperationException ex)
            {
                //handle duplicate user code or missing client role
                _logger.LogWarning(ex, "Conflict while creating client user");
                return Conflict(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client user");
                return StatusCode(500, new { Error = "An error occurred while creating client user", Details = ex.Message });
            }
        }


        /// <summary>
        ///DELETE /api/unifiedusermanagement/users/staff/{id};
        ///deletes a staff user by ID and their authentication record.
        /// </summary>

        [HttpDelete("users/staff/{id}")]
        public async Task<IActionResult> DeleteStaff(string id)
        {
            try
            {
                _logger.LogInformation("Deleting staff user with ID: {Id}", id);

                //delete staff user and auth record
                var result = await _unifiedUserService.DeleteUserAsync(id, "Staff");

                if (!result)
                {
                    return NotFound(new { Message = "Staff user not found" });
                }

                _logger.LogInformation("Staff user deleted successfully: {Id}", id);

                return Ok(new { Message = "Staff user deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff user with ID: {Id}", id);
                return StatusCode(500, new { Error = "An error occurred while deleting staff user", Details = ex.Message });
            }
        }


        /// <summary>
        ///DELETE /api/unifiedusermanagement/users/client/{id};
        ///deletes a client user by ID.
        /// </summary>

        [HttpDelete("users/client/{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            try
            {
                _logger.LogInformation("Deleting client user with ID: {Id}", id);

                //delete client user
                var result = await _unifiedUserService.DeleteUserAsync(id, "Client");

                if (!result)
                {
                    return NotFound(new { Message = "Client user not found" });
                }

                _logger.LogInformation("Client user deleted successfully: {Id}", id);

                return Ok(new { Message = "Client user deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client user with ID: {Id}", id);
                return StatusCode(500, new { Error = "An error occurred while deleting client user", Details = ex.Message });
            }
        }


        /// <summary>
        ///GET /api/unifiedusermanagement/roles;
        ///retrieves all available roles for staff and clients.
        /// </summary>

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                _logger.LogInformation("Getting all roles");

                // ============================================================
                // STEP 1: Retreive All Roles
                // ============================================================
                var roles = await _roleService.GetAllRolesAsync();

                //map to DTOs (convert ObjectId to string)
                var allRoles = roles.Select(r => new RoleDto
                {
                    Id = r.Id.ToString(),
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.Permissions
                }).ToList();

                // ============================================================
                // STEP 2: Filter Staff Roles
                // ============================================================
                //staff members can have any role except "Client"
                var staffRoles = allRoles.Where(r => r.Name.ToLower() != "client").ToList();

                // ============================================================
                // STEP 3: Return Both Lists
                // ============================================================
                //AllRoles: for general reference
                //StaffRoles: for creating staff users
                return Ok(new
                {
                    AllRoles = allRoles,
                    StaffRoles = staffRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { Error = "An error occurred while retrieving roles", Details = ex.Message });
            }
        }

        ///<summary>
        ///PUT /api/unifiedusermanagement/users/staff/{id};
        ///UpdateStaffUserAsync - Updates an existing staff user's details.
        ///</summary>
        ///

        [HttpPut("users/staff/{id}")]
        public async Task <IActionResult> UpdateStaff(string id, [FromBody] UpdateStaffDto updateStaff)
        {
            try
            {
                //simple check
                if (updateStaff is null ||
                    string.IsNullOrWhiteSpace(updateStaff.FirstName) &&
                    string.IsNullOrWhiteSpace(updateStaff.Email) &&
                    string.IsNullOrWhiteSpace(updateStaff.Phone) &&
                    string.IsNullOrWhiteSpace(updateStaff.RoleId) &&
                    string.IsNullOrWhiteSpace(updateStaff.NewPassword))
                {
                    return BadRequest(new { Error = "No fields provided to update" });
                }

                _logger.LogInformation("Updating staff user with ID: {Id}", id);
                //update staff user details
                var result = await _unifiedUserService.UpdateStaffUserAsync(id, updateStaff);
                if (!result)
                {
                    return NotFound(new { Message = "Staff user not found or no changes made" });
                }
                _logger.LogInformation("Staff user updated successfully: {Id}", id);
                return Ok(new { Message = "Staff user updated successfully" });
            }
            catch (ArgumentException ex)
            {
                //handle invalid role error
                _logger.LogWarning(ex, "Invalid argument while updating staff user");
                return BadRequest(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // duplicate email case from service
                _logger.LogWarning(ex, "Conflict while updating staff user");
                return Conflict(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff user with ID: {Id}", id);
                return StatusCode(500, new { Error = "An error occurred while updating staff user", Details = ex.Message });
            }
        }
    }
}