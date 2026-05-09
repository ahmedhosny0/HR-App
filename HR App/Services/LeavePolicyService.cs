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
            if (requestTypeId == 1)
            {
                if ((fromDate - today).TotalDays < 2)
                {
                    return new LeavePolicyResult
                    {
                        IsValid = false,
                        Message = "الإجازة الاعتيادي يجب تقديمها قبلها بـ 48 ساعة على الأقل ⏳"
                    };
                }
            }

            // =========================
            // 2. بعد التعيين 3 شهور
            // =========================
            if ((today - hireDate).TotalDays < 90)
            {
                return new LeavePolicyResult
                {
                    IsValid = false,
                    Message = "لا يمكن طلب إجازة قبل مرور 3 شهور من التعيين ❌"
                };
            }

            // =========================
            // 3. رصيد الإجازات
            // =========================
            int allowed = CalculateAllowedDays(hireDate, insuranceDate, totalUsedDays);

            if (totalDays > allowed)
            {
                return new LeavePolicyResult
                {
                    IsValid = false,
                    AllowedDays = allowed,
                    Message = $"رصيد الإجازات غير كافي. المتاح لك: {allowed} يوم"
                };
            }

            // =========================
            // 4. أنواع الإجازات
            // =========================
            if (requestTypeId == 2 && totalDays > 7)
            {
                return new LeavePolicyResult
                {
                    IsValid = false,
                    Message = "الإجازة العارضة لا تتجاوز 7 أيام ❌"
                };
            }

            if (requestTypeId == 3 && totalDays > 90)
            {
                return new LeavePolicyResult
                {
                    IsValid = false,
                    Message = "الإجازة المرضي لا تتجاوز 3 شهور ❌"
                };
            }

            return new LeavePolicyResult
            {
                IsValid = true,
                AllowedDays = allowed,
                Message = "مسموح"
            };
        }

        private int CalculateAllowedDays(DateTime hireDate, DateTime insuranceDate, int used)
        {
            int years = DateTime.Now.Year - hireDate.Year;

            int allowed;

            // أول سنة
            if (years <= 1)
                allowed = 15;

            // من 1 إلى 10 سنين تأمين + خبرة
            else if ((DateTime.Now - insuranceDate).TotalDays >= 365 * 10 && years <= 1)
                allowed = 15;

            // من 2 إلى 19 سنة
            else if (years < 20)
                allowed = 21;

            // أكثر من 20 سنة
            else
                allowed = 40;

            return allowed - used;
        }
    }
}
