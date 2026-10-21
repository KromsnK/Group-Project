using Microsoft.EntityFrameworkCore;

namespace Store.Data.Entities
{
    public class Order : BaseEntity
    {
        public DateTime OrderDate { get; set; }

        public string OrderStatus { get; set; } = "Pending"; // Default status is "Pending"

        public int UserId { get; set; }

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public User User { get; set; } = null!;
    }
}
