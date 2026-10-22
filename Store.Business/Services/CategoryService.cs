using Microsoft.EntityFrameworkCore;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Data;
using Store.Data.Entities;

namespace Store.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return categories;
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public async Task<CategoryDto> CreateAsync(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public async Task UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new KeyNotFoundException("Категорію не знайдено");

            category.Name = dto.Name;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
