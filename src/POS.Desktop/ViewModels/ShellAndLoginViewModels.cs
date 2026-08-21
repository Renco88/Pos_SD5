using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using POS.Application.DTOs;
using POS.Desktop.Services;
using POS.Domain.Enums;

namespace POS.Desktop.ViewModels;

public class NavigationItemModel
{
    public string Title { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public Type ViewModelType { get; set; } = typeof(object);
    public string RequiredPermission { get; set; } = string.Empty;
    public bool EmployerOnly { get; set; } = false;
    public bool IsSelected { get; set; } = false;
}

public class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthSession _authSession;
    private readonly IApiClient _apiClient;
    private NavigationItemModel? _selectedNavItem;
    private bool _isSidebarCollapsed;

    public ObservableCollection<NavigationItemModel> NavItems { get; } = [];

    public IAuthSession Session => _authSession;
    public INavigationService Navigation => _navigationService;

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public NavigationItemModel? SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            if (SetProperty(ref _selectedNavItem, value) && value != null)
            {
                _navigationService.NavigateTo(value.ViewModelType);
            }
        }
    }

    public ICommand ToggleSidebarCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand OpenChangePasswordCommand { get; }
    public ICommand RefreshSessionCommand { get; }

    public ShellViewModel(
        INavigationService navigationService,
        IAuthSession authSession,
        IApiClient apiClient)
    {
        _navigationService = navigationService;
        _authSession = authSession;
        _apiClient = apiClient;

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        LogoutCommand = new RelayCommand(Logout);
        OpenChangePasswordCommand = new RelayCommand(OpenChangePassword);
        RefreshSessionCommand = new AsyncRelayCommand(RefreshCashSessionAsync);

        BuildNavigationMenu();
    }

    public void BuildNavigationMenu()
    {
        NavItems.Clear();

        var allItems = new List<NavigationItemModel>
        {
            new() { Title = "Dashboard", IconKey = "IconDashboard", ViewModelType = typeof(DashboardViewModel) },
            new() { Title = "POS / New Sale", IconKey = "IconCart", ViewModelType = typeof(PosViewModel) },
            new() { Title = "Sales History", IconKey = "IconSales", ViewModelType = typeof(SalesManagementViewModel) },
            new() { Title = "Products", IconKey = "IconBox", ViewModelType = typeof(ProductManagementViewModel) },
            new() { Title = "Categories", IconKey = "IconCategory", ViewModelType = typeof(CategoryManagementViewModel) },
            new() { Title = "Inventory & Stock", IconKey = "IconBox", ViewModelType = typeof(InventoryManagementViewModel) },
            new() { Title = "Purchases", IconKey = "IconPurchases", ViewModelType = typeof(PurchaseManagementViewModel), EmployerOnly = true },
            new() { Title = "Suppliers", IconKey = "IconSuppliers", ViewModelType = typeof(SupplierManagementViewModel), EmployerOnly = true },
            new() { Title = "Customers", IconKey = "IconUsers", ViewModelType = typeof(CustomerManagementViewModel) },
            new() { Title = "Due Accounts", IconKey = "IconDue", ViewModelType = typeof(DueManagementViewModel) },
            new() { Title = "Expenses", IconKey = "IconExpense", ViewModelType = typeof(ExpenseManagementViewModel) },
            new() { Title = "Sales Return", IconKey = "IconReturn", ViewModelType = typeof(SalesReturnViewModel) },
            new() { Title = "Discounts", IconKey = "IconDiscount", ViewModelType = typeof(DiscountManagementViewModel) },
            new() { Title = "Cash Register", IconKey = "IconCash", ViewModelType = typeof(CashManagementViewModel) },
            new() { Title = "Workers", IconKey = "IconUsers", ViewModelType = typeof(WorkerManagementViewModel), EmployerOnly = true },
            new() { Title = "Reports & P/L", IconKey = "IconReports", ViewModelType = typeof(ReportsViewModel) },
            new() { Title = "Invoices", IconKey = "IconExpense", ViewModelType = typeof(InvoiceManagementViewModel) },
            new() { Title = "Barcodes", IconKey = "IconBarcode", ViewModelType = typeof(BarcodeManagementViewModel) },
            new() { Title = "Settings", IconKey = "IconSettings", ViewModelType = typeof(BusinessSettingsViewModel), EmployerOnly = true },
            new() { Title = "User Admin", IconKey = "IconUsers", ViewModelType = typeof(UserManagementViewModel), EmployerOnly = true },
            new() { Title = "Activity Log", IconKey = "IconActivity", ViewModelType = typeof(ActivityLogViewModel), EmployerOnly = true },
            new() { Title = "Backup & Restore", IconKey = "IconBackup", ViewModelType = typeof(BackupRestoreViewModel), EmployerOnly = true },
        };

        foreach (var item in allItems)
        {
            if (_authSession.IsEmployer)
            {
                NavItems.Add(item);
            }
            else if (!item.EmployerOnly)
            {
                NavItems.Add(item);
            }
        }

        SelectedNavItem = NavItems.FirstOrDefault();
    }

    private async Task RefreshCashSessionAsync()
    {
        var res = await _apiClient.GetCurrentCashSessionAsync();
        if (res.Success)
        {
            _authSession.CurrentCashSession = res.Data;
        }
    }

    private void OpenChangePassword()
    {
        _navigationService.NavigateTo<ChangePasswordViewModel>();
    }

    private void Logout()
    {
        _authSession.Clear();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}

