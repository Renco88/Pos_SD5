using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Enums;

namespace POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts([FromQuery] ProductFilterRequest request)
    {
        var result = await _productService.GetProductsAsync(request, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(string id)
    {
        var product = await _productService.GetProductByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> FindBySkuOrBarcode(string code)
    {
        var product = await _productService.FindBySkuOrBarcodeAsync(code, HttpContext.RequestAborted);
        if (product == null)
            return NotFound(ApiResponse<ProductDto>.Fail($"No product found for code '{code}'."));
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetLowStock()
    {
        var list = await _productService.GetLowStockProductsAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<ProductDto>>.Ok(list));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _productService.CreateProductAsync(request, userId, userName, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetProductById), new { id = created.Id }, ApiResponse<ProductDto>.Ok(created, "Product created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _productService.UpdateProductAsync(id, request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProductDto>.Ok(updated, "Product updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var deleted = await _productService.DeleteProductAsync(id, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(deleted, "Product deleted successfully"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var list = await _categoryService.GetAllCategoriesAsync(includeInactive, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<CategoryDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(string id)
    {
        var cat = await _categoryService.GetCategoryByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<CategoryDto>.Ok(cat));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _categoryService.CreateCategoryAsync(request, userId, userName, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<CategoryDto>.Ok(created, "Category created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(string id, [FromBody] UpdateCategoryRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _categoryService.UpdateCategoryAsync(id, request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<CategoryDto>.Ok(updated, "Category updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var deleted = await _categoryService.DeleteCategoryAsync(id, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(deleted, "Category deleted successfully"));
    }
}
