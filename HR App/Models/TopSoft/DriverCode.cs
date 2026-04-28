using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class DriverCode
{
    public int DriverCode1 { get; set; }

    public string DriverName { get; set; } = null!;

    public string? Phone1 { get; set; }

    public string? Phone2 { get; set; }

    public string? Phone3 { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public int? ZoneSerial { get; set; }

    public int? AreaSerial { get; set; }

    public int? StreetSerial { get; set; }

    public string? Notes { get; set; }
}
