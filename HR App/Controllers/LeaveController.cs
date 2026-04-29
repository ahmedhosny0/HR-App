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

        // =========================
        // Create Request
        // =========================
        public IActionResult ManagerRequests()
        {
            // جلب ID المدير الحالي من الـ Session
            //  var currentManagerId = HttpContext.Session.GetString("EmployeeId");

            //if (string.IsNullOrEmpty(currentManagerId))
            //{
            //    return RedirectToAction("Login", "Login");
            //}
           
            List<LeaveRequestVM> list = new List<LeaveRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // نستخدم View أو Join لجلب أسماء الموظفين بدلاً من الأكواد فقط
                string query = @"
                  select * from RptModel ";
                if (ViewBag.Role == "HeadOfficeHR")
                {
                    query += @"
where ModelStatus='1'and  HRModelStatus='0' ";
            }
                if (ViewBag.Role == "Manager")
                {
                    query += @"
where ManagerCode=@managerCode and ModelStatus='0' ";
                }
                if (ViewBag.Role == "HRandManager")
                {
                    query += @"
where ManagerCode=@managerCode and ModelStatus='1' and  HRModelStatus='0'  ";
                }
                SqlCommand cmd = new SqlCommand(query, con);
                if (ViewBag.Role == "Manager" || ViewBag.Role == "HRandManager")
                {
                cmd.Parameters.AddWithValue("@managerCode", ViewBag.UserName);
                }

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new LeaveRequestVM
                    {
                        ModelSerial = Convert.ToInt32(dr["ModelSerial"]),
                        EmployeeSerial = Convert.ToInt32(dr["EmployeeCode"]),
                        EmployeeName = dr["EmployeeName"].ToString(),
                        ModelName = dr["ModelName"].ToString(),
                        Job = dr["EmployeeJob"]?.ToString(),
                        FromDate = Convert.ToDateTime(dr["FromDate"]),
                        ToDate = Convert.ToDateTime(dr["ToDate"]),
                        Notes = dr["Notes"].ToString()
                    });
                }
            }

            return View(list);
        }
        // 4. إجراءات المدير (موافقة/رفض)
        // =========================
        [HttpPost]
        public IActionResult ProcessLeaveRequest(int id, int status, string reason)
        {
            // نفترض أن الدور مخزن في ViewBag أو نمرره من الصفحة
            string userRole = ViewBag.Role; // 'Manager' أو 'HR'

            if (userRole == "Manager")
            {
                // تحديث حالة المدير (status: 1 موافقة، 2 رفض)
                UpdateStatus(id, status, null, reason);
            }
            else if (userRole == "HeadOfficeHR")
            {
                // تحديث حالة الـ HR
                UpdateStatus(id, null, status, reason);
            }

            return Ok();
        }

        // تعديل دالة التحديث لاستقبال الملاحظات (Notes)
        private void UpdateStatus(int id, int? managerStatus, int? hrStatus, string notes)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                List<string> updates = new List<string>();
                if (managerStatus.HasValue) updates.Add("ModelStatus = @ms");
                if (hrStatus.HasValue) updates.Add("HRModelStatus = @hs");
                if (!string.IsNullOrEmpty(notes)) updates.Add("Notes = @notes"); // تحديث الملاحظات إذا وجدت

                if (updates.Count == 0) return;

                string query = $"UPDATE ModelCode SET {string.Join(",", updates)} WHERE ModelSerial = @id";

                SqlCommand cmd = new SqlCommand(query, con);
                if (managerStatus.HasValue) cmd.Parameters.AddWithValue("@ms", managerStatus.Value);
                if (hrStatus.HasValue) cmd.Parameters.AddWithValue("@hs", hrStatus.Value);
                if (!string.IsNullOrEmpty(notes)) cmd.Parameters.AddWithValue("@notes", notes);
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        // دالة لحساب عدد أيام العمل بين تاريخين
        private int GetWorkDays(DateTime start, DateTime end)
        {
            int workDays = 0;
            // نبدأ من اليوم التالي للحظة التقديم وحتى يوم بداية الإجازة
            for (var date = start.Date.AddDays(1); date < end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Saturday)
                {
                    workDays++;
                }
            }
            return workDays;
        }
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

            //    // 👇 الموظفين
            //    string q2 = "SELECT EmployeeSerial, EmployeeName FROM EmployeeCode";
            //    SqlCommand cmd2 = new SqlCommand(q2, con);
            //    SqlDataReader dr2 = cmd2.ExecuteReader();

            //    while (dr2.Read())
            //    {
            //        vm.Employees.Add(new SelectListItem
            //        {
            //            Value = dr2["EmployeeSerial"].ToString(),
            //            Text = dr2["EmployeeName"].ToString()
            //        });
            //    }
            //    dr2.Close();
            //    // 👇 الموظفين
            //    string q3 = "SELECT * FROM EmployeeCode where IsManager=1";
            //    SqlCommand cmd3 = new SqlCommand(q2, con);
            //    SqlDataReader dr3 = cmd2.ExecuteReader();

            //    while (dr3.Read())
            //    {
            //        vm.Managers.Add(new SelectListItem
            //        {
            //            Value = dr3["EmployeeSerial"].ToString(),
            //            Text = dr3["EmployeeName"].ToString()
            //        });
            //    }
            //    dr3.Close();
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(LeaveRequestVM model)
        {

            if (model.ModelTypeSerial > 0)
            {
                if (model.IsMultipleDays && !string.IsNullOrEmpty(model.SelectedDays))
                {
                    List<string> insertedDays = new List<string>();
                    List<string> duplicateDays = new List<string>();

                    // ✅ Parse + تنظيف + إزالة التكرار
                    var dates = model.SelectedDays
                        .Split(',')
                        .Select(d => DateTime.ParseExact(d.Trim(), "yyyy-MM-dd", null).Date)
                        .Distinct()
                        .ToList();

                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();

                        // ✅ 1. نجيب كل الأيام الموجودة مرة واحدة (Performance 🔥)
                        var paramNames = dates.Select((d, i) => "@d" + i).ToList();

                        string checkQuery = $@"
            SELECT CAST(FromDate AS DATE)
            FROM ModelCode
            WHERE EmployeeSerial = @EmpSerial
            AND CAST(FromDate AS DATE) IN ({string.Join(",", paramNames)})";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                        checkCmd.Parameters.AddWithValue("@EmpSerial", ViewBag.UserName);

                        for (int i = 0; i < dates.Count; i++)
                        {
                            checkCmd.Parameters.AddWithValue(paramNames[i], dates[i]);
                        }

                        HashSet<DateTime> existingDates = new HashSet<DateTime>();

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                existingDates.Add(reader.GetDateTime(0).Date);
                            }
                        }

                        // ✅ 2. Prepare Insert Command (مرة واحدة)
                        string insertQuery = @"
            INSERT INTO ModelCode
            (ModelTypeSerial, EmployeeSerial, ModelStatus, HRModelStatus, Notes, FromDate, ToDate, CreatedDate, CreatedUser)
            VALUES
            (@ModelTypeSerial, @EmployeeSerial, 0, 0, @Notes, @Date, @Date, GETDATE(), @CreatedUser)";

                        SqlCommand insertCmd = new SqlCommand(insertQuery, con);
                        insertCmd.Parameters.Add("@ModelTypeSerial", SqlDbType.Int).Value = model.ModelTypeSerial;
                        insertCmd.Parameters.Add("@EmployeeSerial", SqlDbType.VarChar).Value = ViewBag.UserName;
                        insertCmd.Parameters.Add("@Notes", SqlDbType.VarChar).Value = model.Notes ?? "";
                        insertCmd.Parameters.Add("@Date", SqlDbType.Date);
                        insertCmd.Parameters.Add("@CreatedUser", SqlDbType.VarChar).Value = ViewBag.Username;

                        // ✅ 3. Loop logic
                        foreach (var date in dates)
                        {
                            //// ❌ منع الويك إند
                            //if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                            //{
                            //    duplicateDays.Add(date.ToString("yyyy-MM-dd") + " (عطلة)");
                            //    continue;
                            //}

                            // ❌ منع التكرار
                            if (existingDates.Contains(date))
                            {
                                duplicateDays.Add(date.ToString("yyyy-MM-dd"));
                                continue;
                            }

                            // ✅ Insert
                            insertCmd.Parameters["@Date"].Value = date;
                            insertCmd.ExecuteNonQuery();

                            insertedDays.Add(date.ToString("yyyy-MM-dd"));
                        }
                    }
                    if (insertedDays.Count == 0)
                    {
                        TempData["ErrorMessage"] =
                            "❌ لم يتم حفظ أي يوم لأن جميع الأيام مسجلة مسبقاً:<br><br>" +
                            string.Join("<br>", duplicateDays);

                        return RedirectToAction("Create");
                    }
                    // ✅ رسالة موحدة احترافية
                    TempData["SuccessMessage"] =
                        $"تم حفظ {insertedDays.Count} يوم بنجاح" +
                        (duplicateDays.Any()
                            ? "<br>⚠️ تم تجاهل الأيام التالية: " + string.Join(" , ", duplicateDays)
                            : "");

                    return RedirectToAction("Create");
                }
                else
                {
                    try
                    {
                        List<string> insertedDays = new List<string>();
                        List<string> duplicateDays = new List<string>();

                        DateTime from = model.FromDate.Date;
                        DateTime to = model.ToDate.Date;

                        // ❌ Validation أساسي
                        if (to < from)
                        {
                            TempData["ErrorMessage"] = "تاريخ النهاية قبل البداية ❌";
                            return RedirectToAction("Create");
                        }

                        // ✅ تحويل الفترة إلى قائمة أيام
                        var dates = Enumerable.Range(0, (to - from).Days + 1)
                                              .Select(d => from.AddDays(d))
                                              .ToList();

                        using (SqlConnection con = new SqlConnection(connStr))
                        {
                            con.Open();

                            // ✅ 1. شرط 48 ساعة للإجازة الاعتيادي
                            if (model.ModelTypeSerial == 1)
                            {
                                int workDaysBetween = GetWorkDays(DateTime.Now, from);
                                if (workDaysBetween < 2)
                                {
                                    TempData["ErrorMessage"] =
                                        "يجب تقديم طلب الإجازة الاعتيادي قبل موعدها بيومي عمل على الأقل ⏳";
                                    return RedirectToAction("Create");
                                }
                            }

                            // ✅ 2. نجيب الأيام الموجودة مرة واحدة
                            var paramNames = dates.Select((d, i) => "@d" + i).ToList();

                            string checkQuery = $@"
            SELECT CAST(FromDate AS DATE)
            FROM ModelCode
            WHERE EmployeeSerial = @EmpSerial
            AND CAST(FromDate AS DATE) IN ({string.Join(",", paramNames)})";

                            SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                            checkCmd.Parameters.AddWithValue("@EmpSerial", ViewBag.UserName);

                            for (int i = 0; i < dates.Count; i++)
                            {
                                checkCmd.Parameters.AddWithValue(paramNames[i], dates[i]);
                            }

                            HashSet<DateTime> existingDates = new HashSet<DateTime>();

                            using (var reader = checkCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    existingDates.Add(reader.GetDateTime(0).Date);
                                }
                            }

                            // ✅ 3. تجهيز Insert مرة واحدة
                            string insertQuery = @"
            INSERT INTO ModelCode
            (ModelTypeSerial, EmployeeSerial, ModelStatus, HRModelStatus, Notes, FromDate, ToDate, CreatedDate, CreatedUser)
            VALUES
            (@ModelTypeSerial, @EmployeeSerial, 0, 0, @Notes, @Date, @Date, GETDATE(), @CreatedUser)";

                            SqlCommand insertCmd = new SqlCommand(insertQuery, con);
                            insertCmd.Parameters.Add("@ModelTypeSerial", SqlDbType.Int).Value = model.ModelTypeSerial;
                            insertCmd.Parameters.Add("@EmployeeSerial", SqlDbType.VarChar).Value = ViewBag.UserName;
                            insertCmd.Parameters.Add("@Notes", SqlDbType.VarChar).Value = model.Notes ?? "";
                            insertCmd.Parameters.Add("@Date", SqlDbType.Date);
                            insertCmd.Parameters.Add("@CreatedUser", SqlDbType.VarChar).Value = ViewBag.Username;

                            // ✅ 4. اللوب
                            foreach (var date in dates)
                            {
                                //// ❌ منع الجمعة والسبت
                                //if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                                //{
                                //    duplicateDays.Add(date.ToString("yyyy-MM-dd") + " (عطلة)");
                                //    continue;
                                //}

                                // ❌ منع التكرار
                                if (existingDates.Contains(date))
                                {
                                    duplicateDays.Add(date.ToString("yyyy-MM-dd"));
                                    continue;
                                }

                                // ✅ Insert
                                insertCmd.Parameters["@Date"].Value = date;
                                insertCmd.ExecuteNonQuery();

                                insertedDays.Add(date.ToString("yyyy-MM-dd"));
                            }
                        }
                        if (insertedDays.Count == 0)
                        {
                            TempData["ErrorMessage"] =
                                "❌ لم يتم حفظ أي يوم لأن جميع الأيام مسجلة مسبقاً:<br><br>" +
                                string.Join("<br>", duplicateDays);

                            return RedirectToAction("Create");
                        }

                        // ✅ رسالة واحدة احترافية
                        TempData["SuccessMessage"] =
                            $"تم حفظ {insertedDays.Count} يوم بنجاح" +
                            (duplicateDays.Any()
                                ? "<br>⚠️   تم تجاهل الأيام التالية لأنهم مسجلين من قبل:" + string.Join(" , ", duplicateDays)
                                : "");

                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ ❌";
                    }

                    return RedirectToAction("Create");
                }
            }

            return RedirectToAction("Create");
        }
        // =========================
        // Employee Requests
        // =========================
        public IActionResult MyRequests(int empId)
        {
            List<LeaveRequestVM> list = new List<LeaveRequestVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // يفضل جلب البيانات مرتبة حسب التاريخ
                string query = "SELECT * FROM RptModel WHERE EmployeeCode = @empId ORDER BY FromDate DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@empId", ViewBag.UserName);
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
                        EmployeeName = dr["EmployeeName"].ToString(),
                        Job = dr["EmployeeJob"].ToString(),
                        EmployeeSerial = Convert.ToInt32(dr["EmployeeCode"]),
                        ManagerName = dr["ManagerName"].ToString(),
                        ManagerReply = dr["OrderStatus"].ToString(),
                        HRReply = dr["HROrderStatus"].ToString(),
                        Notes = dr["Notes"].ToString()
                    });
                }
            }
            // --- منطق التجميع الجديد ---
            var monthlySummary = list.GroupBy(item => {
                DateTime dt = DateTime.Parse(item.StartDate);
                // إذا كان اليوم >= 22، ينتمي للشهر القادم إدارياً
                DateTime reportDate = dt.Day >= 22 ? dt.AddMonths(1) : dt;
                return new { reportDate.Year, reportDate.Month };
            })
            .Select(group => new {
                PeriodTitle = $"شهر {group.Key.Month} / {group.Key.Year}",
                // حساب عدد كل نموذج داخل هذه الفترة
                ModelCounts = group.GroupBy(m => m.ModelName)
                                   .Select(mc => new {
                                       Name = mc.Key,
                                       Count = mc.Count()
                                   }).ToList()
            })
            .OrderByDescending(o => o.PeriodTitle).ToList();

            ViewBag.Summary = monthlySummary;
            return View(list);
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

       

    }
}
