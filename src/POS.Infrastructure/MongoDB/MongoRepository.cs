using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Common;
using POS.Domain.Entities;

namespace POS.Infrastructure.MongoDB;

public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<T>();
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _collection.Find(_ => true).ToListAsync(ct);
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _collection.Find(predicate).ToListAsync(ct);
    }

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _collection.Find(predicate).FirstOrDefaultAsync(ct);
    }

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            entity.Id = Guid.NewGuid().ToString("N");
        }
        entity.CreatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(entity, null, ct);
        return entity;
    }

    public async Task<bool> UpdateAsync(T entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<T>.Filter.Eq(x => x.Id, entity.Id);
        var result = await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, ct);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        var result = await _collection.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }

    public async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        if (predicate == null)
            return await _collection.CountDocumentsAsync(_ => true, null, ct);
        return await _collection.CountDocumentsAsync(predicate, null, ct);
    }

    public async Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true,
        CancellationToken ct = default)
    {
        var find = predicate != null ? _collection.Find(predicate) : _collection.Find(_ => true);
        long totalCount = await (predicate != null ? _collection.CountDocumentsAsync(predicate, null, ct) : _collection.CountDocumentsAsync(_ => true, null, ct));

        if (orderBy != null)
        {
            find = isDescending ? find.SortByDescending(orderBy) : find.SortBy(orderBy);
        }
        else
        {
            find = find.SortByDescending(x => x.CreatedAt);
        }

        var items = await find.Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

public static class MongoDbIndexInitializer
{
    public static async Task InitializeIndexesAsync(MongoDbContext dbContext)
    {
        try
        {
            // Product indexes
            var productSkuIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.SKU),
                new CreateIndexOptions { Unique = true, Sparse = true });
            var productBarcodeIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Barcode),
                new CreateIndexOptions { Sparse = true });
            var productNameIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Name));
            await dbContext.Products.Indexes.CreateManyAsync([productSkuIndex, productBarcodeIndex, productNameIndex]);

            // User indexes
            var userUsernameIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true });
            await dbContext.Users.Indexes.CreateOneAsync(userUsernameIndex);

            // Sale indexes
            var saleInvoiceIndex = new CreateIndexModel<Sale>(
                Builders<Sale>.IndexKeys.Ascending(s => s.InvoiceNumber),
                new CreateIndexOptions { Unique = true });
            var saleDateIndex = new CreateIndexModel<Sale>(
                Builders<Sale>.IndexKeys.Descending(s => s.SaleDate));
            var saleCashierIndex = new CreateIndexModel<Sale>(
                Builders<Sale>.IndexKeys.Ascending(s => s.CashierId));
            await dbContext.Sales.Indexes.CreateManyAsync([saleInvoiceIndex, saleDateIndex, saleCashierIndex]);

            // Customer indexes
            var customerPhoneIndex = new CreateIndexModel<Customer>(
                Builders<Customer>.IndexKeys.Ascending(c => c.Phone));
            await dbContext.Customers.Indexes.CreateOneAsync(customerPhoneIndex);

            // Supplier indexes
            var supplierPhoneIndex = new CreateIndexModel<Supplier>(
                Builders<Supplier>.IndexKeys.Ascending(s => s.Phone));
            await dbContext.Suppliers.Indexes.CreateOneAsync(supplierPhoneIndex);
        }
        catch
        {
            // Index creation may proceed or continue if already created
        }
    }
}
