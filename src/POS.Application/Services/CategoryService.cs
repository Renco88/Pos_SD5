using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;

namespace POS.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IActivityLogService _activityLog;

    public CategoryService(
        IRepository<Category> categoryRepo,
        IRepository<Product> productRepo,
        IActivityLogService activityLog)
    {
        _categoryRepo = categoryRepo;
        _productRepo = productRepo;
        _activityLog = activityLog;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var categories = includeInactive
            ? await _categoryRepo.GetAllAsync(ct)
            : await _categoryRepo.FindAsync(c => c.IsActive, ct);

        var allProducts = await _productRepo.GetAllAsync(ct);

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            ProductCount = allProducts.Count(p => p.CategoryId == c.Id && p.IsActive),
            CreatedAt = c.CreatedAt
        }).OrderBy(c => c.Name).ToList();
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(string id, CancellationToken ct = default)
    {
        var cat = await _categoryRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var productCount = (int)await _productRepo.CountAsync(p => p.CategoryId == id && p.IsActive, ct);
        return new CategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            IsActive = cat.IsActive,
            ProductCount = productCount,
            CreatedAt = cat.CreatedAt
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Category name is required.");

        var existing = await _categoryRepo.FindOneAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower(), ct);
        if (existing != null)
            throw new DomainException($"Category '{request.Name}' already exists.");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsActive = true
        };

        var created = await _categoryRepo.AddAsync(category, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateCategory",
            ActivityModule.Categories,
            $"Created category '{category.Name}'.",
            ct: ct);

        return new CategoryDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            IsActive = created.IsActive,
            ProductCount = 0,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(string id, UpdateCategoryRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var cat = await _categoryRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Category name is required.");

        cat.Name = request.Name.Trim();
        if (request.Description != null)
            cat.Description = request.Description.Trim();
        if (request.IsActive.HasValue)
            cat.IsActive = request.IsActive.Value;
        cat.UpdatedAt = DateTime.UtcNow;

        await _categoryRepo.UpdateAsync(cat, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateCategory",
            ActivityModule.Categories,
            $"Updated category '{cat.Name}'.",
            ct: ct);

        var productCount = (int)await _productRepo.CountAsync(p => p.CategoryId == id && p.IsActive, ct);
        return new CategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Description = cat.Description,
            IsActive = cat.IsActive,
            ProductCount = productCount,
            CreatedAt = cat.CreatedAt
        };
    }

    public async Task<bool> DeleteCategoryAsync(string id, string userId, string userName, CancellationToken ct = default)
    {
        var cat = await _categoryRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var productCount = await _productRepo.CountAsync(p => p.CategoryId == id, ct);
        if (productCount > 0)
        {
            // Soft delete by deactivating
            cat.IsActive = false;
            cat.UpdatedAt = DateTime.UtcNow;
            await _categoryRepo.UpdateAsync(cat, ct);
        }
        else
        {
            await _categoryRepo.DeleteAsync(id, ct);
        }

        await _activityLog.LogAsync(
            userId,
            userName,
            "DeleteCategory",
            ActivityModule.Categories,
            $"Deactivated/deleted category '{cat.Name}'.",
            ct: ct);

        return true;
    }
}
