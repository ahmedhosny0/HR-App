using System.Text;
using System.Text.Json;

namespace HR_App.Services
{
    public class AiSqlService
    {
        private readonly HttpClient _http;
        private readonly string _endpoint = "http://localhost:11434/api/chat";
        private readonly string _model = "llama3";

        public AiSqlService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GenerateSql(string question)
        {
            string prompt = @"
You are a SQL Server 2012 expert.

Generate ONLY valid SQL Server query.

NO explanation.
NO markdown.
NO comments.
NO extra text.

====================================================

DATABASE:
[dbo].[RptSalesDynamic]

====================================================

REAL COLUMNS:

StoreName
StoreFranchise
ItemName
ItemLookupCode
Qty
Price
TotalSales
TotalSalesWithoutTax
TransDate
ByDay
ByMonth
ByYear
SupplierName
DpName
district

====================================================

COLUMN RULES:

Branch / Store
= StoreName

Company / Franchise
= StoreFranchise

Product
= ItemName

Sales
= SUM(TotalSalesWithoutTax)

Quantity
= SUM(Qty)

Date
= TransDate

====================================================

STRICT RULES:

1- ONLY:
SELECT
WITH

2- NEVER USE:
INSERT
UPDATE
DELETE
DROP
ALTER
TRUNCATE
EXEC
MERGE
CREATE

3- ALWAYS use:
SUM(TotalSalesWithoutTax)

for sales questions.

4- NEVER invent columns.

DO NOT USE:
SalesAmount
franchise
branchName
productName

5- TEXT SEARCH:
Always use:
LIKE N'%value%'

Example:
StoreName LIKE N'%Almaza%'

6- DATE RULES:

DATE INTELLIGENCE RULE:

You must detect intent from user sentence:

WORDS MEANING:

- امبارح / yesterday → TransDate = DATEADD(DAY,-1)
- اول امبارح → DATEADD(DAY,-2)
- النهارده / today → GETDATE()

NEVER use:
ByMonth for relative dates
ByYear for relative dates

ONLY use:
ByMonth = MONTH(GETDATE())
WHEN user explicitly says (month / شهر / الشهر ده)

ONLY use:
ByYear = YEAR(GETDATE())
WHEN user explicitly says (year / السنة)

IMPORTANT:
If user does NOT mention month/year → ALWAYS use TransDate

7- TOP RULE:
TOP 1 must use:
ORDER BY SUM(...) DESC

8- MULTI ROW OUTPUT:
Use:
STUFF + FOR XML PATH

9- SQL SERVER:
Compatible with SQL Server 2012 only.

10- OUTPUT:
Return ONLY SQL query.

11- COMPARISON RULE:

COMPARISON INTELLIGENCE:

If query contains:
- and / و
- vs
- compare / مقارنة

THEN:
- MUST use GROUP BY StoreName
- MUST return multiple rows
- MUST wrap OR conditions in parentheses
- MUST NOT use scalar result

Correct Example:

SELECT
    StoreName,
    SUM(TotalSalesWithoutTax) AS TotalSales
FROM [dbo].[RptSalesDynamic]
WHERE
(
    StoreName LIKE N'%Zayed%'
    OR StoreName LIKE N'%Almaza%'
)
AND TransDate = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)
GROUP BY StoreName
====================================================

VALID EXAMPLES:

SELECT SUM(TotalSalesWithoutTax)
FROM [dbo].[RptSalesDynamic]
WHERE StoreName LIKE N'%Almaza%'
AND TransDate = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE)

--------------------------------

SELECT TOP 1
StoreName,
SUM(TotalSalesWithoutTax) AS Sales
FROM [dbo].[RptSalesDynamic]
GROUP BY StoreName
ORDER BY SUM(TotalSalesWithoutTax) DESC

====================================================
";

            var body = new
            {
                model = _model,
                messages = new[]
                {
            new
            {
                role = "system",
                content = prompt
            },
            new
            {
                role = "user",
                content = question
            }
        },
                stream = false,
                options = new
                {
                    temperature = 0,
                    top_p = 0.1,
                    num_predict = 250
                }
            };

            var response = await _http.PostAsync(
                _endpoint,
                new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            );

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var sql = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return sql?.Trim() ?? "";
        }
    }
}