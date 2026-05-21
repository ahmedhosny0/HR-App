//using HR_App.Controllers;
//using HR_App.Services;
//using HR_App.ViewModel;
//using Microsoft.AspNetCore.Mvc;
//using System.Text.RegularExpressions;

//public class AiAssistantController : BaseController
//{
//    private readonly AiSqlService _sql;
//    private readonly AiTranslationService _translate;
//    private readonly EgyptianNlpService _nlp;
//    private readonly SqlValidator _validator;
//    private readonly SqlExecutionService _db;

//    public AiAssistantController(
//        AiSqlService sql,
//        AiTranslationService translate,
//        EgyptianNlpService nlp,
//        SqlValidator validator,
//        SqlExecutionService db)
//    {
//        _sql = sql;
//        _translate = translate;
//        _nlp = nlp;
//        _validator = validator;
//        _db = db;
//    }

//    public IActionResult Index() => View();

//    [HttpPost]
//    public async Task<JsonResult> AskAi(string question)
//    {
//        var model = new AiQueryVM
//        {
//            UserQuestion = question
//        };

//        try
//        {
//            // 1️⃣ Normalize (Arabic cleanup)
//            question = _nlp.Normalize(question);

//            // 2️⃣ Translate to English (for AI stability)
//            question = await _translate.Translate(question);

//            // 3️⃣ Generate SQL
//            var sql = await _sql.GenerateSql(question);

//            // 4️⃣ Clean + Fix SQL
//            sql = _validator.FixSql(sql);

//            // 5️⃣ 🚫 Block Arabic inside SQL (VERY IMPORTANT)
//            if (Regex.IsMatch(sql, @"[\u0600-\u06FF]"))
//            {
//                model.ErrorMessage = "Invalid SQL (contains Arabic text)";
//                return Json(model);
//            }

//            // 6️⃣ Validate safety
//            if (!_validator.IsSafe(sql))
//            {
//                model.ErrorMessage = "Unsafe query detected";
//                return Json(model);
//            }

//            model.SqlGenerated = sql;

//            // 7️⃣ Detect if query returns multiple rows
//            bool isGrouped =
//                sql.ToUpper().Contains("GROUP BY") ||
//                sql.ToUpper().Contains("ORDER BY");

//            object result;

//            if (isGrouped)
//            {
//                result = _db.ExecuteQuery(sql);   // ✅ Table
//                model.IsTable = true;
//            }
//            else
//            {
//                result = _db.ExecuteScalar(sql);  // ✅ Single value
//                model.IsTable = false;
//            }

//            model.ResultValue = result;

//            return Json(model);
//        }
//        catch (Exception ex)
//        {
//            model.ErrorMessage = ex.Message;
//            return Json(model);
//        }
//    }
//}