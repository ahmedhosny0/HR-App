namespace HR_App.Services
{
    public class DateHelperService
    {
        public int ConvertToDbWeekDay(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Saturday => 1,
                DayOfWeek.Sunday => 2,
                DayOfWeek.Monday => 3,
                DayOfWeek.Tuesday => 4,
                DayOfWeek.Wednesday => 5,
                DayOfWeek.Thursday => 6,
                DayOfWeek.Friday => 7,
                _ => 0
            };
        }
    }
}
