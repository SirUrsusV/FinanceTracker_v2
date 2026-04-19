using FinanceTracker.Data;
using FinanceTracker.DTO;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var token = GenerateJwtToken(user);
                    Response.Cookies.Append("jwt", token, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = false, // для разработки; в production true
                        Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60"))
                    });
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Неверное имя пользователя или пароль");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser { UserName = model.Username, Email = $"{model.Username}@example.com" };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (model.CreateDefaultCategories)
                    {
                        var defaultCategories = new List<Category>
                {
                    new Category { Name = "Продукты", Icon = "🍎", Color = "#4CAF50", DefaultType = TransactionType.Expense, UserId = user.Id },
                    new Category { Name = "Транспорт", Icon = "🚗", Color = "#2196F3", DefaultType = TransactionType.Expense, UserId = user.Id },
                    new Category { Name = "Кафе", Icon = "🍕", Color = "#FF9800", DefaultType = TransactionType.Expense, UserId = user.Id },
                    new Category { Name = "Развлечения", Icon = "🎬", Color = "#9C27B0", DefaultType = TransactionType.Expense, UserId = user.Id },
                    new Category { Name = "Зарплата", Icon = "💰", Color = "#4CAF50", DefaultType = TransactionType.Income, UserId = user.Id },
                    new Category { Name = "Фриланс", Icon = "💻", Color = "#00BCD4", DefaultType = TransactionType.Income, UserId = user.Id },
                    new Category { Name = "Подарки", Icon = "🎁", Color = "#E91E63", DefaultType = TransactionType.Income, UserId = user.Id }
                };
                        await _context.Categories.AddRangeAsync(defaultCategories);
                        await _context.SaveChangesAsync();
                    }

                    var token = GenerateJwtToken(user);
                    Response.Cookies.Append("jwt", token, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = false,
                        Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60"))
                    });
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("About", "Home");
        }

        private string GenerateJwtToken(AppUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "supersecretkeywithatleast32characterslong!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}