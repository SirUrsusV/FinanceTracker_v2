using System.Collections.Generic;

namespace FinanceTracker.DTO
{
    public class DashboardDto
    {
        public decimal TotalBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public List<TransactionDto> RecentTransactions { get; set; } = new();
        public List<CategoryExpenseDto> ExpensesByCategory { get; set; } = new();
        public List<MonthlyDataDto> MonthlyData { get; set; } = new();
    }

    public class CategoryExpenseDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = "#6c757d";
        public string CategoryIcon { get; set; } = "📊";
        public decimal TotalAmount { get; set; }
        public int Percentage { get; set; }
    }

    public class MonthlyDataDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}