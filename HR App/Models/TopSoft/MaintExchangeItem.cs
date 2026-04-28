using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class MaintExchangeItem
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public string? BranchName { get; set; }

	public int? ItemSerial { get; set; }
	public short? StoreId { get; set; }

	public double? Qty { get; set; }

    public string? Createdby { get; set; }

    public DateTime? TransDate { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public string? Notes { get; set; }
}
