using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class ItemsPurchase
{
    public string? CategoryName { get; set; }

    public string? Company { get; set; }

    public string? Warehouse { get; set; }

    public string? Barcode { get; set; }

    public string? ItemName { get; set; }
    public string? Notes { get; set; }

    public string? Unit { get; set; }

    public int Serial { get; set; }
    public bool IsActive { get; set; }
}
