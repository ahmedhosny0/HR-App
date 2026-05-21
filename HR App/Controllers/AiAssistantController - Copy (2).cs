//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Text;
//using System.Text.Json;
//using HR_App.ViewModel;
//using HR_App.Controllers;

//public class AiAssistantController : BaseController
//{
//    private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");

//    // ضع الـ API Key المجاني الذي حصلت عليه من Google AI Studio هنا
//    private readonly string _geminiApiKey = "AIzaSyC7KTvmsLsjYAiCCrhyXmly_sPVHc2wbhI";

//    // 1. الصفحة الرئيسية الخاصة بالـ AI
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
//            // 1. جلب سجل المحادثة الديناميكي الخاص بهذا المستخدم من الـ Session
//            var history = new List<KeyValuePair<string, string>>();
//            string sessionKey = "AiChatHistory";
//            string existingHistoryJson = HttpContext.Session.GetString(sessionKey);

//            if (!string.IsNullOrEmpty(existingHistoryJson))
//            {
//                // تحويل النص المخزن في السيسشن إلى القائمة الفيدرالية للمحادثات
//                history = JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(existingHistoryJson);
//            }

//            // 2. استدعاء الدالة وإرسال التاريخ الديناميكي مع السؤال الحالي القادم من الـ AJAX
//            string generatedSql = await GetSqlFromGemini(history, question);
//            responseModel.SqlGenerated = generatedSql;

//            // 3. تأمين إضافي: التأكد من أن الاستعلام هو للقراءة فقط
//            string upperSql = generatedSql.ToUpper();
//            if (upperSql.Contains("DROP") || upperSql.Contains("DELETE") || upperSql.Contains("UPDATE") || upperSql.Contains("TRUNCATE") || upperSql.Contains("ALTER"))
//            {
//                responseModel.ErrorMessage = "عذراً، الاستعلام الناتج يحتوي على عمليات غير مسموح بها!";
//                return Json(responseModel);
//            }

//            // 4. تنفيذ الـ SQL في الداتابيز وجلب النتيجة
//            string dbResult = ExecuteReadOnlyQuery(generatedSql);
//            responseModel.ResultValue = dbResult;

//            // 5. تحديث السجل: إذا نجحت العملية، نقوم بإضافة السؤال الحالي والـ SQL المتولد إلى التاريخ
//            history.Add(new KeyValuePair<string, string>(question, generatedSql));

//            // لتجنب تضخم حجم السيسشن والـ Tokens، يمكنك الاحتفاظ بآخر 5 أسئلة فقط كمثال (اختياري)
//            if (history.Count > 5)
//            {
//                history.RemoveAt(0); // حذف أقدم سؤال وإجابة
//            }

//            // 6. حفظ السجل المحدث ديناميكياً داخل الـ Session للمرة القادمة
//            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));
//        }
//        catch (Exception ex)
//        {
//            responseModel.ErrorMessage = "حدث خطأ أثناء معالجة طلبك: " + ex.Message;
//        }

//        return Json(responseModel);
//    }

//    // الدالة الجديدة للاتصال بـ Google Gemini API
//    private async Task<string> GetSqlFromGemini(List<KeyValuePair<string, string>> conversationHistory, string currentUserQuery)
//    {
//        string schemaPrompt = @"
//أنت خبير SQL Server (T-SQL) محترف ومخضرم.
//لديك في قاعدة البيانات فيو (View) شامل للمبيعات باسم: [dbo].[RptSalesDynamic]
    
//أعمدة هذا الفيو وجاهزة للاستخدام المباشر هي:
//- [TransactionNumber]: رقم الحركة / الفاتورة.
//- [StoreID]: كود الفرع.
//- [StoreName]: اسم الفرع (غالباً باللغة العربية، مثل: فرع زايد).
//- [StoreFranchise]: اسم الشركة أو الفرانشايز.
//- [ItemLookupCode]: باركود أو كود الصنف.
//- [ItemName]: اسم الصنف / المنتج.
//- [SupplierName]: اسم المورد.
//- [SupplierCode]: كود المورد.
//- [DpName]: اسم القسم الإداري أو الفئة (Department).
//- [Qty]: الكمية المباعة.
//- [Price]: سعر بيع الحبة.
//- [Cost]: تكلفة الحبة.
//- [TotalCostQty]: إجمالي التكلفة (Cost * Qty).
//- [TotalSales]: إجمالي المبيعات شاملاً الضريبة.
//- [TotalSalesTax]: إجمالي قيمة الضريبة.
//- [TotalSalesWithoutTax]: إجمالي المبيعات بدون ضريبة.
//- [TransDate]: تاريخ المبيعات (صيغة Date).
//- [ByDay]: اليوم (رقم).
//- [ByMonth]: الشهر (رقم).
//- [ByYear]: السنة (رقم).
//- [DManager]: مدير المنطقة (District Manager).
//- [FManager]: مدير العمليات / المشرف.
//- [district]: المنطقة الجغرافية للفرع.

//المطلوب: قم بتحويل سؤال المستخدم الأخير إلى استعلام T-SQL نظيف وصحيح يقرأ من الفيو [dbo].[RptSalesDynamic] بناءً على سياق المحادثة السابقة إذا كان مرتبطاً بها.

