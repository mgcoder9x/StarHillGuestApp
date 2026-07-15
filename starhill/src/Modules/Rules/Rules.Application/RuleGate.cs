using Bedrock.Application.Ports.Persistence;
using Bedrock.Domain.Results;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;
using Rules.Domain;

namespace Rules.Application;

/// <summary>
/// Impl rule-gate backend (CP3/QR-AD-030). Đọc cờ <c>RequireRuleAckFor{Feature}</c> qua
/// <see cref="IResortGuestConfigQuery"/>; cờ TẮT → cho qua ngay. Cờ BẬT → kiểm tồn tại
/// <c>RuleAcknowledgement(GuestVisitId, RulePublicationId=IsCurrent.Id)</c> — chính bản khách đang phải đọc
/// (server-authoritative, cùng nguồn sự thật với AcknowledgeRulesUseCase — CP13). Thiếu ack → <c>rule_ack_required</c>.
/// <para>
/// FAIL-CLOSED: cấu hình nền thiếu → <c>configuration_unavailable</c> (không "bật ngầm" cho qua). Cờ bật nhưng
/// resort CHƯA publish bản nào (không có IsCurrent) → chặn <c>rule_ack_required</c> (khách không thể đã-ack bản
/// không tồn tại; admin phải publish trước) — an toàn hơn cho qua.
/// </para>
/// Read-only (không ghi) → KHÔNG transaction. Consumer gọi qua <see cref="IRuleGate"/> (Rules.Contracts, Id trần).
/// </summary>
public sealed class RuleGate : IRuleGate
{
    private readonly IResortGuestConfigQuery _configQuery;
    private readonly IRulePublicationReader _reader;
    private readonly IRepository<RuleAcknowledgement> _acks;

    public RuleGate(
        IResortGuestConfigQuery configQuery,
        IRulePublicationReader reader,
        IRepository<RuleAcknowledgement> acks)
    {
        _configQuery = configQuery;
        _reader = reader;
        _acks = acks;
    }

    public async Task<Result> EnsureAcknowledgedAsync(
        Guid resortId, Guid guestVisitId, GuestFeature feature, CancellationToken ct = default)
    {
        var config = await _configQuery.GetAsync(resortId, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Result.Failure(RulesErrors.ConfigurationUnavailable);
        }

        var required = feature switch
        {
            GuestFeature.Faq => config.RequireRuleAckForFaq,
            GuestFeature.Chat => config.RequireRuleAckForChat,
            GuestFeature.Housekeeping => config.RequireRuleAckForHousekeeping,
            _ => false,
        };
        if (!required)
        {
            return Result.Success(); // cờ tắt → không gate.
        }

        var snapshot = await _reader.LoadCurrentAsync(resortId, ct).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Result.Failure(RulesErrors.RuleAckRequired); // yêu cầu ack nhưng chưa có bản publish → chặn.
        }

        var acked = await _acks
            .AnyAsync(a => a.GuestVisitId == guestVisitId && a.RulePublicationId == snapshot.PublicationId, ct)
            .ConfigureAwait(false);

        return acked ? Result.Success() : Result.Failure(RulesErrors.RuleAckRequired);
    }
}
