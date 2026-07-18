using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Application;
using Rules.Contracts;
using Rules.Domain;
using Rules.Infrastructure.Persistence;

namespace Rules.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Rules (mirror Identity/ResortConfig/Rooms/GuestAccess). KEYED persistence
/// (P0-1 fix): <see cref="RulesDbContext"/> + repository Draft (<see cref="RuleSet"/>/<see cref="RuleSection"/>/
/// <see cref="RuleSectionTranslation"/>) + Unit of Work theo <see cref="PersistenceKey"/> → resolve đúng module.
/// Use case Draft CRUD (D-Rules.2b) đăng ký factory keyed (mirror Rooms). Rules KHÔNG map Outbox/Inbox (chưa phát event).
/// <para>
/// <see cref="UpsertRuleSectionTranslationUseCase"/> phụ thuộc port bảo mật <c>IHtmlSanitizer</c> (sanitize-on-save) —
/// adapter do Host cấp qua <c>AddStarHillHtml</c> (QR-AD-031). RequirePort(IHtmlSanitizer) đặt ở Host wiring (D-Rules.4);
/// composition root ráp cả hai nên factory dưới resolve được.
/// </para>
/// </summary>
public static class RulesInfrastructureExtensions
{
    /// <summary>Module key persistence của Rules (= schema <c>rules</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = RulesModule.PersistenceKey;

    public static IServiceCollection AddRulesInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "rules"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<RulesDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate Draft KEYED theo RulesDbContext (không dùng generic IRepository<> unkeyed global).
        services.AddBedrockRepository<RulesDbContext, RuleSet>(PersistenceKey);
        services.AddBedrockRepository<RulesDbContext, RuleSection>(PersistenceKey);
        services.AddBedrockRepository<RulesDbContext, RuleSectionTranslation>(PersistenceKey);
        // Publication snapshot (D-Rules.3): repo ghi cho Publish copy đông cứng.
        services.AddBedrockRepository<RulesDbContext, RulePublication>(PersistenceKey);
        services.AddBedrockRepository<RulesDbContext, RulePublicationSection>(PersistenceKey);
        services.AddBedrockRepository<RulesDbContext, RulePublicationSectionTranslation>(PersistenceKey);
        // Acknowledge (D-Rules.4b): repo ghi RuleAcknowledgement (unique (visit,publication) — idempotent CP13).
        services.AddBedrockRepository<RulesDbContext, RuleAcknowledgement>(PersistenceKey);

        // Read-model NỘI-MODULE cho Publish đọc Draft (CQRS-lite — F9 cấm IQueryable ở IRepository). DbContext cụ
        // thể (unkeyed, type riêng module) — mirror EfGuestSessionStore.
        services.AddScoped<IRuleDraftReader, EfRuleDraftReader>();
        services.AddScoped<IRuleAdminReader, EfRuleAdminReader>();

        // Read-model guest đọc publication IsCurrent (D-Rules.4a).
        services.AddScoped<IRulePublicationReader, EfRulePublicationReader>();

        // Query-port stats cho Host Dashboard (QR-AD-002, đếm ack ≥ mốc). Contracts interface — Id trần.
        services.AddScoped<Rules.Contracts.IRulesStatsQuery, EfRulesStatsQuery>();

        // Use case Draft CRUD: factory resolve repo/UoW bằng module key (mirror Rooms). Void command (Update/Delete)
        // khai PersistenceKey → TransactionCommandUseCaseDecorator resolve cùng keyed UoW (nhất quán).
        services.AddScoped<IUseCase<CreateRuleSectionInput, CreateRuleSectionResult>>(sp => new CreateRuleSectionUseCase(
            sp.GetRequiredKeyedService<IRepository<RuleSet>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RuleSection>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>(sp =>
            new UpsertRuleSectionTranslationUseCase(
                sp.GetRequiredKeyedService<IRepository<RuleSection>>(PersistenceKey),
                sp.GetRequiredKeyedService<IRepository<RuleSectionTranslation>>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<Bedrock.Application.Ports.Html.IHtmlSanitizer>()));

        services.AddScoped<ICommandUseCase<UpdateRuleSectionInput>>(sp => new UpdateRuleSectionUseCase(
            sp.GetRequiredKeyedService<IRepository<RuleSection>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<DeleteRuleSectionInput>>(sp => new DeleteRuleSectionUseCase(
            sp.GetRequiredKeyedService<IRepository<RuleSection>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        // Publish snapshot (D-Rules.3): IUseCase value-returning, tự quản transaction hẹp (flip-before-insert) —
        // factory resolve repo publication + UoW keyed (mirror ResolveTokenUseCase/CreateRoom).
        services.AddScoped<IUseCase<PublishRulesInput, PublishRulesResult>>(sp => new PublishRulesUseCase(
            sp.GetRequiredService<IRuleDraftReader>(),
            sp.GetRequiredKeyedService<IRepository<RulePublication>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RulePublicationSection>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RulePublicationSectionTranslation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        // Guest read (D-Rules.4a): read-only, cross-module Contracts (config + i18n resolver). Không transaction.
        services.AddScoped<IUseCase<GetCurrentRulesInput, GetCurrentRulesResult>>(sp => new GetCurrentRulesUseCase(
            sp.GetRequiredService<IRulePublicationReader>(),
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<ResortConfig.Contracts.Localization.ITranslationResolver>()));

        // Admin preview (render Draft như khách, không publish) + history (metadata publication) — D-Rules.3b (Req 8.4). Read-only.
        services.AddScoped<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>>(sp => new GetDraftPreviewUseCase(
            sp.GetRequiredService<IRuleDraftReader>(),
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<ResortConfig.Contracts.Localization.ITranslationResolver>()));

        services.AddScoped<IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>>(sp =>
            new GetPublicationHistoryUseCase(sp.GetRequiredService<IRulePublicationReader>()));

        services.AddScoped<IUseCase<GetRuleAdminDraftInput, GetRuleAdminDraftResult>>(sp => new GetRuleAdminDraftUseCase(
            sp.GetRequiredService<IRuleAdminReader>(),
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<ResortConfig.Contracts.Localization.ITranslationResolver>()));

        // Guest acknowledge (D-Rules.4b, CP13): value-returning write, MỘT insert (mirror CreateRoom — không
        // ITransactionalUseCase). Server đọc IsCurrent + ghi ack idempotent (pre-check + unique backstop).
        services.AddScoped<IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult>>(sp => new AcknowledgeRulesUseCase(
            sp.GetRequiredService<IRulePublicationReader>(),
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<ResortConfig.Contracts.Localization.ITranslationResolver>(),
            sp.GetRequiredKeyedService<IRepository<RuleAcknowledgement>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        // Rule-gate backend (D-Rules.4c, CP3): read-only. Lộ IRuleGate (Rules.Contracts) cho Faq/Concierge/
        // Housekeeping. Resolve keyed repo ack (đọc AnyAsync). Đăng ký dưới interface Contracts (Id trần).
        services.AddScoped<Rules.Contracts.IRuleGate>(sp => new RuleGate(
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<IRulePublicationReader>(),
            sp.GetRequiredKeyedService<IRepository<RuleAcknowledgement>>(PersistenceKey)));

        // Validator module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<CreateRuleSectionInput>, CreateRuleSectionValidator>();
        services.AddTransient<IValidator<UpdateRuleSectionInput>, UpdateRuleSectionValidator>();
        services.AddTransient<IValidator<UpsertRuleSectionTranslationInput>, UpsertRuleSectionTranslationValidator>();
        services.AddTransient<IValidator<PublishRulesInput>, PublishRulesValidator>();

        return services;
    }
}
