namespace ProductManager.Domain.Exceptions;

/// <summary>
/// Raised when an operation would leave a product in a state its invariants forbid,
/// for example a blank name or a negative price.
/// </summary>
public sealed class InvalidProductStateException : DomainException
{
    public InvalidProductStateException(string message)
        : base(message)
    {
    }
}
