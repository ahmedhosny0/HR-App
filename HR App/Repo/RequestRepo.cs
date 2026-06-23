using HR_App.Services;
using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HR_App.Repo
{
    public class RequestRepo : IRequestRepo
    {
        //private readonly LeavePolicyService _leavePolicyService;

        //public RequestRepo(LeavePolicyService leavePolicyService)
        //{
        //    _leavePolicyService = leavePolicyService;
        //}
        private string connStr = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public int GetUsedLeaveDays(int empId)
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
        public EmployeeDatesResult GetEmployeeDates(int empId)
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
       public  (bool IsValid, string Message) CheckLeaveBalance(
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
                        // =========================
                        // اعتيادي
                        // =========================
                        case 1:

                            balance =
                                Convert.ToInt32(
                                    dr["AnnualLeaveBalance"]);

                            used =
                                Convert.ToInt32(
                                    dr["AnnualLeaveUsedDays"]);

                            leaveName = "الاعتيادي";

                            break;

                        // =========================
                        // عارضة
                        // =========================
                        case 2:

                            balance =
                                Convert.ToInt32(
                                    dr["CasualLeaveBalance"]);

                            used =
                                Convert.ToInt32(
                                    dr["CasualLeaveUsedDays"]);

                            leaveName = "العارضة";

                            // =========================
                            // CHECK MONTHLY LIMIT
                            // =========================
                            int currentMonthCasual = 0;

                            using (SqlConnection con2 =
                                new SqlConnection(connStr))
                            {
                                con2.Open();

                                string casualQuery = @"

DECLARE @Today DATE = GETDATE();

DECLARE @StartDate DATE;
DECLARE @EndDate DATE;

IF DAY(@Today) >= 22
BEGIN

    SET @StartDate =
        DATEFROMPARTS
        (
            YEAR(@Today),
            MONTH(@Today),
            22
        );

END
ELSE
BEGIN

    SET @StartDate =
        DATEFROMPARTS
        (
            YEAR(DATEADD(MONTH, -1, @Today)),
            MONTH(DATEADD(MONTH, -1, @Today)),
            22
        );

END

SET @EndDate =
    DATEADD
    (
        DAY,
        -1,
        DATEADD(MONTH, 1, @StartDate)
    );



;WITH LastApproval AS
(
    SELECT
        A.RequestId,
        A.Status,
        ROW_NUMBER() OVER
        (
            PARTITION BY A.RequestId
            ORDER BY A.StepOrder DESC
        ) AS RN
    FROM HR_RequestApprovals A
)

SELECT
ISNULL
(
    SUM(DATEDIFF(DAY, R.FromDate, R.ToDate) + 1),
    0
)

FROM HR_Requests R

JOIN LastApproval LA
    ON LA.RequestId = R.RequestId
   AND LA.RN = 1

WHERE R.EmployeeId = @EmployeeId
AND R.RequestTypeId = 2
AND LA.Status = 2
AND R.FromDate >= @StartDate
AND R.FromDate <= @EndDate";

                                SqlCommand casualCmd =
                                    new SqlCommand(casualQuery, con2);

                                casualCmd.Parameters.AddWithValue(
                                    "@EmployeeId",
                                    employeeId);

                                currentMonthCasual =
                                    Convert.ToInt32(
                                        casualCmd.ExecuteScalar());
                            }

                            // =========================
                            // VALIDATE MONTHLY LIMIT
                            // =========================
                            if ((currentMonthCasual + requestedDays) > 2)
                            {
                                return
                                (
                                    false,
                                    $"لا يمكن الحصول على أكثر من يومين عارضة خلال الفترة الحالية<br>" +
                                    $"تم استخدام {currentMonthCasual} يوم"
                                );
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

                    int remaining = balance - used;

                    // =========================
                    // CHECK BALANCE
                    // =========================
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
        public List<SelectListItem> GetHolidays()
        {
            var list = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql =
                    @"SELECT HolidayId, HolidayName
              FROM HR_OfficialHolidays";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = dr["HolidayId"].ToString(),
                            Text = dr["HolidayName"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        public EmployeeBalanceVM GetCurrentEmployee(string employeeCode)
        {
            EmployeeBalanceVM emp = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql = @"
        SELECT EmployeeId,
               EmployeeName,
               EmployeeCode,
               AnnualLeaveBalance,
               AnnualLeaveUsedDays,
               CasualLeaveBalance,
               CasualLeaveUsedDays,
               SickLeaveBalance,
               SickLeaveUsedDays,
               ExamLeaveBalance,
               ExamLeaveUsedDays
        FROM HR_Employees
        WHERE EmployeeCode = @id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", employeeCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            emp = new EmployeeBalanceVM
                            {
                                EmployeeId = Convert.ToInt32(dr["EmployeeId"]),
                                EmployeeName = dr["EmployeeName"].ToString(),
                                EmployeeCode = dr["EmployeeCode"].ToString(),
                                AnnualBalance = Convert.ToDecimal(dr["AnnualLeaveBalance"]),
                                AnnualUsed = Convert.ToInt32(dr["AnnualLeaveUsedDays"]),
                                CasualBalance = Convert.ToDecimal(dr["CasualLeaveBalance"]),
                                CasualUsed = Convert.ToInt32(dr["CasualLeaveUsedDays"]),
                                SickBalance = Convert.ToDecimal(dr["SickLeaveBalance"]),
                                SickUsed = Convert.ToInt32(dr["SickLeaveUsedDays"]),
                                ExamBalance = Convert.ToDecimal(dr["ExamLeaveBalance"]),
                                ExamUsed = Convert.ToInt32(dr["ExamLeaveUsedDays"])
                            };
                        }
                    }
                }
            }

            return emp;
        }
    
    public List<SelectListItem> GetRequestTypes()
        {
            var list = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql =
                    @"SELECT RequestTypeId, Name
              FROM HR_RequestTypes";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = dr["RequestTypeId"].ToString(),
                            Text = dr["Name"].ToString()
                        });
                    }
                }
            }

            return list;
        }
        public List<SelectListItem> GetEmployees(string employeeCode, string role)
        {
            var employees = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string sql = role == "HeadOfficeHR"
                    ? @"SELECT EmployeeId, EmployeeName
                FROM HR_Employees
                WHERE IsActive = 1"
                    : @"SELECT EmployeeId, EmployeeName
                FROM HR_Employees
                WHERE EmployeeCode = @userId
                AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    if (role != "HeadOfficeHR")
                        cmd.Parameters.AddWithValue("@userId", employeeCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            employees.Add(new SelectListItem
                            {
                                Value = dr["EmployeeId"].ToString(),
                                Text = dr["EmployeeName"].ToString()
                            });
                        }
                    }
                }
            }

            return employees;
        }
        public HashSet<DateTime> GetExistingRequestDates(
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


    }
}