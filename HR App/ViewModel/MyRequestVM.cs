namespace HR_App.ViewModel
{
    public class RequestDetailsVM
    {
        public string Employee { get; set; }
        public string Manager { get; set; }
        public string RoleName { get; set; }
        public string RequestType { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int RequestId { get; set; }
        public string StatusName { get; set; }
    }
}
