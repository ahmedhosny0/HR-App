using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class BranchDatum
{
    public int Serial { get; set; }

    public string? BranchIdR { get; set; }

    public string? BranchIdD { get; set; }

    public string? BranchName { get; set; }

    public virtual ICollection<StreetCode> StreetCodes { get; set; } = new List<StreetCode>();
}
