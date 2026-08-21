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

public class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;

    private EmployerDashboardDto? _employerData;
    private WorkerDashboardDto? _workerData;

    public EmployerDashboardDto? EmployerData
    {
        get => _employerData;
        set => SetProperty(ref _employerData, value);
    }

    public WorkerDashboardDto? WorkerData
    {
        get => _workerData;
        set => SetProperty(ref _workerData, value);
    }

    public bool IsEmployer => _authSession.IsEmployer;
    public bool IsWorker => _authSession.IsWorker;

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(IApiClient apiClient, IAuthSession authSession)
    {
        _apiClient = apiClient;
        _authSession = authSession;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            if (_authSession.IsEmployer)
            {
                var res = await _apiClient.GetEmployerDashboardAsync();
                if (res.Success) EmployerData = res.Data;
            }
            else
            {
                var res = await _apiClient.GetWorkerDashboardAsync();
                if (res.Success) WorkerData = res.Data;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class CartItemModel : ViewModelBase
{
    private int _quantity = 1;
    private decimal _discountPercentage = 0;

    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal UnitSellingPrice { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public int AvailableStock { get; set; }
    public decimal TaxRate { get; set; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value > AvailableStock) value = AvailableStock;
            if (value < 1) value = 1;
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(LineSubtotal));
                OnPropertyChanged(nameof(LineDiscountAmount));
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal DiscountPercentage
    {
        get => _discountPercentage;
        set
        {
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            if (SetProperty(ref _discountPercentage, value))
            {
                OnPropertyChanged(nameof(LineDiscountAmount));
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal LineSubtotal => Quantity * UnitSellingPrice;
    public decimal LineDiscountAmount => Math.Round(LineSubtotal * (DiscountPercentage / 100m), 2);
    public decimal LineTaxAmount => TaxRate > 0 ? Math.Round((LineSubtotal - LineDiscountAmount) * (TaxRate / 100m), 2) : 0;
    public decimal LineTotal => (LineSubtotal - LineDiscountAmount) + LineTaxAmount;
}

public class PosViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;

    private string _searchQuery = string.Empty;
    private CategoryDto? _selectedCategory;
    private CustomerDto? _selectedCustomer;
    private decimal _overallDiscountPercent;
    private decimal _paidAmount;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private bool _isPaymentModalOpen;
    private bool _isReceiptModalOpen;
    private string _receiptContent = string.Empty;
    private SaleDto? _completedSale;

    public ObservableCollection<ProductDto> FilteredProducts { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<CartItemModel> CartItems { get; } = [];
    public ObservableCollection<HoldSaleDto> HeldSales { get; } = [];

    private List<ProductDto> _allProducts = [];

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                FilterProducts();
            }
        }
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                FilterProducts();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public decimal OverallDiscountPercent
    {
        get => _overallDiscountPercent;
        set
        {
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            if (SetProperty(ref _overallDiscountPercent, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal Subtotal => CartItems.Sum(i => i.LineSubtotal);
    public decimal ItemDiscountsTotal => CartItems.Sum(i => i.LineDiscountAmount);
    public decimal OverallDiscountAmount => Math.Round((Subtotal - ItemDiscountsTotal) * (OverallDiscountPercent / 100m), 2);
    public decimal TotalDiscount => ItemDiscountsTotal + OverallDiscountAmount;
    public decimal TotalTax => CartItems.Sum(i => i.LineTaxAmount);
    public decimal GrandTotal => Math.Max(0, Subtotal - TotalDiscount + TotalTax);

    public decimal PaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, value))
            {
                OnPropertyChanged(nameof(ChangeAmount));
                OnPropertyChanged(nameof(DueAmount));
            }
        }
    }

    public decimal ChangeAmount => PaidAmount > GrandTotal ? PaidAmount - GrandTotal : 0;
    public decimal DueAmount => PaidAmount < GrandTotal ? GrandTotal - PaidAmount : 0;

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetProperty(ref _selectedPaymentMethod, value);
    }

    public bool IsPaymentModalOpen
    {
        get => _isPaymentModalOpen;
        set => SetProperty(ref _isPaymentModalOpen, value);
    }

    public bool IsReceiptModalOpen
    {
        get => _isReceiptModalOpen;
        set => SetProperty(ref _isReceiptModalOpen, value);
    }

    public string ReceiptContent
    {
        get => _receiptContent;
        set => SetProperty(ref _receiptContent, value);
    }

    public SaleDto? CompletedSale
    {
        get => _completedSale;
        set => SetProperty(ref _completedSale, value);
    }

    // Commands
    public ICommand AddProductToCartCommand { get; }
    public ICommand SearchBarcodeOrSkuCommand { get; }
    public ICommand IncrementQtyCommand { get; }
    public ICommand DecrementQtyCommand { get; }
    public ICommand RemoveCartItemCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand HoldSaleCommand { get; }
    public ICommand ResumeSaleCommand { get; }
    public ICommand OpenPaymentCommand { get; }
    public ICommand ClosePaymentCommand { get; }
    public ICommand CompleteSaleCommand { get; }
    public ICommand SetExactAmountCommand { get; }
    public ICommand CloseReceiptCommand { get; }

    public PosViewModel(IApiClient apiClient, IAuthSession authSession)
    {
        _apiClient = apiClient;
        _authSession = authSession;

        AddProductToCartCommand = new RelayCommand(p => { if (p is ProductDto prod) AddProduct(prod); });
        SearchBarcodeOrSkuCommand = new AsyncRelayCommand(SearchBarcodeOrSkuAsync);
        IncrementQtyCommand = new RelayCommand(item => { if (item is CartItemModel m) { m.Quantity++; RecalculateTotals(); } });
        DecrementQtyCommand = new RelayCommand(item => { if (item is CartItemModel m) { m.Quantity--; if (m.Quantity <= 0) CartItems.Remove(m); } RecalculateTotals(); });
        RemoveCartItemCommand = new RelayCommand(item => { if (item is CartItemModel m) CartItems.Remove(m); RecalculateTotals(); });
        ClearCartCommand = new RelayCommand(ClearCart);
        HoldSaleCommand = new RelayCommand(HoldCurrentSale);
        ResumeSaleCommand = new RelayCommand(h => { if (h is HoldSaleDto hold) ResumeSale(hold); });
        OpenPaymentCommand = new RelayCommand(OpenPaymentModal);
        ClosePaymentCommand = new RelayCommand(() => IsPaymentModalOpen = false);
        CompleteSaleCommand = new AsyncRelayCommand(CompleteSaleAsync);
        SetExactAmountCommand = new RelayCommand(() => PaidAmount = GrandTotal);
        CloseReceiptCommand = new RelayCommand(() => IsReceiptModalOpen = false);

        CartItems.CollectionChanged += (s, e) => RecalculateTotals();

        _ = InitializeCatalogAsync();
    }

    public async Task InitializeCatalogAsync()
    {
        IsBusy = true;
        try
        {
            var catRes = await _apiClient.GetCategoriesAsync();
            if (catRes.Success && catRes.Data != null)
            {
                Categories.Clear();
                Categories.Add(new CategoryDto { Id = "", Name = "All Categories" });
                foreach (var c in catRes.Data) Categories.Add(c);
                SelectedCategory = Categories.FirstOrDefault();
            }

            var custRes = await _apiClient.GetCustomersAsync();
            if (custRes.Success && custRes.Data != null)
            {
                Customers.Clear();
                foreach (var c in custRes.Data) Customers.Add(c);
                SelectedCustomer = Customers.FirstOrDefault(c => c.Name.Contains("Walk-in")) ?? Customers.FirstOrDefault();
            }

            var prodRes = await _apiClient.GetProductsAsync(new ProductFilterRequest { PageSize = 200, ActiveOnly = true });
            if (prodRes.Success && prodRes.Data != null)
            {
                _allProducts = prodRes.Data.Items;
                FilterProducts();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Catalog load error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterProducts()
    {
        var list = _allProducts.AsEnumerable();
        if (SelectedCategory != null && !string.IsNullOrEmpty(SelectedCategory.Id))
        {
            list = list.Where(p => p.CategoryId == SelectedCategory.Id);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim().ToLower();
            list = list.Where(p => p.Name.ToLower().Contains(q) || p.SKU.ToLower().Contains(q) || p.Barcode.ToLower().Contains(q));
        }

        FilteredProducts.Clear();
        foreach (var p in list.Take(50))
        {
            FilteredProducts.Add(p);
        }
    }

    public void AddProduct(ProductDto product)
    {
        if (product.StockQuantity <= 0)
        {
            ErrorMessage = $"'{product.Name}' is out of stock!";
            return;
        }

        var existing = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
            if (existing.Quantity < product.StockQuantity)
            {
                existing.Quantity++;
            }
            else
            {
                ErrorMessage = $"Maximum available stock ({product.StockQuantity}) reached for '{product.Name}'.";
            }
        }
        else
        {
            CartItems.Add(new CartItemModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                Barcode = product.Barcode,
                UnitSellingPrice = product.SellingPrice,
                UnitPurchasePrice = product.PurchasePrice,
                AvailableStock = product.StockQuantity,
                TaxRate = product.TaxRate,
                Quantity = 1,
                DiscountPercentage = product.DiscountRate
            });
        }
        RecalculateTotals();
    }

    private async Task SearchBarcodeOrSkuAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        var code = SearchQuery.Trim();
        var product = _allProducts.FirstOrDefault(p => p.Barcode.Equals(code, StringComparison.OrdinalIgnoreCase) || p.SKU.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (product != null)
        {
            AddProduct(product);
            SearchQuery = string.Empty;
        }
        else
        {
            var res = await _apiClient.FindProductByCodeAsync(code);
            if (res.Success && res.Data != null)
            {
                _allProducts.Add(res.Data);
                AddProduct(res.Data);
                SearchQuery = string.Empty;
            }
            else
            {
                ErrorMessage = $"No product found with barcode/SKU: '{code}'";
            }
        }
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(ItemDiscountsTotal));
        OnPropertyChanged(nameof(OverallDiscountAmount));
        OnPropertyChanged(nameof(TotalDiscount));
        OnPropertyChanged(nameof(TotalTax));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(DueAmount));
    }

    private void ClearCart()
    {
        CartItems.Clear();
        OverallDiscountPercent = 0;
        PaidAmount = 0;
        ErrorMessage = string.Empty;
        RecalculateTotals();
    }

    private void HoldCurrentSale()
    {
        if (CartItems.Count == 0) return;

        var hold = new HoldSaleDto
        {
            CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
            TotalEstimate = GrandTotal,
            Items = CartItems.Select(i => new CreateSaleItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                DiscountPercentage = i.DiscountPercentage
            }).ToList()
        };

        HeldSales.Add(hold);
        ClearCart();
        SuccessMessage = $"Sale held (Reference: #{hold.HoldId})";
    }

    private void ResumeSale(HoldSaleDto hold)
    {
        ClearCart();
        foreach (var itemReq in hold.Items)
        {
            var prod = _allProducts.FirstOrDefault(p => p.Id == itemReq.ProductId);
            if (prod != null)
            {
                CartItems.Add(new CartItemModel
                {
                    ProductId = prod.Id,
                    ProductName = prod.Name,
                    SKU = prod.SKU,
                    Barcode = prod.Barcode,
                    UnitSellingPrice = prod.SellingPrice,
                    UnitPurchasePrice = prod.PurchasePrice,
                    AvailableStock = prod.StockQuantity,
                    TaxRate = prod.TaxRate,
                    Quantity = itemReq.Quantity,
                    DiscountPercentage = itemReq.DiscountPercentage
                });
            }
        }
        HeldSales.Remove(hold);
        RecalculateTotals();
    }

    private void OpenPaymentModal()
    {
        if (CartItems.Count == 0)
        {
            ErrorMessage = "Cart is empty. Please add products first.";
            return;
        }

        PaidAmount = GrandTotal; // default full cash
        IsPaymentModalOpen = true;
    }

    private async Task CompleteSaleAsync()
    {
        if (CartItems.Count == 0) return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var req = new CreateSaleRequest
            {
                CustomerId = SelectedCustomer?.Id,
                CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
                CustomerPhone = SelectedCustomer?.Phone ?? "",
                Items = CartItems.Select(i => new CreateSaleItemRequest
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    DiscountPercentage = i.DiscountPercentage
                }).ToList(),
                OverallDiscountPercentage = OverallDiscountPercent,
                PaidAmount = PaidAmount,
                PaymentMethod = SelectedPaymentMethod,
                CashSessionId = _authSession.CurrentCashSession?.Id,
                Notes = "Desktop POS checkout"
            };

            var res = await _apiClient.ProcessSaleAsync(req);
            if (res.Success && res.Data != null)
            {
                CompletedSale = res.Data;
                IsPaymentModalOpen = false;

                // Load receipt text
                var receiptRes = await _apiClient.GetReceiptTextAsync(res.Data.Id);
                ReceiptContent = receiptRes.Success ? receiptRes.Data : "Receipt generated.";
                IsReceiptModalOpen = true;

                // Refresh product stock locally
                foreach (var item in CartItems)
                {
                    var p = _allProducts.FirstOrDefault(x => x.Id == item.ProductId);
                    if (p != null) p.StockQuantity -= item.Quantity;
                }

                ClearCart();
                SuccessMessage = $"Sale completed successfully! Invoice: {res.Data.InvoiceNumber}";
            }
            else
            {
                ErrorMessage = res.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Checkout error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
