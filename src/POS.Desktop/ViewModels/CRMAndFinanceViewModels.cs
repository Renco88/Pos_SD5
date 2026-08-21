using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using POS.Application.DTOs;
using POS.Desktop.Services;
using POS.Domain.Enums;

namespace POS.Desktop.ViewModels;

public class PurchaseManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<PurchaseDto> Purchases { get; } = [];

    public ICommand RefreshCommand { get; }

    public PurchaseManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadPurchasesAsync);
        _ = LoadPurchasesAsync();
    }

    public async Task LoadPurchasesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetPurchasesAsync(new PurchaseFilterRequest { PageSize = 100 });
            if (res.Success && res.Data != null)
            {
                Purchases.Clear();
                foreach (var p in res.Data.Items) Purchases.Add(p);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class SupplierManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    private string _name = string.Empty;
    private string _company = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private bool _isModalOpen;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Company { get => _company; set => SetProperty(ref _company, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseModalCommand { get; }

    public SupplierManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        OpenCreateModalCommand = new RelayCommand(() => { Name = ""; Company = ""; Phone = ""; Email = ""; IsModalOpen = true; });
        SaveCommand = new AsyncRelayCommand(SaveSupplierAsync);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);

        _ = LoadSuppliersAsync();
    }

    public async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetSuppliersAsync();
            if (res.Success && res.Data != null)
            {
                Suppliers.Clear();
                foreach (var s in res.Data) Suppliers.Add(s);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        var res = await _apiClient.CreateSupplierAsync(new CreateSupplierRequest
        {
            Name = Name,
            Company = Company,
            Phone = Phone,
            Email = Email
        });
        if (res.Success)
        {
            IsModalOpen = false;
            await LoadSuppliersAsync();
        }
    }
}

public class CustomerManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<CustomerDto> Customers { get; } = [];

    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private bool _isModalOpen;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseModalCommand { get; }

    public CustomerManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadCustomersAsync);
        OpenCreateModalCommand = new RelayCommand(() => { Name = ""; Phone = ""; Email = ""; Address = ""; IsModalOpen = true; });
        SaveCommand = new AsyncRelayCommand(SaveCustomerAsync);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);

        _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetCustomersAsync();
            if (res.Success && res.Data != null)
            {
                Customers.Clear();
                foreach (var c in res.Data) Customers.Add(c);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        var res = await _apiClient.CreateCustomerAsync(new CreateCustomerRequest
        {
            Name = Name,
            Phone = Phone,
            Email = Email,
            Address = Address
        });
        if (res.Success)
        {
            IsModalOpen = false;
            await LoadCustomersAsync();
        }
    }
}

public class DueManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private DueSummaryDto? _dueSummary;

    public DueSummaryDto? DueSummary
    {
        get => _dueSummary;
        set => SetProperty(ref _dueSummary, value);
    }

    public ICommand RefreshCommand { get; }

    public DueManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadDueSummaryAsync);
        _ = LoadDueSummaryAsync();
    }

    public async Task LoadDueSummaryAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetDueSummaryAsync();
            if (res.Success && res.Data != null)
            {
                DueSummary = res.Data;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class ExpenseManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<ExpenseDto> Expenses { get; } = [];

    private string _description = string.Empty;
    private decimal _amount;
    private bool _isModalOpen;

    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseModalCommand { get; }

    public ExpenseManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadExpensesAsync);
        OpenCreateModalCommand = new RelayCommand(() => { Description = ""; Amount = 0; IsModalOpen = true; });
        SaveCommand = new AsyncRelayCommand(SaveExpenseAsync);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);

        _ = LoadExpensesAsync();
    }

    public async Task LoadExpensesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetExpensesAsync();
            if (res.Success && res.Data != null)
            {
                Expenses.Clear();
                foreach (var e in res.Data) Expenses.Add(e);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveExpenseAsync()
    {
        if (Amount <= 0) return;
        var res = await _apiClient.CreateExpenseAsync(new CreateExpenseRequest
        {
            Description = Description,
            Amount = Amount,
            PaymentMethod = PaymentMethod.Cash
        });
        if (res.Success)
        {
            IsModalOpen = false;
            await LoadExpensesAsync();
        }
    }
}

public class SalesReturnViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<ReturnDto> Returns { get; } = [];

    public ICommand RefreshCommand { get; }

    public SalesReturnViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadReturnsAsync);
        _ = LoadReturnsAsync();
    }

    public async Task LoadReturnsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetReturnsAsync();
            if (res.Success && res.Data != null)
            {
                Returns.Clear();
                foreach (var r in res.Data.Items) Returns.Add(r);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class DiscountManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<DiscountRuleDto> Discounts { get; } = [];

    public ICommand RefreshCommand { get; }

    public DiscountManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadDiscountsAsync);
        _ = LoadDiscountsAsync();
    }

    public async Task LoadDiscountsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetDiscountsAsync();
            if (res.Success && res.Data != null)
            {
                Discounts.Clear();
                foreach (var d in res.Data) Discounts.Add(d);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class CashManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;

    private CashSessionDto? _currentSession;
    private decimal _openFloat = 100;
    private decimal _closeActualCash;
    private bool _isOpenModalOpen;
    private bool _isCloseModalOpen;

    public CashSessionDto? CurrentSession { get => _currentSession; set => SetProperty(ref _currentSession, value); }
    public decimal OpenFloat { get => _openFloat; set => SetProperty(ref _openFloat, value); }
    public decimal CloseActualCash { get => _closeActualCash; set => SetProperty(ref _closeActualCash, value); }
    public bool IsOpenModalOpen { get => _isOpenModalOpen; set => SetProperty(ref _isOpenModalOpen, value); }
    public bool IsCloseModalOpen { get => _isCloseModalOpen; set => SetProperty(ref _isCloseModalOpen, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenSessionCommand { get; }
    public ICommand CloseSessionCommand { get; }

    public CashManagementViewModel(IApiClient apiClient, IAuthSession authSession)
    {
        _apiClient = apiClient;
        _authSession = authSession;

        RefreshCommand = new AsyncRelayCommand(LoadCurrentSessionAsync);
        OpenSessionCommand = new AsyncRelayCommand(OpenSessionAsync);
        CloseSessionCommand = new AsyncRelayCommand(CloseSessionAsync);

        _ = LoadCurrentSessionAsync();
    }

    public async Task LoadCurrentSessionAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetCurrentCashSessionAsync();
            if (res.Success)
            {
                CurrentSession = res.Data;
                _authSession.CurrentCashSession = res.Data;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenSessionAsync()
    {
        var res = await _apiClient.OpenCashSessionAsync(new OpenCashSessionRequest { OpeningFloat = OpenFloat });
        if (res.Success)
        {
            IsOpenModalOpen = false;
            await LoadCurrentSessionAsync();
        }
        else ErrorMessage = res.Message;
    }

    private async Task CloseSessionAsync()
    {
        if (CurrentSession == null) return;
        var res = await _apiClient.CloseCashSessionAsync(CurrentSession.Id, new CloseCashSessionRequest { ActualCash = CloseActualCash });
        if (res.Success)
        {
            IsCloseModalOpen = false;
            await LoadCurrentSessionAsync();
        }
        else ErrorMessage = res.Message;
    }
}
