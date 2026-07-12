namespace Bedrock.Domain.Events;

/// <summary>
/// Marker cho domain event (in-process, xảy ra trong biên một aggregate/transaction).
/// Được thu thập trên <see cref="Entities.Entity"/> và dispatch trong SaveChanges (R33).
/// Cố ý là marker interface: contract kiểu học, không có thành viên chung.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces — marker contract cho domain event là chủ đích.
public interface IDomainEvent;
#pragma warning restore CA1040
