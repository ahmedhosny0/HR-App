using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkBranchEvaluation
{
    public int Id { get; set; }

    public string? Createdby { get; set; }
    public string? BranchName { get; set; }
    public string? BranchManager { get; set; }
    public string? Shift { get; set; }
    public string? AreaManager { get; set; }

    public DateTime? EvaluationDate { get; set; }

    public int? TotalScore { get; set; }

    public virtual ICollection<CkEvaluationDetail> CkEvaluationDetails { get; set; } = new List<CkEvaluationDetail>();
}
