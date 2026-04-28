using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkItemBarcode
{
    public int BarcodeId { get; set; }

    public int? ItemId { get; set; }

    public string? ItemLookupCode { get; set; }

    public bool? IsPrimary { get; set; }

    public string? ItemLookupCodeTrim { get; set; }
}
