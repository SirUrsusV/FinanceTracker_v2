using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Иконка")]
        [StringLength(10)]
        public string? Icon { get; set; }

        [Display(Name = "Цвет")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Формат #RRGGBB")]
        public string? Color { get; set; }

        [Display(Name = "Тип по умолчанию")]
        public TransactionType? DefaultType { get; set; }

        public virtual ICollection<Transaction>? Transactions { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }
}