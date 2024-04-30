using System.ComponentModel.DataAnnotations;

namespace MDS_PROJECT.Models
{
    public class SearchResults
    {
        [Key]
        public int Id { get; set; }

        public string Retailer { get; set; }

        public string ItemName { get; set; }

        public string Quantity { get; set; }

        public string MeasureQuantity { get; set; }

        public string Price { get; set; }
    }
}
