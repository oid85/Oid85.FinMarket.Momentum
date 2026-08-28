namespace Oid85.FinMarket.Algo.Core;

public class BaseResponse<TResponseResult>
{
    public Guid TraceId { get; init; } = Guid.NewGuid();
    public TResponseResult? Result { get; init; }
    public ResponseError? Error { get; init; }
    public DateTime MessageDate { get; init; } = DateTime.UtcNow;
}