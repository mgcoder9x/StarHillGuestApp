using System;
using ResortQr.Application.Identity;
using ResortQr.Infrastructure.Security;
using ResortQr.UnitTests.TestDoubles;

namespace ResortQr.UnitTests.Identity;

/// <summary>Ráp use case auth với adapter THẬT (Argon2/JWT/CSPRNG/SHA-256) + fake store/uow/clock. DB-free.</summary>
internal sealed class AuthTestHarness
{
    private const string SigningKey = "test-signing-key-0123456789-abcdefghijklmnop";
    private static readonly string[] DefaultRoles = ["Admin"];

    public FakeUserAuthStore Users { get; } = new();
    public FakeRefreshTokenStore RefreshTokens { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeDateTimeProvider Clock { get; }
    public Argon2idPasswordHasher Hasher { get; }
    public Sha256RefreshTokenHasher RefreshHasher { get; } = new();
    public CryptoTokenGenerator TokenGenerator { get; } = new();
    public JwtTokenService Jwt { get; }
    public RefreshTokenOptions Options { get; } = new() { RefreshTokenDays = 30 };

    public AuthTestHarness(DateTimeOffset now, int hashIterations = 2)
    {
        Clock = new FakeDateTimeProvider(now);
        Hasher = new Argon2idPasswordHasher(new PasswordHashingOptions
        {
            MemoryKib = 1024,
            Iterations = hashIterations,
            DegreeOfParallelism = 1,
            SaltSize = 16,
            HashSize = 32,
        });
        Jwt = new JwtTokenService(
            new JwtOptions { SigningKey = SigningKey, Issuer = "it", Audience = "it", AccessTokenMinutes = 15 },
            Clock);
    }

    public LoginUseCase Login() =>
        new(Users, RefreshTokens, Hasher, RefreshHasher, Jwt, TokenGenerator, Clock, UnitOfWork, Options);

    public RefreshTokenUseCase Refresh() =>
        new(RefreshTokens, Users, RefreshHasher, Jwt, TokenGenerator, Clock, UnitOfWork, Options);

    public LogoutUseCase Logout() => new(RefreshTokens, RefreshHasher, Clock, UnitOfWork);

    public Guid AddUser(string email, string password, bool active = true) =>
        AddUserWithHash(email, Hasher.Hash(password), active);

    public Guid AddUserWithHash(string email, string passwordHash, bool active = true)
    {
        var id = Guid.CreateVersion7();
        Users.Add(new AuthenticatedUser(id, email, passwordHash, DefaultRoles, active));
        return id;
    }
}
