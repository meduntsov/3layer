using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DeveloperPlatform.Api.Services;

public class AiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<string> AnalyzeAsync(object payload)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "AI fallback: HVAC станет критическим через 30 дней; 3 пакета заблокированы решениями; 1 сценарий не готов; Риск сдвига ввода: высокий.";
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var request = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = "Ты PM-ассистент. Дай краткий риск-анализ проекта на русском языке." },
                new { role = "user", content = JsonSerializer.Serialize(payload) }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "AI не вернул ответ";
    }
}
