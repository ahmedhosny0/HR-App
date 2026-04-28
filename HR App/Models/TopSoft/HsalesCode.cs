using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class HsalesCode
{
    public int Serial { get; set; }

    public int? SalesCode { get; set; }

    public string? BranchCode { get; set; }

    public string? CustomerCode { get; set; }

    public double? GrandTotal { get; set; }

    public double? GrandTotalwithFees { get; set; }

    public DateTime? SalesOrderDate { get; set; }

    public DateTime? Createddatetime { get; set; }

    public DateTime? Updateddatetime { get; set; }

    public string? Createdby { get; set; }

    public string? Notes { get; set; }

    public double? Fees { get; set; }

    public int? SalesStatus { get; set; }

    public string? Deliverytime { get; set; }

    public string? Message { get; set; }

    public string? CancelBy { get; set; }

    public int? DriverCode { get; set; }

    public int? OrderStatus { get; set; }

    public virtual ICollection<DsalesCode> DsalesCodes { get; set; } = new List<DsalesCode>();
}
