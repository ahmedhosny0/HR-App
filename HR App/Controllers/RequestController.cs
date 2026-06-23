using HR_App.Services;
using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HR_App.Controllers
{
    public class RequestController : BaseController
    {

        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
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
        private (bool IsValid, string Message) CheckLeaveBalance(
     int employeeId,
     int requestTypeId,
     int requestedDays, List<DateTime> dates)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = @"
SELECT
    SecondaryLeaveBalance,
    ISNULL(SecondaryLeaveUsedDays,0) SecondaryLeaveUsedDays,
    ISNULL(AnnualLeaveUsedDays,0) AnnualLeaveUsedDays,

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
                        // =========================
                        // اعتيادي
                        // =========================
                        case 1:

                            balance =
                                Convert.ToInt32(
                                    dr["SecondaryLeaveBalance"]);

                            used =
                                Convert.ToInt32(
                                    dr["SecondaryLeaveUsedDays"]);

                            leaveName = "الاعتيادي";

                            break;

                        // =========================
                        // عارضة
                        // =========================
                        case 2:

                            balance = 7
                              ;

                            used =
                                Convert.ToInt32(
                                    dr["CasualLeaveUsedDays"]);

                            leaveName = "العارضة";

                            // =========================
                            // CHECK CASUAL MONTHLY LIMIT
                            // =========================

                            var periods = dates
                                .GroupBy(d =>
                                {
                                    if (d.Day >= 22)
                                        return new DateTime(d.Year, d.Month, 22);

                                    var prevMonth = d.AddMonths(-1);

                                    return new DateTime(
                                        prevMonth.Year,
                                        prevMonth.Month,
                                        22);
                                });

                            using (SqlConnection con2 = new SqlConnection(connStr))
                            {
                                con2.Open();

                                string casualQuery = @"

DECLARE @StartDate DATE = @PeriodStart;

DECLARE @EndDate DATE =
    DATEADD
    (
        DAY,
        -1,
        DATEADD(MONTH,1,@StartDate)
    );

SELECT ISNULL
    (
        SUM(DATEDIFF(DAY,R.FromDate,R.ToDate) + 1),
        0
    )
FROM HR_Requests R
    JOIN HR_Employees e
ON r.EmployeeId = e.EmployeeId
join HR_RequestApprovals a
ON a.RequestId = r.RequestId 
WHERE R.EmployeeId = @EmployeeId
AND R.RequestTypeId = 2
and a.Status=1
AND R.ToDate >= @StartDate
AND R.FromDate <= @EndDate";

                                foreach (var period in periods)
                                {
                                    DateTime periodStart = period.Key;
                                    DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

                                    int requestedInPeriod = period.Count();

                                    SqlCommand casualCmd =
                                        new SqlCommand(casualQuery, con2);

                                    casualCmd.Parameters.AddWithValue(
                                        "@EmployeeId",
                                        employeeId);

                                    casualCmd.Parameters.AddWithValue(
                                        "@PeriodStart",
                                        periodStart);

                                    int currentMonthCasual =
                                        Convert.ToInt32(
                                            casualCmd.ExecuteScalar());

                                    if ((currentMonthCasual + requestedInPeriod) > 2)
                                    {
                                        return
                                        (
                                            false,
                                            $"لا يمكن الحصول على أكثر من يومين عارضة خلال الفترة " +
                                            $"{periodStart:dd/MM/yyyy} إلى {periodEnd:dd/MM/yyyy}<br>" +
                                            $"المستخدم أو المعلق: {currentMonthCasual} يوم<br>" +
                                            $"المطلوب حالياً: {requestedInPeriod} يوم"
                                        );
                                    }
                                }
                            }

                            break;

                        // =========================
                        // مرضي
                        // =========================
                        case 3:

                            balance =
                                Convert.ToInt32(
                                    dr["SickLeaveBalance"]);

                            used =
                                Convert.ToInt32(
                                    dr["SickLeaveUsedDays"]);

                            leaveName = "المرضي";

                            break;

                        // =========================
                        // امتحانات
                        // =========================
                        case 4:

                            balance =
                                Convert.ToInt32(
                                    dr["ExamLeaveBalance"]);

                            used =
                                Convert.ToInt32(
                                    dr["ExamLeaveUsedDays"]);

                            leaveName = "الامتحانات";

                            break;
                    }

                    int pendingDays = 0;

                    string pendingQuery = @"
SELECT
    ISNULL(
        SUM(DATEDIFF(DAY,R.FromDate,R.ToDate) + 1)
    ,0)
FROM HR_Requests R
    JOIN HR_Employees e
ON r.EmployeeId = e.EmployeeId
join HR_RequestApprovals a
ON a.RequestId = r.RequestId
WHERE a.Status=1 and  R.EmployeeId = @EmployeeId ";
if (requestTypeId==1 )
                    {
                        pendingQuery += @" AND R.RequestTypeId in (1,2) ";
                    }
                    else
                    {
                        pendingQuery += @" AND R.RequestTypeId = @RequestTypeId ";
                    }
                        pendingQuery += @"

--AND R.Status IN (0) 

--AND LA.Status IN (0)
;"; // Pending / In Progress

                    using (SqlConnection conPending = new SqlConnection(connStr))
                    {
                        conPending.Open();

                        SqlCommand pendingCmd =
                            new SqlCommand(pendingQuery, conPending);

                        pendingCmd.Parameters.AddWithValue(
                            "@EmployeeId",
                            employeeId);

                        pendingCmd.Parameters.AddWithValue(
                            "@RequestTypeId",
                            requestTypeId);

                        pendingDays =
                            Convert.ToInt32(
                                pendingCmd.ExecuteScalar());
                    }

                    int remaining = balance - used;
                    int availableBalance = remaining - pendingDays;

                    // =========================
                    // CHECK BALANCE
                    // =========================
                    if (requestedDays > availableBalance)
                    {
                        return
                        (
                            false,
                            $"رصيد إجازة {leaveName} غير كافٍ <br>" +
                            $"الرصيد الأساسي: {balance} يوم<br>" +
                            $"المستخدم: {used} يوم<br>" +
                            $"المعلق: {pendingDays} يوم<br>" +
                            $"المتاح حالياً: {availableBalance} يوم<br>" +
                            $"المطلوب: {requestedDays} يوم"
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
            var allemployees = new List<SelectListItem>();
            var types = new List<SelectListItem>();
            var holidays = new List<SelectListItem>();

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
               SecondaryLeaveBalance, SecondaryLeaveUsedDays, AnnualLeaveUsedDays,
                CasualLeaveUsedDays,
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
                            SecondaryBalance = dr["SecondaryLeaveBalance"],
                            SecondaryUsed = dr["SecondaryLeaveUsedDays"],
                            AnnualUsed = dr["AnnualLeaveUsedDays"],
                            CasualUsed = dr["CasualLeaveUsedDays"],
                            SickBalance = dr["SickLeaveBalance"],
                            SickUsed = dr["SickLeaveUsedDays"],
                            ExamBalance = dr["ExamLeaveBalance"],
                            ExamUsed = dr["ExamLeaveUsedDays"]
                        };
                        ViewBag.CurrentEmployeeId = dr["EmployeeId"];

                    }

                }

                ViewBag.CurrentEmployee = emp;

                // =========================
                // Employees
                // =========================
                var role = ViewBag.Role;

                string q1 = "";

                if (role == "HeadOfficeHR")
                {
                    // HR يشوف كل الموظفين
                    q1 = "SELECT EmployeeId, EmployeeName FROM HR_Employees WHERE IsActive = 1";
                }
                else
                {
                    // أي حد غير HR يشوف نفسه بس
                    q1 = @"SELECT EmployeeId, EmployeeName 
           FROM HR_Employees 
           WHERE EmployeeCode = @userId AND IsActive = 1";
                }

                SqlCommand cmd1 = new SqlCommand(q1, con);

                if (role != "HeadOfficeHR")
                {
                    cmd1.Parameters.AddWithValue("@userId", userId);
                }

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
                string q3 = " SELECT HolidayId, HolidayName FROM HR_OfficialHolidays ";

                SqlCommand cmd3 = new SqlCommand(q3, con);
                using (SqlDataReader dr3 = cmd3.ExecuteReader())
                {
                    while (dr3.Read())
                    {
                        holidays.Add(new SelectListItem
                        {
                            Value = dr3["HolidayId"].ToString(),
                            Text = dr3["HolidayName"].ToString()
                        });
                    }
                }

                string q4 = "";

                    // HR يشوف كل الموظفين
                    q4 = "SELECT EmployeeId, EmployeeName FROM HR_Employees WHERE IsActive = 1";

                SqlCommand cmd4 = new SqlCommand(q4, con);

                using (SqlDataReader dr4 = cmd4.ExecuteReader())
                {
                    while (dr4.Read())
                    {
                        allemployees.Add(new SelectListItem
                        {
                            Value = dr4["EmployeeId"].ToString(),
                            Text = dr4["EmployeeName"].ToString()
                        });
                    }
                }
            }

            ViewBag.AllEmployees = allemployees;
            ViewBag.Employees = employees;
            ViewBag.RequestTypes = types;
            ViewBag.Holidays = holidays;

            return View(vm);
        }
        private HashSet<DateTime> GetExistingRequestDates(
    SqlConnection con,
    SqlTransaction transaction,
    int employeeId,
    int requestTypeId,
    List<DateTime> dates)
        {
            HashSet<DateTime> existingDates = new HashSet<DateTime>();

            if (dates == null || !dates.Any())
                return existingDates;

            var paramNames = dates
                .Select((d, i) => "@d" + i)
                .ToList();

            string query = $@"
        SELECT CAST(FromDate AS DATE)
        FROM HR_Requests
        WHERE EmployeeId = @EmpId
       -- AND RequestTypeId = @TypeId
        AND CAST(FromDate AS DATE) IN ({string.Join(",", paramNames)})";

            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
            {
                cmd.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
                cmd.Parameters.Add("@TypeId", SqlDbType.Int).Value = requestTypeId;

                for (int i = 0; i < dates.Count; i++)
                {
                    cmd.Parameters.Add(paramNames[i], SqlDbType.Date).Value = dates[i];
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingDates.Add(Convert.ToDateTime(reader[0]).Date);
                    }
                }
            }

            return existingDates;
        }

        private List<DateTime> RemoveWeekendsAndHolidays(
            SqlConnection con,
            SqlTransaction transaction,
            int employeeId,
            List<DateTime> dates)
        {
            var dateHelper = new DateHelperService();

            // =========================
            // WEEKLY OFF DAYS
            // =========================

            List<int> weeklyOffDays = new List<int>();

            string weeklySql = @"
SELECT wod.WeekDayId
FROM HR_Employees e
INNER JOIN HR_WeeklyOffGroups wog
    ON e.WeeklyOffGroupId = wog.WeeklyOffGroupId
INNER JOIN HR_WeeklyOffGroupDetails wod
    ON wog.WeeklyOffGroupId = wod.WeeklyOffGroupId
WHERE e.EmployeeId = @EmpId";

            using (SqlCommand cmd = new SqlCommand(weeklySql, con, transaction))
            {
                cmd.Parameters.AddWithValue("@EmpId", employeeId);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        weeklyOffDays.Add(Convert.ToInt32(rdr["WeekDayId"]));
                    }
                } // reader اتقفل هنا صح
            }

            // =========================
            // OFFICIAL HOLIDAYS
            // =========================

            List<DateTime> holidays = new List<DateTime>();

            string holidaySql = @"
