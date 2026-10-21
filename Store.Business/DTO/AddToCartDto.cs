using System.ComponentModel.DataAnnotations;

namespace Store.Business.DTO;

public class AddToCartDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Кількість повинна бути від 1 до 100.")]
    public int Quantity { get; set; }
}
