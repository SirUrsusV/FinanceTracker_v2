using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
        [Display(Name = "Сумма")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Тип")]
        public TransactionType Type { get; set; }

        [Required]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Display(Name = "Создано")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Обновлено")]
        public DateTime? UpdatedAt { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }

    public enum TransactionType
    {
        [Display(Name = "Расход")]
        Expense = 0,
        [Display(Name = "Доход")]
        Income = 1
    }
}