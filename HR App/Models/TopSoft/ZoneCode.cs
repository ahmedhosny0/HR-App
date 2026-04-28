using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class ZoneCode
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<AreaCode> AreaCodes { get; set; } = new List<AreaCode>();

    public virtual ICollection<StreetCode> StreetCodes { get; set; } = new List<StreetCode>();
}
