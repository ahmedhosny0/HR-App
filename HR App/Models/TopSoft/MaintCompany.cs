using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class MaintCompany
{
    public int Serial { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public string? Notes { get; set; }

    public string? Createdby { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }
}
