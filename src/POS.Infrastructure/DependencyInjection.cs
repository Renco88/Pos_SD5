using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Interfaces;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Infrastructure.MongoDB;
using POS.Infrastructure.Security;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Configuration
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddSingleton<MongoDbContext>();

        // Generic Repositories
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // Security Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Application Business Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDueService, DueService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICashService, CashService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }
}
