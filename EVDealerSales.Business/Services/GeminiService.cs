using EVDealerSales.Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace EVDealerSales.Business.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _modelUrl;

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiService> logger)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            // Set a reasonable default timeout if not set elsewhere
            if (_httpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }

            _apiKey = config["Gemini:ApiKey"]
                      ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                      ?? throw new InvalidOperationException("Gemini API key not configured.");

            // Allow overriding the model URL via configuration for easy testing or model upgrades
            _modelUrl = config["Gemini:ModelUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        }

        public async Task<string> GetGeminiResponseAsync(string userPrompt)
        {
            if (string.IsNullOrWhiteSpace(userPrompt)) throw new ArgumentException("userPrompt is required.", nameof(userPrompt));

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("Gemini API key is not configured (null or empty).");
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            if (_httpClient == null)
            {
                _logger.LogError("HttpClient is not available for GeminiService.");
                throw new InvalidOperationException("Internal error: HttpClient not available.");
            }

            var fullPrompt = $"{GeminiContext.SystemPrompt}\n\"{userPrompt}\"";

            // Decide whether the configured key is a simple API key (Google API key format begins with "AIza")
            // or an OAuth2 access token. If it looks like an API key, it must be passed as the query parameter `key`.
            var useApiKeyInQuery = _apiKey.StartsWith("AIza", StringComparison.OrdinalIgnoreCase);
            var url = useApiKeyInQuery ? _modelUrl + "?key=" + Uri.EscapeDataString(_apiKey) : _modelUrl;

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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException tex)
            {
                _logger.LogError(tex, "Timeout or request canceled when calling Gemini API");
                throw new InvalidOperationException("Timeout when calling Gemini API.");
            }
            catch (HttpRequestException hex)
            {
                _logger.LogError(hex, "HTTP request failed when calling Gemini API");
                throw new InvalidOperationException("Failed to call Gemini API.");
            }

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log a truncated error body but avoid logging secrets
                var truncated = json?.Length > 1000 ? json.Substring(0, 1000) + "..." : json;

                // Detect common invalid credentials patterns and provide actionable guidance
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    !string.IsNullOrEmpty(truncated) && (truncated.Contains("API key expired", StringComparison.OrdinalIgnoreCase) || truncated.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Gemini API returned bad request indicating invalid/expired key");
                    throw new InvalidOperationException("Gemini API key is invalid or expired. Please renew the API key and update configuration.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Gemini API returned {Status}. Key may be invalid or lack permissions.", response.StatusCode);
                    if (useApiKeyInQuery)
                    {
                        throw new InvalidOperationException("Gemini returned 401/403. The provided API key (query) appears to be invalid or lacks permissions. Verify the key and its restrictions.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Gemini returned 401/403. The provided bearer token appears invalid; ensure you're supplying a valid OAuth2 access token or service account token.");
                    }
                }

                _logger.LogError("Gemini API error (HTTP {Status}): {Body}", (int)response.StatusCode, truncated);
                throw new InvalidOperationException($"Gemini API error (HTTP {(int)response.StatusCode}).");
            }

            try
            {
                using var doc = JsonDocument.Parse(json ?? string.Empty);
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
                        if (!string.IsNullOrEmpty(textProp)) return textProp!;
                    }
                }

                // Fallback: search recursively for the first "text" string value anywhere in the JSON
                var fallback = FindFirstTextProperty(root);
                if (!string.IsNullOrEmpty(fallback)) return fallback;

                // Final fallback: return raw JSON
                return json ?? string.Empty;
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex, "Failed to parse Gemini JSON response; returning raw content");
                return json ?? string.Empty;
            }
        }

        // Recursively search for a property named "text" with a string value
        private static string? FindFirstTextProperty(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "text", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                        {
                            return prop.Value.GetString();
                        }
                        var found = FindFirstTextProperty(prop.Value);
                        if (!string.IsNullOrEmpty(found)) return found;
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        var found = FindFirstTextProperty(item);
                        if (!string.IsNullOrEmpty(found)) return found;
                    }
                    break;
                default:
                    break;
            }
            return null;
        }
    }

    public static class GeminiContext
{
    public const string SystemPrompt = """
    You are an AI Electric Vehicle dealership consultant. 
    Your role is to support dealership managers with practical, concise, and safe guidance about:
    - Inventory restocking recommendations
    - Market research and trend monitoring
    - Customer feedback and order behavior analysis
    - Competitive analysis and benchmarking
    - Consulting on importing new vehicles based on market demand

    You have access to general EV market knowledge, allowing you to recommend both current and upcoming EV models, 
    even if they are not currently in the dealer’s inventory.

    Primary capabilities you should support:
    - Restocking recommendations (deterministic HTML table output, see rules below).
    - Market research insights: analyze demand trends, competitor actions, and emerging EV technologies.
    - New vehicle sourcing advice: suggest models to import or promote based on demand, profitability, and future trends.
      You may recommend new EV models that are not currently in stock if market data supports their potential.
    - Customer feedback analysis: summarize satisfaction trends, identify recurring complaints or praise, 
      and suggest operational improvements.
    - Order tracking insights: identify top-selling vehicles, slow-moving stock, or repeat customers, 
      and recommend next steps.
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
    - For all other cases (feedback summaries, how-to, market Q&A, etc.), return short plain text,
      optionally with bullet points or numbered steps.

    TABLE RULES (STRICT for recommendations):
    - When producing recommendations, ALWAYS return exactly one HTML table.
    - The table MUST use only these tags: <table>, <thead>, <tbody>, <tr>, <th>, <td>.
      Simple inline tags like <strong>, <em>, <b>, or <i> are allowed inside cells but avoid attributes and styles.
    - Columns MUST appear in this order: Vehicle, Suggested Quantity, Priority, Short Reason.
    - Provide 3–5 rows when possible. Include both in-stock and potential new models if relevant.
      Mark new model suggestions clearly in the Short Reason (e.g., "New model, rising demand in Asia").
    - Keep the Short Reason short (10–20 words).

    FALLBACKS & SAFE FORMATTING:
    - Never output markdown (no fences), or extra explanatory HTML when returning a table.
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
            <tr><td>BYD Seal</td><td>5</td><td>High</td><td>New model, strong sales growth in Southeast Asia</td></tr>
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
