using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HR_App.Controllers;
using HR_App.ViewModel;
using HR_App.Services;

public class AiAssistantController : BaseController
{
    private readonly string connStr =
        "Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;";

    private readonly string _ollamaEndpoint =
        "http://localhost:11434/api/chat";

    private readonly string _modelName = "llama3";

    private readonly AiTranslationService _translationService;

    public AiAssistantController(AiTranslationService translationService)
    {
        _translationService = translationService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> AskAi(string question)
    {
        var responseModel = new AiQueryVM
        {
            UserQuestion = question
        };

        if (string.IsNullOrWhiteSpace(question))
        {
            responseModel.ErrorMessage = "برجاء كتابة سؤال أولاً.";
            return Json(responseModel);
        }

        try
        {
            // 1- Normalize only (safe)
            question = NormalizeQuestion(question);

            // 2- ONLY translation (no rewriting logic)
            question = await _translationService
                .ExpandArabicQuery(question);

            // 3- History
            var history = new List<KeyValuePair<string, string>>();
            string sessionKey = "AiChatHistory";

            string existingHistory =
                HttpContext.Session.GetString(sessionKey);

            if (!string.IsNullOrWhiteSpace(existingHistory))
            {
                history =
                    JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(existingHistory);
            }

            // 4- Generate SQL
            string generatedSql =
                await GetSqlFromOllama(history, question);

            generatedSql = CleanMarkdownArtifacts(generatedSql);

            // 5- Safety fix layer (IMPORTANT)
            generatedSql = FixCommonSqlIssues(generatedSql);

            responseModel.SqlGenerated = generatedSql;

            // 6- Validate
            if (!IsSafeReadOnlyQuery(generatedSql))
            {
                responseModel.ErrorMessage = "تم رفض الاستعلام لأسباب أمنية.";
                return Json(responseModel);
            }

            // 7- Execute
            string dbResult = ExecuteReadOnlyQuery(generatedSql);
            responseModel.ResultValue = dbResult;

            // 8- Save history
            history.Add(new KeyValuePair<string, string>(question, generatedSql));

            if (history.Count > 5)
                history.RemoveAt(0);

            HttpContext.Session.SetString(sessionKey,
                JsonSerializer.Serialize(history));
        }
        catch (Exception ex)
        {
            responseModel.ErrorMessage =
                "حدث خطأ: " + ex.Message;
        }

        return Json(responseModel);
    }

    // =====================================================
    // FIX AI COMMON MISTAKES
    // =====================================================
    private string FixCommonSqlIssues(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        // FIX LIKE without %
        sql = Regex.Replace(sql,
            @"LIKE\s+N'([^%'][^']*[^%'])'",
            "LIKE N'%$1%'",
            RegexOptions.IgnoreCase);

        // FIX ORDER BY ASC in TOP 1 cases
        if (sql.Contains("TOP 1", StringComparison.OrdinalIgnoreCase))
        {
            sql = Regex.Replace(sql, @"ORDER BY (.*?) ASC",
                "ORDER BY $1 DESC",
                RegexOptions.IgnoreCase);
        }

        return sql;
    }

    // =====================================================
    // OLLAMA
    // =====================================================
    private async Task<string> GetSqlFromOllama(
        List<KeyValuePair<string, string>> history,
        string question)
    {
        string prompt = @"
You are a senior SQL Server expert.

SQL SERVER COMPATIBILITY: 2012 or lower.

You MUST generate ONLY valid SQL Server queries.

NO explanations.
NO markdown.
NO comments.
NO extra text.

====================================

MAIN VIEW:
[dbo].[RptSalesDynamic]

====================================

REAL DATA STRUCTURE:

This view contains sales data joined with:
- Storeuser (StoreName, StoreFranchise, Company, district)
- ItemsData (ItemName, SupplierId, DpName)
- TempItemWithSupplier (Supplier mapping)

====================================

FIELD MAPPING RULES:

Store / Branch:
- Use StoreName

Company / Franchise:
- Use StoreFranchise

Product:
- Use ItemName

Sales:
- Use TotalSalesWithoutTax

Quantity:
- Use Qty

Date:
- Use TransDate

====================================

STRICT RULES:

1- Allowed only:
SELECT, WITH, TOP, GROUP BY, ORDER BY, HAVING

2- FORBIDDEN:
INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, EXEC, MERGE, CREATE

3- SALES RULE:
Always use:
SUM(TotalSalesWithoutTax)

4- GROUPING RULE:
If using SUM → MUST use GROUP BY correctly

5- TOP 1 RULE:
If asking (highest / best / most):
Use TOP 1 with:
ORDER BY SUM(...) DESC

6- LIKE RULE (VERY IMPORTANT):
Always use:
LIKE N'%value%'

NEVER:
LIKE N'value'

7- STRING CONCAT RULE:
NEVER use STRING_AGG

Use ONLY:
STUFF + FOR XML PATH

Example:
SELECT STUFF((
    SELECT CHAR(13)+CHAR(10) + StoreName
    FROM [dbo].[RptSalesDynamic]
    FOR XML PATH(''), TYPE
).value('.', 'NVARCHAR(MAX)'),1,2,'')

8- DATE RULES:
Today:
CAST(GETDATE() AS DATE)

Yesterday:
CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)

Day before yesterday:
CAST(DATEADD(DAY,-2,GETDATE()) AS DATE)

9- DEFAULT RULE:
If unclear → assume SALES analysis

10- OUTPUT RULE:
Return ONLY ONE scalar value (single column, single row)

11- NEVER use any table except:
[dbo].[RptSalesDynamic]

====================================
";

        var messages = new List<object>
    {
        new
        {
            role = "system",
            content = prompt
        }
    };

        // history
        if (history != null)
        {
            foreach (var h in history)
            {
                messages.Add(new
                {
                    role = "user",
                    content = h.Key
                });

                messages.Add(new
                {
                    role = "assistant",
                    content = h.Value
                });
            }
        }

        // current question
        messages.Add(new
        {
            role = "user",
            content = question
        });

        using var client = new HttpClient();

        client.Timeout = TimeSpan.FromMinutes(2);

        var body = new
        {
            model = _modelName,
            messages,
            stream = false,
            options = new
            {
                temperature = 0,
                top_p = 0.1,
                num_predict = 300
            }
        };

        var response = await client.PostAsync(
            _ollamaEndpoint,
            new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            )
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ollama Error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var sql = doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return sql?.Trim() ?? "";
    }

