using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class ItemsforOrder
{
    public int Serial { get; set; }

    public double? Factor { get; set; }

    public string? ItemLookupCode { get; set; }

    public string? ItemName { get; set; }
    public int? Active { get; set; }
    public int? CategorySerial { get; set; }

}
