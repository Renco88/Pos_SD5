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

    private bool _isPaymentModalOpen;
    public bool IsPaymentModalOpen { get => _isPaymentModalOpen; set => SetProperty(ref _isPaymentModalOpen, value); }

    private bool _isCustomerPayment;
    public bool IsCustomerPayment { get => _isCustomerPayment; set => SetProperty(ref _isCustomerPayment, value); }

    private string _modalTitle = "Receive Payment from Customer";
    public string ModalTitle { get => _modalTitle; set => SetProperty(ref _modalTitle, value); }

    private string _targetName = "—";
    public string TargetName { get => _targetName; set => SetProperty(ref _targetName, value ?? "—"); }

    private string _targetId = string.Empty;
    private decimal _paymentAmount;
    public decimal PaymentAmount { get => _paymentAmount; set => SetProperty(ref _paymentAmount, Math.Max(0, value)); }

    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    public PaymentMethod PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }

    public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new ObservableCollection<PaymentMethod>();

    private string _paymentNote = string.Empty;
    public string PaymentNote { get => _paymentNote; set => SetProperty(ref _paymentNote, value ?? string.Empty); }

    private decimal _maxPayable;
    public decimal MaxPayable { get => _maxPayable; set => SetProperty(ref _maxPayable, Math.Max(0, value)); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCustomerPaymentCommand { get; }
    public ICommand OpenSupplierPaymentCommand { get; }
    public ICommand SetFullAmountCommand { get; }
    public ICommand SubmitPaymentCommand { get; }
    public ICommand ClosePaymentModalCommand { get; }

    public DueManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;

        PaymentMethods.Add(PaymentMethod.Cash);
        PaymentMethods.Add(PaymentMethod.Card);
        PaymentMethods.Add(PaymentMethod.MobileBanking);
        PaymentMethods.Add(PaymentMethod.BankTransfer);
        PaymentMethods.Add(PaymentMethod.CreditDue);
        PaymentMethods.Add(PaymentMethod.SplitPartial);

        try
        {
            RefreshCommand = new AsyncRelayCommand(LoadDueSummaryAsync);
            OpenCustomerPaymentCommand = new RelayCommand(p => SafeOpenCustomer(p));
            OpenSupplierPaymentCommand = new RelayCommand(p => SafeOpenSupplier(p));
            SetFullAmountCommand = new RelayCommand(() => PaymentAmount = MaxPayable);
            SubmitPaymentCommand = new AsyncRelayCommand(SafeSubmitPayment, SafeCanSubmit);
            ClosePaymentModalCommand = new RelayCommand(() => { try { IsPaymentModalOpen = false; } catch { /* ignore */ } });

            _ = LoadDueSummaryAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[DueManagement] Init faulted: {t.Exception?.Flatten().Message}");
                    }
                    catch { /* ignore */ }
                }
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] Constructor exception: {ex}");
        }
    }

    private void SafeOpenCustomer(object? p)
    {
        try
        {
            if (p is CustomerDto c) OpenCustomerPayment(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] SafeOpenCustomer: {ex.Message}");
        }
    }

    private void SafeOpenSupplier(object? p)
    {
        try
        {
            if (p is SupplierDto s) OpenSupplierPayment(s);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] SafeOpenSupplier: {ex.Message}");
        }
    }

    private bool SafeCanSubmit()
    {
        try
        {
            return PaymentAmount > 0 && PaymentAmount <= MaxPayable && !string.IsNullOrWhiteSpace(_targetId);
        }
        catch
        {
            return false;
        }
    }

    private async Task SafeSubmitPayment()
    {
        try
        {
            await SubmitPaymentAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] SafeSubmitPayment: {ex.Message}");
        }
    }

    public async Task LoadDueSummaryAsync()
    {
        try
        {
            IsBusy = true;
            var res = await _apiClient.GetDueSummaryAsync();
            if (res != null && res.Success && res.Data != null)
            {
                DueSummary = res.Data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] LoadDueSummaryAsync: {ex.Message}");
        }
        finally
        {
            try { IsBusy = false; } catch { /* ignore */ }
        }
    }

    private void OpenCustomerPayment(CustomerDto? c)
    {
        if (c == null) return;
        try
        {
            _targetId = c.Id ?? string.Empty;
            _isCustomerPayment = true;
            ModalTitle = "💰 Receive Payment from Customer";
            TargetName = string.IsNullOrWhiteSpace(c.Name) ? "Customer" : c.Name;
            MaxPayable = c.CurrentDue;
            PaymentAmount = c.CurrentDue;
            PaymentMethod = PaymentMethod.Cash;
            PaymentNote = string.Empty;
            IsPaymentModalOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] OpenCustomerPayment: {ex.Message}");
        }
    }

    private void OpenSupplierPayment(SupplierDto? s)
    {
        if (s == null) return;
        try
        {
            _targetId = s.Id ?? string.Empty;
            _isCustomerPayment = false;
            ModalTitle = "💳 Pay Due to Supplier";
            TargetName = string.IsNullOrWhiteSpace(s.Name) ? "Supplier" : s.Name;
            MaxPayable = s.CurrentDue;
            PaymentAmount = s.CurrentDue;
            PaymentMethod = PaymentMethod.Cash;
            PaymentNote = string.Empty;
            IsPaymentModalOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] OpenSupplierPayment: {ex.Message}");
        }
    }

    private async Task SubmitPaymentAsync()
    {
        if (!SafeCanSubmit()) return;
        IsBusy = true;
        try
        {
            bool ok;
            if (_isCustomerPayment)
            {
                var req = new CustomerPaymentRequest
                {
                    CustomerId = _targetId,
                    Amount = PaymentAmount,
                    PaymentMethod = PaymentMethod,
                    Note = PaymentNote ?? string.Empty
                };
                var res = await _apiClient.RecordCustomerPaymentAsync(req);
                ok = res != null && res.Success;
            }
            else
            {
                var req = new SupplierPaymentRequest
                {
                    SupplierId = _targetId,
                    Amount = PaymentAmount,
                    PaymentMethod = PaymentMethod,
                    Note = PaymentNote ?? string.Empty
                };
                var res = await _apiClient.RecordSupplierPaymentAsync(req);
                ok = res != null && res.Success;
            }
            if (ok)
            {
                IsPaymentModalOpen = false;
                await LoadDueSummaryAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DueManagement] SubmitPaymentAsync: {ex.Message}");
        }
        finally
        {
            try { IsBusy = false; } catch { /* ignore */ }
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
