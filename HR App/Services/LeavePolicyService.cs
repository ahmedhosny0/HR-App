using HR_App.ViewModel;

namespace HR_App.Services
{
    public class LeavePolicyService
    {
        public LeavePolicyResult Validate(
            int requestTypeId,
            DateTime fromDate,
            DateTime toDate,
            DateTime hireDate,
            DateTime insuranceDate,
            int totalUsedDays)
        {
            var result = new LeavePolicyResult { IsValid = true };

            int totalDays = (toDate - fromDate).Days + 1;
            DateTime today = DateTime.Today;

            // =========================
            // 1. قبل 48 ساعة (اعتيادي)
            // =========================
            if (requestTypeId == 1 || requestTypeId == 8)
            {
                if ((fromDate - today).TotalDays < 2)
                {
                    return new LeavePolicyResult
                    {
                        IsValid = false,
                        Message = "الإجازة الاعتيادي أو الغير مدفوعه يجب تقديمها قبلها بـ 48 ساعة على الأقل ⏳"
                    };
                }
            }

            // =========================
            // 2. بعد التعيين 3 شهور
            // =========================
            if (requestTypeId == 1 || requestTypeId == 2)
            {
                if ((today - hireDate).TotalDays < 90)
                {
                    return new LeavePolicyResult
                    {
                        IsValid = false,
                        Message = "لا يمكن طلب إجازة قبل مرور 3 شهور من التعيين ❌"
                    };
                }
            }

            return new LeavePolicyResult
            {
                IsValid = true,
                Message = "مسموح"
            };
        }

    }
}
