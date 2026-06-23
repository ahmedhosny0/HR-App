using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class EmployeeController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        private void LoadRoles()
        {
            var roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    roles.Add(new SelectListItem
                    {
                        Value = dr["RoleId"].ToString(),
                        Text = dr["RoleName"].ToString()
                    });
                }
            }

            ViewBag.Roles = roles;
        }
        #region Employee
        private void LoadWeeklyOffGroups()
        {
            List<SelectListItem> groups = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = @"
        SELECT
            WeeklyOffGroupId,
            GroupName
        FROM HR_WeeklyOffGroups
        WHERE IsActive = 1
        ORDER BY GroupName";

                SqlCommand cmd = new SqlCommand(q, con);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    groups.Add(new SelectListItem
                    {
                        Value = dr["WeeklyOffGroupId"].ToString(),
                        Text = dr["GroupName"].ToString()
                    });
                }
            }

            ViewBag.WeeklyOffGroups = groups;
        }
        public IActionResult CreateEmployee()
        {
            LoadWeeklyOffGroups();
            var roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    roles.Add(new SelectListItem
                    {
                        Value = dr["RoleId"].ToString(),
                        Text = dr["RoleName"].ToString()
                    });
                }
            }

            ViewBag.Roles = roles;

            return View(new EmployeeBulkVM());
        }
        [HttpPost]
        public IActionResult CreateEmployee(EmployeeBulkVM model)
        {
            if (model?.Employees == null || !model.Employees.Any())
            {
                TempData["ErrorMessage"] = "لا يوجد موظفين للحفظ";
                return RedirectToAction("CreateEmployee");
            }
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // للتحقق من التكرار داخل نفس الفورم
                var duplicateCodes = model.Employees
                    .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode))
                    .GroupBy(x => x.EmployeeCode.Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                var duplicateNames = model.Employees
                    .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeName))
                    .GroupBy(x => x.EmployeeName.Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateCodes.Any())
                {
                    TempData["ErrorMessage"] =
                        "يوجد أكواد مكررة داخل الشاشة: <br>" +
                        string.Join("<br>", duplicateCodes);

                    LoadRoles();
                    return View(model);
                }

                if (duplicateNames.Any())
                {
                    TempData["ErrorMessage"] =
                        "يوجد أسماء مكررة داخل الشاشة: <br>" +
                        string.Join("<br>", duplicateNames);

                    LoadRoles();
                    return View(model);
                }

                foreach (var emp in model.Employees)
                {
                    // تحقق من الكود
                    string checkCode = @"
            SELECT COUNT(*)
            FROM HR_Employees
            WHERE EmployeeCode = @Code";

                    SqlCommand cmdCode = new SqlCommand(checkCode, con);
                    cmdCode.Parameters.AddWithValue("@Code", emp.EmployeeCode ?? "");

                    int codeExists = Convert.ToInt32(cmdCode.ExecuteScalar());

                    if (codeExists > 0)
                    {
                        TempData["ErrorMessage"] =
                            $"كود الموظف موجود بالفعل: {emp.EmployeeCode}";

                        LoadRoles();
                        return View(model);
                    }

                    // تحقق من الاسم
                    string checkName = @"
            SELECT COUNT(*)
            FROM HR_Employees
            WHERE EmployeeName = @Name";

                    SqlCommand cmdName = new SqlCommand(checkName, con);
                    cmdName.Parameters.AddWithValue("@Name", emp.EmployeeName ?? "");

                    int nameExists = Convert.ToInt32(cmdName.ExecuteScalar());

                    if (nameExists > 0)
                    {
                        TempData["ErrorMessage"] =
                            $"اسم الموظف موجود بالفعل: {emp.EmployeeName}";

                        LoadRoles();
                        return View(model);
                    }
                    // التحقق هل الوظيفة تسمح بأكثر من موظف
                    if (emp.RoleId != null)
                    {
                        string roleCheck = @"
SELECT AllowMultipleEmployees
FROM HR_Roles
WHERE RoleId = @RoleId";

                        SqlCommand roleCmd = new SqlCommand(roleCheck, con);

                        roleCmd.Parameters.AddWithValue("@RoleId", emp.RoleId);

                        object allowObj = roleCmd.ExecuteScalar();

                        bool allowMultiple = allowObj != DBNull.Value &&
                                             Convert.ToBoolean(allowObj);

                        // لو الوظيفة لا تسمح بأكثر من موظف
                        if (!allowMultiple)
                        {
                            string employeeExistsQuery = @"
SELECT TOP 1 EmployeeName
FROM HR_Employees
WHERE RoleId = @RoleId";

                            SqlCommand empCmd = new SqlCommand(employeeExistsQuery, con);

                            empCmd.Parameters.AddWithValue("@RoleId", emp.RoleId);

                            object existingEmp = empCmd.ExecuteScalar();

                            if (existingEmp != null)
                            {
                                TempData["ErrorMessage"] =
                                    $"الوظيفة المحددة لا تسمح بأكثر من موظف.<br>" +
                                    $"هذه الوظيفة مشغولة حالياً بواسطة: {existingEmp}";

                                LoadRoles();

                                return View(model);
                            }
                        }
                    }
                    // INSERT
                    string insert = @"
           INSERT INTO HR_Employees
(
    EmployeeCode,
    EmployeeName,
    RoleId,
    JobTitle,
    HireDate,
    LeaveDate,
    IsActive,
    InsuranceStartDate,
    AnnualLeaveBalance,
    CasualLeaveBalance,
    SickLeaveUsedDays,
    LastLeaveBalanceUpdate,
WeeklyOffGroupId,
    CreatedDate
)
VALUES
(
    @Code,
    @Name,
    @RoleId,
    @Job,
    @HireDate,
    @LeaveDate,
    @IsActive,
    @InsuranceStart,
    @AnnualLeaveBalance,
    @CasualLeaveBalance,
    @SickLeaveUsedDays,
    @LastLeaveBalanceUpdate,
@WeeklyOffGroupId,
    GETDATE()
) ";

                    SqlCommand cmd = new SqlCommand(insert, con);

                    cmd.Parameters.AddWithValue(
     "@Code",
     string.IsNullOrWhiteSpace(emp.EmployeeCode)
         ? DBNull.Value
         : (object)emp.EmployeeCode
 );

                    cmd.Parameters.AddWithValue(
                        "@Name",
                        string.IsNullOrWhiteSpace(emp.EmployeeName)
                            ? DBNull.Value
                            : (object)emp.EmployeeName
                    );

                    cmd.Parameters.AddWithValue(
                        "@RoleId",
                        emp.RoleId == null
                            ? DBNull.Value
                            : (object)emp.RoleId
                    );
                    cmd.Parameters.AddWithValue(
                        "@WeeklyOffGroupId",
                        emp.WeeklyOffGroupId == null
                            ? DBNull.Value
                            : (object)emp.WeeklyOffGroupId
                    );

                    cmd.Parameters.AddWithValue(
                        "@Job",
                        string.IsNullOrWhiteSpace(emp.JobTitle)
                            ? DBNull.Value
                            : (object)emp.JobTitle
                    );

                    cmd.Parameters.AddWithValue(
                        "@HireDate",
                        emp.HireDate == null || emp.HireDate == DateTime.MinValue
                            ? DBNull.Value
                            : (object)emp.HireDate
                    );

                    cmd.Parameters.AddWithValue(
                        "@LeaveDate",
                        emp.LeaveDate == null || emp.LeaveDate == DateTime.MinValue
                            ? DBNull.Value
                            : (object)emp.LeaveDate
                    );

                    cmd.Parameters.AddWithValue(
                        "@InsuranceStart",
                        emp.InsuranceStartDate == null || emp.InsuranceStartDate == DateTime.MinValue
                            ? DBNull.Value
                            : (object)emp.InsuranceStartDate
                    );

                    cmd.Parameters.AddWithValue(
                        "@IsActive",
                        (object)(emp.IsActive ? 1 : 0)
                    );
                    cmd.Parameters.AddWithValue("@AnnualLeaveBalance",
    emp.AnnualLeaveBalance == 0 ? 0 : emp.AnnualLeaveBalance);

                    cmd.Parameters.AddWithValue("@CasualLeaveBalance",
                        emp.CasualLeaveBalance == 0 ? 7 : emp.CasualLeaveBalance);

                    cmd.Parameters.AddWithValue("@SickLeaveUsedDays",
                        emp.SickLeaveUsedDays == 0 ? 0 : emp.SickLeaveUsedDays);

                    cmd.Parameters.AddWithValue("@LastLeaveBalanceUpdate",
                        emp.LastLeaveBalanceUpdate == null
                            ? DBNull.Value
                            : (object)emp.LastLeaveBalanceUpdate);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "تم حفظ الموظفين بنجاح ✅";

            return RedirectToAction("CreateEmployee");
        }
        // =============================
        // عرض الموظفين
        // =============================
        public IActionResult Employees()
        {
            List<EmployeeVM> list = new List<EmployeeVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
SELECT 
Emp.EmployeeId,
Emp.EmployeeCode,
Emp.EmployeeName,
Emp.JobTitle,
Emp.HireDate,
Emp.InsuranceStartDate,
Emp.IsActive,
Emp.SecondaryLeaveBalance,
Emp.SecondaryLeaveUsedDays,
Emp.AnnualLeaveUsedDays,
Emp.CasualLeaveUsedDays,
Emp.SickLeaveBalance,
Emp.SickLeaveUsedDays,
Emp.ExamLeaveBalance,
Emp.ExamLeaveUsedDays,
Emp.LastLeaveBalanceUpdate,
Role.RoleName
FROM HR_Employees Emp
LEFT JOIN HR_Roles Role 
ON Emp.RoleId = Role.RoleId
 ";

                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new EmployeeVM
                    {
                        EmployeeId = dr["EmployeeId"] == DBNull.Value
    ? 0
    : Convert.ToInt32(dr["EmployeeId"]),
                        SecondaryLeaveBalance = dr["SecondaryLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["SecondaryLeaveBalance"]),
                        SecondaryLeaveUsedDays = dr["SecondaryLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["SecondaryLeaveUsedDays"]),
                        AnnualLeaveUsedDays = dr["AnnualLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["AnnualLeaveUsedDays"])
                        ,
                        CasualLeaveUsedDays = dr["CasualLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["CasualLeaveUsedDays"]),
                        SickLeaveBalance = dr["SickLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["SickLeaveBalance"]),
                        SickLeaveUsedDays = dr["SickLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["SickLeaveUsedDays"]),
                        ExamLeaveBalance = dr["ExamLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["ExamLeaveBalance"]),
                        ExamLeaveUsedDays = dr["ExamLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["ExamLeaveUsedDays"]),

                        EmployeeCode = dr["EmployeeCode"] == DBNull.Value
    ? ""
    : dr["EmployeeCode"].ToString(),

                        EmployeeName = dr["EmployeeName"] == DBNull.Value
    ? ""
    : dr["EmployeeName"].ToString(),

                        JobTitle = dr["JobTitle"] == DBNull.Value
    ? ""
    : dr["JobTitle"].ToString(),

                        LastLeaveBalanceUpdate = dr["LastLeaveBalanceUpdate"] == DBNull.Value
    ? (DateTime?)null
    : Convert.ToDateTime(dr["LastLeaveBalanceUpdate"]),
                        HireDate = dr["HireDate"] == DBNull.Value
    ? (DateTime?)null
    : Convert.ToDateTime(dr["HireDate"]),
                        InsuranceStartDate = dr["InsuranceStartDate"] == DBNull.Value
    ? (DateTime?)null
    : Convert.ToDateTime(dr["InsuranceStartDate"]),

                        IsActive = dr["IsActive"] != DBNull.Value
    && Convert.ToBoolean(dr["IsActive"]),

                        RoleName = dr["RoleName"] == DBNull.Value
    ? ""
    : dr["RoleName"].ToString()
                    });
                }
            }

            return View(list);
        }

        // =============================
        // صفحة التعديل
        // =============================
        public IActionResult EditEmployee(int id)
        {
            EmployeeVM model = new EmployeeVM();

            ViewBag.Roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // Roles
                string qRole = "SELECT RoleId, RoleName FROM HR_Roles";

                SqlCommand cmdRole = new SqlCommand(qRole, con);

                SqlDataReader drRole = cmdRole.ExecuteReader();

                while (drRole.Read())
                {
                    ((List<SelectListItem>)ViewBag.Roles).Add(new SelectListItem
                    {
                        Value = drRole["RoleId"].ToString(),
                        Text = drRole["RoleName"].ToString()
                    });
                }

                drRole.Close();

                // Employee
                string q = @"SELECT * FROM HR_Employees WHERE EmployeeId=@Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.EmployeeId = Convert.ToInt32(dr["EmployeeId"]);
                    model.EmployeeCode = dr["EmployeeCode"].ToString();
                    model.EmployeeName = dr["EmployeeName"].ToString();
                    model.JobTitle = dr["JobTitle"].ToString();

                    model.RoleId = dr["RoleId"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(dr["RoleId"]);
                    model.WeeklyOffGroupId = dr["WeeklyOffGroupId"] == DBNull.Value
                      ? null
                      : Convert.ToInt32(dr["WeeklyOffGroupId"]);

                    model.HireDate = dr["HireDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(dr["HireDate"]);

                    //model.LeaveDate = dr["LeaveDate"] == DBNull.Value
                    //    ? null
                    //    : Convert.ToDateTime(dr["LeaveDate"]);

                    model.InsuranceStartDate = dr["InsuranceStartDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["InsuranceStartDate"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    //                model.AnnualLeaveBalance = dr["AnnualLeaveBalance"] == DBNull.Value
                    //? 0
                    //: Convert.ToDecimal(dr["AnnualLeaveBalance"]);

                    //                model.CasualLeaveBalance = dr["CasualLeaveBalance"] == DBNull.Value
                    //                    ? 0
                    //                    : Convert.ToDecimal(dr["CasualLeaveBalance"]);

                    //                model.SickLeaveUsedDays = dr["SickLeaveBalance"] == DBNull.Value
                    //                    ? 0
                    //                    : Convert.ToInt32(dr["SickLeaveBalance"]);

                    //model.LastLeaveBalanceUpdate = dr["LastLeaveBalanceUpdate"] == DBNull.Value
                    //    ? null
                    //    : Convert.ToDateTime(dr["LastLeaveBalanceUpdate"]);
                }
            }
            LoadWeeklyOffGroups();
            return View(model);
        }

        // =============================
        // حفظ التعديل
        // =============================
        [HttpPost]
        public IActionResult EditEmployee(EmployeeVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // =========================
                // CHECK DUPLICATE CODE
                // =========================
                string checkCode = @"
        SELECT COUNT(*)
        FROM HR_Employees
        WHERE EmployeeCode = @Code
        AND EmployeeId <> @Id";

                SqlCommand cmdCheckCode = new SqlCommand(checkCode, con);

                cmdCheckCode.Parameters.AddWithValue(
                    "@Code",
                    string.IsNullOrWhiteSpace(model.EmployeeCode)
                        ? ""
                        : model.EmployeeCode.Trim()
                );

                cmdCheckCode.Parameters.AddWithValue("@Id", model.EmployeeId);

                int codeExists = Convert.ToInt32(cmdCheckCode.ExecuteScalar());

                if (codeExists > 0)
                {
                    TempData["ErrorMessage"] =
                        $"كود الموظف مستخدم بالفعل: {model.EmployeeCode}";

                    LoadRoles();

                    return View(model);
                }

                // =========================
                // CHECK DUPLICATE NAME
                // =========================
                string checkName = @"
        SELECT COUNT(*)
        FROM HR_Employees
        WHERE EmployeeName = @Name
        AND EmployeeId <> @Id";

                SqlCommand cmdCheckName = new SqlCommand(checkName, con);

                cmdCheckName.Parameters.AddWithValue(
                    "@Name",
                    string.IsNullOrWhiteSpace(model.EmployeeName)
                        ? ""
                        : model.EmployeeName.Trim()
                );

                cmdCheckName.Parameters.AddWithValue("@Id", model.EmployeeId);

                int nameExists = Convert.ToInt32(cmdCheckName.ExecuteScalar());

                if (nameExists > 0)
                {
                    TempData["ErrorMessage"] =
                        $"اسم الموظف مستخدم بالفعل: {model.EmployeeName}";

                    LoadRoles();

                    return View(model);
                }

                // =========================
                // UPDATE
                // =========================
                string q = @"
UPDATE HR_Employees
SET
    EmployeeCode = @Code,
    EmployeeName = @Name,
    RoleId = @RoleId,
    JobTitle = @Job,
    HireDate = @HireDate,
    --LeaveDate = @LeaveDate,
    InsuranceStartDate = @Insurance,
    IsActive = @IsActive,
    WeeklyOffGroupId = @WeeklyOffGroupId--,

    --AnnualLeaveBalance = @AnnualLeaveBalance,
    --CasualLeaveBalance = @CasualLeaveBalance,
   -- LastLeaveBalanceUpdate = @LastLeaveBalanceUpdate,
   -- SickLeaveUsedDays = @SickLeaveUsedDays

WHERE EmployeeId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue(
                    "@Code",
                    string.IsNullOrWhiteSpace(model.EmployeeCode)
                        ? DBNull.Value
                        : (object)model.EmployeeCode
                );

                cmd.Parameters.AddWithValue(
                    "@Name",
                    string.IsNullOrWhiteSpace(model.EmployeeName)
                        ? DBNull.Value
                        : (object)model.EmployeeName
                );

                cmd.Parameters.AddWithValue(
                    "@RoleId",
                    model.RoleId == null
                        ? DBNull.Value
                        : (object)model.RoleId
                );
                cmd.Parameters.AddWithValue(
                   "@WeeklyOffGroupId",
                   model.WeeklyOffGroupId == null
                       ? DBNull.Value
                       : (object)model.WeeklyOffGroupId
               );
                cmd.Parameters.AddWithValue(
                    "@Job",
                    string.IsNullOrWhiteSpace(model.JobTitle)
                        ? DBNull.Value
                        : (object)model.JobTitle
                );

                //cmd.Parameters.AddWithValue(
                //    "@LastLeaveBalanceUpdate",
                //    model.LastLeaveBalanceUpdate == null
                //        ? DBNull.Value
                //        : (object)model.LastLeaveBalanceUpdate
                //);
                cmd.Parameters.AddWithValue(
                  "@HireDate",
                  model.HireDate == null
                      ? DBNull.Value
                      : (object)model.HireDate
              );

                //cmd.Parameters.AddWithValue(
                //    "@LeaveDate",
                //    model.LeaveDate == null
                //        ? DBNull.Value
                //        : (object)model.LeaveDate
                //);

                cmd.Parameters.AddWithValue(
                    "@Insurance",
                    model.InsuranceStartDate == null
                        ? DBNull.Value
                        : (object)model.InsuranceStartDate
                );

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                //// =========================
                //// LEAVE BALANCE
                //// =========================
                //cmd.Parameters.AddWithValue(
                //    "@AnnualLeaveBalance",
                //    model.AnnualLeaveBalance
                //);

                //cmd.Parameters.AddWithValue(
                //    "@CasualLeaveBalance",
                //    model.CasualLeaveBalance
                //);

                //cmd.Parameters.AddWithValue(
                //    "@SickLeaveUsedDays",
                //    model.SickLeaveUsedDays
                //);

                cmd.Parameters.AddWithValue("@Id", model.EmployeeId);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم تعديل الموظف بنجاح ✅";

            return RedirectToAction("Employees");
        }

        // =============================
        // حذف موظف
        // =============================
        public IActionResult DeleteEmployee(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_Employees WHERE EmployeeId=@Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم حذف الموظف بنجاح 🗑️";

            return RedirectToAction("Employees");
        }
        #endregion Employee
    }
}
