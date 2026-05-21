namespace HR_App.ViewModel
{
    public class AiQueryVM
    {
        public string UserQuestion { get; set; } // السؤال بالعربي
        public string SqlGenerated { get; set; } // كود الـ SQL للتأكيد أو المراجعة (اختياري)
        public string ResultValue { get; set; }  // النتيجة الرقمية أو النصية اللي رجعت من الداتابيز
        public string ErrorMessage { get; set; } // لو حصلت مشكلة أو الـ AI مفهمش
    }
}
