namespace Store.Data.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "User"; // Default role is "User"

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ICollection<SearchHistory> SearchHistory { get; set; } = new List<SearchHistory>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
