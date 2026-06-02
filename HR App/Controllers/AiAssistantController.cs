using HR_App.Controllers;
using HR_App.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AiAssistantController : BaseController
{
    private readonly string connStr =
        "Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Encrypt=False;TrustServerCertificate=True;";

    private readonly string _openRouterApiKey;
    private readonly IMemoryCache _cache;

    public AiAssistantController(IConfiguration configuration, IMemoryCache cache)
    {
        _cache = cache;
        _openRouterApiKey = configuration["OpenRouterSettings:ApiKey"];

        if (string.IsNullOrEmpty(_openRouterApiKey))
            throw new Exception("Missing API Key");
    }

    public IActionResult Index()
    {
        return View();
    }

    // =====================================================
    // MAIN AI ENDPOINT
    // =====================================================
    [HttpPost]
    public async Task<JsonResult> AskAi(string question, string userId = "1", int? sessionId = null)
    {
        var responseModel = new AiQueryVM { UserQuestion = question };

        if (string.IsNullOrWhiteSpace(question))
        {
            responseModel.ErrorMessage = "اكتب السؤال أولاً";
            return Json(responseModel);
        }

        try
        {
            // 1. Create or Get Session
            int chatSessionId = sessionId ?? CreateSession(userId, question);

            // 2. Save user message
            SaveMessage(chatSessionId, "user", question);

            // 3. Load history
            var history = LoadMessages(chatSessionId);

            // 🔥 5. Enhance question
            string enhancedQuestion = EnhanceQuestion(history, question);

            // 🔥 cache key ذكي
            string contextKey = string.Join("|", history
                .Where(x => x.Role == "user")
                .TakeLast(3)
                .Select(x => x.Content.ToLower()));

            string cacheKey = $"ai_{enhancedQuestion}_{contextKey}";

            // 🔥 check cache
            if (_cache.TryGetValue(cacheKey, out CachedAiResult cached))
            {
                SaveMessage(chatSessionId, "assistant", cached.ResultValue);

                return Json(new
                {
                    success = true,
                    sessionId = chatSessionId,
                    sql = cached.SqlGenerated,
                    resultValue = cached.ResultValue
                });
            }

            // 6. Generate SQL
            string sql = await GetSqlFromGemini(history, enhancedQuestion);

            if (IsDangerous(sql))
            {
                SaveMessage(chatSessionId, "assistant", "Blocked Query");
                return Json(new { error = "dangerous sql" });
            }


            var result = ExecuteQuery(sql);

            var response = new
            {
                columns = result.FirstOrDefault()?.Keys,
                rows = result
            };

            string jsonResult = JsonSerializer.Serialize(response);
            // 8. Save assistant response + sql
            SaveMessage(chatSessionId, "assistant", jsonResult);
            SaveMessage(chatSessionId, "sql", sql);

            // 9. Cache
            _cache.Set(cacheKey, new CachedAiResult
            {
                SqlGenerated = sql,
                ResultValue = jsonResult
            },
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Json(new
            {
                success = true,
                sessionId = chatSessionId,
                sql = sql,
                resultValue = jsonResult // 👈 رجّع object مش string
            });
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
    private void SaveMessage(int sessionId, string role, string message)
    {
        using var con = new SqlConnection(connStr);

        using var cmd = new SqlCommand(@"
        INSERT INTO ChatMessages (SessionId, Role, Message)
        VALUES (@SessionId, @Role, @Message)", con);

        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@Message", message);

        con.Open();
        cmd.ExecuteNonQuery();
    }
    private int CreateSession(string userId, string title)
    {
        using var con = new SqlConnection(connStr);

        using var cmd = new SqlCommand(@"
        INSERT INTO ChatSessions (UserId, Title)
        OUTPUT INSERTED.Id
        VALUES (@UserId, @Title)", con);

        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Title", title);

        con.Open();

        return (int)cmd.ExecuteScalar();
    }
    private List<ChatMessage> LoadMessages(int sessionId)
    {
        var list = new List<ChatMessage>();

        using var con = new SqlConnection(connStr);

        using var cmd = new SqlCommand(@"
        SELECT TOP 20 Role, Message
        FROM ChatMessages
        WHERE SessionId = @SessionId
        ORDER BY Id ASC", con);

        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        con.Open();

        var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ChatMessage
            {
                Role = reader["Role"].ToString(),
                Content = reader["Message"].ToString()
            });
        }

        return list;
    }

    // =====================================================
    // CHAT MEMORY MODEL
    // =====================================================
    private class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    // =====================================================
    // LOAD HISTORY
    // =====================================================
    private List<ChatMessage> LoadHistory(string key)
    {
        var json = HttpContext.Session.GetString(key);

        if (string.IsNullOrEmpty(json))
            return new List<ChatMessage>();

        return JsonSerializer.Deserialize<List<ChatMessage>>(json)
               ?? new List<ChatMessage>();
    }

    // =====================================================
    // SAVE HISTORY (FIXED + SMART)
    // =====================================================
    private void SaveHistory(string key,
                             List<ChatMessage> history,
                             string userQuestion,
                             string result,
                             string sql)
    {
        history.Add(new ChatMessage { Role = "user", Content = userQuestion });
        history.Add(new ChatMessage { Role = "assistant", Content = result });
        history.Add(new ChatMessage { Role = "sql", Content = sql });

        // keep last 12 messages only
        if (history.Count > 12)
            history = history.Skip(history.Count - 12).ToList();

        HttpContext.Session.SetString(key, JsonSerializer.Serialize(history));
    }

    // =====================================================
    // CONTEXT ENGINE (IMPORTANT FIX)
    // =====================================================
    private string EnhanceQuestion(List<ChatMessage> history, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return question;

        // =============================
        // 🔤 Normalize
        // =============================
        string NormalizeArabic(string text)
        {
            return text
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ة", "ه")
                .Replace("ى", "ي")
                .Replace("ؤ", "و")
                .Replace("ئ", "ي");
        }

        bool HasAny(string text, params string[] keys)
            => keys.Any(k => text.Contains(k));

        int? ExtractNumber(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            if (match.Success)
                return int.Parse(match.Value);

            var map = new Dictionary<string, int>
        {
            {"واحد",1},{"اتنين",2},{"اثنين",2},{"ثلاثه",3},{"اربعه",4},
            {"خمسه",5},{"سته",6},{"سبعه",7},{"تمانيه",8},{"تسعه",9},{"عشره",10}
        };

            foreach (var k in map)
                if (text.Contains(k.Key))
                    return k.Value;

            return null;
        }

        string q = NormalizeArabic(question.ToLower());

        // =============================
        // 🧠 Context
        // =============================
        var lastMessages = history?
            .Where(x => x.Role == "user")
            .TakeLast(3)
            .Select(x => x.Content.ToLower()) ?? new List<string>();

        string context = string.Join(" ", lastMessages);
        string last = lastMessages.LastOrDefault() ?? "";

        // =============================
        // 🔁 Follow-up
        // =============================
        bool isFollowUp =
            q.Length < 20 ||
            HasAny(q, "طب", "طيب", "كمان", "وال", "بعد كده");

        if (isFollowUp && !string.IsNullOrEmpty(context))
            question = context + " + " + question;

        // =============================
        // 🔢 Numbers
        // =============================
        var number = ExtractNumber(q);

        // =============================
        // 🎯 Intent
        // =============================
        var intents = new List<string>();

        if (HasAny(q, "قارن", "فرق"))
            intents.Add("COMPARISON");

        if (HasAny(q, "اعلى", "اكبر", "افضل"))
            intents.Add($"TOP {(number ?? 1)}");

        if (HasAny(q, "اقل", "اصغر"))
            intents.Add($"LOW {(number ?? 1)}");

        if (HasAny(q, "كل", "جميع"))
            intents.Add("GROUP");

        if (intents.Any())
            question += $" (INTENT: {string.Join(", ", intents)})";

        // =============================
        // 📊 Sorting
        // =============================
        if (HasAny(q, "تنازلي", "من الاكبر"))
            question += " (ORDER: DESC)";

        if (HasAny(q, "تصاعدي", "من الاصغر"))
            question += " (ORDER: ASC)";

        // =============================
        // 📅 Date
        // =============================
        if (q.Contains("اول امبارح"))
            question += " (DATE: DAY-2)";
        else if (q.Contains("امبارح"))
            question += " (DATE: YESTERDAY)";
        else if (HasAny(q, "النهارده", "اليوم"))
            question += " (DATE: TODAY)";
        else if (q.Contains("الشهر"))
            question += " (DATE: MONTH)";

        // =============================
        // 🎯 Targets
        // =============================
        if (HasAny(q, "فرع"))
            question += " (TARGET: StoreName)";

        if (HasAny(q, "منتج", "صنف"))
            question += " (TARGET: ItemName)";

        if (HasAny(q, "شركه", "فرانشايز"))
            question += " (TARGET: StoreFranchise)";

        // =============================
        // 💰 Metrics
        // =============================
        if (HasAny(q, "مبيعات", "ايراد"))
            question += " (METRIC: TotalSales)";

        if (HasAny(q, "كميه", "عدد"))
            question += " (METRIC: Qty)";

        // =============================
        // 🧠 Smart Inheritance
        // =============================
        if (isFollowUp)
        {
            if (!q.Contains("مبيعات") && last.Contains("مبيعات"))
                question += " (METRIC: TotalSales)";

            if (!q.Contains("فرع") && last.Contains("فرع"))
                question += " (TARGET: StoreName)";
        }

        return question;
    }
    // =====================================================
    // AI CALL
    // =====================================================
    private async Task<string> GetSqlFromGemini(List<ChatMessage> history, string question)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = GetPrompt()
            }
        };

        foreach (var msg in history.TakeLast(8))
        {
            messages.Add(new
            {
                role = msg.Role == "sql" ? "assistant" : msg.Role,
                content = msg.Content
            });
        }

        messages.Add(new
        {
            role = "user",
            content = question
        });

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openRouterApiKey);

        var body = new
        {
            model = "deepseek/deepseek-chat",
            messages,
            temperature = 0.1,
            max_tokens = 300
        };

        var json = JsonSerializer.Serialize(body);

        var res = await client.PostAsync(
            "https://openrouter.ai/api/v1/chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json"));

        var result = await res.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(result);

        string sql = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Clean(sql);
    }

    // =====================================================
    // PROMPT
    // =====================================================
    private string GetPrompt()
    {
        return @"
You are SQL Server expert.

Table: RptSalesDynamic
Columns:
StoreName, StoreFranchise, ItemName, Qty, TotalSales, TransDate

Mapping Rules (VERY IMPORTANT):
- كلمة 'فرع' أو 'فروع' = StoreName
- كلمة 'شركة' أو 'فرانشايز' = StoreFranchise
- كلمة 'منتج' أو 'صنف' = ItemName
- كلمة 'مبيعات' = SUM(TotalSales)
- كلمة 'كمية' = SUM(Qty)
IMPORTANT MATCHING RULE:
- When filtering StoreName:
  ALWAYS use LIKE with LOWER
  AND match both Arabic and English loosely

Example:
WHERE LOWER(StoreName) LIKE LOWER(N'%zayed%')
   OR LOWER(StoreName) LIKE LOWER(N'%زايد%')
STRICT RULES:
- Return ONLY raw SQL query
- Do NOT include any explanation
- Do NOT include any text before or after SQL
- Do NOT say 'Here is the query'
- Output must start with SELECT or WITH only
";
    }

    // =====================================================
    // EXECUTE SQL
    // =====================================================
    private List<Dictionary<string, object>> ExecuteQuery(string sql)
    {
        var result = new List<Dictionary<string, object>>();

        using var con = new SqlConnection(connStr);
        using var cmd = new SqlCommand(sql, con);

        con.Open();

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader[i];
            }

            result.Add(row);
        }

        return result;
    }
    private string ExecuteReadOnlyQuery(string sql)
    {
        using var con = new SqlConnection(connStr);
        using var cmd = new SqlCommand(sql, con);

        con.Open();

        using var reader = cmd.ExecuteReader();

        if (!reader.HasRows)
            return "No data found";

        var sb = new StringBuilder();

        // Headers
        for (int i = 0; i < reader.FieldCount; i++)
        {
            sb.Append(reader.GetName(i) + "\t");
        }
        sb.AppendLine();

        // Rows
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                sb.Append(reader[i]?.ToString() + "\t");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // =====================================================
    // SAFETY
    // =====================================================
    private bool IsDangerous(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return true;

        sql = sql.ToUpper();

        return sql.Contains("DROP")
            || sql.Contains("DELETE")
            || sql.Contains("UPDATE")
            || sql.Contains("INSERT")
            || sql.Contains("ALTER")
            || sql.Contains("TRUNCATE");
    }

    // =====================================================
    // CLEAN SQL
    // =====================================================
    private string Clean(string sql)
    {
        return sql
            .Replace("```sql", "")
            .Replace("```", "")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim();
    }
}

// =====================================================
// CACHE MODEL
// =====================================================
public class CachedAiResult
{
    public string SqlGenerated { get; set; }
    public string ResultValue { get; set; }
}