using Microsoft.EntityFrameworkCore;

namespace HR_App.Models.TopSoft;

public partial class TopSoftContext : DbContext
{
    public TopSoftContext()
    {
    }

    public TopSoftContext(DbContextOptions<TopSoftContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreaCode> AreaCodes { get; set; }

    public virtual DbSet<AttendanceDataCode> AttendanceDataCodes { get; set; }

    public virtual DbSet<BranchDatum> BranchData { get; set; }

    public virtual DbSet<CategoryCode> CategoryCodes { get; set; }

    public virtual DbSet<CkItem> CkItems { get; set; }

    public virtual DbSet<CkItemBarcode> CkItemBarcodes { get; set; }

    public virtual DbSet<CloseDay> CloseDays { get; set; }

    public virtual DbSet<CustomerCode> CustomerCodes { get; set; }

    public virtual DbSet<DriverCode> DriverCodes { get; set; }

    public virtual DbSet<DsalesCode> DsalesCodes { get; set; }

    public virtual DbSet<HsalesCode> HsalesCodes { get; set; }

    public virtual DbSet<ItemCode> ItemCodes { get; set; }

    public virtual DbSet<ItemTransactionsCode> ItemTransactionsCodes { get; set; }

    public virtual DbSet<ItemsPurchase> ItemsPurchases { get; set; }

    public virtual DbSet<ItemsforOrder> ItemsforOrders { get; set; }

    public virtual DbSet<MaintCompany> MaintCompanies { get; set; }

    public virtual DbSet<MaintExchangeItem> MaintExchangeItems { get; set; }

    public virtual DbSet<MaintOutSideMaintenance> MaintOutSideMaintenances { get; set; }

    public virtual DbSet<OrderedItemsPurchaseCode> OrderedItemsPurchaseCodes { get; set; }

    public virtual DbSet<RptUser> RptUsers { get; set; }

    public virtual DbSet<RptUsers2> RptUsers2s { get; set; }

    public virtual DbSet<Storeuser> Storeusers { get; set; }

    public virtual DbSet<StreetCode> StreetCodes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ZoneCode> ZoneCodes { get; set; }
    public virtual DbSet<It_Shift> It_Shifts { get; set; }
    public virtual DbSet<CkCoupon> CkCoupons { get; set; }
    public virtual DbSet<CkBranchEvaluation> CkBranchEvaluations { get; set; }
    public virtual DbSet<CkEvaluationDetail> CkEvaluationDetails { get; set; }

    public virtual DbSet<CkEvaluationItem> CkEvaluationItems { get; set; }
    public virtual DbSet<CkCouponsUser> CkCouponsUsers { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=150;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CkCouponsUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CK_Coupo__3214EC07F715CA8D");

            entity.ToTable("CK_Coupons_Users");

            entity.Property(e => e.Activatedby).HasMaxLength(50);
            entity.Property(e => e.CouponCode).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Terminal).HasMaxLength(20);
            entity.Property(e => e.CreatedDate)
                     .HasDefaultValueSql("(getdate())")
                     .HasColumnType("datetime");
        });
        modelBuilder.Entity<CkBranchEvaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CK_branc__3213E83FCDBAD2AB");

            entity.ToTable("CK_branch_evaluations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchName)
                .HasMaxLength(100)
                .HasColumnName("branch_Name");
            entity.Property(e => e.Createdby)
                .HasMaxLength(100)
                .HasColumnName("Createdby");
            entity.Property(e => e.BranchManager)
               .HasMaxLength(100)
               .HasColumnName("BranchManager");
            entity.Property(e => e.AreaManager)
               .HasMaxLength(100)
               .HasColumnName("AreaManager");
            entity.Property(e => e.Shift)
               .HasMaxLength(100)
               .HasColumnName("Shift");
            entity.Property(e => e.EvaluationDate).HasColumnName("evaluation_date");
            entity.Property(e => e.TotalScore)
                .HasDefaultValue(0)
                .HasColumnName("total_score");
        });

        modelBuilder.Entity<CkEvaluationDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CK_evalu__3213E83FA48B95EA");

            entity.ToTable("CK_evaluation_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.NotScore).HasColumnName("NotScore");
            entity.Property(e => e.NotAvailable).HasColumnName("NotAvailable");
            entity.Property(e => e.Percentage).HasColumnName("Percentage");
            entity.Property(e => e.Comment)
                .HasMaxLength(255)
                .HasColumnName("Comment");
            entity.Property(e => e.Notes)
                .HasMaxLength(255)
                .HasColumnName("Notes");
            entity.HasOne(d => d.Evaluation).WithMany(p => p.CkEvaluationDetails)
                .HasForeignKey(d => d.EvaluationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CK_evalua__evalu__26074FDC");

            entity.HasOne(d => d.Item).WithMany(p => p.CkEvaluationDetails)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CK_evalua__item___26FB7415");
        });

        modelBuilder.Entity<CkEvaluationItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CK_evalu__3213E83F10F15433");

            entity.ToTable("CK_evaluation_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.ItemText).HasColumnName("item_text");
            entity.Property(e => e.SectionName)
                .HasMaxLength(200)
                .HasColumnName("section_name");
        }); modelBuilder.Entity<CkCoupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CK_Coupo__3214EC07DF5D2977");

            entity.ToTable("CK_Coupons");

            entity.HasIndex(e => e.CouponCode, "UQ__CK_Coupo__D34908007C9ABF65").IsUnique();

            entity.Property(e => e.CouponCode).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100); 
            entity.Property(e => e.PhoneNumber).HasMaxLength(20); 
            entity.Property(e => e.Terminal).HasMaxLength(20);
            entity.Property(e => e.Activatedby).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastUsedDate).HasColumnType("datetime");
            entity.Property(e => e.Notes).HasMaxLength(250);
        });
        modelBuilder.Entity<It_Shift>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__It_Shift__1A00E092E99C6070");
            entity.ToTable("It_Shift");
            entity.Property(e => e.Transdate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AreaCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__AreaCode__1A00E0921A3F1947");

            entity.ToTable("AreaCode");

            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.ZoneSerialNavigation).WithMany(p => p.AreaCodes)
                .HasForeignKey(d => d.ZoneSerial)
                .HasConstraintName("FK_AreaCode_ZoneCode");
        });

        modelBuilder.Entity<AttendanceDataCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__Attendan__1A00E092486E178E");

            entity.ToTable("AttendanceDataCode");

            entity.Property(e => e.BranchCode).HasMaxLength(30);
            entity.Property(e => e.EventType).HasMaxLength(255);
            entity.Property(e => e.InsertedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LoginDate).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(255);
        });

        modelBuilder.Entity<BranchDatum>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__BranchDa__1A00E092FEBD7D83");

            entity.Property(e => e.BranchIdD).HasMaxLength(20);
            entity.Property(e => e.BranchIdR).HasMaxLength(20);
            entity.Property(e => e.BranchName).HasMaxLength(255);
        });

        modelBuilder.Entity<CategoryCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__Category__1A00E092F360CF3D");

            entity.ToTable("CategoryCode");

            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<CkItem>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__CK_Items__727E838BC264D9C8");

            entity.ToTable("CK_Items");

            entity.Property(e => e.DpName).HasMaxLength(100);
            entity.Property(e => e.EtaCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ETA_code");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ItemName).HasMaxLength(100);
            entity.Property(e => e.SubCategory).HasMaxLength(100);
        });

        modelBuilder.Entity<CkItemBarcode>(entity =>
        {
            entity.HasKey(e => e.BarcodeId).HasName("PK__CK_ItemB__21916CA84E009172");

            entity.ToTable("CK_ItemBarcodes");

            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.ItemLookupCode).HasMaxLength(50);
            entity.Property(e => e.ItemLookupCodeTrim)
                .HasMaxLength(50)
                .HasComputedColumnSql("(ltrim(rtrim([ItemLookupCode])))", true)
                .HasColumnName("ItemLookupCode_Trim");
        });

        modelBuilder.Entity<CloseDay>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CloseDay");

            entity.Property(e => e.Alert).HasMaxLength(500);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<CustomerCode>(entity =>
        {
            entity.HasKey(e => e.CustomerCode1).HasName("PK__Customer__0667852000A90BC8");

            entity.ToTable("CustomerCode");

            entity.Property(e => e.CustomerCode1).HasColumnName("CustomerCode");
            entity.Property(e => e.Address1).HasMaxLength(255);
            entity.Property(e => e.Address2).HasMaxLength(255);
            entity.Property(e => e.CustomerName).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Phone1).HasMaxLength(20);
            entity.Property(e => e.Phone2).HasMaxLength(20);
            entity.Property(e => e.Phone3).HasMaxLength(20);
        });

        modelBuilder.Entity<DriverCode>(entity =>
        {
            entity.HasKey(e => e.DriverCode1).HasName("PK__DriverCo__0BF84B4672B3B9D1");

            entity.ToTable("DriverCode");

            entity.Property(e => e.DriverCode1).HasColumnName("DriverCode");
            entity.Property(e => e.Address1).HasMaxLength(255);
            entity.Property(e => e.Address2).HasMaxLength(255);
            entity.Property(e => e.DriverName).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Phone1).HasMaxLength(20);
            entity.Property(e => e.Phone2).HasMaxLength(20);
            entity.Property(e => e.Phone3).HasMaxLength(20);
        });

        modelBuilder.Entity<DsalesCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__DSalesCo__1A00E092097C51A5");

            entity.ToTable("DSalesCode");

            entity.Property(e => e.CategoryName).HasMaxLength(255);
            entity.Property(e => e.ItemCode).HasMaxLength(100);

            entity.HasOne(d => d.SalesCodeNavigation).WithMany(p => p.DsalesCodes)
                .HasForeignKey(d => d.SalesCode)
                .HasConstraintName("FK_DSalesCode_SalesCode");
        });

        modelBuilder.Entity<HsalesCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__HSalesCo__1A00E092667BB3BD");

            entity.ToTable("HSalesCode");

            entity.Property(e => e.BranchCode).HasMaxLength(100);
            entity.Property(e => e.CancelBy).HasMaxLength(100);
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Createddatetime).HasColumnType("datetime");
            entity.Property(e => e.CustomerCode).HasMaxLength(100);
            entity.Property(e => e.Deliverytime).HasMaxLength(10);
            entity.Property(e => e.Message).HasMaxLength(300);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.SalesOrderDate).HasColumnType("datetime");
            entity.Property(e => e.Updateddatetime).HasColumnType("datetime");
        });

        modelBuilder.Entity<ItemCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__ItemCode__1A00E092097DC006");

            entity.ToTable("ItemCode");

            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SerialNumber).HasMaxLength(80);
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<ItemTransactionsCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__ItemTran__1A00E0928AD5FC3A");

            entity.ToTable("ItemTransactionsCode");

            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Problem).HasMaxLength(255);
            entity.Property(e => e.TransDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Updatedby).HasMaxLength(255);
        });

        modelBuilder.Entity<ItemsPurchase>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__ItemsPur__1A00E0924CE83AB0");

            entity.ToTable("ItemsPurchase");

            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Company).HasMaxLength(100);
            entity.Property(e => e.ItemName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(100);
            entity.Property(e => e.Warehouse).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ItemsforOrder>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__Itemsfor__1A00E0925D4E615F");

            entity.ToTable("ItemsforOrder");

            entity.Property(e => e.ItemLookupCode).HasMaxLength(50);
            entity.Property(e => e.ItemName).HasMaxLength(255);
        });

        modelBuilder.Entity<MaintCompany>(entity =>
		{
			entity.HasKey(e => e.Serial).HasName("PK__Maint_Company__7w7E838BC264D9C4");

			entity.ToTable("Maint_Company");

			entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.Serial).ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<MaintExchangeItem>(entity =>
        {
			entity.HasKey(e => e.Serial).HasName("PK__Maint_ExchangeItems__737E838BC264D9C4");
			entity.ToTable("Maint_ExchangeItems");

			entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.Serial).ValueGeneratedOnAdd();
            entity.Property(e => e.TransDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<MaintOutSideMaintenance>(entity =>
        {
			entity.HasKey(e => e.Serial).HasName("PK__Maint_OutSideMaintenance__787E838BC264D9C4");
			entity.ToTable("Maint_OutSideMaintenance");

			entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.Problem).HasMaxLength(200);
            entity.Property(e => e.Serial).ValueGeneratedOnAdd();
            entity.Property(e => e.TransDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
        });
        modelBuilder.Entity<OrderedItemsPurchaseCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__OrderedI__1A00E0924DB8A3B6");

            entity.ToTable("OrderedItemsPurchaseCode");

            entity.Property(e => e.BranchName).HasMaxLength(255);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Createdby).HasMaxLength(255);
            entity.Property(e => e.ItemLookupCode).HasMaxLength(50);
            entity.Property(e => e.ItemName).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.Reply).HasMaxLength(255);
            entity.Property(e => e.TransactionDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<RptUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("RptUsers");

            entity.Property(e => e.Category).HasMaxLength(255);
            entity.Property(e => e.CloudId).HasMaxLength(100);
            entity.Property(e => e.Company).HasMaxLength(255);
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.District)
                .HasMaxLength(255)
                .HasColumnName("district");
            entity.Property(e => e.Dmanager)
                .HasMaxLength(255)
                .HasColumnName("DManager");
            entity.Property(e => e.Fmanager)
                .HasMaxLength(255)
                .HasColumnName("FManager");
            entity.Property(e => e.FranchiseTmt).HasColumnName("FranchiseTMT");
            entity.Property(e => e.Inventlocation).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RmsstoNumber)
                .HasMaxLength(255)
                .HasColumnName("RMSstoNumber");
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.Server).HasMaxLength(255);
            entity.Property(e => e.StoreIdcloud).HasMaxLength(100);
            entity.Property(e => e.Storenumber)
                .HasMaxLength(255)
                .HasColumnName("storenumber");
            entity.Property(e => e.Username).HasMaxLength(255);
            entity.Property(e => e.Username2).HasMaxLength(255);
        });

        modelBuilder.Entity<RptUsers2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("RptUsers2");

            entity.Property(e => e.Category).HasMaxLength(255);
            entity.Property(e => e.Department).HasMaxLength(255);
            entity.Property(e => e.Dmanager)
                .HasMaxLength(255)
                .HasColumnName("DManager");
            entity.Property(e => e.Fmanager)
                .HasMaxLength(255)
                .HasColumnName("FManager");
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.Server).HasMaxLength(255);
            entity.Property(e => e.Storenumber)
                .HasMaxLength(255)
                .HasColumnName("storenumber");
            entity.Property(e => e.Username).HasMaxLength(255);
        });

        modelBuilder.Entity<Storeuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Storeuse__7CC3769E56C18B49");

            entity.ToTable("Storeuser", tb => tb.HasTrigger("trg_StoreUser_SyncVersion"));

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ArabicN).HasMaxLength(255);
            entity.Property(e => e.AssignedEmployee).HasMaxLength(255);
            entity.Property(e => e.BranchOwner).HasMaxLength(255);
            entity.Property(e => e.CloudId).HasMaxLength(100);
            entity.Property(e => e.Company).HasMaxLength(255);
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Dbase).HasMaxLength(255);
            entity.Property(e => e.District).HasMaxLength(255);
            entity.Property(e => e.Dmanager)
                .HasMaxLength(255)
                .HasColumnName("DManager");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Fmanager)
                .HasMaxLength(255)
                .HasColumnName("FManager");
            entity.Property(e => e.Franchise).HasMaxLength(50);
            entity.Property(e => e.FranchiseTmt).HasColumnName("FranchiseTMT");
            entity.Property(e => e.Inventlocation).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PriceCategory).HasMaxLength(255);
            entity.Property(e => e.RmsstoNumber)
                .HasMaxLength(255)
                .HasColumnName("RMSstoNumber");
            entity.Property(e => e.Server).HasMaxLength(255);
            entity.Property(e => e.StartDate).HasMaxLength(50);
            entity.Property(e => e.StoreIdcloud).HasMaxLength(100);
            entity.Property(e => e.Storenumber)
                .HasMaxLength(255)
                .HasColumnName("storenumber");
            entity.Property(e => e.SyncVersion).HasDefaultValue(1L);
            entity.Property(e => e.TempCloudId).HasMaxLength(50);
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(255);
            entity.Property(e => e.Zkip)
                .HasMaxLength(255)
                .HasColumnName("ZKIP");
        });

        modelBuilder.Entity<StreetCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__StreetCo__1A00E092EB5C39B7");

            entity.ToTable("StreetCode");

            entity.Property(e => e.DeliveryTime).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.AreaSerialNavigation).WithMany(p => p.StreetCodes)
                .HasForeignKey(d => d.AreaSerial)
                .HasConstraintName("FK_StreetCode_AreaCode");

            entity.HasOne(d => d.BranchSerialNavigation).WithMany(p => p.StreetCodes)
                .HasForeignKey(d => d.BranchSerial)
                .HasConstraintName("FK_StreetCode_BranchSerial");

            entity.HasOne(d => d.ZoneSerialNavigation).WithMany(p => p.StreetCodes)
                .HasForeignKey(d => d.ZoneSerial)
                .HasConstraintName("FK_StreetCode_ZoneCode");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC273689BB1B");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");
            entity.Property(e => e.User1)
                .HasMaxLength(100)
                .HasColumnName("User");
        });

        modelBuilder.Entity<ZoneCode>(entity =>
        {
            entity.HasKey(e => e.Serial).HasName("PK__ZoneCode__1A00E092E99C6070");

            entity.ToTable("ZoneCode");

            entity.Property(e => e.Name).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