SELECT HolidayDate
FROM HR_OfficialHolidays ";

            using (SqlCommand cmd = new SqlCommand(holidaySql, con, transaction))
            {
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        holidays.Add(Convert.ToDateTime(rdr["HolidayDate"]).Date);
                    }
                }
            }

            // =========================
            // FILTER
            // =========================

            var filteredDates = dates
                .Where(d =>
                {
                    int weekDayId = dateHelper.ConvertToDbWeekDay(d.DayOfWeek);

                    bool isWeeklyOff = weeklyOffDays.Contains(weekDayId);

                    bool isHoliday = holidays.Contains(d.Date);

                    return !isWeeklyOff && !isHoliday;
                })
                .Distinct()
                .ToList();

            return filteredDates;
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

                        List<DateTime> dates = new List<DateTime>();

                        // =========================
                        // Build Dates
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
                        // =========================
                        // REMOVE WEEKENDS & HOLIDAYS
                        // =========================

                        dates = RemoveWeekendsAndHolidays(
                            con,
                            transaction,
                            model.EmployeeId,
                            dates
                        );

                        if (!dates.Any())
                        {
                            TempData["ErrorMessage"] =
                                "كل الأيام المختارة إجازات أسبوعية أو رسمية";

                            return RedirectToAction("CreateRequest");
                        }
                        int requestedDays = dates.Count;

                        // =========================
                        // Balance Check
                        // =========================
                        var excludedTypes = new List<int> { 1, 2, 3, 4 };

                        if (excludedTypes.Contains(model.RequestTypeId))
                        {
                            var balanceCheck = CheckLeaveBalance(
                                model.EmployeeId,
                                model.RequestTypeId,
                                requestedDays, dates
                            );

                            if (!balanceCheck.IsValid)
                            {
                                TempData["ErrorMessage"] = balanceCheck.Message;
                                return RedirectToAction("CreateRequest");
                            }
                        }
                            
                        // =========================
                        // Existing Dates
                        // =========================
                        var existingDates = GetExistingRequestDates(
                            con,
                            transaction,
                            model.EmployeeId,
                            model.RequestTypeId,
                            dates
                        );

                        // =========================
                        // File Upload
                        // =========================
                        string filePath = null;

                        if (model.File != null && model.File.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string userName = (ViewBag.UserName ?? "User").ToString().Replace(" ", "_");
                            string datePart = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
                            string extension = Path.GetExtension(model.File.FileName);

                            string fileName = $"{userName}_{datePart}{extension}";
                            string fullPath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                model.File.CopyTo(stream);
                            }

                            filePath = "/uploads/" + fileName;
                        }

                        // =========================
                        // FIXED INSERT (IMPORTANT)
                        // =========================
                        string insertSql = @"
