//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Text;
//using System.Text.Json;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Caching.Memory; // 1. تم إضافة مجال الأسماء الخاص بالكاش
//using Microsoft.Extensions.Configuration;
//using HR_App.ViewModel;
//using HR_App.Controllers;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//public class AiAssistantController : BaseController
//{
//    private readonly string connStr = "Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Encrypt=False;TrustServerCertificate=True;";
//    private readonly string[] _geminiApiKeys;
//    private readonly IMemoryCache _cache; // 2. تعيين متغير الكاش الخاص بالنظام
//    private static int _currentKeyIndex = 0;

//    // 3. تحديث الـ Constructor لاستقبال الـ IMemoryCache من خلال الـ Dependency Injection
//    public AiAssistantController(IConfiguration configuration, IMemoryCache cache)
//    {
//        _cache = cache;
//        _geminiApiKeys = configuration.GetSection("GeminiSettings:ApiKeys").Get<string[]>();

//        if (_geminiApiKeys == null || _geminiApiKeys.Length == 0)
//        {
//            throw new Exception("تنبيه: لم يتم العثور على أي مفاتيح API في ملف appsettings.json");
//        }
//    }

//    private string GetCurrentApiKey()
//    {
//        int currentIndex = Thread.VolatileRead(ref _currentKeyIndex);
//        int index = Math.Abs(currentIndex) % _geminiApiKeys.Length;
//        return _geminiApiKeys[index];
//    }

//    private void SwitchToNextKey()
//    {
//        Interlocked.Increment(ref _currentKeyIndex);
//    }

//    public IActionResult Index()
//    {
//        return View();
//    }

//    [HttpPost]
//    public async Task<JsonResult> AskAi(string question)
//    {
//        var responseModel = new AiQueryVM { UserQuestion = question };

//        if (string.IsNullOrEmpty(question))
//        {
//            responseModel.ErrorMessage = "برجاء كتابة سؤال أولاً!";
//            return Json(responseModel);
//        }

//        try
//        {
//            // قراءة تاريخ المحادثة من الـ Session لحفظ السياق
//            var history = new List<KeyValuePair<string, string>>();
//            string sessionKey = "AiChatHistory";
//            string existingHistoryJson = HttpContext.Session.GetString(sessionKey);

//            if (!string.IsNullOrEmpty(existingHistoryJson))
//            {
//                history = JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(existingHistoryJson);
//            }

//            // 4. فحص الكاش: إنشاء مفتاح فريد معتمد على نص السؤال بعد تنظيفه
//            string cacheKey = $"AiCache_{question.Trim().ToLower()}";

//            if (_cache.TryGetValue(cacheKey, out CachedAiResult cachedData))
//            {
//                // [HIT] تم العثور على النتيجة في الكاش -> نملأ الموديل فوراً بدون استدعاء الـ API أو الـ DB
//                responseModel.SqlGenerated = cachedData.SqlGenerated;
//                responseModel.ResultValue = cachedData.ResultValue;

//                // تحديث الـ Session بالطلب لتظل الذاكرة مستمرة مع الـ AI للأسئلة التالية
//                UpdateSessionHistory(history, question, cachedData.SqlGenerated, sessionKey);

//                return Json(responseModel);
//            }

//            // [MISS] في حال عدم وجود كاش مسبق -> نقوم بالدورة الكاملة (الذكاء الاصطناعي + قاعدة البيانات)
//            string generatedSql = await GetSqlFromGemini(history, question);
//            responseModel.SqlGenerated = generatedSql;

//            string upperSql = generatedSql.ToUpper();
//            if (upperSql.Contains("DROP") || upperSql.Contains("DELETE") || upperSql.Contains("UPDATE") || upperSql.Contains("TRUNCATE") || upperSql.Contains("ALTER"))
//            {
//                responseModel.ErrorMessage = "عذراً، الاستعلام الناتج يحتوي على عمليات غير مسموح بها!";
//                return Json(responseModel);
//            }

//            string dbResult = ExecuteReadOnlyQuery(generatedSql);
//            responseModel.ResultValue = dbResult;

//            // تحديث الـ Session بالبيانات الجديدة
//            UpdateSessionHistory(history, question, generatedSql, sessionKey);

//            // 5. تخزين النتيجة الجديدة في الكاش لتسريع الطلبات المتطابقة القادمة
//            var cacheOptions = new MemoryCacheEntryOptions()
//                .SetSlidingExpiration(TimeSpan.FromMinutes(15)) // كاش مرن: يتجدد تلقائياً لمدة 15 دقيقة إضافية كلما سُئل نفس السؤال
//                .SetAbsoluteExpiration(TimeSpan.FromHours(1));   // كاش مطلق: تنتهي صلاحيته تماماً بعد ساعة لتحديث أي أرقام تغيرت بالـ DB

//            var dataToCache = new CachedAiResult
//            {
//                SqlGenerated = generatedSql,
//                ResultValue = dbResult
//            };

