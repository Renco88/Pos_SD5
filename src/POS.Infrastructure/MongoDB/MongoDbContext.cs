using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using POS.Domain.Entities;

namespace POS.Infrastructure.MongoDB;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "POS_SD5_Database";
}

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var conn = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("MongoDbSettings__ConnectionString")
            ?? Environment.GetEnvironmentVariable("MongoDbSettings:ConnectionString")
            ?? settings.Value.ConnectionString;

        var dbName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
            ?? Environment.GetEnvironmentVariable("MongoDbSettings__DatabaseName")
            ?? Environment.GetEnvironmentVariable("MongoDbSettings:DatabaseName")
            ?? settings.Value.DatabaseName;

        var client = new MongoClient(conn);
        _database = client.GetDatabase(dbName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("Categories");
    public IMongoCollection<Sale> Sales => _database.GetCollection<Sale>("Sales");
    public IMongoCollection<Purchase> Purchases => _database.GetCollection<Purchase>("Purchases");
    public IMongoCollection<Supplier> Suppliers => _database.GetCollection<Supplier>("Suppliers");
    public IMongoCollection<Customer> Customers => _database.GetCollection<Customer>("Customers");
    public IMongoCollection<CustomerPayment> CustomerPayments => _database.GetCollection<CustomerPayment>("CustomerPayments");
    public IMongoCollection<SupplierPayment> SupplierPayments => _database.GetCollection<SupplierPayment>("SupplierPayments");
    public IMongoCollection<Expense> Expenses => _database.GetCollection<Expense>("Expenses");
    public IMongoCollection<ExpenseCategory> ExpenseCategories => _database.GetCollection<ExpenseCategory>("ExpenseCategories");
    public IMongoCollection<Return> Returns => _database.GetCollection<Return>("Returns");
    public IMongoCollection<DiscountRule> DiscountRules => _database.GetCollection<DiscountRule>("DiscountRules");
    public IMongoCollection<CashSession> CashSessions => _database.GetCollection<CashSession>("CashSessions");
    public IMongoCollection<Invoice> Invoices => _database.GetCollection<Invoice>("Invoices");
    public IMongoCollection<StockTransaction> StockTransactions => _database.GetCollection<StockTransaction>("StockTransactions");
    public IMongoCollection<ActivityLog> ActivityLogs => _database.GetCollection<ActivityLog>("ActivityLogs");
    public IMongoCollection<BusinessSettings> BusinessSettings => _database.GetCollection<BusinessSettings>("BusinessSettings");
    public IMongoCollection<BackupMetadata> Backups => _database.GetCollection<BackupMetadata>("Backups");

    public IMongoCollection<T> GetCollection<T>()
    {
        return _database.GetCollection<T>(typeof(T).Name + "s");
    }
}
