using System.ComponentModel.DataAnnotations;

namespace MDS_PROJECT.Models
{
	public class Product
	{
		[Key]
		public int Id { get; set; }
		public string Title { get; set; }
		public string Subtitle { get; set; }
		public int Price { get; set; }
		public int Quantity { get; set; }

	}
}