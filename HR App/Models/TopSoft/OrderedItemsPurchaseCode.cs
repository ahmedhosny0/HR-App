using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class OrderedItemsPurchaseCode
{
    public int Serial { get; set; }
    public int? Code { get; set; }

    public string? BranchName { get; set; }

    public string? ItemLookupCode { get; set; }

    public string? ItemName { get; set; }

    public double? Quantity { get; set; }
    public double? Balance { get; set; }

    public string? Createdby { get; set; }
    
    public string? Notes { get; set; }
    public string? Reply { get; set; }

    public DateTime? TransactionDate { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public int? OrderStatus { get; set; }

    public double? SendQty { get; set; }
}
