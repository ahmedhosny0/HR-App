using HR_App.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace HR_App.Repo
{
    public interface IRequestRepo
    {
       int GetUsedLeaveDays(int empId);
        (bool IsValid, string Message) CheckLeaveBalance(
     int employeeId,
     int requestTypeId,
     int requestedDays);
        EmployeeDatesResult GetEmployeeDates(int empId);
        EmployeeBalanceVM GetCurrentEmployee(string employeeCode);

        List<SelectListItem> GetEmployees(string employeeCode, string role);

        List<SelectListItem> GetRequestTypes();

        List<SelectListItem> GetHolidays();
        HashSet<DateTime> GetExistingRequestDates(
SqlConnection con,
SqlTransaction transaction,
int employeeId,
int requestTypeId,
List<DateTime> dates);
       

    }
}
