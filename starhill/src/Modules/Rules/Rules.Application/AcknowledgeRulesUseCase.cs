using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Khách xác nhận đã đọc nội quy — SERVER-AUTHORITATIVE (CP13/QR-AD-030). Server TỰ đọc <c>RulePublication IsCurrent</c>
/// của resort để quyết định ack cho bản nào; KHÔNG tin version/publicationId client gửi (input không mang các field
/// đó — bất biến bằng thiết kế, không chỉ bằng kiểm tra). Ack gắn <c>GuestVisitId</c> (Req 3.7); idempotent theo
/// unique <c>(GuestVisitId, RulePublicationId)</c>.
/// <para>
/// <b>Idempotency hai lớp (fix tận gốc, không dựa may rủi):</b> (1) PRE-CHECK <see cref="IRepository{T}.AnyAsync"/> —
/// đã ack đúng bản IsCurrent trong visit → trả <c>AlreadyAcknowledged=true</c>, KHÔNG insert (chạy đúng mọi provider,
/// phủ ca re-ack thường); (2) BACKSTOP race — hai ack đồng thời cùng (visit,publication) lọt pre-check → SaveChanges
/// vi phạm <c>ux_rule_ack_visit_publication</c> → <see cref="UniqueConstraintViolationException"/> (base QR-AD-010,
/// Npgsql 23505) → coi như đã ack. Ghi MỘT lần (một insert = nguyên tử), không cần transaction bao (mirror CreateRoom).
/// </para>
/// Publish version mới ⇒ <c>IsCurrent</c> mới ⇒ ack cũ (publication khác) không thỏa pre-check ⇒ khách phải ack lại
/// (Req 3.8/3.9). Fail-closed cấu hình nền thiếu (<c>configuration_unavailable</c>); chưa publish → <c>rules_unavailable</c>.
/// </summary>
public sealed class AcknowledgeRulesUseCase : IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult>
{
    private readonly IRulePublicationReader _reader;
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly ITranslationResolver _translationResolver;
    private readonly IRepository<RuleAcknowledgement> _acks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcknowledgeRulesUseCase(
        IRulePublicationReader reader,
        IResortGuestConfigQuery configQuery,
        ITranslationResolver translationResolver,
        IRepository<RuleAcknowledgement> acks,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _reader = reader;
        _configQuery = configQuery;
        _translationResolver = translationResolver;
        _acks = acks;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AcknowledgeRulesResult>> ExecuteAsync(
        AcknowledgeRulesInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Fail-closed cấu hình nền (nhất quán guest read/resolve — QR-AD-024).
        var config = await _configQuery.GetAsync(input.ResortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure<AcknowledgeRulesResult>(RulesErrors.ConfigurationUnavailable);
        }

        // CP13: server đọc bản IsCurrent — KHÔNG tin client. Chưa publish → không có gì để ack.
        var snapshot = await _reader.LoadCurrentAsync(input.ResortId, ct).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Result.Failure<AcknowledgeRulesResult>(RulesErrors.RulesUnavailable);
        }

        var language = _translationResolver.MatchSupported(
            input.RequestedLanguage, config.EnabledLanguageCodes, config.DefaultLanguageCode);

        // (1) Pre-check idempotent — đã ack đúng bản IsCurrent này trong visit → không tạo trùng (mọi provider).
        var alreadyAcked = await _acks
            .AnyAsync(a => a.GuestVisitId == input.GuestVisitId && a.RulePublicationId == snapshot.PublicationId, ct)
            .ConfigureAwait(false);
        if (alreadyAcked)
        {
            return Result.Success(new AcknowledgeRulesResult(snapshot.PublicationId, snapshot.Version, AlreadyAcknowledged: true));
        }

        _acks.Add(new RuleAcknowledgement
        {
            ResortId = input.ResortId,
            RoomId = input.RoomId,
            GuestSessionId = input.GuestSessionId,
            GuestVisitId = input.GuestVisitId,
            RulePublicationId = snapshot.PublicationId,
            Version = snapshot.Version,
            LanguageCode = language,
            AcceptedAt = _clock.UtcNow,
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // (2) Backstop race: hai ack đồng thời cùng (visit,publication) → ux_rule_ack_visit_publication.
            return Result.Success(new AcknowledgeRulesResult(snapshot.PublicationId, snapshot.Version, AlreadyAcknowledged: true));
        }

        return Result.Success(new AcknowledgeRulesResult(snapshot.PublicationId, snapshot.Version, AlreadyAcknowledged: false));
    }
}
