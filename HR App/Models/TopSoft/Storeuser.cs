using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HR_App.Models.TopSoft;

public partial class Storeuser
{
    public string? Inventlocation { get; set; } = null!;
    [Required]
    public string? Storenumber { get; set; } 
    [Required]
    public string? Username { get; set; } 

    public string? Password { get; set; } = null!;

    public string? Name { get; set; } = null!;

    public string? Server { get; set; } = null!;

    [Required]
    public string? RmsstoNumber { get; set; } 
    
    public string? Email { get; set; } = null!;

    public string? Dbase { get; set; } = null!;

    [Required]
    public string? PriceCategory { get; set; } 

    [Required]
    public string? Franchise { get; set; } 

    [Required]
    public string? Company { get; set; } 

    public string? Zkip { get; set; } = null!;

    public string? StartDate { get; set; } = null!;

    public string? ArabicN { get; set; } = null!;

    [Required]
    public string? District { get; set; } 

    public string? Dmanager { get; set; } = null!;

    public string? Fmanager { get; set; } = null!;

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; } 

    public int? BranchStatus { get; set; } 

    public string? BranchOwner { get; set; } = null!;

    public int? Delivery { get; set; } 

    public int? InstaShop { get; set; } 

    public int? Talabat { get; set; } 

    public int? HasAttendance { get; set; } 
    public int? IsEvent { get; set; } 

    public string? CloudId { get; set; } = null!;

    public int? Connection { get; set; } 

    public int? StoreOrder { get; set; } 

    public int? FranchiseTmt { get; set; } 

    public string? StoreIdcloud { get; set; } = null!;

    public string? AssignedEmployee { get; set; } = null!;

    public int? ItemOrder { get; set; } 

    public string? TempCloudId { get; set; } = null!;

    public long? SyncVersion { get; set; } 

    public int? Id { get; set; } 
    [NotMapped]
    public string? DecryptedPassword { get; set; } = null!;
}
