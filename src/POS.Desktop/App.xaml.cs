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

    // =========================
    // 🛡️ FLOOD PROTECTION: Stop duplicate error boxes
    // =========================
    private static DateTime _lastErrorBoxTime = DateTime.MinValue;
    private static string? _lastErrorMessage;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("App not initialized.");

    private static void SafeShowError(Exception ex, string context = "App")
    {
        try
        {
            var now = DateTime.Now;
            var msg = $"{context}|{ex.GetType().Name}|{ex.Message}";

            // 1. Same error within 8 seconds → SKIP silently
            if (msg == _lastErrorMessage && (now - _lastErrorBoxTime).TotalSeconds < 8)
                return;
            // 2. Different error but within 3 seconds → SKIP (stop spam)
            if ((now - _lastErrorBoxTime).TotalSeconds < 3)
                return;

            _lastErrorBoxTime = now;
            _lastErrorMessage = msg;

            System.Diagnostics.Debug.WriteLine($"[CrashShield] {context}: {ex}");
            MessageBox.Show(
                "Something went wrong, but the app will continue running.\n\n" +
                $"Error: {ex.Message}\n" +
                $"Type: {ex.GetType().Name}\n" +
                $"Context: {context}\n\n" +
                "If this keeps happening, restart the app or contact support.",
                "⚠️ Application Error (Safe Mode)",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch { /* ignore */ }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // =====================================================
        // 🛡️ GLOBAL EXCEPTION SHIELD - APP EXIT হবে না!
        // =====================================================
        DispatcherUnhandledException += (sender, args) =>
        {
            try
            {
                SafeShowError(args.Exception, "UI/Dispatcher");
                args.Handled = true; // ✅ APP EXIT হবে না!
            }
            catch
            {
                try { args.Handled = true; } catch { /* ignore */ }
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            try
            {
                var ex = args.Exception?.Flatten();
                if (ex != null) SafeShowError(ex, "BackgroundTask");
                args.SetObserved(); // ✅ APP EXIT হবে না!
            }
            catch { /* ignore */ }
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            try
            {
                var ex = args.ExceptionObject as Exception;
                if (ex != null && args.IsTerminating)
                {
                    MessageBox.Show(
                        "A critical error occurred. Please restart the app.\n\n" +
                        $"Error: {ex.Message}",
                        "🚨 Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                }
                else if (ex != null)
                {
                    SafeShowError(ex, "Domain");
                }
            }
            catch { /* ignore */ }
        };

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
