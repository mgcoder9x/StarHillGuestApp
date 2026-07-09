namespace Bedrock.Application.UseCases;

/// <summary>
/// Đánh dấu một input use case là idempotent theo <see cref="IdempotencyKey"/> (giá trị RUNTIME do client cấp →
/// phải là interface per-instance, KHÔNG dùng attribute). <c>IdempotencyUseCaseDecorator</c>/command variant sẽ
/// gọi <see cref="Bedrock.Application.Ports.Caching.IIdempotencyStore.TryBeginAsync"/>: lần đầu → chạy thân;
/// trùng key → trả <c>idempotency_conflict</c> (v1 KHÔNG replay response — design §8, AD-039). Input KHÔNG
/// implement interface này → behavior bỏ qua (pass-through).
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>Khoá idempotency do client cấp (vd request-id). Phải ổn định giữa các lần retry của cùng thao tác.</summary>
    string IdempotencyKey { get; }
}
