using Store.Business.DTO;

namespace Store.Business.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CategoryCreateDto dto);
        Task UpdateAsync(int id, CategoryUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
