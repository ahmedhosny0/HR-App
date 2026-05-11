using HR_App.Services;
using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Security.Claims;

namespace HR_App.Controllers
{
    public class LeaveController : BaseController
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

        #region RequestTypes
        public IActionResult RequestTypes()
        {
            List<RequestTypeVM> list = new List<RequestTypeVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RequestTypeId, Name FROM HR_RequestTypes";
                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RequestTypeVM
                    {
                        RequestTypeId = Convert.ToInt32(dr["RequestTypeId"]),
                        Name = dr["Name"].ToString()
                    });
                }
            }

            return View(list);
        }
        [HttpPost]
        public IActionResult AddRequestType(RequestTypeVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("اسم النوع مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string insert = @"
        INSERT INTO HR_RequestTypes (Name)
        VALUES (@Name)";

                SqlCommand cmd = new SqlCommand(insert, con);
                cmd.Parameters.AddWithValue("@Name", model.Name);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("RequestTypes");
        }
        public IActionResult EditRequestType(int id)
        {
            RequestTypeVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RequestTypeId, Name FROM HR_RequestTypes WHERE RequestTypeId = @Id";
                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new RequestTypeVM
                    {
                        RequestTypeId = Convert.ToInt32(dr["RequestTypeId"]),
                        Name = dr["Name"].ToString()
                    };
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult EditRequestType(RequestTypeVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("اسم النوع مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        UPDATE HR_RequestTypes
        SET Name = @Name
        WHERE RequestTypeId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Id", model.RequestTypeId);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم التعديل بنجاح";
            return RedirectToAction("RequestTypes");
        }
        public IActionResult DeleteRequestType(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_RequestTypes WHERE RequestTypeId = @Id";
                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "تم الحذف بنجاح";
            return RedirectToAction("RequestTypes");
        }
        #endregion

        public IActionResult WorkflowSteps()
        {
            ViewBag.RequestTypes = new List<SelectListItem>();
            ViewBag.Roles = new List<SelectListItem>();
            ViewBag.Steps = new List<WorkflowStepVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // RequestTypes
                var reqList = new List<SelectListItem>();
                SqlCommand cmd1 = new SqlCommand("SELECT RequestTypeId, Name FROM HR_RequestTypes", con);
                SqlDataReader dr1 = cmd1.ExecuteReader();

                while (dr1.Read())
                {
                    reqList.Add(new SelectListItem
                    {
                        Value = dr1["RequestTypeId"].ToString(),
                        Text = dr1["Name"].ToString()
                    });
                }
                dr1.Close();

                ViewBag.RequestTypes = reqList;

                // Roles
                var roleList = new List<SelectListItem>();
                SqlCommand cmd2 = new SqlCommand("SELECT RoleId, RoleName FROM HR_Roles", con);
                SqlDataReader dr2 = cmd2.ExecuteReader();

                while (dr2.Read())
                {
                    roleList.Add(new SelectListItem
                    {
                        Value = dr2["RoleId"].ToString(),
                        Text = dr2["RoleName"].ToString()
                    });
                }
                dr2.Close();

                ViewBag.Roles = roleList;

                // Steps
                var steps = new List<WorkflowStepVM>();
                SqlCommand cmd3 = new SqlCommand(@"
            SELECT StepId, StepOrder, ApproverType
            FROM HR_WorkflowSteps", con);

                SqlDataReader dr3 = cmd3.ExecuteReader();

                while (dr3.Read())
                {
                    steps.Add(new WorkflowStepVM
                    {
                        StepId = Convert.ToInt32(dr3["StepId"]),
                        StepOrder = Convert.ToInt32(dr3["StepOrder"]),
                        ApproverType = dr3["ApproverType"].ToString()
                    });
                }

                ViewBag.Steps = steps;
            }

            return View();
        }
        private int GetApproverId(string type, int employeeId, int roleId)
        {
            using var con = new SqlConnection(connStr);
            con.Open();

            if (type == "MANAGER")
            {
                string q = @"
        SELECT ManagerId
        FROM HR_EmployeeHierarchy
        WHERE EmployeeId = @emp AND IsActive = 1";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@emp", employeeId);

                return (int)cmd.ExecuteScalar();
            }

            if (type == "ROLE")
            {
                string q = @"
        SELECT TOP 1 EmployeeId
        FROM HR_Employees
        WHERE RoleId = @role AND IsActive = 1";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@role", roleId);

                return (int)cmd.ExecuteScalar();
            }

            return 0;
        }
        [HttpPost]
        public IActionResult AddWorkflowStep(WorkflowStepVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string insert = @"
        INSERT INTO HR_WorkflowSteps
        (RequestTypeId, StepOrder, ApproverType, RoleId)
        VALUES
        (@RequestTypeId, @StepOrder, @ApproverType, @RoleId)";

                SqlCommand cmd = new SqlCommand(insert, con);

                cmd.Parameters.AddWithValue("@RequestTypeId", model.RequestTypeId);
                cmd.Parameters.AddWithValue("@StepOrder", model.StepOrder);
                cmd.Parameters.AddWithValue("@ApproverType", model.ApproverType ?? "");
                cmd.Parameters.AddWithValue("@RoleId",
                    model.RoleId ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("WorkflowSteps");
        }
        #region Employee
        public IActionResult CreateEmployee()
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
Emp.AnnualLeaveBalance,
Emp.CasualLeaveBalance,
Emp.SickLeaveUsedDays,
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
                        AnnualLeaveBalance = dr["AnnualLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToInt32(dr["AnnualLeaveBalance"]),
                        CasualLeaveBalance = dr["CasualLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToInt32(dr["CasualLeaveBalance"]),
                        SickLeaveUsedDays = dr["SickLeaveUsedDays"] == DBNull.Value
    ? 0
    : Convert.ToInt32(dr["SickLeaveUsedDays"]),

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

                    model.HireDate = dr["HireDate"] == DBNull.Value
    ? null
    : Convert.ToDateTime(dr["HireDate"]); 

                    model.LeaveDate = dr["LeaveDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["LeaveDate"]);

                    model.InsuranceStartDate = dr["InsuranceStartDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["InsuranceStartDate"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.AnnualLeaveBalance = dr["AnnualLeaveBalance"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(dr["AnnualLeaveBalance"]);

                    model.CasualLeaveBalance = dr["CasualLeaveBalance"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["CasualLeaveBalance"]);

                    model.SickLeaveUsedDays = dr["SickLeaveUsedDays"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(dr["SickLeaveUsedDays"]);

                    model.LastLeaveBalanceUpdate = dr["LastLeaveBalanceUpdate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["LastLeaveBalanceUpdate"]);
                }
            }

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
    LeaveDate = @LeaveDate,
    InsuranceStartDate = @Insurance,
    IsActive = @IsActive,

    AnnualLeaveBalance = @AnnualLeaveBalance,
    CasualLeaveBalance = @CasualLeaveBalance,
    LastLeaveBalanceUpdate = @LastLeaveBalanceUpdate,
    SickLeaveUsedDays = @SickLeaveUsedDays

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
                    "@Job",
                    string.IsNullOrWhiteSpace(model.JobTitle)
                        ? DBNull.Value
                        : (object)model.JobTitle
                );
                
                cmd.Parameters.AddWithValue(
                    "@LastLeaveBalanceUpdate",
                    model.LastLeaveBalanceUpdate == null
                        ? DBNull.Value
                        : (object)model.LastLeaveBalanceUpdate
                );
                cmd.Parameters.AddWithValue(
                  "@HireDate",
                  model.HireDate == null
                      ? DBNull.Value
                      : (object)model.HireDate
              );

                cmd.Parameters.AddWithValue(
                    "@LeaveDate",
                    model.LeaveDate == null
                        ? DBNull.Value
                        : (object)model.LeaveDate
                );

                cmd.Parameters.AddWithValue(
                    "@Insurance",
                    model.InsuranceStartDate == null
                        ? DBNull.Value
                        : (object)model.InsuranceStartDate
                );

                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                // =========================
                // LEAVE BALANCE
                // =========================
                cmd.Parameters.AddWithValue(
                    "@AnnualLeaveBalance",
                    model.AnnualLeaveBalance
                );

                cmd.Parameters.AddWithValue(
                    "@CasualLeaveBalance",
                    model.CasualLeaveBalance
                );

                cmd.Parameters.AddWithValue(
                    "@SickLeaveUsedDays",
                    model.SickLeaveUsedDays
                );

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
        #region EmployeeHierarchy
        public IActionResult EmployeeHierarchy()
        {
            ViewBag.Roles = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ((List<SelectListItem>)ViewBag.Roles).Add(new SelectListItem
                    {
                        Value = dr["RoleId"].ToString(),
                        Text = dr["RoleName"].ToString()
                    });
                }
            }

            ViewBag.List = GetHierarchy(); // لازم ترجع Id كمان

            return View();
        }
        [HttpPost]
        public IActionResult EmployeeHierarchy(int ChildRoleId, int ParentRoleId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        INSERT INTO HR_EmployeeHierarchy
        (ChildRoleId, ParentRoleId, IsActive)
        VALUES (@Child, @Parent, 1)";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Child", ChildRoleId);
                cmd.Parameters.AddWithValue("@Parent", ParentRoleId);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
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
        public IActionResult EditHierarchy(int id)
        {
            EmployeeHierarchyVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"SELECT Id, ChildRoleId, ParentRoleId 
                     FROM HR_EmployeeHierarchy
                     WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new EmployeeHierarchyVM
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        ChildRoleId = Convert.ToInt32(dr["ChildRoleId"]),
                        ParentRoleId = Convert.ToInt32(dr["ParentRoleId"])
                    };
                }
            }

            LoadRoles();
            return View(model);
        }
        [HttpPost]
        public IActionResult EditHierarchy(EmployeeHierarchyVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"UPDATE HR_EmployeeHierarchy
                     SET ChildRoleId = @Child,
                         ParentRoleId = @Parent
                     WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Child", model.ChildRoleId);
                cmd.Parameters.AddWithValue("@Parent", model.ParentRoleId);
                cmd.Parameters.AddWithValue("@Id", model.Id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
        public IActionResult DeleteHierarchy(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "DELETE FROM HR_EmployeeHierarchy WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("EmployeeHierarchy");
        }
        private List<dynamic> GetHierarchy()
        {
            var list = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
        SELECT 
            h.Id,
            c.RoleName AS ChildRole,
            p.RoleName AS ParentRole
        FROM HR_EmployeeHierarchy h
        JOIN HR_Roles c ON h.ChildRoleId = c.RoleId
        JOIN HR_Roles p ON h.ParentRoleId = p.RoleId";

                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Child = dr["ChildRole"].ToString(),
                        Parent = dr["ParentRole"].ToString()
                    });
                }
            }

            return list;
        }        // =========================
        #endregion
        private EmployeeDatesResult GetEmployeeDates(int empId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql = @"
            SELECT HireDate, InsuranceStartDate
            FROM HR_Employees
            WHERE EmployeeId = @EmpId";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@EmpId", empId);

                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        var hire = dr["HireDate"];
                        var insurance = dr["InsuranceStartDate"];

                        // ❌ لو HireDate فاضي = الموظف جديد
                        if (hire == DBNull.Value)
                        {
                            return new EmployeeDatesResult
                            {
                                IsValid = false,
                                Message = "لا يوجد تاريخ تعيين لهذا الموظف (موظف جديد أو بيانات ناقصة)"
                            };
                        }

                        return new EmployeeDatesResult
                        {
                            IsValid = true,
                            HireDate = Convert.ToDateTime(hire),
                            InsuranceDate = insurance == DBNull.Value
                                ? null
                                : Convert.ToDateTime(insurance)
                        };
                    }
                }
            }

            return new EmployeeDatesResult
            {
                IsValid = false,
                Message = "الموظف غير موجود"
            };
        }
        private int GetUsedLeaveDays(int empId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql = @"
            SELECT COUNT(*)
            FROM HR_Requests
            WHERE EmployeeId = @EmpId
            AND RequestTypeId = 1
            AND Status = 2"; // approved

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@EmpId", empId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public JsonResult GetEmployeeLeaveBalance(int employeeId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = @"
SELECT 
    EmployeeName,
    EmployeeCode,

    AnnualLeaveBalance,
    ISNULL(AnnualLeaveUsedDays,0) AnnualLeaveUsedDays,

    CasualLeaveBalance,
    ISNULL(CasualLeaveUsedDays,0) CasualLeaveUsedDays,

    SickLeaveBalance,
    ISNULL(SickLeaveUsedDays,0) SickLeaveUsedDays,

    ExamLeaveBalance,
    ISNULL(ExamLeaveUsedDays,0) ExamLeaveUsedDays

FROM HR_Employees
WHERE EmployeeId = @EmployeeId";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    return Json(new
                    {
                        success = true,

                        employeeName = dr["EmployeeName"].ToString(),
                        employeeCode = dr["EmployeeCode"].ToString(),

                        annualBalance = Convert.ToInt32(dr["AnnualLeaveBalance"]),
                        annualUsed = Convert.ToInt32(dr["AnnualLeaveUsedDays"]),

                        casualBalance = Convert.ToInt32(dr["CasualLeaveBalance"]),
                        casualUsed = Convert.ToInt32(dr["CasualLeaveUsedDays"]),

                        sickBalance = Convert.ToInt32(dr["SickLeaveBalance"]),
                        sickUsed = Convert.ToInt32(dr["SickLeaveUsedDays"]),

                        examBalance = Convert.ToInt32(dr["ExamLeaveBalance"]),
                        examUsed = Convert.ToInt32(dr["ExamLeaveUsedDays"])
                    });
                }
            }

            return Json(new { success = false });
        }
        private (bool IsValid, string Message) CheckLeaveBalance(
    int employeeId,
    int requestTypeId,
    int requestedDays)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
