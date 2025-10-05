using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PrototypeGroupProject_WebDashboard.Models.DTO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrototypeGroupProject_WebDashboard.Controllers
{
    //this controller is to handle the auth views (login) and sends the credentials to the api for verification

    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string SessionTokenKey = "JWToken";

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;     //better practice than httpclient directly as it handles pooling
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new LoginRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginRequestDto model)       
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            // Adjust endpoint path if API differs
            var response = await client.PostAsJsonAsync("api/Auth/loginStaff", model);           //this hits the loginStaff endpoint exposed by api

            if (response.IsSuccessStatusCode)
            {
                // Expecting JSON like { "token": "..." }
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (!string.IsNullOrWhiteSpace(loginResponse?.Token))
                {
                    HttpContext.Session.SetString(SessionTokenKey, loginResponse.Token);
                    //setup claims for cookies
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProp = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProp);
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Error = "Invalid token response.";
            }
            else
            {
                ViewBag.Error = $"Login failed ({(int)response.StatusCode}).";      //expecting 401 for invalid creds and 400 for bad request
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);  //signout
            HttpContext.Session.Remove(SessionTokenKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
