using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkEvaluationItem
{
    public int Id { get; set; }

    public string? SectionName { get; set; }

    public string ItemText { get; set; } = null!;
    public int Grade { get; set; }
    public virtual ICollection<CkEvaluationDetail> CkEvaluationDetails { get; set; } = new List<CkEvaluationDetail>();
}
