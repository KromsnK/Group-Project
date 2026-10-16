using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;

        public ProductService(IRepository<Product> productRepository)
        {
            this._productRepository = productRepository;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await this._productRepository.GetAllAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            });
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await this._productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return null;
            }

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };
        }

        public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity
            };

            await this._productRepository.AddAsync(product);
            await this._productRepository.SaveChangesAsync();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };
        }

        public async Task UpdateAsync(ProductUpdateDto dto)
        {
            var product = await this._productRepository.GetByIdAsync(dto.Id);
            if (product == null)
            {
                throw new InvalidOperationException($"Товар з ID {dto.Id} не знайдено.");
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;

            this._productRepository.Update(product);
            await this._productRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await this._productRepository.GetByIdAsync(id);
            if (product != null)
            {
                this._productRepository.Delete(product);
                await this._productRepository.SaveChangesAsync();
            }
        }
    }
}
