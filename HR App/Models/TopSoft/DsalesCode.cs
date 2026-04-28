using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class DsalesCode
{
    public int Serial { get; set; }

    public int? SalesCode { get; set; }

    public string? ItemCode { get; set; }

    public double? Price { get; set; }

    public double? Quantity { get; set; }

    public double? Total { get; set; }

    public string? CategoryName { get; set; }

    public virtual HsalesCode? SalesCodeNavigation { get; set; }
}