INSERT INTO HR_Requests
(RequestTypeId, EmployeeId, FromDate, ToDate, FromTime, ToTime,
 Location, Purpose, Result, FilePath, Notes, Status, CurrentStep, CreatedDate,MedicalExam,HolidayId,ActingEmployeeId,MissionPercentage)
OUTPUT INSERTED.RequestId
VALUES
(@TypeId, @EmpId, @FromDate, @ToDate, @FromTime, @ToTime,
 @Location, @Purpose, @Result, @FilePath, @Notes, 0, 1, GETDATE(),@MedicalExam,@HolidayId,@ActingEmployeeId,@MissionPercentage);";

                        List<string> insertedDays = new List<string>();
                        List<string> skippedDays = new List<string>();

                        foreach (var date in dates)
                        {
                            if (existingDates.Contains(date.Date))
                            {
                                skippedDays.Add(date.ToString("dd-MM-yyyy"));
                                continue;
                            }

                            var check = policy.Validate(
                                model.RequestTypeId,
                                date,
                                date,
                                datescheck.HireDate.Value,
                                datescheck.InsuranceDate ?? datescheck.HireDate.Value,
                                usedDays
                            );

                            if (!check.IsValid)
                            {
                                TempData["ErrorMessage"] = check.Message;
                                return RedirectToAction("CreateRequest");
                            }

                            SqlCommand insertCmd = new SqlCommand(insertSql, con, transaction);

                            insertCmd.Parameters.Add("@TypeId", SqlDbType.Int).Value = model.RequestTypeId;
                            insertCmd.Parameters.Add("@MedicalExam", SqlDbType.Int).Value = model.MedicalExam;
                            insertCmd.Parameters.Add("@EmpId", SqlDbType.Int).Value = model.EmployeeId;
                            insertCmd.Parameters.Add("@HolidayId", SqlDbType.Int).Value = model.HolidayId;
                            insertCmd.Parameters.Add("@ActingEmployeeId", SqlDbType.Int).Value = model.ActingEmployeeId;
                            insertCmd.Parameters.Add("@MissionPercentage", SqlDbType.Decimal).Value = model.MissionPercentage;
                            insertCmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = date; 
                            insertCmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = date;

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

                            insertCmd.Parameters.Add("@Notes", SqlDbType.NVarChar).Value =
                                model.Notes ?? "";

                            int requestId = Convert.ToInt32(insertCmd.ExecuteScalar());

                            if (requestId <= 0)
                            {
                                skippedDays.Add(date.ToString("dd-MM-yyyy"));
                                continue;
                            }

                            // =========================
                            // APPROVAL
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
                            approvalCmd.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                            approvalCmd.Parameters.Add("@EmpId", SqlDbType.Int).Value = model.EmployeeId;

                            approvalCmd.ExecuteNonQuery();

                            insertedDays.Add(date.ToString("dd-MM-yyyy"));
                        }

                        // =========================
                        // RESULT
                        // =========================
                        if (!insertedDays.Any())
                        {
                            transaction.Rollback();

                            TempData["ErrorMessage"] =
                                "❌ لم يتم حفظ أي طلب لأن الأيام مكررة:<br>" +
                                string.Join("<br>", skippedDays);

                            return RedirectToAction("CreateRequest");
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] =
                            $"✅ تم حفظ {insertedDays.Count} طلب بنجاح<br>" +
                            (skippedDays.Any()
                                ? "⚠️ الأيام المكررة:<br>" + string.Join("<br>", skippedDays)
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
        [HttpPost]
        public IActionResult DeleteRequest(int id)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // =========================
                // CHECK STATUS
                // =========================
                string checkSql =
                    @"SELECT Status
              FROM HR_Requests
              WHERE RequestId = @Id";

                SqlCommand checkCmd =
                    new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue("@Id", id);

                int status =
                    Convert.ToInt32(
                        checkCmd.ExecuteScalar());

                if (status != 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "لا يمكن حذف الطلب بعد اعتماده"
                    });
                }
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // حذف الموافقات
                        string deleteApprovals =
                            @"DELETE FROM HR_RequestApprovals
                      WHERE RequestId = @Id";

                        SqlCommand cmd1 =
                            new SqlCommand(deleteApprovals, con, transaction);

                        cmd1.Parameters.AddWithValue("@Id", id);

                        cmd1.ExecuteNonQuery();

                        // حذف الطلب
                        string deleteRequest =
                            @"DELETE FROM HR_Requests
                      WHERE RequestId = @Id";

                        SqlCommand cmd2 =
                            new SqlCommand(deleteRequest, con, transaction);

                        cmd2.Parameters.AddWithValue("@Id", id);

                        cmd2.ExecuteNonQuery();

                        transaction.Commit();

                        return Json(new
                        {
                            success = true
                        });
                    }
                    catch
                    {
                        transaction.Rollback();

                        return Json(new
                        {
                            success = false
                        });
                    }
                }
            }
        }
        public IActionResult EditRequest(int id)
        {
            LeaveRequestVM model = new LeaveRequestVM();
            var vm = new LeaveRequestVM();

            var employees = new List<SelectListItem>();
            var types = new List<SelectListItem>();
            var holidays = new List<SelectListItem>();

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
               SecondaryLeaveBalance, SecondaryLeaveUsedDays, AnnualLeaveUsedDays,
              CasualLeaveUsedDays,
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
                            SecondaryBalance = dr["SecondaryLeaveBalance"],
                            AnnualUsed = dr["AnnualLeaveUsedDays"],
                            SecondaryUsed = dr["SecondaryLeaveUsedDays"],
                            CasualUsed = dr["CasualLeaveUsedDays"],
                            SickBalance = dr["SickLeaveBalance"],
                            SickUsed = dr["SickLeaveUsedDays"],
                            ExamBalance = dr["ExamLeaveBalance"],
                            ExamUsed = dr["ExamLeaveUsedDays"]
                        };
                        ViewBag.CurrentEmployeeId = dr["EmployeeId"];

                    }

                }

                ViewBag.CurrentEmployee = emp;

                // =========================
                // Employees
                // =========================
                var role = ViewBag.Role;

                string q1 = "";

                if (role == "HeadOfficeHR")
                {
                    // HR يشوف كل الموظفين
                    q1 = "SELECT EmployeeId, EmployeeName FROM HR_Employees WHERE IsActive = 1";
                }
                else
                {
                    // أي حد غير HR يشوف نفسه بس
                    q1 = @"SELECT EmployeeId, EmployeeName 
           FROM HR_Employees 
           WHERE EmployeeCode = @userId AND IsActive = 1";
                }

                SqlCommand cmd1 = new SqlCommand(q1, con);

                if (role != "HeadOfficeHR")
                {
                    cmd1.Parameters.AddWithValue("@userId", userId);
                }

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
                string q3 = " SELECT HolidayId, HolidayName FROM HR_OfficialHolidays ";

                SqlCommand cmd3 = new SqlCommand(q3, con);
                using (SqlDataReader dr3 = cmd3.ExecuteReader())
                {
                    while (dr3.Read())
                    {
                        holidays.Add(new SelectListItem
                        {
                            Value = dr3["HolidayId"].ToString(),
                            Text = dr3["HolidayName"].ToString()
                        });
                    }
                }
            }

            ViewBag.Employees = employees;
            ViewBag.RequestTypes = types;
            ViewBag.Holidays = holidays;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();


                string sql = @"
