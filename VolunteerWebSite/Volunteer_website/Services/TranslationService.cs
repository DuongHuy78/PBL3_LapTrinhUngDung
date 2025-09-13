using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TranslationService(HttpClient httpClient, string baseUrl = "https://libretranslate.de")
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _baseUrl = baseUrl;
    }

    public async Task<string> TranslateText(string text, string sourceLang, string targetLang)
    {
        var requestData = new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync($"{_baseUrl}/translate", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Translation API error: {response.StatusCode} - {errorMsg}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var responseObj = JsonSerializer.Deserialize<TranslationResponse>(responseString);

        return responseObj?.TranslatedText ?? string.Empty;
    }
}

public class TranslationResponse
{
    public string TranslatedText { get; set; }
}
