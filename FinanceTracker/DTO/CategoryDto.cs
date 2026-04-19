using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.DTO
{
    public class CategoryDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        public string? Icon { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Введите HEX цвет (#RRGGBB)")]
        public string? Color { get; set; }

        public string? DefaultType { get; set; }

        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}