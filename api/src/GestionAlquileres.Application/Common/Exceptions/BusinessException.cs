namespace GestionAlquileres.Application.Common.Exceptions;

/// <summary>
/// Thrown from Application handlers for domain rule violations.
/// ExceptionMiddleware maps InvalidOperationException → HTTP 409 Conflict;
/// BusinessException inherits from InvalidOperationException to leverage that mapping.
/// </summary>
public class BusinessException : InvalidOperationException
{
    public BusinessException(string message) : base(message) { }
}
