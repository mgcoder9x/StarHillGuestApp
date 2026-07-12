namespace Bedrock.Messaging.Contracts;

/// <summary>
/// Base cho mọi integration event công khai giữa các module (bounded context).
/// Zero-dependency (AD-017): sống ở assembly trung tính <c>Bedrock.Messaging.Contracts</c> để
/// vừa được <c>Bedrock.Application</c> (seam messaging) vừa được mọi <c>Modules.*.Contracts</c>
/// reference — <c>Contracts</c> do đó KHÔNG bao giờ trỏ ngược lên tầng Application.
/// </summary>
/// <param name="Id">Định danh event (dùng cho idempotency phía consumer — Inbox).</param>
/// <param name="OccurredAt">Thời điểm phát sinh (UTC-offset).</param>
public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredAt)
{
    /// <summary>Mã kiểu event ỔN ĐỊNH (hợp đồng cross-module; dùng cho type-registry + routing).</summary>
    public abstract string EventType { get; }

    /// <summary>Phiên bản schema payload (versioning F32 — tolerant reader). Mặc định 1.</summary>
    public virtual int SchemaVersion => 1;
}
