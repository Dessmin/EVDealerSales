using EVDealerSales.Business.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace EVDealerSales.Business.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["Gemini:ApiKey"]
                      ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                      ?? throw new Exception("Gemini API key not configured.");
        }
        public async Task<string> GetGeminiResponseAsync(string userPrompt)
        {
            var fullPrompt = $"{GeminiContext.SystemPrompt}\n\"{userPrompt}\"";

            var baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

            // Decide whether the configured key is a simple API key (Google API key format begins with "AIza")
            // or an OAuth2 access token. If it looks like an API key, it must be passed as the query parameter `key`.
            var useApiKeyInQuery = _apiKey.StartsWith("AIza", StringComparison.OrdinalIgnoreCase) || _apiKey.StartsWith("AIzaSy", StringComparison.OrdinalIgnoreCase);
            var url = useApiKeyInQuery ? baseUrl + "?key=" + Uri.EscapeDataString(_apiKey) : baseUrl;

            var body = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text = fullPrompt }
                    }
                }
            }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            // If not using API key in query, assume the configured value is an OAuth2 access token and use it as a Bearer token.
            if (!useApiKeyInQuery)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                // Detect common invalid credentials patterns and provide actionable guidance
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorBody != null && (errorBody.Contains("API key expired", StringComparison.OrdinalIgnoreCase) || errorBody.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception("Gemini API key is invalid or expired. Please renew the API key and update configuration.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (useApiKeyInQuery)
                    {
                        throw new Exception("Gemini returned 401/403. The provided API key (query) appears to be invalid or lacks permissions. Verify the key and its restrictions.");
                    }
                    else
                    {
                        throw new Exception("Gemini returned 401/403. The provided bearer token appears invalid; ensure you're supplying a valid OAuth2 access token or service account token.");
                    }
                }

                throw new Exception($"Gemini API error (HTTP {(int)response.StatusCode}): {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Navigate defensively through the expected structure
                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var textProp = parts[0].TryGetProperty("text", out var textVal) ? textVal.GetString() : null;
                        return textProp ?? string.Empty;
                    }
                }

                // Fallback: return raw JSON if structure not as expected
                return json ?? string.Empty;
            }
            catch (JsonException)
            {
                // If parsing fails, return raw content to aid debugging
                return json ?? string.Empty;
            }
        }
    }

        public static class GeminiContext
{
    public const string SystemPrompt =
    """
    You are the EVDealerSales assistant. Your role is to support dealership managers with practical, concise, and safe guidance about:
    - Inventory restocking recommendations
    - Market research and trend monitoring
    - Customer feedback and order behavior analysis
    - Competitive analysis and benchmarking
    - Consulting on importing new vehicles based on market demand

    Primary capabilities you should support:
    - Restocking recommendations (deterministic HTML table output, see rules below).
    - Market research insights: analyze demand trends, competitor actions, and emerging EV technologies.
    - New vehicle sourcing advice: suggest models to import or promote based on demand, profitability, and future trends.
    - Customer feedback analysis: summarize satisfaction trends, identify recurring complaints or praise, and suggest operational improvements.
    - Order tracking insights: identify top-selling vehicles, slow-moving stock, or repeat customers, and recommend next steps.
    - Manager advice and action plans (plain text, bullet points, or numbered steps).
    - Market Q&A: answer short factual or interpretive questions about trends, customer behavior, or inventory metrics.
    - Adding or configuring a vehicle in inventory: provide concise steps or validation checks a manager should perform.

    Tone & Constraints:
    - Reply in English unless the user requests another language.
    - Be concise, professional, and actionable. Prefer short lists or numbered steps for instructions.
    - Do not reveal internal secrets or configuration values (API keys, secrets, system internals).

    When to return a table vs plain text:
    - If the user asks for restocking, new vehicle sourcing, or data-driven recommendations, RETURN A SINGLE SIMPLE HTML TABLE (see table rules). 
      This ensures deterministic rendering in the UI.
    - For all other cases (feedback summaries, how-to, market Q&A, etc.), return short plain text, optionally with bullet points or numbered steps.

    TABLE RULES (STRICT for recommendations):
    - When producing recommendations, ALWAYS return exactly one HTML table.
    - The table MUST use only these tags: <table>, <thead>, <tbody>, <tr>, <th>, <td>. 
      Simple inline tags like <strong>, <em>, <b>, or <i> are allowed inside cells but avoid attributes and styles.
    - Columns MUST appear in this order: Vehicle, Suggested Quantity, Priority, Short Reason.
    - Provide 3–5 rows when possible. Keep the Short Reason short (10–20 words).

    FALLBACKS & SAFE FORMATTING:
    - Never output markdown (no ``` fences), or extra explanatory HTML when returning a table.
    - For non-table answers, use clear paragraphs or bullet points (<200 words unless user requests more detail).

    EXAMPLES:

    - Valid table (for restock/importing recommendations):
    <table>
        <thead>
            <tr><th>Vehicle</th><th>Suggested Quantity</th><th>Priority</th><th>Short Reason</th></tr>
        </thead>
        <tbody>
            <tr><td>Tesla Model 3 RWD</td><td>12</td><td>High</td><td>High satisfaction rate and repeat purchases</td></tr>
            <tr><td>Hyundai Ioniq 6</td><td>7</td><td>Medium</td><td>Positive feedback on comfort and range; growing demand</td></tr>
        </tbody>
    </table>

    - Valid plain-text (feedback & trend analysis):
    "Recent feedback shows increased customer interest in long-range EVs with faster charging. 
    Consider prioritizing imports of vehicles with >500km range and offering extended warranty options."

    ACTIONABLE RESPONSES:
    - For customer feedback: summarize top themes (e.g., delivery delays, vehicle quality, service experience).
    - For order analysis: identify bestsellers, slow movers, or repeat patterns and give next-step recommendations.
    - For importing or restock: output HTML table as described.
    """;
}

}
