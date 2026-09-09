using System.Net;
using System.Text.Json;
using FluentValidation;
using GestionAlquileres.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestionAlquileres.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";
            var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { errors }));
        }
        // Domain rule violation. Caught before the generic handler; BusinessException derives from
        // InvalidOperationException, so its message is safe to surface. Plain InvalidOperationExceptions
        // (framework/internal) fall through to the generic 500 and never leak their message.
        catch (BusinessException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        // Return a constant message rather than reflecting ex.Message (audit B1): this is a broad
        // catch, so any future UnauthorizedAccessException would otherwise leak its internal text.
        // The real detail is logged server-side only.
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogInformation(ex, "Unauthorized request to {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Credenciales inválidas." }));
        }
        // A concurrent write changed the row between read and save (optimistic concurrency, audit M7).
        catch (DbUpdateConcurrencyException)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "El registro fue modificado por otra operación simultánea. Reintentá."
            }));
        }
        // Unique-index violation (e.g. a duplicate adjustment for the same contract+date). Translate
        // Postgres 23505 to 409 instead of the generic 500 (audit M5). Non-unique DbUpdateExceptions
        // are left to fall through to the 500 handler by the exception filter.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "La operación genera un registro duplicado."
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    }
}
