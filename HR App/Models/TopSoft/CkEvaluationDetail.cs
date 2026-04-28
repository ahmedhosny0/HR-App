using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkEvaluationDetail
{
    public int Id { get; set; }

    public int EvaluationId { get; set; }

    public int ItemId { get; set; }
    
    public string? Notes { get; set; }
    
    public string? Comment { get; set; }
    public decimal? Percentage { get; set; }

    public bool? Score { get; set; }
    public bool? NotScore { get; set; }
    public bool? NotAvailable { get; set; }
    public int? Grade { get; set; }

    public virtual CkBranchEvaluation Evaluation { get; set; } = null!;

    public virtual CkEvaluationItem Item { get; set; } = null!;
}
