namespace HR_App.ViewModel
{
    public class LeavePolicyResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public int AllowedDays { get; set; }
    }
}
