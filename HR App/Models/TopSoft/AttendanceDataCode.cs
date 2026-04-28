using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class AttendanceDataCode
{
    public int Serial { get; set; }

    public string? UserId { get; set; }

    public DateTime? LoginDate { get; set; }

    public string? EventType { get; set; }

    public int? WorkCode { get; set; }

    public string? BranchCode { get; set; }

    public DateTime InsertedAt { get; set; }
}
