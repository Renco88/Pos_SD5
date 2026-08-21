using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using POS.Application.DTOs;
using POS.Desktop.Services;
using POS.Domain.Enums;

namespace POS.Desktop.ViewModels;

public class WorkerManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<UserDto> Workers { get; } = [];

    private bool _isModalOpen;
    private bool _isEditMode;
    private string _editingId = string.Empty;

    private string _formFullName = string.Empty;
    private string _formUsername = string.Empty;
    private string _formEmail = string.Empty;
    private string _formPhone = string.Empty;
    private string _formPassword = string.Empty;
    private decimal _formMaxDiscount = 5.0m;
    private bool _formMustChangePassword = true;

    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }
    public bool IsEditMode
    {
        get => _isEditMode;
        set { if (SetProperty(ref _isEditMode, value)) OnPropertyChanged(nameof(ModalTitle)); }
    }

    public string ModalTitle => IsEditMode ? "Edit Worker" : "Create New Worker";

    public string FormFullName { get => _formFullName; set => SetProperty(ref _formFullName, value); }
    public string FormUsername { get => _formUsername; set => SetProperty(ref _formUsername, value); }
    public string FormEmail { get => _formEmail; set => SetProperty(ref _formEmail, value); }
    public string FormPhone { get => _formPhone; set => SetProperty(ref _formPhone, value); }
    public string FormPassword { get => _formPassword; set => SetProperty(ref _formPassword, value); }
    public decimal FormMaxDiscount { get => _formMaxDiscount; set => SetProperty(ref _formMaxDiscount, value); }
    public bool FormMustChangePassword { get => _formMustChangePassword; set => SetProperty(ref _formMustChangePassword, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand OpenEditModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseModalCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand ToggleStatusCommand { get; }

    public WorkerManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadWorkersAsync);
        OpenCreateModalCommand = new RelayCommand(OpenCreateModal);
        OpenEditModalCommand = new RelayCommand(p => { if (p is UserDto u) OpenEditModal(u); });
        SaveCommand = new AsyncRelayCommand(SaveWorkerAsync);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);
        DeleteCommand = new AsyncRelayCommand(p => p is UserDto u ? DeleteWorkerAsync(u) : Task.CompletedTask);
        ResetPasswordCommand = new AsyncRelayCommand(p => p is UserDto u ? ResetPasswordAsync(u) : Task.CompletedTask);
        ToggleStatusCommand = new AsyncRelayCommand(p => p is UserDto u ? ToggleStatusAsync(u) : Task.CompletedTask);

        _ = LoadWorkersAsync();
    }

    public async Task LoadWorkersAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetWorkersAsync();
            if (res.Success && res.Data != null)
            {
                Workers.Clear();
                foreach (var w in res.Data) Workers.Add(w);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenCreateModal()
    {
        IsEditMode = false;
        _editingId = string.Empty;
        FormFullName = "";
        FormUsername = "";
        FormEmail = "";
        FormPhone = "";
        FormPassword = "ChangeMe123!";
        FormMaxDiscount = 5.0m;
        FormMustChangePassword = true;
        IsModalOpen = true;
    }

    private void OpenEditModal(UserDto u)
    {
        IsEditMode = true;
        _editingId = u.Id;
        FormFullName = u.FullName;
        FormUsername = u.Username;
        FormEmail = u.Email;
        FormPhone = u.Phone;
        FormPassword = "";
        FormMaxDiscount = u.MaxDiscountPercentage;
        FormMustChangePassword = false;
        IsModalOpen = true;
    }

    private async Task SaveWorkerAsync()
    {
        if (string.IsNullOrWhiteSpace(FormFullName) || string.IsNullOrWhiteSpace(FormUsername))
        {
            ErrorMessage = "Full Name and Username are required.";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var req = new UpdateUserRequest
                {
                    FullName = FormFullName,
                    Email = FormEmail,
                    Phone = FormPhone,
                    MaxDiscountPercentage = FormMaxDiscount
                };
                var res = await _apiClient.UpdateWorkerAsync(_editingId, req);
                if (res.Success)
                {
                    SuccessMessage = $"Worker '{FormUsername}' updated successfully!";
                    IsModalOpen = false;
                    await LoadWorkersAsync();
                }
                else ErrorMessage = res.Message;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FormPassword))
                {
                    ErrorMessage = "Password is required for new workers.";
                    return;
                }
                var req = new CreateUserRequest
                {
                    FullName = FormFullName,
                    Username = FormUsername,
                    Email = FormEmail,
                    Phone = FormPhone,
                    Password = FormPassword,
                    Role = Roles.Worker,
                    MaxDiscountPercentage = FormMaxDiscount,
                    MustChangePassword = FormMustChangePassword
                };
                var res = await _apiClient.CreateWorkerAsync(req);
                if (res.Success)
                {
                    SuccessMessage = $"Worker '{FormUsername}' created successfully!";
                    IsModalOpen = false;
                    await LoadWorkersAsync();
                }
                else ErrorMessage = res.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetPasswordAsync(UserDto u)
    {
        var newPass = "ChangeMe123!";
        var res = await _apiClient.ResetWorkerPasswordAsync(u.Id, newPass);
        if (res.Success)
        {
            SuccessMessage = $"Password for '{u.Username}' reset to '{newPass}'. Must change on next login.";
        }
        else ErrorMessage = res.Message;
    }

    private async Task ToggleStatusAsync(UserDto u)
    {
        var res = await _apiClient.ToggleUserStatusAsync(u.Id, !u.IsActive);
        if (res.Success)
        {
            SuccessMessage = $"Worker '{u.Username}' status: {(u.IsActive ? "Deactivated" : "Activated")}";
            await LoadWorkersAsync();
        }
        else ErrorMessage = res.Message;
    }

    private async Task DeleteWorkerAsync(UserDto u)
    {
        if (u == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"Are you sure you want to permanently delete Worker '{u.Username}' ({u.FullName})? This action cannot be undone.",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var res = await _apiClient.DeleteWorkerAsync(u.Id);
            if (res.Success)
            {
                SuccessMessage = $"Worker '{u.Username}' deleted permanently.";
                await LoadWorkersAsync();
            }
            else ErrorMessage = res.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class ReportsViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private ProfitLossReportDto? _profitLoss;
    private SalesReportDto? _salesReport;

    public ProfitLossReportDto? ProfitLoss { get => _profitLoss; set => SetProperty(ref _profitLoss, value); }
    public SalesReportDto? SalesReport { get => _salesReport; set => SetProperty(ref _salesReport, value); }

    public ICommand RefreshCommand { get; }

    public ReportsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadReportsAsync);
        _ = LoadReportsAsync();
    }

    public async Task LoadReportsAsync()
    {
        IsBusy = true;
        try
        {
            var from = DateTime.UtcNow.AddMonths(-1);
            var to = DateTime.UtcNow;

            var plRes = await _apiClient.GetProfitLossReportAsync(from, to);
            if (plRes.Success) ProfitLoss = plRes.Data;

            var salesRes = await _apiClient.GetSalesReportAsync(new ReportFilterRequest());
            if (salesRes.Success) SalesReport = salesRes.Data;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class InvoiceManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<SaleDto> Invoices { get; } = [];

    public ICommand RefreshCommand { get; }

    public InvoiceManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        _ = LoadInvoicesAsync();
    }

    public async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetSalesAsync(new SaleFilterRequest { PageSize = 50 });
            if (res.Success && res.Data != null)
            {
                Invoices.Clear();
                foreach (var s in res.Data.Items) Invoices.Add(s);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class BarcodeManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private string _code = "MILK-001";
    private string _barcodeSvg = string.Empty;

    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string BarcodeSvg { get => _barcodeSvg; set => SetProperty(ref _barcodeSvg, value); }

    public ICommand GenerateCommand { get; }

    public BarcodeManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        GenerateCommand = new AsyncRelayCommand(GenerateBarcodeAsync);
        _ = GenerateBarcodeAsync();
    }

    public async Task GenerateBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        var res = await _apiClient.GenerateBarcodeSvgAsync(Code);
        if (res.Success && res.Data != null)
        {
            BarcodeSvg = res.Data;
        }
    }
}

