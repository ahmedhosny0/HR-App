using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class RoleController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        #region Roles
        public IActionResult Roles()
        {
            List<RoleVM> list = new List<RoleVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        SELECT 
            RoleId,
            RoleName,
            AllowMultipleEmployees
        FROM HR_Roles";

                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RoleVM
                    {
                        RoleId = Convert.ToInt32(dr["RoleId"]),

                        RoleName = dr["RoleName"] == DBNull.Value
                            ? ""
                            : dr["RoleName"].ToString(),

                        AllowMultipleEmployees =
                            dr["AllowMultipleEmployees"] != DBNull.Value
                            && Convert.ToBoolean(dr["AllowMultipleEmployees"])
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult AddRole(RoleVM model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleName))
            {
                TempData["ErrorMessage"] = "اسم الدور مطلوب";
                return RedirectToAction("Roles");
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // CHECK DUPLICATE
                string checkSql = @"
        SELECT COUNT(*)
        FROM HR_Roles
        WHERE RoleName = @RoleName";

                SqlCommand checkCmd = new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue(
                    "@RoleName",
                    model.RoleName.Trim()
                );

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["ErrorMessage"] =
                        "اسم الدور موجود بالفعل";

                    return RedirectToAction("Roles");
                }

                // INSERT
                string insert = @"
        INSERT INTO HR_Roles
        (
            RoleName,
            AllowMultipleEmployees
        )
        VALUES
        (
            @RoleName,
            @AllowMultiple
        )";

                SqlCommand cmd = new SqlCommand(insert, con);

                cmd.Parameters.AddWithValue(
                    "@RoleName",
                    model.RoleName.Trim()
                );

                cmd.Parameters.AddWithValue(
                    "@AllowMultiple",
                    model.AllowMultipleEmployees
                );

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم إضافة الوظيفة بنجاح ✅";

            return RedirectToAction("Roles");
        }

        public IActionResult EditRole(int id)
        {
            RoleVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        SELECT
            RoleId,
            RoleName,
            AllowMultipleEmployees
        FROM HR_Roles
        WHERE RoleId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new RoleVM
                    {
                        RoleId = Convert.ToInt32(dr["RoleId"]),

                        RoleName = dr["RoleName"] == DBNull.Value
                            ? ""
                            : dr["RoleName"].ToString(),

                        AllowMultipleEmployees =
                            dr["AllowMultipleEmployees"] != DBNull.Value
                            && Convert.ToBoolean(dr["AllowMultipleEmployees"])
                    };
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult EditRole(RoleVM model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleName))
            {
                TempData["ErrorMessage"] = "اسم الدور مطلوب";
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // CHECK DUPLICATE
                string checkSql = @"
        SELECT COUNT(*)
        FROM HR_Roles
        WHERE RoleName = @RoleName
        AND RoleId <> @Id";

                SqlCommand checkCmd = new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue(
                    "@RoleName",
                    model.RoleName.Trim()
                );

                checkCmd.Parameters.AddWithValue(
                    "@Id",
                    model.RoleId
                );

                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists > 0)
                {
                    TempData["ErrorMessage"] =
                        "اسم الدور موجود بالفعل";

                    return View(model);
                }

                // UPDATE
                string q = @"
        UPDATE HR_Roles
        SET
            RoleName = @RoleName,
            AllowMultipleEmployees = @AllowMultiple
        WHERE RoleId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue(
                    "@RoleName",
                    model.RoleName.Trim()
                );

                cmd.Parameters.AddWithValue(
                    "@AllowMultiple",
                    model.AllowMultipleEmployees
                );

                cmd.Parameters.AddWithValue(
                    "@Id",
                    model.RoleId
                );

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم تعديل الوظيفة بنجاح ✅";

            return RedirectToAction("Roles");
        }
        public IActionResult DeleteRole(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_Roles WHERE RoleId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Roles");
        }
        #endregion Roles
    }
}
