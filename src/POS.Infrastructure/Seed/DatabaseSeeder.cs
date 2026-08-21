using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Infrastructure.MongoDB;
using POS.Infrastructure.Security;

namespace POS.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetService<ILogger<MongoDbContext>>();

        // 1. Initialize Indexes
        await MongoDbIndexInitializer.InitializeIndexesAsync(dbContext);

        // 2. Seed Business Settings
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IRepository<BusinessSettings>>();
        var settings = await settingsRepo.GetAllAsync();
        if (settings.Count == 0)
        {
            await settingsRepo.AddAsync(new BusinessSettings
            {
                StoreName = "NexPOS Retail Store",
                Tagline = "Fast, Reliable & Modern Point of Sale",
                Address = "100 Commercial Blvd, Retail Hub",
                Phone = "+1 (800) 555-0199",
                Email = "info@nexpos.local",
                Website = "https://nexpos.local",
                CurrencySymbol = "৳",
                TaxRatePercentage = 5.0m,
                InvoicePrefix = "INV-",
                NextInvoiceNumber = 1001,
                DefaultDiscountPercentage = 0.0m,
                MaxWorkerDiscountPercentage = 5.0m,
                LowStockAlertThreshold = 5,
                ReceiptHeaderNote = "Thank you for shopping with us!",
                ReceiptFooterNote = "Items can be exchanged within 7 days with original invoice.",
                ThermalPaperWidthMm = 80,
                AutoPrintInvoice = true
            });
        }

        // 3. Seed Users (Employer: admin / ChangeMe123!, Worker: worker / ChangeMe123!)
        var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var admin = await userRepo.FindOneAsync(u => u.Username == "admin");
        if (admin == null)
        {
            await userRepo.AddAsync(new User
            {
                Username = "admin",
                Email = "admin@nexpos.local",
                FullName = "System Administrator (Employer)",
                Phone = "+1 555-0100",
                PasswordHash = passwordHasher.HashPassword("ChangeMe123!"),
                Role = Roles.Employer,
                Permissions = Permissions.EmployerDefaultPermissions.ToList(),
                MaxDiscountPercentage = 100.0m,
                IsActive = true,
                MustChangePassword = true
            });
        }

        var worker = await userRepo.FindOneAsync(u => u.Username == "worker");
        if (worker == null)
        {
            await userRepo.AddAsync(new User
            {
                Username = "worker",
                Email = "worker@nexpos.local",
                FullName = "John Doe (Cashier)",
                Phone = "+1 555-0200",
                PasswordHash = passwordHasher.HashPassword("ChangeMe123!"),
                Role = Roles.Worker,
                Permissions = Permissions.WorkerDefaultPermissions.ToList(),
                MaxDiscountPercentage = 5.0m,
                IsActive = true,
                MustChangePassword = true
            });
        }

        // 4. Seed Categories
        var categoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Category>>();
        var existingCategories = await categoryRepo.GetAllAsync();
        var categoryMap = new Dictionary<string, string>();

        if (existingCategories.Count == 0)
        {
            var seedCats = new[]
            {
                new Category { Name = "Groceries", Description = "Daily grocery essentials & packaged foods" },
                new Category { Name = "Beverages", Description = "Cold drinks, juices, coffee & tea" },
                new Category { Name = "Snacks & Bakery", Description = "Breads, cookies, chips & chocolates" },
                new Category { Name = "Personal Care", Description = "Hygiene, soaps, shampoos & oral care" },
                new Category { Name = "Electronics & Tech", Description = "Cables, chargers, peripherals & accessories" },
                new Category { Name = "Household Items", Description = "Cleaning supplies, tissue paper & detergents" }
            };

            foreach (var cat in seedCats)
            {
                var created = await categoryRepo.AddAsync(cat);
                categoryMap[cat.Name] = created.Id;
            }
        }
        else
        {
            foreach (var cat in existingCategories)
            {
                categoryMap[cat.Name] = cat.Id;
            }
        }

        // 5. Seed Suppliers
        var supplierRepo = scope.ServiceProvider.GetRequiredService<IRepository<Supplier>>();
        var suppliers = await supplierRepo.GetAllAsync();
        string defaultSupplierId = string.Empty;
        if (suppliers.Count == 0)
        {
            var s1 = await supplierRepo.AddAsync(new Supplier
            {
                Name = "Apex Global Distributing",
                Company = "Apex Trading Co.",
                Phone = "+1 (555) 301-4455",
                Email = "orders@apexdist.com",
                Address = "450 Logistics Park, Industrial Area",
                PreviousDue = 0,
                CurrentDue = 0,
                TotalPurchases = 0,
                IsActive = true
            });
            var s2 = await supplierRepo.AddAsync(new Supplier
            {
                Name = "Prime Foods Supply",
                Company = "Prime Wholesale LLC",
                Phone = "+1 (555) 782-9900",
                Email = "sales@primefoods.com",
                Address = "88 Harbor Way, Port District",
                PreviousDue = 0,
                CurrentDue = 0,
                TotalPurchases = 0,
                IsActive = true
            });
            defaultSupplierId = s1.Id;
        }
        else
        {
            defaultSupplierId = suppliers[0].Id;
        }

        // 6. Seed Customers
        var customerRepo = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
        var customers = await customerRepo.GetAllAsync();
        if (customers.Count == 0)
        {
            await customerRepo.AddAsync(new Customer
            {
                Name = "Walk-in Customer",
                Phone = "N/A",
                Email = "",
                Address = "Counter Direct",
                CurrentDue = 0,
                TotalPurchases = 0,
                IsActive = true
            });
            await customerRepo.AddAsync(new Customer
            {
                Name = "Michael Scott",
                Phone = "+1 (555) 987-6543",
                Email = "mscott@dundermifflin.com",
                Address = "1725 Slough Ave, Scranton, PA",
                PreviousDue = 0,
                CurrentDue = 45.50m,
                TotalPurchases = 350.00m,
                IsActive = true
            });
            await customerRepo.AddAsync(new Customer
            {
                Name = "Pam Beesly",
                Phone = "+1 (555) 432-1098",
                Email = "pam@artstudio.com",
                Address = "42 Elm Street, Scranton, PA",
                PreviousDue = 0,
                CurrentDue = 0,
                TotalPurchases = 120.00m,
                IsActive = true
            });
        }

        // 7. Seed Sample Products
        var productRepo = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
        var products = await productRepo.GetAllAsync();
        if (products.Count == 0)
        {
            string grocId = categoryMap.GetValueOrDefault("Groceries", "");
            string bevId = categoryMap.GetValueOrDefault("Beverages", "");
            string snackId = categoryMap.GetValueOrDefault("Snacks & Bakery", "");
            string elecId = categoryMap.GetValueOrDefault("Electronics & Tech", "");
            string houseId = categoryMap.GetValueOrDefault("Household Items", "");

            var sampleProducts = new List<Product>
            {
                new()
                {
                    Name = "Organic Whole Milk 1 Gallon",
                    SKU = "MILK-001",
                    Barcode = "8901001001",
                    CategoryId = bevId,
                    CategoryName = "Beverages",
                    Brand = "FarmFresh",
                    PurchasePrice = 2.80m,
                    SellingPrice = 4.49m,
                    WholesalePrice = 3.99m,
                    StockQuantity = 45,
                    MinStockLevel = 10,
                    Unit = "gal",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 0,
                    IsActive = true
                },
                new()
                {
                    Name = "Artisan Sourdough Bread",
                    SKU = "BAKE-002",
                    Barcode = "8901001002",
                    CategoryId = snackId,
                    CategoryName = "Snacks & Bakery",
                    Brand = "GoldenCrust",
                    PurchasePrice = 2.10m,
                    SellingPrice = 3.99m,
                    WholesalePrice = 3.50m,
                    StockQuantity = 28,
                    MinStockLevel = 5,
                    Unit = "loaf",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 0,
                    IsActive = true
                },
                new()
                {
                    Name = "Arabica Dark Roast Coffee Beans 1kg",
                    SKU = "BEV-003",
                    Barcode = "8901001003",
                    CategoryId = bevId,
                    CategoryName = "Beverages",
                    Brand = "BaristaChoice",
                    PurchasePrice = 9.50m,
                    SellingPrice = 16.99m,
                    WholesalePrice = 14.50m,
                    StockQuantity = 18,
                    MinStockLevel = 5,
                    Unit = "bag",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 5.0m,
                    IsActive = true
                },
                new()
                {
                    Name = "Extra Virgin Olive Oil 750ml",
                    SKU = "GROC-004",
                    Barcode = "8901001004",
                    CategoryId = grocId,
                    CategoryName = "Groceries",
                    Brand = "TuscanGold",
                    PurchasePrice = 7.20m,
                    SellingPrice = 12.50m,
                    WholesalePrice = 11.00m,
                    StockQuantity = 32,
                    MinStockLevel = 8,
                    Unit = "bottle",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 0,
                    IsActive = true
                },
                new()
                {
                    Name = "USB-C Fast Charging Cable 2m",
                    SKU = "ELEC-005",
                    Barcode = "8901001005",
                    CategoryId = elecId,
                    CategoryName = "Electronics & Tech",
                    Brand = "PowerMax",
                    PurchasePrice = 3.50m,
                    SellingPrice = 9.99m,
                    WholesalePrice = 7.50m,
                    StockQuantity = 50,
                    MinStockLevel = 10,
                    Unit = "pcs",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 8.0m,
                    IsActive = true
                },
                new()
                {
                    Name = "Ultra Clean Laundry Detergent Pods 40ct",
                    SKU = "HOUSE-006",
                    Barcode = "8901001006",
                    CategoryId = houseId,
                    CategoryName = "Household Items",
                    Brand = "Sparkle",
                    PurchasePrice = 6.40m,
                    SellingPrice = 11.99m,
                    WholesalePrice = 10.00m,
                    StockQuantity = 4, // Low stock sample
                    MinStockLevel = 8,
                    Unit = "pack",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 5.0m,
                    IsActive = true
                },
                new()
                {
                    Name = "Gourmet Dark Chocolate 85% 100g",
                    SKU = "SNACK-007",
                    Barcode = "8901001007",
                    CategoryId = snackId,
                    CategoryName = "Snacks & Bakery",
                    Brand = "SwissDelight",
                    PurchasePrice = 1.20m,
                    SellingPrice = 2.75m,
                    WholesalePrice = 2.20m,
                    StockQuantity = 0, // Out of stock sample
                    MinStockLevel = 10,
                    Unit = "bar",
                    SupplierId = defaultSupplierId,
                    SupplierName = "Apex Global Distributing",
                    TaxRate = 5.0m,
                    IsActive = true
                }
            };

            foreach (var prod in sampleProducts)
            {
                await productRepo.AddAsync(prod);
            }
        }

        // 8. Seed Expense Categories
        var expCatRepo = scope.ServiceProvider.GetRequiredService<IRepository<ExpenseCategory>>();
        var expCats = await expCatRepo.GetAllAsync();
        if (expCats.Count == 0)
        {
            await expCatRepo.AddAsync(new ExpenseCategory { Name = "Utilities (Electricity, Water, Internet)", Description = "Store utility bills" });
            await expCatRepo.AddAsync(new ExpenseCategory { Name = "Store Supplies & Packaging", Description = "Bags, receipts, cleaning items" });
            await expCatRepo.AddAsync(new ExpenseCategory { Name = "Rent & Maintenance", Description = "Store premise rental & repairs" });
            await expCatRepo.AddAsync(new ExpenseCategory { Name = "Staff Refreshments & Meals", Description = "Daily cashier and staff refreshments" });
        }
    }
}
