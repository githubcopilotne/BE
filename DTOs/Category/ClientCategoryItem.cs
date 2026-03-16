namespace BE.DTOs.Category
{
    public class ClientCategoryItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}
