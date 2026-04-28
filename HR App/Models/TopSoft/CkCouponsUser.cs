using System;
using System.Collections.Generic;

namespace HR_App.Models.TopSoft;

public partial class CkCouponsUser
{
    public int Id { get; set; }

    public string CouponCode { get; set; } = null!;

    public string? Notes { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Terminal { get; set; }

    public string? Activatedby { get; set; }
    public DateTime CreatedDate { get; set; }
}
