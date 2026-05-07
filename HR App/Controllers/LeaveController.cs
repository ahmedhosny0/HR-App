using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

                string q = "SELECT RoleId, RoleName FROM HR_Roles";
                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RoleVM
                    {
                        RoleId = Convert.ToInt32(dr["RoleId"]),
                        RoleName = dr["RoleName"].ToString()
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult AddRole(RoleVM model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleName))
                return BadRequest("اسم الدور مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string insert = @"
        INSERT INTO HR_Roles (RoleName)
        VALUES (@RoleName)";

                SqlCommand cmd = new SqlCommand(insert, con);
                cmd.Parameters.AddWithValue("@RoleName", model.RoleName);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Roles");
        }
        public IActionResult EditRole(int id)
        {
            RoleVM model = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT RoleId, RoleName FROM HR_Roles WHERE RoleId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new RoleVM
                    {
                        RoleId = Convert.ToInt32(dr["RoleId"]),
                        RoleName = dr["RoleName"].ToString()
                    };
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult EditRole(RoleVM model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleName))
                return BadRequest("اسم الدور مطلوب");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"UPDATE HR_Roles
                     SET RoleName = @RoleName
                     WHERE RoleId = @Id";

                SqlCommand cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@RoleName", model.RoleName);
                cmd.Parameters.AddWithValue("@Id", model.RoleId);

                cmd.ExecuteNonQuery();
            }

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
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                foreach (var emp in model.Employees)
                {
                    string insert = @"
            INSERT INTO HR_Employees
            (EmployeeCode, EmployeeName, RoleId, JobTitle, HireDate, LeaveDate, IsActive, InsuranceStartDate, CreatedDate)
            VALUES
            (@Code, @Name, @RoleId, @Job, @HireDate, @LeaveDate, @IsActive, @InsuranceStart, GETDATE())";

                    SqlCommand cmd = new SqlCommand(insert, con);

                    cmd.Parameters.AddWithValue("@Code", emp.EmployeeCode ?? "");
                    cmd.Parameters.AddWithValue("@Name", emp.EmployeeName);
                    cmd.Parameters.AddWithValue("@RoleId", (object?)emp.RoleId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Job", emp.JobTitle ?? "");
                    cmd.Parameters.AddWithValue("@HireDate", emp.HireDate);
                    cmd.Parameters.AddWithValue("@LeaveDate", (object?)emp.LeaveDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@InsuranceStart", (object?)emp.InsuranceStartDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", emp.IsActive ? 1 : 0);

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
Emp.IsActive,
Role.RoleName
FROM HR_Employees Emp
LEFT JOIN HR_Roles Role 
ON Emp.RoleId = Role.RoleId
ORDER BY Emp.EmployeeId DESC";

                SqlCommand cmd = new SqlCommand(q, con);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new EmployeeVM
                    {
                        EmployeeId = Convert.ToInt32(dr["EmployeeId"]),
                        EmployeeCode = dr["EmployeeCode"].ToString(),
                        EmployeeName = dr["EmployeeName"].ToString(),
                        JobTitle = dr["JobTitle"].ToString(),
                        HireDate = Convert.ToDateTime(dr["HireDate"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        RoleName = dr["RoleName"].ToString()
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

                    model.HireDate = Convert.ToDateTime(dr["HireDate"]);

                    model.LeaveDate = dr["LeaveDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["LeaveDate"]);

                    model.InsuranceStartDate = dr["InsuranceStartDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["InsuranceStartDate"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
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

                string q = @"
UPDATE HR_Employees
SET
EmployeeCode=@Code,
EmployeeName=@Name,
RoleId=@RoleId,
JobTitle=@Job,
HireDate=@HireDate,
LeaveDate=@LeaveDate,
InsuranceStartDate=@Insurance,
IsActive=@IsActive
WHERE EmployeeId=@Id";

                SqlCommand cmd = new SqlCommand(q, con);

                cmd.Parameters.AddWithValue("@Code", model.EmployeeCode ?? "");
                cmd.Parameters.AddWithValue("@Name", model.EmployeeName);
                cmd.Parameters.AddWithValue("@RoleId", (object?)model.RoleId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Job", model.JobTitle ?? "");
                cmd.Parameters.AddWithValue("@HireDate", model.HireDate);
                cmd.Parameters.AddWithValue("@LeaveDate", (object?)model.LeaveDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Insurance", (object?)model.InsuranceStartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
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
        public IActionResult CreateRequest()
        {
            var vm = new LeaveRequestVM();

            var employees = new List<SelectListItem>();
            var types = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // Employees
                string q1 = "SELECT EmployeeId, EmployeeName FROM HR_Employees WHERE IsActive = 1";
                SqlCommand cmd1 = new SqlCommand(q1, con);
                SqlDataReader dr1 = cmd1.ExecuteReader();

                while (dr1.Read())
                {
                    employees.Add(new SelectListItem
                    {
                        Value = dr1["EmployeeId"].ToString(),
                        Text = dr1["EmployeeName"].ToString()
                    });
                }
                dr1.Close();

                // Request Types
                string q2 = "SELECT RequestTypeId, Name FROM HR_RequestTypes";
                SqlCommand cmd2 = new SqlCommand(q2, con);
                SqlDataReader dr2 = cmd2.ExecuteReader();

                while (dr2.Read())
                {
                    types.Add(new SelectListItem
                    {
                        Value = dr2["RequestTypeId"].ToString(),
                        Text = dr2["Name"].ToString()
                    });
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
                // بنستخدم Transaction عشان نضمن إن الطلب وخطواته يتسجلوا مع بعض أو لا
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // 1. استعلام إدخال الطلب مع الحصول على الـ ID الجديد
                        string insertRequest = @"
                    INSERT INTO HR_Requests 
                    (RequestTypeId, EmployeeId, FromDate, ToDate, Notes, Status, CurrentStep, CreatedDate)
                    VALUES 
                    (@TypeId, @EmpId, @FromDate, @ToDate, @Notes, 0, 1, GETDATE());
                    SELECT SCOPE_IDENTITY();"; // لجلب رقم الطلب اللي اتعمل حالا

                        SqlCommand cmd = new SqlCommand(insertRequest, con, transaction);
                        cmd.Parameters.AddWithValue("@TypeId", model.RequestTypeId);
                        cmd.Parameters.AddWithValue("@EmpId", model.EmployeeId);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)model.FromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)model.ToDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");

                        // الحصول على رقم الطلب
                        int newRequestId = Convert.ToInt32(cmd.ExecuteScalar());

                        // 2. استعلام الـ Recursive CTE لإدخال خطوات الموافقة أوتوماتيكياً
                        string insertApprovals = @"
                    WITH RequestPath AS (
                        SELECT h.ChildRoleId, h.ParentRoleId, 1 AS StepLevel
                        FROM HR_Employees e
                        INNER JOIN HR_EmployeeHierarchy h ON e.RoleId = h.ChildRoleId
                        WHERE e.EmployeeId = @EmpId
                        UNION ALL
                        SELECT h.ChildRoleId, h.ParentRoleId, rp.StepLevel + 1
                        FROM HR_EmployeeHierarchy h
                        INNER JOIN RequestPath rp ON h.ChildRoleId = rp.ParentRoleId
                    )
                    INSERT INTO HR_RequestApprovals (RequestId, StepOrder, ApproverId, Status)
                    SELECT 
                        @RequestId,
                        rp.StepLevel,
                        e.EmployeeId,
                        CASE WHEN rp.StepLevel = 1 THEN 1 ELSE 0 END -- أول خطوة بتبقى Pending (1)
                    FROM RequestPath rp
                    INNER JOIN HR_Employees e ON rp.ParentRoleId = e.RoleId
                    WHERE e.IsActive = 1;";

                        SqlCommand cmdApprovals = new SqlCommand(insertApprovals, con, transaction);
                        cmdApprovals.Parameters.AddWithValue("@RequestId", newRequestId);
                        cmdApprovals.Parameters.AddWithValue("@EmpId", model.EmployeeId);

                        cmdApprovals.ExecuteNonQuery();

                        transaction.Commit(); // حفظ التغييرات
                        TempData["SuccessMessage"] = "تم إنشاء الطلب وتحديد مسار الموافقات بنجاح ✅";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // إلغاء كل شيء في حال حدوث خطأ
                        ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                        return View(model);
                    }
                }
            }

            return RedirectToAction("CreateRequest");
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
            JOIN HR_RequestTypes rt ON r.RequestTypeId = rt.RequestTypeId
            WHERE a.ApproverId = @ManagerId AND a.Status = 1"; // 1 يعني Pending

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
