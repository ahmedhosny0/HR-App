using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_App.ViewModel
{
    public class LeaveRequestVM
    {
        public int RequestId { get; set; }

        public int RequestTypeId { get; set; }
        public int EmployeeId { get; set; }
        public int HolidayId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string Notes { get; set; }

        public int Status { get; set; }
        public decimal MissionPercentage { get; set; }
        public int ActingEmployeeId { get; set; }
        public int MedicalExam { get; set; }
        public int CurrentStep { get; set; }
        public bool IsMultipleDays { get; set; }
        public string? SelectedDays { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }

        public string Location { get; set; }
        public string Purpose { get; set; }
        public string Result { get; set; }

        public IFormFile File { get; set; } // للرفع
    }
}
