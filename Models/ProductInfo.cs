namespace MDS_PROJECT.Models
{
    public interface IProductScraper
    {
        Task<List<ProductInfo>> GetProductInfo(string query);
    }

    public class ProductInfo
    {
        public string Name { get; set; }
        public string Price { get; set; }
    }
}
