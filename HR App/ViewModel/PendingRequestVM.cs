namespace HR_App.ViewModel
{
    public class PendingRequestVM
    {
        public int ApprovalId { get; set; }     // كود خطوة الموافقة عشان نعرف نحدثها
        public int RequestTypeId { get; set; }     // كود خطوة الموافقة عشان نعرف نحدثها
        public int RequestId { get; set; }      // كود الطلب الأصلي
        public string EmployeeName { get; set; } // اسم الموظف اللي قدم الطلب
        public string JobTitle { get; set; } // اسم الموظف اللي قدم الطلب
        public string RequestType { get; set; }  // نوع الطلب (إجازة، إذن، إلخ)
        public DateTime FromDate { get; set; }  // تاريخ البداية
        public DateTime CreatedDate { get; set; }  // تاريخ البداية
        public DateTime ToDate { get; set; }    // تاريخ النهاية
        public int StepOrder { get; set; }      // ترتيب الخطوة (اختياري للعرض)
        public string Notes { get; set; }
        public string MedicalExam { get; set; }

        public string FilePath { get; set; }

        public TimeSpan? FromTime { get; set; }

        public TimeSpan? ToTime { get; set; }

        public string Location { get; set; }

        public string Purpose { get; set; }

        public string Result { get; set; }

        public int? HolidayId { get; set; }

        public string HolidayName { get; set; }
        // ممكن تضيف أي حقول تانية محتاجها في الشاشة
        // مثلاً لو عايز تعرض عدد الأيام
        public int TotalDays => (ToDate - FromDate).Days + 1;
    }
}
