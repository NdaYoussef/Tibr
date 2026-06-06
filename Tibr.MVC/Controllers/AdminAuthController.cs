using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Tibr.Application.Dtos;
using Tibr.MVC.Models;

namespace Tibr.MVC.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AdminAuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            var loginUrl = $"{apiBaseUrl}/api/admin-login";

            var loginRequest = new LoginRequestData(model.Email, model.Password, model.RememberMe);
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await client.PostAsync(loginUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Invalid login credentials or you don't have admin permissions.");
                    return View(model);
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var authResult = JsonSerializer.Deserialize<AuthResponse>(
                    responseString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (authResult == null || !authResult.IsSuccess || string.IsNullOrEmpty(authResult.Token))
                {
                    ModelState.AddModelError("", authResult?.MessageEN ?? "This panel is for administrators only.");
                    return View(model);
                }

                // Create claims for the authenticated admin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResult.userId ?? ""),
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.Name, model.Email),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("Token", authResult.Token)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = authResult.Expiration ?? DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred during login: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
