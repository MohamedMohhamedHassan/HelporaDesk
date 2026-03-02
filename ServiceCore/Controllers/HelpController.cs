using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ServiceCore.Controllers
{
    [Authorize]
    public class HelpController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HelpController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message is required." });

            var apiKey = _configuration["AI:ApiKey"];
            var baseUrl = _configuration["AI:BaseUrl"] ?? "https://api.openai.com/v1/";
            var model = _configuration["AI:Model"] ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("YOUR_API_KEY"))
                return Ok(new { reply = "⚠️ AI not configured. Please add your API key to `appsettings.json` under `AI.ApiKey`." });

            var messages = new List<object>
            {
                new {
                    role = "system",
                    content = @"You are HelporaAI, an expert IT support assistant.
Your ONLY purpose is to help with technical IT questions (Hardware, Software, Networking, Cloud, SysAdmin).
If a question is NOT related to IT or technology, politely decline and redirect them.
Be concise, professional, and use markdown for code blocks."
                }
            };

            if (request.History != null)
            {
                foreach (var h in request.History.TakeLast(10))
                {
                    messages.Add(new { role = h.Role, content = h.Content });
                }
            }

            messages.Add(new { role = "user", content = request.Message });

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = model,
                messages = messages,
                max_tokens = 1024,
                temperature = 0.3
            };

            try
            {
                var jsonBody = JsonSerializer.Serialize(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                
                // Ensure URL ends with chat/completions
                var finalUrl = baseUrl.EndsWith("/") ? baseUrl + "chat/completions" : baseUrl + "/chat/completions";
                
                var response = await client.PostAsync(finalUrl, content);
                var responseStr = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"❌ API Error `{(int)response.StatusCode}`.";
                    try {
                        using var errDoc = JsonDocument.Parse(responseStr);
                        if (errDoc.RootElement.TryGetProperty("error", out var errorEl) && 
                            errorEl.TryGetProperty("message", out var msgEl))
                        {
                            errorMsg += $" Details: **{msgEl.GetString()}**";
                        }
                    } catch { }
                    return Ok(new { reply = errorMsg });
                }

                using var doc = JsonDocument.Parse(responseStr);
                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                return Ok(new { reply = $"❌ Connection error: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public List<ChatHistoryItem>? History { get; set; }
    }

    public class ChatHistoryItem
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }
}
