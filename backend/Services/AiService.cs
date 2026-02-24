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
            return "AI fallback:\n1) Риски: HVAC станет критическим через 30 дней; 3 пакета заблокированы решениями; 1 сценарий не готов.\n2) Ссылки на пакеты: /api/packages/{projectId}#package-<id>.\n3) Ссылки на сценарии: /api/integration/{projectId}#scenario-<id>.\n4) Связи: каждый сценарий зависит от связанных пакетов исполнения (scenarioPackageLinks), поэтому блокировки и незавершенные пакеты напрямую снижают готовность сценариев.";
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var request = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Ты PM-ассистент. Сформируй структурированный отчет на русском языке: " +
                              "(1) ключевые риски, " +
                              "(2) ссылки на пакеты (из поля packages[].Link), " +
                              "(3) ссылки на сценарии (из поля scenarios[].Link), " +
                              "(4) явные связи сценариев и пакетов из scenarioPackageLinks, " +
                              "(5) краткое объяснение, почему эти связи важны для сроков/рисков. " +
                              "Для каждого тезиса используй конкретные имена и ссылки из входных данных."
                },
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
