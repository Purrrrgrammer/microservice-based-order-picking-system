namespace Common.Messages.PickingCompleted;

public record PickingCompletedMessage(
    long OrderId,
    long PickingId,
    Guid ExternalOrderId,
    string UserId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string PickingStatus,
    string? Notes,
    List<PickingResultItem> Items);