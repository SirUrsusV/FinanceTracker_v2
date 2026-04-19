using FinanceTracker.Data;
using FinanceTracker.DTO;
using FinanceTracker.Extensions;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinanceTracker.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        [Route("Categories")]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var categories = await _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.DefaultType)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var transactions = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();

            var stats = transactions
                .GroupBy(t => t.CategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => new { Count = g.Count(), Sum = g.Sum(t => t.Amount) }
                );

            var categoryDtos = categories.ToDtoList();
            foreach (var dto in categoryDtos)
            {
                if (stats.TryGetValue(dto.Id, out var stat))
                {
                    dto.TransactionCount = stat.Count;
                    dto.TotalAmount = stat.Sum;
                }
            }

            return View(categoryDtos);
        }

        public IActionResult Create()
        {
            return View(new CategoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDto dto)
        {
            if (ModelState.IsValid)
            {
                var userId = GetUserId();
                var category = new Category
                {
                    Name = dto.Name,
                    Icon = dto.Icon,
                    Color = dto.Color,
                    DefaultType = string.IsNullOrEmpty(dto.DefaultType) ? null : System.Enum.Parse<TransactionType>(dto.DefaultType),
                    UserId = userId
                };
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Категория создана!";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null) return NotFound();
            var dto = category.ToDto();
            dto.DefaultType = category.DefaultType?.ToString();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryDto dto)
        {
            if (id != dto.Id) return NotFound();
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null) return NotFound();

            if (ModelState.IsValid)
            {
                category.Name = dto.Name;
                category.Icon = dto.Icon;
                category.Color = dto.Color;
                category.DefaultType = string.IsNullOrEmpty(dto.DefaultType) ? null : System.Enum.Parse<TransactionType>(dto.DefaultType);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Категория обновлена!";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category == null) return NotFound();
            var hasTransactions = await _context.Transactions.AnyAsync(t => t.CategoryId == id && t.UserId == userId);
            if (hasTransactions)
            {
                TempData["Error"] = "Невозможно удалить категорию: есть транзакции.";
                return RedirectToAction(nameof(Index));
            }
            return View(category.ToDto());
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (category != null) _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Категория удалена!";
            return RedirectToAction(nameof(Index));
        }
    }
}