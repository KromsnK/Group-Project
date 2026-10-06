namespace Store.Data.Entities
{
    public class SearchHistory : BaseEntity
    {
        public string SearchQuery { get; set; } = string.Empty;
        public DateTime SearchDate { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
