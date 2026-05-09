using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_App.ViewModel
{
    public class LeaveRequestVM
    {
        public int RequestId { get; set; }

        public int RequestTypeId { get; set; }
        public int EmployeeId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string Notes { get; set; }

        public int Status { get; set; }
        public int CurrentStep { get; set; }
        public bool IsMultipleDays { get; set; }
        public string? SelectedDays { get; set; }
    }
}
