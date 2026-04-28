using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkItem
{
    public int ItemId { get; set; }

    public string? ItemName { get; set; }

    public string? DpName { get; set; }

    public string? SubCategory { get; set; }

    public string? EtaCode { get; set; }

    public bool? IsActive { get; set; }
}
