
namespace MDS_PROJECT.Models
{
    {

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
