using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Identity.Contracts.Events;
using Identity.Contracts;
using Identity.Domain;

namespace Identity.Application.RefreshToken;

/// <summary>
/// Rotation refresh-token NGUYÊN TỬ (design §7.4, F5/F10). <see cref="ITransactionalUseCase"/> gắn module key để
/// pipeline mở transaction bằng đúng Identity DbContext. Toàn bộ get → reuse-detect → consume-if-not-revoked →
/// insert token mới → outbox → SaveChanges nằm trong transaction đó. Consume dùng UPDATE nguyên tử ở DB nên hai
/// request đồng thời chỉ một request thắng.
/// </summary>
public sealed class RefreshAccessTokenUseCase : ICommandUseCase<RefreshTokenCommand, RefreshTokenResult>
{
    public string PersistenceKey => IdentityModule.PersistenceKey;

    /// <summary>
    /// TTL refresh token mới. Design không chốt con số → chọn 14 ngày (cân bằng UX "đăng nhập lại" vs cửa sổ
    /// rủi ro nếu token rò) — AD-041. Có thể nâng thành Options per-app sau (backward-compat).
    /// </summary>
    internal static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    private const string RotatedReason = "rotated";

    private readonly IRefreshTokenStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IJwtTokenService _jwt;
    private readonly IOutboxWriter _outboxWriter;

    public RefreshAccessTokenUseCase(
        IRefreshTokenStore store,
        IUnitOfWork unitOfWork,
        IClock clock,
        ITokenGenerator tokenGenerator,
        IJwtTokenService jwt,
        IOutboxWriter outboxWriter)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(jwt);
        ArgumentNullException.ThrowIfNull(outboxWriter);
        _store = store;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
        _jwt = jwt;
        _outboxWriter = outboxWriter;
    }

    public Task<Result<RefreshTokenResult>> ExecuteAsync(RefreshTokenCommand input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return RotateAsync(ct);

        async Task<Result<RefreshTokenResult>> RotateAsync(CancellationToken token)
        {
            var now = _clock.UtcNow;
            var presentedHash = Sha256Hex(input.RawRefreshToken);

            var current = await _store.GetByHashAsync(presentedHash, token).ConfigureAwait(false);
            if (current is null || current.ExpiresAt < now)
            {
                return Result<RefreshTokenResult>.Failure(AuthErrors.InvalidRefreshToken);
            }

            // reuse-detection: token ĐÃ revoked mà vẫn được dùng → kẻ tấn công dùng token cũ → thu hồi CẢ family.
            // RevokeFamilyAsync là UPDATE nguyên tử (ghi ngay trong transaction); trả Failure → transaction COMMIT
            // (không exception) nên family-revoke được persist.
            if (current.RevokedAt is not null)
            {
                await _store.RevokeFamilyAsync(current.FamilyId, token).ConfigureAwait(false);
                return Result<RefreshTokenResult>.Failure(AuthErrors.InvalidRefreshToken);
            }

            var newRawToken = _tokenGenerator.NewToken();
            var newTokenId = Guid.CreateVersion7();
            var expiresAt = now.Add(RefreshTokenLifetime);

            // consume-if-not-revoked NGUYÊN TỬ (row-lock ở DB). 0 row → request khác đã consume (race) → thua.
            var consumed = await _store
                .TryConsumeAsync(current.Id, now, RotatedReason, newTokenId, token)
                .ConfigureAwait(false);
            if (!consumed)
            {
                return Result<RefreshTokenResult>.Failure(AuthErrors.InvalidRefreshToken);
            }

            var newSnapshot = new RefreshTokenSnapshot(
                newTokenId, current.UserId, current.FamilyId, Sha256Hex(newRawToken), expiresAt, RevokedAt: null);
            await _store.AddAsync(newSnapshot, token).ConfigureAwait(false);

            // EMIT integration event vào Outbox TRONG CÙNG transaction rotation (N-040 → nay có consumer path
            // worker→RabbitMQ, AD-056). Chỉ enqueue trên đường THÀNH CÔNG (sau consume+insert) → nếu SaveChanges
            // dưới đây fail/rollback thì event KHÔNG được publish (outbox + state all-or-nothing, CP6/F5). Dùng
            // IOutboxWriter (namespace Messaging, KHÔNG Dispatch → CP11 nguyên vẹn: use case không publish thẳng bus).
            // Event.Id = OutboxMessage.Id (AD-029, idempotency đầu-cuối); KHÔNG rò refresh token thô (chỉ UserId).
            await _outboxWriter.EnqueueAsync(
                new UserTokenRefreshedIntegrationEvent(Guid.CreateVersion7(), now, current.UserId), token)
                .ConfigureAwait(false);

            // Enlist consume (ghi ngay) + insert token mới + outbox event vào CÙNG transaction → all-or-nothing (fix F5).
            await _unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);

            var accessToken = _jwt.Issue(BuildIdentity(current.UserId));
            return Result<RefreshTokenResult>.Success(new RefreshTokenResult(accessToken, newRawToken, expiresAt));
        }
    }

    // Claim tối thiểu cho rotation: sub = UserId (AD-023). role/permission/tenant/sid cần user-profile store —
    // ngoài phạm vi rotation skeleton (N-040); module bổ sung khi có user store thật.
    private static ClaimsIdentity BuildIdentity(Guid userId) =>
        new([new Claim("sub", userId.ToString())], authenticationType: "jwt");

    // Hash refresh token bằng SHA-256 hex (design §7.4: hash ← SHA256). Token là 256-bit CSPRNG entropy cao →
    // KHÔNG cần Argon2 (Argon2 dành cho password entropy thấp). Primitive chuẩn, không phải "công nghệ swap được".
    private static string Sha256Hex(string raw) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