SELECT *
FROM HR_Requests
WHERE RequestId = @Id";

                SqlCommand cmd = new SqlCommand(sql, con);

                cmd.Parameters.AddWithValue("@Id", id);


                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.RequestId =
                        Convert.ToInt32(dr["RequestId"]);

                    model.EmployeeId =
                        Convert.ToInt32(dr["EmployeeId"]);

                    model.RequestTypeId =
                        Convert.ToInt32(dr["RequestTypeId"]);

                    model.FromDate =
                        Convert.ToDateTime(dr["FromDate"]);

                    model.ToDate =
                        Convert.ToDateTime(dr["ToDate"]);

                    model.Notes =
                        dr["Notes"]?.ToString();

                    model.Location =
                        dr["Location"]?.ToString();

                    model.Purpose =
                        dr["Purpose"]?.ToString();
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult EditRequest(LeaveRequestVM model)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                // =========================
                // CHECK STATUS
                // =========================
                string checkSql =
                    @"SELECT Status
              FROM HR_Requests
              WHERE RequestId = @Id";

                SqlCommand checkCmd =
                    new SqlCommand(checkSql, con);

                checkCmd.Parameters.AddWithValue(
                    "@Id",
                    model.RequestId);

                int status =
                    Convert.ToInt32(
                        checkCmd.ExecuteScalar());

                // لو ليس Pending
                if (status != 0)
                {
                    TempData["ErrorMessage"] =
                        "لا يمكن تعديل الطلب بعد اعتماده";

                    return RedirectToAction("MyRequests");
                }
                string sql = @"

