namespace HR_App.ViewModel
{
    public class EmployeeVM
    {
        public int EmployeeId { get; set; }
        public int? WeeklyOffGroupId { get; set; }
        public string EmployeeCode { get; set; }
        public string RoleName { get; set; }
        public string EmployeeName { get; set; }

        public int? RoleId { get; set; }
        public string JobTitle { get; set; }

        public DateTime? HireDate { get; set; }
        public DateTime? LeaveDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? InsuranceStartDate { get; set; }
        // ✅ الجديد
        public int? ParentRoleId { get; set; }
        public decimal AnnualLeaveUsedDays { get; set; }
        public decimal AnnualLeaveBalance { get; set; }
        public decimal SecondaryLeaveUsedDays { get; set; }
        public decimal SecondaryLeaveBalance { get; set; }
        public decimal CasualLeaveUsedDays { get; set; } = 7;
        public decimal CasualLeaveBalance { get; set; } = 7;
        public decimal SickLeaveUsedDays { get; set; }
        public decimal SickLeaveBalance { get; set; }
        public decimal ExamLeaveUsedDays { get; set; }
        public decimal ExamLeaveBalance { get; set; }
        public DateTime? LastLeaveBalanceUpdate { get; set; }
    }
}
