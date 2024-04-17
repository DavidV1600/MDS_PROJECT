using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

public class Product
{
    public string Name { get; set; }
    public string Price { get; set; }
    // Add more properties as needed
}

namespace MDS_PROJECT.Controllers
{
    
}

public class ProductsController : Controller
{
    public async Task<ActionResult> Index(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return Content("Please provide a search query.");
        }

        // URL of the search results page
        string searchUrl = $"https://www.mega-image.ro/search?q={query}";

        // Create HttpClient instance to send HTTP requests
        using (HttpClient client = new HttpClient())
        {
            // Set User-Agent header to mimic a real web browser
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            // Send a GET request to the search URL and retrieve the HTML content
            HttpResponseMessage searchResponse = await client.GetAsync(searchUrl);

            // Check if the request was successful (status code 200)
            if (searchResponse.IsSuccessStatusCode)
            {
                // Parse the HTML content using HtmlAgilityPack
                HtmlDocument searchDocument = new HtmlDocument();
                string searchHtmlContent = await searchResponse.Content.ReadAsStringAsync();
                searchDocument.LoadHtml(searchHtmlContent);

                // Find all href links containing the input word in the href attribute
                var hrefNodes = searchDocument.DocumentNode.SelectNodes($"//a[contains(@href, '{query}')]");

                // List to store href link text
                List<string> hrefTexts = new List<string>();

                // Extract href link text
                if (hrefNodes != null)
                {
                    foreach (var hrefNode in hrefNodes)
                    {
                        // Extract href link text
                        string hrefText = hrefNode.InnerText.Trim();
                        hrefTexts.Add(hrefText);
                    }
                }

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