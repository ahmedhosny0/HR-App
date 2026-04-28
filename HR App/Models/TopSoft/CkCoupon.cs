using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_App.Models.TopSoft;

public partial class CkCoupon
{
    public int Id { get; set; }

    public string CouponCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastUsedDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }
    
    public string? PhoneNumber { get; set; }
    public string? Terminal { get; set; }
    public string? Activatedby { get; set; }

    public string? Notes { get; set; }
    public int NumofDays { get; set; }
    [NotMapped]
    public bool CanUse
    {
        get
        {
            if (!IsActive)
                return false;

            if (!LastUsedDate.HasValue)
                return true;

            return LastUsedDate.Value.AddDays(7) <= DateTime.Now;
        }
    }
}