UPDATE HR_Requests
SET

    RequestTypeId = @RequestTypeId,
    FromDate = @FromDate,
    ToDate = @ToDate,
    Notes = @Notes,
    Location = @Location,
    Purpose = @Purpose

WHERE RequestId = @RequestId";

                SqlCommand cmd = new SqlCommand(sql, con);

                cmd.Parameters.AddWithValue(
                    "@RequestId",
                    model.RequestId);

                cmd.Parameters.AddWithValue(
                    "@RequestTypeId",
                    model.RequestTypeId);

                cmd.Parameters.AddWithValue(
                    "@FromDate",
                    model.FromDate);

                cmd.Parameters.AddWithValue(
                    "@ToDate",
                    model.ToDate);

                cmd.Parameters.AddWithValue(
                    "@Notes",
                    (object?)model.Notes ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Location",
                    (object?)model.Location ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Purpose",
                    (object?)model.Purpose ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "تم تعديل الطلب بنجاح";

            return RedirectToAction("MyRequests");
        }
        public IActionResult MyRequests()
        {
            var userCode = ViewBag.UserName;

            List<RequestDetailsVM> list = new List<RequestDetailsVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string sql = @"
        SELECT  

    Em.EmployeeName AS Employee,
    Em.JobTitle AS JobTitle,

    Ro.RoleName,

    Em.EmployeeId,
    Em.HireDate,

    Re.RequestId,

    rt.Name AS RequestType,

    Re.FromDate,

    Re.ToDate,

    Re.CreatedDate,

    STUFF
    (
        (
            SELECT 
                ' | ' +
                Mang2.EmployeeName + N' : ' +

                CASE 
                    WHEN App2.Status = 1 THEN N'قيد الانتظار'
                    WHEN App2.Status = 2 THEN N'مقبول'
                    WHEN App2.Status = 3 THEN N'مرفوض'
                    ELSE N'غير معروف'
                END

            FROM HR_RequestApprovals App2

            INNER JOIN HR_Employees Mang2
                ON App2.ApproverId = Mang2.EmployeeId

            WHERE App2.RequestId = Re.RequestId

            ORDER BY App2.StepOrder

            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)')
    ,1,3,'') AS ManagersResponse

FROM HR_Requests Re

INNER JOIN HR_Employees Em 
    ON Re.EmployeeId = Em.EmployeeId

INNER JOIN HR_Roles Ro 
    ON Ro.RoleId = Em.RoleId

INNER JOIN HR_RequestTypes rt 
    ON Re.RequestTypeId = rt.RequestTypeId 

WHERE Em.EmployeeCode = @code
  --AND Re.RequestId = 35

";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@code", userCode);

                con.Open();
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RequestDetailsVM
                    {
                        RequestId = Convert.ToInt32(dr["RequestId"]),
                        Employee = dr["Employee"].ToString(),
                        JobTitle = dr["JobTitle"].ToString(),
                        RequestType = dr["RequestType"].ToString(),
                        //Manager = dr["Manager"].ToString(),
                        RoleName = dr["RoleName"].ToString(),
                        FromDate = (DateTime)dr["FromDate"],
                        ToDate = (DateTime)dr["ToDate"],
                        CreatedDate = (DateTime)dr["CreatedDate"],
                        HireDate = (DateTime)dr["HireDate"],
                        StatusName = dr["ManagersResponse"].ToString()
                    });
                }
            }

            return View(list);
        }
        public async Task<IActionResult> AllRequests()
        {
            var branches = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT distinct UserName from StoreUser";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    branches.Add(new SelectListItem
                    {
                        Value = dr["UserName"].ToString(),
                        Text = dr["UserName"].ToString()
                    });
                }
            }

            ViewBag.Branches = branches;
            return View();
        }
        [HttpPost]
        public IActionResult AllRequests(string startDate, string endDate, string Branch, bool All, bool No, bool Yes, bool Wait)
        {
            var userCode = ViewBag.UserName;
            var branches = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string q = "SELECT distinct UserName from StoreUser";
                SqlCommand cmd = new SqlCommand(q, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    branches.Add(new SelectListItem
                    {
                        Value = dr["UserName"].ToString(),
                        Text = dr["UserName"].ToString()
                    });
                }
            }

            ViewBag.Branches = branches;
            List<RequestDetailsVM> list = new List<RequestDetailsVM>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string sql = @"
        SELECT  
    Em.EmployeeName AS Employee,
    Em.JobTitle AS JobTitle,
    Ro.RoleName,
    Em.EmployeeId,
    Em.HireDate,
    Re.RequestTypeId,
    Re.RequestId,
    rt.Name AS RequestType,
    Re.FromDate,
    Re.ToDate,
    Re.CreatedDate,

    STUFF
    (
        (
            SELECT 
                ' | ' +
                Mang2.EmployeeName + N' : ' +

                CASE 
                    WHEN App2.Status = 1 THEN N'قيد الانتظار'
                    WHEN App2.Status = 2 THEN N'مقبول'
                    --WHEN App2.Status = 4 THEN N'مقبول'
                    WHEN App2.Status = 3 THEN N'مرفوض'
                    ELSE N'غير معروف'
                END

            FROM HR_RequestApprovals App2

            INNER JOIN HR_Employees Mang2
                ON App2.ApproverId = Mang2.EmployeeId

            WHERE App2.RequestId = Re.RequestId

            ORDER BY App2.StepOrder

            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)')
    ,1,3,'') AS ManagersResponse

