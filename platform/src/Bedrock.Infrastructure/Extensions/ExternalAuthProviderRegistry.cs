using Bedrock.Application.Ports.ExternalAuth;

namespace Bedrock.Infrastructure.Extensions;

/// <summary>
/// Registry multi-impl (F27): resolve <see cref="IExternalAuthProvider"/> theo <see cref="IExternalAuthProvider.Name"/>
/// (case-insensitive). Build map lúc construct từ mọi provider đã đăng ký (IEnumerable — adapter Google/Zalo append).
/// Trùng Name → ném lúc boot (fail-fast). Provider chưa đăng ký → Resolve ném (không silent-null).
/// </summary>
public sealed class ExternalAuthProviderRegistry : IExternalAuthProviderRegistry
{
    private readonly Dictionary<string, IExternalAuthProvider> _byName = new(StringComparer.OrdinalIgnoreCase);

    public ExternalAuthProviderRegistry(IEnumerable<IExternalAuthProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        foreach (var provider in providers)
        {
            if (!_byName.TryAdd(provider.Name, provider))
            {
                throw new InvalidOperationException(
                    $"Trùng external-auth provider Name '{provider.Name}' — mỗi provider phải có Name duy nhất.");
            }
        }
    }

    public IExternalAuthProvider Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _byName.TryGetValue(name, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"Chưa đăng ký external-auth provider '{name}'. Cắm adapter tương ứng ở Host (vd AddGoogleAuth/AddZaloAuth).");
    }
}