public class LoginViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;
    private readonly INavigationService _navigationService;
    private readonly ShellViewModel _shellViewModel;

    private string _username = "admin";
    private string _password = "ChangeMe123!";

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand UseAdminCredentialsCommand { get; }
    public ICommand UseWorkerCredentialsCommand { get; }

    public LoginViewModel(
        IApiClient apiClient,
        IAuthSession authSession,
        INavigationService navigationService,
        ShellViewModel shellViewModel)
    {
        _apiClient = apiClient;
        _authSession = authSession;
        _navigationService = navigationService;
        _shellViewModel = shellViewModel;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        UseAdminCredentialsCommand = new RelayCommand(() => { Username = "admin"; Password = "ChangeMe123!"; });
        UseWorkerCredentialsCommand = new RelayCommand(() => { Username = "worker"; Password = "ChangeMe123!"; });
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var res = await _apiClient.LoginAsync(new LoginRequest { Username = Username.Trim(), Password = Password });
            if (res.Success && res.Data != null)
            {
                _authSession.SetSession(res.Data);

                // Fetch active cash session
                var cashRes = await _apiClient.GetCurrentCashSessionAsync();
                if (cashRes.Success)
                {
                    _authSession.CurrentCashSession = cashRes.Data;
                }

                _shellViewModel.BuildNavigationMenu();

                if (res.Data.User.MustChangePassword)
                {
                    _navigationService.NavigateTo<ChangePasswordViewModel>();
                }
                else
                {
                    _navigationService.NavigateTo<DashboardViewModel>();
                }
            }
            else
            {
                ErrorMessage = res.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class ChangePasswordViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;
    private readonly INavigationService _navigationService;

    private string _oldPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;

    public string OldPassword
    {
        get => _oldPassword;
        set => SetProperty(ref _oldPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
    }

    public ICommand ChangePasswordCommand { get; }
    public ICommand CancelCommand { get; }

    public ChangePasswordViewModel(
        IApiClient apiClient,
        IAuthSession authSession,
        INavigationService navigationService)
    {
        _apiClient = apiClient;
        _authSession = authSession;
        _navigationService = navigationService;

        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        CancelCommand = new RelayCommand(() => _navigationService.NavigateTo<DashboardViewModel>());
    }

    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            ErrorMessage = "New password and confirmation do not match.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var res = await _apiClient.ChangePasswordAsync(new ChangePasswordRequest
            {
                OldPassword = OldPassword,
                NewPassword = NewPassword,
                ConfirmNewPassword = ConfirmNewPassword
            });

            if (res.Success)
            {
                if (_authSession.CurrentUser != null)
                {
                    _authSession.CurrentUser.MustChangePassword = false;
                }
                SuccessMessage = "Password changed successfully!";
                await Task.Delay(1000);
                _navigationService.NavigateTo<DashboardViewModel>();
            }
            else
            {
                ErrorMessage = res.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
