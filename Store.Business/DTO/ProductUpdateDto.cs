using System.ComponentModel.DataAnnotations;

namespace Store.Business.DTO;

public class ProductUpdateDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Range(0, 10000)]
    public int StockQuantity { get; set; }
}
