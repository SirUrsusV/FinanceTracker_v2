using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.DTO
{
    public class TransactionCreateUpdateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
        [Display(Name = "Сумма")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Описание обязательно")]
        [StringLength(200, ErrorMessage = "Максимум 200 символов")]
        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Дата обязательна")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Тип обязателен")]
        [Display(Name = "Тип")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Категория обязательна")]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        public List<CategoryDto>? AvailableCategories { get; set; }
    }
}