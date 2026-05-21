using System.Text;
using System.Text.Json;

namespace HR_App.Services
{
    public class AiTranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint = "http://localhost:11434/api/chat";
        private readonly string _model = "llama3";

        public AiTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Translate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var prompt = $@"
Convert Arabic to bilingual text (Arabic + English hints).

Do NOT change meaning.
Do NOT remove words.
Return ONE sentence.

TEXT:
{input}";

            var body = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                stream = false
            };

            var res = await _httpClient.PostAsync(
                _endpoint,
                new StringContent(JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json"));

            var json = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? input;
        }
    }
}