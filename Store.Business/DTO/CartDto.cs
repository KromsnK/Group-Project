namespace Store.Business.DTO;
public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
}
