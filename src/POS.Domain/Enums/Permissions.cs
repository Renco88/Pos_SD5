namespace POS.Domain.Enums;

public static class Permissions
{
    // POS & Sales
    public const string PosNewSale = "PosNewSale";
    public const string ViewSales = "ViewSales";
    public const string ManageSales = "ManageSales";
    public const string CanApplyDiscount = "CanApplyDiscount";
    public const string CanReturnSale = "CanReturnSale";
    public const string CanHoldSale = "CanHoldSale";

    // Products & Inventory
    public const string ViewProducts = "ViewProducts";
    public const string ManageProducts = "ManageProducts";
    public const string ViewCategories = "ViewCategories";
    public const string ManageCategories = "ManageCategories";
    public const string ViewInventory = "ViewInventory";
    public const string CanAdjustStock = "CanAdjustStock";

    // Purchases & Suppliers
    public const string ViewPurchases = "ViewPurchases";
    public const string ManagePurchases = "ManagePurchases";
    public const string ViewSuppliers = "ViewSuppliers";
    public const string ManageSuppliers = "ManageSuppliers";

    // Customers & Due
    public const string ViewCustomers = "ViewCustomers";
    public const string ManageCustomers = "ManageCustomers";
    public const string ViewDue = "ViewDue";
    public const string CanCollectDue = "CanCollectDue";

    // Expenses & Discounts
    public const string ViewExpenses = "ViewExpenses";
    public const string ManageExpenses = "ManageExpenses";
    public const string ViewDiscounts = "ViewDiscounts";
    public const string ManageDiscounts = "ManageDiscounts";

    // Workers & Reports
    public const string ViewWorkers = "ViewWorkers";
    public const string ManageWorkers = "ManageWorkers";
    public const string ViewReports = "ViewReports";
    public const string ViewOwnReports = "ViewOwnReports";

    // Invoices & Barcodes & Cash
    public const string ViewInvoices = "ViewInvoices";
    public const string PrintInvoices = "PrintInvoices";
    public const string ManageBarcodes = "ManageBarcodes";
    public const string ManageCash = "ManageCash";

    // Admin / System
    public const string ManageSettings = "ManageSettings";
    public const string ManageUsers = "ManageUsers";
    public const string ViewActivityLogs = "ViewActivityLogs";
    public const string ManageBackups = "ManageBackups";

    public static readonly IReadOnlyList<string> EmployerDefaultPermissions =
    [
        PosNewSale, ViewSales, ManageSales, CanApplyDiscount, CanReturnSale, CanHoldSale,
        ViewProducts, ManageProducts, ViewCategories, ManageCategories, ViewInventory, CanAdjustStock,
        ViewPurchases, ManagePurchases, ViewSuppliers, ManageSuppliers,
        ViewCustomers, ManageCustomers, ViewDue, CanCollectDue,
        ViewExpenses, ManageExpenses, ViewDiscounts, ManageDiscounts,
        ViewWorkers, ManageWorkers, ViewReports, ViewOwnReports,
        ViewInvoices, PrintInvoices, ManageBarcodes, ManageCash,
        ManageSettings, ManageUsers, ViewActivityLogs, ManageBackups
    ];

    public static readonly IReadOnlyList<string> WorkerDefaultPermissions =
    [
        PosNewSale, ViewSales, CanApplyDiscount, CanHoldSale,
        ViewProducts, ViewCategories, ViewInventory,
        ViewCustomers, ManageCustomers, ViewDue, CanCollectDue,
        ViewOwnReports, ViewInvoices, PrintInvoices, ManageCash
    ];
}
