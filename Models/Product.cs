using System.ComponentModel.DataAnnotations;

namespace MDS_PROJECT.Models
{
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }

                // Pass the list of href link texts to the view
                return View(hrefTexts);
            }
            else
            {
                // Handle failed HTTP request
                return Content($"Failed to retrieve the search results page: {searchResponse.StatusCode}");
            }
        }
    }
}
