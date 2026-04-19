using FinanceTracker.Data;
using FinanceTracker.DTO;
using FinanceTracker.Extensions;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinanceTracker.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<TransactionType>(filter.Type, true, out var type))
                query = query.Where(t => t.Type == type);
            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
                query = query.Where(t => t.CategoryId == filter.CategoryId);
            if (filter.FromDate.HasValue)
                query = query.Where(t => t.Date >= filter.FromDate);
            if (filter.ToDate.HasValue)
                query = query.Where(t => t.Date <= filter.ToDate);
            return query;
        }

        // GET: Transactions
        [Route("Transactions")]
        public async Task<IActionResult> Index(TransactionFilterDto filter)
        {
            var userId = GetUserId();
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId);
            query = ApplyFilters(query, filter);
            var transactions = await query.ToListAsync();

            var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.Balance = totalIncome - totalExpense;
            ViewBag.Categories = await _context.Categories.Where(c => c.UserId == userId).ToDtoListAsync();
            ViewBag.CurrentType = filter.Type;
            ViewBag.CurrentCategoryId = filter.CategoryId;
            ViewBag.FromDate = filter.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = filter.ToDate?.ToString("yyyy-MM-dd");

            return View(transactions.ToDtoList());
        }

        // GET: Transactions/CreateModal
        public async Task<IActionResult> CreateModal()
        {
            var userId = GetUserId();
            var dto = new TransactionCreateUpdateDto
            {
                Date = DateTime.Today,
                AvailableCategories = await _context.Categories.Where(c => c.UserId == userId).ToDtoListAsync()
            };
            return PartialView("_CreateModalPartial", dto);
        }

        // POST: Transactions/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateUpdateDto dto)
        {
            var userId = GetUserId();
            if (ModelState.IsValid)
            {
                var transaction = dto.ToEntity(userId);
                _context.Add(transaction);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            dto.AvailableCategories = await _context.Categories.Where(c => c.UserId == userId).ToDtoListAsync();
            return PartialView("_CreateModalPartial", dto);
        }

        // GET: Transactions/EditModal/5
        public async Task<IActionResult> EditModal(int? id)
        {
            if (id == null) return NotFound();
            var userId = GetUserId();
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction == null) return NotFound();
            var dto = new TransactionCreateUpdateDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                CategoryId = transaction.CategoryId,
                AvailableCategories = await _context.Categories.Where(c => c.UserId == userId).ToDtoListAsync()
            };
            return PartialView("_EditModalPartial", dto);
        }

        // POST: Transactions/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransactionCreateUpdateDto dto)
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == dto.Id && t.UserId == userId);
            if (transaction == null) return NotFound();
            if (ModelState.IsValid)
            {
                dto.UpdateEntity(transaction);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            dto.AvailableCategories = await _context.Categories.Where(c => c.UserId == userId).ToDtoListAsync();
            return PartialView("_EditModalPartial", dto);
        }

        // GET: Transactions/DeleteModal/5
        public async Task<IActionResult> DeleteModal(int? id)
        {
            if (id == null) return NotFound();
            var userId = GetUserId();
            var transaction = await _context.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction == null) return NotFound();
            return PartialView("_DeleteModalPartial", transaction.ToDto());
        }

        // POST: Transactions/DeleteConfirmed (AJAX)
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedAjax(int id)
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction != null) _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET: Transactions/ExportToExcel
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(TransactionFilterDto filter)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var userId = GetUserId();
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId);
            query = ApplyFilters(query, filter);
            var transactions = await query.ToListAsync();

            var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var balance = totalIncome - totalExpense;

            var expensesByCategory = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key?.Name ?? "Без категории", Amount = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var monthlyData = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new { Month = $"{g.Key.Month:00}/{g.Key.Year}", Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount), Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount) })
                .OrderBy(m => m.Month)
                .ToList();

            using (var package = new ExcelPackage())
            {
                // Лист транзакций
                var sheet = package.Workbook.Worksheets.Add("Транзакции");
                sheet.Cells[1, 1].Value = "Дата";
                sheet.Cells[1, 2].Value = "Описание";
                sheet.Cells[1, 3].Value = "Категория";
                sheet.Cells[1, 4].Value = "Сумма";
                sheet.Cells[1, 5].Value = "Тип";
                using (var range = sheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                int row = 2;
                foreach (var t in transactions)
                {
                    sheet.Cells[row, 1].Value = t.Date.ToString("dd.MM.yyyy");
                    sheet.Cells[row, 2].Value = t.Description;
                    sheet.Cells[row, 3].Value = t.Category?.Name;
                    sheet.Cells[row, 4].Value = t.Amount;
                    sheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";
                    sheet.Cells[row, 5].Value = t.Type == TransactionType.Income ? "Доход" : "Расход";
                    row++;
                }
                sheet.Cells.AutoFitColumns();

                // Лист сводки
                var summarySheet = package.Workbook.Worksheets.Add("Сводка");
                summarySheet.Cells[1, 1].Value = "Показатель";
                summarySheet.Cells[1, 2].Value = "Значение";
                summarySheet.Cells[2, 1].Value = "Общий доход";
                summarySheet.Cells[2, 2].Value = totalIncome;
                summarySheet.Cells[2, 2].Style.Numberformat.Format = "#,##0.00 ₽";
                summarySheet.Cells[3, 1].Value = "Общий расход";
                summarySheet.Cells[3, 2].Value = totalExpense;
                summarySheet.Cells[3, 2].Style.Numberformat.Format = "#,##0.00 ₽";
                summarySheet.Cells[4, 1].Value = "Баланс";
                summarySheet.Cells[4, 2].Value = balance;
                summarySheet.Cells[4, 2].Style.Numberformat.Format = "#,##0.00 ₽";
                summarySheet.Cells[1, 1, 4, 2].AutoFitColumns();

                // Лист расходов по категориям
                var catSheet = package.Workbook.Worksheets.Add("Расходы по категориям");
                catSheet.Cells[1, 1].Value = "Категория";
                catSheet.Cells[1, 2].Value = "Сумма";
                int catRow = 2;
                foreach (var cat in expensesByCategory)
                {
                    catSheet.Cells[catRow, 1].Value = cat.Category;
                    catSheet.Cells[catRow, 2].Value = cat.Amount;
                    catSheet.Cells[catRow, 2].Style.Numberformat.Format = "#,##0.00 ₽";
                    catRow++;
                }
                catSheet.Cells.AutoFitColumns();

                // Лист с графиком
                var chartSheet = package.Workbook.Worksheets.Add("График");
                chartSheet.Cells[1, 1].Value = "Месяц";
                chartSheet.Cells[1, 2].Value = "Доход";
                chartSheet.Cells[1, 3].Value = "Расход";
                int chartRow = 2;
                foreach (var md in monthlyData)
                {
                    chartSheet.Cells[chartRow, 1].Value = md.Month;
                    chartSheet.Cells[chartRow, 2].Value = md.Income;
                    chartSheet.Cells[chartRow, 3].Value = md.Expense;
                    chartRow++;
                }
                var chart = chartSheet.Drawings.AddChart("Динамика", OfficeOpenXml.Drawing.Chart.eChartType.Line);
                chart.SetPosition(0, 0, 4, 0);
                chart.SetSize(800, 400);
                chart.Title.Text = "Динамика доходов и расходов";
                var seriesIncome = chart.Series.Add(chartSheet.Cells[2, 2, chartRow - 1, 2], chartSheet.Cells[2, 1, chartRow - 1, 1]);
                seriesIncome.Header = "Доход";
                var seriesExpense = chart.Series.Add(chartSheet.Cells[2, 3, chartRow - 1, 3], chartSheet.Cells[2, 1, chartRow - 1, 1]);
                seriesExpense.Header = "Расход";

                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Транзакции_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}