using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class HolidaysController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        #region Official Holidays

        public IActionResult OfficialHolidays()
        {
            List<OfficialHolidayVM> list = new List<OfficialHolidayVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
SELECT 
    HolidayId,
    HolidayName,
    HolidayDate,
    IsPaid,
    Notes,
    CreatedDate
FROM HR_OfficialHolidays
ORDER BY HolidayDate DESC";

                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new OfficialHolidayVM
                    {
                        HolidayId = Convert.ToInt32(dr["HolidayId"]),

                        HolidayName = dr["HolidayName"] == DBNull.Value
                            ? ""
                            : dr["HolidayName"].ToString(),

                        HolidayDate = dr["HolidayDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(dr["HolidayDate"]),

                        IsPaid =
                            dr["IsPaid"] != DBNull.Value
                            && Convert.ToBoolean(dr["IsPaid"]),

                        Notes = dr["Notes"] == DBNull.Value
                            ? ""
                            : dr["Notes"].ToString(),

                        CreatedDate = dr["CreatedDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(dr["CreatedDate"])
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult AddOfficialHoliday(OfficialHolidayVM model)
        {
            if (string.IsNullOrWhiteSpace(model.HolidayName))
            {
                TempData["ErrorMessage"] = "اسم الإجازة مطلوب";
                return RedirectToAction("OfficialHolidays");
            }

            if (model.HolidayDate == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "تاريخ الإجازة مطلوب";
                return RedirectToAction("OfficialHolidays");
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // CHECK DUPLICATE
                string checkSql = @"
SELECT COUNT(*)
FROM HR_OfficialHolidays
WHERE HolidayName = @HolidayName
AND HolidayDate = @HolidayDate";

                SqlCommand checkCmd = new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue(
                    "@HolidayName",
                    model.HolidayName.Trim()
                );

                checkCmd.Parameters.AddWithValue(
                    "@HolidayDate",
                    model.HolidayDate.Date
                );

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["ErrorMessage"] =
                        "الإجازة موجودة بالفعل";

                    return RedirectToAction("OfficialHolidays");
                }

                // INSERT
                string insert = @"
INSERT INTO HR_OfficialHolidays
(
    HolidayName,
    HolidayDate,
    IsPaid,
    Notes
)
VALUES
(
    @HolidayName,
    @HolidayDate,
    @IsPaid,
    @Notes
)";

                SqlCommand cmd = new SqlCommand(insert, con);

                cmd.Parameters.AddWithValue(
                    "@HolidayName",
                    model.HolidayName.Trim()
                );

                cmd.Parameters.AddWithValue(
                    "@HolidayDate",
                    model.HolidayDate.Date
                );

                cmd.Parameters.AddWithValue(
                    "@IsPaid",
                    model.IsPaid
                );

                cmd.Parameters.AddWithValue(
                    "@Notes",
                    string.IsNullOrWhiteSpace(model.Notes)
                        ? (object)DBNull.Value
                        : model.Notes.Trim()
                );

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم إضافة الإجازة الرسمية بنجاح ✅";

            return RedirectToAction("OfficialHolidays");
        }

        public IActionResult EditOfficialHoliday(int id)
        {
            OfficialHolidayVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
SELECT
    HolidayId,
    HolidayName,
    HolidayDate,
    IsPaid,
    Notes,
    CreatedDate
FROM HR_OfficialHolidays
WHERE HolidayId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new OfficialHolidayVM
                    {
                        HolidayId = Convert.ToInt32(dr["HolidayId"]),

                        HolidayName = dr["HolidayName"] == DBNull.Value
                            ? ""
                            : dr["HolidayName"].ToString(),

                        HolidayDate = dr["HolidayDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(dr["HolidayDate"]),

                        IsPaid =
                            dr["IsPaid"] != DBNull.Value
                            && Convert.ToBoolean(dr["IsPaid"]),

                        Notes = dr["Notes"] == DBNull.Value
                            ? ""
                            : dr["Notes"].ToString(),

                        CreatedDate = dr["CreatedDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(dr["CreatedDate"])
                    };
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult EditOfficialHoliday(OfficialHolidayVM model)
        {
            if (string.IsNullOrWhiteSpace(model.HolidayName))
            {
                TempData["ErrorMessage"] = "اسم الإجازة مطلوب";
                return View(model);
            }

            if (model.HolidayDate == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "تاريخ الإجازة مطلوب";
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // CHECK DUPLICATE
                string checkSql = @"
SELECT COUNT(*)
FROM HR_OfficialHolidays
WHERE HolidayName = @HolidayName
AND HolidayDate = @HolidayDate
AND HolidayId <> @Id";

                SqlCommand checkCmd = new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue(
                    "@HolidayName",
                    model.HolidayName.Trim()
                );

                checkCmd.Parameters.AddWithValue(
                    "@HolidayDate",
                    model.HolidayDate.Date
                );

                checkCmd.Parameters.AddWithValue(
                    "@Id",
                    model.HolidayId
                );

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["ErrorMessage"] =
                        "الإجازة موجودة بالفعل";

                    return View(model);
                }

                // UPDATE
                string q = @"
UPDATE HR_OfficialHolidays
SET
    HolidayName = @HolidayName,
    HolidayDate = @HolidayDate,
    IsPaid = @IsPaid,
    Notes = @Notes
WHERE HolidayId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue(
                    "@HolidayName",
                    model.HolidayName.Trim()
                );

                cmd.Parameters.AddWithValue(
                    "@HolidayDate",
                    model.HolidayDate.Date
                );

                cmd.Parameters.AddWithValue(
                    "@IsPaid",
                    model.IsPaid
                );

                cmd.Parameters.AddWithValue(
                    "@Notes",
                    string.IsNullOrWhiteSpace(model.Notes)
                        ? (object)DBNull.Value
                        : model.Notes.Trim()
                );

                cmd.Parameters.AddWithValue(
                    "@Id",
                    model.HolidayId
                );

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم تعديل الإجازة الرسمية بنجاح ✅";

            return RedirectToAction("OfficialHolidays");
        }

        public IActionResult DeleteOfficialHoliday(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
DELETE FROM HR_OfficialHolidays
WHERE HolidayId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم حذف الإجازة الرسمية بنجاح ✅";

            return RedirectToAction("OfficialHolidays");
        }

        #endregion Official Holidays
    }
}