    // =====================================================
    // CLEAN
    // =====================================================
    private string CleanMarkdownArtifacts(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "";

        sql = sql.Replace("```sql", "")
                 .Replace("```", "")
                 .Replace("\n", " ")
                 .Replace("\r", " ");

        int s = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        int w = sql.IndexOf("WITH", StringComparison.OrdinalIgnoreCase);

        if (s >= 0) sql = sql.Substring(s);
        else if (w >= 0) sql = sql.Substring(w);

        int semi = sql.IndexOf(";");
        if (semi > 0) sql = sql.Substring(0, semi);

        return sql.Trim();
    }

    // =====================================================
    // SECURITY
    // =====================================================
    private bool IsSafeReadOnlyQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        string u = sql.ToUpperInvariant();

        string[] blocked =
        {
            "INSERT","UPDATE","DELETE","DROP","ALTER",
            "TRUNCATE","EXEC","CREATE"
        };

        if (blocked.Any(b => u.Contains(b)))
            return false;

        if (!u.TrimStart().StartsWith("SELECT") &&
            !u.TrimStart().StartsWith("WITH"))
            return false;

        return true;
    }

    // =====================================================
    // EXECUTE
    // =====================================================
    private string ExecuteReadOnlyQuery(string sql)
    {
        using var con = new SqlConnection(connStr);
        using var cmd = new SqlCommand(sql, con);

        con.Open();

        var result = cmd.ExecuteScalar();

        return result?.ToString() ?? "لا توجد بيانات.";
    }

    // =====================================================
    // NORMALIZE
    // =====================================================
    private string NormalizeQuestion(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return "";

        q = Regex.Replace(q, @"\s+", " ");

        return q;
    }
}