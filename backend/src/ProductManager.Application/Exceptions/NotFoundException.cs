namespace ProductManager.Application.Exceptions;

/// <summary>
/// Raised when an operation targets a resource that does not exist. Surfaces as HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
    }

    public string Resource { get; }

    public object Key { get; }
}
