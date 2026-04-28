using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CloseDay
{
    public int Id { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Alert { get; set; }
}
