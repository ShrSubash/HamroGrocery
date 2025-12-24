using HamroGrocery.Models;
using HamroGrocery.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace HamroGrocery.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Country> Countries { get; set; }
        public DbSet<StateProvince> StateProvinces { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        // Ecommerce entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ProductVariation> ProductVariations { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<ShippingProvider> ShippingProviders { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<TrackingEvent> TrackingEvents { get; set; }
        public DbSet<ProductSpecial> ProductSpecials { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<LoyaltySettings> LoyaltySettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==========================
            // CRITICAL: Fix ALL shadow properties FIRST
            // ==========================

            // 1. Fix User entity shadow properties
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.Role).HasConversion<int>();
                entity.Ignore(u => u.FullName);

                // EXPLICIT foreign key property mapping
                entity.Property(u => u.CountryId).HasColumnName("CountryId");
                entity.Property(u => u.StateProvinceId).HasColumnName("StateProvinceId");
                entity.Property(u => u.CityId).HasColumnName("CityId");

                // EXPLICIT relationship configuration with HasForeignKey
                entity.HasOne(u => u.Country)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CountryId)
                    .HasConstraintName("FK_User_Country")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.StateProvince)
                    .WithMany(s => s.Users)
                    .HasForeignKey(u => u.StateProvinceId)
                    .HasConstraintName("FK_User_StateProvince")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.City)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CityId)
                    .HasConstraintName("FK_User_City")
                    .OnDelete(DeleteBehavior.SetNull);
            });
            builder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Order)
                    .WithMany()
                    .HasForeignKey(r => r.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(r => new { r.ProductId, r.UserId });
            });

            // NEW: Configure Complaint relationships
            builder.Entity<Complaint>(entity =>
            {
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Complaints)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Order)
                    .WithMany()
                    .HasForeignKey(c => c.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.ResolvedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.ResolvedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.CreatedAt);
            });

            // NEW: Ensure only one LoyaltySettings record
           
            // 2. Fix OrderItem shadow properties
            builder.Entity<OrderItem>(entity =>
            {
                // EXPLICIT foreign key property mapping
                entity.Property(oi => oi.OrderId).HasColumnName("OrderId");
                entity.Property(oi => oi.ProductId).HasColumnName("ProductId");
                entity.Property(oi => oi.ProductVariationId).HasColumnName("ProductVariationId");

                // EXPLICIT relationship configuration with HasForeignKey
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .HasConstraintName("FK_OrderItem_Order")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .HasConstraintName("FK_OrderItem_Product")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(oi => oi.ProductVariation)
                    .WithMany(pv => pv.OrderItems)
                    .HasForeignKey(oi => oi.ProductVariationId)
                    .HasConstraintName("FK_OrderItem_ProductVariation")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // 3. Fix PurchaseOrderItem shadow properties
            builder.Entity<PurchaseOrderItem>(entity =>
            {
                // EXPLICIT foreign key property mapping
                entity.Property(poi => poi.PurchaseOrderId).HasColumnName("PurchaseOrderId");
                entity.Property(poi => poi.ProductVariationId).HasColumnName("ProductVariationId");

                // EXPLICIT relationship configuration with HasForeignKey
                entity.HasOne(poi => poi.PurchaseOrder)
                    .WithMany(po => po.Items)
                    .HasForeignKey(poi => poi.PurchaseOrderId)
                    .HasConstraintName("FK_PurchaseOrderItem_PurchaseOrder")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(poi => poi.ProductVariation)
                    .WithMany(pv => pv.PurchaseOrderItems)
                    .HasForeignKey(poi => poi.ProductVariationId)
                    .HasConstraintName("FK_PurchaseOrderItem_ProductVariation")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Fix Shipment shadow properties (THIS WAS MISSING!)
            builder.Entity<Shipment>(entity =>
            {
                // This defines a TRUE one-to-one relationship
                entity.HasOne(s => s.Order)
                      .WithOne(o => o.Shipment)
                      .HasForeignKey<Shipment>(s => s.OrderId)   // This tells EF: use this property as FK
                      .OnDelete(DeleteBehavior.Restrict)         // or Cascade if you want
                      .HasConstraintName("FK_Shipment_Order");

                // Optional: make OrderId required (shipment must belong to an order)
                entity.Property(s => s.OrderId).IsRequired();

                // ShippingProvider relationship (one-to-many)
                entity.HasOne(s => s.ShippingProvider)
                      .WithMany(sp => sp.Shipments)
                      .HasForeignKey(s => s.ShippingProviderId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Shipment_ShippingProvider");
            });

            // 5. Configure Announcement
            builder.Entity<Announcement>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);

                // Index for performance
                entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.StartDate, e.EndDate });
            });

            // ==========================
            // Configure Enhanced Checkout Fields
            // ==========================
            ConfigureCheckoutFields(builder);

            // ==========================
            // Continue with other configurations
            // ==========================
            ConfigureBaseEntityProperties(builder);
            ConfigureRelationships(builder);
            ConfigureDecimalPrecision(builder);
            ConfigureTableNames(builder);
           
        }

        /// <summary>
        /// Configure new checkout fields and indexes (MySQL compatible)
        /// </summary>
        private void ConfigureCheckoutFields(ModelBuilder builder)
        {
            // ============= ORDER CONFIGURATION =============
            builder.Entity<Order>(entity =>
            {
                // Add indexes for new checkout fields
                entity.HasIndex(o => o.OrderNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Order_OrderNumber");

                entity.HasIndex(o => o.PaymentStatus)
                    .HasDatabaseName("IX_Order_PaymentStatus");

                entity.HasIndex(o => o.StripeSessionId)
                    .HasDatabaseName("IX_Order_StripeSessionId");

                entity.HasIndex(o => o.StripePaymentIntentId)
                    .HasDatabaseName("IX_Order_StripePaymentIntentId");

                entity.HasIndex(o => o.OrderDate)
                    .HasDatabaseName("IX_Order_OrderDate");

                entity.HasIndex(o => o.Status)
                    .HasDatabaseName("IX_Order_Status");

                entity.HasIndex(o => o.TrackingNumber)
                    .HasDatabaseName("IX_Order_TrackingNumber");

                // Configure string lengths
                entity.Property(o => o.CustomerName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(o => o.CustomerEmail)
                    .HasMaxLength(100);

                entity.Property(o => o.CustomerPhone)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(o => o.DeliveryMethod)
                    .HasMaxLength(50)
                    .IsRequired()
                    .HasDefaultValue("Delivery");

                entity.Property(o => o.PaymentMethod)
                    .HasMaxLength(50)
                    .IsRequired()
                    .HasDefaultValue("Online Payment");

                entity.Property(o => o.PaymentStatus)
                    .HasMaxLength(50)
                    .IsRequired()
                    .HasDefaultValue("Pending");

                entity.Property(o => o.ShippingAddress)
                    .HasMaxLength(500);

                entity.Property(o => o.ShippingSuburb)
                    .HasMaxLength(100);

                entity.Property(o => o.ShippingCity)
                    .HasMaxLength(100);

                entity.Property(o => o.ShippingState)
                    .HasMaxLength(50);

                entity.Property(o => o.ShippingPostcode)
                    .HasMaxLength(10);

                entity.Property(o => o.ShippingPhone)
                    .HasMaxLength(20);

                entity.Property(o => o.DeliveryInstructions)
                    .HasMaxLength(500);

                entity.Property(o => o.PickupLocation)
                    .HasMaxLength(200);

                entity.Property(o => o.StripeSessionId)
                    .HasMaxLength(100);

                entity.Property(o => o.StripePaymentIntentId)
                    .HasMaxLength(100);

                entity.Property(o => o.Notes)
                    .HasMaxLength(500);

                entity.Property(o => o.CancellationReason)
                    .HasMaxLength(500);
            });

            // ============= ORDER ITEM CONFIGURATION =============
            builder.Entity<OrderItem>(entity =>
            {
                // Configure new fields
                entity.Property(oi => oi.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(oi => oi.SKU)
                    .HasMaxLength(50);

                entity.Property(oi => oi.VariationDetails)
                    .HasMaxLength(100);

                entity.Property(oi => oi.CategoryName)
                    .HasMaxLength(100);
            });
        }

        private void ConfigureBaseEntityProperties(ModelBuilder builder)
        {
            // Configure each entity explicitly to avoid shadow property issues

            builder.Entity<Country>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Cart>(entity =>
            {
                entity.Property(c => c.GuestInfoJson)
                    .HasColumnType("json")  // MySQL JSON type
                    .HasMaxLength(4000)     // Adjust if needed (MySQL JSON max ~1GB, but safe limit)
                    .IsRequired(false);     // Optional for guests
            });
            builder.Entity<StateProvince>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<City>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Category>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Product>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<ProductVariation>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Order>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<StockMovement>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Supplier>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<PurchaseOrder>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<PurchaseOrderItem>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<ShippingProvider>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<Shipment>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<TrackingEvent>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });

            builder.Entity<ProductSpecial>(entity =>
            {
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("TINYINT(1)").HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasColumnType("TINYINT(1)").HasDefaultValue(false);
            });
        }

        private void ConfigureTableNames(ModelBuilder builder)
        {
            builder.Entity<Country>().ToTable("countries");
            builder.Entity<StateProvince>().ToTable("state_provinces");
            builder.Entity<City>().ToTable("cities");
            builder.Entity<Category>().ToTable("categories");
            builder.Entity<Product>().ToTable("products");
            builder.Entity<ProductVariation>().ToTable("product_variations");
            builder.Entity<Order>().ToTable("orders");
            builder.Entity<OrderItem>().ToTable("order_items");
            builder.Entity<StockMovement>().ToTable("stock_movements");
            builder.Entity<Supplier>().ToTable("suppliers");
            builder.Entity<PurchaseOrder>().ToTable("purchase_orders");
            builder.Entity<PurchaseOrderItem>().ToTable("purchase_order_items");
            builder.Entity<ShippingProvider>().ToTable("shipping_providers");
            builder.Entity<Shipment>().ToTable("shipments");
            builder.Entity<TrackingEvent>().ToTable("tracking_events");
            builder.Entity<ProductSpecial>().ToTable("product_specials");
            builder.Entity<Announcement>().ToTable("announcements");

            // Configure Identity tables
            builder.Entity<User>().ToTable("users");
            builder.Entity<IdentityRole<int>>().ToTable("roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("user_roles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("user_claims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("user_logins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("role_claims");
            builder.Entity<IdentityUserToken<int>>().ToTable("user_tokens");
        }

        private void ConfigureRelationships(ModelBuilder builder)
        {
            // State/Province and City relationships
            builder.Entity<StateProvince>()
                .HasOne(s => s.Country)
                .WithMany(c => c.StateProvinces)
                .HasForeignKey(s => s.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<City>()
                .HasOne(c => c.StateProvince)
                .WithMany(s => s.Cities)
                .HasForeignKey(c => c.StateProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<City>()
                .HasOne(c => c.Country)
                .WithMany()
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product relationships
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductVariation>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariations)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order relationships (NOTE: CustomerId relationship configured explicitly in OnModelCreating)
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Stock and Purchase Order relationships
            builder.Entity<StockMovement>()
                .HasOne(sm => sm.ProductVariation)
                .WithMany(pv => pv.StockMovements)
                .HasForeignKey(sm => sm.ProductVariationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product Special relationships
            builder.Entity<ProductSpecial>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSpecials)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tracking Event relationships
            builder.Entity<TrackingEvent>()
                .HasOne(te => te.Shipment)
                .WithMany(s => s.TrackingEvents)
                .HasForeignKey(te => te.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // NOTE: OrderItem, PurchaseOrderItem, Shipment, and User relationships
            // are explicitly configured at the top of OnModelCreating to prevent shadow properties
        }

        private void ConfigureDecimalPrecision(ModelBuilder builder)
        {
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.ComparePrice).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.Weight).HasPrecision(18, 2);

            builder.Entity<ProductVariation>().Property(pv => pv.Price).HasPrecision(18, 2);
            builder.Entity<ProductVariation>().Property(pv => pv.Weight).HasPrecision(18, 2);

            builder.Entity<Order>().Property(o => o.SubTotal).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.TaxAmount).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.ShippingAmount).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);

            builder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.TotalPrice).HasPrecision(18, 2);

            builder.Entity<PurchaseOrder>().Property(po => po.TotalAmount).HasPrecision(18, 2);
            builder.Entity<PurchaseOrderItem>().Property(poi => poi.UnitPrice).HasPrecision(18, 2);

            builder.Entity<ProductSpecial>().Property(ps => ps.DiscountValue).HasPrecision(18, 2);
            builder.Entity<ShippingProvider>().Property(sp => sp.BaseRate).HasPrecision(18, 2);
        }


    }
}