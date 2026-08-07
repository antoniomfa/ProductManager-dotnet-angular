namespace ProductManager.Domain.Exceptions;

/// <summary>
/// Raised when a stock decrement would take a product's stock below zero.
/// Surfaces as HTTP 409 Conflict: the request was well-formed but the
/// current state of the resource does not permit it.
/// </summary>
public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(int productId, int available, int requested)
        : base($"Product {productId} has {available} unit(s) in stock; cannot remove {requested}.")
    {
        ProductId = productId;
        Available = available;
        Requested = requested;
    }

    public int ProductId { get; }

    public int Available { get; }

    public int Requested { get; }
}
