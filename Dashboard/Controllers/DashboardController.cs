using Core.Models.DTO;
using Dashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Dashboard.Controllers
{

    /// <summary>
    /// DashboardController handles user authentication for the web dashboard,
    /// manages login/logout flows and creates cookie-based sessions.
    /// Authentication flow:
    /// - user submits login form
    /// - dashboard calls API to validate credentials
    /// - API returns JWT token if valid
    /// - dashboard creates cookie with user claims
    /// - user stays logged in via cookie for 24 hours (!can change!)
    /// </summary>

    public class DashboardController : Controller
    {
        private readonly DashboardAuthService _authService;

        public DashboardController(DashboardAuthService authService)
        {
            _authService = authService;
        }


        /// <summary>
        ///GET /Dashboard/Index
        ///displays login page,
        ///redirects to home if user is already authenticated.
        /// </summary>

        [HttpGet]
        public IActionResult Index()
        {
            // ============================================================
            // CHECK IF USER IS ALREADY LOGGED IN   
            // ============================================================
            //if user hass valid authentication cookie, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            //show login page for unauthenticated users
            return View("Login");
        }


        /// <summary>
        ///POST /Dashboard/Login
        ///processes login form submission,
        ///validates credentials via API, creates auth cookie.
        /// </summary>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            // ============================================================
            // STEP 1: Validate Form Input
            // ============================================================
            //check if model binding succeeded and required fields are present
            if (!ModelState.IsValid)
            {
                return View(model); //return to login with validation errors
            }

            // ============================================================
            // STEP 2: Authenticate via API
            // ============================================================
            //call DashboardAuthService which makes HTTP request to API
            var result = await _authService.LoginAsync(model.Email, model.Password);


            // ============================================================
            // STEP 3: Handle Authentication Failure
            // ============================================================
            if (!result.Success)
            {
                //add error message to model state and return to login view
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // ============================================================
            // STEP 4: Create Authentication Cookie
            // ============================================================
            //built list of claims to store in cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId),
                new Claim(ClaimTypes.Email, result.Email),
                new Claim(ClaimTypes.GivenName, result.FirstName),
                new Claim(ClaimTypes.Name, result.FirstName),
                new Claim("Token", result.Token)
            };

            //add role claim if available
            if (!string.IsNullOrEmpty(result.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, result.Role));
            }

            // ============================================================
            // STEP 5: Configure Cookie Properties
            // ============================================================
            //create claims identity for cookie authentication
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            //parsing jwt on login
            var handler = new JwtSecurityTokenHandler();        //this securely parses the jwt token to dashboard user
            var jwt = handler.ReadJwtToken(result.Token);       //not sure if this is best prac


            //set cookie properties
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, //survives browser close
                ExpiresUtc = jwt.ValidTo //expiration        //aligning with JWT, and ensuring that it cannot be used after JWT expires
            };

            // ============================================================
            // STEP 6: Sign In User (Create Cookie)
            // ============================================================
            //creates authentication cookie and sets it in response
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            //redirect to home page (user is now authenticated)
            return RedirectToAction("Index", "Home");
        }


        /// <summary>
        ///POST /Dashboard/Logout
        ///logs out the current user by deleting the auth cookie.
        /// </summary>

        [HttpPost]
        [ValidateAntiForgeryToken] //CSRF protection
        public async Task<IActionResult> Logout()
        {
            // ============================================================
            // DELETE AUTHENTICATION COOKIE
            // ============================================================
            //removes cookie and signs out user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //redirect to home page after logout
            return RedirectToAction("Index", "Home");
        }
    }
}