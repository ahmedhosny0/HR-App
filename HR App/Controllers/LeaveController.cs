using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace HR_App.Controllers
{
    public class LeaveController : BaseController
    {
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");

        // =========================
        // Create Request
        // =========================
        public IActionResult Create()
        {
            LeaveRequestVM vm = new LeaveRequestVM
            {
                LeaveTypes = new List<SelectListItem>(),
                Employees = new List<SelectListItem>(),
                Managers = new List<SelectListItem>()
            };

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // 👇 نوع الإجازة
                string q1 = "SELECT ModelTypeSerial, ModelName FROM ModelTypeCode";
                SqlCommand cmd1 = new SqlCommand(q1, con);
                SqlDataReader dr1 = cmd1.ExecuteReader();

                while (dr1.Read())
                {
                    vm.LeaveTypes.Add(new SelectListItem
                    {
                        Value = dr1["ModelTypeSerial"].ToString(),
                        Text = dr1["ModelName"].ToString()
                    });
                }
                dr1.Close();

                // 👇 الموظفين
                string q2 = "SELECT EmployeeSerial, EmployeeName FROM EmployeeCode";
                SqlCommand cmd2 = new SqlCommand(q2, con);
                SqlDataReader dr2 = cmd2.ExecuteReader();

                while (dr2.Read())
                {
                    vm.Employees.Add(new SelectListItem
                    {
                        Value = dr2["EmployeeSerial"].ToString(),
                        Text = dr2["EmployeeName"].ToString()
                    });
                }
                dr2.Close();
                // 👇 الموظفين
                string q3 = "SELECT * FROM EmployeeCode where IsManager=1";
                SqlCommand cmd3 = new SqlCommand(q2, con);
                SqlDataReader dr3 = cmd2.ExecuteReader();

                while (dr3.Read())
                {
                    vm.Managers.Add(new SelectListItem
                    {
                        Value = dr3["EmployeeSerial"].ToString(),
                        Text = dr3["EmployeeName"].ToString()
                    });
                }
                dr3.Close();
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(LeaveRequestVM model)
        {
            LeaveRequestVM vm = new LeaveRequestVM
            {
                LeaveTypes = new List<SelectListItem>(),
                Employees = new List<SelectListItem>(),
                Managers = new List<SelectListItem>()
            };

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // 👇 نوع الإجازة
                string q1 = "SELECT ModelTypeSerial, ModelName FROM ModelTypeCode";
                SqlCommand cmd1 = new SqlCommand(q1, con);
                SqlDataReader dr1 = cmd1.ExecuteReader();

                while (dr1.Read())
                {
                    vm.LeaveTypes.Add(new SelectListItem
                    {
                        Value = dr1["ModelTypeSerial"].ToString(),
                        Text = dr1["ModelName"].ToString()
                    });
                }
                dr1.Close();

                // 👇 الموظفين
                string q2 = "SELECT EmployeeSerial, EmployeeName FROM EmployeeCode";
                SqlCommand cmd2 = new SqlCommand(q2, con);
                SqlDataReader dr2 = cmd2.ExecuteReader();

                while (dr2.Read())
                {
                    vm.Employees.Add(new SelectListItem
                    {
                        Value = dr2["EmployeeSerial"].ToString(),
                        Text = dr2["EmployeeName"].ToString()
                    });
                }
                dr2.Close();
                // 👇 الموظفين
                string q3 = "SELECT * FROM EmployeeCode where IsManager=1";
                SqlCommand cmd3 = new SqlCommand(q2, con);
                SqlDataReader dr3 = cmd2.ExecuteReader();

                while (dr3.Read())
                {
                    vm.Managers.Add(new SelectListItem
                    {
                        Value = dr3["EmployeeSerial"].ToString(),
                        Text = dr3["EmployeeName"].ToString()
                    });
                }
                dr3.Close();
            }
            if (model.EmployeeSerial>0)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        string query = @"
            INSERT INTO ModelCode
            (ModelTypeSerial, EmployeeSerial, ModelStatus, HRModelStatus, Notes, FromDate, ToDate, CreatedDate,CreatedUser)
            VALUES
            (@ModelTypeSerial, @EmployeeSerial, 0, 0, @Notes, @FromDate, @ToDate, GETDATE(),@CreatedUser)";

                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@ModelTypeSerial", model.ModelTypeSerial);
                        cmd.Parameters.AddWithValue("@EmployeeSerial", model.EmployeeSerial);
                        //cmd.Parameters.AddWithValue("@ManagerSerial", model.ManagerSerial);
                        cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                        cmd.Parameters.AddWithValue("@FromDate", model.FromDate);
                        cmd.Parameters.AddWithValue("@ToDate", model.ToDate);
                        cmd.Parameters.AddWithValue("@CreatedUser", ViewBag.Username);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "تم الحفظ بنجاح ✅";
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ ❌";
                }

            }

            return RedirectToAction("Create") ;
        }

        // =========================
        // Employee Requests
        // =========================
        public IActionResult MyRequests(int empId)
        {
            List<LeaveRequestVM> list = new List<LeaveRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT * FROM RptModel --WHERE EmployeeSerial = @empId";

                SqlCommand cmd = new SqlCommand(query, con);
               //cmd.Parameters.AddWithValue("@empId", empId);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new LeaveRequestVM
                    {
                        ModelSerial = Convert.ToInt32(dr["ModelSerial"]),
                        StartDate = ((DateTime)dr["FromDate"]).ToString("yyyy-MM-dd"),
                        EndDate = Convert.ToDateTime(dr["ToDate"]).ToString("yyyy-MM-dd"),
                        ModelName = dr["ModelName"].ToString(),
                        EmployeeName = dr["Notes"].ToString(),
                        Job = dr["EmployeeJob"].ToString(),
                        RoleName = dr["RoleName"].ToString(),
                        ManagerName = dr["ManagerName"].ToString(),
                        ManagerReply = dr["OrderStatus"].ToString(),
                        HRReply = dr["HROrderStatus"].ToString(),
                        Notes = dr["Notes"].ToString(),
                    });
                }
            }

            return View(list);
        }

        // =========================
        // Manager شاشة الموافقة
        // =========================
        public IActionResult ManagerRequests(int managerId)
        {
            List<LeaveRequestVM> list = new List<LeaveRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT * FROM ModelCode 
            WHERE ManagerSerial = @managerId AND ModelStatus = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@managerId", managerId);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new LeaveRequestVM
                    {
                        ModelSerial = Convert.ToInt32(dr["ModelSerial"]),
                        FromDate = Convert.ToDateTime(dr["FromDate"]),
                        ToDate = Convert.ToDateTime(dr["ToDate"]),
                        Notes = dr["Notes"].ToString()
                    });
                }
            }

            return View(list);
        }

        // موافقة المدير
        public IActionResult ApproveByManager(int id)
        {
            UpdateStatus(id, 1, null);
            return RedirectToAction("ManagerRequests");
        }

        public IActionResult RejectByManager(int id)
        {
            UpdateStatus(id, 2, null);
            return RedirectToAction("ManagerRequests");
        }

        // =========================
        // HR شاشة الموافقة
        // =========================
        public IActionResult HRRequests()
        {
            List<LeaveRequestVM> list = new List<LeaveRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT * FROM ModelCode 
            WHERE ModelStatus = 1 AND HRModelStatus = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new LeaveRequestVM
                    {
                        ModelSerial = Convert.ToInt32(dr["ModelSerial"]),
                        FromDate = Convert.ToDateTime(dr["FromDate"]),
                        ToDate = Convert.ToDateTime(dr["ToDate"]),
                        Notes = dr["Notes"].ToString()
                    });
                }
            }

            return View(list);
        }

        public IActionResult ApproveByHR(int id)
        {
            UpdateStatus(id, null, 1);
            return RedirectToAction("HRRequests");
        }

        public IActionResult RejectByHR(int id)
        {
            UpdateStatus(id, null, 2);
            return RedirectToAction("HRRequests");
        }

        // =========================
        // Helper
        // =========================
        private void UpdateStatus(int id, int? managerStatus, int? hrStatus)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "UPDATE ModelCode SET ";

                if (managerStatus != null)
                    query += "ModelStatus = @ms ";

                if (hrStatus != null)
                    query += (managerStatus != null ? "," : "") + " HRModelStatus = @hs ";

                query += " WHERE ModelSerial = @id";

                SqlCommand cmd = new SqlCommand(query, con);

                if (managerStatus != null)
                    cmd.Parameters.AddWithValue("@ms", managerStatus);

                if (hrStatus != null)
                    cmd.Parameters.AddWithValue("@hs", hrStatus);

                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
