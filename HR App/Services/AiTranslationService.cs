using System.Text;
using System.Text.Json;

namespace HR_App.Services
{
    public class AiTranslationService
    {
        private readonly HttpClient _httpClient;

        private readonly string _ollamaEndpoint =
            "http://localhost:11434/api/chat";

        private readonly string _modelName = "llama3";

        public AiTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.Timeout =
                TimeSpan.FromSeconds(30);
        }

        public async Task<string> ExpandArabicQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return query;
            }

            try
            {
                string prompt = $@"
You are a smart bilingual assistant.

Your task:
Expand Arabic business/store/item/location names
into English equivalents.

Rules:
- Keep original Arabic words.
- Add English equivalent beside them.
- Return ONE sentence only.
- No explanations.
- No markdown.
- No bullets.
- No quotes.

Examples:

الشيخ زايد
=
الشيخ زايد Sheikh Zayed

التجمع
=
التجمع Tagamoa Fifth Settlement

كوكاكولا
=
كوكاكولا Coca Cola

مدينة نصر
=
مدينة نصر Nasr City

المهندسين
=
المهندسين Mohandessin

Sentence:
" + query;

                var requestBody = new
                {
                    model = _modelName,

                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },

                    stream = false,

                    options = new
                    {
                        temperature = 0,
                        top_p = 0.1,
                        num_predict = 100
                    }
                };

                var jsonPayload =
                    JsonSerializer.Serialize(requestBody);

                var content =
                    new StringContent(
                        jsonPayload,
                        Encoding.UTF8,
                        "application/json"
                    );

                var response =
                    await _httpClient.PostAsync(
                        _ollamaEndpoint,
                        content
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return query;
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument doc =
                    JsonDocument.Parse(json);

                string result =
                    doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? query;

                result = result.Trim();

                // حماية ضد الهلاوس
                if (result.Length > 500)
                {
                    return query;
                }

                return result;
            }
            catch
            {
                return query;
            }
        }
    }
}