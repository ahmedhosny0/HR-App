namespace HR_App.ViewModel
{
    public class EmployeeBalanceVM
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeCode { get; set; }

        public decimal AnnualBalance { get; set; }
        public int AnnualUsed { get; set; }

        public decimal CasualBalance { get; set; }
        public int CasualUsed { get; set; }

        public decimal SickBalance { get; set; }
        public int SickUsed { get; set; }

        public decimal ExamBalance { get; set; }
        public int ExamUsed { get; set; }
    }
}
