using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_App.ViewModel
{
    public class LeaveRequestVM
    {
        public int ModelSerial { get; set; }

        public int EmployeeSerial { get; set; }   // 👈 المختار
        public int ManagerSerial { get; set; }

        public int ModelTypeSerial { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Notes { get; set; }
        public string Job { get; set; }
        public string RoleName { get; set; }
        public string EmployeeName { get; set; }
        public string ManagerName { get; set; }
        public string ModelName { get; set; }
        public string ManagerReply { get; set; }
        public string HRReply { get; set; }

        public List<SelectListItem> LeaveTypes { get; set; }
        public List<SelectListItem> Employees { get; set; }   // 👈 الجديد
        public List<SelectListItem> Managers { get; set; }   // 👈 الجديد
    }
}
