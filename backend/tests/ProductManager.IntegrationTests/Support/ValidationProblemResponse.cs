namespace ProductManager.IntegrationTests.Support;

/// <summary>
/// Deserialisation target for the API's RFC 7807 problem documents, including the extension
/// members the stock endpoints attach.
/// </summary>
public sealed record ValidationProblemResponse
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int Status { get; init; }

    public string? Detail { get; init; }

    public string? Instance { get; init; }

    /// <summary>Per-field messages, present on validation failures.</summary>
    public Dictionary<string, string[]>? Errors { get; init; }

    public int? ProductId { get; init; }

    public int? AvailableStock { get; init; }

    public int? RequestedQuantity { get; init; }
}