//            _cache.Set(cacheKey, dataToCache, cacheOptions);
//        }
//        catch (Exception ex)
//        {
//            responseModel.ErrorMessage = "حدث خطأ أثناء معالجة طلبك: " + ex.Message;
//        }

//        return Json(responseModel);
//    }

//    // دالة مساعدة معزولة لمنع تكرار كود تحديث الـ Session وحصر الـ History في 5 عناصر
//    private void UpdateSessionHistory(List<KeyValuePair<string, string>> history, string question, string sql, string sessionKey)
//    {
//        // التحقق من أن السؤال الأخير ليس مكرراً لتجنب تشويه الـ History
//        if (history.Count == 0 || history[history.Count - 1].Key != question)
//        {
//            history.Add(new KeyValuePair<string, string>(question, sql));

//            if (history.Count > 5)
//            {
//                history.RemoveAt(0);
//            }

//            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));
//        }
//    }

//    private string EnhanceQuestion(string question)
//    {
//        string q = question.ToLower();

//        // MOST SPECIFIC FIRST
//        if (q.Contains("الشهر ده") || q.Contains("this month"))
//            question += " (DATE: THIS_MONTH)";

//        else if (q.Contains("الشهر اللي فات") || q.Contains("last month"))
//            question += " (DATE: LAST_MONTH)";

//        else if (q.Contains("السنة دي") || q.Contains("this year"))
//            question += " (DATE: THIS_YEAR)";

//        else if (q.Contains("السنة اللي فاتت") || q.Contains("last year"))
//            question += " (DATE: LAST_YEAR)";

//        else if (q.Contains("النهارده") || q.Contains("today"))
//            question += " (DATE: TODAY)";

//        else if (q.Contains("امبارح") || q.Contains("yesterday"))
//            question += " (DATE: YESTERDAY)";

//        else if (q.Contains("اول امبارح") || q.Contains("قبل امبارح") || q.Contains("day before yesterday"))
//            question += " (DATE: TWO_DAYS_AGO)";

//        else if (q.Contains("آخر 7 ايام") || q.Contains("اسبوع") || q.Contains("week"))
//            question += " (DATE: LAST_7_DAYS)";

//        else if (q.Contains("آخر 30 يوم") || q.Contains("last 30 days"))
//            question += " (DATE: LAST_30_DAYS)";

//        // INTENT
//        if (q.Contains("قارن") || q.Contains("compare"))
//            question += " (INTENT: COMPARISON, GROUP BY StoreName)";

//        if (q.Contains("اعلى") || q.Contains("اكبر") || q.Contains("اكتر") || q.Contains("top"))
//            question += " (INTENT: TOP, ORDER BY DESC)";

//        if (q.Contains("اقل") || q.Contains("اصغر") || q.Contains("lowest"))
//            question += " (INTENT: LOWEST, ORDER BY ASC)";

//        // TARGET
//        if (q.Contains("فرع") || q.Contains("store"))
//            question += " (TARGET: StoreName)";

//        if (q.Contains("صنف") || q.Contains("منتج") || q.Contains("item"))
//            question += " (TARGET: ItemName)";

//        // SALES TYPE
//        if (q.Contains("فلوس") || q.Contains("مبيعات"))
//            question += " (METRIC: TotalSales)";

//        if (q.Contains("كمية") || q.Contains("وحدات"))
//            question += " (METRIC: Qty)";

//        return question;
//    }
//    private async Task<string> GetSqlFromGemini(List<KeyValuePair<string, string>> conversationHistory, string currentUserQuery)
//    {
//        currentUserQuery = EnhanceQuestion(currentUserQuery);
//        string schemaPrompt = @"
//    You are a professional SQL Server (T-SQL) expert.
//    Database View: [dbo].[RptSalesDynamic]
//    IMPORTANT COLUMNS: StoreName, StoreFranchise, ItemName, Qty, TotalSales, TransDate
//    ====================================================
//    CRITICAL RULES:
//    1- ALWAYS return ONE SINGLE TEXT VALUE (ExecuteScalar).
//    2- NEVER return table.
//    3- NEVER use SELECT *.
//    4- NEVER use UNION.
//    5- MUST use string concatenation using + or CONCAT.
//    6- Convert numbers to NVARCHAR.
//    ====================================================
//    LANGUAGE RULES:
//    - You MUST understand Arabic and English.
//    - If user writes Arabic → understand it → generate SQL in English.
//    - DO NOT output Arabic words inside SQL except in LIKE filters.
//    ====================================================
//    BRANCH SEARCH RULE:
//    If user mentions branch: Use BOTH Arabic & English: (StoreName LIKE N'%زايد%' OR StoreName LIKE N'%Zayed%')
//    ====================================================
//    DATE RULES:
//    TODAY: TransDate = CAST(GETDATE() AS DATE)
//    YESTERDAY: TransDate = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)
//    TWO_DAYS_AGO: TransDate = CAST(DATEADD(DAY,-2,GETDATE()) AS DATE)
//    LAST_7_DAYS: TransDate >= CAST(DATEADD(DAY,-7,GETDATE()) AS DATE)
//    LAST_30_DAYS: TransDate >= CAST(DATEADD(DAY,-30,GETDATE()) AS DATE)
//    THIS_MONTH: ByMonth = MONTH(GETDATE()) AND ByYear = YEAR(GETDATE())
//    LAST_MONTH: ByMonth = MONTH(DATEADD(MONTH,-1,GETDATE())) AND ByYear = YEAR(DATEADD(MONTH,-1,GETDATE()))
//    THIS_YEAR: ByYear = YEAR(GETDATE())
//    LAST_YEAR: ByYear = YEAR(GETDATE()) - 1
//    ====================================================
//    INTENT RULES:
//    - Comparison → return highest + lowest
//    - Top → ORDER BY DESC
//    - Lowest → ORDER BY ASC
//    ====================================================
//    SQL SERVER VERSION RULE:
//    You are working on SQL Server 2012.
//    DO NOT USE: STRING_AGG, FORMAT, CONCAT_WS
//    USE INSTEAD: STUFF + FOR XML PATH for aggregation
//    OUTPUT RULE: Return ONLY SQL. No explanation. No markdown.
//    ====================================================
//    IMPORTANT: DO NOT include currency words like: 'ريال' or 'SAR' or 'EGP'. Return numbers ONLY.";

