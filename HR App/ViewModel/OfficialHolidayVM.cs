namespace HR_App.ViewModel
{
    public class OfficialHolidayVM
    {
        public int HolidayId { get; set; }

        public string HolidayName { get; set; }

        public DateTime HolidayDate { get; set; }

        public bool IsPaid { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
