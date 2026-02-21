using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ServiceCore.Data;
using ServiceCore.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ServiceCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ServiceCoreDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(ServiceCoreDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }

            // Check if email already exists
            if (_db.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }

            var user = new ServiceCore.Models.User
            {
                Name = name,
                Email = email,
                Role = "User",
                IsActive = true
            };

            // Hash using ASP.NET Core Identity hasher
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            ViewBag.Success = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // Retrieve user by email
            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !user.IsActive)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Verify using ASP.NET Core Identity hasher first
            var verifyResult = PasswordVerificationResult.Failed;
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }

            // Support legacy SHA256 hashed passwords (User.VerifyPassword) and re-hash
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                if (user.VerifyPassword(password))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    _db.Users.Update(user);
                    await _db.SaveChangesAsync();
                    verifyResult = PasswordVerificationResult.Success;
                }
            }

            // Allow login on Success or SuccessRehashNeeded
            if (verifyResult != PasswordVerificationResult.Success && verifyResult != PasswordVerificationResult.SuccessRehashNeeded)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // If rehash recommended, update stored hash
            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            // Update last login
            user.LastLoginAt = DateTime.Now;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> CompleteRegistration(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.InviteToken == token);
            if (user == null)
            {
                return NotFound("Invalid or expired token.");
            }

            var model = new CompleteRegistrationViewModel
            {
                Token = token,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> CompleteRegistration(CompleteRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.InviteToken == model.Token);
            if (user == null)
            {
                return NotFound("Invalid or expired token.");
            }

            // Hash password using IPasswordHasher to ensure a secure, standard hash
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            user.Name = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = true;
            user.InviteToken = null; // Clear token so it can't be used again
            // user.EmailConfirmed = true; // Property doesn't exist yet, rely on IsActive

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // Log the user in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }
    }
}
