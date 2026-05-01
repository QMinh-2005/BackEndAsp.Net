using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyOwnLearning.Models;

namespace MyOwnLearning.Data;

public partial class WebBadmintonContext : DbContext
{
    public WebBadmintonContext()
    {
    }

    public WebBadmintonContext(DbContextOptions<WebBadmintonContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Function> Functions { get; set; }

    public virtual DbSet<Module> Modules { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductDetail> ProductDetails { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductSpecification> ProductSpecifications { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleModuleFunction> RoleModuleFunctions { get; set; }

    public virtual DbSet<ServiceTicket> ServiceTickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherCondition> VoucherConditions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-S80KR1F\\SQLEXPRESS;Initial Catalog=Web_Badminton;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Brands__DAD4F3BED4509BC8");

            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__51BCD7971AE47B99");

            entity.HasIndex(e => e.UserId, "UQ__Carts__1788CCAD17392792").IsUnique();

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .HasConstraintName("FK_Carts_Users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B2A099D0A59");

            entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Carts");

            entity.HasOne(d => d.Detail).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.DetailId)
                .HasConstraintName("FK_CartItems_ProductDetails");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2BD1817004");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Function>(entity =>
        {
            entity.HasKey(e => e.FunctionId).HasName("PK__Function__31ABF918BA5610F5");

            entity.Property(e => e.FunctionId).HasColumnName("FunctionID");
            entity.Property(e => e.FunctionName).HasMaxLength(50);
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.ModuleId).HasName("PK__Modules__2B7477871134FFAF");

            entity.Property(e => e.ModuleId).HasColumnName("ModuleID");
            entity.Property(e => e.ModuleName).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFA6FD3200");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatusId)
                .HasDefaultValue(1)
                .HasColumnName("OrderStatusID");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ShippingAddress).HasMaxLength(255);
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OrderStatusId)
                .HasConstraintName("FK_Orders_OrderStatuses");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C4E2C313E");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.IsStringingService).HasDefaultValue(false);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.StringBrand).HasMaxLength(100);
            entity.Property(e => e.StringerId).HasColumnName("StringerID");
            entity.Property(e => e.TensionKg)
                .HasColumnType("decimal(4, 1)")
                .HasColumnName("TensionKG");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Detail).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.DetailId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Order_ProductDetails");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__797309D9");

            entity.HasOne(d => d.Stringer).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.StringerId)
                .HasConstraintName("FK__OrderDeta__Strin__7E37BEF6");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("PK__OrderSta__BC674F41592CF9EB");

            entity.Property(e => e.OrderStatusId).HasColumnName("OrderStatusID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A5814B653F8");

            entity.HasIndex(e => e.OrderId, "UQ__Payments__C3905BAEEAFDEDDA").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("TransactionID");

            entity.HasOne(d => d.Order).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.OrderId)
                .HasConstraintName("FK__Payments__OrderI__4D5F7D71");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6ED2D00FCFA");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BrandId).HasColumnName("BrandID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.DiscountPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(250);
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Products_Brands");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<ProductDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__ProductD__135C314DBD1D0E93");

            entity.HasIndex(e => e.SerialNumber, "UQ__ProductD__048A0008441FC351").IsUnique();

            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.BalancePoint).HasMaxLength(50);
            entity.Property(e => e.GripSize).HasMaxLength(20);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Stiffness).HasMaxLength(50);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.WeightClass).HasMaxLength(20);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductDetail_Product");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F4EC7520F48D");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ProductIm__Produ__67DE6983");
        });

        modelBuilder.Entity<ProductSpecification>(entity =>
        {
            entity.HasKey(e => e.SpecId).HasName("PK__ProductS__883D519BEDCD67CC");

            entity.Property(e => e.SpecId).HasColumnName("SpecID");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SpecName).HasMaxLength(100);
            entity.Property(e => e.SpecValue).HasMaxLength(250);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecifications)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ProductSp__Produ__6BAEFA67");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79AE2AE4E682");

            entity.HasIndex(e => e.OrderDetailId, "UQ__Reviews__D3B9D30D7512D994").IsUnique();

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("ImageURL");
            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.OrderDetail).WithOne(p => p.Review)
                .HasForeignKey<Review>(d => d.OrderDetailId)
                .HasConstraintName("FK__Reviews__OrderDe__2BFE89A6");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A000E793E");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616009272DA6").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<RoleModuleFunction>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.ModuleId, e.FunctionId }).HasName("PK__RoleModu__F17C22BBEAAD9CE9");

            entity.ToTable("RoleModuleFunction");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.ModuleId).HasColumnName("ModuleID");
            entity.Property(e => e.FunctionId).HasColumnName("FunctionID");

            entity.HasOne(d => d.Function).WithMany(p => p.RoleModuleFunctions)
                .HasForeignKey(d => d.FunctionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RoleModul__Funct__719CDDE7");

            entity.HasOne(d => d.Module).WithMany(p => p.RoleModuleFunctions)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RoleModul__Modul__70A8B9AE");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleModuleFunctions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RoleModul__RoleI__6FB49575");
        });

        modelBuilder.Entity<ServiceTicket>(entity =>
        {
            entity.HasKey(e => e.ServiceTicketId).HasName("PK__ServiceT__3BB0FE64FD4322CA");

            entity.Property(e => e.ServiceTicketId).HasColumnName("ServiceTicketID");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CustomerRacketName).HasMaxLength(250);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ReceivedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Đang chờ");

            entity.HasOne(d => d.Order).WithMany(p => p.ServiceTickets)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__ServiceTi__Order__06CD04F7");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC65C17C1F");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__690797E6"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__681373AD"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF27604F723E625F");
                        j.ToTable("UserRoles");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE79C19C5585F2");

            entity.HasIndex(e => e.VoucherCode, "UQ__Vouchers__7F0ABCA916502AC9").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsGlobal).HasDefaultValue(true);
            entity.Property(e => e.IsPercent).HasDefaultValue(false);
            entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.VoucherCode).HasMaxLength(50);
        });

        modelBuilder.Entity<VoucherCondition>(entity =>
        {
            entity.HasKey(e => e.ConditionId).HasName("PK__VoucherC__37F5C0EF86653ECD");

            entity.Property(e => e.ConditionId).HasColumnName("ConditionID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");

            entity.HasOne(d => d.Category).WithMany(p => p.VoucherConditions)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__VoucherCo__Categ__3D2915A8");

            entity.HasOne(d => d.Product).WithMany(p => p.VoucherConditions)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__VoucherCo__Produ__3C34F16F");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherConditions)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK__VoucherCo__Vouch__3B40CD36");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
