using FinanceTracker.DTO;
using FinanceTracker.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceTracker.Extensions
{
    public static class MappingExtensions
    {
        public static TransactionDto ToDto(this Transaction transaction)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category?.Name ?? string.Empty,
                CategoryIcon = transaction.Category?.Icon ?? string.Empty,
                CategoryColor = transaction.Category?.Color ?? "#6c757d",
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }

        public static List<TransactionDto> ToDtoList(this IEnumerable<Transaction> transactions)
            => transactions.Select(t => t.ToDto()).ToList();

        public static CategoryDto ToDto(this Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Icon = category.Icon,
                Color = category.Color,
                DefaultType = category.DefaultType?.ToString(),
                TransactionCount = 0,
                TotalAmount = 0
            };
        }

        public static List<CategoryDto> ToDtoList(this IEnumerable<Category> categories)
            => categories.Select(c => c.ToDto()).ToList();

        public static async Task<List<CategoryDto>> ToDtoListAsync(this IQueryable<Category> categories)
        {
            var list = await categories.ToListAsync();
            return list.ToDtoList();
        }

        public static Transaction ToEntity(this TransactionCreateUpdateDto dto, string userId)
        {
            return new Transaction
            {
                Id = dto.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                Date = dto.Date,
                Type = Enum.Parse<TransactionType>(dto.Type),
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.Now,
                UserId = userId
            };
        }

        public static void UpdateEntity(this TransactionCreateUpdateDto dto, Transaction entity)
        {
            entity.Amount = dto.Amount;
            entity.Description = dto.Description;
            entity.Date = dto.Date;
            entity.Type = Enum.Parse<TransactionType>(dto.Type);
            entity.CategoryId = dto.CategoryId;
            entity.UpdatedAt = DateTime.Now;
        }
    }
}