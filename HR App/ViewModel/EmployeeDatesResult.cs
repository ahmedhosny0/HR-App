namespace HR_App.ViewModel
{
    public class EmployeeDatesResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }

        public DateTime? HireDate { get; set; }
        public DateTime? InsuranceDate { get; set; }
    }
}
