using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Volunteer_website.Controllers
{
    public class BlogController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "e888c7ecc6e54ac08fe09185035258fd"; // Thay bằng API key của bạn

        // Tiêm IHttpClientFactory qua constructor
        public BlogController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(); // Tạo instance HttpClient
        }

        public async Task<IActionResult> Index()
        {
            var articles = await FetchVolunteerArticles();
            return View(articles);
        }

        private async Task<List<Article>> FetchVolunteerArticles()
        {
            try
            {
                var url = $"https://newsapi.org/v2/everything?q=volunteer+Vietnam&sortBy=publishedAt&apiKey={_apiKey}&pageSize=10";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Lỗi API: {response.StatusCode} - {errorContent}");
                    ViewBag.ErrorMessage = $"Lỗi API: {response.StatusCode} - {errorContent}";
                    return new List<Article>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Phản hồi từ API: {responseContent}"); // Log dữ liệu thô để kiểm tra
                var json = JObject.Parse(responseContent);

                if (json["status"]?.ToString() != "ok")
                {
                    Console.WriteLine($"Lỗi từ API: {json["message"]?.ToString()}");
                    ViewBag.ErrorMessage = $"Lỗi từ API: {json["message"]?.ToString()}";
                    return new List<Article>();
                }

                var articles = json["articles"]
                    .Select(a => new Article
                    {
                        Title = a["title"]?.ToString() ?? "No Title", // Sử dụng giá trị mặc định nếu null
                        Description = a["description"]?.ToString() ?? "No Description",
                        Url = a["url"]?.ToString() ?? "",
                        PublishedAt = a["publishedAt"]?.ToString() ?? "",
                        Source = a["source"]?["name"]?.ToString() ?? "Unknown Source"
                    })
                    .Where(a => !string.IsNullOrEmpty(a.Url)) // Chỉ lọc dựa trên URL
                    .ToList();

                Console.WriteLine($"Số bài viết sau khi lọc: {articles.Count}");
                if (!articles.Any())
                {
                    Console.WriteLine("Không tìm thấy bài viết nào với truy vấn đã cho sau khi lọc.");
                }

                return articles;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Lỗi khi gọi API: {ex.Message}");
                ViewBag.ErrorMessage = $"Lỗi khi gọi API: {ex.Message}";
                return new List<Article>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Lỗi khi phân tích JSON: {ex.Message}");
                ViewBag.ErrorMessage = $"Lỗi khi phân tích JSON: {ex.Message}";
                return new List<Article>();
            }
        }
    }

    public class Article
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? PublishedAt { get; set; }
        public string? Source { get; set; }
    }
}
