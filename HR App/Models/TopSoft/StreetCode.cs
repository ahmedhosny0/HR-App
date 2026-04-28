using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class StreetCode
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public int? BranchSerial { get; set; }

    public int? ZoneSerial { get; set; }

    public int? AreaSerial { get; set; }

    public string? Name { get; set; }

    public string? DeliveryTime { get; set; }

    public double? ServiceCost { get; set; }

    public virtual AreaCode? AreaSerialNavigation { get; set; }

    public virtual BranchDatum? BranchSerialNavigation { get; set; }

    public virtual ZoneCode? ZoneSerialNavigation { get; set; }
}
