using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;
public partial class AreaCode
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public int? ZoneSerial { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<StreetCode> StreetCodes { get; set; } = new List<StreetCode>();

    public virtual ZoneCode? ZoneSerialNavigation { get; set; }
}
