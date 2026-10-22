namespace ProductManager.Application.Exceptions;

/// <summary>
/// Raised when a resource was changed by another request between being read and being saved.
/// Surfaces as HTTP 409.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string resource, object key, Exception innerException)
        : base($"{resource} '{key}' was changed by another request. Reload it and try again.", innerException)
    {
        Resource = resource;
        Key = key;
    }

    public string Resource { get; }

    public object Key { get; }
}