SELECT

    AnnualLeaveBalance,
    ISNULL(AnnualLeaveUsedDays,0) AnnualLeaveUsedDays,

    CasualLeaveBalance,
    ISNULL(CasualLeaveUsedDays,0) CasualLeaveUsedDays,

    SickLeaveBalance,
    ISNULL(SickLeaveUsedDays,0) SickLeaveUsedDays,

    ExamLeaveBalance,
    ISNULL(ExamLeaveUsedDays,0) ExamLeaveUsedDays

FROM HR_Employees
WHERE EmployeeId = @EmployeeId";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    int balance = 0;
                    int used = 0;
                    string leaveName = "";

                    switch (requestTypeId)
                    {
                        case 1:
                            balance = Convert.ToInt32(dr["AnnualLeaveBalance"]);
                            used = Convert.ToInt32(dr["AnnualLeaveUsedDays"]);
                            leaveName = "الاعتيادي";
                            break;

                        case 2:
                            balance = Convert.ToInt32(dr["CasualLeaveBalance"]);
                            used = Convert.ToInt32(dr["CasualLeaveUsedDays"]);
                            leaveName = "العارضة";
                            break;

                        case 3:
                            balance = Convert.ToInt32(dr["SickLeaveBalance"]);
                            used = Convert.ToInt32(dr["SickLeaveUsedDays"]);
                            leaveName = "المرضي";
                            break;

                        case 4:
                            balance = Convert.ToInt32(dr["ExamLeaveBalance"]);
                            used = Convert.ToInt32(dr["ExamLeaveUsedDays"]);
                            leaveName = "الامتحانات";
                            break;
                    }

                    int remaining = balance - used;

                    if (requestedDays > remaining)
                    {
                        return
                        (
                            false,
                            $"رصيد إجازة {leaveName} غير كافٍ<br>" +
                            $"الرصيد المتبقي: {remaining} يوم"
                        );
                    }

                    return (true, "");
                }
            }

            return (false, "الموظف غير موجود");
        }
        public IActionResult CreateRequest()
        {
            var vm = new LeaveRequestVM();

            var employees = new List<SelectListItem>();
            var types = new List<SelectListItem>();

            dynamic emp = null;

            var userId = ViewBag.UserName;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // =========================
                // Current Employee
                // =========================
                string qEmp = @"
        SELECT EmployeeId, EmployeeName, EmployeeCode,
               AnnualLeaveBalance, AnnualLeaveUsedDays,
               CasualLeaveBalance, CasualLeaveUsedDays,
               SickLeaveBalance, SickLeaveUsedDays,
               ExamLeaveBalance, ExamLeaveUsedDays
        FROM HR_Employees
        WHERE EmployeeCode = @id";

                SqlCommand cmdEmp = new SqlCommand(qEmp, con);
                cmdEmp.Parameters.AddWithValue("@id", userId);

                using (SqlDataReader dr = cmdEmp.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        emp = new
                        {
                            EmployeeId = dr["EmployeeId"],
                            EmployeeName = dr["EmployeeName"],
                            EmployeeCode = dr["EmployeeCode"],
                            AnnualBalance = dr["AnnualLeaveBalance"],
                            AnnualUsed = dr["AnnualLeaveUsedDays"],
                            CasualBalance = dr["CasualLeaveBalance"],
                            CasualUsed = dr["CasualLeaveUsedDays"],
                            SickBalance = dr["SickLeaveBalance"],
                            SickUsed = dr["SickLeaveUsedDays"],
                            ExamBalance = dr["ExamLeaveBalance"],
                            ExamUsed = dr["ExamLeaveUsedDays"]
                        };
                    }
                }

                ViewBag.CurrentEmployee = emp;

                // =========================
                // Employees
                // =========================
                string q1 = "SELECT EmployeeId, EmployeeName FROM HR_Employees WHERE IsActive = 1";

                SqlCommand cmd1 = new SqlCommand(q1, con);
                using (SqlDataReader dr1 = cmd1.ExecuteReader())
                {
                    while (dr1.Read())
                    {
                        employees.Add(new SelectListItem
                        {
                            Value = dr1["EmployeeId"].ToString(),
                            Text = dr1["EmployeeName"].ToString()
                        });
                    }
                }

                // =========================
                // Request Types
                // =========================
                string q2 = "SELECT RequestTypeId, Name FROM HR_RequestTypes";

                SqlCommand cmd2 = new SqlCommand(q2, con);
                using (SqlDataReader dr2 = cmd2.ExecuteReader())
                {
                    while (dr2.Read())
                    {
                        types.Add(new SelectListItem
                        {
                            Value = dr2["RequestTypeId"].ToString(),
                            Text = dr2["Name"].ToString()
                        });
                    }
                }
            }

            ViewBag.Employees = employees;
            ViewBag.RequestTypes = types;

            return View(vm);
        }
        [HttpPost]
        public IActionResult CreateRequest(LeaveRequestVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        var policy = new LeavePolicyService();

                        var datescheck = GetEmployeeDates(model.EmployeeId);

                        if (!datescheck.IsValid)
                        {
                            TempData["ErrorMessage"] = datescheck.Message;
                            return RedirectToAction("CreateRequest");
                        }
                        if (datescheck.HireDate == null)
                        {
                            TempData["ErrorMessage"] = "لا يمكن إنشاء إجازة قبل تحديد تاريخ التعيين";
                            return RedirectToAction("CreateRequest");
                        }
                        if (datescheck.InsuranceDate == null)
                        {
                            TempData["ErrorMessage"] = "لا يمكن إنشاء إجازة قبل تحديد تاريخ التأمين";
                            return RedirectToAction("CreateRequest");
                        }

                        int usedDays = GetUsedLeaveDays(model.EmployeeId);

  
                        List<string> insertedDays = new List<string>();
                        List<string> duplicateDays = new List<string>();

                        List<DateTime> dates = new List<DateTime>();

                        // =========================
                        // 1. تحديد الأيام
                        // =========================
                        if (model.IsMultipleDays && !string.IsNullOrEmpty(model.SelectedDays))
                        {
                            dates = model.SelectedDays
                                .Split(',')
                                .Select(x => DateTime.Parse(x.Trim()).Date)
                                .Distinct()
                                .ToList();
                        }
                        else
                        {
                            if (model.FromDate == null || model.ToDate == null)
                            {
                                TempData["ErrorMessage"] = "يجب إدخال التواريخ";
                                return RedirectToAction("CreateRequest");
                            }

                            if (model.ToDate < model.FromDate)
                            {
                                TempData["ErrorMessage"] = "تاريخ النهاية قبل البداية";
                                return RedirectToAction("CreateRequest");
                            }

                            dates = Enumerable.Range(0, (model.ToDate.Value - model.FromDate.Value).Days + 1)
                                .Select(d => model.FromDate.Value.AddDays(d))
                                .ToList();
                        }
                        int requestedDays = dates.Count;
                        (bool IsValid, string Message) balanceCheck = (true, "");

                        var excludedTypes = new List<int> { 1, 2, 3, 4 };

                        if (excludedTypes.Contains(model.RequestTypeId))
                        {
                            balanceCheck = CheckLeaveBalance(
                                model.EmployeeId,
                                model.RequestTypeId,
                                requestedDays
                            );
                        }

                        if (!balanceCheck.IsValid)
                        {
                            TempData["ErrorMessage"] = balanceCheck.Message;
                            return RedirectToAction("CreateRequest");
                        }
                        // =========================
                        // 2. Check duplicates
                        // =========================
                        var paramNames = dates.Select((d, i) => "@d" + i).ToList();

                        string checkQuery = $@"
                    SELECT CAST(FromDate AS DATE)
                    FROM HR_Requests
                    WHERE EmployeeId = @EmpId
                    AND RequestTypeId = @TypeId
                    AND CAST(FromDate AS DATE) IN ({string.Join(",", paramNames)})";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, con, transaction);
                        checkCmd.Parameters.AddWithValue("@EmpId", model.EmployeeId);
                        checkCmd.Parameters.AddWithValue("@TypeId", model.RequestTypeId);

                        for (int i = 0; i < dates.Count; i++)
                        {
                            checkCmd.Parameters.AddWithValue(paramNames[i], dates[i]);
                        }

                        HashSet<DateTime> existingDates = new HashSet<DateTime>();

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingDates.Add(Convert.ToDateTime(reader[0]).Date);
                            }
                        }
                        string filePath = null;

                        if (model.File != null && model.File.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            //string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.File.FileName);
                            string userName = (ViewBag.UserName ?? "User").ToString();

                            // تنظيف الاسم (مهم)
                            userName = userName.Replace(" ", "_");

                            // تاريخ بصيغة مناسبة
                            string datePart = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                            // الامتداد
                            string extension = Path.GetExtension(model.File.FileName);

                            // الاسم النهائي
                            string fileName = $"{userName}_{datePart}{extension}";
                            string fullPath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                model.File.CopyTo(stream);
                            }

                            filePath = "/uploads/" + fileName;
                        }
                        // =========================
                        // 3. Insert Request
                        // =========================
                        string insertRequest = @"
