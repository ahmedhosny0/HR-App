using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_App.ViewModel
{
    public class WorkflowStepVM
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public string ApproverType { get; set; }

        public int RequestTypeId { get; set; }
        public string RequestTypeName { get; set; }

        public int? RoleId { get; set; }   // ✅ مهم جدًا
        public string RoleName { get; set; }
        // Dropdowns
        public List<SelectListItem> RequestTypes { get; set; }
        public List<SelectListItem> Roles { get; set; }

        // List
        public List<WorkflowStepVM> StepsList { get; set; }
    }
}
