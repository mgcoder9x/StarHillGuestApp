using System.Reflection;
using Bedrock.Application.Behaviors;
using Bedrock.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Application.DependencyInjection;

/// <summary>
/// Kết nối pipeline behaviors (§8, F13) vào DI: bọc use case theo thứ tự
/// <c>Logging → Authorization → Validation → Idempotency → Transaction → UseCase</c> bằng Scrutor
/// <c>Decorate</c> (open-generic — AD-037; marker-scan tự viết ở <see cref="BedrockRegistrationExtensions"/> giữ nguyên).
/// <para>
/// <b>THỨ TỰ GỌI (BẮT BUỘC):</b> Host phải gọi <see cref="AddBedrockCore"/> SAU khi đã đăng ký use case
/// (<c>AddBedrockConventions</c>) — Scrutor chỉ bọc được service đã có mặt. Không có use case → TryDecorate no-op
/// (không ném) để host/test tối thiểu vẫn boot.
/// </para>
/// <para>
/// <b>Nesting:</b> lời gọi <c>TryDecorate</c> ĐẦU tạo lớp TRONG CÙNG, lời gọi CUỐI tạo lớp NGOÀI CÙNG → phải
/// đăng ký từ trong (Transaction) ra ngoài (Logging). Transaction chỉ áp cho <see cref="ICommandUseCase{TInput}"/>
/// (AD-040); họ value-returning không có Transaction (query đọc không mở transaction thừa).
/// </para>
/// <para>
/// <b>IDEMPOTENCY (A-12/AD-085):</b> chỉ gọi MỘT lần ở composition root. Gọi lần 2 → NÉM (fail-loud) vì sẽ bọc
/// pipeline HAI lớp (validation/authorization/idempotency/transaction chạy 2 lần) + validator trùng.
/// </para>
/// </summary>
public static class BedrockCoreExtensions
{
    public static IServiceCollection AddBedrockCore(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        // A-12: IDEMPOTENCY GUARD — AddBedrockCore chỉ được gọi MỘT lần ở composition root. Gọi lần 2 sẽ khiến
        // Scrutor Decorate bọc pipeline HAI lớp (Validation/Authorization/Idempotency/Transaction chạy 2 lần =
        // sai nghiêm trọng: transaction lồng, idempotency claim 2 lần, validate 2 lần) + đăng ký validator trùng.
        // Fail-loud (không no-op âm thầm) để lộ lỗi composition ngay (F35/R13, đồng nhất AD-071 idempotent-register).
        if (services.Any(descriptor => descriptor.ServiceType == typeof(BedrockCoreMarker)))
        {
            throw new InvalidOperationException(
                "AddBedrockCore đã được gọi rồi — chỉ gọi MỘT lần ở composition root (Host), SAU khi mọi module đã "
                + "đăng ký use case. Gọi lại sẽ bọc pipeline behaviors HAI lớp (validation/authorization/idempotency/"
                + "transaction chạy 2 lần) và đăng ký validator trùng.");
        }

        services.AddSingleton(BedrockCoreMarker.Instance);

        RegisterValidators(services, assemblies);
        DecoratePipeline(services);
        return services;
    }

    /// <summary>Sentinel đánh dấu <see cref="AddBedrockCore"/> đã chạy trên collection này (idempotency guard A-12).
    /// Đăng ký dạng INSTANCE → DI không dựng lại (ValidateOnBuild an toàn); ctor private chặn tạo ngoài.</summary>
    private sealed class BedrockCoreMarker
    {
        public static readonly BedrockCoreMarker Instance = new();

        private BedrockCoreMarker()
        {
        }
    }

    /// <summary>
    /// Quét + đăng ký (transient) mọi implementation đóng của <see cref="IValidator{T}"/> trong các assembly ứng dụng
    /// để <c>ValidationUseCaseDecorator</c> nhận qua <c>IEnumerable&lt;IValidator&lt;T&gt;&gt;</c> (không kéo thêm
    /// package FluentValidation.DependencyInjection — giữ lõi tối thiểu dependency, AD-018).
    /// </summary>
    private static void RegisterValidators(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                foreach (var @interface in type.GetInterfaces())
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                    {
                        services.AddTransient(@interface, type);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Bọc pipeline behaviors. TryDecorate (Scrutor) — không ném nếu chưa có use case nào đăng ký.
    /// Thứ tự trong→ngoài: Transaction (command) → Idempotency → Validation → Authorization → Logging.
    /// </summary>
    private static void DecoratePipeline(IServiceCollection services)
    {
        // --- Họ value-returning IUseCase<,> (query + command-trả-giá-trị): KHÔNG Transaction (AD-040) ---
        services.TryDecorate(typeof(IUseCase<,>), typeof(IdempotencyUseCaseDecorator<,>));
        services.TryDecorate(typeof(IUseCase<,>), typeof(ValidationUseCaseDecorator<,>));
        services.TryDecorate(typeof(IUseCase<,>), typeof(AuthorizationUseCaseDecorator<,>));
        services.TryDecorate(typeof(IUseCase<,>), typeof(LoggingUseCaseDecorator<,>));

        // --- Họ ICommandUseCase<> (ghi thuần): có Transaction trong cùng ---
        services.TryDecorate(typeof(ICommandUseCase<>), typeof(TransactionCommandUseCaseDecorator<>));
        services.TryDecorate(typeof(ICommandUseCase<>), typeof(IdempotencyCommandUseCaseDecorator<>));
        services.TryDecorate(typeof(ICommandUseCase<>), typeof(ValidationCommandUseCaseDecorator<>));
        services.TryDecorate(typeof(ICommandUseCase<>), typeof(AuthorizationCommandUseCaseDecorator<>));
        services.TryDecorate(typeof(ICommandUseCase<>), typeof(LoggingCommandUseCaseDecorator<>));
    }
}
