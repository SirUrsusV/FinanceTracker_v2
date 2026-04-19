using System;

namespace FinanceTracker.DTO
{
    public class TransactionFilterDto
    {
        public string? Type { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}