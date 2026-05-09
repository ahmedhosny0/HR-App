namespace HR_App.ViewModel
{
    public class EmployeeVM
    {
        public int EmployeeId { get; set; }

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
        public decimal AnnualLeaveBalance { get; set; }
        public decimal CasualLeaveBalance { get; set; } = 7;
        public int SickLeaveUsedDays { get; set; }
        public DateTime? LastLeaveBalanceUpdate { get; set; }
    }
}
