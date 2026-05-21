namespace HR_App.ViewModel
{
    public class AiQueryVM
    {
        public string UserQuestion { get; set; } // السؤال بالعربي
        public string SqlGenerated { get; set; } // كود الـ SQL للتأكيد أو المراجعة (اختياري)
        public object ResultValue { get; set; }
        public bool IsTable { get; set; }
        public string ErrorMessage { get; set; } // لو حصلت مشكلة أو الـ AI مفهمش
    }
}
