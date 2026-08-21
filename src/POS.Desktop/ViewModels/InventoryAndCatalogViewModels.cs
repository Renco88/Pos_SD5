using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using POS.Application.DTOs;
using POS.Desktop.Services;
using POS.Domain.Enums;

namespace POS.Desktop.ViewModels;

public class SalesManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;

    private string _searchTerm = string.Empty;
    private SaleDto? _selectedSale;
    private bool _isDetailsModalOpen;
    private string _receiptText = string.Empty;

    public ObservableCollection<SaleDto> Sales { get; } = [];

    public string SearchTerm
    {
        get => _searchTerm;
        set { if (SetProperty(ref _searchTerm, value)) _ = LoadSalesAsync(); }
    }

    public SaleDto? SelectedSale
    {
        get => _selectedSale;
        set => SetProperty(ref _selectedSale, value);
    }

    public bool IsDetailsModalOpen
    {
        get => _isDetailsModalOpen;
        set => SetProperty(ref _isDetailsModalOpen, value);
    }

    public string ReceiptText
    {
        get => _receiptText;
        set => SetProperty(ref _receiptText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand CloseDetailsCommand { get; }
    public ICommand CancelSaleCommand { get; }

    public SalesManagementViewModel(IApiClient apiClient, IAuthSession authSession)
    {
        _apiClient = apiClient;
        _authSession = authSession;

        RefreshCommand = new AsyncRelayCommand(LoadSalesAsync);
        ViewDetailsCommand = new AsyncRelayCommand(ViewSaleDetailsAsync);
        CloseDetailsCommand = new RelayCommand(() => IsDetailsModalOpen = false);
        CancelSaleCommand = new AsyncRelayCommand(param => param is SaleDto s ? CancelSaleAsync(s) : Task.CompletedTask);

        _ = LoadSalesAsync();
    }

    public async Task LoadSalesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetSalesAsync(new SaleFilterRequest { SearchTerm = SearchTerm, PageSize = 100 });
            if (res.Success && res.Data != null)
            {
                Sales.Clear();
                foreach (var s in res.Data.Items) Sales.Add(s);
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

    private async Task ViewSaleDetailsAsync(object? param)
    {
        if (param is SaleDto s)
        {
            SelectedSale = s;
            var textRes = await _apiClient.GetReceiptTextAsync(s.Id);
            ReceiptText = textRes.Success ? textRes.Data : "Receipt details.";
            IsDetailsModalOpen = true;
        }
    }

    private async Task CancelSaleAsync(object? param)
    {
        if (param is SaleDto s)
        {
            var res = await _apiClient.CancelSaleAsync(s.Id, "Cancelled by user");
            if (res.Success)
            {
                SuccessMessage = $"Sale {s.InvoiceNumber} cancelled.";
                await LoadSalesAsync();
            }
            else
            {
                ErrorMessage = res.Message;
            }
        }
    }
}

public class ProductManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IAuthSession _authSession;

    private string _searchTerm = string.Empty;
    private ProductDto? _selectedProduct;
    private bool _isEditModalOpen;
    private bool _isNewProduct;

    // Form fields
    private string _formName = string.Empty;
    private string _formSku = string.Empty;
    private string _formBarcode = string.Empty;
    private string _formCategoryId = string.Empty;
    private decimal _formSellingPrice;
    private decimal _formPurchasePrice;
    private int _formStockQuantity;
    private int _formMinStockLevel = 5;

    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public string SearchTerm
    {
        get => _searchTerm;
        set { if (SetProperty(ref _searchTerm, value)) _ = LoadProductsAsync(); }
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public bool IsEditModalOpen
    {
        get => _isEditModalOpen;
        set => SetProperty(ref _isEditModalOpen, value);
    }

    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }
    public string FormSku { get => _formSku; set => SetProperty(ref _formSku, value); }
    public string FormBarcode { get => _formBarcode; set => SetProperty(ref _formBarcode, value); }
    public string FormCategoryId { get => _formCategoryId; set => SetProperty(ref _formCategoryId, value); }
    public decimal FormSellingPrice { get => _formSellingPrice; set => SetProperty(ref _formSellingPrice, value); }
    public decimal FormPurchasePrice { get => _formPurchasePrice; set => SetProperty(ref _formPurchasePrice, value); }
    public int FormStockQuantity { get => _formStockQuantity; set => SetProperty(ref _formStockQuantity, value); }
    public int FormMinStockLevel { get => _formMinStockLevel; set => SetProperty(ref _formMinStockLevel, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand OpenEditModalCommand { get; }
    public ICommand SaveProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand CloseModalCommand { get; }

    public ProductManagementViewModel(IApiClient apiClient, IAuthSession authSession)
    {
        _apiClient = apiClient;
        _authSession = authSession;

        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);
        OpenCreateModalCommand = new RelayCommand(OpenCreateModal);
        OpenEditModalCommand = new RelayCommand(p => { if (p is ProductDto prod) OpenEditModal(prod); });
        SaveProductCommand = new AsyncRelayCommand(SaveProductAsync);
        DeleteProductCommand = new AsyncRelayCommand(param => param is ProductDto p ? DeleteProductAsync(p) : Task.CompletedTask);
        CloseModalCommand = new RelayCommand(() => IsEditModalOpen = false);

        _ = LoadCategoriesAsync();
        _ = LoadProductsAsync();
    }

    public async Task LoadCategoriesAsync()
    {
        var res = await _apiClient.GetCategoriesAsync();
        if (res.Success && res.Data != null)
        {
            Categories.Clear();
            foreach (var c in res.Data) Categories.Add(c);
        }
    }

    public async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetProductsAsync(new ProductFilterRequest { SearchTerm = SearchTerm, PageSize = 100 });
            if (res.Success && res.Data != null)
            {
                Products.Clear();
                foreach (var p in res.Data.Items) Products.Add(p);
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

    private void OpenCreateModal()
    {
        _isNewProduct = true;
        FormName = "";
        FormSku = "SKU-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        FormBarcode = FormSku;
        FormCategoryId = Categories.FirstOrDefault()?.Id ?? "";
        FormSellingPrice = 0;
        FormPurchasePrice = 0;
        FormStockQuantity = 10;
        FormMinStockLevel = 5;
        IsEditModalOpen = true;
    }

    private void OpenEditModal(ProductDto p)
    {
        _isNewProduct = false;
        SelectedProduct = p;
        FormName = p.Name;
        FormSku = p.SKU;
        FormBarcode = p.Barcode;
        FormCategoryId = p.CategoryId;
        FormSellingPrice = p.SellingPrice;
        FormPurchasePrice = p.PurchasePrice;
        FormStockQuantity = p.StockQuantity;
        FormMinStockLevel = p.MinStockLevel;
        IsEditModalOpen = true;
    }

    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "Product name is required.";
            return;
        }

        if (Categories.Count > 0 && string.IsNullOrWhiteSpace(FormCategoryId))
        {
            ErrorMessage = "Please select a category.";
            return;
        }

        IsBusy = true;
        try
        {
            if (_isNewProduct)
            {
                var req = new CreateProductRequest
                {
                    Name = FormName,
                    SKU = FormSku,
                    Barcode = FormBarcode,
                    CategoryId = FormCategoryId,
                    SellingPrice = FormSellingPrice,
                    PurchasePrice = FormPurchasePrice,
                    StockQuantity = FormStockQuantity,
                    MinStockLevel = FormMinStockLevel
                };
                var res = await _apiClient.CreateProductAsync(req);
                if (res.Success)
                {
                    SuccessMessage = $"Product '{FormName}' created!";
                    IsEditModalOpen = false;
                    await LoadProductsAsync();
                }
                else ErrorMessage = res.Message;
            }
            else if (SelectedProduct != null)
            {
                var req = new UpdateProductRequest
                {
                    Name = FormName,
                    SKU = FormSku,
                    Barcode = FormBarcode,
                    CategoryId = FormCategoryId,
                    SellingPrice = FormSellingPrice,
                    PurchasePrice = FormPurchasePrice,
                    MinStockLevel = FormMinStockLevel,
                    IsActive = true
                };
                var res = await _apiClient.UpdateProductAsync(SelectedProduct.Id, req);
                if (res.Success)
                {
                    SuccessMessage = $"Product '{FormName}' updated!";
                    IsEditModalOpen = false;
                    await LoadProductsAsync();
                }
                else ErrorMessage = res.Message;
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

    private async Task DeleteProductAsync(object? param)
    {
        if (param is ProductDto p)
        {
            var res = await _apiClient.DeleteProductAsync(p.Id);
            if (res.Success)
            {
                SuccessMessage = $"Product '{p.Name}' deleted.";
                await LoadProductsAsync();
            }
        }
    }
}

public class CategoryManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private CategoryDto? _selectedCategory;
    private bool _isModalOpen;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public CategoryDto? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
    public bool IsModalOpen { get => _isModalOpen; set => SetProperty(ref _isModalOpen, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateModalCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CloseModalCommand { get; }

    public CategoryManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadCategoriesAsync);
        OpenCreateModalCommand = new RelayCommand(() => { Name = ""; Description = ""; IsModalOpen = true; });
        SaveCommand = new AsyncRelayCommand(SaveCategoryAsync);
        DeleteCommand = new AsyncRelayCommand(param => param is CategoryDto c ? DeleteCategoryAsync(c) : Task.CompletedTask);
        CloseModalCommand = new RelayCommand(() => IsModalOpen = false);

        _ = LoadCategoriesAsync();
    }

    public async Task LoadCategoriesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetCategoriesAsync();
            if (res.Success && res.Data != null)
            {
                Categories.Clear();
                foreach (var c in res.Data) Categories.Add(c);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        var res = await _apiClient.CreateCategoryAsync(new CreateCategoryRequest { Name = Name, Description = Description });
        if (res.Success)
        {
            IsModalOpen = false;
            await LoadCategoriesAsync();
        }
        else ErrorMessage = res.Message;
    }

    private async Task DeleteCategoryAsync(object? param)
    {
        if (param is CategoryDto c)
        {
            var res = await _apiClient.DeleteCategoryAsync(c.Id);
            if (res.Success) await LoadCategoriesAsync();
        }
    }
}

public class InventoryManagementViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    public ObservableCollection<ProductDto> LowStockProducts { get; } = [];

    public ICommand RefreshCommand { get; }

    public InventoryManagementViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadInventoryAsync);
        _ = LoadInventoryAsync();
    }

    public async Task LoadInventoryAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _apiClient.GetLowStockProductsAsync();
            if (res.Success && res.Data != null)
            {
                LowStockProducts.Clear();
                foreach (var p in res.Data) LowStockProducts.Add(p);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