FROM HR_Requests Re 

INNER JOIN HR_Employees Em 
    ON Re.EmployeeId = Em.EmployeeId

INNER JOIN HR_Roles Ro 
    ON Ro.RoleId = Em.RoleId

INNER JOIN HR_RequestTypes rt 
    ON Re.RequestTypeId = rt.RequestTypeId 
where 1=1 
";
                if (Wait)
                {
                    sql += @" 
    AND EXISTS
    (
        SELECT 1
        FROM HR_RequestApprovals A
        WHERE A.RequestId = Re.RequestId
        AND A.Status = 1
    )";
                }

                if (Yes)
                {
                    sql += @" 
    AND EXISTS
    (
        SELECT 1
        FROM HR_RequestApprovals A
        WHERE A.RequestId = Re.RequestId
        AND A.Status = 2
    )";
                }

                if (No)
                {
                    sql += @" 
    AND EXISTS
    (
        SELECT 1
        FROM HR_RequestApprovals A
        WHERE A.RequestId = Re.RequestId
        AND A.Status = 3
    )";
                }
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@code", userCode);

                con.Open();
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new RequestDetailsVM
                    {
                        Employee = dr["Employee"].ToString(),
                        RequestType = dr["RequestType"].ToString(),
                        // Manager = dr["Manager"].ToString(),
                        RoleName = dr["RoleName"].ToString(),
                        JobTitle = dr["JobTitle"].ToString(),
                        HireDate = (DateTime)dr["HireDate"],
                        FromDate = (DateTime)dr["FromDate"],
                        ToDate = (DateTime)dr["ToDate"],
                        CreatedDate = (DateTime)dr["CreatedDate"],
                        StatusName = dr["ManagersResponse"].ToString()
                    });
                }
            }

            return View(list);
        }
        public JsonResult GetEmployeeLeaveBalance(int employeeId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = @"
SELECT 
    EmployeeName,
    EmployeeCode,
 SecondaryLeaveBalance,
    ISNULL( SecondaryLeaveUsedDays,0)  SecondaryLeaveUsedDays,
    ISNULL(AnnualLeaveUsedDays,0) AnnualLeaveUsedDays,

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

                        secondaryBalance = Convert.ToDecimal(dr["secondaryLeaveBalance"]),
                        secondaryUsed = Convert.ToDecimal(dr["secondaryLeaveUsedDays"]),
                        annualUsed = Convert.ToDecimal(dr["AnnualLeaveUsedDays"]),

                        casualUsed = Convert.ToDecimal(dr["CasualLeaveUsedDays"]),

                        sickBalance = Convert.ToDecimal(dr["SickLeaveBalance"]),
                        sickUsed = Convert.ToDecimal(dr["SickLeaveUsedDays"]),

                        examBalance = Convert.ToDecimal(dr["ExamLeaveBalance"]),
                        examUsed = Convert.ToDecimal(dr["ExamLeaveUsedDays"])
                    });
                }
            }

            return Json(new { success = false });
        }


    }
}
