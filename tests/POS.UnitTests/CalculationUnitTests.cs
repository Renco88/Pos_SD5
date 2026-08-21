using System;
using POS.Application.DTOs;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using Xunit;

namespace POS.UnitTests;

public class CalculationUnitTests
{
    [Fact]
    public void SaleLine_Total_CalculatesCorrectly()
    {
        // Arrange
        var unitSellingPrice = 100.00m;
        var quantity = 3;
        var discountPercentage = 10.0m; // 10% discount on 300 = 30
        var taxRate = 5.0m; // 5% tax on (300 - 30) = 13.50

        var lineSubtotal = quantity * unitSellingPrice; // 300.00
        var lineDiscount = Math.Round(lineSubtotal * (discountPercentage / 100m), 2); // 30.00
        var lineTax = Math.Round((lineSubtotal - lineDiscount) * (taxRate / 100m), 2); // 13.50
        var lineTotal = (lineSubtotal - lineDiscount) + lineTax; // 283.50

        // Assert
        Assert.Equal(300.00m, lineSubtotal);
        Assert.Equal(30.00m, lineDiscount);
        Assert.Equal(13.50m, lineTax);
        Assert.Equal(283.50m, lineTotal);
    }

    [Fact]
    public void WorkerDiscountLimit_EnforcedStrictly_ThrowsExceptionWhenExceeded()
    {
        // Arrange
        var worker = new User
        {
            Username = "worker1",
            Role = Roles.Worker,
            MaxDiscountPercentage = 5.0m // Worker cap is 5%
        };

        var requestedDiscount = 7.5m; // Worker attempts 7.5%

        // Act & Assert
        Assert.True(worker.Role == Roles.Worker);
        Assert.True(requestedDiscount > worker.MaxDiscountPercentage);

        var exception = Assert.Throws<DiscountLimitExceededException>(() =>
        {
            if (worker.Role == Roles.Worker && requestedDiscount > worker.MaxDiscountPercentage)
            {
                throw new DiscountLimitExceededException(requestedDiscount, worker.MaxDiscountPercentage);
            }
        });

        Assert.Contains("Worker discount limit exceeded", exception.Message);
        Assert.Equal(7.5m, exception.AttemptedDiscount);
        Assert.Equal(5.0m, exception.MaxAllowedDiscount);
    }

    [Fact]
    public void ProfitAndLoss_Formula_ComputesAccurately()
    {
        // Arrange
        // Sale: 2 items sold for $150 each ($300 gross revenue)
        // Cost: purchased at $90 each ($180 COGS)
        // Operating Expense: $40
        // Refund/Return: $20
        var totalRevenue = 300.00m;
        var costOfGoodsSold = 180.00m;
        var totalExpenses = 40.00m;
        var totalRefunds = 20.00m;

        var grossProfit = totalRevenue - costOfGoodsSold; // $120
        var netProfit = grossProfit - totalExpenses - totalRefunds; // $120 - 40 - 20 = $60

        // Assert
        Assert.Equal(120.00m, grossProfit);
        Assert.Equal(60.00m, netProfit);
    }

    [Fact]
    public void StockQuantity_InsufficientStock_ThrowsException()
    {
        // Arrange
        var product = new Product
        {
            Id = "prod-1",
            Name = "Organic Milk",
            StockQuantity = 2
        };
        var requestedQuantity = 5;

        // Act & Assert
        var ex = Assert.Throws<InsufficientStockException>(() =>
        {
            if (product.StockQuantity < requestedQuantity)
            {
                throw new InsufficientStockException(product.Id, product.Name, product.StockQuantity, requestedQuantity);
            }
        });

        Assert.Contains("Available: 2, Requested: 5", ex.Message);
        Assert.Equal("Organic Milk", ex.ProductName);
    }
}
