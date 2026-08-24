using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using POS.Application.DTOs;
using POS.Domain.Exceptions;

namespace POS.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            _logger.LogError(ex, "Unhandled exception occurred at {Method} {Path}. TraceId: {TraceId}. Message: {Message}",
                requestMethod, requestPath, traceId, ex.Message);

            await HandleExceptionAsync(context, ex, traceId, requestPath);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId, string requestPath)
    {
        context.Response.ContentType = "application/json";

        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            DiscountLimitExceededException => HttpStatusCode.Forbidden,
            UnauthorizedDomainException => HttpStatusCode.Forbidden,
            InsufficientStockException => HttpStatusCode.BadRequest,
            DomainException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        var message = statusCode == HttpStatusCode.InternalServerError
            ? $"An internal server error occurred. Please contact system support. Reference: {traceId}"
            : exception.Message;

        var errors = statusCode == HttpStatusCode.InternalServerError
            ? new List<string> { "Internal server error", $"Reference ID: {traceId}", $"Path: {requestPath}" }
            : new List<string> { exception.Message };

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Data = new
            {
                TraceId = traceId,
                RequestPath = requestPath,
                StatusCode = (int)statusCode,
                Timestamp = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        return context.Response.WriteAsync(json);
    }
}
