using FinanceTracker.Data;
using FinanceTracker.DTO;
using FinanceTracker.Extensions;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinanceTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLast6Months = startOfMonth.AddMonths(-5);

            // Загружаем все транзакции за последние 6 месяцев (агрегация в памяти для SQLite)
            var allTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= startOfLast6Months && t.Date <= today)
                .ToListAsync();

            var monthlyTransactions = allTransactions
                .Where(t => t.Date >= startOfMonth && t.Date <= today)
                .ToList();

            var totalIncome = monthlyTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var totalExpense = monthlyTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var totalBalance = totalIncome - totalExpense;

            var recentTransactions = allTransactions
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToDtoList();

            var expensesByCategory = monthlyTransactions
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.Category)
                .Select(g => new CategoryExpenseDto
                {
                    CategoryName = g.Key?.Name ?? "Без категории",
                    CategoryColor = g.Key?.Color ?? "#6c757d",
                    CategoryIcon = g.Key?.Icon ?? "📊",
                    TotalAmount = g.Sum(t => t.Amount),
                    Percentage = totalExpense > 0 ? (int)((g.Sum(t => t.Amount) / totalExpense) * 100) : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            var monthlyData = allTransactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new MonthlyDataDto
                {
                    Month = $"{g.Key.Month:00}/{g.Key.Year}",
                    Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                })
                .OrderBy(m => m.Month)
                .ToList();

            var dashboard = new DashboardDto
            {
                TotalBalance = totalBalance,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                RecentTransactions = recentTransactions,
                ExpensesByCategory = expensesByCategory,
                MonthlyData = monthlyData
            };

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(int months)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var today = DateTime.Today;
            var startDate = today.AddMonths(-months + 1); // +1 чтобы включить текущий месяц
            var startOfMonth = new DateTime(startDate.Year, startDate.Month, 1);

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= startOfMonth && t.Date <= today)
                .ToListAsync();

            var monthlyData = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new MonthlyDataDto
                {
                    Month = $"{g.Key.Month:00}/{g.Key.Year}",
                    Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                })
                .OrderBy(m => m.Month)
                .ToList();

            return Json(monthlyData);
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}