//قواعد تنظيمية برمجية صارمة جداً (أهم شروط):
//1. بما أن النظام ينفذ الاستعلام عبر ExecuteScalar، يجب أن يعيد الاستعلام دائماً 'قيمة نصية واحدة فقط' (Single Scalar Text Value) تحتوي على الإجابة الكاملة مدمجة بشكل مقروء ومنظم (String Concatenation) إذا كانت الإجابة تتكون من عدة خلايا أو صفوف (مثل: أعلى فرع وأقل فرع وتاريخ اليوم، أو قائمة أصناف).
//2. لا تستخدم UNION ALL لإنشاء صفوف متعددة، ولا تستخدم SELECT * نهائياً، ولا تعيد جداول. ادمج النصوص باستخدام دالة CONCAT أو رمز (+) وحوّل الأرقام والتواريخ إلى نصوص NVARCHAR ليخرج الاستعلام بصف واحد وعمود واحد فقط.
//   - مثال: لنص مدمج: SELECT N'أعلى فرع: ' + StoreName + N' بمبيعات ' + CAST(TotalSales AS NVARCHAR) ...
//3. اكتب كود الـ SQL فقط بدون أي مقدمات، شروحات، أو علامات تنسيق مثل ```sql أو 
//``` نهائياً. يبدأ النص بـ SELECT أو WITH وينتهي بشرطك.
//4. عندما يذكر المستخدم اسم فرع (عربي أو إنجليزي)، ابحث دائماً في الاسم العربي والإنجليزي معاً باستخدام شرط OR و LIKE لتغطية اللغتين تلقائياً.
//5. تذكر دائماً وضع حرف N قبل النصوص العربية في الاستعلام للتعامل مع الـ Unicode بشكل صحيح (مثل N'%زايد%').";

//        // 1. بناء الهيكل الرسمي للمحادثة التبادلية (Contents) لتخفيف حجم الـ Payload ومنع أخطاء السيرفر
//        var contentsList = new List<object>();

//        // وضع الـ Schema والتعليمات الصارمة كأول توجيه للموديل
//        contentsList.Add(new
//        {
//            role = "user",
//            parts = new[] { new { text = schemaPrompt } }
//        });

//        contentsList.Add(new
//        {
//            role = "model",
//            parts = new[] { new { text = "مفهوم تماماً. أنا جاهز لتحويل أسئلتك إلى استعلامات T-SQL مدمجة لـ ExecuteScalar بناءً على الشروط الصارمة مع مراعاة سياق الشات السابـق." } }
//        });

//        // إضافة السجل التاريخي الحقيقي للشات الممرر ديناميكياً (سؤال من المستخدم -> رد الـ SQL من الموديل)
//        if (conversationHistory != null)
//        {
//            foreach (var chat in conversationHistory)
//            {
//                contentsList.Add(new
//                {
//                    role = "user",
//                    parts = new[] { new { text = chat.Key } }
//                });
//                contentsList.Add(new
//                {
//                    role = "model",
//                    parts = new[] { new { text = chat.Value } }
//                });
//            }
//        }

//        // إضافة السؤال الحالي المطلوب حله في هذه اللحظة
//        contentsList.Add(new
//        {
//            role = "user",
//            parts = new[] { new { text = currentUserQuery } }
//        });

//        // مصفوفة بالموديلات البديلة لضمان التشغيل المستقر
//        string[] models = new string[] { "gemini-2.5-flash", "gemini-1.5-flash", "gemini-1.5-pro" };
//        int maxRetryAttempts = 3;

//        foreach (var modelName in models)
//        {
//            string url = $"https://generativelanguage.googleapis.com/v1/models/{modelName}:generateContent?key={_geminiApiKey}";

//            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
//            {
//                try
//                {
//                    using (var client = new HttpClient())
//                    {
//                        var requestBody = new
//                        {
//                            contents = contentsList.ToArray() // تمرير الهيكل التبادلي المنظم والمستقر
//                        };

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

//                                            // تنظيف شامل ودقيق لأي تنسيقات نصوص متبقية
//                                            return rawSql.Replace("```sql", "")
//                                                         .Replace("```SQL", "")
//                                                         .Replace("```", "")
//                                                         .Replace("\n", " ")
//                                                         .Replace("\r", " ")
//                                                         .Trim();
//                                        }
//                                    }
//                                }
//                            }
//                        }

//                        // معالجة ذكية للضغط المؤقت على السيرفر (Rate Limits)
//                        if (((int)response.StatusCode == 429 || (int)response.StatusCode == 503) && attempt < maxRetryAttempts)
//                        {
//                            await Task.Delay(2000 * attempt); // انتظار تصاعدي (2 ثانية ثم 4 ثواني) لتهدئة السيرفر
//                            continue;
//                        }

//                        break; // الخروج لتجربة الموديل التالي فوراً إذا كان الخطأ غير متعلق بالضغط
//                    }
//                }
//                catch (Exception)
//                {
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
//        string conString = connStr;

//        using (SqlConnection con = new SqlConnection(conString))
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