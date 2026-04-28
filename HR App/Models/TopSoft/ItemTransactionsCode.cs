using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class ItemTransactionsCode
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public int? ItemSerial { get; set; }

    public int? UserSerial { get; set; }

    public int? ItemStatus { get; set; }

    public string? Createdby { get; set; }

    public DateTime? TransDate { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public string? BranchName { get; set; }

    public string? Updatedby { get; set; }

    public string? Problem { get; set; }
    public bool? SendBeforeReceive { get; set; }
    public bool? Arrived { get; set; }

    public int? DeviceStatus { get; set; }

}