INSERT INTO HR_Requests
(RequestTypeId, EmployeeId, FromDate, ToDate, FromTime, ToTime, Location, Purpose, Result, FilePath, Notes, Status, CurrentStep, CreatedDate)
VALUES
(@TypeId, @EmpId, @FromDate, @ToDate, @FromTime, @ToTime, @Location, @Purpose, @Result, @FilePath, @Notes, 0, 1, GETDATE());

SELECT SCOPE_IDENTITY();";

                        SqlCommand insertCmd = new SqlCommand(insertRequest, con, transaction);
                        insertCmd.Parameters.Add("@TypeId", SqlDbType.Int).Value = model.RequestTypeId;
                        insertCmd.Parameters.Add("@EmpId", SqlDbType.Int).Value = model.EmployeeId;
                        insertCmd.Parameters.Add("@FromDate", SqlDbType.Date);
                        insertCmd.Parameters.Add("@ToDate", SqlDbType.Date);
                        insertCmd.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = model.Notes ?? "";
                        insertCmd.Parameters.Add("@FromTime", SqlDbType.Time).Value =
    (object?)model.FromTime ?? DBNull.Value;

                        insertCmd.Parameters.Add("@ToTime", SqlDbType.Time).Value =
                            (object?)model.ToTime ?? DBNull.Value;

                        insertCmd.Parameters.Add("@Location", SqlDbType.NVarChar).Value =
                            (object?)model.Location ?? DBNull.Value;

                        insertCmd.Parameters.Add("@Purpose", SqlDbType.NVarChar).Value =
                            (object?)model.Purpose ?? DBNull.Value;

                        insertCmd.Parameters.Add("@Result", SqlDbType.NVarChar).Value =
                            (object?)model.Result ?? DBNull.Value;

                        insertCmd.Parameters.Add("@FilePath", SqlDbType.NVarChar).Value =
                            (object?)filePath ?? DBNull.Value;
                        // =========================
                        // 4. Loop insert + approvals
                        // =========================
                        foreach (var date in dates)
                        {
                            if (existingDates.Contains(date))
                            {
                                duplicateDays.Add(date.ToString("yyyy-MM-dd"));
                                continue;
                            }
                            var check = policy.Validate(
      model.RequestTypeId,
      date,
      date,
      datescheck.HireDate.Value,
      datescheck.InsuranceDate ?? datescheck.HireDate.Value, // fallback
      usedDays
  );

                            if (!check.IsValid)
                            {
                                TempData["ErrorMessage"] = check.Message;
                                return RedirectToAction("CreateRequest");
                            }
                          
                            insertCmd.Parameters["@FromDate"].Value = date;
                            insertCmd.Parameters["@ToDate"].Value = date;

                            int requestId = Convert.ToInt32(insertCmd.ExecuteScalar());

                            // =========================
                            // 5. INSERT APPROVAL FLOW
                            // =========================
                            string approvalSql = @"
                        WITH RequestPath AS (
                            SELECT h.ChildRoleId, h.ParentRoleId, 1 AS StepLevel
                            FROM HR_Employees e
                            INNER JOIN HR_EmployeeHierarchy h 
                                ON e.RoleId = h.ChildRoleId
                            WHERE e.EmployeeId = @EmpId

                            UNION ALL

                            SELECT h.ChildRoleId, h.ParentRoleId, rp.StepLevel + 1
                            FROM HR_EmployeeHierarchy h
                            INNER JOIN RequestPath rp 
                                ON h.ChildRoleId = rp.ParentRoleId
                        )
                        INSERT INTO HR_RequestApprovals
                        (RequestId, StepOrder, ApproverId, Status)
                        SELECT 
                            @RequestId,
                            rp.StepLevel,
                            e.EmployeeId,
                            CASE WHEN rp.StepLevel = 1 THEN 1 ELSE 0 END
                        FROM RequestPath rp
                        INNER JOIN HR_Employees e 
                            ON rp.ParentRoleId = e.RoleId
                        WHERE e.IsActive = 1;";

                            SqlCommand approvalCmd = new SqlCommand(approvalSql, con, transaction);
                            approvalCmd.Parameters.AddWithValue("@RequestId", requestId);
                            approvalCmd.Parameters.AddWithValue("@EmpId", model.EmployeeId);

                            approvalCmd.ExecuteNonQuery();

                            insertedDays.Add(date.ToString("yyyy-MM-dd"));
                        }

                        // =========================
                        // 6. Result
                        // =========================
                        if (insertedDays.Count == 0)
                        {
                            transaction.Rollback();

                            TempData["ErrorMessage"] =
                                "❌ لم يتم حفظ أي طلب لأن الأيام مكررة:<br>" +
                                string.Join("<br>", duplicateDays);

                            return RedirectToAction("CreateRequest");
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] =
                            $"تم حفظ {insertedDays.Count} طلب بنجاح<br>" +
                            (duplicateDays.Any()
                                ? "⚠️ تم تجاهل: " + string.Join(" , ", duplicateDays)
                                : "");

                        return RedirectToAction("CreateRequest");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ ❌";
                        return RedirectToAction("CreateRequest");
                    }
                }
            }
        }
        public IActionResult PendingApprovals()
        {
            // نفترض إن ده الـ ID بتاع المدير اللي فاتح الشاشة حالياً
            int currentManagerId = Convert.ToInt32(ViewBag.UserName);

            List<PendingRequestVM> pendingRequests = new List<PendingRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Query بتجيب بيانات الطلب + بيانات الموظف اللي قدمه من جدول الـ Approvals
                string sql = @"
             SELECT a.ApproverId ,a.ApprovalId, a.RequestId, r.EmployeeId, e.EmployeeName, 
        rt.Name TypeName, r.FromDate, r.ToDate, a.StepOrder
 FROM HR_RequestApprovals a
 JOIN HR_Requests r ON a.RequestId = r.RequestId
 JOIN HR_Employees e ON r.EmployeeId = e.EmployeeId
 JOIN HR_Employees App ON App.EmployeeId = a.ApproverId
 JOIN HR_RequestTypes rt ON r.RequestTypeId = rt.RequestTypeId
 WHERE App.EmployeeCode = @ManagerId AND a.Status = 1 "; // 1 يعني Pending

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@ManagerId", currentManagerId);
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    pendingRequests.Add(new PendingRequestVM
                    {
                        ApprovalId = (int)rdr["ApprovalId"],
                        RequestId = (int)rdr["RequestId"],
                        EmployeeName = rdr["EmployeeName"].ToString(),
                        RequestType = rdr["TypeName"].ToString(),
                        FromDate = (DateTime)rdr["FromDate"],
                        ToDate = (DateTime)rdr["ToDate"]
                    });
                }
            }
            return View(pendingRequests);
        }
        [HttpPost]
        public IActionResult ProcessApproval(int approvalId, int requestId, int status, string notes)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        // 1. تحديث الخطوة الحالية (موافقة 2 أو رفض 3)
                        string updateCurrent = @"UPDATE HR_RequestApprovals 
                                         SET Status = @Status, ActionDate = GETDATE(), Notes = @Notes 
                                         WHERE ApprovalId = @AppId";
                        SqlCommand cmd1 = new SqlCommand(updateCurrent, con, trans);
                        cmd1.Parameters.AddWithValue("@Status", status);
                        cmd1.Parameters.AddWithValue("@Notes", notes ?? "");
                        cmd1.Parameters.AddWithValue("@AppId", approvalId);
                        cmd1.ExecuteNonQuery();

                        if (status == 2) // حالة الموافقة
                        {
                            // 2. تفعيل الخطوة التالية (لو موجودة)
                            string activateNext = @"UPDATE HR_RequestApprovals 
                                            SET Status = 1 
                                            WHERE RequestId = @ReqId AND StepOrder = 
                                            (SELECT StepOrder + 1 FROM HR_RequestApprovals WHERE ApprovalId = @AppId)";
                            SqlCommand cmd2 = new SqlCommand(activateNext, con, trans);
                            cmd2.Parameters.AddWithValue("@ReqId", requestId);
                            cmd2.Parameters.AddWithValue("@AppId", approvalId);
                            int nextRows = cmd2.ExecuteNonQuery();

                            // 3. لو مفيش خطوة تانية، يبقى الطلب اكتمل نهائياً
                            if (nextRows == 0)
                            {
                                string finalizeReq = "UPDATE HR_Requests SET Status = 1 WHERE RequestId = @ReqId";
                                new SqlCommand(finalizeReq, con, trans).Parameters.AddWithValue("@ReqId", requestId);
                                // هنا ممكن تضيف كود خصم الرصيد
                            }
                        }
                        else if (status == 3) // حالة الرفض
                        {
                            // لو رفض، بنقفل الطلب كله
                            string rejectReq = "UPDATE HR_Requests SET Status = 2 WHERE RequestId = @ReqId";
                            new SqlCommand(rejectReq, con, trans).Parameters.AddWithValue("@ReqId", requestId);
                        }

                        trans.Commit();
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
        }

    }
}
