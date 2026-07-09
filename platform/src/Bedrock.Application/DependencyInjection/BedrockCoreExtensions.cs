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
/// </summary>
public static class BedrockCoreExtensions
{
    public static IServiceCollection AddBedrockCore(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        RegisterValidators(services, assemblies);
        DecoratePipeline(services);
        return services;
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
