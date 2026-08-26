using System;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.Desktop.Services;
using POS.Desktop.ViewModels;

namespace POS.Desktop;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("App not initialized.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Initialize Navigation to Login
            var nav = _serviceProvider.GetRequiredService<INavigationService>();
            nav.NavigateTo<LoginViewModel>();

            // Create and show Main Window
            var shellVm = _serviceProvider.GetRequiredService<ShellViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = shellVm
            };

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "POS Startup Crash Log", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // HTTP Client - PRODUCTION (Render Live URL)
        services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("https://nexpos-api.onrender.com/"),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Services
        services.AddSingleton<IAuthSession, AuthSession>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IApiClient, ApiClient>();

        // ViewModels
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<SalesManagementViewModel>();
        services.AddTransient<ProductManagementViewModel>();
        services.AddTransient<CategoryManagementViewModel>();
        services.AddTransient<InventoryManagementViewModel>();
        services.AddTransient<PurchaseManagementViewModel>();
        services.AddTransient<SupplierManagementViewModel>();
        services.AddTransient<CustomerManagementViewModel>();
        services.AddTransient<DueManagementViewModel>();
        services.AddTransient<ExpenseManagementViewModel>();
        services.AddTransient<SalesReturnViewModel>();
        services.AddTransient<DiscountManagementViewModel>();
        services.AddTransient<CashManagementViewModel>();
        services.AddTransient<WorkerManagementViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<InvoiceManagementViewModel>();
        services.AddTransient<BarcodeManagementViewModel>();
        services.AddTransient<BusinessSettingsViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<ActivityLogViewModel>();
        services.AddTransient<BackupRestoreViewModel>();
    }
}
