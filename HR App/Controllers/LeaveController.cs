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
     
      

        public IActionResult PendingApprovals()
        {
            // نفترض إن ده الـ ID بتاع المدير اللي فاتح الشاشة حالياً
            int currentManagerId = Convert.ToInt32(ViewBag.UserName);

            List<PendingRequestVM> pendingRequests = new List<PendingRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Query بتجيب بيانات الطلب + بيانات الموظف اللي قدمه من جدول الـ Approvals
                string sql = @"
SELECT
    a.ApprovalId,
    a.RequestId,

    r.EmployeeId,
    r.RequestTypeId,
    r.FromDate,
    r.ToDate,
    r.Notes,

    r.MedicalExam,
    r.FilePath,

    r.FromTime,
    r.ToTime,
    r.Location,
    r.Purpose,
    r.Result,

    r.HolidayId,

    e.EmployeeName,

    rt.Name AS TypeName,

    h.HolidayName,r.CreatedDate,e.JobTitle

FROM HR_RequestApprovals a

JOIN HR_Requests r
ON a.RequestId = r.RequestId

JOIN HR_Employees e
ON r.EmployeeId = e.EmployeeId

JOIN HR_Employees App
ON App.EmployeeId = a.ApproverId

JOIN HR_RequestTypes rt
ON r.RequestTypeId = rt.RequestTypeId

LEFT JOIN HR_OfficialHolidays h
ON r.HolidayId = h.HolidayId

WHERE App.EmployeeCode = @ManagerId
AND a.Status = 1  "; // 1 يعني Pending

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
                        JobTitle = rdr["JobTitle"].ToString(),
                        RequestTypeId = (int)rdr["RequestTypeId"],
                        RequestType = rdr["TypeName"].ToString(),
                        FromDate = (DateTime)rdr["FromDate"],
                        CreatedDate = (DateTime)rdr["CreatedDate"],
                        Notes = rdr["Notes"]?.ToString(),

                        MedicalExam = rdr["MedicalExam"]?.ToString(),

                        FilePath = rdr["FilePath"]?.ToString(),

                        Location = rdr["Location"]?.ToString(),

                        Purpose = rdr["Purpose"]?.ToString(),

                        Result = rdr["Result"]?.ToString(),

                        HolidayName = rdr["HolidayName"]?.ToString(),
                       
                        FromTime = rdr["FromTime"] == DBNull.Value
    ? null
    : (TimeSpan?)rdr["FromTime"],

                        ToTime = rdr["ToTime"] == DBNull.Value
    ? null
    : (TimeSpan?)rdr["ToTime"],
                        ToDate = (DateTime)rdr["ToDate"]
                    });
                }
            }
            return View(pendingRequests);
        }
        [HttpPost]
        public IActionResult ProcessApprovalBulk([FromBody] ProcessApprovalBulkVM model)
        {
            // =========================
            // VALIDATION
            // =========================
            if (model == null ||
                model.ApprovalIds == null ||
                model.ApprovalIds.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "لا يوجد عناصر"
                });
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var approvalId in model.ApprovalIds)
                        {
                            // =========================
                            // GET REQUEST DATA
                            // =========================
                            string getReq = @"
                        SELECT 
                            RequestId,
                            StepOrder
                        FROM HR_RequestApprovals
                        WHERE ApprovalId = @AppId";

                            SqlCommand getCmd = new SqlCommand(getReq, con, trans);

                            getCmd.Parameters.AddWithValue("@AppId", approvalId);

                            int requestId = 0;
                            int stepOrder = 0;

                            using (var reader = getCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    requestId = Convert.ToInt32(reader["RequestId"]);
                                    stepOrder = Convert.ToInt32(reader["StepOrder"]);
                                }
                            }

                            // لو الطلب مش موجود
                            if (requestId == 0)
                            {
                                throw new Exception($"ApprovalId {approvalId} غير موجود");
                            }
                            string updateCurrent = " ";
                            // =========================
                            // UPDATE CURRENT STEP
                            // =========================
                            if (ViewBag.Role == "HeadOfficeHR" && model.Status==4)
                            {
                                 updateCurrent = @"
                     UPDATE HR_RequestApprovals
                        SET
                            Status = 2,
                            ActionDate = GETDATE(),
                            Notes = @Notes
                        WHERE ApprovalId = @AppId
";
                            }
                            else  if (ViewBag.Role == "HeadOfficeHR" && model.Status == 2)
                                {
                                updateCurrent = @"
                       UPDATE App
                        SET
                            Status =2 ,
                            ActionDate = GETDATE(),
                            Notes = @Notes

                        from HR_RequestApprovals App
                                                    inner join HR_Employees em on em.EmployeeId=App.ApproverId
                                                    WHERE  (em.EmployeeCode=1
                        and  requestId =" +requestId +") or (ApprovalId = @AppId) ";
                            }
                                else
                            {
                                updateCurrent = @"
                     UPDATE HR_RequestApprovals
                        SET
                            Status = @Status,
                            ActionDate = GETDATE(),
                            Notes = @Notes
                        WHERE ApprovalId = @AppId
";
                              
                            }
                               

                            SqlCommand cmd1 = new SqlCommand(updateCurrent, con, trans);

                            cmd1.Parameters.AddWithValue("@Status", model.Status);

                            cmd1.Parameters.AddWithValue(
                                "@Notes",
                                model.Notes ?? ""
                            );

                            cmd1.Parameters.AddWithValue(
                                "@AppId",
                                approvalId
                            );

                            cmd1.ExecuteNonQuery();


                            // =========================
                            // APPROVED
                            // =========================
                            if ((model.Status == 2 && ViewBag.Role != "HeadOfficeHR") || model.Status == 4)
                            {
                                // تفعيل الخطوة التالية
                                string activateNext = @"
                            UPDATE HR_RequestApprovals
                            SET Status = 1
                            WHERE RequestId = @ReqId
                            AND StepOrder = @NextStep";

                                SqlCommand cmd2 = new SqlCommand(
                                    activateNext,
                                    con,
                                    trans
                                );

                                cmd2.Parameters.AddWithValue(
                                    "@ReqId",
                                    requestId
                                );

                                cmd2.Parameters.AddWithValue(
                                    "@NextStep",
                                    stepOrder + 1
                                );

                                int nextRows = cmd2.ExecuteNonQuery();

                                // =========================
                                // FINAL APPROVAL
                                // =========================
                                if (nextRows == 0)
                                {
                                    string finalizeReq = @"
                                UPDATE HR_Requests
                                SET Status = 1
                                WHERE RequestId = @ReqId";

                                    SqlCommand cmd3 = new SqlCommand(
                                        finalizeReq,
                                        con,
                                        trans
                                    );

                                    cmd3.Parameters.AddWithValue(
                                        "@ReqId",
                                        requestId
                                    );

                                    cmd3.ExecuteNonQuery();

                                    // =========================
                                    // OPTIONAL:
                                    // خصم الرصيد هنا
                                    // =========================
                                }
                            }

                            // =========================
                            // REJECTED
                            // =========================
                            else if (model.Status == 3)
                            {
                                string rejectReq = @"
                            UPDATE HR_Requests
                            SET Status = 2
                            WHERE RequestId = @ReqId";

                                SqlCommand cmd4 = new SqlCommand(
                                    rejectReq,
                                    con,
                                    trans
                                );

                                cmd4.Parameters.AddWithValue(
                                    "@ReqId",
                                    requestId
                                );

                                cmd4.ExecuteNonQuery();
                            }
                        }

                        // =========================
                        // COMMIT
                        // =========================
                        trans.Commit();

                        return Json(new
                        {
                            success = true,
                            count = model.ApprovalIds.Count,
                            message = "تم تنفيذ العملية بنجاح"
                        });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();

                        return Json(new
                        {
                            success = false,
                            message = ex.Message
                        });
                    }
                }
            }
        }
 

    }
}
