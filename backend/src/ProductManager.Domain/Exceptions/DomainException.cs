namespace ProductManager.Domain.Exceptions;

/// <summary>
/// Base type for violations of a business rule expressed in the domain model.
/// The API layer translates these into meaningful HTTP status codes.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}
