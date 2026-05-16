namespace HR_App.ViewModel
{
    public class WeeklyOffGroupVM
    {
        public int WeeklyOffGroupId { get; set; }

        public string GroupName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        // الأيام المختارة
        public List<int> SelectedDays { get; set; } = new List<int>();
    }
}
