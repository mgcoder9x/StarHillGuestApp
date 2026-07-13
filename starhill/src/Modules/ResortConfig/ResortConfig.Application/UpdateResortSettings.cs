using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using FluentValidation;
using ResortConfig.Contracts;
using ResortConfig.Domain;

namespace ResortConfig.Application;

/// <summary>
/// Input sửa cấu hình resort (B-Config.3). KHÔNG mang <c>ResortId</c> (single-resort — sửa bản ghi settings duy
/// nhất; ResortId là danh tính, không đổi qua endpoint này). Mọi field vận hành đều sửa được (feature flag,
/// require-rule-ack, cửa sổ/expiry, GuestWebBaseUrl, giới hạn message/rate).
/// </summary>
public sealed record UpdateResortSettingsInput(
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RequireRuleAckForFaq,
    bool RequireRuleAckForChat,
    bool RequireRuleAckForHousekeeping,
    int PortalWindowMinutes,
    int VisitIdleExpiryHours,
    string? GuestWebBaseUrl,
    int MaxMessageLength,
    int MessageRateLimitPerMinute,
    int HousekeepingRateLimitPerHour);

/// <summary>
/// Sửa cấu hình resort (single-resort → một bản ghi <see cref="ResortSettings"/>). Mirror khuôn command Rooms:
/// inject keyed <see cref="IRepository{T}"/> + <see cref="IUnitOfWork"/> (giữ Application ⊥ Infrastructure — KHÔNG
/// chạm DbContext); pipeline lo transaction (<see cref="PersistenceKey"/>) + validation. Bản ghi luôn tồn tại
/// (seeder fail-fast lúc boot — QR-AD-017); phòng thủ null → <see cref="CommonErrors.NotFoundGeneric()"/> (KHÔNG
/// tạo error catalog mới). Concurrency (xmin) để middleware base map (rủi ro thấp: một admin sửa settings).
/// </summary>
public sealed class UpdateResortSettingsUseCase : ICommandUseCase<UpdateResortSettingsInput>
{
    // KEYED: khai module key → TransactionCommandUseCaseDecorator resolve đúng Unit of Work ResortConfig.
    public string PersistenceKey => ResortConfigModule.PersistenceKey;

    private readonly IRepository<ResortSettings> _settings;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateResortSettingsUseCase(IRepository<ResortSettings> settings, IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(UpdateResortSettingsInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Single-resort → bản ghi settings duy nhất. Tracked → mutate → SaveChanges (điểm ghi duy nhất qua UoW).
        var settings = await _settings.FirstOrDefaultAsync(_ => true, ct).ConfigureAwait(false);
        if (settings is null)
        {
            return Result.Failure(CommonErrors.NotFoundGeneric("Cấu hình resort chưa được khởi tạo."));
        }

        settings.FaqEnabled = input.FaqEnabled;
        settings.ChatEnabled = input.ChatEnabled;
        settings.HousekeepingEnabled = input.HousekeepingEnabled;
        settings.RequireRuleAckForFaq = input.RequireRuleAckForFaq;
        settings.RequireRuleAckForChat = input.RequireRuleAckForChat;
        settings.RequireRuleAckForHousekeeping = input.RequireRuleAckForHousekeeping;
        settings.PortalWindowMinutes = input.PortalWindowMinutes;
        settings.VisitIdleExpiryHours = input.VisitIdleExpiryHours;
        settings.GuestWebBaseUrl = input.GuestWebBaseUrl;
        settings.MaxMessageLength = input.MaxMessageLength;
        settings.MessageRateLimitPerMinute = input.MessageRateLimitPerMinute;
        settings.HousekeepingRateLimitPerHour = input.HousekeepingRateLimitPerHour;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Validate settings update (Req 15.6 GuestWebBaseUrl https; các giới hạn dương hợp lý). Chạy ở
/// ValidationCommandUseCaseDecorator TRƯỚC thân → input sai trả <c>validation_error</c>, không đổi trạng thái.
/// </summary>
public sealed class UpdateResortSettingsValidator : AbstractValidator<UpdateResortSettingsInput>
{
    public UpdateResortSettingsValidator()
    {
        // GuestWebBaseUrl: cho phép null (chưa cấu hình) HOẶC phải absolute https (Req 15.6 — camera secure-context).
        RuleFor(x => x.GuestWebBaseUrl)
            .Must(BeNullOrAbsoluteHttps)
            .WithMessage("GuestWebBaseUrl phải là URL https tuyệt đối hợp lệ (hoặc để trống).");

        RuleFor(x => x.PortalWindowMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.VisitIdleExpiryHours).InclusiveBetween(1, 8760);
        RuleFor(x => x.MaxMessageLength).InclusiveBetween(1, 10000);
        RuleFor(x => x.MessageRateLimitPerMinute).InclusiveBetween(1, 1000);
        RuleFor(x => x.HousekeepingRateLimitPerHour).InclusiveBetween(1, 1000);
    }

    private static bool BeNullOrAbsoluteHttps(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
}