public class BusinessSettingsViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private BusinessSettingsDto _settings = new();

    public BusinessSettingsDto Settings { get => _settings; set => SetProperty(ref _settings, value); }

    public ICommand SaveCommand { get; }
    public ICommand RefreshCommand { get; }

    public BusinessSettingsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
        RefreshCommand = new AsyncRelayCommand(LoadSettingsAsync);
        _ = LoadSettingsAsync();
    }

    public async Task LoadSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetSettingsAsync();
            if (res.Success && res.Data != null) Settings = res.Data;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.UpdateSettingsAsync(Settings);
            if (res.Success) SuccessMessage = "Settings saved successfully!";
            else ErrorMessage = res.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class UserManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<UserDto> Users { get; } = [];

    private bool _isModalOpen;
    private bool _isEditMode;
    private string _editingId = string.Empty;

    private string _formFullName = string.Empty;
    private string _formUsername = string.Empty;
    private string _formEmail = string.Empty;
    private string _formPhone = string.Empty;
    private string _formPassword = string.Empty;
    private string _formRole = Roles.Worker;
    private decimal _formMaxDiscount = 5.0m;
    private bool _formMustChangePassword = true;

    public List<string> AvailableRoles { get; } = [Roles.Employer, Roles.Worker];

    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }
    public bool IsEditMode
    {
        get => _isEditMode;
        set { if (SetProperty(ref _isEditMode, value)) OnPropertyChanged(nameof(ModalTitle)); }
    }

    public string ModalTitle => IsEditMode ? "Edit User" : "Create New User";

    public string FormFullName { get => _formFullName; set => SetProperty(ref _formFullName, value); }
    public string FormUsername { get => _formUsername; set => SetProperty(ref _formUsername, value); }
    public string FormEmail { get => _formEmail; set => SetProperty(ref _formEmail, value); }
    public string FormPhone { get => _formPhone; set => SetProperty(ref _formPhone, value); }
    public string FormPassword { get => _formPassword; set => SetProperty(ref _formPassword, value); }
    public string FormRole { get => _formRole; set => SetProperty(ref _formRole, value); }
    public decimal FormMaxDiscount { get => _formMaxDiscount; set => SetProperty(ref _formMaxDiscount, value); }
    public bool FormMustChangePassword { get => _formMustChangePassword; set => SetProperty(ref _formMustChangePassword, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand OpenEditModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseModalCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleStatusCommand { get; }

    public UserManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadUsersAsync);
        OpenCreateModalCommand = new RelayCommand(OpenCreateModal);
        OpenEditModalCommand = new RelayCommand(p => { if (p is UserDto u) OpenEditModal(u); });
        SaveCommand = new AsyncRelayCommand(SaveUserAsync);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);
        DeleteCommand = new AsyncRelayCommand(p => p is UserDto u ? DeleteUserAsync(u) : Task.CompletedTask);
        ToggleStatusCommand = new AsyncRelayCommand(p => p is UserDto u ? ToggleStatusAsync(u) : Task.CompletedTask);

        _ = LoadUsersAsync();
    }

    public async Task LoadUsersAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetUsersAsync();
            if (res.Success && res.Data != null)
            {
                Users.Clear();
                foreach (var u in res.Data) Users.Add(u);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenCreateModal()
    {
        IsEditMode = false;
        _editingId = string.Empty;
        FormFullName = "";
        FormUsername = "";
        FormEmail = "";
        FormPhone = "";
        FormPassword = "ChangeMe123!";
        FormRole = Roles.Worker;
        FormMaxDiscount = 5.0m;
        FormMustChangePassword = true;
        IsModalOpen = true;
    }

    private void OpenEditModal(UserDto u)
    {
        IsEditMode = true;
        _editingId = u.Id;
        FormFullName = u.FullName;
        FormUsername = u.Username;
        FormEmail = u.Email;
        FormPhone = u.Phone;
        FormPassword = "";
        FormRole = u.Role;
        FormMaxDiscount = u.MaxDiscountPercentage;
        FormMustChangePassword = false;
        IsModalOpen = true;
    }

    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FormFullName) || string.IsNullOrWhiteSpace(FormUsername))
        {
            ErrorMessage = "Full Name and Username are required.";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var req = new UpdateUserRequest
                {
                    FullName = FormFullName,
                    Email = FormEmail,
                    Phone = FormPhone,
                    Role = FormRole,
                    MaxDiscountPercentage = FormMaxDiscount
                };
                var res = await _apiClient.UpdateUserAsync(_editingId, req);
                if (res.Success)
                {
                    SuccessMessage = $"User '{FormUsername}' updated successfully!";
                    IsModalOpen = false;
                    await LoadUsersAsync();
                }
                else ErrorMessage = res.Message;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FormPassword))
                {
                    ErrorMessage = "Password is required for new users.";
                    return;
                }
                var req = new CreateUserRequest
                {
                    FullName = FormFullName,
                    Username = FormUsername,
                    Email = FormEmail,
                    Phone = FormPhone,
                    Password = FormPassword,
                    Role = FormRole,
                    MaxDiscountPercentage = FormMaxDiscount,
                    MustChangePassword = FormMustChangePassword
                };
                var res = await _apiClient.CreateUserAsync(req);
                if (res.Success)
                {
                    SuccessMessage = $"User '{FormUsername}' ({FormRole}) created successfully!";
                    IsModalOpen = false;
                    await LoadUsersAsync();
                }
                else ErrorMessage = res.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleStatusAsync(UserDto u)
    {
        var res = await _apiClient.ToggleUserStatusAsync(u.Id, !u.IsActive);
        if (res.Success)
        {
            SuccessMessage = $"User '{u.Username}' status: {(u.IsActive ? "Deactivated" : "Activated")}";
            await LoadUsersAsync();
        }
        else ErrorMessage = res.Message;
    }

    private async Task DeleteUserAsync(UserDto u)
    {
        if (u == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"Are you sure you want to permanently delete User '{u.Username}' ({u.FullName}, Role: {u.Role})? This action cannot be undone.",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var res = await _apiClient.DeleteUserAsync(u.Id);
            if (res.Success)
            {
                SuccessMessage = $"User '{u.Username}' deleted permanently.";
                await LoadUsersAsync();
            }
            else ErrorMessage = res.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class ActivityLogViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<ActivityLogDto> Logs { get; } = [];

    public ICommand RefreshCommand { get; }

    public ActivityLogViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadLogsAsync);
        _ = LoadLogsAsync();
    }

    public async Task LoadLogsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetActivityLogsAsync(page: 1, pageSize: 100);
            if (res.Success && res.Data != null)
            {
                Logs.Clear();
                foreach (var l in res.Data.Items) Logs.Add(l);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class BackupRestoreViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<BackupDto> Backups { get; } = [];

    public ICommand RefreshCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }

    public BackupRestoreViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadBackupsAsync);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);

        _ = LoadBackupsAsync();
    }

    public async Task LoadBackupsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetBackupsAsync();
            if (res.Success && res.Data != null)
            {
                Backups.Clear();
                foreach (var b in res.Data) Backups.Add(b);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.CreateBackupAsync();
            if (res.Success)
            {
                SuccessMessage = "Backup snapshot created successfully!";
                await LoadBackupsAsync();
            }
            else ErrorMessage = res.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreBackupAsync(object? param)
    {
        if (param is BackupDto b)
        {
            IsBusy = true;
            try
            {
                var res = await _apiClient.RestoreBackupAsync(b.Id);
                if (res.Success) SuccessMessage = "Database snapshot restored successfully!";
                else ErrorMessage = res.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
