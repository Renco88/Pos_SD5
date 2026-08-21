using System;
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class CashSession : BaseEntity
{
    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal CashDueCollections { get; set; }
    public decimal CashAdjustments { get; set; }

    public decimal ExpectedCash => OpeningFloat + CashSales + CashDueCollections + CashAdjustments - CashExpenses;
    public decimal? ActualCash { get; set; }
    public decimal? Difference => ActualCash.HasValue ? ActualCash.Value - ExpectedCash : null;

    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public string Notes { get; set; } = string.Empty;
}
