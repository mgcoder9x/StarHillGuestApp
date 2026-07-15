using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using Faq.Application;
using Faq.Contracts;
using Faq.Domain;
using Faq.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Faq.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Faq (mirror Rules/Rooms/GuestAccess). KEYED persistence (P0-1 fix):
/// <see cref="FaqDbContext"/> + repository (FaqCategory/FaqCategoryTranslation/FaqItem/FaqItemTranslation) + Unit of
/// Work theo <see cref="PersistenceKey"/> → resolve đúng module. Faq KHÔNG map Outbox/Inbox (chưa phát event).
/// E-Faq.1 = persistence nền; use case CRUD/reorder/read đăng ký ở E-Faq.2..4 (factory keyed, mirror Rules).
/// </summary>
public static class FaqInfrastructureExtensions
{
    /// <summary>Module key persistence của Faq (= schema <c>faq</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = FaqModule.PersistenceKey;

    public static IServiceCollection AddFaqInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "faq"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<FaqDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate KEYED theo FaqDbContext (không dùng generic IRepository<> unkeyed global).
        services.AddBedrockRepository<FaqDbContext, FaqCategory>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqCategoryTranslation>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqItem>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqItemTranslation>(PersistenceKey);

        // Read-model guest đọc cây FAQ active (E-Faq.4). DbContext cụ thể (unkeyed, type riêng module) — mirror EfFaqReader.
        services.AddScoped<IFaqReader, EfFaqReader>();

        // Guest read cây (E-Faq.4, CP3/CP5): read-only, cross-module Contracts (config + i18n resolver + rule-gate).
        // Faq là consumer ĐẦU TIÊN của IRuleGate — gate đặt TRONG use case (defense-in-depth). Không transaction.
        services.AddScoped<IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>>(sp => new GetGuestFaqTreeUseCase(
            sp.GetRequiredService<IFaqReader>(),
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<ResortConfig.Contracts.Localization.ITranslationResolver>(),
            sp.GetRequiredService<Rules.Contracts.IRuleGate>()));

        // Use case Admin CRUD (E-Faq.2): factory resolve repo/UoW bằng module key (mirror Rules). Value-returning
        // IUseCase tự quản một SaveChanges; void command ICommandUseCase khai PersistenceKey (decorator resolve keyed UoW).
        services.AddScoped<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>>(sp => new CreateFaqCategoryUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<UpdateFaqCategoryInput>>(sp => new UpdateFaqCategoryUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<DeleteFaqCategoryInput>>(sp => new DeleteFaqCategoryUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>>(sp =>
            new UpsertFaqCategoryTranslationUseCase(
                sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
                sp.GetRequiredKeyedService<IRepository<FaqCategoryTranslation>>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<Bedrock.Application.Ports.Html.IHtmlSanitizer>()));

        services.AddScoped<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>(sp => new CreateFaqItemUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<UpdateFaqItemInput>>(sp => new UpdateFaqItemUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<DeleteFaqItemInput>>(sp => new DeleteFaqItemUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>>(sp =>
            new UpsertFaqItemTranslationUseCase(
                sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
                sp.GetRequiredKeyedService<IRepository<FaqItemTranslation>>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<Bedrock.Application.Ports.Html.IHtmlSanitizer>()));

        // Reorder (E-Faq.3): void command khai PersistenceKey (batch SortOrder nguyên tử một transaction).
        services.AddScoped<ICommandUseCase<ReorderFaqCategoriesInput>>(sp => new ReorderFaqCategoriesUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqCategory>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<ReorderFaqItemsInput>>(sp => new ReorderFaqItemsUseCase(
            sp.GetRequiredKeyedService<IRepository<FaqItem>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        // Validator module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<CreateFaqCategoryInput>, CreateFaqCategoryValidator>();
        services.AddTransient<IValidator<UpdateFaqCategoryInput>, UpdateFaqCategoryValidator>();
        services.AddTransient<IValidator<UpsertFaqCategoryTranslationInput>, UpsertFaqCategoryTranslationValidator>();
        services.AddTransient<IValidator<CreateFaqItemInput>, CreateFaqItemValidator>();
        services.AddTransient<IValidator<UpdateFaqItemInput>, UpdateFaqItemValidator>();
        services.AddTransient<IValidator<UpsertFaqItemTranslationInput>, UpsertFaqItemTranslationValidator>();
        services.AddTransient<IValidator<ReorderFaqCategoriesInput>, ReorderFaqCategoriesValidator>();
        services.AddTransient<IValidator<ReorderFaqItemsInput>, ReorderFaqItemsValidator>();

        return services;
    }
}
