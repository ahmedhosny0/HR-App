namespace HR_App.ViewModel
{
    public class PendingRequestVM
    {
        public int ApprovalId { get; set; }     // كود خطوة الموافقة عشان نعرف نحدثها
        public int RequestTypeId { get; set; }     // كود خطوة الموافقة عشان نعرف نحدثها
        public int RequestId { get; set; }      // كود الطلب الأصلي
        public string EmployeeName { get; set; } // اسم الموظف اللي قدم الطلب
        public string RequestType { get; set; }  // نوع الطلب (إجازة، إذن، إلخ)
        public DateTime FromDate { get; set; }  // تاريخ البداية
        public DateTime ToDate { get; set; }    // تاريخ النهاية
        public int StepOrder { get; set; }      // ترتيب الخطوة (اختياري للعرض)

        // ممكن تضيف أي حقول تانية محتاجها في الشاشة
        // مثلاً لو عايز تعرض عدد الأيام
        public int TotalDays => (ToDate - FromDate).Days + 1;
    }
}