//        var contentsList = new List<object>
//        {
//            new { role = "user", parts = new[] { new { text = schemaPrompt } } },
//            new { role = "model", parts = new[] { new { text = "Understood. I will strictly generate T-SQL for ExecuteScalar based on your rules." } } }
//        };

//        if (conversationHistory != null)
//        {
//            foreach (var chat in conversationHistory)
//            {
//                if (!string.IsNullOrWhiteSpace(chat.Key) && !string.IsNullOrWhiteSpace(chat.Value))
//                {
//                    contentsList.Add(new { role = "user", parts = new[] { new { text = chat.Key.Trim() } } });
//                    contentsList.Add(new { role = "model", parts = new[] { new { text = chat.Value.Trim() } } });
//                }
//            }
//        }

//        contentsList.Add(new { role = "user", parts = new[] { new { text = currentUserQuery } } });

//        string[] models = new string[] { "gemini-2.5-flash", "gemini-1.5-flash", "gemini-1.5-pro" };
//        int maxRetryAttempts = 3;

//        foreach (var modelName in models)
//        {
//            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
//            {
//                string activeKey = GetCurrentApiKey();
//                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={activeKey}";

//                try
//                {
//                    using (var client = new HttpClient())
//                    {
//                        var requestBody = new { contents = contentsList.ToArray() };
//                        var json = JsonSerializer.Serialize(requestBody);
//                        var content = new StringContent(json, Encoding.UTF8, "application/json");

//                        var response = await client.PostAsync(url, content);

//                        if (response.IsSuccessStatusCode)
//                        {
//                            var jsonResponse = await response.Content.ReadAsStringAsync();

//                            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
//                            {
//                                var root = doc.RootElement;
//                                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
//                                {
//                                    var firstCandidate = candidates[0];
//                                    if (firstCandidate.TryGetProperty("content", out var resContent))
//                                    {
//                                        if (resContent.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
//                                        {
//                                            string rawSql = parts[0].GetProperty("text").GetString();

//                                            var cleaned = rawSql.Replace("```sql", "")
//                                                                .Replace("```SQL", "")
//                                                                .Replace("```", "")
//                                                                .Replace("\n", " ")
//                                                                .Replace("\r", " ")
//                                                                .Trim();

//                                            cleaned = cleaned.Replace("ريال", "")
//                                                             .Replace("SAR", "")
//                                                             .Replace("EGP", "");

//                                            return cleaned;
//                                        }
//                                    }
//                                }
//                            }
//                        }

//                        if ((int)response.StatusCode == 429)
//                        {
//                            SwitchToNextKey();
//                            await Task.Delay(1000);
//                            continue;
//                        }

//                        if (((int)response.StatusCode == 503) && attempt < maxRetryAttempts)
//                        {
//                            await Task.Delay(2000 * attempt);
//                            continue;
//                        }

//                        break;
//                    }
//                }
//                catch (Exception)
//                {
//                    SwitchToNextKey();
//                    if (attempt == maxRetryAttempts && modelName == models[models.Length - 1])
//                        throw;

//                    await Task.Delay(1000);
//                }
//            }
//        }

//        throw new Exception("جميع خوادم الذكاء الاصطناعي مشغولة حالياً، برجاء إعادة المحاولة بعد لحظات.");
//    }

//    private string ExecuteReadOnlyQuery(string sqlQuery)
//    {
//        using (SqlConnection con = new SqlConnection(connStr))
//        {
//            using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
//            {
//                con.Open();
//                var result = cmd.ExecuteScalar();
//                return result != null && result != DBNull.Value ? result.ToString() : "لا توجد بيانات مطابقة.";
//            }
//        }
//    }
//}

//// 6. كلاس مساعد لتهيئة الهيكل المخزن داخل الكاش
//public class CachedAiResult
//{
//    public string SqlGenerated { get; set; }
//    public string ResultValue { get; set; }
//}